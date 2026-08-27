# 00 — REGRA DAS VISTAS (a lição do ângulo, escrita UMA vez)

> **Leia este arquivo antes de gerar qualquer vista de personagem.** Os 18
> arquivos de mago desta pasta referenciam os três blocos daqui em vez de
> repetir o texto longo. Nada aqui é opinião: é defeito medido.

---

## 1. O defeito medido (por que este arquivo existe)

Auditoria de 25/08/2026, registrada em `personagens/00-LEIA.md`, `docs/ART.md`
e na pendência 1.2 de `docs/PROJETO.md`:

- A `vista-3-4.png` do lote de 24/08 é **a frontal repetida** — **12 de 12**
  personagens conferidos.
- A `vista-lateral.png` é um **três-quartos de ~60–70°**, não um perfil.
- **Não existe perfil de 90° verdadeiro em nenhum dos 20 magos.**

**A consequência prática:** o `multi-image-to-3d` da Meshy usa o perfil
justamente para resolver a **espessura lateral** do corpo — peito, barriga,
curva das costas, nádega, calcanhar. Sem perfil verdadeiro, mandar quatro
imagens entrega **dois ângulos úteis** e o modelo nasce achatado ou inflado no
lugar errado. Mandar as quatro assim não melhora nada: só gasta crédito
(`docs/MESHY.md` §3).

**A prova pelo lado certo:** a Pyra recriada em 26/08 — a peça que o Diretor
aceitou levar ao veredito — foi gerada com **frente + perfil esquerdo
VERDADEIRO + costas**, e com a **"3/4" excluída de propósito**, porque a 3/4
disponível era a frontal repetida e ensinaria o perfil errado
(`docs/PROJETO.md`, leva 4).

**A regra, então:**

1. **Uma figura por imagem, sempre.** Nunca folha de personagem com várias
   vistas na mesma imagem — em 21/08 a Meshy tratou uma folha inteira como UM
   objeto e extrudou um painel plano (1,9 de largura × 0,05 de profundidade),
   e o rigging morreu com "pose estimation failed". Lição de 30 créditos.
2. **O perfil é de 90° ou não é perfil.** O critério de reprovação é objetivo:
   se dá para ver os dois olhos, os dois ombros ou qualquer pedaço do peito, a
   imagem está errada e é **regerada em 2D** (barato) — nunca compensada com
   crédito de 3D (loteria de 30).
3. **A 3/4 é vista de julgamento, não de produção.** Ela entra no multi-view
   **só** se for comprovadamente distinta da frontal. Em dúvida, manda três:
   frente + perfil + costas. Foi assim que a Pyra passou.
4. **Ordem no Meshy 7:** frente (é tratada como vista principal) → perfil →
   costas → 3/4 se aprovada (`docs/pipeline-arte/PERSONAGENS/02-REFERENCIAS_MULTI_VIEW.md`).

---

## 2. BLOCO A — ÂNCORA DE ESTILO (colar no início de TODA geração)

Esta é a direção escolhida para os 18 (a justificativa está em
`00-FILA.md` §3). Copiar em inglês, verbatim:

```
Premium stylized 3D game character concept for a fantasy battle royale.
SCULPTED surface, not painted illustration: readable planes, chiselled forms,
simplified premium materials, clean shape language. Controlled exaggeration —
slightly larger head, hands, boots and shoulder shapes; expressive eyes;
strong silhouette; a few large iconic costume shapes instead of many small
straps, seams and buckles. The character STANDS ON THE GROUND with a firm
contact shadow under the feet. Mature heroic tone. Deep night-blue and gold
world palette (#0B1026, #F0C75E) behind the character's own colours. NOT
photorealistic, NOT anime, NOT manga, NOT chibi, NOT cel-shaded outlines, NOT
a painted illustration.
```

⚠️ **Nunca escrever "less realism" / "menos realismo" no prompt.** Está medido
em `docs/ART.md`: essa instrução empurra o gerador para **anime** — o grupo que
mais quebra a coerência do elenco. O eixo é **superfície esculpida × pintada**,
sombra de contato e proporção exagerada. O rosto do Brok (aprovado) é MAIS
realista que o da Pyra (reprovada).

---

## 3. BLOCO B — ENQUADRAMENTO DE VISTA (colar depois do BLOCO A, nas 4 vistas)

