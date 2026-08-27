# 05 — CORVOMANTE, o Olho Distante

> **Fila:** posição 7 (lote 3). **Custo:** ~30 créditos (+~30 se o corvo for
> gerado em 3D pela Meshy — decisão do Diretor).
> **Antes de gerar:** ler [`00-REGRA-DAS-VISTAS.md`](00-REGRA-DAS-VISTAS.md) —
> BLOCOS **A**, **B** e **C** saem de lá.
> **Fontes:** `godot/menu/Elenco.gd` (id 5) · `personagens/05-corvomante.md`.

## 0. Quem é

Humano, Vidente, Longa distância / recon. **Trocou o olho direito** por uma
pedra de obsidiana: o corvo espectral carrega o olho VIVO dele, e tudo que a ave
vê, ele vê. Enquanto ela voa, o corpo dele fica parado e indefeso. De perto,
quase não enxerga nada.

## 1. Ficha física — DIMENSÃO É LEI

- **Altura: 1,80 m.** Magro, ombros de quem lê mais do que dorme.
- **A marca mágica:** **não tem o olho direito** — no lugar, uma **pedra de
  obsidiana lisa e polida**, preenchendo a órbita, com brilho especular escuro
  (emissivo FRACO no modelo, nunca farol).
- Pele clara de biblioteca; **olheira funda no olho que sobrou**; **cabelo
  grisalho precoce nas têmporas**.
- **Vestuário:** **sobretudo de colarinho alto verde-escuro** com **penas
  costuradas por DENTRO da gola** (segredo de afeto — aparecem só quando a gola
  abre); **luneta rúnica dobrável pendurada no peito** — e ele a usa **sobre a
  OBSIDIANA**, não sobre o olho bom (ninguém sabe por quê; ele não explica);
  **luvas de escriba manchadas de tinta**.
- **DOIS modelos:** o corpo e o **corvo espectral** (~800 vértices + shader de
  energia), que orbita o ombro quando ocioso. Geração separada, §5 — **nunca na
  mesma imagem** que ele.

## 2. Paleta

| Uso | Hex |
|---|---|
| Verde arcano (dominante) | `#2E8B57` |
| Cinza-tinta (secundária) | `#4A4A50` |
| Obsidiana (acento) | `#1A1A22` |

## 3. Prompt mestre — imagem de apresentação (16:9)

Colar o **BLOCO A** e em seguida:

```
A lean human seer, 1.80 m, standing very still on a tower balcony at night,
the posture of a man who reads more than he sleeps. HIS RIGHT EYE IS GONE: the
socket is filled by a smooth polished stone of black obsidian, dark and
specular, faintly warm. The remaining left eye is human, ringed by a deep
shadow of sleeplessness; grey has come early at his temples. A dark green
high-collared overcoat, feathers stitched on the INSIDE of the collar and
glimpsed only where it opens; a folding runic monocle-scope hangs on his chest,
worn UP OVER THE OBSIDIAN, never over the good eye; ink-stained scribe's
gloves. A translucent green spectral raven wheels far away in the sky behind
him, carrying a single clear human eye. Palette #2E8B57, #4A4A50, #1A1A22.
```

## 4. As 4 vistas para o Meshy (3:4, UMA figura por imagem)

⚠️ **O corvo NÃO entra nas vistas dele.** Uma figura por imagem, sempre.

Colar **BLOCO A + BLOCO B + o texto da vista**. Descrição do corpo, idêntica
nas quatro:

```
Lean human man, 1.80 m, narrow shoulders. RIGHT EYE socket filled by a smooth
polished black obsidian stone; LEFT eye human with a deep shadow beneath it;
early grey at the temples. Dark green #2E8B57 high-collared overcoat reaching
mid-thigh, feathers stitched inside the collar; a folding runic scope hanging on
the chest by a cord, NOT held in the hand; ink-stained scribe's gloves. Plain
flat boots. No bird, no companion, no particles in frame.
```

- **FRENTE** — vista principal; a assimetria dos olhos precisa ser inegável.
- **PERFIL ESQUERDO 90° VERDADEIRO** — atenção: **este perfil mostra o lado do
  olho HUMANO**, não o da obsidiana. Gerar **também um perfil direito de 90°**
  como quinta imagem, só para o Blender saber a obsidiana de lado; no
  multi-view entra o esquerdo (padrão do lote, para todas as vistas casarem).
- **COSTAS** — a queda do sobretudo e a gola alta por trás.
- **TRÊS-QUARTOS REAL** — julgamento apenas; se sair, prefira o 3/4 do **lado
  direito**, que mostra a obsidiana em ângulo.

## 5. Peças-assinatura isoladas (gerações extras, 2D)

```
1) THE SPECTRAL RAVEN, its own set: front / TRUE 90-degree side / back / wings
   spread, one per image, plain dark grey. A raven made of translucent green
   energy, geometric feather planes, and ONE single sharp HUMAN EYE where a
   bird's eye would be — unsettling but rated 10+, no gore, no blood, no
   eyeball veins. Body about the size of a real raven.
2) The folding runic scope, isolated: closed and open, front and side.
3) The obsidian eye stone, isolated: a smooth polished black oval, front and
   side, with a faint dark specular highlight and NO emissive glow baked in.
```

## 6. Negative prompt

**BLOCO C** + os extras dele:

```
two human eyes, matching eyes, eyepatch, bandage over the eye, empty bloody
socket, gore, veins, glowing bright eye, lantern eye, scope over the left eye,
scope held in the hand, bird in frame, raven on the shoulder, feathers on the
outside of the collar, wizard hat, staff, robe, muscular build, young smooth
face, tattoos
```

## 7. Critérios de aprovação (reprova se qualquer um falhar)

1. **Olho direito = obsidiana lisa e polida**, olho esquerdo humano. Se os dois
   forem iguais — ou se houver tapa-olho ou faixa — reprovada.
2. **Sem gore na órbita:** a pedra preenche, limpa. 10+.
3. **Luneta rúnica pendurada no peito, sobre o lado da obsidiana**, nunca na
   mão.
4. **Gola alta verde-escura** com as penas **por dentro** (visíveis só na
   abertura).
5. **Luvas de escriba manchadas de tinta.**
6. **Perfil de 90° real** — um olho, uma orelha, nenhum pedaço do peito. E o
   perfil direito extra existe, para a obsidiana.
7. **Corvo (§5):** energia verde translúcida com **UM olho humano nítido**, sem
   gore. Corpo de corvo real, não águia, não corvo gigante.
