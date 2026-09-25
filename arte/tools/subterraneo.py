"""Subterraneo do Documento Mestre (§6.2, §7, R10–R12): tracado, cotas, medicoes e cascas para o Blender (25/09/2026).

Uso:  python arte/tools/subterraneo.py <pasta>
Le o relevo (Resources/ilha-mestre-altura.bytes) e grava:
  Resources/ilha-mestre-subterraneo.json   saloes, tuneis (pontos x, y, piso, largura, altura a cada ~6 m), pocos, medicoes
  <pasta>/subterraneo-R10.gltf, -R11.gltf, -R12.gltf   cascas FECHADAS (o Blender funde com remesh de voxel) + escadas
  <pasta>/buracos.png   2048 x 2048, norte em cima: onde o chao do Terrain passa por dentro de uma boca (vira buraco)
Falha (exit 1) se: rocha sobre o teto < 12 m fora das bocas; declive > 30 %; tunel sob o lago; ligacao do §7.2 faltando.
Coordenadas do doc (x leste, y norte, z cima). No glTF: (x, z, -y) — o importador do Blender devolve (x, y, z).
"""
import base64
import json
import math
import os
import sys

import numpy as np
from scipy import ndimage

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from ilha_mestre import FAIXA, FUNDO, LAGO, N, NOS_U, RES, X0, X1, Y0, Y1  # noqa: E402

COBERTURA, SOB_LAGO, DECL_MAX, PASSO, BOCA_MAX = 12.0, 25.0, 0.30, 6.0, 120.0
DX, DY = (X1 - X0) / (N - 1), (Y1 - Y0) / (N - 1)
H = np.fromfile(os.path.join(RES, "ilha-mestre-altura.bytes"), "<u2").reshape(N, N) / 65535.0 * FAIXA + FUNDO
HMIN = ndimage.minimum_filter(H, size=9)          # ~21 m: o teto passa sob o ponto mais baixo em volta do tunel

# saloes e camaras: id, regiao, x, y, piso, largura (x), comprimento (y), altura, giro (graus)
SALOES = [
    ("R10_Salao_Raizes", "R10", 860, 1090, 186, 70, 60, 22, 20),
    ("R10_Grande_Galeria", "R10", 1000, 1100, 160, 120, 95, 45, 25),        # U02
    ("R10_Salao_Cristais", "R10", 905, 765, 128, 75, 60, 30, 0),            # U03 na borda sul
    ("R11_Nucleo_Leste", "R11", 1150, 250, 95, 70, 55, 22, 10),             # U04
    ("R11_Camara_Templo", "R11", 960, -40, 108, 50, 40, 15, 0),
    ("R11_Nucleo_Oeste", "R11", -900, 100, 110, 70, 55, 22, -15),           # U07
    ("R11_Galeria_Sudoeste", "R11", -700, -500, 70, 60, 45, 18, 30),        # U09
    ("R11_Camara_Mina", "R11", 1150, -620, 68, 55, 45, 16, 0),
    ("R11_Camara_Ponte_Oeste", "R11", -330, -800, 74, 55, 45, 16, 35),
    ("R11_Camara_Canion", "R11", 0, -1050, 80, 45, 35, 14, 0),              # U10
    ("R11_Galeria_Sul", "R11", 550, -1350, 40, 65, 50, 20, -20),            # U14
    ("R12_Salao_Principal", "R12", -450, -1050, 20, 320, 220, 45, 0),       # U12
    ("R12_Camara_Norte", "R12", -455, -770, 34, 110, 90, 30, 0),            # U11 na borda norte
    ("R12_Camara_Leste", "R12", -120, -1120, 24, 140, 110, 34, 15),
    ("R12_Camara_Sul", "R12", -440, -1400, 12, 130, 100, 30, -10),
]
NO_SALAO = {"U02": "R10_Grande_Galeria", "U03": "R10_Salao_Cristais", "U04": "R11_Nucleo_Leste",
            "U07": "R11_Nucleo_Oeste", "U09": "R11_Galeria_Sudoeste", "U10": "R11_Camara_Canion",
            "U11": "R12_Camara_Norte", "U12": "R12_Salao_Principal", "U14": "R11_Galeria_Sul"}
