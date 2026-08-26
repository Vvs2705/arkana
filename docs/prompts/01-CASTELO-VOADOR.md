# 01 — O CASTELO VOADOR

## 0. O que é

A primeira imagem de toda partida: o castelo cruza o céu do entardecer a 320 m
de altura, **sem ninguém visível a bordo** (decisão nº 14), e os magos surgem
das aberturas ao saltar. É a peça que mais vende "jogo de verdade" por segundo
de tela. No código: `world/Castelo.gd` (rota determinística), `Queda.gd`
(PORTAO — o ponto de salto fica na face lateral).

## 1. Ficha física (o modelo obedece; a key art pode dramatizar)

- **Envergadura total: 52 m** de proa a popa · **28 m** de largura · **34 m**
  da base da rocha ao topo da torre mais alta.
- **A base é uma ROCHA INVERTIDA**: um cone irregular de pedra arrancada do
  chão, 18 m de altura, raízes retorcidas e terra exposta pendendo da parte de
  baixo, três pedras menores (1–3 m) orbitando soltas a 2–4 m da base, cada
  uma com uma runa acesa.
- **Sobre a rocha, a fortaleza**: muralha externa de pedra cinza-azulada com
  ameias, UM portão principal em arco ogival de 6 m com o **Selo de Arkana**
  (pentágono + "A") gravado em ouro; pátio interno visível de cima; **cinco
  torres** — a central com 16 m e telhado cônico azul-noite, quatro menores
  (9–11 m, alturas DIFERENTES entre si, nunca simétricas) nos cantos.
- **Cada uma das cinco torres carrega UMA gema elemental** encaixada a 2/3 da
  altura, do tamanho de um escudo (1,2 m): fogo `#FF5A2A`, água `#2AA7FF`,
  terra `#A8763E`, vento `#8FE8C9`, raio `#F5D90A`. As gemas PULSAM — são a
  leitura de longe.
- **A porta de salto** (a que importa para o jogo): uma abertura lateral de
  2,5 × 3 m no flanco da muralha, sem porta física, com o interior em brilho
  dourado suave — é dela que os magos surgem.
- **Vela de energia**: entre as torres, faixas de luz dourada esticadas como
  velas de navio, translúcidas, com runas correndo por elas.

## 2. Micro-detalhes obrigatórios (o "fio da linha")

Pedra da muralha com juntas visíveis e cantos LASCADOS (nada de blocos
perfeitos); musgo verde-escuro nas quinas voltadas para baixo; trepadeiras
secas pendendo da base rochosa; goteiras de terra caindo da rocha (partículas
finas); bandeirolas azul-noite rasgadas nas pontas, bordadas com o fio dourado
do Selo; telhas de ardósia com 3–4 telhas faltando; corrente de âncora partida
pendurada na proa, elos de 30 cm oxidados em verde-bronze; janelas ogivais
ACESAS em âmbar (o castelo está vivo, mas ninguém aparece); cachoeira fina de
luz dourada escorrendo de uma fenda da rocha e evaporando antes de cair.

## 3. PROMPT MESTRE — key art (uma imagem de clima, 16:9)

> Stylized high-end 3D game art, hand-painted textures, soft painterly
> surfaces, strong contact shadows, slightly exaggerated proportions, fantasy
> battle royale key art, deep night-blue and gold palette (#0B1026, #F0C75E),
> clean silhouette readable at distance, no photorealism.
>
> A majestic flying castle crossing an amber sunset sky at high altitude, seen
> from below at a three-quarter angle. The castle rides an inverted cone of
> torn rock, 18 meters of jagged stone with exposed roots and dry vines
> hanging beneath, three small runic boulders orbiting loosely around the
> base, fine debris and dust motes drifting off the underside. Above the rock,
> a blue-grey stone fortress with battlements: one tall central tower with a
> conical night-blue slate roof (a few tiles missing), four smaller
> asymmetrical corner towers of different heights. Each of the five towers
> holds one giant glowing elemental gem embedded at two-thirds height — fire
> red #FF5A2A, water blue #2AA7FF, earth brown #A8763E, wind teal #8FE8C9,
> lightning yellow #F5D90A — pulsing softly. A grand ogival gate bears a
> golden pentagon seal with five small gems and an engraved letter A.
> Translucent golden energy sails stretched between the towers like ship
> sails, faint runes running along them. Torn night-blue banners with golden
> embroidery flutter from the walls; a broken bronze anchor chain with
> oxidized 30 cm links hangs from the prow; warm amber light glows in every
> ogival window but NO people, NO figures anywhere — the castle travels empty.
> A thin waterfall of golden light spills from a crack in the rock and
> evaporates mid-air. One dark side door opening (2.5 by 3 meters) glows soft
> gold on the flank — the jump door. Moss on the lower stone edges, chipped
> masonry corners, visible stone joints. Dramatic scale against painted
> clouds, low camera, the island far below out of focus.

## 4. Vistas para o Meshy (gerar SEPARADAS, fundo neutro, 3:4)

Prefixe cada uma com a âncora de estilo do item 3 (primeiro parágrafo) e:
*"single object centered, plain dark grey studio background, soft even
lighting, full object in frame, no ground, no clouds, no people"*.

- **FRENTE:** *front orthographic view of the flying castle described:
  inverted rock cone base with hanging roots, fortress with central tower and
  four asymmetrical towers, five glowing elemental gems (red, blue, brown,
  teal, yellow), golden pentagon seal gate facing the camera, golden energy
  sails, torn blue banners.*
- **LADO DIREITO:** *right side orthographic view… (mesma descrição), the side
  jump door opening visible glowing gold on this flank, broken anchor chain
  hanging from the prow.*
- **COSTAS:** *back orthographic view… rear battlements, the central tower
  dominating, two rear gems visible (teal and yellow).*
- **TRÊS-QUARTOS:** *three-quarter view from slightly below… all five towers
  readable, gate partially visible, underside roots and orbiting runic
  boulders visible.*

## 5. Negative prompt

> photorealistic, photo, realistic render, people, characters, silhouettes in
> windows, text, watermark, logo, signature, blurry, low quality, symmetrical
> towers, modern architecture, sci-fi, spaceship, extra castles, cropped
> object, frame, border

## 6. Critérios de aprovação (conferir ANTES do Meshy)

1. As CINCO gemas existem, uma por torre, nas cinco cores certas?
2. O Selo (pentágono + A dourado) está no portão?
3. Existe a porta lateral de salto, legível?
4. NINGUÉM aparece (nem sombra em janela)?
5. As torres são assimétricas (alturas diferentes)?
6. A rocha da base tem raízes/terra — parece ARRANCADA do chão, não cortada?
7. Silhueta lê em miniatura (teste: reduza a imagem a 128 px — ainda é um
   castelo voador?)
