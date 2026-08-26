# PROMPTS — a pasta única de prompts do Arkana

> **Ordem do Diretor (26/08):** todo prompt solicitado vive AQUI, nesta pasta,
> agora e nos próximos pedidos. Fluxo: concept por IA de imagem → aprovação do
> Diretor → Meshy → Blender → jogo.

**O fluxo (ordem do Diretor, 26/08):** gerar a imagem de concept numa IA de
imagem → o Diretor aprova → a imagem aprovada vira referência do Meshy →
modelo 3D → refino no Blender → jogo. **Nenhum crédito Meshy antes da
aprovação da imagem.**

## As regras que valem para TODOS os arquivos desta pasta

1. **UMA figura por imagem, sempre.** Nunca gerar "folha de personagem" com
   várias vistas na mesma imagem: em 21/08 o Meshy tratou uma folha inteira
   como UM objeto e extrudou um painel plano — 30 créditos de lição. Cada
   vista (frente, lado, costas, três-quartos) é uma geração separada, e os
   prompts de vista de cada arquivo já vêm separados.
2. **Fundo neutro nas vistas para o Meshy.** Cinza-escuro liso ou gradiente
   suave. Fundo cheio de cenário confunde a fotogrametria da IA. A imagem de
   APRESENTAÇÃO (key art, para o Diretor avaliar clima) pode ter cenário — os
   arquivos separam "prompt mestre" (clima) de "vistas" (produção).
3. **A âncora de estilo abre todo prompt.** Ela é a nossa direção de arte por
   escrito — superfície pintada, sombra de contato, proporção exagerada — e
   está repetida em cada arquivo para o prompt ser autossuficiente no
   copiar-e-colar.
4. **Paleta oficial** (usar os hex no prompt quando o gerador aceitar):
   - Noite-funda `#05070F` · Noite `#0B1026` · Painel `#0E1430`
   - Ouro `#F0C75E` · Ouro fosco `#8A7336`
   - Fogo `#FF5A2A` · Água `#2AA7FF` · Terra `#A8763E` · Vento `#8FE8C9` · Raio `#F5D90A`
5. **Dimensões são LEI.** Cada arquivo traz a ficha física em metros, tirada do
   jogo quando o objeto já existe em código (ex.: o Baú mede 1,15 × 0,70 ×
   0,85 m em `BauCelestial.gd`). A imagem pode exagerar drama, o MODELO não.
6. **Negative prompt sempre.** Os geradores adoram devolver fotorrealismo,
   texto, marca d'água e membros extras. Cada arquivo traz o seu.

## Parâmetros por gerador (resumo)

| Gerador | Como usar |
|---|---|
| **Midjourney** | prompt mestre + ` --ar 16:9` (key art) ou ` --ar 3:4` (vistas) + ` --style raw --stylize 250` |
| **DALL·E / ChatGPT** | colar o prompt inteiro em linguagem corrida; pedir "wide 16:9" ou "portrait 3:4" no texto |
| **Firefly** | prompt no campo, estilo "Digital art / Painting"; desligar "Photo" |
| **SDXL/ComfyUI local** | CFG 6–7 · steps 30–40 · sampler DPM++ 2M Karras · o negative do arquivo no campo negativo |

## Os arquivos

| # | Peça | Uso no jogo |
|---|---|---|
| 01 | Castelo Voador | abertura de toda partida |
| 02 | Luva Comum | loot básico — a arma que todos acham primeiro |
| 03 | Luva de Conjurador | loot raro — o "rifle" |
| 04 | Manopla | lendária, só no Baú Celestial |
| 05 | Baú Celestial | o evento anunciado do mapa |
| 06 | Ruínas — estátuas e arcos | POI RUINS vira lore |
| 07 | Torre Arcana | landmark com função de gameplay |
| 08 | Altar de Sintonia | o monumento do pilar do jogo |
