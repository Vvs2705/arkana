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

## 2. Fontes (origem Google Fonts — licença SIL Open Font License 1.1)

**EMPACOTADAS** em `public/fonts/`, subconjunto `latin`, ~69 KB no total —
não são mais servidas por `fonts.googleapis.com`. Duas razões, e as duas
importam para a loja: era a **única chamada de rede do protótipo inteiro**
(sem ela, a ficha de Segurança de Dados da Play declara "nenhuma coleta"), e
o GDD §19.6 quer o Modo Treino Offline como produto — com fonte remota o jogo
abria em modo avião com a tipografia errada.

A SIL OFL permite uso comercial, embed e redistribuição (as fontes em si não
podem ser vendidas isoladamente). Nenhuma das três declara *Reserved Font Name*.
Corpos de licença e avisos de copyright em `public/fonts/OFL.txt`.

| Fonte | Uso no jogo | Pesos empacotados | Licença |
|---|---|---|---|
| Cinzel | Logo e títulos | 500 + 700 (arquivo variável) | SIL OFL 1.1 |
| Chakra Petch | UI, HUD e números | 400 · 700 | SIL OFL 1.1 |
| Inter | Textos corridos | 400 | SIL OFL 1.1 |

Pesos que o `index.html` declarava e o código nunca pediu (Cinzel 900, Chakra
Petch 600, Inter 600) **não** foram empacotados — peso baixado é peso que o
jogador paga na abertura.

Um portão no `vite.config.ts` quebra o `npm run build` se alguém reintroduzir
referência a CDN de fonte, ou se algum `.woff2` sumir do `dist/`.

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
