# -*- coding: utf-8 -*-
"""pc-unreal/scripts/semear_kit.py — decide ONDE cada peca do kit vai parar.

RODA EM PYTHON PURO, fora do Unreal. Por que fora: a decisao de onde plantar
depende de ALTURA e INCLINACAO do terreno, e essas duas coisas estao no proprio
heightmap. Ler o `.r16` aqui e' exato, instantaneo e testavel — enquanto pedir
altura ao Unreal exigiria carregar as 64 regioes do World Partition e fazer mil
line traces, que e' lento e ainda depende do editor estar num estado certo.

Saida: `kit-plantado.json`, uma lista de transformacoes por peca. O
`plantar_kit.py` la' dentro do Unreal so' obedece — ele nao decide nada.

AS TRES REGRAS QUE VALEM (herdadas de `Island.gd` e do MAPA-GRANDE-PLANO §4.3):

1. **NADA E' COLOCADO A MAO.** Cada peca e' sorteada por regra dentro da sua
   regiao. Mapa de battle royale se veste com instanciamento, nao com carinho
   peca por peca — 2.400 m nao cabem em trabalho manual.

2. **A ILHA E' SEMPRE A MESMA.** `random.Random(SEMENTE)` com semente fixa. O
   jogador tem que poder aprender o mapa; ilha que muda a cada build nao se
   aprende. (A ZONA e' que muda por partida — isso e' outro sistema.)

3. **ESCALA VEM DE ALTURA-ALVO, NUNCA DE NUMERO REDONDO.** O Meshy exporta peca
   com ~2 m. Escala 100 daria arco de 191 m. Aqui cada peca declara quantos
   METROS ela deve ter no mundo, e a escala sai disso dividido pela caixa medida
   em `kit-medido.json`.

USO:
  python semear_kit.py
"""
import io
import json
import math
import os
import random
import struct

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.abspath(os.path.join(AQUI, "..", ".."))
HEIGHTMAP = os.path.join(RAIZ, "pc-unreal", "heightmap", "ilha-fraturada-2017.r16")
MEDIDO = os.path.join(AQUI, "kit-medido.json")
SAIDA = os.path.join(AQUI, "kit-plantado.json")

SEMENTE = 7          # a mesma do ruido da ilha, por simetria de leitura
LADO = 2017
MAPA_M = 2400.0
MEIA_FAIXA_GODOT_M = 30.0788
ESCALA_VS_GODOT = 4.0        # 2400 m / 600 m

PASSO_CM = MAPA_M * 100.0 / (LADO - 1)

# ONDE A ILHA REALMENTE ESTA, medido pelos limites dos proxies em 28/08:
# X e Y vao de -240.000 a 0 cm. Ou seja, o Landscape cresce no sentido NEGATIVO
# a partir da sua posicao (-120.000, -120.000) — o centro da ilha NAO e' a
# origem do mundo, e' (-1.200, -1.200) m.
#
# Isto foi MEDIDO, nao suposto, e a conferencia foi o PICO: `Island.gd` poe o
# pico em (12, 84) do espaco-base, o que dá (-110.400, -52.800) cm neste
# referencial; o proxy mais alto do terreno esta' centrado em (-105.000,
# -45.000) cm, dentro do mesmo pedaco de 300 m. Bate.
CENTRO_ILHA_M = (-1200.0, -1200.0)
CANTO_CM = (CENTRO_ILHA_M[0] - MAPA_M * 0.5) * 100.0
FAIXA_M = 2.0 * MEIA_FAIXA_GODOT_M * ESCALA_VS_GODOT   # 240,63 m de amplitude

# --- OS POIs -----------------------------------------------------------------
# Vem de `Island.pois()` em espaco-BASE (ilha de 300 m) e sao multiplicados por
# 8 (= 2400/300). Godot usa (x, z) com Y para cima; o Unreal usa (X, Y) com Z
# para cima, e o heightmap foi gravado com x na coluna e z na linha — entao
# x_godot -> X_unreal e z_godot -> Y_unreal, sem espelhar.
K = MAPA_M / 300.0
CX, CY = CENTRO_ILHA_M


