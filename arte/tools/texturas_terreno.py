"""Camadas do terreno da ilha do Documento Mestre: fotos CC0 do Poly Haven, RECOLORIDAS na paleta das concept arts.

Uso: python arte/tools/texturas_terreno.py
Baixa (se faltar) a difusa e a normal 1K de cada camada e grava em mobile-unity/Assets/_Arkana/Resources/
terreno-<camada>-{cor,normal}.png. A cor e' recalculada: mantem o DETALHE (luminancia relativa da foto) e troca o tom
pela cor-alvo — medido em 25/09: "leafy_grass" tem media bege (151,131,89) e "rocky_terrain_02" media verde-oliva
(80,78,28); pelo nome, a grama saia areia e a rocha saia grama.
"""
import io
import json
import os
import urllib.request

import numpy as np
from PIL import Image

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
RES = os.path.join(RAIZ, "mobile-unity", "Assets", "_Arkana", "Resources")
H = {"User-Agent": "arkana-pipeline"}
# camada: (slug CC0, cor-alvo sRGB media)
CAMADAS = {
    "grama": ("leafy_grass", (92, 128, 58)),
    "mata": ("forrest_ground_01", (78, 70, 44)),
    "rocha": ("rocky_terrain_02", (112, 106, 98)),
    "areia": ("aerial_beach_01", (196, 180, 142)),
    "terra": ("brown_mud_leaves_01", (118, 92, 60)),
}


# materiais de ARQUITETURA que se repetem (1 u de UV = 2 m no kit): ficam em arte/cenario/texturas e entram
# embutidos nos GLB das pecas grandes (casa, ponte, galpao...). Assar em 1024 borraria uma ponte de 300 m.
ARQ = {
    "pedra-templo": "large_sandstone_blocks", "pedra-rustica": "rustic_stone_wall", "reboco": "clay_plaster",
    "telha": "clay_roof_tiles_02", "tabua": "brown_planks_05", "zinco": "rusty_corrugated_iron", "concreto": "chipped_concrete",
}
ARQ_DIR = os.path.join(RAIZ, "arte", "cenario", "texturas")
ARQ_ALVO = {"pedra-templo": (168, 158, 138), "pedra-rustica": (150, 142, 125), "reboco": (205, 190, 160), "tabua": (112, 78, 48)}


def arquitetura():
    for nome, slug in ARQ.items():
        for mapa, chave in (("cor", "Diffuse"), ("normal", "nor_gl")):
            destino = os.path.join(ARQ_DIR, f"arq-{nome}-{mapa}.png")
            if not os.path.exists(destino):
                baixar(slug, chave).save(destino)
        cor_path = os.path.join(ARQ_DIR, f"arq-{nome}-cor.png")
        if nome in ARQ_ALVO:   # medido: o nome engana (sandstone cinza-escuro, plaster marrom, planks cinza)
            foto = np.asarray(baixar(slug, "Diffuse"), dtype=np.float32)
            Image.fromarray(recolorir(foto, ARQ_ALVO[nome])).save(cor_path)
        im = Image.open(cor_path).convert("RGB")
        print(nome, slug, "media:", [int(v) for v in np.asarray(im, dtype=np.float32).reshape(-1, 3).mean(axis=0)])


def recolorir(foto, alvo):
    lum = foto @ np.array([0.299, 0.587, 0.114], dtype=np.float32)
    f = np.clip(lum / lum.mean(), 0.35, 1.9)[..., None]
    alvo_v = np.array(alvo, dtype=np.float32)
    tom = foto * (alvo_v / foto.reshape(-1, 3).mean(axis=0))
    return np.clip(0.75 * alvo_v * f + 0.25 * tom, 0, 255).astype(np.uint8)


def baixar(slug, chave):
    f = json.loads(urllib.request.urlopen(urllib.request.Request(f"https://api.polyhaven.com/files/{slug}", headers=H), timeout=60).read())
    url = f[chave]["1k"]["jpg"]["url"]
    return Image.open(io.BytesIO(urllib.request.urlopen(urllib.request.Request(url, headers=H), timeout=120).read())).convert("RGB")


def main():
    for nome, (slug, alvo) in CAMADAS.items():
        normal = os.path.join(RES, f"terreno-{nome}-normal.png")
        if not os.path.exists(normal):
            baixar(slug, "nor_gl").save(normal)
        foto = np.asarray(baixar(slug, "Diffuse"), dtype=np.float32)
        lum = foto @ np.array([0.299, 0.587, 0.114], dtype=np.float32)
        f = np.clip(lum / lum.mean(), 0.35, 1.9)[..., None]
        alvo_v = np.array(alvo, dtype=np.float32)
        tom = foto * (alvo_v / foto.reshape(-1, 3).mean(axis=0))      # o tom da foto, levado a' media-alvo
        cor = np.clip(0.75 * alvo_v * f + 0.25 * tom, 0, 255).astype(np.uint8)
        Image.fromarray(cor).save(os.path.join(RES, f"terreno-{nome}-cor.png"))
        print(nome, slug, "media:", [int(v) for v in cor.reshape(-1, 3).mean(axis=0)])


if __name__ == "__main__":
    import sys
    arquitetura() if "arq" in sys.argv[1:] else main()
