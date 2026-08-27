# 06 — RUÍNAS: ESTÁTUAS E ARCOS (o POI vira lore)

## 0. O que é

O POI RUINS da ilha (platô elevado, `Island.gd`) hoje é pedra genérica. Vira o
**cemitério cerimonial dos Primeiros Conjuradores**: estátuas de magos em
círculo e arcos rúnicos partidos — cobertura de combate que conta história. O
kit tem TRÊS peças geradas separadamente (o Meshy quer um objeto por vez):

- **6A** — Estátua do Conjurador (2 poses)
- **6B** — Arco rúnico partido
- **6C** — Coluna quebrada com braseiro

## 1. Ficha física

### 6A — Estátua do Conjurador
- **3,2 m de altura** sobre pedestal octogonal de 0,9 m (total 4,1 m) — altura
  de cobertura: esconde um mago em pé (1,8 m) atrás do pedestal.
- Mago encapuzado em **granito cinza-azulado** (`#8E97AD`), veios diagonais
  visíveis; o manto esculpido cai em dobras profundas (sombra dura dentro das
  dobras — o toon shader adora isso).
- **Pose A "O Ofertante":** braço direito estendido à frente, palma aberta
  para cima; na palma, um **cristal bruto de 20 cm** (a única parte NÃO de
  pedra — cor por elemento nas variantes, âmbar `#F0C75E` na neutra).
- **Pose B "O Vigia":** as duas mãos apoiadas num bastão fincado, capuz baixo,
  olhando o horizonte.
- **Dano do tempo:** nariz e três dedos QUEBRADOS expostos em pedra clara
  (fratura recente vs. superfície escurecida); rachadura em raio atravessando
  o peito; **musgo verde-escuro** (`#74A06A`) nas superfícies voltadas ao
  norte e nos vãos das dobras; **líquen amarelo-pálido** em placas circulares
  de 3–8 cm; escorrimento escuro de chuva sob o queixo e axilas.
- No pedestal, **friso gravado**: procissão de figuras minúsculas (10 cm) de
  mãos dadas — a Sintonia dos antigos — meio comida pela erosão.

### 6B — Arco rúnico partido
- Vão original de **4 m de largura × 5 m de altura**; o arco está **PARTIDO no
  topo** — as duas metades sobem e terminam em fratura, com o bloco-chave
  caído aos pés, meio enterrado (45°).
- Aduelas de granito com **runas geométricas puncionadas** (pentágonos,
  meias-luas — nunca alfabeto real), uma runa por aduela; **três runas ainda
  ACESAS em âmbar fraco**, piscando como lâmpada cansada — o resto apagou.
- Trepadeira seca agarrada à metade esquerda; um arbusto nascendo da junta da
  terceira aduela.

### 6C — Coluna quebrada com braseiro
- Coluna dórica-arcana de **60 cm de diâmetro**, quebrada a **1,6 m** (altura
  de cobertura agachada); topo em fratura diagonal.
- Sobre a fratura, um **braseiro de bronze de 45 cm** com pátina verde,
  três pés de garra, **chama espectral azul-pálida** (`#8FE8C9`) queimando
  sem lenha — nunca apaga, não aquece: é memória.
- Tambores da coluna deslocados 4 cm entre si (terremoto antigo); estrias
  preenchidas de terra e musgo na metade de baixo.

## 2. PROMPT MESTRE — apresentação do conjunto (16:9)

> Stylized high-end 3D game art, hand-painted textures, soft painterly
> surfaces, strong contact shadows, slightly exaggerated proportions, fantasy
> game environment concept, deep night-blue and gold palette (#0B1026,
> #F0C75E), no photorealism.
>
> Ancient ceremonial ruins of the First Conjurers on a raised stone plateau at
> amber dusk: a circle of weathered blue-grey granite statues (#8E97AD) of
> hooded mages 3.2 meters tall on octagonal pedestals — one holding out an
> open palm bearing a rough amber crystal, one resting both hands on a planted
> staff — deep carved robe folds with hard shadows, broken noses and fingers
> showing lighter fresh stone, dark-green moss (#74A06A) on north faces, pale
> yellow lichen patches, rain streaks under chins; pedestals carved with an
> eroded frieze of tiny hand-holding figures. A broken runic arch, 4 by 5
> meters, split at the crown, its keystone half-buried at its feet, each
> voussoir punched with one geometric rune — pentagons and half-moons, no real
> alphabet — three runes still glowing faint amber and flickering. A snapped
> Doric-arcane column, 60 cm thick, broken at 1.6 meters, drums shifted
> slightly, topped by a green-patina bronze brazier on three claw feet burning
> a pale-blue spectral flame with no fuel. Dry vines, small bushes growing
> from stone joints, golden dust motes in the air. Environment concept, no
> people, readable silhouettes for combat cover.

## 3. Vistas para o Meshy — POR PEÇA (fundo neutro, 3:4)

Prefixe cada geração com a âncora + *"single object centered, plain dark grey
background, soft even lighting, no ground scenery, no people"*.

- **6A frente / lado / costas / três-quartos** — *the hooded mage statue
  described (pose A: offering palm with rough crystal), full statue with
  pedestal in frame.* (Repetir a série para a pose B.)
- **6B frente / três-quartos** — *the broken runic arch described, both
  halves and the fallen keystone included.*
- **6C frente / três-quartos** — *the snapped column with the bronze brazier
  and pale-blue spectral flame.*

## 4. Negative prompt

> photorealistic, photo, people, living person, modern ruins, concrete,
> graffiti, real alphabet, latin letters, text, watermark, clean new stone,
> symmetrical perfect arch, skulls, gore, blurry, low quality

## 5. Critérios de aprovação

1. As alturas de COBERTURA funcionam (pedestal esconde em pé; coluna esconde
   agachado)?
2. Fraturas em pedra CLARA contra superfície escurecida (o tempo lê)?
3. Musgo/líquen só onde faz sentido (faces norte, vãos, metade de baixo)?
4. Runas geométricas, zero alfabeto real?
5. O friso da Sintonia aparece no pedestal (o lore está lá)?
6. A chama espectral é AZUL-PÁLIDA e sem lenha (memória, não fogueira)?