POCOS = ["U05", "U06"]        # cripta do templo e porao da vila: poco com escada em caracol ate' a superficie
# tuneis: id, regiao, de, para, pontos intermediarios, largura (inicio, fim), altura (inicio, fim), desvio lateral nas
# pontas (m, para dois acessos ao mesmo salao). "B10" = boca na parede do canion, sob a ponte.
TUNEIS = [
    ("R10_Boca_U01", "R10", "U01", "R10_Salao_Raizes", [], (35, 18), (25, 14), 0),
    ("R10_Raizes_Galeria", "R10", "R10_Salao_Raizes", "R10_Grande_Galeria", [(930, 1020), (1010, 1030)], (16, 16),
     (11, 11), 0),
    ("R10_Galeria_Cristais", "R10", "R10_Grande_Galeria", "R10_Salao_Cristais", [(1010, 900)], (14, 14), (10, 10), 0),
    ("R10_Raizes_Cristais", "R10", "R10_Salao_Raizes", "R10_Salao_Cristais", [(780, 930)], (9, 9), (7, 7), 0),
    ("R11_U03_U04", "R11", "U03", "U04", [(1060, 520)], (10, 10), (7, 7), 0),
    ("R11_U04_Templo", "R11", "U04", "R11_Camara_Templo", [], (10, 10), (7, 7), 0),
    ("R11_Templo_U05", "R11", "R11_Camara_Templo", "U05", [(820, -200)], (9, 9), (7, 7), 0),
    ("R11_U06_U07", "R11", "U06", "U07", [], (9, 9), (7, 7), 0),
    ("R11_U07_U09", "R11", "U07", "U09", [(-760, -200)], (10, 10), (7, 7), 0),
    ("R11_U07_U08", "R11", "U07", "U08", [(-700, -120)], (9, 9), (7, 7), 0),
    ("R11_U09_U11", "R11", "U09", "U11", [], (10, 10), (7, 7), 0),
    ("R11_U12_U14", "R11", "R12_Camara_Leste", "U14", [(200, -1230)], (10, 10), (7, 7), 0),
    ("R11_U14_U13", "R11", "U14", "U13", [(820, -1380)], (9, 9), (7, 7), 0),
    ("R11_U14_U10", "R11", "U14", "U10", [(330, -1270), (190, -1250), (60, -1180)], (10, 10), (7, 7), 0),
    ("R11_U12_U15", "R11", "R12_Camara_Sul", "U15", [(-470, -1700)], (10, 10), (8, 8), 0),
    ("R11_U04_Mina", "R11", "U04", "R11_Camara_Mina", [(1200, -200)], (9, 9), (6.5, 6.5), 0),
    ("R11_Mina_U14", "R11", "R11_Camara_Mina", "U14", [(930, -1060)], (9, 9), (6.5, 6.5), 0),
    ("R11_U09_Ponte", "R11", "U09", "R11_Camara_Ponte_Oeste", [], (10, 10), (7, 7), 0),
    ("R11_Ponte_U10", "R11", "R11_Camara_Ponte_Oeste", "U10", [], (10, 10), (7, 7), 0),
    ("R11_U10_Canion", "R11", "U10", "B10", [], (10, 12), (7, 9), 0),
    ("R12_Principal_Norte_A", "R12", "U12", "U11", [], (12, 12), (9, 9), -45),
    ("R12_Principal_Norte_B", "R12", "U12", "U11", [], (9, 9), (7, 7), 45),
    ("R12_Principal_Leste_A", "R12", "U12", "R12_Camara_Leste", [], (12, 12), (9, 9), -40),
    ("R12_Principal_Leste_B", "R12", "U12", "R12_Camara_Leste", [], (9, 9), (7, 7), 40),
    ("R12_Principal_Sul_A", "R12", "U12", "R12_Camara_Sul", [], (12, 12), (9, 9), -45),
    ("R12_Principal_Sul_B", "R12", "U12", "R12_Camara_Sul", [], (9, 9), (7, 7), 45),
    ("R12_Norte_Leste", "R12", "U11", "R12_Camara_Leste", [(-230, -860)], (10, 10), (8, 8), 0),
    ("R12_Leste_Sul", "R12", "R12_Camara_Leste", "R12_Camara_Sul", [(-220, -1360)], (10, 10), (8, 8), 0),
]
LIGACOES_DOC = [("U01", "U02"), ("U02", "U03"), ("U03", "U04"), ("U04", "U05"), ("U06", "U07"), ("U07", "U09"),
                ("U07", "U08"), ("U09", "U11"), ("U11", "U12"), ("U12", "U14"), ("U14", "U13"), ("U14", "U10"),
                ("U12", "U15"), ("U04", "U14"), ("U09", "U10")]


