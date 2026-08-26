#!/usr/bin/env python3
"""Pipeline Meshy -> Arkana: concept art vira personagem 3D riggado.

A CHAVE NUNCA VIVE AQUI. Ela e' lida de MESHY_API_KEY (variavel de ambiente)
ou de tools/meshy/.env, que o .gitignore ja' bloqueia. Nada de chave em codigo,
em commit, ou colada em chat.

Uso:
    python tools/meshy/meshy.py gerar 01-pyra       # concept -> GLB texturizado
    python tools/meshy/meshy.py riggar 01-pyra      # GLB -> esqueleto + anims
    python tools/meshy/meshy.py tudo 01-pyra        # os dois em sequencia
    python tools/meshy/meshy.py status <task_id>    # consulta uma tarefa
    python tools/meshy/meshy.py saldo               # testa a chave

Saida: godot/characters/modelos/<slug>/
"""
import base64
import json
import os
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

API = "https://api.meshy.ai/openapi/v1"
RAIZ = Path(__file__).resolve().parents[2]
SAIDA = RAIZ / "godot" / "characters" / "modelos"

# Orcamento do Arkana (docs/PASSOS_GRAFICOS.md secao 6): mobile, 7 magos em tela.
ALVO_POLY = 15000
RESOLUCAO_TEXTURA = "2k"      # 4k nao cabe em celular medio
POSE = "t-pose"               # exigida para riggar bem
TETO_FACES_RIG = 300000       # limite duro do endpoint de rigging


def chave() -> str:
    """Le a chave do ambiente ou do .env local. Erro claro se faltar."""
    k = os.environ.get("MESHY_API_KEY", "").strip()
    if k:
        return k
    env = Path(__file__).parent / ".env"
    if env.exists():
        for linha in env.read_text(encoding="utf-8").splitlines():
            linha = linha.strip()
            if linha.startswith("MESHY_API_KEY"):
                return linha.split("=", 1)[1].strip().strip('"').strip("'")
    sys.exit(
        "ERRO: chave nao encontrada.\n"
        "  Crie tools/meshy/.env com a linha:\n"
        "      MESHY_API_KEY=msy_sua_chave_aqui\n"
        "  (o .gitignore ja' bloqueia .env — a chave nao vai para o git)\n"
        "  Ou exporte: export MESHY_API_KEY=msy_..."
    )


def pedir(metodo: str, rota: str, corpo: dict | None = None) -> dict:
    req = urllib.request.Request(
        f"{API}{rota}",
        method=metodo,
        data=json.dumps(corpo).encode() if corpo else None,
        headers={
            "Authorization": f"Bearer {chave()}",
            "Content-Type": "application/json",
        },
    )
    try:
        with urllib.request.urlopen(req, timeout=60) as r:
            return json.loads(r.read() or "{}")
    except urllib.error.HTTPError as e:
        detalhe = e.read().decode(errors="replace")[:400]
        dicas = {
            401: "chave invalida ou revogada",
            402: "creditos insuficientes na conta Meshy",
            429: "limite de taxa — espere e tente de novo",
        }
        sys.exit(f"ERRO HTTP {e.code} ({dicas.get(e.code, 'ver detalhe')}): {detalhe}")


def esperar(rota: str, task_id: str, rotulo: str) -> dict:
    """Poll ate' terminar. 5s e' o intervalo que a doc da Meshy usa."""
    ultimo = -1
    while True:
        t = pedir("GET", f"{rota}/{task_id}")
        st, pr = t.get("status"), t.get("progress", 0)
        if pr != ultimo:
            print(f"  {rotulo}: {st} {pr}%", flush=True)
            ultimo = pr
        if st == "SUCCEEDED":
            return t
        if st in ("FAILED", "CANCELED"):
            erro = (t.get("task_error") or {}).get("message", st)
            sys.exit(f"ERRO: tarefa {st} — {erro}")
        time.sleep(5)


def concept_em_data_uri(slug: str) -> str:
    """Manda SO' A VISTA FRONTAL, nunca a folha inteira.

    Licao paga com 30 creditos em 21/08: mandando o character sheet completo
    (3 vistas + paleta + insets) a Meshy trata tudo como UMA imagem e extruda
    um PAINEL PLANO — a malha saiu com 1.9 de largura e 0.05 de profundidade,
    e o rigging morreu com "pose estimation failed". Uma figura por imagem.

    Desde a entrega de 24/08 cada arquivo ja' e' UMA figura isolada, entao o
    recorte da folha antiga deixou de existir. Lemos a vista frontal direto.

    LIMITACAO CONHECIDA: mandamos 1 imagem (/image-to-3d) e o atelie tem 4
    vistas por mago. O endpoint /multi-image-to-3d aceita ate' 4 e daria um
    modelo melhor — mas as vistas do lote atual estao erradas (o "3/4" e' a
    frontal repetida e nao existe perfil de 90 graus), entao mandar as quatro
    hoje nao ajudaria. Ver docs/pipeline-arte/PERSONAGENS/02-REFERENCIAS_MULTI_VIEW.md
    """
    arte = RAIZ / "personagens" / slug / "arte"
    frente = arte / "_originais" / "master-reference-frente.png"
    if not frente.exists():
        sys.exit(f"ERRO: falta {frente}\n"
                 f"       o atelie de arte nao vai para o git; veja personagens/00-LEIA.md")
    b64 = base64.b64encode(frente.read_bytes()).decode()
    print(f"  imagem: {frente.name} ({frente.stat().st_size // 1024} KB)")
    return f"data:image/png;base64,{b64}"


