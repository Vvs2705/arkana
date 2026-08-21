#!/usr/bin/env python3
"""Junta os GLBs da Meshy num unico modelo com as animacoes que o jogo exige.

POR QUE existe: o Mage.gd (contrato do projeto) so' aceita um modelo externo se
ele tiver as tres animacoes 'idle', 'run' e 'cast' — senao cai no procedural, de
proposito. A Meshy entrega walking e running em ARQUIVOS SEPARADOS e nenhum
idle/cast. Este script mescla tudo num arquivo so' e batiza as animacoes com os
nomes do contrato.

Todos os GLBs do rigging compartilham malha e esqueleto, entao da' para copiar
as animacoes de um para o outro: e' so' reindexar accessors/bufferViews e
concatenar o buffer binario.

Uso:
    python tools/meshy/montar_glb.py 01-pyra pyra
       (le godot/characters/modelos/01-pyra/*.glb -> escreve modelos/pyra.glb)
"""
import json
import struct
import sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[2]
MODELOS = RAIZ / "godot" / "characters" / "modelos"
CAST_MIN = 0.30      # Mage.gd rejeita cast <= CAST_FIRE_TIME (0.22): folga aqui


def ler_glb(p: Path):
    b = p.read_bytes()
    if b[:4] != b"glTF":
        sys.exit(f"ERRO: {p.name} nao e' GLB")
    off, js, bina = 12, None, b""
    while off < len(b):
        clen, ctype = struct.unpack("<II", b[off:off + 8])
        dados = b[off + 8:off + 8 + clen]
        if ctype == 0x4E4F534A:
            js = json.loads(dados.decode("utf-8"))
        elif ctype == 0x004E4942:
            bina = dados
        off += 8 + clen + (-(clen) % 4)
    return js, bina


def escrever_glb(g: dict, bina: bytes, destino: Path) -> None:
    g["buffers"] = [{"byteLength": len(bina)}]
    js = json.dumps(g, separators=(",", ":")).encode("utf-8")
    js += b" " * (-len(js) % 4)
    bina += b"\x00" * (-len(bina) % 4)
    total = 12 + 8 + len(js) + 8 + len(bina)
    destino.write_bytes(
        b"glTF" + struct.pack("<II", 2, total)
        + struct.pack("<II", len(js), 0x4E4F534A) + js
        + struct.pack("<II", len(bina), 0x004E4942) + bina)


def importar_animacao(base, bbin, doador, dbin, nome):
    """Copia UMA animacao do doador para a base, reindexando o que ela usa."""
    if not doador.get("animations"):
        return None
    anim = json.loads(json.dumps(doador["animations"][0]))  # copia profunda

    mapa = {}   # accessor do doador -> accessor na base
    for canal in anim["channels"]:
        s = anim["samplers"][canal["sampler"]]
        for chave in ("input", "output"):
            ai = s[chave]
            if ai in mapa:
                s[chave] = mapa[ai]
                continue
            acc = json.loads(json.dumps(doador["accessors"][ai]))
            bv = doador["bufferViews"][acc["bufferView"]]
            ini = bv.get("byteOffset", 0)
            trecho = dbin[ini:ini + bv["byteLength"]]
            # concatena alinhado a 4 bytes
            pad = -len(bbin[0]) % 4
            bbin[0] += b"\x00" * pad
            novo_bv = {"buffer": 0, "byteOffset": len(bbin[0]),
                       "byteLength": bv["byteLength"]}
            if "byteStride" in bv:
                novo_bv["byteStride"] = bv["byteStride"]
            bbin[0] += trecho
            base.setdefault("bufferViews", []).append(novo_bv)
            acc["bufferView"] = len(base["bufferViews"]) - 1
            acc.pop("byteOffset", None)
            base.setdefault("accessors", []).append(acc)
            mapa[ai] = len(base["accessors"]) - 1
            s[chave] = mapa[ai]

    anim["name"] = nome
    base.setdefault("animations", []).append(anim)
    return anim


