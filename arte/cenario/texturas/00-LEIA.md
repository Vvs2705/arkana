# texturas de detalhe

Duas imagens em tons de cinza, 512x512, geradas com ruído de valor **periódico**
(elas ladrilham sem costura). Não são textura de cor: são **modulação**. Entram
multiplicando a cor do chão, em espaço de MUNDO, e servem para uma coisa só:

> tirar a cara de plástico do terreno.

O defeito que elas curam, medido no celular em 27/08 e de novo no PC em 28/08:
um chão pintado com cor lisa mais ruído de baixa frequência lê como massinha a
dois metros do olho. O relevo pode estar certo e a cor pode estar certa — sem
gramatura de perto, o chão não parece chão.

| arquivo | onde entra |
|---|---|
| `detalhe-chao.png` | areia e grama |
| `detalhe-rocha.png` | encosta e penhasco |

**Quem usa:** o material de terreno do `mobile-unity/` (Shader Graph no URP, as
duas como detail maps em coordenada de mundo). O `mobile-godot/` tem a sua
própria cópia em `godot/world/textura/` porque a engine só lê de `res://`; é
referência, não se mexe.
