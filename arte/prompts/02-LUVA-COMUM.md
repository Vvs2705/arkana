# 02 — LUVA COMUM (o loot que todos acham primeiro)

## 0. O que é

A arma básica do Arkana (papel de SMG/pistola — GDD §16.2): conjuração rápida,
curto alcance, dano baixo. **Um elemento por luva**, e a cor da luva É o
elemento (decisão nº 1). Vai existir em CINCO variantes de cor — o modelo 3D é
um só, retexturizado. No chão ela flutua girando sobre o marcador de raridade;
na mão, veste a mão direita.

## 1. Ficha física

- **Comprimento total: 32 cm** (da ponta do dedo médio à boca do punho).
  Cobre a mão inteira e **um terço do antebraço**.
- **Punho (cano):** 11 cm de comprimento, boca com 9 cm de diâmetro, couro
  curtido escuro (`#3A3230`) com DUAS voltas de tira de couro mais clara
  (`#5C4A3A`) afiveladas — fivela de bronze simples de 2 cm, com o furo em uso
  deformado pelo tempo.
- **Dorso:** placa de couro rígido moldado, costurada ao corpo da luva com
  **pontos de sela visíveis, linha grossa encerada cor de osso, 2 pontos por
  centímetro** — o "fio da linha" tem que aparecer. No centro do dorso, a
  **runa do elemento**: um losango facetado de 3,5 cm, encaixado em garra de
  bronze de quatro dentes, brilhando na cor do elemento.
- **Dedos:** couro em segmentos articulados (três gomos por dedo), pontas dos
  dedos ABERTAS — as falanges finais ficam de fora, é uma luva de conjurador
  pobre, meio surrada. Costura reforçada em X entre polegar e indicador (a
  região que mais rasga).
- **Palma:** camurça gasta, mais clara no centro (uso), com uma runa
  circular DESENHADA a tinta que só acende quando conjura.
- **Desgaste:** canto do punho puído com fios soltos; um remendo de couro de
  tom diferente costurado na lateral do mindinho; a garra de bronze com
  pátina esverdeada nos vãos.
- **Emissão de luz:** APENAS a runa do dorso e finas linhas que correm dela
  pelos sulcos das costuras até os nós dos dedos, na cor do elemento —
  intensidade baixa (é a luva COMUM; o brilho cresce com o tier).

## 2. As cinco variantes de cor (mesma luva, outro elemento)

| Elemento | Cor da runa/linhas | Detalhe próprio |
|---|---|---|
| Fogo | `#FF5A2A` | micro-brasas soltando da runa, fumacinha fina |
| Água | `#2AA7FF` | gotículas condensadas no couro ao redor da runa |
| Terra | `#A8763E` | pó de pedra acumulado nas costuras |
| Vento | `#8FE8C9` | fiapos da costura FLUTUANDO como se houvesse brisa |
| Raio | `#F5D90A` | arcos elétricos finos saltando entre os nós dos dedos |

## 3. PROMPT MESTRE — imagem de apresentação (16:9, versão FOGO)

> Stylized high-end 3D game art, hand-painted textures, soft painterly
> surfaces, strong contact shadows, slightly exaggerated proportions, fantasy
> game item concept, deep night-blue and gold palette accents (#0B1026,
> #F0C75E), no photorealism.
>
> A worn leather spellcasting glove for the right hand, 32 centimeters long,
> covering the hand and one third of the forearm, floating and slowly rotating
> above a stone floor, dramatic single spotlight. Dark tanned leather #3A3230
> body; the cuff wrapped twice with lighter leather straps #5C4A3A closed by a
> small worn bronze buckle. On the back of the hand, a rigid molded leather
> plate hand-stitched with THICK visible waxed bone-colored saddle stitching,
> two stitches per centimeter; at its center a faceted diamond-shaped FIRE
> rune gem, 3.5 cm, glowing ember red #FF5A2A, held by a four-pronged bronze
> claw with green patina in the crevices. Articulated leather finger segments,
> three pads per finger, OPEN fingertips exposing the last knuckles,
> reinforced X-stitch between thumb and index. Suede palm worn lighter at the
> center, a faint circular ink rune on it. Thin glowing ember lines run from
> the rune gem along the stitching grooves to the knuckles. Frayed threads at
> the cuff edge, one mismatched leather patch stitched near the little finger,
> tiny floating embers and a wisp of smoke rising from the rune. Common-tier
> item: modest glow, humble materials, heavily used but loved. Item concept
> art, single object, readable silhouette.

## 4. Vistas para o Meshy (separadas, fundo neutro, 3:4)

Prefixe com a âncora + *"single glove centered, plain dark grey background,
soft even lighting, entire glove in frame, no hand inside, no people"*.

- **DORSO (a vista principal):** *back view of the glove flat, fingers up:
  the stitched leather plate, the glowing diamond fire rune in its bronze
  claw, ember lines to the knuckles, cuff straps and buckle visible.*
- **PALMA:** *palm view, fingers up: worn suede palm, faint circular ink
  rune, open fingertips, X reinforcement stitching at the thumb.*
- **LADO DO POLEGAR:** *side profile from the thumb side: cuff depth, strap
  wraps, buckle, the rune gem edge-on showing its height above the plate.*
- **TRÊS-QUARTOS:** *three-quarter hero view slightly from above, fingers
  half-curled as if resting, every material readable: leather, bronze, gem.*

## 5. Negative prompt

> photorealistic, photo, human hand inside, arm, person, mannequin, text,
> watermark, extra fingers, fused fingers, clean new leather, plastic look,
> sci-fi, metal armor gauntlet, symmetrical left glove, pair of gloves, blurry

## 6. Critérios de aprovação

1. Lê como LUVA DE COURO usada — não manopla de metal (isso é o tier 3)?
2. A costura aparece (linha grossa, pontos contáveis)?
3. A runa é UMA, no dorso, na cor do elemento — e o brilho é MODESTO?
4. Pontas dos dedos abertas?
5. Os cinco recolores funcionam trocando SÓ runa/linhas/detalhe (o couro não
   muda de cor)?