```
Single character centered, full body entirely in frame from the top of the
head to the soles of the feet, nothing cropped. Neutral A-pose: arms lowered
about 45 degrees away from the torso, hands open, palms facing the thighs,
legs slightly apart, head level and looking straight ahead. Plain flat dark
grey background, even soft studio lighting, no cast shadow on the background,
orthographic or long focal length camera, no perspective distortion, no lens
blur, no vignette. Same character, same apparent height, same costume, same
hair, same props, same materials and same palette in every view.
```

### As quatro vistas — o texto de cada uma

**FRENTE** (é a vista principal para o Meshy):

```
FRONT VIEW, camera exactly at chest height, perfectly frontal: both shoulders
equally wide, face symmetrical, both feet equally visible.
```

**PERFIL ESQUERDO — o que este documento existe para corrigir:**

```
TRUE 90-DEGREE LEFT SIDE VIEW. The camera is exactly PERPENDICULAR to the
character's shoulders. Only the LEFT side of the body is visible: the right
arm and right leg are hidden directly behind the left ones, the nose points at
the left edge of the frame, ONE eye only, ONE ear only, and NO part of the
chest and NO part of the far shoulder is visible. This is NOT a three-quarter
view, NOT a 60-degree view, NOT a 70-degree view, NOT a front view. If both
eyes or both shoulders can be seen, the image is WRONG. Show the true LATERAL
THICKNESS of the body: chest depth, belly line, curve of the back, buttock and
heel profile, and the thickness of every prop seen edge-on.
```

**COSTAS:**

```
BACK VIEW, camera exactly behind the character, perfectly aligned: both
shoulder blades equally visible, no part of the face visible, hair and cape
and back-mounted equipment fully readable.
```

**TRÊS-QUARTOS REAL** (vista de julgamento — ver §1 regra 3):

```
TRUE THREE-QUARTER VIEW, camera at 45 degrees between the front and the LEFT
profile: both shoulders visible but the far one clearly NARROWER, the nose
turned 45 degrees away from the camera, the far cheek partially hidden by the
bridge of the nose, the far leg partially occluded by the near one. It must
NOT look like the front view — if the face reads symmetrical, the image is
WRONG.
```

---

## 4. BLOCO C — NEGATIVE PROMPT BASE (vale para os 18; cada arquivo soma os seus)

```
character sheet, turnaround sheet, multiple views in one image, front and side
and back in the same frame, side-by-side figures, colour swatches, palette
bar, inset detail panels, labels, arrows, text, letters, numbers, watermark,
signature, UI, HUD, logo, frame, border, three-quarter view instead of a true
profile, anime, manga, manhwa, anime face, big glossy anime eyes, cel-shaded
black outlines, chibi, painted illustration brushwork, oil painting texture,
photorealistic, photo, skin pores, microscopic fabric noise, cinematic
dramatic pose, action pose, running, jumping, flying pose, motion blur, depth
of field, low camera angle, high camera angle, cropped head, cropped face,
cropped hands, cropped feet, cut off limbs, extra limbs, extra arms, extra
fingers, fused fingers, two heads, malformed hands, blurry, dark-on-dark
unreadable layering, floating character, no contact shadow, cape covering the
whole body, particles or smoke hiding the body, weapon covering the torso,
mannequin, doll joints, cross, crucifix, rosary, halo, religious symbol,
real-world brand or trademark, gore, blood, open wounds, nudity, sexualized
outfit
```

---

## 5. O portão antes de gastar crédito (por mago)

Da `docs/design/DESBLOQUEIO-ELENCO.md` §4.1, na ordem:

1. **Auditar as 4 vistas geradas** contra os critérios de aprovação do arquivo
   do mago. Vista reprovada → regerar em 2D. Só vista aprovada entra no 3D.
2. **Geração multi-view no SITE, na conta do Diretor** — Meshy 7 + Ultra +
   Textura + Pose A-Pose + licença PRIVADO (a receita da Pyra, ~30 créditos).
3. **Revisar NO VIEWER antes de baixar** — regra nascida de correção do
   próprio Diretor em 26/08. Zoom em mãos, rosto e adereço-assinatura.
4. Aprovado → remesh ~15k (grátis) → rig **na altura da ficha** → clipes da
   biblioteca do Brok → fusão → selftest de characters.

**Peça-assinatura tem geração isolada própria** (manopla, cajado, arco, fole,
corvo, máscara, mochila, totem): o adereço-assinatura já sumiu uma vez na
Pyra (`docs/MESHY.md` §4). Cada arquivo de mago traz a linha da peça dele.

**Detalhe 2D que a geração perder duas vezes vira decal no Blender**
(determinístico, grátis), não terceira geração paga.
