# 17 — SYLVA, a Filha da Floresta

> **Fila:** posição 15 (lote 5). **Custo:** ~30 créditos.
> **Antes de gerar:** ler [`00-REGRA-DAS-VISTAS.md`](00-REGRA-DAS-VISTAS.md) —
> BLOCOS **A**, **B** e **C** saem de lá.
> **Fontes:** `godot/menu/Elenco.gd` (id 17) · `personagens/17-sylva.md`.
>
> ⚠️ **Mago de temporada.** `classe`, `passiva`, `tatica`, `suprema` e
> `limitadores` estão **VAZIOS no `Elenco.gd`** — kit é proposta da equipe até o
> Diretor aprovar. Nada aqui desenha habilidade não aprovada.

## 0. Quem é

Dríade, função **Suporte / Vida**. É a memória viva da floresta do mapa: lembra
de cada árvore que caiu e do nome de quem a derrubou. Doce com quase todos.
Menos com quem queima.

## 1. Ficha física — DIMENSÃO É LEI

- **Altura: 1,75 m.** Silhueta **orgânica e ASSIMÉTRICA** — a regra dela é
  "galhos > simetria". Nenhum par de membros idêntico.
- **A pele é CASCA VIVA verde-amadeirada**, com **veios que acendem em seiva
  dourada** quando ela conjura (versão sem glow obrigatória, §5).
- **Cabelo de FOLHAS e GALHOS FLORIDOS** que mudam com a partida (flores →
  outono conforme a vida cai — a barra de vida é o próprio visual).
  ⚠️ **Gerar SÓ o estado FLORIDO** (vida cheia): é o estado canônico, e o outono
  é material trocável no jogo (`personagens/17-sylva.md`, notas de 3D).
- **Olhos inteiramente VERDES** — sem branco, sem pupila.
- **Dedos alongados como raminhos** (mãos são leitura de personagem: alongar,
  não engrossar).
- **Vestuário — ela NÃO veste tecido:** **musgo denso como túnica viva**;
  **cinto de vinhas com frutos luminosos** (as "poções" dela); **UM ombro
  coberto por uma placa de casca de árvore ancestral gravada com o Selo de
  Arkana** — pentágono com o "A" ao centro, **gravado em baixo-relevo,
  patinado, SEM luz** (é assinatura, não farol).

## 2. Paleta

| Uso | Hex |
|---|---|
| Verde-folha (dominante) | `#3E7A3A` |
| Seiva (energia / acento) | `#F0C75E` |
| Flor (secundária) | `#E48AB0` |

## 3. Prompt mestre — imagem de apresentação (16:9)

Colar o **BLOCO A** e em seguida:

```
A dryad, 1.75 m, standing in a clearing among the stumps of felled trees, an
asymmetric organic silhouette — branches, never symmetry. HER SKIN IS LIVING
BARK, green and woody, veined with channels that light up in GOLDEN SAP as she
casts. Her HAIR IS LEAVES AND FLOWERING BRANCHES in full bloom, pink blossoms
open. Her eyes are ENTIRELY GREEN with no white and no pupil. Her fingers are
long like twigs. She wears no cloth: dense living MOSS serves as a tunic, a
belt of vines carries luminous fruit, and ONE shoulder is covered by a plate of
ancient tree bark engraved in low relief with a PENTAGON SEAL bearing the letter
A — aged patina, NOT glowing. Slow drops of golden sap, pollen drifting like
fireflies. Palette #3E7A3A, #F0C75E, #E48AB0.
```

## 4. As 4 vistas para o Meshy (3:4, UMA figura por imagem)

Colar **BLOCO A + BLOCO B + o texto da vista**. Descrição do corpo, idêntica
nas quatro:

```
Dryad woman, 1.75 m, asymmetric organic build — the two sides of the body are
deliberately NOT identical. Skin of living green-woody bark with sap channels;
hair made of leaves and FLOWERING branches in full pink bloom; entirely green
eyes, no white, no pupil; long twig-like fingers. Dense moss serving as a
tunic; a vine belt with luminous fruit; ONE shoulder — the RIGHT — plated with
ancient bark engraved with a low-relief pentagon seal bearing the letter A, aged
patina, no glow. Bare rooted feet flat on the ground with a firm contact shadow.
No sap drops, no pollen, no particles, no autumn leaves, no bare branches.
```

- **FRENTE** — vista principal; a assimetria e o cabelo florido são o assunto.
- **PERFIL ESQUERDO 90° VERDADEIRO** — **crítica**: o cabelo-galhada é volume
  para todos os lados e só o perfil informa **quanto avança à frente e atrás**.
  Exigir que os galhos **não** cubram a linha do rosto de perfil.
- **COSTAS** — a placa de casca por trás, a queda do musgo e a galhada.
- **TRÊS-QUARTOS REAL** — julgamento apenas.

## 5. Peças-assinatura isoladas (gerações extras, 2D)

```
1) The ancient bark shoulder plate, isolated on plain dark grey: front, true
   90-degree side, back. Thick weathered bark, an engraved low-relief pentagon
   seal with the letter A at its centre, aged patina, NO emissive glow.
2) The vine belt with luminous fruit, isolated: laid flat, front and side, the
   fruit readable as separate small objects.
3) The sap channels: torso and arms isolated on plain dark grey, TWO versions —
   one flat channel pattern with NO glow, one lit gold. Do not bake strong glow
   into the albedo.
4) The hair, TWO STATES for reference only (bloom and autumn), each on its own
   image — the autumn state is a MATERIAL SWAP in engine and does NOT go into
   the multi-view.
```

## 6. Negative prompt

**BLOCO C** + os extras dela:

```
symmetrical body, mirrored limbs, human skin, smooth skin, cloth clothing,
fabric dress, leather armour, autumn hair, bare branches, dead leaves, no
flowers, white sclera, pupils, human eyes, glowing seal, lit pentagon, thick
sausage fingers, tree trunk legs, full tree form, ent, treant, flower crown as
a separate accessory, sap drops, pollen, particles, sexualized plant outfit,
exposed torso
```

## 7. Critérios de aprovação (reprova se qualquer um falhar)

1. **Pele de CASCA VIVA** — se sair pele humana pintada de verde, reprovada.
2. **Cabelo de folhas e galhos FLORIDOS** (estado de vida cheia). Outono é
   material trocável e não entra no multi-view.
3. **Assimetria visível** — a ficha manda "galhos > simetria"; corpo espelhado
   reprova.
4. **Olhos inteiramente verdes**, sem branco e sem pupila.
5. **Dedos alongados como raminhos.**
6. **Placa de casca num ombro SÓ, com o Selo de Arkana gravado e SEM luz.**
7. **Cinto de vinhas com frutos luminosos** — os frutos são as poções dela.
8. **Perfil de 90° real** — um olho, uma orelha, nenhum pedaço do peito, com o
   volume da galhada medido.
9. Nada de tecido em nenhuma peça.
