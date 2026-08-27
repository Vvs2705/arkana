# 15 — MARIS, a Voz das Marés

> **Fila:** posição 14 (lote 5). **Custo:** ~30 créditos.
> **Antes de gerar:** ler [`00-REGRA-DAS-VISTAS.md`](00-REGRA-DAS-VISTAS.md) —
> BLOCOS **A**, **B** e **C** saem de lá.
> **Fontes:** `godot/menu/Elenco.gd` (id 15) · `personagens/15-maris.md`.
>
> ⚠️ **Mago de temporada.** `classe`, `passiva`, `tatica`, `suprema` e
> `limitadores` estão **VAZIOS no `Elenco.gd`** — kit é proposta da equipe até o
> Diretor aprovar. Nada aqui desenha habilidade não aprovada.

## 0. Quem é

Nereida, função **Longa distância**. Sacerdotisa do lago sob o castelo voador;
subiu junto quando o castelo começou a cruzar o céu e descobriu, com certa
surpresa, que gosta de brigar. Fala em maré: frases que sobem devagar e quebram
de uma vez.

## 1. Ficha física — DIMENSÃO É LEI

- **Altura: 1,80 m.** Curvas fluidas, silhueta ondulante — sem quinas.
- **Pele azul-clara** com **escamas iridescentes** em **três lugares
  específicos: ombros, têmporas e costas das mãos** (não no corpo todo).
- **Guelras discretas no pescoço** — três fendas curtas de cada lado, sutis.
- **OLHOS VERDE-MAR INTEIROS, SEM BRANCO** — ⚠️ **a auditoria de 25/08 mediu que
  a arte de 24/08 veio com esclera branca.** Sem esclera: a íris toma o olho
  todo. É o defeito medido e o motivo principal desta regeração.
- **Cabelo azul-profundo que FLUTUA** como se estivesse sempre submerso —
  fitas verticais em suspensão, nunca caindo por gravidade. No jogo é bone chain
  + shader de fluxo.
- **Vestuário:** **vestido-armadura de conchas polidas e "seda d'água"**
  (tecido que ondula sozinho) — **é 60% do modelo**; **coroa fina de coral
  branco**; **braceletes de pérolas**; **descalça** — os pés nunca tocam de
  verdade o chão: há uma **lâmina d'água de ~2 cm** entre ela e o solo, e ela é
  **parte do visual**.
- ⚠️ **Nas vistas de produção, gerar os PÉS APOIADOS no chão com sombra de
  contato**, sem a lâmina d'água: o Meshy precisa entender que ela pisa. A
  lâmina volta como VFX no jogo e aparece só na imagem de apresentação.

## 2. Paleta

| Uso | Hex |
|---|---|
| Água (dominante / energia) | `#2AA7FF` |
| Verde-mar (olhos / secundária) | `#2E8B74` |
| Pérola (acento) | `#EDE8E0` |

## 3. Prompt mestre — imagem de apresentação (16:9)

Colar o **BLOCO A** e em seguida:

```
A nereid priestess, 1.80 m, flowing and unhurried, standing above the surface
of a still lake with a THIN SHEET OF WATER about two centimetres deep between
her bare feet and the ground — her feet never truly touch it. Pale blue skin
with iridescent scales ONLY on her shoulders, temples and the backs of her
hands. Discreet gills at the sides of the neck. HER EYES ARE ENTIRELY SEA
GREEN, edge to edge, with NO WHITE SCLERA at all. Deep blue hair FLOATING
upward and outward as if she were submerged. A dress-armour of polished shells
and water-silk that ripples on its own; a thin white coral crown; pearl
bracelets. Stylised water in flat cel bands around her, never realistic
transparency. Palette #2AA7FF, #2E8B74, #EDE8E0.
```

## 4. As 4 vistas para o Meshy (3:4, UMA figura por imagem)

Colar **BLOCO A + BLOCO B + o texto da vista**. Descrição do corpo, idêntica
nas quatro:

```
Nereid woman, 1.80 m, flowing silhouette, pale blue skin. Iridescent scales
ONLY on the shoulders, temples and backs of the hands. Discreet gills at the
neck. EYES ENTIRELY SEA GREEN with NO white sclera and no visible pupil ring.
Deep blue hair in floating vertical ribbons, suspended, not falling. A
dress-armour of polished shells and rippling water-silk reaching the ankles; a
thin white coral crown; pearl bracelets on both wrists. BARE FEET FLAT ON THE
GROUND with a firm contact shadow — no water sheet, no water, no splash, no
particles, no transparency effects in frame.
```

- **FRENTE** — vista principal; olhos e escamas legíveis.
- **PERFIL ESQUERDO 90° VERDADEIRO** — **crítica**: o vestido é 60% do modelo e
  só o perfil informa **quanto ele avança à frente e atrás**. Exigir que o
  cabelo flutuante não cubra a linha das costas.
- **COSTAS** — as escamas dos ombros por trás, o fecho do vestido e a queda das
  fitas de cabelo.
- **TRÊS-QUARTOS REAL** — julgamento apenas.

## 5. Peças-assinatura isoladas (gerações extras, 2D)

```
1) The coral crown, isolated on plain dark grey: front, true 90-degree side,
   back. Thin white branching coral, no gems.
2) The shell dress-armour, isolated on a mannequin-free flat lay: front and
   back, showing how the polished shell plates overlap at the shoulders, waist
   and hip.
3) The scale patches: shoulders, temples and backs of the hands isolated on
   plain dark grey, TWO versions — flat pattern with NO iridescence baked in,
   and lit.
```

## 6. Negative prompt

**BLOCO C** + os extras dela:

```
white sclera, visible eye whites, human eyes, round pupils, scales over the
whole body, fish scales everywhere, fish head, mermaid tail, fin legs, hair
falling down, straight heavy hair, wet look hair, water in frame, splash,
foam, bubbles, realistic transparent water, floating above the ground in the
production views, no contact shadow, high heels, shoes, seashell bikini,
sexualized outfit, exposed midriff, trident, harpoon
```

## 7. Critérios de aprovação (reprova se qualquer um falhar)

1. **OLHOS VERDE-MAR INTEIROS, SEM ESCLERA BRANCA.** É o defeito medido no lote
   de 24/08 e a razão desta regeração.
2. **Escamas iridescentes SÓ em ombros, têmporas e costas das mãos** — não no
   corpo todo, não peixe.
3. **Guelras discretas no pescoço** (sutis, não fendas de monstro).
4. **Cabelo flutuando para cima/para fora**, nunca caindo por gravidade.
5. **Coroa de coral branco fina** + **braceletes de pérolas**.
6. **Pés descalços APOIADOS no chão com sombra de contato** nas quatro vistas — a
   lâmina d'água é VFX e só existe na apresentação.
7. **Perfil de 90° real** — um olho, uma orelha, nenhum pedaço do peito, com o
   volume do vestido medido.
8. Água nas imagens: **nenhuma** nas vistas; em **bandas cel** na apresentação,
   nunca transparência realista.
