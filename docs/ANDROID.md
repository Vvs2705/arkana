# ANDROID — a rota da frente mobile do Arkana

> Documento de produto da **frente Android**, escrito em 19/08/2026 a pedido do
> Diretor, que decidiu **antecipar** esta frente: *"as mecânicas sendo validadas
> e sendo funcional vamos começar a desenvolver no android para poder jogar e um
> dia vender na Apple Store e Play Store, e vamos fazer conforme as diretrizes"*.
>
> **Regra deste documento:** toda afirmação sobre o estado do projeto aponta o
> arquivo onde é verificável. Onde não deu para verificar, está escrito
> **não verificado**. Onde a regra é de loja e pode ter mudado, está escrito
> **[verificar na política vigente]** — política de loja muda, e documento errado
> aqui custa reprovação de submissão, não uma linha de código.
>
> **Fronteira dura:** nenhum agente cria conta, faz login, aceita termos, envia
> build ou publica em loja nenhuma. Este documento descreve o passo; **quem
> executa é o Diretor** — a mesma regra que o `ROBLOX.md` §7 já aplica à
> publicação no Roblox.
>
> Este documento **não altera o GDD**. Onde ele encontra contradição com a fonte
> da verdade, ele aponta (§7) e para por aí.

---

## 1. Qual produto vai para a loja

Existem dois candidatos, e eles não estão no mesmo estágio.

| | **Candidato A — protótipo 2D** | **Candidato B — premium first-person** |
|---|---|---|
| O que é | Phaser 3 + TypeScript + Vite empacotado com Capacitor 8.5 | A visão declarada do GDD §9: first-person Android, régua Spell Arena (PRISMA §1) |
| Estado no repositório | **compila hoje** — `android/app/build/outputs/apk/debug/app-debug.apk`, 4.569.590 bytes, gerado em 19/08/2026 12:51 | **não existe** — zero linha de código, e a rota de engine ainda está aberta (PRISMA §2: *"pendente de 1 palavra"*; PRISMA §9 lista as fases 3D-0…3D-5 como não iniciadas) |
| Conteúdo de jogo | arena top-down contra bots, 5 elementos, terreno reativo, escudo evolutivo (CHANGELOG v0.1.0) | — |
| O que falta de jogo | Sintonia (o pilar) é **decorativa** no 2D — CHANGELOG v0.1.0: *"o Selo no topo do HUD é placeholder"*; não há multiplayer; não há queda do castelo | tudo |
| Defeitos conhecidos | os dois que o próprio Diretor reprovou em 17/08 (PRISMA §0) **continuam no código**: `src/ui/TouchControls.ts` ainda é *"arrasto no lado direito (mira)"* + botão de Ataque separado — dois gestos. O gesto único existe só no Roblox (PROJETO.md §4) | — |
| Veredito estético do Diretor | *"graficamente ainda longe do aceitável"* (PRISMA §0, 17/08) | é o produto feito para passar na régua |

### Recomendação

**O produto de loja é o B — o premium first-person. O protótipo 2D não vai para
uma ficha pública de loja.**

O 2D tem um papel nesta frente, e é outro: **é o veículo mais barato que existe
para exercitar o encanamento de publicação** (ícone, splash, assinatura,
versionamento, conta, ficha, classificação etária, política de privacidade) sem
apostar a marca nele. Ele vai para o telefone do Diretor e, no máximo, para uma
faixa de **teste fechado** — nunca para produção pública.

**Custo de publicar o 2D publicamente (o que se paga por escolher A):** uma ficha
de loja é memória pública permanente. Avaliações, nota e histórico grudam no
pacote, e o pacote hoje é `br.com.vstack.arkana`
(`capacitor.config.ts` e `android/app/build.gradle`). Publicar nele um build que
o próprio Diretor classificou como "longe do aceitável", sem o pilar Sintonia
jogável e com o defeito de toque de 17/08 ainda presente, queima o identificador
e a reputação do nome antes do jogo existir. Some-se que o 2D precisaria de tudo
o que está na §4 de qualquer forma — o trabalho de conformidade é o mesmo; só o
risco é maior.
→ **Consequência prática:** se o Diretor quiser mesmo um build do 2D nas mãos de
estranhos, use um **appId separado** (ex.: `br.com.vstack.arkana.lab`) e mantenha
`br.com.vstack.arkana` intocado para o jogo real. Identificador de aplicativo é
recurso de uso único; queimá-lo não tem desfazer.

**Custo de esperar pelo B (o que se paga por escolher B):** o projeto fica sem
presença de loja por tempo indeterminado, e o encanamento de publicação continua
não testado — que é exatamente o que a Fase 1–5 da §5 resolve barato, com o 2D
como carga de teste. Some-se o custo maior, tratado a seguir: **o B começa antes
da validação que o GDD exige.**

### A contradição com o GDD §9 — apontada, não resolvida aqui

O GDD §9 está em **"Decisões travadas"**, e diz, com todas as letras:

> *A visão **first-person premium no Android** (qualidade régua Spell Arena,
> PROJETO_PRISMA §1) fica como destino, e **só inicia quando o checklist de
> validação do Roblox fechar**.*

A decisão do Diretor de antecipar a frente Android **contraria essa cláusula**.
O checklist não fechou: as cinco perguntas V1–V5 (`ROBLOX.md` §1) **não têm uma
única resposta com jogador real** — o `PROJETO.md` §4 registra o Campo de Provas
como *"completo, jogável, nunca testado por terceiros"*.