def matar_root_motion(base, bbin, anim, raiz_idx):
    """Zera o deslocamento HORIZONTAL do osso raiz na animacao.

    A CAUSA REAL DA PATINACAO (medida em 21/08): as animacoes da Meshy vem com
    ROOT MOTION — o Hips anda 7,7m para frente dentro do proprio clipe. Como o
    jogo TAMBEM move o personagem por codigo (velocity), os dois deslocamentos
    se somam e o pe nunca casa com o chao. Nenhum ajuste de speed_scale
    conserta isso; o deslocamento tem que sair da animacao.

    Mantem o eixo Y (o sobe-e-desce da passada e' o que da' peso).
    Devolve a distancia horizontal que foi removida = a PASSADA REAL do ciclo.
    """
    for c in anim["channels"]:
        if c["target"]["path"] != "translation" or c["target"]["node"] != raiz_idx:
            continue
        s = anim["samplers"][c["sampler"]]
        acc = base["accessors"][s["output"]]
        bv = base["bufferViews"][acc["bufferView"]]
        ini = bv.get("byteOffset", 0)
        n = acc["count"]
        vals = list(struct.unpack("<" + "f" * (n * 3),
                                  bbin[0][ini:ini + n * 12]))
        xs, zs = vals[0::3], vals[2::3]
        percorrido = ((max(xs) - min(xs)) ** 2 + (max(zs) - min(zs)) ** 2) ** 0.5
        # trava X e Z no primeiro quadro; Y continua livre
        for i in range(n):
            vals[i * 3] = xs[0]
            vals[i * 3 + 2] = zs[0]
        novo = struct.pack("<" + "f" * (n * 3), *vals)
        bbin[0] = bbin[0][:ini] + novo + bbin[0][ini + len(novo):]
        acc.pop("min", None)
        acc.pop("max", None)
        return percorrido
    return 0.0


def duracao(g, anim):
    fim = 0.0
    for c in anim["channels"]:
        acc = g["accessors"][anim["samplers"][c["sampler"]]["input"]]
        if "max" in acc:
            fim = max(fim, float(acc["max"][0]))
    return fim


def main():
    if len(sys.argv) < 3:
        sys.exit(__doc__)
    slug, saida = sys.argv[1], sys.argv[2]
    pasta = MODELOS / slug
    base_p = pasta / f"{slug}_rigged.glb"
    if not base_p.exists():
        sys.exit(f"ERRO: {base_p} nao existe (rode 'meshy.py riggar' antes)")

    base, bbin_bytes = ler_glb(base_p)
    bbin = [bbin_bytes]
    base["animations"] = []          # descarta o clip0 e monta do zero

    # Prefere as animacoes da BIBLIOTECA da Meshy (endpoint /animations, 3
    # creditos cada) — idle parado de verdade e conjuracao de mago. Se nao
    # existirem, cai no walking/running que vem junto do rigging.
    anim = pasta / "anim"
    fontes = {
        "idle": anim / "idle_real.glb" if (anim / "idle_real.glb").exists()
                else anim / "walking_glb.glb",
        "run": anim / "running_glb.glb",
        "cast": anim / "cast_real.glb" if (anim / "cast_real.glb").exists()
                else anim / "walking_glb.glb",
    }
    raiz = (base.get("skins") or [{}])[0].get("joints", [None])[0]
    a = None
    passada_run = 0.0
    for nome, pf in fontes.items():
        if not pf.exists():
            sys.exit(f"ERRO: {pf} nao existe")
        d, dbin = ler_glb(pf)
        a = importar_animacao(base, bbin, d, dbin, nome)
        andou = matar_root_motion(base, bbin, a, raiz) if raiz is not None else 0.0
        if nome == "run":
            passada_run = andou
        print(f"  animacao '{nome}' <- {pf.name}"
              + (f"  [root motion removido: {andou:.2f}m]" if andou > 0.01 else ""))

    dur = duracao(base, a)
    if dur <= CAST_MIN:
        sys.exit(f"ERRO: cast ficou com {dur:.2f}s; o Mage exige > 0.22s")

    destino = MODELOS / f"{saida}.glb"
    escrever_glb(base, bbin[0], destino)
    if passada_run > 0.01:
        print(f"  >> PASSADA REAL do 'run': {passada_run:.2f} m por ciclo.")
        print(f"     Balance.ANIM.run_stride_m tem que valer ESTE numero.")
    print(f"OK: {destino.relative_to(RAIZ)} "
          f"({destino.stat().st_size // 1024} KB) "
          f"animacoes={[x['name'] for x in base['animations']]} cast={dur:.2f}s")


if __name__ == "__main__":
    main()
