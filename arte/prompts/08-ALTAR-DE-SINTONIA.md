# 08 — ALTAR DE SINTONIA (o monumento do pilar do jogo)

## 0. O que é

A Sintonia — dois magos fundindo elementos — é o pilar de inovação do GDD §9.
O altar é o monumento que ENSINA a mecânica sem palavras: **duas posições de
conjurador frente a frente e um ponto de fusão no meio**. Landmark de POI e,
no futuro, palco de eventos de partida.

## 1. Ficha física

- **Pegada total: círculo de 9 m** de diâmetro em lajota de pedra clara
  (`#B3BCCF`) com rejunte fundo; a lajota desenha um **pentágono inscrito**
  no piso, com sulcos de 4 cm que correm luz (âmbar fraco em repouso).
- **As duas PLATAFORMAS DE CONJURADOR:** discos de pedra elevados 30 cm, de
  **1,4 m de diâmetro**, diametralmente opostos, cada um com **um par de
  pegadas POLIDAS pelo uso** fundidas na pedra — séculos de conjuradores no
  mesmo lugar. Atrás de cada disco, um **encosto de pedra em meia-lua** de
  1,2 m de altura com runas geométricas na face interna.
- **O CENTRO:** um **braseiro-fonte de bronze de 1,1 m** de diâmetro sobre
  tripé de garras, contendo não fogo, mas uma **esfera de energia neutra de
  40 cm** flutuando 60 cm acima — cinza-perolada em repouso; quando dois
  elementos chegam, ela assume as DUAS cores em espiral (a imagem de
  apresentação mostra FOGO+VENTO: `#FF5A2A` + `#8FE8C9` trançados).
- **Os CINCO OBELISCOS:** nos vértices do pentágono do piso, obeliscos de
  granito de **2,6 m**, cada um coroado por uma **gema elemental de 25 cm**
  (as cinco cores oficiais). Dois deles estão **TOMBADOS** — um partido em
  três tambores, outro inteiro mas caído — e as gemas desses dois estão
  APAGADAS: o altar é antigo e meio quebrado, o mundo esqueceu a Sintonia.
- **Canais de luz:** dos obeliscos ACESOS, sulcos no piso correm até o
  braseiro central — nos tombados, o sulco morre no meio do caminho, seco.
- **Vida:** grama alta invadindo as lajotas da borda; musgo nos encostos;
  uma árvore jovem nascida ENTRE os tambores do obelisco partido; pétalas
  douradas (da árvore) espalhadas pelo piso; pó dourado suspenso sobre a
  esfera.

## 2. PROMPT MESTRE — apresentação (16:9)

> Stylized high-end 3D game art, hand-painted textures, soft painterly
> surfaces, strong contact shadows, slightly exaggerated proportions, fantasy
> game environment concept, deep night-blue and gold palette (#0B1026,
> #F0C75E), no photorealism.
>
> An ancient attunement altar in a grass clearing at amber dusk: a 9-meter
> circle of pale stone tiles (#B3BCCF) with deep joints, a glowing pentagon
> inlaid in the floor as 4 cm light-filled grooves. Two raised stone caster
> platforms, 1.4 meters wide and 30 cm tall, facing each other across the
> circle, each bearing a pair of footprints POLISHED into the stone by
> centuries of use, backed by a half-moon stone rest carved with geometric
> runes. At the center, a bronze basin-brazier 1.1 meters wide on claw-foot
> tripod, and floating 60 cm above it a 40 cm energy sphere braided in TWO
> colors — ember red #FF5A2A and wind teal #8FE8C9 spiraling together. Five
> granite obelisks 2.6 meters tall at the pentagon's points, each crowned by
> a 25 cm elemental gem (red, blue, brown, teal, yellow); TWO obelisks have
> fallen — one broken into three drums with a young golden-leaf tree growing
> between them, one lying whole — their gems dark and dead, their floor
> light-grooves dry halfway. From the standing obelisks, glowing grooves run
> along the floor into the central basin. Tall grass invading the outer
> tiles, moss on the stone rests, golden petals scattered, gold dust
> suspended above the sphere. Environment concept, no people, readable
> layout from a high camera angle.

## 3. Vistas para o Meshy — POR PEÇA (fundo neutro)

O altar vai ao Meshy **desmontado** (peças modulares; o conjunto monta no
Godot). Prefixe com a âncora + *"single object centered, plain dark grey
background, soft even lighting, no scenery, no people"*.

- **Plataforma de conjurador** (3:4): *the raised caster platform described:
  30 cm stone disc with polished footprints, half-moon runic back rest.*
  (frente + três-quartos)
- **Braseiro central** (3:4): *the bronze claw-foot basin with the floating
  two-color braided energy sphere.* (frente + três-quartos)
- **Obelisco INTEIRO** (9:16): *standing granite obelisk crowned by one
  glowing elemental gem.* (frente + lado)
- **Obelisco PARTIDO** (3:4): *the fallen obelisk broken into three drums,
  dead gem, young golden-leaf tree growing between the drums.* (três-quartos)

## 4. Negative prompt

> photorealistic, photo, people, wizards standing, sacrificial altar, blood,
> skulls, pentagram occult symbols, inverted pentagram, candles, text,
> watermark, real alphabet, church, gothic cathedral, blurry, low quality

## 5. Critérios de aprovação

1. A MECÂNICA lê sem texto: dois lugares de pé + um ponto de fusão no meio?
2. O pentágono do piso é o SELO do jogo (nunca vira pentagrama oculto — sem
   velas, sem sangue, checagem do público 10+)?
3. Dois obeliscos mortos contam a história (o mundo esqueceu a Sintonia)?
4. As pegadas polidas aparecem (o detalhe que faz arqueologia)?
5. A esfera trança DUAS cores — não mistura numa terceira?
