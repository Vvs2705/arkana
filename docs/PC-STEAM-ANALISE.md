# ARKANA no PC / Steam — análise de mercado, arquitetura, segurança e receita

> Pedido do Diretor em 27/08/2026, ao decidir migrar o projeto de mobile para PC:
> *"faça uma análise de mercado completa, veja configurações, ajustes, servidores,
> ferramentas, linguagens, segurança anti-cheat entre outras coisas, LGPD para
> jogos, tudo que for necessário para fazer algo seguro, válido, e com
> rentabilidade."*
>
> **Tudo aqui é pesquisado, com fonte.** Onde eu não achei número, está escrito
> que não achei. Fontes no fim.

---

## 0. O RESUMO DE UMA PÁGINA

**Migrar para PC é tecnicamente barato e estrategicamente arriscado — mas o
risco não vem do PC, vem do GÊNERO.**

Três fatos que mudam a conversa:

1. **O Spellbreak foi encerrado em 2024.** A referência declarada deste projeto
   fechou as portas. O Hyper Scape, da Ubisoft, também.
2. **46% dos battle royales lançados não sustentam base de jogadores** além da
   janela de lançamento.
3. **A mediana de receita de um indie na Steam é US$ 5.000 a 15.000 vitalícios.**
   Metade dos jogos fica abaixo de US$ 15.000 — para sempre.

E o problema estrutural: **battle royale é o gênero que mais depende de gente
online**. Um BR com 30 jogadores simultâneos não é um BR pequeno — é um jogo
quebrado, porque a partida não fecha. Isso se chama *cold start*, e é o único
problema de design que dinheiro de marketing resolve e talento não.

**A recomendação está na §9.** Ela não é "não faça" — é mudar a FORMA do produto
para que ele funcione com pouca gente, e manter o núcleo que já existe.

---

## 1. MERCADO — os números

### 1.1 O tamanho do funil na Steam

| Faixa | Receita vitalícia bruta |
|---|---|
| Metade de baixo | abaixo de **US$ 15.000** |
| p50–p75 | US$ 15.000 – 75.000 |
| p75–p90 | US$ 75.000 – 300.000 |
| p90–p95 | US$ 300.000 – 1.000.000 |
| **Top 5%** | **acima de US$ 1.000.000** |

**Mediana: US$ 5.000 a 15.000.** Depois dos 30% da Valve, sobram US$ 3.500 a
10.500 — vitalícios, não por mês.

### 1.2 A wishlist decide antes do lançamento

Receita do PRIMEIRO MÊS por wishlists no dia do lançamento:

| Wishlists | 1º mês |
|---|---|
| menos de 5.000 | abaixo de US$ 15.000 |
| 5.000 – 10.000 | US$ 15.000 – 40.000 |
| 10.000 – 25.000 | US$ 40.000 – 100.000 |
| 25.000 – 50.000 | US$ 100.000 – 250.000 |
| mais de 50.000 | acima de US$ 250.000 |

**Isto é o dado mais acionável do documento.** A página da Steam tem que existir
**muito antes** do jogo, porque a wishlist é o produto real do desenvolvimento.

### 1.3 O gênero

- **67 battle royales lançados em 2026.** Não é um mar aberto.
- **46% não sustentam base** depois do lançamento.
- Causas apontadas: falta de diferenciação, execução técnica ruim, e ausência de
  suporte pós-lançamento.
- **Spellbreak (2020–2024) e Hyper Scape (Ubisoft) encerrados.** O Spellbreak
  tinha combate de magia elemental com combinação — exatamente a proposta da
  ARKANA — e mesmo com estúdio, dinheiro e Epic por trás, não segurou.

⚠️ **Isto não é motivo para desistir.** É motivo para não repetir a aposta deles.
O Spellbreak morreu de **retenção**, não de combate ruim — o combate dele é
elogiado até hoje. Ver §9.

---

## 2. O PROBLEMA ESTRUTURAL: BR PRECISA DE GENTE

Um BR de 60 jogadores precisa de **60 pessoas na fila ao mesmo tempo**, na mesma
região, na mesma faixa de habilidade. Com 100 jogadores simultâneos no mundo
inteiro, a fila não fecha, o jogador espera, desiste, e o número cai — é uma
espiral.

Três saídas, e só três:

| Saída | O que exige |
|---|---|
| **Base grande no dia 1** | verba de marketing ou viralização — nenhuma das duas se planeja |
| **Lobby pequeno** | 10–20 jogadores por partida em vez de 60 |
| **Bots preenchendo** | IA boa o bastante para não humilhar a experiência |

