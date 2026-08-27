# 12 — UMBRA, a Lâmina da Noite

> **Fila:** posição 10 (lote 4). **Custo:** ~30 créditos.
> **Antes de gerar:** ler [`00-REGRA-DAS-VISTAS.md`](00-REGRA-DAS-VISTAS.md) —
> BLOCOS **A**, **B** e **C** saem de lá.
> **Fontes:** `godot/menu/Elenco.gd` (id 12) · `personagens/12-umbra.md`.
>
> ⚠️ **Mago de temporada.** No `Elenco.gd`, os campos `classe`, `passiva`,
> `tatica`, `suprema` e `limitadores` dela estão **VAZIOS de propósito** — o kit
> é proposta da equipe até o Diretor aprovar. **Nenhum prompt deste arquivo
> desenha habilidade não aprovada**; o que aparece vem só de aparência,
> vestuário e VFX de assinatura, que estão fechados na ficha.

## 0. Quem é

Elfa Negra (Drow), função **Ataque / Perseguição**. A corte subterrânea a
expulsou e ela agradeceu em voz alta na saída. Caça por contrato, cobra caro,
desconto zero para nobres. Luz direta a incomoda mais do que qualquer inimigo.

## 1. Ficha física — DIMENSÃO É LEI

- **Altura: 1,70 m.** Compacta e **felina** — musculatura enxuta, cintura
  estreita, postura de corredora, ligeiramente agachada.
- **A marca mágica:** **tatuagens rúnicas ROXAS na pele cinza-ardósia**, que
  **acendem no escuro**. Gerar em **duas versões** (§5): apagadas (albedo) e
  acesas — **não assar o glow forte no albedo**.
- **Olhos brancos luminosos**, sem pupila.
- **Cabelo branco: raspado de um lado, trança longa do outro** — assimetria
  obrigatória, é metade da leitura de silhueta.
- **Cicatriz fina atravessando a sobrancelha.**
- **Vestuário:** **couro escuro justo** com **placas segmentadas nos ombros e
  antebraços**; **meia-capa com capuz** que sombreia o rosto (nas vistas, capuz
  **abaixado**: o modelo precisa aprender o rosto); **fitas roxas nos punhos**
  que flutuam quando ela corre.
- **AS LÂMINAS GÊMEAS CURVAS presas na LOMBAR** — ⚠️ **a auditoria de 25/08
  mediu que elas FALTAM na arte de 24/08.** São o adereço-assinatura e o motivo
  principal desta regeração.

## 2. Paleta

| Uso | Hex |
|---|---|
| Ardósia (pele / dominante) | `#3A3A46` |
| Roxo umbrio (runas / energia) | `#8A5CF0` |
| Branco lunar (cabelo / acento) | `#E8E6F0` |

## 3. Prompt mestre — imagem de apresentação (16:9)

Colar o **BLOCO A** e em seguida:

```
A drow elf mercenary, 1.70 m, compact and feline, crouched low on a dark
rooftop at night. Slate-grey skin covered with PURPLE RUNIC TATTOOS that glow
in the dark along her arms, collarbones and thighs. Luminous white eyes with no
pupil. White hair SHAVED on one side and worn in a long braid on the other. A
thin scar crosses one eyebrow. Tight dark leather with segmented plates on the
shoulders and forearms; a half-cape with a hood; purple ribbons at her wrists
drifting behind her. TWO CURVED TWIN BLADES are sheathed at the small of her
back, crossed, clearly visible. Purple smoke curls at her feet. Palette
#3A3A46, #8A5CF0, #E8E6F0.
```

## 4. As 4 vistas para o Meshy (3:4, UMA figura por imagem)

Colar **BLOCO A + BLOCO B + o texto da vista**. Descrição do corpo, idêntica
nas quatro:

```
Drow elf woman, 1.70 m, compact athletic build, slate-grey skin #3A3A46 with
purple runic tattoos on the arms, collarbones and thighs. Luminous white eyes,
no pupil. White hair shaved on one side, long braid on the other; a thin scar
across one eyebrow. Tight dark leather with segmented shoulder and forearm
plates; half-cape with the HOOD DOWN so the whole face is visible; purple
ribbons at the wrists hanging still. TWO CURVED TWIN BLADES sheathed crossed at
the small of the back — visible in every view, never removed. Flat soft boots.
No smoke, no particles, no glow haze in frame.
```

- **FRENTE** — vista principal; as lâminas aparecem pelas pontas dos cabos
  saindo dos flancos.
- **PERFIL ESQUERDO 90° VERDADEIRO** — a vista que informa **quanto as lâminas
  projetam para trás** e a espessura da meia-capa.
- **COSTAS** — **a vista mais importante dela**: é onde as duas lâminas cruzadas
  na lombar são o assunto.
- **TRÊS-QUARTOS REAL** — julgamento apenas.

## 5. Peças-assinatura isoladas (gerações extras, 2D)

```
1) ONE curved twin blade with its sheath, isolated on plain dark grey: flat
   side view, edge-on view, and the sheath alone. Dark metal, a shallow curve,
   a wrapped grip, a small purple rune inlaid near the guard.
2) The runic tattoos: arms, collarbones and thighs isolated on plain dark grey,
   TWO versions — one flat purple line pattern with NO glow, one lit. Do not
   bake strong glow into the albedo.
```

## 6. Negative prompt

**BLOCO C** + os extras dela:

```
no blades, unarmed, blades missing from the back, single sword, greatsword,
bow, gun, hood up, face in shadow, symmetrical hair, both sides shaved, both
sides braided, dark skin without the grey slate tone, green skin, pupils,
coloured eyes, glowing tattoos burned into the base colour, heavy plate armour,
robe, dress, sexualized outfit, exposed midriff, high heels, spider motif,
web motif
```

## 7. Critérios de aprovação (reprova se qualquer um falhar)

1. **AS DUAS LÂMINAS GÊMEAS CURVAS, cruzadas na lombar, nas quatro vistas.** É
   o defeito medido no lote de 24/08 e a razão desta regeração. Uma lâmina só,
   ou nenhuma, reprova.
2. **Cabelo assimétrico:** raspado de um lado, trança do outro.
3. **Tatuagens rúnicas roxas** sobre pele **cinza-ardósia** — e entregues em
   versão sem glow (§5).
4. **Capuz abaixado, rosto visível**, nas quatro vistas.
5. **Olhos brancos luminosos sem pupila** + **cicatriz na sobrancelha**.
6. **Perfil de 90° real** — um olho, uma orelha, nenhum pedaço do peito, e a
   projeção das lâminas visível de lado.
7. Teste da silhueta: toda preta, as lâminas cruzadas e o perfil do cabelo ainda
   a identificam?