def amostra(A, x, y):
    r = ndimage.map_coordinates(A, [(np.atleast_1d(y) - Y0) / DY, (np.atleast_1d(x) - X0) / DX], order=1, mode="nearest")
    return r[0] if np.ndim(x) == 0 else r


def q_lago(x, y):
    th = np.arctan2((y - LAGO["cy"]) / LAGO["b"], (x - LAGO["cx"]) / LAGO["a"])
    return np.hypot((x - LAGO["cx"]) / LAGO["a"], (y - LAGO["cy"]) / LAGO["b"]) / \
        (1 + 0.05 * np.sin(3 * th + 1.1) + 0.035 * np.sin(5 * th + 2.6))


def teto_max(x, y):
    """Cota maxima do teto: 12 m sob o chao (minimo em volta); sob o lago, 25 m sob o leito."""
    return np.where(q_lago(x, y) < 1.05, amostra(H, x, y) - SOB_LAGO, amostra(HMIN, x, y) - COBERTURA)


def raio_elipse(s, ang):
    a = ang - math.radians(s[8])
    return 1.0 / math.hypot(math.cos(a) / (s[5] / 2), math.sin(a) / (s[6] / 2))


SAL = {s[0]: s for s in SALOES}


def ponta(ref, alvo, desvio):
    """(x, y, piso, tipo) da ponta do tunel. Salao: na borda, 8 m para dentro, na direcao do proximo ponto."""
    ref = NO_SALAO.get(ref, ref)
    if ref in SAL:
        s = SAL[ref]
        ang = math.atan2(alvo[1] - s[3], alvo[0] - s[2])
        r = raio_elipse(s, ang) - 8.0
        px, py = -math.sin(ang), math.cos(ang)
        return (s[2] + r * math.cos(ang) + desvio * px, s[3] + r * math.sin(ang) + desvio * py, s[4], "salao")
    if ref == "B10":
        return (150.0, -1050.0, 80.0, "boca")
    x, y, z = NOS_U[ref]
    return (x, y, z, "poco" if ref in POCOS else "boca")


def centro(ref):
    ref = NO_SALAO.get(ref, ref)
    if ref in SAL:
        return SAL[ref][2], SAL[ref][3]
    return (150.0, -1050.0) if ref == "B10" else NOS_U[ref][:2]


