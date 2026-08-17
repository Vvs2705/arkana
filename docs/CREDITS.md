# CREDITS.md — Assets e licenças do Arkana v0.1

> Regra do GDD (seções 13 e 15): **todo asset externo + licença documentados
> aqui desde o dia 1.**

## 1. Declaração — v0.1 não usa NENHUM asset externo

- **Arte:** 100% procedural, gerada em runtime com `Phaser.Graphics`
  (`src/core/textures.ts`). Zero imagens, spritesheets ou tilesets de
  terceiros. Ver `docs/ART.md`.
- **Áudio:** 100% procedural, sintetizado em runtime com WebAudio
  (`src/core/audio.ts`). Zero samples, faixas ou SFX de terceiros. Ver
  `docs/AUDIO.md`.

## 2. Fontes (Google Fonts — licença SIL Open Font License 1.1)

Servidas via `fonts.googleapis.com` (link no `index.html`). A SIL OFL permite
uso comercial, embed e redistribuição (as fontes em si não podem ser vendidas
isoladamente).

| Fonte | Uso no jogo | Licença |
|---|---|---|
| Cinzel | Logo e títulos | SIL OFL 1.1 |
| Chakra Petch | UI, HUD e números | SIL OFL 1.1 |
| Inter | Textos corridos | SIL OFL 1.1 |

## 3. Dependências de código (npm)

| Pacote | Papel | Licença |
|---|---|---|
| Phaser 3 | Engine 2D | MIT |
| Vite | Bundler / dev server | MIT |
| TypeScript | Linguagem / compilador | Apache-2.0 |

Licenças permissivas, compatíveis com distribuição comercial; os textos das
licenças acompanham cada pacote em `node_modules/`.

## 4. Processo para novos assets

Qualquer asset externo que entrar no projeto (mesmo placeholder temporário)
deve, **no mesmo commit**: (1) ganhar uma linha nesta tabela com fonte, autor e
licença; (2) ter a licença verificada arquivo a arquivo (atenção especial a
freesound.org, onde a licença varia por arquivo — GDD seção 13).
