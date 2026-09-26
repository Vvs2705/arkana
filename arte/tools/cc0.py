"""Pacotes CC0 (dominio publico) para preencher a ilha rapido — Kenney e Quaternius (25/09/2026).
Baixa e descompacta em arte/cenario/cc0/<pacote>/ (fora do git: grandes e reproduziveis por este script).
Uso:  python arte/tools/cc0.py            # todos
      python arte/tools/cc0.py nature-kit # um so'
Uso no jogo: escolher pecas por nome e copiar o GLB/FBX para mobile-unity/Assets/_Arkana/Resources como as do kit;
credito em design/gdd/CREDITS.md (CC0 nao exige, mas registramos a origem)."""
import io
import os
import sys
import urllib.request
import zipfile

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DESTINO = os.path.join(RAIZ, "arte", "cenario", "cc0")
PACOTES = {
    # Kenney (kenney.nl, CC0): arvores, pedras, folhagem low-poly (330 modelos) e kit de sobrevivencia (barracas, caixas, fogueira, 80)
    "nature-kit": "https://kenney.nl/media/pages/assets/nature-kit/37ac38a37b-1677698939/kenney_nature-kit.zip",
    "survival-kit": "https://kenney.nl/media/pages/assets/survival-kit/4065a8185b-1712149243/kenney_survival-kit.zip",
}


def baixar(nome, url):
    pasta = os.path.join(DESTINO, nome)
    if os.path.isdir(pasta) and os.listdir(pasta):
        print(f"{nome}: ja' existe")
        return
    os.makedirs(pasta, exist_ok=True)
    print(f"{nome}: baixando {url}")
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0 (ARKANA cc0.py)"})
    dados = urllib.request.urlopen(req, timeout=300).read()
    zipfile.ZipFile(io.BytesIO(dados)).extractall(pasta)
    n = sum(len(f) for _, _, f in os.walk(pasta))
    print(f"{nome}: {len(dados) / 1e6:.1f} MB, {n} arquivos em {pasta}")


if __name__ == "__main__":
    for nome, url in PACOTES.items():
        if len(sys.argv) < 2 or nome in sys.argv[1:]:
            baixar(nome, url)
