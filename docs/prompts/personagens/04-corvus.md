# 04 — CORVUS, o Caçador

> **Fila:** posição 5 (lote 2). **Custo:** ~30 créditos na forma humana
> (+~30 se a forma de lobisomem for gerada em 3D — decisão do Diretor).
> **Antes de gerar:** ler [`00-REGRA-DAS-VISTAS.md`](00-REGRA-DAS-VISTAS.md) —
> BLOCOS **A**, **B** e **C** saem de lá.
> **Fontes:** `godot/menu/Elenco.gd` (id 4) · `personagens/04-corvus.md`.

## 0. Quem é

Humano tribal, Errante, Ataque / Perseguição. **É CEGO.** No inverno da fome o
espírito-lobo o atacou; sangrando, ele dividiu com a fera a última caça, e o
espírito ficou — em dívida. É POR ele que Corvus "enxerga": sente cheiros como
cores e trilhas no ar. Antes de vestir a forma da fera, ele pede licença.

## 1. Ficha física — DIMENSÃO É LEI

- **Altura: 1,85 m**, mas **curvado para frente** como quem fareja o chão — a
  altura de rig é 1,85; a **postura curvada é a silhueta**, e é ela que quebra
  a fila de silhuetas eretas do elenco.
- **A marca mágica:** **três cicatrizes de garra atravessando os dois olhos**,
  que estão **pálidos e sem foco**. Ele não olha para nada.
- Pele curtida de frio; **cabelo escuro trançado com penas**.
- **Vestuário:** **máscara de couro com olhos de vidro** — não são para ver, são
  para os OUTROS não desviarem o olhar das cicatrizes; **penas de corvo no
  capuz**; **peles sobrepostas**; **faixas nos antebraços com nós de contagem**,
  um nó para cada inverno sobrevivido.
- **DUAS formas a modelar:** humana (curvada, farejando) e **lobisomem**
  (massiva). A forma de besta é geração própria, §5 — **nunca na mesma imagem**.
- ⚠️ **A máscara:** gerar as vistas com a máscara **erguida na testa ou pendurada
  no pescoço**, com o **rosto e as cicatrizes visíveis**. O modelo precisa
  aprender o rosto; a máscara vira adereço separado no Blender.

## 2. Paleta

| Uso | Hex |
|---|---|
| Terrosos (dominante) | `#6B5138` |
| Vento (as trilhas de cheiro) | `#8FE8C9` |
| Vermelho-caça (só nos rastros da suprema) | `#C2452D` |

## 3. Prompt mestre — imagem de apresentação (16:9)

Colar o **BLOCO A** e em seguida:

```
A tall tribal hunter, 1.85 m, hunched forward low over frozen ground as if
scenting it. HE IS BLIND: three parallel claw scars run across both of his
eyes, which are pale and unfocused, looking at nothing. Weather-cured skin,
dark hair braided with feathers. A leather mask with round glass eyes is
pushed up onto his forehead so the scars stay visible. Raven feathers on his
hood, layered furs over his shoulders, forearm wraps knotted with counting
knots — one knot for every winter survived. Around him, ribbons of translucent
teal light drift low along the ground: the smell trails he perceives instead
of sight. Palette #6B5138, #8FE8C9, #C2452D.
```

## 4. As 4 vistas para o Meshy (3:4, UMA figura por imagem)

⚠️ **Só a forma humana.** A forma de lobisomem é §5, em imagens próprias.

Colar **BLOCO A + BLOCO B + o texto da vista**, com **uma alteração de pose**:

```
Tall tribal man, 1.85 m, in a neutral A-pose but with the SPINE SLIGHTLY
HUNCHED FORWARD and the head carried low — this stoop is his silhouette and
must appear in every view. Blind: three parallel claw scars across both eyes,
eyes pale and unfocused. Dark hair braided with feathers. Leather mask with
glass eyes pushed UP onto the forehead, face fully visible. Raven feathers on
the hood, layered furs, forearm wraps with counting knots. Earth-toned palette
#6B5138. Fur boots. No smell trails, no particles, no weapon in frame.
```

- **FRENTE** — vista principal; as três cicatrizes e os olhos pálidos legíveis.
- **PERFIL ESQUERDO 90° VERDADEIRO** — **é a vista crítica dele**: a curvatura
  da coluna e o quanto a cabeça avança à frente dos ombros só existem aqui. Sem
  este perfil, o modelo nasce ereto e perde o personagem.
- **COSTAS** — as peles sobrepostas e o capuz de penas por trás.
- **TRÊS-QUARTOS REAL** — julgamento apenas.

## 5. Peças-assinatura isoladas (gerações extras, 2D)

```
1) The leather mask with round glass eyes, isolated on plain dark grey: front,
   TRUE 90-degree side, and back of the mask — three separate images.
2) THE WEREWOLF FORM, its own full set of views (front / true 90-degree left
   profile / back), one figure per image, same BLOCO B framing: a massive
   upright wolf-beast, 2.4 m at the shoulder when standing, SAME colour scheme
   as the human form so players read it as him — earth-toned fur #6B5138, the
   same three claw scars across the muzzle where his eyes are, the same
   counting-knot wraps on the forelimbs. Digitigrade legs, heavy shoulders. No
   gore, no blood, no exposed flesh: the transformation is feathers and mist.
```

## 6. Negative prompt

**BLOCO C** + os extras dele:

```
upright straight posture, military bearing, seeing eyes, focused gaze, pupils,
mask covering the face, mask over the eyes, blindfold, glowing eyes, wolf and
man in the same image, transformation mid-way, gore, blood, torn flesh,
exposed bone, feathers hiding the face, bow, gun, sword, smell trails,
particles, war paint on the face
```

## 7. Critérios de aprovação (reprova se qualquer um falhar)

1. **As três cicatrizes de garra atravessando OS DOIS olhos**, e os olhos
   pálidos e sem foco. Se o olhar tiver direção, reprovada.
2. **Postura curvada nas quatro vistas** — é a silhueta que o distingue do
   elenco inteiro.
3. **Máscara erguida, rosto à mostra** (a máscara vira adereço depois).
4. **Nós de contagem nos antebraços** legíveis.
5. **Penas no capuz e nas tranças** — as duas coisas.
6. **Perfil de 90° real** — um olho, uma orelha, nenhum pedaço do peito, e a
   curva da coluna claramente visível.
7. Forma de lobisomem (§5): **mesmo esquema de cor** da forma humana e as
   **mesmas três cicatrizes**, sem gore. Se um jogador não reconhecer que é ele,
   reprovada.
