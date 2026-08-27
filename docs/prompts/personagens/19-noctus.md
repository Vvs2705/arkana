# 19 — NOCTUS, o Sedento de Éter

> **Fila:** posição 12 (lote 4). **Custo:** ~30 créditos.
> **Antes de gerar:** ler [`00-REGRA-DAS-VISTAS.md`](00-REGRA-DAS-VISTAS.md) —
> BLOCOS **A**, **B** e **C** saem de lá.
> **Fontes:** `godot/menu/Elenco.gd` (id 19) · `personagens/19-noctus.md`.
>
> ⚠️ **Mago de temporada.** `classe`, `passiva`, `tatica`, `suprema` e
> `limitadores` estão **VAZIOS no `Elenco.gd`** — kit é proposta da equipe até o
> Diretor aprovar. Nada aqui desenha habilidade não aprovada.

## 0. Quem é

Vampiro arcano, função **Ataque / Perseguição**. Nobre de uma corte que já não
existe, e continua se vestindo como se ela existisse. **Não bebe sangue —
considera vulgar. Bebe ÉTER:** a mana alheia. Chama o torneio inteiro de
banquete de má educação, e não perde um.

⚠️ **Regra 10+ do projeto, literal em `personagens/00-LEIA.md`:** *"o Noctus
drena ÉTER, não sangue"*. **Nenhuma gota de sangue em nenhuma imagem dele.**

## 1. Ficha física — DIMENSÃO É LEI

- **Altura: 1,88 m.** Magro e anguloso — silhueta vertical elegante.
- **A marca mágica:** **pele branco-mármore** com **veias de ÉTER VIOLETA
  visíveis no pescoço e nas têmporas** — traçado fino, ramificado, luminoso por
  shader (entregar versão **sem glow**, §5).
- **Olhos carmesim de pupila fina.** **Caninos discretos** — 10+: elegante,
  nunca gore, e **nunca à mostra num rosnado**.
- **Cabelo negro escorrido para trás.** Postura de **nobreza antiga levemente
  entediada** — queixo alto, ombros relaxados.
- **Vestuário:** **casaca vitoriana preta de gola alta forrada de carmesim**;
  **abotoadura de rubi**; **colete bordado**; **luvas SEM DEDOS revelando
  anéis-sigilo**; **capa interna curta** que **vira NÉVOA nas bordas** quando
  ele se move rápido — no modelo isso é **shader nas bordas**, não tecido: nas
  vistas de produção a capa é **tecido sólido, borda definida**.

## 2. Paleta

| Uso | Hex |
|---|---|
| Mármore (pele / dominante) | `#E8E4EC` |
| Carmesim (forro / secundária) | `#8B1E2E` |
| Éter violeta (energia) | `#8A5CF0` |

## 3. Prompt mestre — imagem de apresentação (16:9)

Colar o **BLOCO A** e em seguida:

```
An arcane vampire noble, 1.88 m, lean and angular, standing with the mildly
bored bearing of an old court that no longer exists. Marble-white skin with
VIOLET ETHER VEINS visible at the neck and temples, fine and branching and
faintly luminous. Crimson eyes with narrow pupils; discreet fangs, mouth
closed, never snarling. Black hair swept back. A black high-collared Victorian
coat lined in crimson, a ruby cufflink, an embroidered waistcoat, FINGERLESS
gloves showing sigil rings on his fingers, a short inner cape whose edges
dissolve into violet mist as he moves. A thin thread of violet light runs from
an unseen source toward his open hand — he drinks ETHER, never blood. NO blood
anywhere in the image, no bite marks, no wounds. Palette #E8E4EC, #8B1E2E,
#8A5CF0.
```

## 4. As 4 vistas para o Meshy (3:4, UMA figura por imagem)

Colar **BLOCO A + BLOCO B + o texto da vista**. Descrição do corpo, idêntica
nas quatro:

```
Lean angular man, 1.88 m, marble-white skin with fine violet branching veins at
the neck and temples. Crimson eyes, narrow pupils, MOUTH CLOSED. Black hair
swept back. Black high-collared Victorian coat to mid-thigh, lined in crimson
#8B1E2E, an embroidered waistcoat beneath, a ruby cufflink; FINGERLESS gloves
with sigil rings on the exposed fingers; a SHORT inner cape hanging behind the
shoulders as SOLID FABRIC with a clean defined edge — no mist, no dissolve, no
smoke. Polished flat boots. Arms clear of the torso; the cape never crosses the
body. No blood, no fangs bared, no particles.
```

- **FRENTE** — vista principal; as veias de éter e os anéis-sigilo legíveis.
- **PERFIL ESQUERDO 90° VERDADEIRO** — a gola alta é a armadilha: exigir que a
  **linha do queixo e a profundidade do peito** apareçam **por dentro** da gola,
  e que a capa curta fique **atrás**, sem cobrir o perfil.
- **COSTAS** — o corte da casaca e a queda da capa curta.
- **TRÊS-QUARTOS REAL** — julgamento apenas.

## 5. Peças-assinatura isoladas (gerações extras, 2D)

```
1) The sigil rings and fingerless gloves: both hands isolated on plain dark
   grey, back and palm. Thin dark leather cut at the knuckles, three engraved
   metal sigil rings, one ruby cufflink at the wrist.
2) The ether veins: neck, temples and hands isolated on plain dark grey, TWO
   versions — one flat violet line pattern with NO glow, one lit. Do not bake
   strong glow into the albedo.
3) The high collar of the coat, isolated: front, true 90-degree side and back,
   showing the crimson lining and how the collar stands.
```

## 6. Negative prompt

**BLOCO C** + os extras dele:

```
blood, blood drops, bloody mouth, bite marks, wounds, gore, bared fangs,
snarling, monster vampire, bat wings, bat ears, Nosferatu face, claws, coffin,
cross, crucifix, holy symbol, garlic, red glowing eyes as headlights, cape
covering the body, cape turning to smoke in the production views, mist,
particles, gloves with full fingers, muscular build, modern suit, sunglasses
```

## 7. Critérios de aprovação (reprova se qualquer um falhar)

1. **ZERO sangue** — em qualquer imagem, inclusive na apresentação. Ele drena
   éter. Regra 10+ do projeto.
2. **Veias de éter VIOLETA no pescoço e nas têmporas**, finas e ramificadas — e
   entregues também sem glow (§5).
3. **Caninos discretos, boca fechada.** Sem rosnado, sem presa à mostra.
4. **Luvas SEM dedos com anéis-sigilo visíveis.**
5. **Gola alta forrada de carmesim** e **abotoadura de rubi**.
6. **Capa curta como TECIDO SÓLIDO nas vistas de produção** — a névoa é shader
   no jogo e não pode entrar na referência de malha.
7. **Perfil de 90° real** — um olho, uma orelha, nenhum pedaço do peito, com a
   linha do queixo visível apesar da gola.
8. Nenhum símbolo religioso, nem como adereço "de vampiro".
