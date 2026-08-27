# 08 — ILUSIONISTA, o Espelho

> **Fila:** posição 9 (lote 3) — **fecha o elenco de LANÇAMENTO (ids 1–10)**.
> **Custo:** ~30 créditos.
> **Antes de gerar:** ler [`00-REGRA-DAS-VISTAS.md`](00-REGRA-DAS-VISTAS.md) —
> BLOCOS **A**, **B** e **C** saem de lá.
> **Fontes:** `godot/menu/Elenco.gd` (id 8) · `personagens/08-ilusionista.md`.

## 0. Quem é

Humano, Guardião, Suporte / engano. Ficou **três anos preso dentro do espelho
do próprio mestre** e **saiu pelo lado errado**: o corpo voltou invertido. Um
dos reflexos do Baile é o original dele, e nem ele sabe qual. Fala sem parar
desde então — três anos de silêncio foram suficientes.

## 1. Ficha física — DIMENSÃO É LEI

- **Altura: 1,76 m.** Porte de showman, elegante, nada musculoso.
- **A marca mágica — a INVERSÃO, e ela é literal em cada detalhe:**
  - **Olhos ESPELHADOS:** de perto **não têm íris** — refletem quem olha.
    Superfície espelhada convexa, não olho branco, não olho leitoso.
  - **Os botões da casaca no lado ERRADO** (fechamento invertido em relação ao
    de um homem — alfaiate nenhum conserta).
  - **O cabelo cai para o lado oposto** do que caía.
  - **Ele é canhoto** — era destro. Onde houver assimetria de mão (baralho,
    gesto, anel), fica na **esquerda**.
  - O coração bate à direita (não é visível, mas explica o resto).
- **Vestuário:** **casaca de gala roxa bordada de fio dourado**; **cartola
  opcional de pose** — ⚠️ **fora das vistas de produção** (adereço separado);
  **luvas brancas**; **abotoaduras de caco de espelho**, do espelho original
  (ele guarda todos os cacos).
- Sorriso fácil que **nunca chega inteiro aos olhos**.

## 2. Paleta

| Uso | Hex |
|---|---|
| Roxo de palco (dominante) | `#6B3FA0` |
| Dourado (bordado / acento) | `#F0C75E` |
| Prata-espelho (energia) | `#D8D8E0` |

## 3. Prompt mestre — imagem de apresentação (16:9)

Colar o **BLOCO A** e em seguida:

```
A human stage illusionist, 1.76 m, elegant and unmuscled, mid-flourish in a
hall of standing mirrors, an easy showman's smile that does not reach his eyes.
HIS EYES ARE MIRRORS: convex reflective surfaces with NO iris and NO pupil,
throwing back whatever looks at him. Everything about him is REVERSED, because
he came out of a mirror the wrong way round: the buttons of his coat fasten on
the wrong side, his hair falls to the opposite side from the one it used to,
and he gestures with his LEFT hand — he was right-handed once. A purple stage
coat embroidered with gold thread, white gloves, cufflinks made from shards of
a broken mirror. Around him, shards of light are reassembling into reflections
of himself; each reflection catches a golden frame highlight for an instant.
No blood, no pain — glass and stagecraft. Palette #6B3FA0, #F0C75E, #D8D8E0.
```

## 4. As 4 vistas para o Meshy (3:4, UMA figura por imagem)

⚠️ **Sem cartola, sem reflexos, sem cacos flutuando** nas vistas de produção.

Colar **BLOCO A + BLOCO B + o texto da vista**. Descrição do corpo, idêntica
nas quatro:

```
Human man, 1.76 m, elegant slim build, no hat, head bare. MIRRORED EYES:
convex reflective surfaces with no iris and no pupil. Hair parted and falling
to his LEFT. A purple #6B3FA0 stage coat to mid-thigh, embroidered with gold
#F0C75E thread, the buttons fastening on the REVERSED side; white gloves on
both hands; mirror-shard cufflinks at the wrists. Polished flat shoes. No top
hat, no cane, no cards, no reflections, no floating shards, no particles in
frame.
```

- **FRENTE** — vista principal; a inversão do fechamento da casaca precisa ser
  visível.
- **PERFIL ESQUERDO 90° VERDADEIRO** — a única vista que informa a **espessura
  da casaca de gala** e a queda das abas.
- **COSTAS** — o corte da casaca por trás (é o que dá elegância à silhueta).
- **TRÊS-QUARTOS REAL** — julgamento apenas.

## 5. Peças-assinatura isoladas (gerações extras, 2D)

```
1) The top hat, isolated on plain dark grey: front, TRUE 90-degree side, top.
   Purple with a gold band.
2) The mirror-shard cufflink, isolated, front and side: an irregular fragment
   of silvered glass set in gold.
3) The mirror eyes, close study on plain dark grey: a pair of eyes whose
   surface is a convex mirror — NO iris, NO pupil, NO white sclera pattern,
   just a clean reflective dome inside the lids.
```

## 6. Negative prompt

**BLOCO C** + os extras dele:

```
irises, pupils, coloured eyes, white blind eyes, milky eyes, glowing eyes,
buttons on the normal side, symmetrical coat, top hat worn, cane, playing
cards, doves, reflections in frame, duplicate figures, floating glass shards,
particles, muscular build, armour, mask, blood, cracked face, broken skin,
sad expression, villain sneer
```

## 7. Critérios de aprovação (reprova se qualquer um falhar)

1. **Olhos espelhados sem íris e sem pupila** — não branco, não leitoso, não
   brilhante. Superfície reflexiva. É a assinatura dele e a arte de 24/08 foi
   gerada com a ficha ANTIGA.
2. **Botões no lado ERRADO** — a inversão tem de ser lida na roupa.
3. **Cabelo caindo para a esquerda** (lado oposto ao natural) e, onde houver
   gesto de mão, a **esquerda**.
4. **Abotoaduras de caco de espelho** nos dois punhos.
5. **Sem cartola e sem reflexos** nas quatro vistas — reflexo é o MESMO modelo
   com shader (`personagens/08-ilusionista.md`), não pode entrar na referência.
6. **Perfil de 90° real** — um olho, uma orelha, nenhum pedaço do peito.
7. **Nada de vidro quebrando o corpo dele**: ele estilhaça em **cacos de luz**,
   nunca em ferimento. 10+.
