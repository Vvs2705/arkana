"""Relevo da ilha do Documento Mestre (4.800 x 4.400 m) -> mapa de alturas do Unity.

Uso:  python arte/tools/ilha_mestre.py [pasta_de_previa]
Saida (dentro do projeto Unity):
  mobile-unity/Assets/_Arkana/Resources/ilha-mestre-altura.bytes  uint16 LE, 2049 x 2049, linha = sul->norte
  mobile-unity/Assets/_Arkana/Resources/ilha-mestre.json           dados que o Unity usa (rio, lago, regioes, ponte, nos U)
Imprime as medicoes do Documento Mestre §18 (V01, V02, V04, V05) e FALHA (exit 1) se uma delas sair da meta.

Convencao do doc: +X leste, +Y norte, +Z cima, 1 u = 1 m, mar em Z=0. No Unity: (x, z_doc -> y, y_doc -> z).
Correcoes da verificacao de 24/09 ja' aplicadas: ponte principal LESTE-OESTE (encontros (50,-1050) e (350,-1050));
o rio corre norte-sul por baixo dela, no canion.
"""
import json
import math
import os
import sys

import numpy as np
from scipy import ndimage

SEED = 24092026
N = 2049                      # 4 x 4 blocos de 513 amostras (bordas compartilhadas)
X0, X1, Y0, Y1 = -2400.0, 2400.0, -2200.0, 2200.0
FUNDO = -40.0                 # fundo do mar = 0 no mapa de alturas
FAIXA = 700.0                 # -40 .. 660 m
LAGO = dict(cx=0.0, cy=150.0, a=450.0, b=350.0, nivel=105.0)
# rio (doc §6.1): (x, y, nivel da agua, meia-largura do leito)
RIO = [(100, -220, 105.0, 9), (60, -420, 100.0, 10), (160, -650, 95.0, 12), (240, -830, 74.0, 14),
       (200, -1050, 50.0, 16), (150, -1250, 40.0, 17), (220, -1450, 30.0, 18), (320, -1750, 15.0, 22),
       (260, -2050, 1.5, 26), (270, -2300, -3.0, 30)]   # meandros; os 5 pontos do doc §6.1 ficam na polilinha
CANION = (-700.0, -1480.0)    # trecho de garganta (y)
PONTE = dict(y=-1050.0, x0=50.0, x1=350.0, z=125.0, largura=16.0)

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
RES = os.path.join(RAIZ, "mobile-unity", "Assets", "_Arkana", "Resources")

