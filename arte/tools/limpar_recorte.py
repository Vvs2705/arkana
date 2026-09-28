#!/usr/bin/env python3
"""Limpa a BORDA COLORIDA dos recortes do Documento Mestre antes do Tripo (27/09/2026).

O recorte das concept arts deixou pixels vermelhos/amarelos/magenta vivos na faixa de 3 px junto do fundo
transparente (ex.: 102, entre as raizes). A IA 3D copia isso para a textura. Aqui, NA FAIXA DA BORDA, pixel de cor
"neon" (nao existe em casca, folha ou pedra) e pixel quase transparente somem. O original fica intacto: a saida e'
<nome>-limpo.png ao lado. Uso: python arte/tools/limpar_recorte.py 102-vegetacao-arvore-gigante.png [...]
"""
import sys
from pathlib import Path

import numpy as np
from PIL import Image
from scipy import ndimage

PASTA = Path(__file__).resolve().parents[1] / "cenario" / "documento-mestre"
FAIXA = 3        # px a partir do fundo transparente
ALFA_MIN = 40    # abaixo disso, na faixa, vira fundo


def neon(rgb):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    return (((r > 170) & (g < 90) & (b < 90)) | ((r > 150) & (b > 150) & (g < 90)) |
            ((r > 200) & (g > 200) & (b < 80)) | ((g > 180) & (r < 90) & (b < 90)))


def limpar(nome):
    im = np.array(Image.open(PASTA / nome).convert("RGBA")).astype(int)
    a = im[..., 3]
    faixa = (a > 0) & ndimage.binary_dilation(a == 0, iterations=FAIXA)
    fora = faixa & (neon(im[..., :3]) | (a < ALFA_MIN))
    im[fora, 3] = 0
    saida = PASTA / nome.replace(".png", "-limpo.png")
    Image.fromarray(im.astype(np.uint8), "RGBA").save(saida)
    print(f"OK {nome}: {int(fora.sum())} px da borda removidos -> {saida.name}")
    return int(neon(np.array(Image.open(saida))[..., :3])[faixa & (np.array(Image.open(saida))[..., 3] > 0)].sum())


if __name__ == "__main__":
    for n in sys.argv[1:]:
        assert limpar(n) == 0, f"{n}: ainda sobrou neon na borda"
