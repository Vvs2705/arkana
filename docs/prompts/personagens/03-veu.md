# 03 — VÉU, a Andarilha

> **Fila:** posição 1 (lote 1). **Custo:** ~30 créditos.
> **Antes de gerar:** ler [`00-REGRA-DAS-VISTAS.md`](00-REGRA-DAS-VISTAS.md) —
> os BLOCOS **A** (âncora de estilo), **B** (enquadramento) e **C** (negative
> base) são colados a partir de lá; este arquivo só traz o que é dela.
> **Fontes:** `godot/menu/Elenco.gd` (id 3) · `personagens/03-veu.md`.

## 0. Quem é

Humana (planos), Errante, Perseguição / fuga. Caiu no plano espectral aos 12
anos quando a torre da vila colapsou e voltou **7 anos depois sem ter
envelhecido um dia**. Kit implementado no jogo (`godot/core/Kits.gd`).

## 1. Ficha física — DIMENSÃO É LEI

- **Altura: 1,58 m** (a menor humana do elenco de lançamento). É a altura que
  vai para o rigger; não arredondar.
- **Porte:** pequena e rápida, ossatura leve, ombros estreitos.
- **Idade aparente: 19 anos.** Ela caiu aos 12 e voltou 7 anos depois **sem
  envelhecer** — o rosto é o de uma jovem de 19, não de 25.
- **Silhueta de leitura:** capuz fechado + **cachecol longo** que flutua como
  fumaça mesmo sem vento — o cachecol é a bandeira dela em movimento e precisa
  aparecer inteiro nas quatro vistas.
- **A marca mágica:** a **mão esquerda é permanentemente semi-espectral** —
  azulada, translúcida, atravessa objetos quando ela não se concentra. Por isso
  a **luva com fecho rúnico** nessa mão, que a mantém sólida.
- Olhos brancos etéreos, **sem pupila e sem íris**.
- Roupas de viagem surradas — ela nunca desfez as malas.

## 2. Paleta

| Uso | Hex |
|---|---|
| Azul espectral (dominante) | `#4A6FA5` |
| Branco etéreo (secundária) | `#E8E6F0` |
| Azul-noite (base) | `#2B2E63` |
| Energia / mão espectral | `#4A6FA5` com translucidez, sem glow assado no albedo |

## 3. Prompt mestre — imagem de apresentação (16:9)

Colar o **BLOCO A** e em seguida:

```
A 19-year-old human wanderer, 1.58 m tall, small and quick, standing at the
edge of a ruined stone tower at dusk. Hood up and closed around her face; a
very long scarf drifts sideways like smoke although there is no wind. Her eyes
are blank ethereal white with NO pupil and NO iris. Her LEFT HAND is
permanently half-spectral — bluish, translucent, the fingers visibly fading
into light — and is contained inside a fitted rune-clasped glove with small
buckles that keep it solid; the right hand is ordinary flesh. Worn travelling
clothes, layered and patched, cold blues and spectral white. She looks 19, not
25: young face, unlined, the age she was when she came back. Palette #4A6FA5,
#E8E6F0, #2B2E63.
```

## 4. As 4 vistas para o Meshy (3:4, UMA figura por imagem)

Colar **BLOCO A + BLOCO B + o texto da vista** (§3 de `00-REGRA-DAS-VISTAS.md`).
Descrição do corpo, idêntica nas quatro:

```
19-year-old human woman, 1.58 m, slight build, narrow shoulders. Hood UP but
pushed back enough that the whole face is visible. Blank ethereal white eyes,
no pupil, no iris. LEFT HAND half-spectral and translucent inside a
rune-clasped glove with small buckles; right hand bare flesh. Very long scarf
hanging along the body — arranged so it NEVER crosses the torso or hides the
hands. Worn layered travelling clothes in cold blue #4A6FA5, ethereal white
#E8E6F0 and night blue #2B2E63. Flat soles, no heel.
```

- **FRENTE** — vista principal do Meshy.
- **PERFIL ESQUERDO 90° VERDADEIRO** — é o lado da **mão espectral**: a luva
  rúnica e a translucidez ficam voltadas para a câmera, e o cachecol deve cair
  para trás para não esconder o perfil do peito.
- **COSTAS** — o cachecol e o capuz por trás; a queda do tecido nas costas é o
  que o modelo precisa aprender.
- **TRÊS-QUARTOS REAL** — só para julgamento; **não** entra no multi-view se
  sair parecida com a frontal.

## 5. Peça-assinatura isolada (geração extra, 2D)

```
The rune-clasp glove of a spectral hand, isolated on plain dark grey, three
views on three separate images: back of the hand, palm, and thumb-side profile.
Fitted leather glove in cold blue with a row of small buckles along the wrist
and an engraved runic clasp on the back of the hand; the fingertips fade into
translucent blue light where the flesh stops being solid.
```

## 6. Negative prompt

**BLOCO C** + os extras dela:

```
adult woman in her mid-twenties, mature face, aged face, pupils, irises,
coloured eyes, both hands solid, both hands spectral, glove on the right hand,
scarf crossing the chest, scarf hiding the hands, heels, clean new clothes,
warm colours, fire, ghost sheet, skull face
```

## 7. Critérios de aprovação (reprova se qualquer um falhar)

1. **Rosto de 19 anos** — a arte de 24/08 a envelheceu para ~25 e isso diverge
   da ficha (auditoria de 25/08). A ficha manda.
2. **Mão ESQUERDA semi-espectral e translúcida, dentro da luva rúnica** — e a
   direita de carne, nua. Se as duas mãos forem iguais, reprovada.
3. **Olhos brancos sem pupila** nas quatro vistas.
4. **Cachecol longo inteiro no quadro**, sem cruzar o torso nem cobrir as mãos.
5. **Perfil de 90° real** — um olho, uma orelha, nenhum pedaço do peito.
6. **1,58 m lido na proporção:** cabeça pequena em relação ao corpo NÃO — a
   direção pede cabeça discretamente maior; o que dá a baixa estatura é a
   ossatura leve e as pernas curtas, não a cabeça.
7. Teste da silhueta: toda preta, o capuz + cachecol ainda a identificam?