def tracar(t, semente):
    tid, reg, de, para, meio, larg, alt, desvio = t
    a = ponta(de, meio[0] if meio else centro(para), desvio)
    b = ponta(para, meio[-1] if meio else centro(de), -desvio)
    P = np.array([a[:2]] + [p[:2] for p in meio] + [b[:2]], float)
    seg = np.hypot(*np.diff(P, axis=0).T)
    s0 = np.concatenate([[0], np.cumsum(seg)])
    s = np.linspace(0, s0[-1], max(int(s0[-1] / PASSO), 3) + 1)
    x, y = np.interp(s, s0, P[:, 0]), np.interp(s, s0, P[:, 1])
    if len(P) > 2:          # cantos dos pontos intermediarios arredondados, pontas fixas
        g = ndimage.gaussian_filter1d
        x, y = g(x, 5, mode="nearest"), g(y, 5, mode="nearest")
        u = s / s[-1]
        x += (a[0] - x[0]) * (1 - u) + (b[0] - x[-1]) * u
        y += (a[1] - y[0]) * (1 - u) + (b[1] - y[-1]) * u
    # meandro lateral suave (zero nas pontas): corredor que segue a rocha, nao regua
    L = s[-1]
    tx, ty = np.gradient(x), np.gradient(y)
    n = np.hypot(tx, ty)
    nx, ny = -ty / n, tx / n
    rng = np.random.default_rng(semente)
    amp = min(14.0, L / 25.0)
    off = amp * np.sin(math.pi * s / L) * np.sin(2 * math.pi * s / rng.uniform(160, 260) + rng.uniform(0, 6.3))
    x, y = x + nx * off, y + ny * off
    # reamostra por comprimento de arco: o alisamento junta pontos nas curvas e o passo deixaria de ser uniforme
    sa = np.concatenate([[0], np.cumsum(np.hypot(np.diff(x), np.diff(y)))])
    s = np.linspace(0, sa[-1], max(int(sa[-1] / PASSO), 3) + 1)
    x, y = np.interp(s, sa, x), np.interp(s, sa, y)
    L = s[-1]
    # largura/altura: rampa entre inicio e fim + variacao (nunca abaixo do minimo do doc: 8 x 6)
    u = s / L
    w = larg[0] + (larg[1] - larg[0]) * np.clip(u / 0.4, 0, 1)
    h = alt[0] + (alt[1] - alt[0]) * np.clip(u / 0.4, 0, 1)
    var = 1 + 0.14 * np.sin(2 * math.pi * s / 97 + rng.uniform(0, 6.3)) + 0.07 * np.sin(2 * math.pi * s / 41)
    w, h = np.maximum(w * var, 8.0), np.maximum(h * (0.5 + 0.5 * var), 6.0)
    # piso: reta entre as pontas, rebaixada onde falta rocha (menos nos BOCA_MAX m junto de uma boca/poco, que e' a
    # entrada prevista do doc); depois a envoltoria inferior com declive <= 30 % (erosao 1D: ida e volta bastam).
    # Se uma ponta teve de descer, o tunel e' curto demais para o desnivel: a montagem falha e o tracado muda.
    ds = np.hypot(np.diff(x), np.diff(y))          # distancias reais (o meandro e as curvas mudam o passo)
    lim = teto_max(x, y) - h
    if a[3] != "salao":
        lim[s <= BOCA_MAX] = 1e9
    if b[3] != "salao":
        lim[s >= L - BOCA_MAX] = 1e9
    f = np.minimum(a[2] + (b[2] - a[2]) * u, lim)
    f[0], f[-1] = a[2], b[2]
    for i in range(1, len(f)):
        f[i] = min(f[i], f[i - 1] + DECL_MAX * ds[i - 1])
    for i in range(len(f) - 2, -1, -1):
        f[i] = min(f[i], f[i + 1] + DECL_MAX * ds[i])
    curto = f[0] < a[2] - 0.05 or f[-1] < b[2] - 0.05
    pts = np.stack([x, y, f, w, h], 1)
    # boca abaixo do chao (U13, U08): o tunel continua subindo ate' sair na superficie
    ext = []
    for fim, tipo in ((0, a[3]), (-1, b[3])):
        if tipo != "boca":
            ext.append(None)
            continue
        p, q = pts[fim], pts[1 if fim == 0 else -2]
        d = (p[:2] - q[:2]) / np.hypot(*(p[:2] - q[:2]))
        novos, c = [], p.copy()
        while amostra(H, c[0], c[1]) > c[2] + 0.3 and len(novos) < 12:
            c = c.copy()
            c[:2] += d * PASSO
            c[2] = float(np.clip(amostra(H, c[0], c[1]), c[2] - DECL_MAX * PASSO, c[2] + DECL_MAX * PASSO))
            novos.append(c)
        ext.append(novos)
    if ext[0]:
        pts = np.vstack([np.array(ext[0][::-1]), pts])
    if ext[1]:
        pts = np.vstack([pts, np.array(ext[1])])
    return pts, a, b, curto


