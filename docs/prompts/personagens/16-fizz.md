# 16 — FIZZ, o Artífice de Bolso

> **Fila:** posição 16 (lote 6 — escala extrema, por último de propósito).
> **Custo:** ~30 créditos (+~30 por torreta/bobina se forem geradas em 3D pela
> Meshy — decisão do Diretor).
> **Antes de gerar:** ler [`00-REGRA-DAS-VISTAS.md`](00-REGRA-DAS-VISTAS.md) —
> BLOCOS **A**, **B** e **C** saem de lá.
> **Fontes:** `godot/menu/Elenco.gd` (id 16) · `personagens/16-fizz.md`.
>
> ⚠️ **Mago de temporada.** `classe`, `passiva`, `tatica`, `suprema` e
> `limitadores` estão **VAZIOS no `Elenco.gd`** — kit é proposta da equipe até o
> Diretor aprovar. Nada aqui desenha habilidade não aprovada.

## 0. Quem é

Gnomo, função **Longa distância**. Inventa mais rápido do que documenta, o que é
a maneira educada de dizer que explode coisas com frequência preocupante. Pede
desculpa explodindo outra coisa.

## 1. Ficha física — DIMENSÃO É LEI

- **Altura: 0,95 m** — **o menor do elenco** (hitbox honesta; a mobilidade
  compensa). ⚠️ **É a altura que vai para o rigger.** A lição da patinação do
  anão (pendência 1.1 de `docs/PROJETO.md`) nasceu justamente de escala errada:
  **0,95, não 1,7.**
- **Proporção de gnomo adulto**, não de criança: cabeça grande, tronco curto e
  robusto, membros curtos, **mãos grandes**. **Não é chibi e não é criança.**
- **Pele rosada com manchas de fuligem.**
- **ÓCULOS de lentes múltiplas empilhadas — a de cima SEMPRE virada para fora**
  (ele jura que é de propósito). Detalhe obrigatório nas quatro vistas.
- **Orelhas pontudas CAÍDAS** (para baixo, não erguidas).
- **Topete ruivo chamuscado.** Sorriso de quem acabou de explodir algo de
  propósito.
- **Vestuário:** **colete de couro cheio de bolsos NUMERADOS fora de ordem**;
  **MOCHILA-OFICINA maior que o próprio tronco**, com **braço mecânico dobrável**
  que entrega ferramentas — **o personagem É a mochila**, é ela que dá a
  silhueta; **luvas grossas demais para as mãos**; **botas com MOLAS visíveis nos
  calcanhares**.

## 2. Paleta

| Uso | Hex |
|---|---|
| Cobre (dominante) | `#B07030` |
| Faísca (energia / acento) | `#F5D90A` |
| Azul-óculos (secundária) | `#2AA7FF` |

## 3. Prompt mestre — imagem de apresentação (16:9)

Colar o **BLOCO A** e em seguida:

```
A gnome artificer, only 0.95 m tall — the smallest of the cast — standing in a
workshop yard beside something he has just blown up, grinning about it. ADULT
gnome proportions, not a child and not chibi: large head, short sturdy torso,
short limbs, big hands. Rosy skin smudged with soot; a scorched red quiff;
POINTED EARS DROOPING DOWNWARD. He wears GOGGLES built from several stacked
lenses, and THE TOP LENS IS ALWAYS FLIPPED OUT to the side. A leather vest
covered in NUMBERED pockets, the numbers out of order; a WORKSHOP BACKPACK
LARGER THAN HIS OWN TORSO with a folding mechanical arm handing him a tool;
gloves far too big for his hands; boots with visible springs at the heels.
Golden gears and blue sparks in the air. Palette #B07030, #F5D90A, #2AA7FF.
```

## 4. As 4 vistas para o Meshy (3:4, UMA figura por imagem)

Colar **BLOCO A + BLOCO B + o texto da vista**. Descrição do corpo, idêntica
nas quatro:

```
Adult gnome man, 0.95 m tall, gnome proportions — large head, short sturdy
torso, short limbs, big hands. NOT a child, NOT chibi. Rosy soot-smudged skin,
scorched red quiff, pointed ears drooping downward. Stacked multi-lens goggles
with the TOP LENS FLIPPED OUT. Leather vest with numbered pockets; a
WORKSHOP BACKPACK LARGER THAN HIS TORSO on his back, its folding mechanical arm
FOLDED CLOSED against the pack; oversized gloves; boots with visible heel
springs. No tools in his hands, no turret, no coil, no gears, no sparks, no
particles in frame.
```

- **FRENTE** — vista principal; os óculos e a lente virada precisam ser
  legíveis.
- **PERFIL ESQUERDO 90° VERDADEIRO** — **a vista mais importante dele**: é a
  única que mede **quanto a mochila-oficina projeta para trás** e a altura dela
  em relação à cabeça. Sem este perfil, a mochila nasce colada nas costas.
- **COSTAS** — a mochila é o assunto: tampas, correias, o braço dobrado.
- **TRÊS-QUARTOS REAL** — julgamento apenas.

## 5. Peças-assinatura isoladas (gerações extras, 2D)

```
1) THE WORKSHOP BACKPACK, isolated on plain dark grey: front, TRUE 90-degree
   side, back, and one view with the folding mechanical arm EXTENDED — four
   separate images. Copper and dark leather, numbered hatches, exhaust vents, a
   segmented folding arm with a simple gripper.
2) The stacked multi-lens goggles, isolated: front and side, with the top lens
   flipped out.
3) THE TURRET, its own set (front / true 90-degree side / back): a small
   copper tripod turret with a blinking beacon lamp, about knee-high to him.
4) THE MEGACOIL, its own set: a copper coil device that charges with growing
   electric arcs.
```

## 6. Negative prompt

**BLOCO C** + os extras dele:

```
child, chibi, baby proportions, cute toddler, human dwarf, dwarf with a beard,
long beard, tall gnome, average height, 1.7 m, adult human proportions, ears
pointing up, all lenses flat, goggles worn on the forehead, small backpack,
no backpack, backpack in front, tools in hands, turret in frame, coil in frame,
gears, sparks, particles, steampunk top hat, monocle, cigar
```

## 7. Critérios de aprovação (reprova se qualquer um falhar)

1. **0,95 m com proporção de gnomo ADULTO** — cabeça grande, tronco curto, mãos
   grandes. Se ler como criança ou chibi, reprovada. **A altura vai literal para
   o rigger.**
2. **A mochila-oficina é maior que o tronco** e presente nas quatro vistas — o
   personagem é a mochila.
3. **A lente de cima dos óculos virada para fora.**
4. **Orelhas pontudas CAÍDAS**, não erguidas.
5. **Bolsos numerados fora de ordem** + **luvas grandes demais** + **molas nos
   calcanhares**.
6. **Braço mecânico DOBRADO** nas vistas (estendido só na peça isolada).
7. **Perfil de 90° real** — um olho, uma orelha, nenhum pedaço do peito, com a
   projeção da mochila medida.
8. **Sem barba** — é gnomo, não anão (o anão do elenco é o Brok).