def _poi(bx, bz, br):
    return {"c": (CX + bx * K, CY + bz * K), "r": br * K}


POIS = {
    "alagado":  _poi(-70,  60, 22),
    "floresta": _poi(-60, -66, 40),
    "lago":     _poi( 75,  30, 24),
    "ruinas":   _poi( 63, -73, 20),
    "pico":     _poi( 12,  84, 40),
    "dunas":    _poi( 10, -98, 32),
}

# --- O KIT, e o que cada peca quer -------------------------------------------
# altura_m: quanto a peca deve MEDIR no mundo. E' o unico numero de escala.
# quantos: instancias. onde: 'ilha' | nome de POI | lista de POIs.
# cota: (min, max) em metros acima do mar. inclin: (min, max) de inclinacao
#       (0 = deitado, 1 = 45 graus). afasta_m: raio minimo entre instancias.
KIT = [
    {"slug": "17-rocha-basalto-modular",      "altura_m": 9.0,  "quantos": 300,
     "onde": "ilha",     "cota": (3.0, 130.0), "inclin": (0.12, 0.90), "afasta_m": 26.0,
     "escala_var": (0.7, 1.7)},
    {"slug": "18-rocha-vulcanica-cobertura",  "altura_m": 6.5,  "quantos": 200,
     "onde": "ilha",     "cota": (2.0, 130.0), "inclin": (0.05, 0.60), "afasta_m": 30.0,
     "escala_var": (0.8, 1.6)},
    {"slug": "19-arco-calcario-nymara",       "altura_m": 24.0, "quantos": 7,
     "onde": "lago",     "cota": (4.0, 40.0),  "inclin": (0.0, 0.22),  "afasta_m": 70.0,
     "escala_var": (0.9, 1.25)},
    # DEITADA: mede-se pelo COMPRIMENTO, nao pela altura. A caixa Z dela tem
    # 39,6 cm; pedir 20 m de ALTURA daria escala 50 e uma ponte de 95 m de vao.
    {"slug": "20-ponte-raiz-aeris",           "altura_m": 30.0, "eixo": 0, "quantos": 6,
     "onde": "floresta", "cota": (5.0, 60.0),  "inclin": (0.0, 0.25),  "afasta_m": 80.0,
     "escala_var": (0.9, 1.3)},
    {"slug": "21-pilar-condutor-fulgar",      "altura_m": 26.0, "quantos": 10,
     "onde": ["ruinas", "pico"], "cota": (5.0, 130.0), "inclin": (0.0, 0.30),
     "afasta_m": 55.0, "escala_var": (0.85, 1.3)},
    {"slug": "25-casa-vigia",                 "altura_m": 15.0, "quantos": 6,
     "onde": ["ruinas", "floresta", "lago", "alagado", "dunas", "pico"],
     "cota": (5.0, 90.0), "inclin": (0.0, 0.14), "afasta_m": 120.0,
     "escala_var": (1.0, 1.0)},
    {"slug": "28-pedestal-de-arma",           "altura_m": 4.5,  "quantos": 12,
     "onde": ["ruinas", "floresta", "lago", "alagado", "dunas", "pico"],
     "cota": (4.0, 120.0), "inclin": (0.0, 0.16), "afasta_m": 60.0,
     "escala_var": (1.0, 1.0)},
    {"slug": "29-arvore-folhas-douradas",     "altura_m": 18.0, "quantos": 150,
     "onde": "floresta", "cota": (4.0, 70.0),  "inclin": (0.0, 0.45),  "afasta_m": 22.0,
     "escala_var": (0.75, 1.4)},
    {"slug": "30-arvore-carbonizada-renascendo", "altura_m": 15.0, "quantos": 110,
     "onde": "ilha",     "cota": (4.0, 90.0),  "inclin": (0.0, 0.40),  "afasta_m": 34.0,
     "escala_var": (0.75, 1.35)},
    {"slug": "31-juncos-nymara",              "altura_m": 3.0,  "quantos": 450,
     "onde": ["alagado", "lago"], "cota": (0.6, 8.0), "inclin": (0.0, 0.20),
     "afasta_m": 9.0, "escala_var": (0.7, 1.5)},
    {"slug": "32-piso-runa-reativa",          "altura_m": 1.2,  "quantos": 26,
     "onde": "ruinas",   "cota": (5.0, 60.0),  "inclin": (0.0, 0.10),  "afasta_m": 18.0,
     "escala_var": (0.9, 1.4)},
]


