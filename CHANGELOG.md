# CHANGELOG — Arkana

Cada versão do protótipo documentada (regra do GDD, seção 15).

## v0.5.0-roblox — 2026-08-19 — R11: tela mais limpa, tutorial opcional e o Android de volta

Três ordens do Diretor, e a frente mobile retomada.

**"Informação demais na tela".** A raia mediu antes de cortar: **65 004 px² permanentes
= 8,9% de um celular em paisagem** — e, pior que a área, **72 px de mobília girando na
coluna da mira** (o Selo de Arkana, em 3ª pessoa, bem onde o jogador aponta). O Selo
some no modo ESSENCIAL porque suas duas informações eram **100% duplicadas**: "combo
disponível" já é dito pela pílula com **palavra + losango** (canal mais acessível que
gema colorida) e "meu elemento" pelo slot do carrossel, permanente em cor+forma, onde o
dedo já está. Nome do parceiro e `FASE x/y` viraram sob demanda — texto que nunca muda
durante um tiroteio não paga o espaço que ocupa; o nome volta **no instante em que ele
cai**, que é a única hora em que decide algo. **Permanente depois: 55 140 px², 15% menos**,
coluna da mira zerada. Preferência `hudDensity` (ESSENCIAL padrão × COMPLETO), persistida
e trocável ao vivo — a queixa foi de excesso, então o excesso é que virou opt-in.

**"Tutorial não é obrigatório."** Ele era pior que obrigatório-com-botão-de-pular: o
`Main` o empurrava em todo corpo novo, **e o lobby segurava por até 240 s** enquanto
alguém treinava — quem nem escolheu o tutorial esperava por quem escolheu. Agora é
CONVITE (`Tutorial.offer`): duas saídas do mesmo peso, a sessão só abre com aceite, e
recusar não é porta de mão única (o botão TREINAR volta no lobby, no mesmo slot do PULAR,
sem mobília nova). **O teto de 240 s deixou de existir** — o conserto não era mexer no
número, era não ter espera — e um assert trava a volta dele. Quem entra no minuto 4 de
uma prova não perde o convite: ele fica pendente e sai no lobby seguinte.

**O cenário S1 trocou de pergunta, não foi apagado.** Ele cobrava que o lobby SEGURASSE
— exatamente a regra derrubada. Mas o defeito que ele existe para pegar continua
possível: o perigo nunca foi "a partida começa", foi a **sessão do onboarding ficar
órfã**, com o jogador preso num Campo de Provas que o `startMatch` já desmontou. O S1
agora cobra: ninguém entra sem aceitar · o aceite chega pelo payload REAL do cliente ·
o lobby não espera · e quem treinava sai limpo. Provado com dois defeitos reintroduzidos.

### Android — a frente mobile saiu do papel, com prova
**O APK compila.** Debug 4,4 MB, release 3,4 MB e **AAB 3,3 MB** (o formato que a Play
exige de app novo), build frio em 59 s, `typecheck` limpo. O `docs/BUILD_ANDROID.md`
estava errado em cinco pontos: dizia que "falta o SDK para compilar" (compila), mandava
instalar **JDK 17** quando o projeto **exige 21**, pedia `platforms;android-34` com
`compileSdk` 36, e instruía a referenciar um bloco `signingConfigs` **que não existia**.
Agora existe, lê `keystore.properties` (fora do versionamento, com `.gitignore` cobrindo
`*.jks`/`*.keystore`/`keystore.properties`) e o release compila sem chave em vez de travar.

Armadilha documentada: `bundleRelease` termina com `BUILD SUCCESSFUL` e o `.aab` sai
**não assinado** — a Play recusa. A fiação de assinatura foi provada apontando para a
keystore de *debug* do próprio SDK (`apksigner verify` → `Verifies`), e depois apagada.
**Nenhuma chave existe no repositório.** `versionName` era `"1.0"` (default do Capacitor,
anunciando protótipo como versão final) → `0.1.0`.

**`docs/ANDROID.md`** (novo): qual produto vai para a loja, o que falta para publicar
(existe/falta/não verificado) e as diretrizes que ESTE jogo precisa cumprir — ECA Digital,
zero caixa aleatória, classificação etária.

