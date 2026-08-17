# ART.md — Decisões visuais do Arkana v0.1

> Fonte da verdade: `docs/GDD.md` seção 10 (Identidade Visual).
> Regra de processo: toda decisão de design entra no GDD antes de virar código.

## 1. Paleta oficial (GDD seção 10)

| Uso | Cor | Hex |
|---|---|---|
| Base — azul-noite arcano (fundos) | ■ | `#0B1026` |
| Fundo profundo (body/canvas) | ■ | `#05070F` |
| Dourado arcano (marca, HUD, barra de carregamento, raridade lendária) | ■ | `#F0C75E` |
| Fogo | ■ | `#FF5A2A` |
| Água | ■ | `#2AA7FF` |
| Terra | ■ | `#A8763E` |
| Vento | ■ | `#8FE8C9` |
| Raio | ■ | `#F5D90A` |

No código, a paleta vive em `src/core/config.ts` (`COLORS`, `ELEMENT_COLORS`,
`SHIELD_COLORS`). Nenhuma cor de jogo é digitada inline nas cenas — sempre via
config. Cores do escudo evolutivo: branco → azul → roxo → dourado (níveis 1–4).

## 2. Regra de acessibilidade: cor + forma + ícone

Regra inegociável do GDD (seção 10): **cada elemento tem cor + ícone + forma de
projétil distintos — nunca só cor** (daltonismo).

- **Projéteis:** cada elemento tem uma textura própria (`TEX.PROJ_*`) com
  silhueta diferente (não são 5 bolinhas recoloridas).
- **Ícones:** cada elemento tem um ícone 40×40 (`TEX.ICON_*`) com símbolo e
  contorno próprios, usados no carrossel do HUD.
- **HUD:** o elemento ativo aparece grande com borda na cor do elemento; os
  demais ficam menores e acinzentados — a leitura funciona por tamanho e
  posição mesmo sem distinguir cor.
- Complemento futuro: modo daltonismo nas configurações (Protanopia /
  Deuteranopia / Tritanopia) já previsto em `settings.ts`; no v0.1 a opção
  existe e persiste, o remapeamento de paleta é trabalho do v0.2.

## 3. Arte 100% procedural no v0.1

Todo o visual do protótipo é gerado em runtime com `Phaser.Graphics` em
`src/core/textures.ts` (`generateAllTextures`) — **zero PNGs, zero assets
externos**:

- Tiles 32×32 dos biomas (grama, grama alta, terra, rocha, água, árvore) e dos
  estados reativos (queimado, congelado, lama, muro), com ruído determinístico
  para variação sem quebrar a coerência entre tiles.
- Entidades (player, bot, dummy), projéteis, ícones de elemento, Selo de
  Arkana, partículas e pixel utilitário.

Vantagens dessa escolha para a Fase 1: nenhum problema de licença, build
minúsculo, e iteração instantânea (mudar a arte = mudar código).

## 4. Tipografia (Google Fonts, licença SIL OFL)

| Uso | Fonte | Por quê (GDD seção 10) |
|---|---|---|
| Logo/títulos | **Cinzel** | serifada épica, "mágica" sem ser medieval demais |
| UI/HUD/números | **Chakra Petch** | angular, legível pequena, cara de competitivo |
| Textos corridos | **Inter** | neutra, perfeita para menus e descrições |

Carregadas via `fonts.googleapis.com` no `index.html`; pilhas com fallback de
sistema em `src/core/config.ts` (`FONTS`). Licença SIL OFL — livre para uso
comercial (ver `docs/CREDITS.md`).

## 5. O que é placeholder (a substituir por artista humano)

Conforme `equipe-arkana.md` — Parte 4 ("o que agentes NÃO substituem"): a
**arte final autoral** virá de artista humano (ou de direção de geração de arte
com curadoria dura + style guide). É placeholder declarado no v0.1:

- Todos os sprites procedurais (tiles, entidades, projéteis, ícones, selo) —
  servem para validar leitura de jogo, não para vender o jogo.
- A key art da tela de carregamento (a descrição oficial está no GDD seção 10:
  castelo voador rasgando nuvens ao entardecer, magos saltando com 5 trilhas
  de luz elementais).
- O logo/wordmark ARKANA (o "K" com relâmpago sutil do GDD ainda não existe —
  hoje é texto em Cinzel).
- Animações: o v0.1 usa tweens (flash de dano, fade, balanço do dummy) no
  lugar de spritesheets animados.

O que **não** é placeholder e deve sobreviver ao v1.0: a paleta, a regra
cor+forma+ícone, a hierarquia do HUD (vida/escudo/mana à esquerda, elementos à
direita, minimapa no topo) e o Selo de Arkana como identidade de três usos
(logo, medidor de Sintonia, ícone).