def baixar(url: str, destino: Path) -> None:
    destino.parent.mkdir(parents=True, exist_ok=True)
    with urllib.request.urlopen(url, timeout=300) as r:
        destino.write_bytes(r.read())
    print(f"  salvo: {destino.relative_to(RAIZ)} ({destino.stat().st_size // 1024} KB)")


def gerar(slug: str) -> str:
    print(f"[1/2] Gerando malha 3D de {slug}")
    corpo = {
        "image_url": concept_em_data_uri(slug),
        "ai_model": "latest",
        "topology": "triangle",       # engine quer triangulo; quad so' p/ escultura
        "target_polycount": ALVO_POLY,
        "should_remesh": True,
        "should_texture": True,
        "enable_pbr": True,           # normal + roughness + metallic (secao 6 do doc)
        "texture_resolution": RESOLUCAO_TEXTURA,
        "pose_mode": POSE,            # T-pose: o rigger precisa dela
        "target_formats": ["glb"],    # glb so'; FBX da' problema no Godot
    }
    tid = pedir("POST", "/image-to-3d", corpo)["result"]
    print(f"  task: {tid}")
    t = esperar("/image-to-3d", tid, "malha")

    dest = SAIDA / slug
    baixar(t["model_urls"]["glb"], dest / f"{slug}.glb")
    for i, tex in enumerate(t.get("texture_urls") or []):
        for papel, chave_url in (
            ("albedo", "base_color"), ("normal", "normal"),
            ("roughness", "roughness"), ("metallic", "metallic"),
        ):
            if tex.get(chave_url):
                sufixo = f"_{i}" if i else ""
                baixar(tex[chave_url], dest / f"{slug}_{papel}{sufixo}.png")
    print(f"  creditos usados: {t.get('consumed_credits', '?')}")
    return tid


def riggar(slug: str, task_id: str | None = None) -> None:
    print(f"[2/2] Riggando {slug}")
    glb = SAIDA / slug / f"{slug}.glb"
    if task_id:
        corpo = {"input_task_id": task_id}
    elif glb.exists():
        b64 = base64.b64encode(glb.read_bytes()).decode()
        corpo = {"model_url": f"data:model/gltf-binary;base64,{b64}"}
    else:
        sys.exit(f"ERRO: {glb} nao existe. Rode 'gerar' antes.")
    corpo["height_meters"] = altura(slug)

    tid = pedir("POST", "/rigging", corpo)["result"]
    print(f"  task: {tid}")
    t = esperar("/rigging", tid, "rig")

    # A doc diz "model_urls", mas a resposta REAL do /rigging traz tudo em
    # "result", com as animacoes aninhadas em basic_animations (visto 21/08).
    dest = SAIDA / slug
    r = t.get("result") or t.get("model_urls") or {}
    alvos = {
        "rigged_character_glb_url": f"{slug}_rigged.glb",
        "rigged_character_fbx_url": f"{slug}_rigged.fbx",
    }
    for k, nome in alvos.items():
        if isinstance(r.get(k), str):
            baixar(r[k], dest / nome)
    for k, url in (r.get("basic_animations") or {}).items():
        # PRECEDENCIA: `and` liga mais forte que `or`, entao sem os parenteses
        # isto virava (str and endswith) OR ("glb_url" in k) — e uma chave com
        # glb_url e valor None passava direto para baixar(), que espera string.
        if isinstance(url, str) and (url.endswith((".glb", ".fbx")) or "glb_url" in k):
            ext = "fbx" if "fbx" in k else "glb"
            baixar(url, dest / "anim" / f"{k.replace('_url','')}.{ext}")
    print(f"  creditos usados: {t.get('consumed_credits', '?')}")


def altura(slug: str) -> float:
    """Le a altura da ficha do personagem — o rig escala por ela.
    Brok tem 1,40m e Basalto 2,30m: mandar 1.7 para todos apaga a raca."""
    md = RAIZ / "personagens" / f"{slug}.md"
    if md.exists():
        import re
        m = re.search(r"(\d),(\d{2})\s*m", md.read_text(encoding="utf-8"))
        if m:
            h = float(f"{m.group(1)}.{m.group(2)}")
            print(f"  altura da ficha: {h}m")
            return h
    print("  altura: 1.7m (padrao — ficha sem medida)")
    return 1.7


def saldo() -> None:
    """Bate na API so' para provar que a chave funciona."""
    pedir("GET", "/image-to-3d?page_size=1")
    print("OK: chave valida, API respondendo.")


def main() -> None:
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    cmd = sys.argv[1]
    arg = sys.argv[2] if len(sys.argv) > 2 else None

    if cmd == "saldo":
        saldo()
    elif cmd == "status":
        print(json.dumps(pedir("GET", f"/image-to-3d/{arg}"), indent=2)[:2000])
    elif cmd == "gerar":
        gerar(arg or sys.exit("informe o slug, ex.: 01-pyra"))
    elif cmd == "riggar":
        riggar(arg or sys.exit("informe o slug, ex.: 01-pyra"))
    elif cmd == "tudo":
        slug = arg or sys.exit("informe o slug, ex.: 01-pyra")
        riggar(slug, gerar(slug))
    else:
        sys.exit(__doc__)


if __name__ == "__main__":
    main()