**A ARKANA já está na terceira** — o jogo hoje é 1 jogador + 12 bots, e funciona.
Isso não é uma limitação: é o único formato de BR que sobrevive a um lançamento
indie, e o projeto chegou nele por acidente.

---

## 3. O QUE FICA E O QUE MUDA NA TECNOLOGIA

### 3.1 Fica tudo

| | |
|---|---|
| **Engine** | Godot 4.4.1 — exporta Windows/Linux/macOS do MESMO projeto |
| **Linguagem** | GDScript — sem motivo para trocar; o gargalo nunca foi a linguagem |
| **Código de jogo** | Zona, queda, loot, kits, terreno, os 12 selftests: **nada se perde** |
| **Arte** | as peças do Meshy existem em 3 M de faces no workspace, não só as decimadas |

### 3.2 O que o PC devolve na hora

Tudo que travou o desenvolvimento em agosto era teto de celular:

| Restrição mobile | No PC |
|---|---|
| decimar peças de 3.000.000 → 800–4.000 faces | usar quase como vieram |
| textura 2K → 512 | 2K/4K normal |
| shader "sem textura" | texturas, normal map, PBR completo |
| `specular_disabled`, sem reflection probe (**causa do "mago preto"**) | probe/SSR, e o metal vira metal |
| ~120 draw calls | milhares |
| 150 MB de teto na Play Store | irrelevante |
| `Island.ESCALA` limitado por FPS não medido | mapa maior sem chunking |

### 3.3 O que precisa ser CONSTRUÍDO

| | Estado hoje |
|---|---|
| **Rede** | **não existe uma linha.** Plano em `docs/infra/01-MULTIJOGADOR.md` |
| Contas de jogador | não existe |
| Matchmaking | não existe |
| Servidor autoritativo | não existe — toda a lógica roda no cliente |
| Anti-cheat | não existe |
| Salvamento em nuvem / progressão | não existe (a moeda do elenco é local) |

**A rede é o item mais caro do projeto, e o preço é o mesmo em PC ou mobile.**
Escolher PC não a torna mais barata.

---

## 4. ARQUITETURA DE REDE E SERVIDOR

### 4.1 A decisão que não dá para adiar

**Servidor autoritativo, desde a primeira linha.**

Hoje toda a lógica da ARKANA roda no cliente: dano, mana, posição, loot, zona.
Num jogo em rede isso significa que **o cliente decide se acertou** — e é
exatamente o que um trapaceiro edita.

Retrofitar autoridade depois **é reescrever o jogo**. Antes da primeira linha de
rede, a regra tem que ser: o cliente manda INTENÇÃO (andei, atirei), o servidor
decide RESULTADO (acertou, tomou dano, pegou o item), e o cliente só desenha.

Boa notícia: o projeto já tem os pontos únicos para isso — `Combat.deal` é o
único lugar que aplica dano, o `Balance` é a fonte única dos números, e a Zona já
guarda o seed da partida para transmitir. A arquitetura de raia ajuda.

### 4.2 Topologia

| Opção | Serve? |
|---|---|
| **P2P / listen server** (um jogador hospeda) | ❌ para BR competitivo — o host vê tudo e trapaceia de graça |
| **Servidor dedicado autoritativo** | ✅ é o único caminho honesto |

Godot exporta **build headless para Linux**, que roda em contêiner sem interface —
é literalmente o mesmo projeto com outro preset de exportação. Tick padrão de
física: 60/s.

### 4.3 Serviços — o que existe

| Serviço | Para quê | Custo |
|---|---|---|
| **Epic Online Services (EOS)** | contas, amigos, lobby, matchmaking, voz, **Easy Anti-Cheat** | **grátis**, sem teto de jogadores |
| **Nakama** (Heroic Labs) | backend open source, self-host, tem SDK Godot | grátis self-host; cloud pago |
| **AccelByte** | matchmaking + servidor dedicado num pacote | grátis até 30 CCU |
| **Gameye / GSB** | orquestração de servidores por sessão | paga por sessão rodando |

⚠️ **Dois fornecedores de servidor dedicado FECHARAM em 2026** (Hathora e o
Multiplay da Unity). Não amarrar o projeto a um fornecedor só — a build headless
em contêiner tem que rodar em qualquer lugar.

### 4.4 Custo de servidor, na conta

