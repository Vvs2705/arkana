# 10 — TESSA, a Tecelã de Raios

> **Fila:** posição 2 (lote 1). **Custo:** ~30 créditos.
> **Antes de gerar:** ler [`00-REGRA-DAS-VISTAS.md`](00-REGRA-DAS-VISTAS.md) —
> BLOCOS **A**, **B** e **C** saem de lá.
> **Fontes:** `godot/menu/Elenco.gd` (id 10) · `personagens/10-tessa.md`.

## 0. Quem é

Humana, Dominador, Curta distância / defesa. Aos 9 anos sobreviveu ao raio que
destruiu o moinho da família; aos 15 forjou sozinha o próprio marca-passo
rúnico. Kit implementado no jogo (`godot/core/Kits.gd`). A auditoria de 25/08
apontou Tessa como uma das **duas mais fiéis à ficha** no lote antigo — o
concept dela já sabe o que é; falta só a vista certa.

## 1. Ficha física — DIMENSÃO É LEI

- **Altura: 1,62 m.** Jovem, compacta.
- **Porte:** pequena, ágil, ombros normais; leitura instantânea pelo **cabelo
  arrepiado por estática permanente**, preso num coque com **duas agulhas de
  tear cravadas**.
- **As marcas mágicas — as duas, obrigatórias:**
  1. **Figuras de Lichtenberg** — cicatrizes em forma de samambaia, como um
     raio tatuado pela própria tempestade, subindo do **ombro esquerdo até o
     punho esquerdo**. Ela as adora: **manga curta SÓ desse lado**.
  2. **Marca-passo rúnico** pulsando visível sob a pele do peito, como um
     pequeno tear de luz — e o macacão tem um **recorte de tecido transparente
     sobre ele**, porque ela QUER que vejam.
- **Vestuário:** macacão de oficina com **bobinas de fio de cobre nos bolsos**;
  luvas isolantes **penduradas no cinto** (nunca calçadas — ela usa só para
  dormir).
- Sorriso pronto. Relógios param perto dela.

## 2. Paleta

| Uso | Hex |
|---|---|
| Raio (acento / energia) | `#F5D90A` |
| Azul-noite (dominante do macacão) | `#2B2E63` |
| Cobre (secundária) | `#B07030` |

## 3. Prompt mestre — imagem de apresentação (16:9)

Colar o **BLOCO A** e em seguida:

```
A young human artificer woman, 1.62 m, standing in a workshop lit by arcs of
lightning she is weaving between two anchors. Spiky hair permanently raised by
static, tied in a bun with TWO tear-weaving needles driven through it. Her LEFT
arm is bare — short sleeve on that side only — and covered from shoulder to
wrist with Lichtenberg figures: fern-shaped pale scars branching like a
lightning bolt tattooed by the storm itself, faintly luminous. On her chest,
under a transparent fabric cut-out deliberately sewn into her workshop overall,
a RUNIC PACEMAKER pulses like a tiny loom of light, slightly off-beat. Copper
wire spools in the overall's pockets; insulating gloves hang UNUSED from her
belt. Ready smile. Palette #F5D90A, #2B2E63, #B07030.
```

## 4. As 4 vistas para o Meshy (3:4, UMA figura por imagem)

Colar **BLOCO A + BLOCO B + o texto da vista**. Descrição do corpo, idêntica
nas quatro:

```
Young human woman, 1.62 m, compact build. Spiky static-raised hair in a bun
with two needles through it. Workshop overall in night blue #2B2E63 with a
transparent fabric cut-out over the chest showing a glowing runic pacemaker;
SHORT SLEEVE on the LEFT arm only, that whole arm bare and marked from
shoulder to wrist with pale fern-shaped Lichtenberg scars; the RIGHT arm has a
full long sleeve. Copper wire spools in the pockets, insulating gloves hanging
from the belt, never worn. Flat work boots. Palette #F5D90A, #2B2E63, #B07030.
```

- **FRENTE** — vista principal; o recorte transparente e o marca-passo têm de
  estar legíveis.
- **PERFIL ESQUERDO 90° VERDADEIRO** — é o lado da manga curta: o braço
  cicatrizado fica voltado para a câmera e as agulhas do coque aparecem de
  lado.
- **COSTAS** — o coque com as duas agulhas e as costas do macacão.
- **TRÊS-QUARTOS REAL** — julgamento apenas.

## 5. Peças-assinatura isoladas (gerações extras, 2D)

```
1) The runic pacemaker: a palm-sized device of copper and rune-etched plates
   worn under the skin of the chest, glowing like a tiny loom, shown isolated
   on plain dark grey, front and side.
2) The Lichtenberg scars: the left arm alone, shoulder to wrist, on plain dark
   grey — TWO versions, one flat black-and-white line pattern with NO glow, one
   coloured. Do not bake strong glow into the albedo.
```

## 6. Negative prompt

**BLOCO C** + os extras dela:

```
long sleeves on both arms, bare both arms, scars on the right arm, tattoos
instead of scars, tribal tattoo, gloves worn on the hands, smooth flat hair,
long straight hair, armour, robe, wizard hat, staff, exposed cleavage,
sexualized overall, glowing scars burned into the base colour
```

## 7. Critérios de aprovação (reprova se qualquer um falhar)

1. **Lichtenberg no braço ESQUERDO, do ombro ao punho**, e manga curta **só**
   desse lado. Braço direito com manga longa.
2. **Marca-passo visível pelo recorte transparente do peito** — é a assinatura
   dela, e o macacão foi feito para mostrá-lo.
3. **Duas agulhas de tear no coque** e cabelo arrepiado por estática.
4. **Luvas isolantes penduradas no cinto, não nas mãos.**
5. **Perfil de 90° real** — um olho, uma orelha, nenhum pedaço do peito.
6. Cicatriz sem glow forte assado no albedo (o brilho é do shader —
   `docs/pipeline-arte/PERSONAGENS/02-REFERENCIAS_MULTI_VIEW.md`).
7. Teste de 10% de tela: a 10% do tamanho, ainda se lê o cabelo arrepiado e o
   braço claro contra o macacão escuro?