# regioes (doc §4.1): id, nome, centro, envelope, Z alvo do piso
REGIOES = [
    ("R01", "Lago Central", (0, 150), (1300, 1100), 125),
    ("R02", "Floresta Gigante", (-150, 1150), (1600, 1200), 260),
    ("R03", "Templo Antigo", (750, -250), (650, 650), 150),
    ("R04", "Vila Abandonada", (-1200, 650), (850, 850), 200),
    ("R05", "Ponte e Desfiladeiro", (200, -1050), (700, 750), 128),
    ("R06", "Acampamento", (-900, -350), (450, 450), 130),
    ("R07", "Zona Industrial", (1550, 100), (850, 850), 185),
    ("R08", "Base Militar", (1250, -1350), (800, 700), 165),
    ("R09", "Torre de Observacao", (-550, 1850), (400, 400), 430),
    ("R10", "Caverna Profunda", (1000, 1100), (600, 650), 300),
    ("R11", "Rede de Tuneis", (0, 0), (0, 0), 0),
    ("R12", "Caverna Subterranea", (-450, -1050), (1000, 800), 135),
]
# pontos de controle do relevo macro (x, y, z)
CONTROLE = [(r[2][0], r[2][1], r[4]) for r in REGIOES if r[0] != "R11"] + [
    (-100, 1300, 280), (-2000, 100, 110), (-1500, -1300, 100), (-300, -1800, 90), (700, -1800, 110),
    (2000, -700, 150), (1700, 1000, 260), (300, 1700, 420), (-250, 1800, 400), (850, 1600, 400),
    (-1500, 1400, 300), (-600, -700, 125), (500, -600, 130), (1000, 500, 170), (-700, 600, 160),
    (600, 700, 150), (-300, -350, 120), (1800, -1100, 140), (-1700, -700, 105),
]
MONTES = [  # (x, y, altura extra, raio x, raio y)
    (-1050, 1250, 330, 380, 380), (350, 1650, 260, 260, 260), (1350, 1350, 300, 330, 330),
    (1900, -300, 150, 180, 450), (-1700, 450, 130, 180, 420), (1050, 1150, 120, 250, 250),
]
# nivelamentos (x, y, z, raio pleno, raio de transicao) — pisos das regioes e acessos
PATAMARES = [
    (-900, -350, 130, 90, 220), (750, -250, 150, 140, 280), (760, -240, 166, 55, 100),
    (-1250, 720, 215, 90, 170), (-1150, 600, 195, 90, 170), (-1050, 480, 175, 90, 170),
    (1550, 100, 185, 200, 380), (1250, -1350, 150, 190, 340),
    (PONTE["x0"] - 10, PONTE["y"], 126, 45, 110), (PONTE["x1"] + 10, PONTE["y"], 126, 45, 110),
    (-550, 1850, 430, 110, 210), (-100, 1300, 280, 140, 280),
    (-450, 1050, 245, 50, 120), (150, 1000, 235, 50, 120), (-200, 1500, 300, 50, 110),
    (850, 950, 220, 50, 130), (-450, -1050, 135, 200, 360), (-600, -350, 125, 80, 180),
    (-500, -250, 116, 20, 50), (-450, -1950, 10, 25, 60),
]
# rotas de superficie (doc §5), pontos (x, y); a ponte liga S06/S15 (oeste) a S12/S14 (leste)
ROTAS = [
    ("S01", [(0, 520), (-60, 800), (-100, 1100)]),
    ("S02", [(-470, 200), (-800, 400), (-1080, 520)]),
    ("S03", [(-330, -120), (-600, -300), (-820, -340)]),
    ("S04", [(460, 120), (620, -100), (690, -200)]),
    ("S05", [(820, -200), (1150, -20), (1450, 80)]),
    ("S06", [(-60, -250), (-40, -600), (10, -950), (40, -1050)]),
    ("S07", [(-1150, 760), (-800, 1000), (-450, 1100)]),
    ("S08", [(-1100, 470), (-1000, 100), (-920, -250)]),
    ("S09", [(-250, 1450), (-450, 1650), (-550, 1780)]),
    ("S10", [(100, 1250), (500, 1100), (820, 960)]),
    ("S12", [(700, -330), (500, -700), (360, -1050)]),
    ("S13", [(1550, -150), (1450, -700), (1300, -1180)]),
    ("S14", [(360, -1050), (650, -1180), (1050, -1320)]),
    ("S15", [(-880, -450), (-500, -800), (40, -1050)]),
]
ILHOTAS = [(-120, 250, 40, 9), (150, 80, 30, 7), (60, 330, 25, 6), (-250, 60, 35, 8)]  # x, y, raio, altura
NOS_U = {"U01": (850, 950, 220), "U02": (1000, 1100, 160), "U03": (900, 700, 125), "U04": (1150, 250, 95),
         "U05": (700, -300, 125), "U06": (-1050, 450, 160), "U07": (-900, 100, 110), "U08": (-500, -250, 115),
         "U09": (-700, -500, 70), "U10": (0, -1050, 80), "U11": (-450, -700, 35), "U12": (-450, -1050, 20),
         "U13": (1050, -1250, 145), "U14": (550, -1350, 40), "U15": (-450, -1950, 10)}

rng = np.random.default_rng(SEED)
xs = np.linspace(X0, X1, N)
ys = np.linspace(Y0, Y1, N)
X, Y = np.meshgrid(xs, ys)          # linha = y (sul -> norte), coluna = x
DX, DY = xs[1] - xs[0], ys[1] - ys[0]


def smooth(a, b, t):
    t = np.clip((t - a) / (b - a), 0.0, 1.0)
    return t * t * (3 - 2 * t)


def fbm(escala_m, oitavas=5, persist=0.5):
    tot, amp, norm = np.zeros((N, N)), 1.0, 0.0
    for o in range(oitavas):
        n = int(4800.0 / (escala_m / 2 ** o)) + 4
        g = rng.standard_normal((n, n))
        c = np.linspace(1, n - 2, N)
        R, C = np.meshgrid(c, c, indexing="ij")
        tot += amp * ndimage.map_coordinates(g, [R, C], order=3, mode="nearest")
        norm += amp
        amp *= persist
    return tot / norm


def angdif(a, b):
    return np.angle(np.exp(1j * (a - b)))


