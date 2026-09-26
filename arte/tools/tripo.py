#!/usr/bin/env python3
"""Tripo -> ARKANA: concept art (ou texto) vira peca 3D em GLB, em LOTE (plano de 25.000 creditos/mes, 25/09/2026).

A CHAVE NUNCA VIVE AQUI: vem de TRIPO_API_KEY (ambiente) ou de arte/tools/.env.tripo (o .gitignore bloqueia .env.*).
Uso (na raiz do repo):
    python arte/tools/tripo.py saldo
    python arte/tools/tripo.py imagem <slug> <imagem.png> [faces]         # 1 peca: image-to-3D (PBR)
    python arte/tools/tripo.py texto  <slug> "descricao em ingles" [faces]
    python arte/tools/tripo.py lote   <lista.txt> [faces]                  # linhas: slug|imagem.png  ou  slug|texto:descricao
    python arte/tools/tripo.py baixar <slug> <task_id>                     # rebaixa uma tarefa (sem gastar credito)
Saida: arte/cenario/<slug>/<slug>.glb (+ tripo.json com a tarefa). Faces padrao 12.000 (celular; a IlhaMestre usa LOD).
Modelo v3.1 (mais recente) com textura PBR; o LOTE roda ate' 8 tarefas em paralelo. Cada geracao custa ~20-40 creditos.
"""
import asyncio
import json
import os
import sys
from pathlib import Path

RAIZ = Path(__file__).resolve().parents[2]
SAIDA = RAIZ / "arte" / "cenario"
MODELO = "v3.1-20260211"
FACES = 12000
PARALELO = 8


def chave() -> str:
    k = os.environ.get("TRIPO_API_KEY", "").strip()
    if k:
        return k
    env = Path(__file__).parent / ".env.tripo"
    if env.exists():
        for linha in env.read_text(encoding="utf-8").splitlines():
            if linha.strip().startswith("TRIPO_API_KEY"):
                return linha.split("=", 1)[1].strip().strip('"').strip("'")
    sys.exit("ERRO: chave nao encontrada. Crie arte/tools/.env.tripo com  TRIPO_API_KEY=tsk_...  (fica fora do git).")


async def gerar(client, slug: str, fonte: str, faces: int):
    """fonte = caminho de imagem ou 'texto:descricao'. Devolve (slug, task_id, arquivos)."""
    from tripo3d import TaskStatus
    if fonte.startswith("texto:"):
        tid = await client.text_to_model(prompt=fonte[6:], model_version=MODELO, face_limit=faces, texture=True, pbr=True)
    else:
        tid = await client.image_to_model(image=str(fonte), model_version=MODELO, face_limit=faces, texture=True, pbr=True)
    print(f"[{slug}] tarefa {tid}")
    task = await client.wait_for_task(tid, verbose=False)
    if task.status != TaskStatus.SUCCESS:
        print(f"[{slug}] FALHOU: {task.status}")
        return slug, tid, {}
    return slug, tid, await baixar(client, slug, task)


async def baixar(client, slug: str, task):
    pasta = SAIDA / slug
    pasta.mkdir(parents=True, exist_ok=True)
    arquivos = await client.download_task_models(task, str(pasta))
    for tipo, caminho in arquivos.items():
        if caminho.lower().endswith(".glb"):
            alvo = pasta / f"{slug}.glb"
            Path(caminho).replace(alvo)
            arquivos[tipo] = str(alvo)
    (pasta / "tripo.json").write_text(json.dumps({"task_id": task.task_id, "modelo": MODELO, "arquivos": arquivos},
                                                 ensure_ascii=False, indent=1), encoding="utf-8")
    print(f"[{slug}] -> {arquivos}")
    return arquivos


async def main(argv):
    from tripo3d import TripoClient
    cmd = argv[1] if len(argv) > 1 else "saldo"
    async with TripoClient(api_key=chave()) as client:
        if cmd == "saldo":
            print(await client.get_balance())
        elif cmd in ("imagem", "texto"):
            slug, fonte = argv[2], argv[3] if cmd == "imagem" else "texto:" + argv[3]
            faces = int(argv[4]) if len(argv) > 4 else FACES
            await gerar(client, slug, fonte, faces)
        elif cmd == "baixar":
            task = await client.get_task(argv[3])
            await baixar(client, argv[2], task)
        elif cmd == "lote":
            faces = int(argv[3]) if len(argv) > 3 else FACES
            pedidos = []
            for linha in Path(argv[2]).read_text(encoding="utf-8").splitlines():
                linha = linha.strip()
                if not linha or linha.startswith("#"):
                    continue
                slug, fonte = linha.split("|", 1)
                if (SAIDA / slug / f"{slug}.glb").exists():
                    print(f"[{slug}] ja' existe, pulando")
                    continue
                pedidos.append((slug.strip(), fonte.strip()))
            sem = asyncio.Semaphore(PARALELO)

            async def um(slug, fonte):
                async with sem:
                    return await gerar(client, slug, fonte, faces)
            res = await asyncio.gather(*(um(s, f) for s, f in pedidos), return_exceptions=True)
            ok = sum(1 for r in res if isinstance(r, tuple) and r[2])
            print(f"lote: {ok}/{len(pedidos)} pecas")
            for r in res:
                if isinstance(r, Exception):
                    print("erro:", r)
        else:
            sys.exit(__doc__)


if __name__ == "__main__":
    asyncio.run(main(sys.argv))