Não achei tabela pública de preço por CCU do Gameye. A conta que dá para fazer:

- uma partida de 20 jogadores cabe folgada num vCPU
- **paga-se sessão rodando**, não frota parada
- partida de ~8 minutos (a tabela da Zona hoje soma 396 s + queda)

Com 100 jogadores simultâneos no pico, são ~5 partidas simultâneas, ou seja
**5 contêineres pequenos**. Isso é dezenas de dólares por mês, não milhares.
**Servidor não é o custo que mata — o custo que mata é não ter jogador.**

---

## 5. ANTI-CHEAT — a parte honesta

### 5.1 A hierarquia real

1. **Servidor autoritativo** — 80% do problema. Sem isso, nada mais importa.
2. **Validação de plausibilidade no servidor** — velocidade máxima, cadência de
   tiro, alcance, mana. A ARKANA tem isso pronto no papel: *velocidade é produto
   único* e o `Balance` já é a fonte da verdade. O servidor recusa o impossível.
3. **Anti-cheat de kernel** (EAC, BattlEye) — pega *aimbot* e *wallhack*, que o
   servidor sozinho não vê.
4. **Telemetria e banimento** — estatística de acerto por jogador.

### 5.2 EAC com Godot: **não confirmado**

O Easy Anti-Cheat é gratuito dentro do EOS, sem teto de jogadores. **Mas não
encontrei um caso público e documentado de integração EAC + Godot funcionando.**
A pergunta foi feita no repositório de bindings EOS para Godot em 2023 e a
discussão está fechada sem conclusão pública acessível.

**Isso é um risco a verificar antes de prometer anti-cheat de kernel**, não um
impedimento: EAC é integração nativa (C++), e Godot aceita GDExtension, então o
caminho existe — só não está trilhado.

Existem plugins de anti-cheat comunitários no Asset Library do Godot. **São
paliativos client-side**: qualquer coisa que rode no cliente é editável pelo
cliente. Servem contra o trapaceiro casual, não contra quem quer trapacear.

### 5.3 O que fazer

- **Fase 1:** servidor autoritativo + validação de plausibilidade. Isso já entrega
  um jogo competitivo honesto.
- **Fase 2:** EAC via EOS, com um teste técnico ANTES de anunciar. Se não sair,
  BattlEye é a alternativa paga.
- **Nunca:** confiar em anti-cheat client-side como se fosse segurança.

---

## 6. JURÍDICO E DADOS — Brasil e mundo

### 6.1 Classificação indicativa (obrigatória no Brasil)

- Regulada pela **Portaria MJ nº 1.189/2018**, e vale para jogos eletrônicos.
- Faixas: **L, 10, 12, 14, 16, 18.**
- **A Steam bloqueia jogos sem classificação indicativa no Brasil.** A plataforma
  implementou questionário automatizado (padrão IARC) para o desenvolvedor
  responder e obter a classificação.
- **Na prática:** responder o questionário do Steamworks resolve. Não é processo
  separado no Ministério da Justiça para quem distribui só digital.
- **Para a ARKANA:** magia elemental sem sangue, sem gore, sem tema sexual. A
  regra do projeto já proíbe sangue (*"o Noctus drena ÉTER, não sangue"*).
  Provável **10 ou 12**. Manter essa regra é o que segura a faixa baixa — e faixa
  baixa é audiência maior.

### 6.2 LGPD

Aplica assim que houver **conta de jogador**, porque aí há dado pessoal.

O que a LGPD exige, na prática:

| Exigência | O que fazer |
|---|---|
| Base legal | execução de contrato (a conta é necessária para jogar) |
| Minimização | **não coletar o que não usa.** Para jogar basta um identificador e um apelido |
| Transparência | política de privacidade acessível, em português |
| Direitos do titular | acesso, correção e **exclusão** da conta — botão, não e-mail |
| Segurança | senha com hash forte, TLS, sem log de dado pessoal |
| Incidente | plano de resposta e comunicação à ANPD |
| Menores | consentimento parental para menor de 12; outro motivo para manter a faixa |

**O atalho legítimo:** usar **login da Steam / EOS** e **não guardar dado pessoal
nenhum**. Se o jogo só armazena um ID opaco da plataforma e estatística de
partida, a superfície de LGPD encolhe para quase nada. **É a decisão de
arquitetura mais barata deste documento inteiro.**

### 6.3 Fora do Brasil