def costa():
    """Distancia (m) ate' a costa: >0 em terra. Elipse 2400 x 2200 recortada, com os 4 extremos preservados."""
    th = np.arctan2(Y / 2200.0, X / 2400.0)
    rho = np.hypot(X / 2400.0, Y / 2200.0)
    ruido = sum(a * np.sin(k * th + f) for k, a, f in
                [(3, 0.018, 0.7), (5, 0.014, 2.1), (7, 0.010, 4.0), (11, 0.007, 1.3), (17, 0.004, 5.5),
                 (23, 0.004, 0.4), (31, 0.003, 3.3)])
    R = 0.905 + ruido
    for c in (0.0, math.pi / 2, math.pi, -math.pi / 2):          # cabos LARGOS nos quatro extremos (V01)
        R = R + (0.997 - R) * np.exp(-(angdif(th, c) / 0.2) ** 2)
    for c, a, w in ((math.radians(160), -0.06, 0.12), (math.radians(-145), -0.07, 0.12), (math.radians(20), -0.05, 0.1),
                    (math.radians(45), 0.03, 0.2), (math.radians(-50), 0.075, 0.22)):   # enseadas O/SO/L; saliencias NE e SE (base)
        R = R + a * np.exp(-(angdif(th, c) / w) ** 2)
    # falesias: fortes ao norte; parciais no SE, onde a base militar fica num plato sobre o mar
    penhasco = np.maximum(np.exp(-(angdif(th, math.pi / 2) / 0.8) ** 4),     # arco norte (~NE a ~NO)
                          0.8 * np.exp(-(angdif(th, math.radians(-50)) / 0.22) ** 2))
    return (R - rho) * 2300.0, penhasco


