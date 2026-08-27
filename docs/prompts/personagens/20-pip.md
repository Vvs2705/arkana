# 20 — PIP, a Centelha Selvagem

> **Fila:** posição 18 (lote 6 — escala extrema, por último de propósito).
> **Custo:** ~30 créditos.
> **Antes de gerar:** ler [`00-REGRA-DAS-VISTAS.md`](00-REGRA-DAS-VISTAS.md) —
> BLOCOS **A**, **B** e **C** saem de lá.
> **Fontes:** `godot/menu/Elenco.gd` (id 20) · `personagens/20-pip.md`.
>
> ⚠️ **Mago de temporada.** `classe`, `passiva`, `tatica`, `suprema` e
> `limitadores` estão **VAZIOS no `Elenco.gd`** — kit é proposta da equipe até o
> Diretor aprovar. Nada aqui desenha habilidade não aprovada.

## 0. Quem é

Fada, função **Ataque / Perseguição**. Sessenta centímetros de fada com
velocidade suficiente para chegar quase antes do próprio barulho. **Quase:** o
sino no tornozelo é maldição de infância que magia nenhuma tira, e é por ele que
todo mundo sabe que ela está por perto. Coleciona botões arrancados de casacos
alheios — **o do Noctus é a joia da coleção e ela o usa como pingente**. Parada,
ela definha. Então ela não para.

## 1. Ficha física — DIMENSÃO É LEI

- **Altura: 0,60 m.** **Voa sempre a ~1,2 m do chão e NUNCA POUSA** — hitbox
  pequena e alta; o counter é prever. **A âncora de animação do jogo é "nunca
  toca o chão"** (`personagens/20-pip.md`, notas de 3D).
- ⚠️ **Conflito real entre a ficha e o pipeline, e a decisão desta arte:** o
  Meshy pede **A-pose com os pés no chão e sombra de contato** para entender o
  corpo, e o `BLOCO B` exige isso. **As quatro vistas de produção são geradas em
  A-POSE NEUTRA COM OS PÉS APOIADOS**, exatamente como os outros 17 — o "nunca
  pousa" é resolvido no **rig e na animação**, não na referência. A imagem de
  apresentação, sim, a mostra no ar.
- **Proporção de fada adulta pequena**, não de bebê: cabeça grande, corpo
  esguio, membros finos. **Não é chibi.**
- **Pele dourado-clara** com **sardas luminosas que piscam quando ela ri**.
- **QUATRO ASAS de libélula com padrão de CIRCUITO ELÉTRICO** — duas maiores em
  cima, duas menores embaixo, membranosas, translúcidas, com o circuito desenhado
  nas nervuras.
- **Olhos enormes violeta**; **cabelo curto arrepiado por estática permanente**.
- **Vestuário:** **vestido-pétala em camadas azul-elétrico** com **a barra
  chamuscada** (ela voa perto demais dos próprios raios); **polainas listradas**;
  **um SINO minúsculo no tornozelo que ela NÃO consegue tirar** — é o counter
  sonoro dela e não pode faltar; **um BOTÃO de casaca escuro usado como pingente
  no peito** (o botão do Noctus).

## 2. Paleta

| Uso | Hex |
|---|---|
| Azul-elétrico (dominante) | `#2AA7FF` |
| Raio (energia / acento) | `#F5D90A` |
| Violeta (olhos / secundária) | `#8A5CF0` |

## 3. Prompt mestre — imagem de apresentação (16:9)

Colar o **BLOCO A** e em seguida:

```
A tiny fairy, only 0.60 m tall, hovering about 1.2 m above the ground — she
NEVER lands. Adult small-fairy proportions, not a baby and not chibi: large
head, slim body, thin limbs. Warm golden skin with LUMINOUS FRECKLES that blink
when she laughs; huge violet eyes; short hair permanently spiked by static. FOUR
DRAGONFLY WINGS, two larger above and two smaller below, membranous and
translucent, their veins drawn as an ELECTRIC CIRCUIT pattern. A layered
electric-blue petal dress with a SCORCHED HEM; striped leggings; a TINY BELL
locked around her ankle that she cannot remove; a dark coat BUTTON worn as a
pendant on her chest. Zig-zag spark trails behind her, a cel-shaded cloud of
cartoon lightning nearby. Palette #2AA7FF, #F5D90A, #8A5CF0.
```