**O que a antecipação custa, item a item:**

| Risco | Por quê | Fonte |
|---|---|---|
| Construir em 3D caro um pilar que ninguém provou ser divertido | **V1** (Sintonia é divertida entre dois jogadores?) é a pergunta que justifica o produto inteiro. Se voltar negativa, tudo que a frente Android construir em cima de Sintonia é retrabalho | `ROBLOX.md` §1, §11.1 |
| Herdar um número de combate que não existe | **V2** (TTK) não tem dado humano, e o repositório tem **duas réguas conflitantes de TTK** ao mesmo tempo (1,5–2,5 s × 2,75–3,5 s) | `ROBLOX.md` §11.9 nº 1 |
| Orçar terreno reativo no escuro | **V3** só sai qualitativa de uma sessão; o evento de terreno não tem sujeito na telemetria | `ROBLOX.md` §11.1 |
| Rebalancear com ruído de bot | **V4** precisa de ~8 humanos em 3 sessões para ser honesta — e o projeto **já foi enganado uma vez** por dado de bot (o "Fogo 1,42×" da R7 era taxa de acerto do piloto, não balanceamento) | `ROBLOX.md` §11.2, §11.8 |
| Decidir engine sem tração | A rota do premium ainda não foi escolhida: PRISMA §2 continua *"pendente de 1 palavra"*, e a Rota B (first-person) está lá escrita como *"o destino final, mas **depois** do mini-BR top-down 3D ter tração"* | `PROJETO_PRISMA.md` §2, §9 |

**O que a antecipação NÃO custa:** uma parte grande desta frente **não depende de
V1–V5 em nada**. Conta de desenvolvedor, ícone, splash, política de privacidade,
build assinado, ficha de loja, classificação etária, decisão de engine — nada
disso muda se a Sintonia for divertida ou não. Essa é a fatia que pode começar
hoje **sem contrariar o §9**, e é exatamente a que a §5 põe primeiro.

**Três enquadramentos possíveis. A escolha é do Diretor, não deste documento:**

- **(a) Antecipação total** — abre a frente premium agora, aceitando o
  retrabalho se V1 vier negativa.
- **(b) Antecipação parcial** *(a que este documento consegue defender com o que
  o repositório mostra)* — começa **agora** tudo o que não depende de V1–V5
  (Fases 1–7 da §5, com o 2D como carga); o **design de combate e Sintonia** do
  premium espera o playtest.
- **(c) Emenda formal** — o §9 é decisão travada; se a antecipação total é a
  vontade, o caminho honesto é o Diretor **editar o GDD §9** para registrar a
  mudança. Nenhum agente edita decisão travada, e este documento não editou.

Enquanto o §9 não for emendado, **o GDD e a ordem de serviço estão em conflito
declarado**, e quem for programar precisa saber disso antes de escrever a
primeira linha.

---

## 2. A regra que não pode ser quebrada

> **GDD §9, decisão travada:** *"Nada técnico migra do Roblox — o GDD é a única
> fonte que serve os dois produtos."*

Não é preferência de estilo. `PROJETO_PRISMA.md` §2 (Rota D) registra a razão
dura: *"NÃO existe 'migrar para fora' — Luau, assets e sistemas são proprietários
e inexportáveis. Só o GDD e o aprendizado saem."*

### O que isso PROÍBE, na prática

- Copiar qualquer arquivo `.luau` de `roblox/` para o projeto Android, em
  qualquer grau de tradução automática.
- Portar módulo por módulo (`Combat`, `Sintonia`, `Match`, `Terrain`, `Teams`)
  reproduzindo a arquitetura do Roblox porque "lá funcionou".
- Exportar ou reaproveitar **asset** do Roblox: geometria, animação, som, GUI,
  o `.rbxlx` como fonte de level design.
- Depender de semântica da plataforma: `Humanoid`, `DataStore`, `Teams`,
  `RemoteEvent`, câmera padrão, replicação. No Android **nada disso vem pronto**
  — e é justamente o item mais caro do roadmap (PRISMA §2).
- Copiar número direto de `roblox/src/shared/Balance.luau` para o código Android.

### O que isso PERMITE

- **Tudo que já está no GDD**, ou que **passa a estar** antes de virar código.
  O caminho legal de um aprendizado do playtest é sempre este, nesta ordem:
  `resultado do playtest → decisão do Diretor → escrito no GDD → implementado no
  Android`. Nunca `Balance.luau → código Android`.
- **Conhecimento**, que não é ativo técnico: as armadilhas do `PROJETO.md` §7, a
  disciplina "design antes de código; números viram dados; o código lê dados"
  (`ROBLOX.md` §3), a regra de servidor autoritativo como princípio, os padrões
  do PRISMA-1 (*"os padrões, não o código"*, PRISMA §8).
- **Specs de produto** já no GDD: os 5 elementos, a matriz dos 10 combos (§9), a
  tabela de terreno reativo (§14), o escudo evolutivo (§5), a régua de
  acessibilidade cor + ícone + forma (§10), os alvos de toque ≥48dp e o layout
  editável (§19.3), os orçamentos de performance (§19.2 e PRISMA §7).