def main():
    prev = sys.argv[1] if len(sys.argv) > 1 else None
    d, penh = costa()
    # relevo macro: IDW dos pontos de controle
    num, den = np.zeros((N, N)), np.zeros((N, N))
    for (cx, cy, cz) in CONTROLE:
        w = 1.0 / (np.hypot(X - cx, Y - cy) ** 2.2 + 400.0)
        num += w * cz
        den += w
    h = num / den
    for (mx, my, ma, rx, ry) in MONTES:
        h += ma * np.exp(-(((X - mx) / rx) ** 2 + ((Y - my) / ry) ** 2))
    h += (6.0 + 0.12 * np.maximum(h - 150.0, 0)) * fbm(900.0) * 2.0
    # descida ate' o mar: rampa longa nas praias (800 m: acesso ao interior, doc §4.2) e paredao nas falesias
    # (70 m, com inclinacao ja' na base — senao sobra uma faixa plana isolada ao pe' do paredao). Areia so' na beira.
    dm = np.maximum(d, 0)
    lc = 800.0 * (1 - penh) + 70.0 * penh
    tt = np.clip(dm / lc, 0, 1)
    h = 1.5 + (h - 1.5) * (tt * tt * (3 - 2 * tt) * (1 - penh) + tt ** 0.6 * penh)
    h = np.minimum(h, (1.5 + 0.12 * dm + 0.002 * dm * dm) * (1 - penh) + 1e4 * penh)
    h = np.where(h > 560, 560 + 60 * np.tanh((h - 560) / 60), h)   # cumes <= 620
    h = ndimage.gaussian_filter(h, 1.2)
    for (px, py, pz, r0, r1) in PATAMARES:
        # a transicao cresce com o desnivel (<= ~12 graus): patamar fixo sobre encosta virava anel de paredao
        dz = abs(pz - h[int(round((py - Y0) / DY)), int(round((px - X0) / DX))])
        r1 = max(r1, r0 + dz / math.tan(math.radians(12)))
        w = 1 - smooth(r0, r1, np.hypot(X - px, Y - py))
        h = h * (1 - w) + pz * w
    # rotas: faixa de ~10 m com o relevo alisado ao longo do caminho (tira o degrau e o calombo do trajeto)
    liso = ndimage.gaussian_filter(h, 10)
    drot = np.full((N, N), 1e9)
    for _, pts in ROTAS:
        for (ax, ay), (bx, by) in zip(pts[:-1], pts[1:]):
            vx, vy = bx - ax, by - ay
            u = np.clip(((X - ax) * vx + (Y - ay) * vy) / (vx * vx + vy * vy), 0, 1)
            drot = np.minimum(drot, np.hypot(X - (ax + u * vx), Y - (ay + u * vy)))
    w = 1 - smooth(5.0, 20.0, drot)
    h = h * (1 - w) + liso * w
    # lago (bacia com borda acima da agua, ilhotas)
    th = np.arctan2((Y - LAGO["cy"]) / LAGO["b"], (X - LAGO["cx"]) / LAGO["a"])
    q = np.hypot((X - LAGO["cx"]) / LAGO["a"], (Y - LAGO["cy"]) / LAGO["b"]) / \
        (1 + 0.05 * np.sin(3 * th + 1.1) + 0.035 * np.sin(5 * th + 2.6))
    nivel = LAGO["nivel"]
    fundo = nivel - (0.6 + 24.0 * smooth(0.95, 0.35, q))
    # margem em RAMPA ate' o chao em volta (1,0 -> 1,55): antes o lago ficava num buraco com parede de ~20 m
    h = np.where(q < 1, fundo, h)
    margem = (q >= 1) & (q < 1.55)
    alvo = np.maximum(h, nivel + 0.8)
    h = np.where(margem, nivel + 0.8 + (alvo - nivel - 0.8) * smooth(1.0, 1.55, q) ** 1.2, h)
    for (ix, iy, ir, ia) in ILHOTAS:
        di = np.hypot(X - ix, Y - iy)
        h = np.where(di < ir, np.maximum(h, nivel + ia * (1 - (di / ir) ** 2) + 0.3), h)
    # rio: vale por min(); garganta no trecho do canion
    dmin = np.full((N, N), 1e9)
    wz = np.zeros((N, N))
    bw = np.zeros((N, N))
    for (ax, ay, az, aw), (bx, by, bz, bwb) in zip(RIO[:-1], RIO[1:]):
        vx, vy = bx - ax, by - ay
        u = np.clip(((X - ax) * vx + (Y - ay) * vy) / (vx * vx + vy * vy), 0, 1)
        dd = np.hypot(X - (ax + u * vx), Y - (ay + u * vy))
        m = dd < dmin
        dmin = np.where(m, dd, dmin)
        wz = np.where(m, az + u * (bz - az), wz)
        bw = np.where(m, aw + u * (bwb - aw), bw)
    garg = smooth(CANION[0] + 60, CANION[0] - 60, Y) * smooth(CANION[1] - 60, CANION[1] + 60, Y)
    t = np.maximum(dmin - bw, 0)
    aberto = wz + 0.5 + t * math.tan(math.radians(9)) + (t / 150.0) ** 2 * 20
    # parede do canion sobe ate' o chao LOCAL (antes subia ate' 132 fixo e achatava a faixa inteira em y)
    canion = wz + 0.5 + (np.maximum(h, wz + 0.5) - wz - 0.5) * smooth(0, 85, t) ** 0.7
    vale = np.where(dmin < bw, wz - 2.2 * (1 - (dmin / bw) ** 2), aberto * (1 - garg) + canion * garg)
    h = np.minimum(h, vale)
    # mar
    h = np.where(d < 0, np.minimum(h, -2 - 38 * (1 - np.exp(d / 150.0))), h)
    h = np.clip(h, FUNDO, FUNDO + FAIXA)
    # ---------------------------------------------------------------- medicoes (doc §18)
    cel = DX * DY
    seco = h > 0.5
    agua_lago = (q < 1) & (h < nivel)
    agua_rio = (dmin < bw) & (h < wz)
    gy, gx = np.gradient(h, DY, DX)
    decl = np.degrees(np.arctan(np.hypot(gx, gy)))
    acess = seco & ~agua_lago & ~agua_rio & (decl <= 25)
    lab, n = ndimage.label(acess)
    maior = np.bincount(lab.ravel())[1:].max() if n else 0
    cols = np.where(seco.any(axis=0))[0]
    rows = np.where(seco.any(axis=1))[0]
    ext_x = xs[cols[-1]] - xs[cols[0]]
    ext_y = ys[rows[-1]] - ys[rows[0]]
    borda_lago = h[(q >= 1) & (q < 1.15) & (dmin > bw + 30)]
    ri, ci = lambda y: int(round((y - Y0) / DY)), lambda x: int(round((x - X0) / DX))
    m = {
        "V01_extensao_m": [round(ext_x), round(ext_y)],
        "V02_terra_km2": round(seco.sum() * cel / 1e6, 2),
        "V02_acessivel_km2": round(acess.sum() * cel / 1e6, 2),
        "V05_maior_componente": round(maior / max(acess.sum(), 1), 3),
        "V04_borda_do_lago_min": round(float(borda_lago.min()), 2),
        "lago_km2": round(agua_lago.sum() * cel / 1e6, 3),
        "cume_max": round(float(h.max()), 1),
        "encontro_oeste_z": round(float(h[ri(PONTE["y"]), ci(PONTE["x0"])]), 1),
        "encontro_leste_z": round(float(h[ri(PONTE["y"]), ci(PONTE["x1"])]), 1),
        "rio_sob_ponte_leito": round(float(h[ri(PONTE["y"]), ci(200)]), 1),
        "plato_torre_z": round(float(h[ri(1850), ci(-550)]), 1),
        "base_arvore_z": round(float(h[ri(1300), ci(-100)]), 1),
    }
    niveis = [p[2] for p in RIO]
    m["V04_rio_so_desce"] = all(a > b for a, b in zip(niveis, niveis[1:]))
    for k, v in m.items():
        print(f"{k}: {v}")
    falhas = []
    if not (4752 <= ext_x <= 4848 and 4356 <= ext_y <= 4444):
        falhas.append("V01 extensao fora de 1%")
    if not (13 <= m["V02_terra_km2"] <= 15):
        falhas.append("V02 terra fora de 13-15 km2")
    if not (10 <= m["V02_acessivel_km2"] <= 12):
        falhas.append("V02 acessivel fora de 10-12 km2")
    if m["V05_maior_componente"] < 0.95:
        falhas.append("V05 maior componente < 95%")
    if m["V04_borda_do_lago_min"] <= nivel:
        falhas.append("V04 borda do lago vaza")
    if not m["V04_rio_so_desce"]:
        falhas.append("V04 rio sobe")
    if abs(m["encontro_oeste_z"] - 126) > 4 or abs(m["encontro_leste_z"] - 126) > 4:
        falhas.append("encontros da ponte fora de 126 +- 4 m")
    # ---------------------------------------------------------------- saida
    os.makedirs(RES, exist_ok=True)
    u16 = np.round((h - FUNDO) / FAIXA * 65535).astype("<u2")
    u16.tofile(os.path.join(RES, "ilha-mestre-altura.bytes"))
    dados = {
        "amostras": N, "x0": X0, "x1": X1, "y0": Y0, "y1": Y1, "fundo": FUNDO, "faixa": FAIXA, "semente": SEED,
        "lago": LAGO, "ilhotas": [dict(x=a, y=b, r=c, h=e) for a, b, c, e in ILHOTAS],
        "rio": [dict(x=a, y=b, z=c, meia=e) for a, b, c, e in RIO], "canion": list(CANION), "ponte": PONTE,
        "regioes": [dict(id=a, nome=b, x=c[0], y=c[1], ex=e[0], ey=e[1], z=f) for a, b, c, e, f in REGIOES],
        "nos": [dict(id=k, x=v[0], y=v[1], z=v[2]) for k, v in NOS_U.items()],
        "rotas": [dict(id=k, x=[p[0] for p in pts], y=[p[1] for p in pts]) for k, pts in ROTAS],
        "medicoes": m,
    }
    with open(os.path.join(RES, "ilha-mestre.json"), "w", encoding="utf-8") as f:
        json.dump(dados, f, ensure_ascii=False, indent=1)
    if prev:
        salvar_previa(h, decl, agua_lago | agua_rio, os.path.join(prev, "ilha-mestre-previa.png"))
    if falhas:
        print("FALHAS:", "; ".join(falhas))
        sys.exit(1)
    print("OK: relevo dentro das metas medidas")


def salvar_previa(h, decl, agua, caminho):
    from PIL import Image
    ls = np.clip(0.5 + 0.5 * (np.gradient(h, axis=1) - np.gradient(h, axis=0)) / 3.0, 0, 1)
    cor = np.zeros((N, N, 3))
    t = np.clip(h / 620.0, 0, 1)[..., None]
    cor[:] = np.array([0.36, 0.52, 0.25]) * (1 - t) + np.array([0.72, 0.68, 0.60]) * t
    cor[decl > 25] = [0.45, 0.42, 0.40]
    cor[(h > 0.5) & (h < 5)] = [0.84, 0.78, 0.58]
    cor = cor * (0.55 + 0.6 * ls[..., None])
    cor[h <= 0.5] = [0.12, 0.35, 0.60]
    cor[agua] = [0.20, 0.55, 0.80]
    img = Image.fromarray((np.clip(cor, 0, 1)[::-1] * 255).astype(np.uint8)).resize((1024, 939))
    img.save(caminho)
    print("previa:", caminho)


if __name__ == "__main__":
    main()