def medir(tid, pts, a, b):
    x, y, f, w, h = pts.T
    ds = np.hypot(np.diff(x), np.diff(y))
    decl = np.abs(np.diff(f)) / np.maximum(ds, 1e-6)
    cob = np.where(q_lago(x, y) < 1.05, amostra(H, x, y) - SOB_LAGO + COBERTURA, amostra(HMIN, x, y)) - (f + h)
    # bocas: os BOCA_MAX m junto de uma ponta aberta (entrada prevista); boca_m = quanto disso ainda tem < 12 m de rocha
    s = np.concatenate([[0], np.cumsum(ds)])
    entrada = np.zeros(len(x), bool)
    if a[3] != "salao":
        entrada |= s <= BOCA_MAX
    if b[3] != "salao":
        entrada |= s >= s[-1] - BOCA_MAX
    lago = bool((q_lago(x, y) < 1.05).any())
    return dict(id=tid, comprimento_m=round(float(np.sum(np.hypot(ds, np.diff(f))))), dz=round(float(f[-1] - f[0]), 1),
                declive_max=round(float(decl.max()), 3), rocha_min_fora_das_bocas=round(float(cob[~entrada].min()), 1)
                if (~entrada).any() else None, boca_m=round(float((entrada & (cob < COBERTURA)).sum() * PASSO)),
                sob_lago=lago, largura_min=round(float(w.min()), 1), altura_min=round(float(h.min()), 1))


# ------------------------------------------------------------------------------------------------ malhas
def casca_tunel(pts):
    """Casca fechada: chao plano no piso, paredes, abobada; tampas nas pontas."""
    x, y, f, w, h = pts.T
    tx, ty = np.gradient(x), np.gradient(y)
    n = np.hypot(tx, ty)
    nx, ny = -ty / n, tx / n
    perfil = [(-0.5, 0.0), (0.5, 0.0)] + [(0.5 * math.cos(t), 0.45 + 0.55 * math.sin(t))
                                           for t in np.linspace(0, math.pi, 11)] + [(-0.5, 0.0)]
    perfil = perfil[:-1]
    K = len(perfil)
    V = []
    for i in range(len(x)):
        for (pu, pv) in perfil:
            V.append((x[i] + nx[i] * pu * w[i], y[i] + ny[i] * pu * w[i], f[i] + pv * h[i]))
    F = []
    for i in range(len(x) - 1):
        for k in range(K):
            a0, a1, b0, b1 = i * K + k, i * K + (k + 1) % K, (i + 1) * K + k, (i + 1) * K + (k + 1) % K
            F += [(a0, b0, b1), (a0, b1, a1)]
    for i, inv in ((0, True), (len(x) - 1, False)):
        c = len(V)
        V.append(tuple(np.mean([V[i * K + k] for k in range(K)], 0)))
        for k in range(K):
            t = (c, i * K + k, i * K + (k + 1) % K)
            F.append(t[::-1] if inv else t)
    return V, F