## 4. As 4 vistas para o Meshy (3:4, UMA figura por imagem)

⚠️ **Em A-pose com os PÉS NO CHÃO e sombra de contato** — ver §1. O voo é do rig.

Colar **BLOCO A + BLOCO B + o texto da vista**. Descrição do corpo, idêntica
nas quatro:

```
Tiny adult fairy woman, 0.60 m tall, small-fairy proportions — large head, slim
body, thin limbs; NOT a baby, NOT chibi. Warm golden skin with luminous
freckles; huge violet eyes; short static-spiked hair. FOUR dragonfly wings, two
larger above and two smaller below, membranous and translucent with an electric
CIRCUIT pattern in the veins, held OPEN and SPREAD so each wing is fully visible
and none overlaps the torso. Layered electric-blue #2AA7FF petal dress with a
scorched hem; striped leggings; a tiny bell on the RIGHT ankle; a dark coat
button as a pendant on the chest. BARE FEET FLAT ON THE GROUND with a firm
contact shadow. No sparks, no lightning, no cloud, no particles, no motion
trails in frame.
```

- **FRENTE** — vista principal; as quatro asas, o sino e o pingente-botão no
  mesmo quadro.
- **PERFIL ESQUERDO 90° VERDADEIRO** — **crítica e a mais difícil**: as asas
  precisam aparecer **de canto, com espessura**, sem cobrir o perfil do corpo.
  Instruir explicitamente: *"the wings seen edge-on, thin, not hiding the body"*.
- **COSTAS** — a raiz das quatro asas nas escápulas é o assunto: é o que o rig
  precisa para pendurar os ossos das asas.
- **TRÊS-QUARTOS REAL** — julgamento apenas.

## 5. Peças-assinatura isoladas (gerações extras, 2D)

```
1) THE FOUR WINGS, isolated on plain dark grey: one image with all four spread
   flat (like a specimen plate), one true edge-on view showing thickness, and one
   close view of the circuit pattern in the veins. TWO versions of the pattern —
   one flat with NO glow, one lit.
2) The ankle bell, isolated: front and side, a tiny hollow metal bell on a fixed
   band that has no clasp — it cannot be removed.
3) The coat button pendant, isolated: a dark ornate coat button on a fine chain.
4) THE STORM CLOUD, its own set (front / true 90-degree side): a small
   cel-shaded ball of cloud with cartoon lightning bolts, its own model.
```

## 6. Negative prompt

**BLOCO C** + os extras dela:

```
baby, toddler, chibi, infant proportions, human child, average height, tall
fairy, two wings, six wings, butterfly wings, feathered wings, bird wings,
wings folded, wings behind the body, wings hiding the torso, no bell, bell
missing, bell with a clasp, clean hem, new dress, long hair, straight hair,
flying pose in the production views, floating above the ground in the production
views, no contact shadow, sparks, lightning, cloud in frame, particles, motion
trails, sexualized outfit
```

## 7. Critérios de aprovação (reprova se qualquer um falhar)

1. **0,60 m com proporção de fada ADULTA** — se ler como bebê ou chibi,
   reprovada. **A altura vai literal para o rigger** (a lição da patinação do
   anão, pendência 1.1 de `docs/PROJETO.md`).
2. **QUATRO asas de libélula** (não duas, não de borboleta, não de pássaro) com
   **padrão de circuito nas nervuras**, abertas e sem cobrir o torso.
3. **O SINO no tornozelo, sem fecho** — é o counter sonoro dela (GDD §10: cor +
   forma + som). Se faltar, reprovada.
4. **O botão de casaca como pingente** — o detalhe de história dela (o botão do
   Noctus).
5. **Barra do vestido chamuscada** + **polainas listradas** + **cabelo curto
   arrepiado**.
6. **Sardas luminosas** na pele dourado-clara.
7. **A-pose com pés no chão e sombra de contato nas quatro vistas** — o "nunca
   pousa" é do rig, não da referência.
8. **Perfil de 90° real** — um olho, uma orelha, nenhum pedaço do peito, com as
   asas **de canto** e o corpo visível.