### O buraco que essa regra abre agora — e como fechá-lo legalmente

O **gesto único de mirar-e-atirar** é a correção mais importante que este projeto
já descobriu para mobile (PRISMA §0: *"mira e disparo em dois gestos separados é
inviável"*). Ele foi implementado **no Roblox** (`PROJETO.md` §4). Mas o
**GDD §19.3 ainda descreve dois gestos**: *"lado direito: mirar arrastando +
botões de Ataque, Tática, Esquiva, Suprema"*.

Ou seja: pela regra do §9, **o gesto único não pode atravessar para o Android
hoje** — a única ponte permitida é o GDD, e o GDD ainda não o descreve.
**Ação necessária, e é do Diretor:** promover a spec do gesto único para o
GDD §19.3. Enquanto isso não acontece, qualquer implementação Android que copie o
comportamento do Roblox está atravessando a ponte errada.

---

## 3. Diretrizes de loja que **este** jogo precisa cumprir

Não é resumo genérico de política. Parte do que o Arkana **é** — battle royale de
duplas, com cosméticos, e (a confirmar, §3.1) público jovem — e do que o GDD já
decidiu.

### 3.1 Classificação etária

**O ponto mais urgente, e é uma lacuna:** o repositório **não declara público-alvo
etário em lugar nenhum**. Busca por "público", "idade", "faixa etária",
"criança", "adolescente" no GDD, `PROJETO.md`, `PROJETO_PRISMA.md` e `README.md`
não retorna nenhuma decisão. **Falta** — e essa decisão precisa entrar no GDD
**antes** de qualquer ativo de loja ser produzido, porque ela muda tudo o que vem
depois (§3.2 e §3.4).

O que o jogo é, e que o questionário de classificação vai perguntar:

- **Violência de fantasia** contra personagens humanoides (magos), com
  eliminação. Nada no repositório declara sangue ou gore; o único registro
  próximo é o nome de uma habilidade ("Magia de sangue" do Mercúrio, GDD §2) —
  nome, não representação.
- **Temas de morte e não-mortos**: Ossaria, a Necromante (GDD §17), com
  esqueletos e "colheita de almas"; e o Espírito Errante pós-morte (GDD §18.6).
  Isso costuma acionar as perguntas de horror/mortos-vivos do questionário.
- **Interação entre usuários**: o produto final é multiplayer. Interação entre
  jogadores é, por si só, um item declarável e afeta a faixa **[verificar na
  política vigente]**.
- **Compras dentro do aplicativo**: quando existirem (GDD §7), viram item
  declarável.

Como se declara: na Google Play, por questionário **IARC**, que emite de uma vez
as classificações regionais (no Brasil, **ClassInd**) **[verificar na política
vigente — quais órgãos, quais perguntas e o fluxo mudam]**. A Apple usa
questionário próprio, com faixas etárias diferentes das da Play e revistas
recentemente **[verificar na política vigente]**.

**Consequência de produto:** se a decisão for mirar **abaixo de 13 anos** ou
declarar **público misto**, as duas lojas jogam o app em programas de família com
regras muito mais pesadas (SDKs permitidos, publicidade, coleta de dados)
**[verificar na política vigente]**. Se a decisão for **13+ / 12+**, o caminho é
consideravelmente mais simples. **Decidir isso é decisão de produto do Diretor,
registrada no GDD — não é escolha de submissão.**

### 3.2 Dados de menores

**A boa notícia é verificável e vale ouro: hoje o jogo não coleta nada.**

| Verificação | Resultado |
|---|---|
| Chamadas de rede no protótipo 2D (`fetch`, `XMLHttpRequest`, `WebSocket`) em `src/**` | **nenhuma** |
| Persistência | só `localStorage`, para as configurações (`src/core/settings.ts`) |
| Permissões no `AndroidManifest.xml` | só `android.permission.INTERNET` |
| SDK de anúncio, analytics de terceiro ou rastreamento | **nenhum** — nem em `package.json`, nem em `android/app/build.gradle` |

Isso torna o formulário de Segurança de Dados da Play trivialmente respondível
hoje: **nenhum dado coletado, nenhum dado compartilhado**. **Isso é um ativo de
conformidade — proteja-o.**

**Regra dura desta frente, alinhada ao GDD:** não entra SDK de anúncio, não entra
analytics de terceiro, não entra rastreamento entre aplicativos. Se algum dia for
preciso medir, mede-se do lado do servidor do próprio jogo, com dado agregado e
sem identificador de dispositivo — e a decisão entra no GDD antes do código.

**O único vazamento que existe hoje, e é fácil de fechar:** `index.html` busca as
fontes em `fonts.googleapis.com` **em tempo de execução**. Isso é uma requisição
de saída para um terceiro, carregando o IP do aparelho, toda vez que o jogo abre
— e num app de público jovem isso é um fluxo declarável, não um detalhe. Também
quebra o jogo offline (o `BUILD_ANDROID.md` já registra a degradação para fonte
de fallback). Não há nenhum `.woff`/`.ttf`/`.otf` no repositório.
→ **Correção recomendada:** empacotar Cinzel, Chakra Petch e Inter localmente. O
`CREDITS.md` §2 já confirma que a SIL OFL 1.1 permite embed e redistribuição.
Fecha o furo de privacidade, resolve o offline (que o GDD §19.6 trata como
estratégico no Brasil) e não custa dependência nova.

**Marco legal aplicável, sem chutar o conteúdo:**

- **ECA Digital** (em vigor desde 17/03/2026), já citado pelo GDD §19.1 como
  motivo de a regra anti-caixa aleatória ser inegociável. As obrigações concretas
  sobre **dados de menores** e **verificação de idade** que ele impõe a um jogo
  publicado no Brasil: **[verificar na política vigente e com assessoria
  jurídica]**.
- **LGPD**, tratamento de dado de criança e adolescente e a exigência de
  consentimento específico de responsável: **[verificar na política vigente]**.
- Se o público declarado incluir menores, ambas as lojas exigem aderência aos
  seus programas de família, que restringem SDKs e publicidade: **[verificar na
  política vigente]**.

**Conclusão operacional:** a arquitetura mais barata de conformidade é a que o
projeto já tem por acidente — **não colete nada**. Cada campo coletado é um campo
a declarar, defender e revisar em duas lojas e sob duas leis.

### 3.3 Monetização — permitido × proibido

O GDD §19.1 (Juramento de Arkana) é **mais restritivo que as lojas**. Ele manda.

| | Regra |
|---|---|
| **Permitido** | Venda direta de cosméticos (GDD §7: vestes, chapéus, montarias de queda, cajados, efeitos de magia, emotes, estandartes). Passe de Batalha de 60 níveis com trilha **determinística e integralmente divulgada**. Moeda premium, se e quando existir |
| **Proibido pelo GDD** | **Qualquer caixa aleatória paga, em qualquer plataforma** (§19.1 nº 4). Qualquer item que afete poder (§19.1 nº 1–2). Pagar para desbloquear ou **acelerar** mago (§19.1 nº 3, corrigido em 18/08). Acesso antecipado vendido, reroll pago, boost competitivo |
| **Efeito colateral bom** | O Juramento **elimina uma superfície inteira de conformidade**: as duas lojas exigem divulgação de probabilidades de itens aleatórios pagos, e há restrição etária brasileira sobre a mecânica (§19.1). Sem caixa, nada disso se aplica |

O que ainda precisa ser checado no lado da loja:

- Uso obrigatório do faturamento da plataforma (Google Play Billing / compra no
  app da Apple) para bens digitais, e o que hoje é permitido em termos de link
  externo de pagamento: **[verificar na política vigente — esta é a regra que mais
  se moveu nos últimos ciclos; não escreva número nem exceção de memória]**.
- Exibição de preço, conversão de moeda virtual e salvaguardas de compra por
  menores (fluxo de aprovação de responsável): **[verificar na política vigente]**.
- Divulgação obrigatória de "contém compras dentro do app" na ficha:
  **[verificar na política vigente]**.

**Recomendação de produto:** a **primeira** submissão sai com **zero
monetização**. Sem faturamento integrado não há nada a revisar, nada a declarar e
nada a errar. Monetização é submissão separada, depois de a ficha existir e o
jogo estar de pé. E o Juramento vai publicado na ficha desde o dia 1 — o próprio
GDD §19.1 nº 5 trata isso como ativo de marketing.

### 3.4 O que muda entre Google Play e App Store

| | **Google Play** | **App Store** |
|---|---|---|
| **Alcançável desta máquina?** | **Sim.** O projeto compila hoje em Windows (`android/`, Gradle 8.13, `compileSdk`/`targetSdk` 36) | **Não.** Gerar `.ipa` exige macOS + Xcode; e a plataforma iOS **nem está instalada** — `package.json` traz só `@capacitor/android`. **Sem um Mac, a App Store não é alcançável** |
| **Conta** | taxa única **[verificar na política vigente]** | assinatura anual **[verificar na política vigente]** |
| **Formato de envio** | **AAB** para apps novos (o APK serve para instalação lateral e teste interno) | `.ipa` via Xcode/Transporter |
| **Teste antes de produção** | contas de desenvolvedor **pessoais** novas precisam rodar um teste fechado com um número mínimo de testadores por um período mínimo antes de liberar produção — **[verificar na política vigente: os números e o próprio requisito já mudaram]**. **É a dependência mais longa e a menos técnica de toda a frente** | TestFlight, com revisão própria para teste externo **[verificar na política vigente]** |
| **Revisão** | majoritariamente automatizada, com revisão humana por amostragem | revisão humana em todo envio, historicamente dura com app que pareça demo, beta ou protótipo — **é motivo provável de reprovação para o candidato A** **[verificar na política vigente para o texto exato da diretriz]** |
| **Nível de API alvo** | exigência mínima que sobe todo ano; o projeto está em `targetSdk 36` — **[verificar na política vigente qual é o mínimo aceito no momento do envio]** | equivalente por versão de SDK |
| **Classificação** | questionário IARC | questionário próprio da Apple |
| **Privacidade** | formulário de Segurança de Dados + URL pública de política | rótulo de privacidade + URL pública de política; ATT se houver rastreamento (não haverá, §3.2) |

**Consequência de sequenciamento:** a Play é a única loja alcançável com o
equipamento que o projeto tem. A App Store entra na conversa quando existir um
Mac — e isso é decisão de compra do Diretor, não item de backlog técnico.

### 3.5 Acessibilidade (GDD §10) — já é ativo, mantenha

A regra **cor + ícone + forma** (nunca só cor) já está implementada nas duas
frentes (`src/ui/TouchControls.ts`, cabeçalho: *"carrossel dos 5 elementos (cor +
forma — regra de daltonismo)"*; `ROBLOX.md` §2 a trata como inegociável), e o
GDD §12 prevê modo daltonismo em Configurações › Jogo. Alvos de toque ≥48dp e
layout de HUD editável já são spec (§19.3) e entrega (CHANGELOG v0.1.1). Isso
sustenta declaração de acessibilidade na ficha sem inventar nada.

---

## 4. O que o jogo ainda NÃO tem para ser publicável

Levantado contra o repositório em 19/08/2026. **existe** = verificado no
worktree · **falta** = verificado que não há · **não verificado** = o repositório
não é capaz de provar (tipicamente, coisa que vive fora dele).

### Encanamento de build

| Item | Status | Evidência |
|---|---|---|
| Projeto nativo Android | **existe** | `android/`, Capacitor 8.5, `minSdk 24`, `compileSdk`/`targetSdk` 36 |
| APK de debug compilado | **existe** | `android/app/build/outputs/apk/debug/app-debug.apk` — 4.569.590 bytes, 19/08/2026 12:51 (ignorado no git por política) |
| `appId` | **existe** | `br.com.vstack.arkana` em `capacitor.config.ts` e `android/app/build.gradle`. ⚠ recurso de uso único — ver a recomendação da §1 |
| Nome do app | **existe** | `android/app/src/main/res/values/strings.xml` → `Arkana` |
| Configuração de assinatura de release | **existe** *(entrou durante a redação deste documento, por outra raia)* | `android/app/build.gradle` ganhou bloco `signingConfigs` condicional, lendo `android/keystore.properties` (não versionado). Sem esse arquivo, o release sai **sem assinatura** — o que é o estado de hoje |
| Keystore de upload + backup | **não verificado** | `.gitignore` exclui `*.keystore`/`*.jks` por política (correto), e não há `android/keystore.properties` no worktree. Se a keystore existe, está fora do repositório. Perder = nunca mais atualizar o app (aviso já registrado em `BUILD_ANDROID.md`) |
| `.aab` de release | **falta** | nenhum bundle gerado |
| Política de `versionCode`/`versionName` | **falta** | `versionCode 1`, nunca incrementado (`versionName` foi para `"0.1.0"` na mesma alteração acima). A loja recusa reenvio com `versionCode` repetido — falta a **política**, não o campo |
| Ofuscação/minificação de release | **falta** | `minifyEnabled false` no build type `release` |

### Identidade visual no aparelho

| Item | Status | Evidência |
|---|---|---|
| Ícone do app | **EXISTE** (R12) | Selo de Arkana em VectorDrawable: adaptativo (26+) + fallback próprio (24–25). Os 15 PNG do Capacitor foram **apagados** — verificado dentro do APK: zero PNG de ícone/splash |
| Fundo do ícone adaptativo | **falta** | `values/ic_launcher_background.xml` = `#FFFFFF`, não a paleta do GDD §10 |
| Ícone monocromático (tema do Android 13+) | **EXISTE** (R12) | camada `<monochrome>` no adaptativo. É a camada que ninguém lembra de testar — vale conferir no aparelho |
| Splash | **EXISTE** (R12) | azul-noite `#0B1026` + Selo, em `drawable/splash.xml`; `values-v31` cobre a splash do sistema em Android 12+, senão sairia um flash claro em todo aparelho moderno |
| Cores do tema nativo | **falta** | `styles.xml` referencia `@color/colorPrimary`, que vem da **biblioteca do Capacitor** (`#3F51B5`, índigo do Material), não do `#0B1026`/`#F0C75E` do GDD §10 |
| Orientação e tela cheia | **existe** | `sensorLandscape` no manifesto; `windowFullscreen` nos temas |
| Fontes empacotadas (offline) | **EXISTE** (R12) | 4 `.woff2` (~69 KB, subconjunto `latin`) em `public/fonts/`, licenças em `OFL.txt`. Portão no `vite.config.ts` quebra o build se voltar CDN. **Achado:** o canvas do Phaser nunca dispara o download de um `@font-face` — precisou de `document.fonts.load()` no boot, senão abriria com fonte de sistema mesmo tendo a local |

### Ficha de loja e conformidade

| Item | Status | Evidência |
|---|---|---|
| Conta de desenvolvedor Google Play | **não verificado** | nada no repositório pode provar. **Ato do Diretor** |
| Conta Apple Developer | **não verificado** | idem — e sem plataforma iOS instalada nem máquina macOS |
| Política de privacidade (texto + URL pública) | **falta** | busca por "privacidade"/"privacy" não retorna nada em `docs/`, `src/` ou `README.md` |
| Termos de uso / EULA | **falta** | inexistente no repositório |
| Canal de suporte / e-mail público | **não verificado** | inexistente no repositório |
| Título e descrição curta | **existe (parcial)** | GDD §9: nome `Arkana: Magos Battle Royale` e descrição de 80 caracteres *"Caia do castelo voador, domine os 5 elementos e seja o último mago de pé."* |
| Descrição longa | **falta** | — |
| Capturas de tela / gráfico de destaque / ícone de loja | **falta** | nenhum ativo de marketing no repositório |
| Faixa etária declarada | **falta** | nenhum documento do projeto declara público-alvo ou classificação (§3.1) |
| Questionário IARC preenchido | **falta** | depende da conta |
| Formulário de Segurança de Dados | **falta** | mas a resposta hoje é "nenhuma coleta" (§3.2) |
| Juramento publicado na ficha | **falta** | previsto no GDD §19.1 nº 5 |

### Conteúdo de jogo (se o candidato A for o veículo)

| Item | Status | Evidência |
|---|---|---|
| Gesto único de mirar-e-atirar no 2D | **falta** | `src/ui/TouchControls.ts` ainda separa mira (arrasto na zona direita) do botão de Ataque |
| Troca de elemento respondendo ao toque | **não verificado** | o defeito foi relatado em aparelho real (PRISMA §0); não há registro de correção no CHANGELOG do 2D, que para em v0.1.2 |
| Sintonia jogável no 2D | **falta** | CHANGELOG v0.1.0: Selo de Sintonia é placeholder decorativo |
| Multiplayer no 2D | **falta** | o 2D é single-player contra bots |
| Licenciamento de todo asset | **existe** | `CREDITS.md`: arte e áudio 100% procedurais, zero asset de terceiros; fontes sob SIL OFL 1.1; dependências MIT/Apache-2.0 |

---

## 5. Ordem de execução

Fases numeradas. Cada uma fecha com um critério **verificável** — não com uma
impressão. **Nenhuma fase tem data**: a ordem é o compromisso; prazo depende de
coisas que este documento não controla (conta de loja, decisão de engine,
disponibilidade do Diretor para o playtest).

Legenda: 🧑 = **ato exclusivo do Diretor** (agente nenhum executa).

### Fase 1 — "No telefone do Diretor" 🧑

*A menor coisa que existe. Não inclui ícone, não inclui splash, não inclui
conserto de gameplay.* O APK já está compilado; falta atravessar o cabo.

- Transferir e instalar `android/app/build/outputs/apk/debug/app-debug.apk` no
  aparelho, pelo caminho já documentado (`adb install -r`, ou copiar o arquivo e
  instalar de fonte desconhecida).
- **Pronto quando:** o Diretor abre o jogo no próprio aparelho, chega à arena e
  **termina uma partida contra os bots**; e registra três informações — modelo do
  aparelho, versão do Android, e o FPS observado (o contador já existe em
  Configurações › Vídeo, CHANGELOG v0.1.0 nº 11). Mais três linhas do que
  incomodou no toque.

### Fase 2 — "Parece o Arkana"

- Ícone adaptativo a partir do **Selo de Arkana** (GDD §10), com camada
  monocromática; fundo na paleta do GDD, não em branco.
- Splash em azul-noite `#0B1026` com o Selo, substituindo o padrão do Capacitor.
- Cores do tema nativo alinhadas ao GDD §10.
- Fontes OFL empacotadas localmente, removendo a busca a `fonts.googleapis.com`.
- **Pronto quando:** instalação limpa mostra o Selo na gaveta de aplicativos, o
  splash em azul-noite, e o jogo abre **em modo avião** com a tipografia correta.
- ✅ **ENTREGUE na R12**, com prova: o APK foi extraído e não contém nenhum PNG de
  ícone/splash (o padrão do Capacitor saiu do pacote, não só do código-fonte), e o
  `dist/` não referencia CDN de fonte nenhuma. **Falta só a confirmação no aparelho
  físico**, que é ato do Diretor — a Fase 1 e esta se conferem no mesmo gesto.

### Fase 3 — "Consertar o que o Diretor já reprovou"
*(só se o 2D seguir como veículo — ver §1)*

- **Pré-requisito 🧑:** promover a spec do **gesto único** para o GDD §19.3
  (hoje o GDD ainda descreve dois gestos — §2 deste documento). Sem isso, a
  correção não tem ponte legal para atravessar do Roblox.
- Implementar o gesto único no `src/ui/TouchControls.ts` e corrigir a troca de
  elemento que não responde.
- **Pronto quando:** no aparelho físico, arrastar a partir do botão de ataque
  mira e dispara **em um gesto**; a troca de elemento responde ao primeiro
  toque; e o Diretor joga 3 partidas sem reencontrar nenhum dos dois defeitos de
  17/08.
- ⚠️ **METADE ENTREGUE na R12.** A troca de elemento foi consertada (o defeito era
  REAL e falhava 100% das vezes — ver abaixo); o **gesto único continua bloqueado**
  pelo pré-requisito acima, e a equipe recusou improvisar meio-termo.
- **A causa-raiz da troca de elemento, para o registro** — eram DOIS defeitos, e o
  segundo explica por que só o aparelho do Diretor via:
  1. `setScrollFactor(0)` era aplicado ao *container*, nunca aos ícones. O Phaser
     restaura o `scrollFactor` do filho depois de desenhar, mas o hit-test usa o do
     próprio filho — então a área de toque ficava deslocada pelo scroll da câmera,
     que na Arena **segue o jogador**. O toque caía na zona de mira.
  2. O piso de 48dp brigava com o espaçamento de 54px: em tela de 640px o raio de
     toque vira 48px e **três quartos da superfície do próprio ícone** entregavam o
     toque ao vizinho. Em desktop (1280px) o raio é 24px e nada disso acontece —
     por isso o defeito era invisível fora do celular.

### Fase 4 — "A conta e o relógio" 🧑

Vem cedo de propósito: o requisito de teste fechado para contas novas
**[verificar na política vigente]** é a dependência mais longa da frente inteira,
e o relógio só começa a correr depois que a conta existe. Nenhuma linha de código
acelera isso.

- Abrir a conta de desenvolvedor da Google Play.
- Criar o aplicativo como rascunho, com o `appId` decidido (ver a recomendação de
  `appId` separado, §1).
- **Pronto quando:** o console está acessível e o rascunho existe com o
  identificador definitivo escolhido.

### Fase 5 — "Build assinado e reproduzível"

- O bloco `signingConfigs` **já entrou** no `android/app/build.gradle` (raia
  paralela, 19/08), lendo `android/keystore.properties` fora do controle de
  versão. **Falta o que ele consome:** a keystore gerada e o
  `keystore.properties` preenchido 🧑.
- Política de `versionCode` incremental documentada.
- **Pronto quando:** `gradlew bundleRelease` produz um `.aab` **assinado**; a
  keystore está em backup **fora desta máquina** 🧑; e um segundo build com
  `versionCode` incrementado é gerado sem editar nada além do número.

### Fase 6 — "Ficha mínima e legal"

- Política de privacidade escrita e publicada em URL estável e pública 🧑 —
  hoje ela é curta e honesta, porque o app não coleta nada (§3.2).
- **Pré-requisito 🧑:** faixa etária/público-alvo decidido e registrado **no GDD**
  (§3.1) — sem isso o questionário de classificação não tem resposta defensável.
- Descrição longa, capturas do aparelho real, gráfico de destaque, Juramento
  publicado na ficha (GDD §19.1 nº 5).
- Questionário de classificação e formulário de Segurança de Dados 🧑.
- **Pronto quando:** todos os campos obrigatórios do console estão preenchidos e o
  rascunho passa na validação do próprio console — **sem enviar nada**.

### Fase 7 — "Teste fechado" 🧑

- Enviar o build para a faixa de teste fechado e recrutar os testadores.
- **Pronto quando:** pelo menos **um aparelho que não é o do Diretor** instala o
  jogo **pela loja**, e o número de testadores e o período exigidos pela política
  vigente estão cumpridos **[verificar na política vigente]**.

### Fase 8 — "Decidir o produto de loja definitivo" 🧑

O portão que a §1 e o GDD §9 apontam. Não abre por tempo; abre por resposta.

- **Pré-requisito 1:** V1 respondida por dupla real (protocolo do `ROBLOX.md`
  §11; arranjo mínimo recomendado: 4 humanos = 2 duplas).
- **Pré-requisito 2:** rota de engine escolhida (PRISMA §2 ainda pendente).
- **Pronto quando:** o GDD registra, pela mão do Diretor, o veredito de V1 e a
  rota escolhida. **A frente premium só recebe código depois disso.**

> **Sobre as fases 1–7:** nenhuma delas depende de V1–V5. É por isso que elas
> podem começar agora sem contrariar o GDD §9 (o enquadramento (b) da §1). O que
> contraria o §9 é abrir a **Fase 8** antes do playtest.

---

## 6. O que a validação do Roblox ainda deve responder

As cinco perguntas estão em `ROBLOX.md` §1. **Nenhuma foi respondida com jogador
real** — o `PROJETO.md` §4 registra o Campo de Provas como *"completo, jogável,
nunca testado por terceiros"*.

| # | Pergunta | O que ela decide na frente Android | Se vier negativa |
|---|---|---|---|
| **V1** | A Sintonia é divertida entre dois jogadores? | Se o premium é construído **em volta** da Sintonia ou não. É a pergunta que justifica o produto inteiro | **Invalida a frente Android como está desenhada.** O pilar cai, e tudo que tiver sido construído em cima dele (VFX de combo, netcode de janela compartilhada, HUD de Sintonia) é retrabalho |
| **V2** | O TTK está certo com humanos mirando? | Os números de combate que o Android implementa | Recalibra. **Mas hoje nem a régua está fechada:** `ROBLOX.md` §11.9 nº 1 registra duas réguas conflitantes (1,5–2,5 s × 2,75–3,5 s). Enquanto o Diretor não escolher uma, **nenhum número de combate do Android é seguro** |
| **V3** | Queimar a floresta / congelar o lago vira jogada real? | Quanto orçamento o premium gasta no terreno reativo — o sistema mais caro depois do netcode | Reduz o escopo do terreno. Uma sessão só entrega qualitativo (§11.1: o evento de terreno não tem sujeito na telemetria) |
| **V4** | Algum elemento domina? | Se os 5 elementos vão para o Android como estão | Rebalanceia. Precisa de ~8 humanos em 3 sessões para ser honesto (§11.2), e o projeto **já foi enganado por dado de bot uma vez** |
| **V5** | O jogador volta? | Se vale investir em produto premium ou se o loop precisa mudar antes | Redesenho de retenção. **Não se responde na mesma sessão, por construção** (§11.1) — e a própria definição de V5 diverge entre §1 e §10 do `ROBLOX.md` (§11.9 nº 2) |

### Como isso alimenta a frente Android — o caminho legal

`resultado do playtest` → `decisão do Diretor` → **escrito no GDD** →
`implementado no Android`.

Nunca `Balance.luau` → código Android. A ponte é o documento (§2).

### A leitura honesta

Uma sessão de playtest **responde V1** e dá sinal preliminar de V2/V3. **V4 e V5
não saem de uma sessão** — `ROBLOX.md` §11.1 diz literalmente que quem tratá-las
como respondidas *"vai rebalancear o jogo com ruído"*. Portanto, o "checklist de
validação" do GDD §9 **não fecha com uma sessão**, e qualquer plano da frente
Android que assuma o contrário está assumindo dado que não vai existir.

E o oposto também é verdade, e é o argumento a favor de começar já: **as Fases
1–7 da §5 não têm nada a ver com V1–V5.** Ícone, assinatura, conta, política de
privacidade e ficha de loja não mudam nem um pixel em função de a Sintonia ser
divertida. Antecipar **essa** parte não custa retrabalho nenhum; antecipar o
código do premium custa.

---

## 7. Contradições encontradas — registradas, não corrigidas

Nenhuma foi resolvida aqui. Resolver decisão travada é ato do Diretor no GDD.

1. **GDD §9 × ordem de serviço.** O §9 é decisão travada e diz que o premium
   *"só inicia quando o checklist de validação do Roblox fechar"*. A decisão de
   antecipar contraria isso, e o checklist não fechou (zero respostas com jogador
   real). Detalhado na §1.
2. **GDD §9 × GDD §8 × PRISMA §2.** O §9 aponta o destino como **first-person**.
   Mas a escada do §8 põe o **Degrau 3 (mini-BR top-down)** antes do Degrau 4, e
   o PRISMA §2 (Rota B) diz que o first-person *"continua sendo o destino final,
   mas **depois** do mini-BR top-down 3D ter tração"* — e recomenda a **Rota A**
   (Godot 4, top-down 3D) como próxima. **Não está decidido se o próximo build
   Android é top-down 3D ou first-person**, e o PRISMA §2 segue *"pendente de 1
   palavra"*.
3. **GDD §19.3 × PRISMA §0 × código.** O GDD (fonte da verdade) ainda especifica
   **dois gestos** (*"mirar arrastando + botões de Ataque…"*); o PRISMA §0
   registra que dois gestos é **inviável** em celular; `src/ui/TouchControls.ts`
   implementa dois gestos; o Roblox implementa um. A fonte da verdade está
   desatualizada em relação ao aprendizado, e o 2D nunca recebeu a correção.
4. **`BUILD_ANDROID.md` × `PROJETO.md`/`README.md` × o disco.** A versão lida do
   `BUILD_ANDROID.md` diz *"Falta só o SDK na máquina para compilar"* e o
   CHANGELOG v0.1.1 lista *"APK não compilado nesta máquina"*; `PROJETO.md` §3–§4
   e o `README.md` afirmam *"APK compilado, testado em aparelho"*; e o APK existe
   agora em `android/app/build/outputs/apk/debug/` (19/08, 12:51).
   *Nota: `BUILD_ANDROID.md` está sendo reescrito em paralelo — pode já ter sido
   corrigido.*
5. **GDD §19.5 abandonada no meio, sem nota.** A "ordem de execução" prevê
   **Fase 3 = v0.2 (Sintonia com bot aliado, tela de queda, Selo do Campeão,
   Presságios)**. Ela nunca foi executada no 2D — o CHANGELOG do protótipo para
   em v0.1.2 e o projeto virou para o Roblox. O GDD não registra o desvio.
6. **CHANGELOG v0.1.0 cita Tauri.** *"o arquivo real chega no empacotamento
   **Tauri** da Fase 2"* — a Fase 2 foi feita com **Capacitor**. Registro
   obsoleto.
7. **Público-alvo etário nunca declarado.** Não é contradição, é buraco: nenhum
   documento do projeto define faixa etária ou classificação pretendida, e a
   frente de loja não anda sem isso (§3.1).
8. **Contradições herdadas.** O `ROBLOX.md` §11.9 já registra quatro (régua de
   TTK, definição de V5, §6 congelada em 18/08, cabeçalho do `Playtest.luau`).
   Elas **atravessam** para o Android junto com qualquer número que a frente
   importar — resolvê-las antes é mais barato do que descobrir depois qual dos
   dois TTKs o jogo premium implementou.

---

## 8. Mapa de leitura

| Documento | Para quê, nesta frente |
|---|---|
| [GDD.md](GDD.md) | **Fonte da verdade.** §9 decisões travadas · §10 identidade e acessibilidade · §12 configurações · §19.1 Juramento · §19.2 guardrails mobile · §19.3 toque |
| [PROJETO_PRISMA.md](PROJETO_PRISMA.md) | §1 a régua "Spell Arena" · §2 rotas de engine (ainda pendente) · §7 orçamentos mobile |
| [ROBLOX.md](ROBLOX.md) | §1 as perguntas V1–V5 · §11 protocolo do playtest · §11.9 contradições conhecidas |
| [PROJETO.md](PROJETO.md) | §3 linha do tempo · §4 estado atual · §7 armadilhas |
| [BUILD_ANDROID.md](BUILD_ANDROID.md) | como o APK é gerado e instalado (o encanamento; este documento cuida do produto) |
| [CREDITS.md](CREDITS.md) | licenças — o que sustenta a declaração de propriedade na ficha de loja |
