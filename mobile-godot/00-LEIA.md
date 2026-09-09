# mobile-godot — REFERÊNCIA para a reescrita em Unity

O jogo de celular como ficou em Godot 4.4.1. **12/12 autotestes verdes** na
última execução (04/09/2026). Não roda mais nesta máquina: o Godot foi
desinstalado em 09/09, quando o Diretor decidiu que o produto é Unity.

## Para que esta pasta serve

**É o que o Unity tem que alcançar.** Cada sistema aqui foi jogado no aparelho
do Diretor e ajustado por vídeo; os números foram medidos, não chutados. Quem
for reescrever um sistema em C# lê a decisão em `design/`, abre o `.gd`
correspondente aqui para ver como foi resolvido, e escreve de novo.

Comece por `godot/ARQUITETURA.md` (quem é dono do quê, contratos entre raias,
armadilhas de teste) e por `godot/core/` (Balance, Kits, Bus, Textos: os
números e os contratos).

| Sistema | Onde está |
|---|---|
| Números de jogo | `core/Balance.gd`, `core/Kits.gd` |
| Queda do castelo, planar, pouso | `gameplay/Queda.gd`, `world/Castelo.gd` |
| Zona que fecha (5 fases, fração do raio do mapa) | `gameplay/Zona.gd` |
| Dano num ponto só (NaN barrado com `not (x > 0)`) | `gameplay/Combat.gd` |
| Luvas, loot, Baú Celestial | `gameplay/Arma.gd`, `ArmaSlot.gd`, `Loot.gd`, `BauCelestial.gd` |
| Kits (passiva/tática/suprema) | `gameplay/KitRunner.gd`, `gameplay/habilidades/` |
| Bots com percepção (visto/ouvido/disparo/revide) | `gameplay/Bot.gd` |
| Derrubado e reerguer | `gameplay/Derrubado.gd` |
| Terreno reativo (fogo por orçamento, água, gelo, raio, terra, vento) | `terrain/` |
| Ilha procedural, água, sol, culling da grama | `world/` |
| Mago procedural + modelo externo `.glb` com aliases de clipe | `characters/Mage.gd` |
| HUD, gesto único de disparo, área segura | `ui/`, `gameplay/FireGesture.gd` |
| Menu, seleção dos 20, configurações | `menu/` |
| Áudio sintetizado (48 timbres) | `audio/Sfx.gd` |

## Orçamento MEDIDO (a única referência real de custo do projeto)

| | |
|---|---|
| Ilha de 600 m | 184.967 tris e 67 draw calls na tela |
| Colisão da ilha | 34.848 faces |
| Kit de cenário | 201.203 tris, 98 draw calls |
| Bots | ~0,25 ms de física por bot por passo (12 bots = 2,6 ms) |
| APK | 141 MB com os 5 modelos hero em 2K |

**FPS no aparelho nunca foi medido.** Nenhum aparelho apareceu em `adb devices`
em três semanas. É a primeira medição que o Unity deve fazer.

## O que NÃO fazer

- **Não traduzir GDScript para C#.** A reescrita lê `design/` e reimplementa.
  O que atravessa é a decisão, o número e o teste (o que cada selftest cobra).
- **Não mexer aqui.** Se algo tem que mudar no design, muda em `design/`.

## Os modelos que estão aqui e servem ao Unity

`world/modelos/*.glb` (castelo, kit da ilha) e `gameplay/modelos/*.glb` (luvas,
baú) são as versões **decimadas para celular** (800 a 30 mil tris) das peças do
Meshy. O Unity mobile usa exatamente estas. Quando `mobile-unity/` as importar,
elas mudam de pasta para `arte/` e esta pasta pode ir embora.