def casca_salao(s, semente):
    rng = np.random.default_rng(semente)
    fases = rng.uniform(0, 6.3, 4)
    M, niveis = 64, 12
    g = math.radians(s[8])
    V, F = [], []
    for j in range(niveis + 1):
        v = j / niveis
        fator = (1 - v ** 2.4) ** (1 / 2.4) if j < niveis else 0.0
        for i in range(M):
            t = 2 * math.pi * i / M
            r = 1 + 0.10 * math.sin(3 * t + fases[0]) + 0.06 * math.sin(5 * t + fases[1]) + \
                0.05 * math.sin(2 * t + 3 * v + fases[2])
            lx, ly = r * fator * s[5] / 2 * math.cos(t), r * fator * s[6] / 2 * math.sin(t)
            V.append((s[2] + lx * math.cos(g) - ly * math.sin(g), s[3] + lx * math.sin(g) + ly * math.cos(g),
                      s[4] + v * s[7]))
    for j in range(niveis):
        for i in range(M):
            a0, a1, b0, b1 = j * M + i, j * M + (i + 1) % M, (j + 1) * M + i, (j + 1) * M + (i + 1) % M
            F += [(a0, a1, b1), (a0, b1, b0)]
    c = len(V)
    V.append((s[2], s[3], s[4]))
    F += [(c, (i + 1) % M, i) for i in range(M)]                 # chao plano (o topo fecha no ultimo anel)
    return V, F


def casca_poco(x, y, z0, z1, r, seg=32):
    V = [(x + r * math.cos(2 * math.pi * i / seg), y + r * math.sin(2 * math.pi * i / seg), z)
         for z in (z0, z1) for i in range(seg)] + [(x, y, z0), (x, y, z1)]
    F = []
    for i in range(seg):
        j = (i + 1) % seg
        F += [(i, j, seg + j), (i, seg + j, seg + i), (2 * seg, j, i), (2 * seg + 1, seg + i, seg + j)]
    return V, F


def escada_caracol(x, y, z0, z1, r0=1.8, r1=6.4, subida_volta=7.5, esp=0.5):
    """Rampa helicoidal (andavel, ~30 %) + coluna central. Nao entra na fusao: e' piso, nao vazio."""
    voltas = (z1 - z0) / subida_volta
    n = max(int(voltas * 36), 2)
    V, F = [], []
    for i in range(n + 1):
        t = 2 * math.pi * voltas * i / n
        z = z0 + (z1 - z0) * i / n
        for (r, dz) in ((r0, 0), (r1, 0), (r1, -esp), (r0, -esp)):
            V.append((x + r * math.cos(t), y + r * math.sin(t), z + dz))
    for i in range(n):
        for k in range(4):
            a0, a1, b0, b1 = i * 4 + k, i * 4 + (k + 1) % 4, (i + 1) * 4 + k, (i + 1) * 4 + (k + 1) % 4
            F += [(a0, b0, b1), (a0, b1, a1)]
    cv, cf = casca_poco(x, y, z0, z1 + 3, r0 + 0.05, 16)
    o = len(V)
    return V + cv, F + [(a + o, b + o, c + o) for a, b, c in cf]


def gravar_gltf(caminho, cena, objetos):
    """objetos: [(nome, V, F, extras)] com V em coordenadas do doc -> glTF (x, z, -y)."""
    buf, acess, vistas, malhas, nos = bytearray(), [], [], [], []
    for nome, V, F, extras in objetos:
        v = np.asarray(V, np.float32)[:, [0, 2, 1]] * np.array([1, 1, -1], np.float32)
        ix = np.asarray(F, np.uint32).ravel()
        for dados, alvo in ((v.tobytes(), 34962), (ix.tobytes(), 34963)):
            while len(buf) % 4:
                buf.append(0)
            vistas.append(dict(buffer=0, byteOffset=len(buf), byteLength=len(dados), target=alvo))
            buf += dados
        acess.append(dict(bufferView=len(vistas) - 2, componentType=5126, count=len(v), type="VEC3",
                          min=v.min(0).tolist(), max=v.max(0).tolist()))
        acess.append(dict(bufferView=len(vistas) - 1, componentType=5125, count=len(ix), type="SCALAR"))
        malhas.append(dict(name=nome, primitives=[dict(attributes=dict(POSITION=len(acess) - 2), indices=len(acess) - 1)]))
        nos.append(dict(name=nome, mesh=len(malhas) - 1, extras=extras))
    doc = dict(asset=dict(version="2.0", generator="arkana-subterraneo"), scene=0,
               scenes=[dict(name=cena, nodes=list(range(len(nos))))], nodes=nos, meshes=malhas, accessors=acess,
               bufferViews=vistas, buffers=[dict(byteLength=len(buf), uri="data:application/octet-stream;base64," +
                                                 base64.b64encode(bytes(buf)).decode())])
    with open(caminho, "w", encoding="utf-8") as fh:
        json.dump(doc, fh)