def carregar_alturas():
    """O .r16 inteiro em memoria, ja' convertido para METROS acima do mar."""
    dados = io.open(HEIGHTMAP, "rb").read()
    esperado = LADO * LADO * 2
    if len(dados) != esperado:
        raise SystemExit("heightmap com %d bytes, esperava %d" % (len(dados), esperado))
    crus = struct.unpack("<%dH" % (LADO * LADO), dados)
    return [(v / 65535.0 - 0.5) * FAIXA_M for v in crus]


class Terreno(object):
    def __init__(self, alturas):
        self.h = alturas

    def _idx(self, i, j):
        i = 0 if i < 0 else (LADO - 1 if i > LADO - 1 else i)
        j = 0 if j < 0 else (LADO - 1 if j > LADO - 1 else j)
        return j * LADO + i

    def em_metros(self, xm, ym):
        """Altura no ponto (metros de mundo), pelo vizinho mais proximo.

        Vizinho mais proximo basta: o passo da grade e' 1,19 m e as pecas medem
        de 3 a 26 m. Interpolar aqui seria precisao que ninguem enxerga.
        """
        i = int(round((xm * 100.0 - CANTO_CM) / PASSO_CM))
        j = int(round((ym * 100.0 - CANTO_CM) / PASSO_CM))
        return self.h[self._idx(i, j)]

    def inclinacao(self, xm, ym):
        """Inclinacao adimensional (subida/andado) numa janela de ~5 m.

        JANELA LARGA, e nao vizinho imediato: a licao esta' em `Island.gd`, onde
        medir inclinacao vertice a vertice fez a rocha nunca aparecer. O que
        importa para plantar peca de 9 m nao e' a rugosidade de 1 m, e' se a
        ENCOSTA sobe.
        """
        d = 5.0
        hx = abs(self.em_metros(xm + d, ym) - self.em_metros(xm - d, ym))
        hy = abs(self.em_metros(xm, ym + d) - self.em_metros(xm, ym - d))
        return max(hx, hy) / (2.0 * d)


def _regioes(onde):
    if onde == "ilha":
        return None
    if isinstance(onde, str):
        return [POIS[onde]]
    return [POIS[n] for n in onde]


