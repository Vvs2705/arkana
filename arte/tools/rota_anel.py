"""S16 — anel exterior oeste-norte-leste-sul (doc §5), MEDIDO no relevo: caminho de menor custo (Dijkstra) entre 8 pontos
de passagem em volta da ilha, numa grade de ~19 m, custo = 1 + 12 x declive (agua e declive > 30 graus proibidos).
Uso:  python arte/tools/rota_anel.py   -> imprime a polilinha simplificada para colar em ROTAS de ilha_mestre.py
(o gerador alisa uma faixa de ~10 m ao longo dela; rodar ilha_mestre.py de novo depois)."""
import heapq
import math
import os
import sys

import numpy as np

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from ilha_mestre import FAIXA, FUNDO, LAGO, N, RES, X0, X1, Y0, Y1  # noqa: E402

PASSO = 8                     # 2049 / 8 -> 257 celulas de 18,75 m
H = np.fromfile(os.path.join(RES, "ilha-mestre-altura.bytes"), "<u2").reshape(N, N) / 65535.0 * FAIXA + FUNDO
h = H[::PASSO, ::PASSO]
M = h.shape[0]
cx = (X1 - X0) / (N - 1) * PASSO
xs = X0 + np.arange(M) * cx
ys = Y0 + np.arange(M) * cx
gy, gx = np.gradient(h, cx)
decl = np.degrees(np.arctan(np.hypot(gx, gy)))
X, Y = np.meshgrid(xs, ys)
th = np.arctan2((Y - LAGO["cy"]) / LAGO["b"], (X - LAGO["cx"]) / LAGO["a"])
q = np.hypot((X - LAGO["cx"]) / LAGO["a"], (Y - LAGO["cy"]) / LAGO["b"]) / (1 + 0.05 * np.sin(3 * th + 1.1) + 0.035 * np.sin(5 * th + 2.6))
livre = (h > 3.0) & (decl < 22.0) & (q > 1.1)
custo = 1.0 + 40.0 * np.tan(np.radians(decl))   # estrada: foge do morro; teto 22 graus
# pontos de passagem: 8 direcoes a ~80 % do raio da ilha, cada um puxado para a celula livre de menor declive num raio de 250 m
ANG = [180, 135, 90, 45, 0, -45, -90, -135]
RAIO = (1900.0, 1750.0)


def celula(x, y):
    return int(round((y - Y0) / cx)), int(round((x - X0) / cx))


def melhor_perto(x, y, r=250.0):
    i0, j0 = celula(x, y)
    k = int(r / cx)
    best, bi, bj = 1e9, None, None
    for i in range(max(i0 - k, 0), min(i0 + k + 1, M)):
        for j in range(max(j0 - k, 0), min(j0 + k + 1, M)):
            if livre[i, j] and decl[i, j] < best:
                best, bi, bj = decl[i, j], i, j
    return bi, bj


def dijkstra(a, b):
    dist = np.full((M, M), np.inf)
    prev = {}
    dist[a] = 0
    fila = [(0.0, a)]
    while fila:
        d, u = heapq.heappop(fila)
        if u == b:
            break
        if d > dist[u]:
            continue
        for di in (-1, 0, 1):
            for dj in (-1, 0, 1):
                if di == 0 and dj == 0:
                    continue
                v = (u[0] + di, u[1] + dj)
                if not (0 <= v[0] < M and 0 <= v[1] < M) or not livre[v]:
                    continue
                nd = d + math.hypot(di, dj) * (custo[u] + custo[v]) / 2
                if nd < dist[v]:
                    dist[v] = nd
                    prev[v] = u
                    heapq.heappush(fila, (nd, v))
    if b not in prev:
        return None
    cam = [b]
    while cam[-1] != a:
        cam.append(prev[cam[-1]])
    return cam[::-1]


def simplificar(pts, tol):
    if len(pts) < 3:
        return pts
    a, b = np.array(pts[0]), np.array(pts[-1])
    ab = b - a
    n = np.hypot(*ab) or 1e-9
    dmax, k = -1, 0
    for i in range(1, len(pts) - 1):
        d = abs(ab[0] * (pts[i][1] - a[1]) - ab[1] * (pts[i][0] - a[0])) / n
        if d > dmax:
            dmax, k = d, i
    if dmax > tol:
        return simplificar(pts[:k + 1], tol)[:-1] + simplificar(pts[k:], tol)
    return [pts[0], pts[-1]]


def main():
    nos = []
    for a in ANG:
        x, y = RAIO[0] * math.cos(math.radians(a)), RAIO[1] * math.sin(math.radians(a))
        c = melhor_perto(x, y)
        if c[0] is None:
            print("sem celula livre perto de", (round(x), round(y)))
            sys.exit(1)
        nos.append(c)
    caminho, simp, total = [], [], 0.0
    for k in range(len(nos)):
        seg = dijkstra(nos[k], nos[(k + 1) % len(nos)])
        if seg is None:
            print("sem caminho entre", k, (k + 1) % len(nos))
            sys.exit(1)
        pts = [(xs[j], ys[i]) for i, j in seg]
        total += sum(math.hypot(p[0] - q_[0], p[1] - q_[1]) for p, q_ in zip(pts, pts[1:]))
        caminho += pts if not caminho else pts[1:]
        s = simplificar(pts, 25.0)          # por trecho: o laco fechado (inicio = fim) degenerava a simplificacao
        simp += s if not simp else s[1:]
    dmax = max(decl[celula(x, y)] for x, y in caminho)
    print(f"S16: {total / 1000:.1f} km, {len(caminho)} celulas -> {len(simp)} pontos, declive max {dmax:.1f} graus")
    print('    ("S16", [' + ", ".join(f"({int(round(x))}, {int(round(y))})" for x, y in simp) + "]),")


if __name__ == "__main__":
    main()