def main(pasta):
    tuneis, medidas, falhas = [], [], []
    buracos = np.zeros((N - 1, N - 1), bool)          # celulas do Terrain (2048 x 2048), linha = sul -> norte
    cx = X0 + (np.arange(N - 1) + 0.5) * DX
    cy = Y0 + (np.arange(N - 1) + 0.5) * DY
    hc = ndimage.uniform_filter(H, 2)[:-1, :-1]         # chao no centro de cada celula (aprox.)
    for k, t in enumerate(TUNEIS):
        pts, a, b, curto = tracar(t, 1000 + k)
        m = medir(t[0], pts, a, b)
        medidas.append(m)
        tuneis.append(dict(id=t[0], regiao=t[1], de=t[2], para=t[3], pontos=np.round(pts, 2).tolist()))
        if curto:
            falhas.append(f"{t[0]}: curto demais para o desnivel (ponta teve de descer)")
        if m["declive_max"] > DECL_MAX + 0.01:
            falhas.append(f"{t[0]}: declive {m['declive_max']}")
        if m["rocha_min_fora_das_bocas"] is not None and m["rocha_min_fora_das_bocas"] < COBERTURA - 0.5:
            falhas.append(f"{t[0]}: rocha {m['rocha_min_fora_das_bocas']} m")
        if m["sob_lago"]:
            falhas.append(f"{t[0]}: passa sob o lago")
        # buraco no chao onde a superficie corta o vao do tunel (so' acontece nas bocas)
        for (x, y, f, w, h) in pts:
            c0, c1 = np.searchsorted(cx, [x - w, x + w])
            r0, r1 = np.searchsorted(cy, [y - w, y + w])
            gx, gy = np.meshgrid(cx[c0:c1], cy[r0:r1])
            dentro = (np.hypot(gx - x, gy - y) < w / 2 - 0.5) & (hc[r0:r1, c0:c1] > f - 0.5) & \
                     (hc[r0:r1, c0:c1] < f + h)
            buracos[r0:r1, c0:c1] |= dentro
    pocos = []
    for u in POCOS:
        x, y, z = NOS_U[u]
        topo = float(amostra(H, x, y))
        pocos.append(dict(id=u, x=x, y=y, piso=z, topo=round(topo, 1), raio=7.0))
        gx, gy = np.meshgrid(cx, cy, sparse=True)
        buracos |= np.hypot(gx - x, gy - y) < 6.5
    saloes = []
    for s in SALOES:
        ang = np.linspace(0, 2 * math.pi, 48, endpoint=False)
        g = math.radians(s[8])
        rr = np.linspace(0, 1, 6)[:, None]
        lx, ly = rr * s[5] / 2 * np.cos(ang), rr * s[6] / 2 * np.sin(ang)
        px, py = s[2] + lx * math.cos(g) - ly * math.sin(g), s[3] + lx * math.sin(g) + ly * math.cos(g)
        rocha = float((teto_max(px, py) + COBERTURA - (s[4] + s[7])).min())
        saloes.append(dict(id=s[0], regiao=s[1], x=s[2], y=s[3], piso=s[4], largura=s[5], comprimento=s[6],
                           altura=s[7], giro=s[8], rocha_min=round(rocha, 1)))
        if rocha < COBERTURA - 0.5:
            falhas.append(f"{s[0]}: rocha {rocha:.1f} m sobre o teto")
    # ligacoes do §7.2: caminho direto entre os dois nos (so' camaras intermediarias sem U no meio)
    viz = {}
    for t in TUNEIS:
        a, b = NO_SALAO.get(t[2], t[2]), NO_SALAO.get(t[3], t[3])
        viz.setdefault(a, set()).add(b)
        viz.setdefault(b, set()).add(a)
    nos_u = {NO_SALAO.get(u, u) for u in NOS_U}
    for (p, q) in LIGACOES_DOC:
        a, b = NO_SALAO.get(p, p), NO_SALAO.get(q, q)
        vistos, fila, ok = {a}, [a], a == b
        while fila and not ok:
            c = fila.pop()
            for d in viz.get(c, ()):
                if d == b:
                    ok = True
                elif d not in vistos and d not in nos_u:
                    vistos.add(d)
                    fila.append(d)
        if not ok:
            falhas.append(f"ligacao {p}-{q} faltando")
    for m in medidas:
        print(f"{m['id']:<24} {m['comprimento_m']:>5} m  dz {m['dz']:>6}  decl {m['declive_max']:.2f}  "
              f"rocha {m['rocha_min_fora_das_bocas']}  boca {m['boca_m']} m  {m['largura_min']}x{m['altura_min']}")
    for s in saloes:
        print(f"{s['id']:<24} piso {s['piso']:>4}  {s['largura']}x{s['comprimento']}x{s['altura']}  rocha {s['rocha_min']}")
    total = sum(m["comprimento_m"] for m in medidas)
    print(f"tuneis: {len(TUNEIS)}, {total / 1000:.1f} km; saloes/camaras: {len(SALOES)}; pocos: {len(pocos)}; "
          f"celulas de buraco: {int(buracos.sum())}")
    with open(os.path.join(RES, "ilha-mestre-subterraneo.json"), "w", encoding="utf-8") as fh:
        json.dump(dict(saloes=saloes, tuneis=tuneis, pocos=pocos, medicoes=medidas, falhas=falhas), fh)
    if pasta:
        from PIL import Image
        for reg in ("R10", "R11", "R12"):
            objs = []
            for k, t in enumerate(tuneis):
                if t["regiao"] == reg:
                    V, F = casca_tunel(np.array(t["pontos"]))
                    objs.append((t["id"], V, F, dict(tipo="vazio", regiao=reg)))
            for k, s in enumerate(SALOES):
                if s[1] == reg:
                    V, F = casca_salao(s, 77 + k)
                    objs.append((s[0], V, F, dict(tipo="vazio", regiao=reg)))
            if reg == "R11":
                for p in pocos:
                    V, F = casca_poco(p["x"], p["y"], p["piso"], p["topo"] + 5, p["raio"])
                    objs.append((f"R11_Poco_{p['id']}", V, F, dict(tipo="vazio", regiao=reg)))
                    V, F = escada_caracol(p["x"], p["y"], p["piso"], p["topo"])
                    objs.append((f"R11_Escada_{p['id']}", V, F, dict(tipo="piso", regiao=reg)))
            gravar_gltf(os.path.join(pasta, f"subterraneo-{reg}.gltf"), f"ARKANA_{reg}", objs)
        Image.fromarray((buracos[::-1] * 255).astype(np.uint8)).save(os.path.join(pasta, "buracos.png"))
        print("cascas e buracos em", pasta)
    if falhas:
        print("FALHAS:", "; ".join(falhas))
        sys.exit(1)
    print("OK: subterraneo dentro das regras medidas")


if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else None)