- **GDPR** vale para jogador europeu, e é mais rígido que a LGPD. Quem cumpre
  GDPR cumpre LGPD.
- **Imposto americano:** a Valve retém imposto na fonte. O Brasil tem tratado, e
  preencher o **W-8BEN** no Steamworks reduz a retenção. Sem isso perde-se
  dinheiro por burocracia não feita.
- **Pessoa jurídica:** dá para publicar como pessoa física, mas CNPJ facilita
  imposto, contrato e futuro. Decisão de contador, não minha.

---

## 7. RENTABILIDADE — os modelos

| Modelo | Serve para a ARKANA? |
|---|---|
| **Premium** (pago uma vez, US$ 10–20) | ✅ **Sim.** Receita no dia 1, sem loja, sem servidor de economia, sem LGPD de pagamento. E resolve trapaça: quem é banido perde o jogo comprado |
| **Free-to-play + cosmético** | ❌ **Não agora.** F2P só funciona com base grande: precisa de loja, economia, moeda, servidor de inventário, e ~2% de pagantes. Com 500 jogadores, 2% são 10 pessoas |
| **Early Access** | ✅ **Sim.** É o caminho natural: vende antes de estar pronto, financia o desenvolvimento e constrói comunidade |

**Recomendado: Early Access premium, US$ 9,99 a 14,99.**

E o detalhe que quase ninguém faz: **o desbloqueio de magos por moeda de partida
já está desenhado** (`docs/design/DESBLOQUEIO-ELENCO.md`) e é **progressão, não
monetização**. Isso é exatamente certo para premium — dá o que fazer sem loja.

### 7.1 A conta realista

Com uma página de Steam bem-feita e divulgação consistente, **10.000 wishlists no
lançamento é uma meta ambiciosa mas alcançável** para um projeto com identidade
visual forte. Isso projeta **US$ 40.000 a 100.000 no primeiro mês**.

Sem página de Steam antecipada e sem divulgação, o cenário é a mediana:
**US$ 5.000 a 15.000 vitalícios.**

**A diferença entre os dois cenários não é o jogo. É a wishlist.**

---

## 8. CUSTOS — a tabela

| Item | Custo | Quando |
|---|---|---|
| **Steam Direct** | **US$ 100 por produto**, recuperável após US$ 1.000 de receita | ao criar a página |
| Comissão da Valve | **30%** até US$ 10 M; 25% de 10 a 50 M; 20% acima | por venda |
| Classificação indicativa | grátis (questionário Steamworks) | antes de vender no Brasil |
| EOS (contas, lobby, matchmaking, EAC) | **grátis** | — |
| Servidor dedicado | dezenas de US$/mês em escala pequena | ao ligar a rede |
| Capsule art, trailer, página | tempo, ou US$ 300–1.500 terceirizado | 6+ meses antes |
| Créditos Meshy | ~US$ ? (saldo atual 1.884) | contínuo |
| Contador / CNPJ | conforme o profissional | antes da primeira receita |

**O dinheiro não é o obstáculo. US$ 100 e tempo são o obstáculo.**

---

## 9. RECOMENDAÇÃO

### 9.1 Sobre a migração

**Sim, migrar para PC.** As razões técnicas da §3.2 são fortes e o custo é quase
zero — o Godot exporta do mesmo projeto.

**Manter a exportação mobile viva como alvo, não como lançamento.** As restrições
dela deixaram o jogo eficiente e esse trabalho não se perde.

**Não fazer dois produtos separados.** Um projeto, duas exportações, dois pools de
partida separados por input quando a rede existir.

### 9.2 Sobre o gênero — a parte difícil

**Battle royale puro, com lobby de 60 e matchmaking global, é a aposta mais
arriscada possível para este projeto.** O Spellbreak provou que nem combate
excelente segura um BR sem base.

Três ajustes que preservam TUDO que já foi feito e removem o risco de morte:

1. **Lobby de 16 a 20, não 60.** Partida fecha com menos gente, o mapa de 600 m
   já está calibrado para isso, e a Zona nova já aperta em 6 min 36 s.
2. **Bots por padrão, sempre.** Não como plano B — como recurso anunciado. A
   partida NUNCA espera. Hoje o jogo já roda 1 + 12 bots e funciona; isso é ativo,
   não dívida.
3. **Modo solo/PvE valendo por si.** Um BR contra bots bons, com progressão de
   magos, é um produto vendável ANTES de existir uma linha de rede. E é o que
   permite lançar em Early Access sem servidor nenhum.

