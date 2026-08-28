# texturas de detalhe

Duas imagens em tons de cinza, 512x512, geradas com ruido de valor **periodico**
(elas ladrilham sem costura). Nao sao textura de cor: sao **modulacao**. Entram
multiplicando a cor do chao, em espaco de MUNDO, e servem para uma coisa so':

> tirar a cara de plastico do terreno.

O defeito que elas curam, medido no celular em 27/08 e de novo no Unreal em
28/08: um chao pintado com cor lisa mais ruido de baixa frequencia le' como
massinha a dois metros do olho. O relevo pode estar certo e a cor pode estar
certa — sem gramatura de perto, o chao nao parece chao.

| arquivo | onde entra |
|---|---|
| `detalhe-chao.png` | areia e grama |
| `detalhe-rocha.png` | encosta e penhasco |

**Quem usa:** `pc-unreal` (material `M_IlhaFraturada`) e `mobile-godot`
(`world/toon.gdshader`, uniforms `tex_chao` / `tex_rocha`). O mobile tem a sua
propria copia dentro de `godot/world/textura/` porque a engine so' le' de
`res://` — e aquele projeto esta' congelado, entao a copia de la' nao se mexe.