def semear(terreno, medidas, rng):
    plantado = {}
    resumo = []
    for peca in KIT:
        slug = peca["slug"]
        med = medidas.get("pecas", {}).get(slug)
        if med is None:
            resumo.append("SEM MEDIDA, PULADA: %s" % slug)
            continue

        # ESCALA a partir da altura-alvo. A caixa vem em cm do Unreal.
        # Qual eixo da caixa vale como "tamanho" desta peca. Padrao Z (peca em
        # pe'); `eixo: 0` para peca deitada, que se mede pelo comprimento.
        eixo = peca.get("eixo", 2)
        caixa_z_cm = med["caixa_cm"][eixo]
        if caixa_z_cm <= 0.01:
            resumo.append("CAIXA ZERO, PULADA: %s" % slug)
            continue
        escala_base = (peca["altura_m"] * 100.0) / caixa_z_cm

        regioes = _regioes(peca["onde"])
        postos = []
        afasta2 = peca["afasta_m"] ** 2
        cota_min, cota_max = peca["cota"]
        inc_min, inc_max = peca["inclin"]
        tentativas = 0
        limite = peca["quantos"] * 400

        while len(postos) < peca["quantos"] and tentativas < limite:
            tentativas += 1
            if regioes is None:
                # AREA-UNIFORME: sqrt no raio. Sortear raio linear amontoa tudo
                # no centro — o mesmo erro que a Zona ja' cometeu uma vez.
                ang = rng.uniform(0.0, 2.0 * math.pi)
                raio = math.sqrt(rng.random()) * (MAPA_M * 0.5 * 0.86)
                xm = CX + math.cos(ang) * raio
                ym = CY + math.sin(ang) * raio
            else:
                reg = regioes[rng.randrange(len(regioes))]
                ang = rng.uniform(0.0, 2.0 * math.pi)
                raio = math.sqrt(rng.random()) * reg["r"]
                xm = reg["c"][0] + math.cos(ang) * raio
                ym = reg["c"][1] + math.sin(ang) * raio

            h = terreno.em_metros(xm, ym)
            if h < cota_min or h > cota_max:
                continue
            inc = terreno.inclinacao(xm, ym)
            if inc < inc_min or inc > inc_max:
                continue
            perto = False
            for px, py, _ in postos:
                if (px - xm) ** 2 + (py - ym) ** 2 < afasta2:
                    perto = True
                    break
            if perto:
                continue
            postos.append((xm, ym, h))

        ev = peca["escala_var"]
        # O PIVO DO MESHY ESTA NO CENTRO DA PECA, nao na base — medido em
        # 28/08: `base_z_cm` vem NEGATIVO em todas as onze. Plantar na cota do
        # chao enterraria metade de cada peca. Entao a instancia sobe
        # `-base_z * escala`, e so' depois afunda 6% da altura para a base
        # cravar em terreno inclinado em vez de flutuar numa perna so'.
        base_z_cm = med["base_z_cm"]
        instancias = []
        for xm, ym, h in postos:
            e = escala_base * rng.uniform(ev[0], ev[1])
            sobe = -base_z_cm * e
            crava = peca["altura_m"] * 100.0 * 0.06
            instancias.append({
                "loc": [round(xm * 100.0, 1), round(ym * 100.0, 1),
                        round(h * 100.0 + sobe - crava, 1)],
                "yaw": round(rng.uniform(0.0, 360.0), 1),
                "escala": round(e, 5),
            })
        plantado[slug] = {"ativo": med["ativo"], "instancias": instancias}
        resumo.append("%-42s %4d/%-4d  escala %.3f  (%s %.1f cm -> %.1f m)  altura final %.1f m"
                      % (slug, len(instancias), peca["quantos"], escala_base,
                         "XYZ"[eixo], caixa_z_cm, peca["altura_m"],
                         med["caixa_cm"][2] * escala_base / 100.0))
    return plantado, resumo


def main():
    if not os.path.exists(MEDIDO):
        raise SystemExit("falta %s — rode importar_kit.py primeiro" % MEDIDO)
    medidas = json.load(io.open(MEDIDO, encoding="utf-8"))
    terreno = Terreno(carregar_alturas())
    rng = random.Random(SEMENTE)
    plantado, resumo = semear(terreno, medidas, rng)

    total = sum(len(v["instancias"]) for v in plantado.values())
    io.open(SAIDA, "w", encoding="utf-8").write(
            json.dumps({"semente": SEMENTE, "pecas": plantado},
                       indent=1, ensure_ascii=False))
    for linha in resumo:
        print(linha)
    print("-" * 70)
    print("TOTAL DE INSTANCIAS: %d" % total)
    print("SAIDA: %s" % SAIDA)


if __name__ == "__main__":
    main()
