# -*- coding: utf-8 -*-
"""Validacao OFFLINE dos .glb game-ready. NAO consome credito da Meshy: le'
apenas arquivos que ja' estao em disco.

    python tools/meshy/test_glb.py

Existe porque o portao do Godot cobre o que o JOGO ve' (animacao resolvida,
laco, triangulo), e nao o que o ARQUIVO promete: versao do glTF, escala contra a
ficha, e movimento de raiz. Root motion horizontal e' o mais traicoeiro — a
animacao andaria sozinha por cima do passo de fisica e ninguem entende por que o
personagem desliza.
"""
import json, os, re, struct, sys, glob

RAIZ = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
MODELOS = os.path.join(RAIZ, "godot", "characters", "modelos")
falhas = []


def ok(cond, msg):
    print(("  ok    - " if cond else "  FALHA - ") + msg)
    if not cond:
        falhas.append(msg)


def ler_glb(caminho):
    with open(caminho, "rb") as f:
        magic, ver, _ = struct.unpack("<III", f.read(12))
        assert magic == 0x46546C67, "nao e' um GLB"
        clen, _ = struct.unpack("<II", f.read(8))
        return ver, json.loads(f.read(clen).decode("utf-8"))


def altura_da_ficha(slug):
    for f in glob.glob(os.path.join(RAIZ, "personagens", "%s-*.md" % slug)):
        m = re.search(r"(\d),(\d{2})\s*m", open(f, encoding="utf-8").read())
        if m:
            return float("%s.%s" % (m.group(1), m.group(2)))
    return None


def valida(nome_glb, slug_ficha):
    caminho = os.path.join(MODELOS, nome_glb)
    if not os.path.exists(caminho):
        ok(False, "%s existe" % nome_glb)
        return
    print("[%s]" % nome_glb)
    ver, j = ler_glb(caminho)
    ok(ver == 2, "container GLB versao 2 (glTF 2.0)")
    ok(j.get("asset", {}).get("version", "").startswith("2."),
       "asset.version = %s" % j.get("asset", {}).get("version"))
    ok(len(j.get("meshes", [])) >= 1, "tem malha (%d)" % len(j.get("meshes", [])))
    ok(len(j.get("skins", [])) >= 1, "tem esqueleto (%d skin)" % len(j.get("skins", [])))

    anims = [a.get("name", "") for a in j.get("animations", [])]
    ok(len(anims) >= 3, "tem ao menos 3 animacoes: %s" % anims)

    # ROOT MOTION: translacao no no' raiz da animacao faz o corpo andar sozinho
    # por cima do passo de fisica. Procuramos canal de translation apontando
    # para um no' que nao tenha pai (a raiz da cena).
    filhos = set()
    for n in j.get("nodes", []):
        for c in n.get("children", []) or []:
            filhos.add(c)
    raizes = [i for i in range(len(j.get("nodes", []))) if i not in filhos]
    com_root_motion = []
    for a in j.get("animations", []):
        for ch in a.get("channels", []):
            alvo = ch.get("target", {})
            if alvo.get("path") == "translation" and alvo.get("node") in raizes:
                com_root_motion.append(a.get("name", "?"))
                break
    ok(not com_root_motion,
       "nenhuma animacao move a RAIZ (root motion horizontal): %s" % (com_root_motion or "nenhuma"))

    h = altura_da_ficha(slug_ficha)
    if h is not None:
        print("  info    - a ficha diz %.2f m (a escala real e' medida no Godot," % h)
        print("            por characters/_shot_mago.gd — aqui so' registramos)")


valida("pyra.glb", "01")
valida("brok.glb", "13")
print()
print("RESULTADO: %s" % ("OK — 0 falhas" if not falhas else "%d FALHAS" % len(falhas)))
sys.exit(0 if not falhas else 1)