**Com esses três, o jogo pode lançar sem rede, vender, juntar comunidade — e a
rede entra depois, financiada.** Sem eles, o lançamento depende de um servidor
caro e de uma base que ainda não existe.

### 9.3 A ordem

| Fase | O quê | Portão para a seguinte |
|---|---|---|
| **0** | build de PC sem os tetos de celular; ver o jogo em alta | o Diretor aprova o visual |
| **1** | página de Steam no ar + trailer, **muito antes do jogo** | wishlists começam a subir |
| **2** | elenco refeito no site, mapa fechado, VFX e áudio | jogo bonito de ver em vídeo |
| **3** | Early Access **solo + bots**, premium | receita e comunidade |
| **4** | servidor autoritativo + EOS + matchmaking | rede funcionando |
| **5** | EAC, ranqueada, pools por input | competitivo honesto |

**A fase 1 é a que mais rende e a que sempre fica para depois.** A wishlist é o
produto real dos próximos meses.

---

## 10. O QUE EU NÃO SEI

Registrado para não virar afirmação:

- **Preço por CCU dos orquestradores de servidor** — não é público; precisa de
  cotação.
- **EAC + Godot funcionando** — não achei caso público documentado. Precisa de
  teste técnico antes de virar promessa.
- **Se o público brasileiro paga premium** — a intuição do mercado é que F2P
  domina no Brasil, mas na Steam o público é diferente. Não achei dado firme.
- **FPS no aparelho** — continua sem medição, e agora só importa para decidir se
  a exportação mobile continua viável.

---

## Fontes

- [Indie Game Revenue Data 2026 — Steam Page Analyzer](https://www.steampageanalyzer.com/blog/indie-game-revenue-data) — medianas, percentis e conversão de wishlist
- [Steam Revenue Share Explained — Steam Page Analyzer](https://www.steampageanalyzer.com/blog/steam-revenue-share-explained) — faixas 30/25/20
- [Steam Direct 2026: Complete Publishing Walkthrough — Egmatic](https://egmatic.com/blog/steam-direct-2026-publishing-walkthrough) — taxa de US$ 100 e processo
- [Battle Royale Games in 2026: Dying or Evolving? — Zyntohub](https://zyntohub.com/battle-royale-future-extraction-shooters/) — encerramento de Spellbreak e Hyper Scape
- [Is Friendslop saturated? — How To Market A Game](https://howtomarketagame.com/2026/07/30/is-friendslop-saturated/) — 67 BRs lançados em 2026
- [Are Battle Royale Games Dying in 2026 — squaddost](https://squaddost.online/are-battle-royale-games-dying-in-2026-data-trends/) — 46% não sustentam base
- [Easy Anti-Cheat / Epic Online Services](https://www.easy.ac/) — EAC gratuito dentro do EOS
- [Does this work with Easy Anti-Cheat? — epic-online-services-godot #10](https://github.com/3ddelano/epic-online-services-godot/issues/10) — pergunta sem conclusão pública
- [Godot 4 Dedicated Server Hosting — Gameye](https://gameye.com/blog/godot-dedicated-server-hosting/) — build headless, contêiner, 60 tick, saída de Hathora e Multiplay em 2026
- [Best Game Backend for Real-Time Multiplayer — AccelByte](https://accelbyte.io/blog/best-game-backend-for-real-time-multiplayer-matchmaking) — EOS grátis, Nakama self-host, AccelByte grátis até 30 CCU
- [Nakama — Heroic Labs](https://heroiclabs.com/nakama/) — backend open source com SDK Godot
- [Steam bloqueia jogos sem classificação indicativa no Brasil — Steam Community](https://steamcommunity.com/discussions/forum/27/3175526477755411545/)
- [Classificação indicativa e IARC — Caputo Duarte Advogados](https://caputoduarte.com.br/a-classificacao-indicativa-nos-jogos-digitais-e-a-utilizacao-do-international-age-rating-coalition-iarc/) — questionário automatizado da loja
- [Jogos e Apps — Ministério da Justiça](https://www.gov.br/mj/pt-br/assuntos/seus-direitos/classificacao-1/paginas-classificacao-indicativa/jogos-e-apps) — Portaria MJ nº 1.189/2018
- [Jogos Eletrônicos e a LGPD — Jusbrasil](https://www.jusbrasil.com.br/artigos/jogos-eletronicos-e-a-lgpd/1664978754)