### Decisões que ficaram para o Diretor (não tomadas aqui)
- **O gesto único não pode atravessar para o Android.** O GDD §19.3 ainda especifica
  DOIS gestos ("mirar arrastando + botões"); o gesto único nasceu da reprovação do teste
  de APK e vive no Roblox e no PRISMA. Pela regra travada do §9 ("nada técnico migra do
  Roblox — o GDD é a única ponte"), ele precisa ser **promovido ao §19.3** para valer.
- **Não está decidido qual build Android vem primeiro**: §9 aponta first-person; a escada
  do §8 põe o mini-BR top-down (Degrau 3) antes; o PRISMA está "pendente de 1 palavra".
- **Antecipar o Android contraria o §9**, que trava o início para depois da validação do
  Roblox — e nenhuma das 5 perguntas foi respondida com jogador real.
- **Público-alvo etário nunca foi declarado** em documento nenhum — sem isso não há
  questionário IARC nem política de dados de menores.

Gates: 40 arquivos Luau sem erro · `rojo build` limpo (852 KB) · harness **48/48** ·
APK/AAB compilados e verificados. **Nenhum número de `Balance` foi tocado.**

## v0.4.0-roblox — 2026-08-18 — R10: a medição que faltava para o playtest valer

O produto existe para responder **cinco perguntas** (`docs/ROBLOX.md` §1). Esta fase
foi auditar se ele consegue — e a resposta era não.

**A V5 ("o combate segura o jogador até a próxima prova?") não tinha uma linha de
instrumentação**, e `Events.Name.SessionEnd` estava declarado no schema desde a R5
**sem nunca ter sido emitido**. Agora há sessão que ATRAVESSA partidas (o único
recorte que `matchStart`/`matchEnd` não zeram): quanto tempo ficou, quantas provas
jogou, e **em que ponto desistiu** — lobby, onboarding, meio da prova ou depois
dela. São quatro consertos diferentes, e por isso não viram um número só.

**`device_tier` saía "unknown" por um bug, não por falta de feature.** A pendência
dizia "falta o cliente reportar plataforma"; o cliente reporta desde sempre
(`Device.luau` dispara `Net.Names.DeviceInfo`) e **nunca existiu handler no
servidor**. O dado era enviado e jogado fora — por duas fases, com o diagnóstico
errado registrado. Fiado, e o portão agora dispara o remote de verdade e reprova se
a ponta a ponta abrir.

**O disparo passou a carimbar o gesto** (`drag` / `tap` / `key`) — a régua do
requisito de celular do §4, que reprovou no teste do APK 2D e até aqui não tinha
como ser medido. Viaja como campo do `spell_cast` que já existia: é telemetria
pura, e um remote a mais seria superfície de ataque a mais num lugar público.

**O relatório passou a responder as cinco perguntas por nome**, com o número e a
decisão que ele muda. Amostra pequena sai como `n=4, NÃO CONCLUI` em vez de
percentil de anedota.

### A régua do TTK estava contraditória — e o relatório afirmava sucesso contra a mais frouxa
`docs/GDD.md:168`, fonte da verdade, define **TTK ~1,5–2,5s**. A varredura media
contra **2,75–3,5s** ("alvo da auditoria") e vinha concluindo **"5/5 DENTRO do
alvo" desde a R7** — contra a régua do GDD, nenhum dos cinco está dentro. As duas
réguas são legítimas e ninguém as reconciliou. **Qual vale é decisão do Diretor**:
os relatórios passaram a mostrar as duas, com a origem no nome, e a avisar quando
discordam. Nenhum número de `Balance` foi tocado.

### Também nesta fase
- **Preferências atravessam o rejoin** (`server/Prefs.luau`): as 11 opções de
  conforto morriam na saída — quem baixava o volume levava o susto de novo a cada
  entrada. O servidor guarda só o TIPO de cada chave, nunca o valor: os padrões
  continuam num lugar só e não há dois números para divergir. Zero escritas durante
  a partida (uma por sessão, na saída, mais `BindToClose`).
- **`Playtest.luau` parou de mentir**: o cabeçalho dizia que não dava para encerrar
  partida em curso e o módulo empurrava o lobby escrevendo em `Balance.match`.
  `Match.forceStart/forceEnd` existem desde a R8 — agora `P.duo()` chama o Match, há
  **`P.stop()`**, e a ferramenta do Diretor não escreve mais em balanceamento.
- **Protocolo do playtest alfa** (`docs/ROBLOX.md` §11): arranjo, duração, o que o
  Diretor NÃO pode dizer (a V1 é "a dupla aperta COMBO sem você mandar"), como o
  relatório chega num lugar publicado, limiar de "bom" por pergunta e as
  contradições encontradas entre documento e código.
- **`Discovery` era coletado e nunca impresso** — a melhor proxy de "descobriu a
  química sozinho" (V3) estava invisível. E a distribuição de dano por jogador
  humano entrou no relatório: sem ela o gatilho pós-playtest do escudo era
  inacionável justamente quando houvesse dado humano.

### Continua aberto, declarado no próprio relatório
`Events.Name.TerrainTacticalOutcome` está no schema e é consumido, mas **ninguém o
emite** — a V3 fica sem métrica de DESFECHO e sobra a descoberta do Grimório como
proxy honesta. E o funil da V1 é contaminado pelo tutorial, que usa o ping de
verdade: a mitigação é ter todo mundo dentro antes da largada.

Gates: 40 arquivos Luau sem erro · `rojo build` limpo (822 KB) · harness **46/46**.
**Nenhum número de `Balance` foi tocado.**

## v0.3.0-roblox — 2026-08-18 — R9: robustez com teste, anti-griefing e a tela do estranho

Fase de **preparo para o alpha público**. Nada de feature nova: o alvo foi o que
quebra quando o jogador é um desconhecido e o servidor é de verdade.

**Os 6 travamentos da R8 viraram teste** (`roblox/tools/scenarios.luau`) — eles
estavam corrigidos e sem uma linha de cobertura, ou seja, a próxima fase podia
reintroduzir qualquer um em silêncio. S1 tutorial atropelado pela largada · S2
spam de arrepio · S3 bot preso em geometria · S4 servidor esvazia (seguia
`Playing` com 8 bots para plateia nenhuma) · S5 `Playing` eterno (a fase final da
zona PARA e segura; o teto tem de **derivar** do cronograma, e o teste separa
"tem um número grande" de "acompanha a variante de ritmo") · S6 `forceStart` de
`Ended` vazando escudo e cooldown. Cada cenário foi validado **reintroduzindo o
defeito**: 10 mutações, 10 vermelhos. Portão do projeto: 35 → **43 verificações**.

**Griefing entre estranhos pareados em dupla** — mesma razão que desligou o fogo
amigo na R8 (um desconhecido arruína a sessão do outro e a pergunta V1 fica
ilegível):
- A canalização de Sintonia só é cancelável morrendo ou saindo, e o cooldown
  compartilhado é cobrado no INÍCIO — então bastava o parceiro morrer de
  propósito para tirar 24 s do pilar do jogo do outro. Agora a interrupção
  **devolve o cooldown a quem continua vivo**; quem caiu ou saiu segue pagando.
  O limitador do GDD §9 fica inteiro: **o combo que DISPARA continua cobrando os
  dois** — é esse que impede o spam. (Corrige a regra como descrita em R2.)
- O muro de Terra nascia sem olhar quem estava de pé na célula: dava para
  enterrar o parceiro dentro de 10 studs de pedra por 20 s. Muro não sobe em
  célula ocupada — regra uniforme, porque o GDD §14 define o muro como
  **cobertura destrutível**, não como botão de deletar alguém.

**A primeira impressão de quem entra no meio da prova.** Num servidor público
essa é a experiência da maioria, e ela estava muda: o jogador nascia espectador a
120 studs do chão sem uma palavra na tela. Agora há três estados distintos —
jogando · **espírito** (fora da contagem, não da partida: ainda volta pelo altar)
· espectador — e quem chegou agora lê "você entra na próxima" em vez de "você
está fora". Sem relógio de próxima partida: o teto de duração é teto, não
previsão, e erraria por minutos; a tela mostra fase da zona e times restantes,
que são exatos.

**Volume.** O jogo saiu do silêncio na R8 mas o jogador não tinha como baixá-lo —
`Audio.setVolume` era loja órfã, sem controle na tela. A preferência passou a
morar no `Accessibility` (uma loja só; o `Audio` apenas aplica) com dois
controles em opções. Zerar o volume liga as legendas dos avisos: mudo não pode
custar informação que só existe no som.

**Laje das ruínas e lodo ganharam cor própria** (`Slab`/`Silt`): os dois POIs mais
característicos da arena se pintavam com a cor da estrada e a do barro comum. As
regras são as mesmas — inclusive "Água + terra = lamaçal" (§14) no lodo, que é a
jogada da casa da baixada e agora tem teste próprio.

Gates: 39 arquivos Luau sem erro de sintaxe · `rojo build` limpo · harness
**43/43** · varredura de 12 partidas sem regressão (TTK 5/5 no alvo, régua de
dano/mana 1,17× contra teto de 1,25×). **Nenhum número de `Balance` foi tocado.**

## v0.2.0-roblox — 2026-08-18 — "Campo de Provas" no Roblox (R0–R3)

Primeira versão da vertente **Roblox** (produto de validação — GDD §9 "Decisões
travadas", plano em `docs/ROBLOX.md`). Terceira pessoa, estética blocky
(referência Pixel Gun 3D), servidor autoritativo. Código em `roblox/`,
sincronizado por **Rojo**; `rojo build` gera o arquivo do lugar.

**R0 · Fundação** — projeto Rojo, `Shared/Balance` (porte fiel dos números do
protótipo 2D + conversão px→studs), `Shared/Elements` (5 elementos com cor +
FORMA e a matriz dos 10 combos), `Shared/Grid` (materiais imutáveis, estados
mutáveis) e `Shared/Net` (remotes com sanitização — o cliente manda intenção,
nunca dano).

**R1 · Os pilares**
- Arena 60×60 determinística: lago de 205 células contíguas, floresta com
  densidade **acima do limiar de percolação** (238 de 282 árvores conectadas —
  abaixo disso o incêndio morre em bolsões e o pilar do GDD §14 não acontece).
- Terreno reativo completo: fogo propaga e derruba a copa, lago **congela
  erguendo a lâmina até o chão** (ponte literal), raio eletrocuta toda a água
  conectada, terra ergue muro, água+terra vira lamaçal.
- Combate 100% autoritativo: projéteis com tempo de viagem (zero hitscan),
  mana/cooldown/dano decididos no servidor, Escudo Evolutivo 1→4, i-frames.
- **Controles com mira e disparo em UM gesto** (correção da reprovação do teste
  em aparelho real): arrasta do botão da magia e solta; toque curto atira
  rápido; voltar ao centro cancela. Carrossel de elementos com alvos de 58dp.
- HUD com nível de escudo em número **e** pips (acessibilidade além da cor),
  Selo de Arkana, VFX blocky, números de dano e i18n PT-BR/EN.

**R2 · Sintonia (o pilar de inovação, GDD §9)** — os 10 combos com dano em área
e reação de terreno coerente; janela de 1,5s, canalização interrompível de 1s e
**cooldown compartilhado cobrado no início** (combo interrompido não devolve o
custo — é o que impede spam; **revisto na R9**: quem continua VIVO é reembolsado,
senão o parceiro tira 24 s do outro de graça — o combo que dispara segue cobrando
os dois, que é onde o anti-spam realmente mora); Ping de Sintonia (GDD §18.7) para combinar sem
microfone; mana dos dois conjuradores drenada.

**R3 · Loop de battle royale** — `Lobby → Playing → Ended`, zona arcana em 5
fases com dano crescente, eliminações com crédito de abate, **bots de
preenchimento** montados em blocos por código (FSM do protótipo: vagar,
perseguir, atacar, fugir do fogo) e **Selo do Campeão** com abates, dano,
combos e elementos usados.

### Verificação

`rojo build` limpo e **19 arquivos Luau sem erro de sintaxe** (`luau-compile`).
Autotestes embutidos: `Combat.selfTest()`, `Sintonia.selfTest()`,
`Match.selfTest()`. **Playtest é humano** — nada foi jogado ainda.

### Limitações conhecidas

- Sem times: dois jogadores hostis podem disparar um combo entre si (queima o
  cooldown de ambos). Resolve-se quando houver squads.
- Atordoamento dos combos elétricos pendente (exige `Combat.applyStatus`).
- Bots limitados a 11 até haver medição de performance em aparelho fraco.
- Jogador eliminado ainda consegue conjurar do poleiro de espectador.
- Fontes Cinzel/Chakra Petch do GDD §10 exigem upload pelo Diretor.

## v0.1.2 — 2026-08-17 — PRISMA-1: fundação de render

Primeira leva do Projeto Prisma (docs/PROJETO_PRISMA.md §3), executada em
4 raias paralelas sobre o contrato `RenderModule` (`src/render/contract.ts`),
fiado na ArenaScene. Só a camada de apresentação mudou — simulação,
balanceamento e contratos intactos.

1. **Luz & pós-processamento** (`src/render/lighting.ts`) — entardecer arcano
   (Light2D + luz ambiente), luzes dinâmicas com pool por distância (projéteis
   na cor do elemento, fogo/eletricidade pulsantes, cajado dourado com
   flicker), Bloom + vinheta + color grading na câmera. No-op gracioso em
   Canvas; reage à mudança de qualidade ao vivo.
2. **Pele do terreno** (`src/render/terrainTextures.ts` + `terrainSkin.ts` +
   TerrainGrid visual) — autotiling por bitmask com bordas onduladas
   (água>areia>terra>grama), 4 variantes por tile a 64px (RT 2×, mundo lógico
   intacto), decals determinísticos, água com ondulação e espuma na borda
   (gelo congela a animação — estado legível), praia em terra vizinha de água.
3. **Personagens vivos** (`src/render/characterRig.ts` + `characterTextures.ts`
   + entidades) — sprites 64px, idle respirando, bob de passo, inclinação na
   direção do movimento, manto em 2 segmentos com inércia, sombra elíptica,
   squash & stretch na esquiva; identidade de elemento dos bots preservada.
4. **VFX & juice** (`src/render/vfx.ts` + `vfxTextures.ts` + Projectile
   visual) — trilhas por elemento (cor+forma), impacto com onda de choque +
   flash + hitstop (40–60ms com cooldown) + recuo de câmera, fogo do terreno
   em 3 camadas (chama + brasas + fumaça), pólen ambiental, screen shake por
   trauma decaindo.

Tudo com gate pela config de Qualidade (Baixa/Média/Alta) — antecipando o
PRISMA-4 (mobile). Validado em runtime: WebGL, 10 luzes ativas, 3
post-pipelines, zero texturas ausentes, zero erros de console.

### Limitações conhecidas

- RT do terreno usa 3840×3840px — GPUs com `MAX_TEXTURE_SIZE < 4096`
  precisarão de fallback 1× (PRISMA-4).
- Teto de 10 luzes simultâneas (default do Phaser) — subir exige config em
  `main.ts` (PRISMA-4).
- Feel do hitstop/shake e densidade do fogo merecem ajuste fino com controle
  humano (sondas validam presença e orçamento, não gosto).

## v0.1.1 — 2026-08-17 — Fase 2: pipeline Android + controles de toque

Entrega o escopo da Fase 2 (GDD seções 19.3–19.5), executada em 2 raias paralelas:

1. **Pipeline web→APK (Capacitor 8.5)** — `capacitor.config.ts`
   (`br.com.vstack.arkana`), projeto nativo em `android/` com orientação
   landscape travada e tela cheia, script `npm run build:android` e guia
   completo de compilação em `docs/BUILD_ANDROID.md`.
2. **Controles de toque (GDD 19.3)** — `src/ui/TouchControls.ts`: joystick
   virtual (zona esquerda), mira por arrasto (zona direita), botões de
   Ataque (segurar = fogo contínuo), Tática e Esquiva com cooldown desenhado
   no próprio botão, carrossel dos 5 elementos (cor + forma), botão de pausa
   e alvos ≥ 48dp.
3. **Dois esquemas de mira** — Simples (assistida por cone) e Avançado
   (manual), escolhíveis em Configurações › Controles.
4. **Layout editável e escalável** — slider de escala (0.7–1.5) e modo
   "Editar layout" (`src/scenes/TouchLayoutScene.ts`) com arrastar-e-soltar,
   persistência e "Restaurar padrão".
5. **Ativação Auto/Ligado/Desligado** — auto-detecção por `maxTouchPoints`;
   "Ligado" permite testar toque no desktop. Com toque desligado, o desktop
   permanece 100% intacto.
6. **HUD mobile** — painéis realocados para zonas seguras dos polegares;
   `index.html` endurecido para WebView (viewport fixo, sem long-press/zoom).

### Limitações conhecidas

- **APK não compilado nesta máquina** — falta JDK/Android SDK; o passo a
  passo está em `docs/BUILD_ANDROID.md` (o build é 1 comando após instalar).
- Mudanças de modo/escala/layout de toque aplicam ao (re)entrar na arena.
- Ergonomia real dos polegares deve ser conferida no aparelho físico.

## v0.1.0 — 2026-08-17 — Protótipo "Campo de Provas" (Fase 1)

Primeira versão jogável. Entrega o Definition of Done da seção 15 do GDD:

1. **Boot completo** — splash do estúdio → carregamento (key art + logo +
   barra dourada + dicas rotativas) → título → menu.
2. **Menu principal** navegável com música gerativa e fundo animado
   (partículas elementais + Selo de Arkana).
3. **Configurações funcionais e persistidas** — todas as abas da seção 12
   (Vídeo, Áudio, Controles com remapeamento, Jogo com PT-BR/EN), com
   "Restaurar padrão" por aba.
4. **Arena top-down 60×60 tiles** com biomas: lago, floresta, grama alta,
   rochas e campo aberto.
5. **1 mago jogável (Evocador)** — WASD + mira no mouse, ataque básico (M1),
   magia tática (M2) e esquiva com i-frames curtos (ESPAÇO).
6. **5 elementos no Q** — Fogo, Água, Terra, Vento e Raio, cada um com
   projétil, cor, ícone e som próprios (regra de acessibilidade cor+forma).
7. **Terreno reativo** — árvore queima e propaga; lago congela (ponte) e
   eletrocuta com Raio; água + terra = lamaçal; Terra ergue muro de pedra
   destrutível (tabela da seção 14).
8. **Escudo de Magia Evolutivo** — dano causado acumula e sobe o nível
   (1→4, branco→azul→roxo→dourado) com banner e flash no HUD.
9. **5 bots de treino + 1 dummy** — bots com FSM (vagam, perseguem, atacam
   com erro de mira e FOGEM do fogo — reagem ao terreno como o player);
   dummy imóvel com números de dano e janelinha de DPS que reseta sozinha.
10. **HUD completo** — vida, escudo (com progresso de evolução), mana,
    carrossel do elemento ativo + cooldowns de tática/esquiva, minimapa com
    terreno e entidades, Selo de Sintonia decorativo e números de dano.
11. **Pausa (ESC) + contador de FPS** (ligável nas configurações de vídeo).

### Limitações conhecidas

- **Settings em `localStorage`** — equivalente web do `settings.json`; o
  arquivo real chega no empacotamento Tauri da Fase 2.
- **VSync e resolução** efetivamente controlados pelo navegador; as opções
  existem e persistem, mas o navegador tem a palavra final.
- **Sintonia decorativa** — o Selo no topo do HUD é placeholder do medidor
  real de Conjuração Combinada (v0.2, com 1 bot aliado).
- **Suprema reservada** — a tecla R está mapeada nas configurações, mas a
  magia suprema é escopo do v0.2.

### Fora do escopo (anotado para o v0.2)

Sintonia jogável, queda do castelo, multiplayer, cosméticos e passe — ver GDD
seção 15 ("Fora do escopo do v0.1").
