# Backend e serviços — o que um BR mobile precisa além do servidor de partida

> Fase que ativa: **G5** (o mínimo para conectar) e **G6** (o mínimo para
> público). A régua de tudo aqui é a postura do GDD §9: **"nenhuma coleta"**.
> Cada campo NÃO coletado é um campo que não precisa ser declarado na Play,
> defendido sob LGPD/ECA Digital nem protegido de vazamento. O barato e o
> correto apontam para o mesmo lado.

## 1. Contas / login

**Recomendação: login anônimo por identidade gerada no aparelho. Nenhuma
conta tradicional.**

| Opção | O que coleta | Veredito |
|---|---|---|
| **ID anônimo gerado no primeiro boot** (UUID aleatório criado pelo jogo, guardado em `user://`) + apelido escolhido pelo jogador | nada pessoal — o ID não deriva de hardware, não identifica o aparelho, e o apelido é público por natureza | **← a recomendada.** Mantém "nenhuma coleta" de dado pessoal; suficiente para reconectar, leaderboard e progressão local |
| ID de hardware (Android ID, IMEI etc.) | identificador de dispositivo = **dado declarável** na Segurança de Dados da Play | **Não.** Quebra a postura de graça |
| Conta e-mail/senha própria | e-mail = dado pessoal; senha = obrigação de segurança permanente; menor de idade = consentimento de responsável (LGPD) | **Não agora.** Só se um dia a progressão precisar atravessar aparelhos — e aí é decisão de produto do Diretor, registrada no GDD antes do código |
| Google Play Games Services | SDK do Google no pacote + fluxo de conta Google | Contraria "nenhum SDK de terceiro". Fica como decisão futura do Diretor se cross-device virar requisito |

Limitação honesta do ID anônimo: **trocou de aparelho, perdeu o progresso**.
Para um jogo cujo progresso vendável é só cosmético (anti-P2W), isso é
aceitável no lançamento — e é o preço da postura de dados mais barata que
existe.

## 2. Matchmaking

Para o escopo (squads de 2, partidas de 20 slots com 12+ bots preenchendo —
GDD §19.2 e ROADMAP G4), matchmaking é um problema PEQUENO. Não contratar nem
construir plataforma:

- **G5 (teste): lobby por código.** O servidor cria a sala e devolve um código
  curto (4–6 caracteres); a dupla digita o código. Zero serviço extra, e é o
  fluxo natural de squad de amigos — o público BR do jogo.
- **Depois (público): fila única.** Um processo pequeno no MESMO VPS mantém a
  lista de salas abertas; jogador entra na primeira com vaga; bots completam
  os 20 slots (o preenchimento por bot já é feature do G4, não da infra).
  Sem MMR/ranking de habilidade no lançamento — com bots preenchendo, fila
  única sustenta partida boa muito antes de existir massa para pareamento
  por habilidade.
- MMR, regiões múltiplas, party > 2: futuro, só com base de jogadores que o
  justifique.

## 3. Leaderboard

- Entra quando houver partidas públicas (não no primeiro teste G5).
- **SQLite no próprio VPS** resolve por muito tempo: uma tabela
  (`id_anônimo, apelido, vitórias, selos`). Nada de banco gerenciado pago no
  começo.
- **Nenhum dado pessoal**: apelido + números de jogo. É o que mantém o
  formulário de Segurança de Dados trivial mesmo com backend.
- Moderação de apelido (10+ obriga a pensar nisso): filtro de palavras no
  servidor na criação/renomeação. Simples, no código do jogo, não é serviço.

## 4. Anti-cheat honesto

O que o **servidor autoritativo já resolve** — a lição que o projeto Roblox
provou e que atravessa a ponte como princípio (`docs/ANDROID.md` §2):

- Cliente envia **input/intenção**; o servidor simula movimento, dano, mana,
  cooldown, loot e terreno. Cliente nunca afirma "acertei" — pede "disparei
  nesta direção".
- Validações baratas no servidor: velocidade máxima de movimento, cadência
  máxima de disparo, alcance, custo de mana, cooldown — tudo já é dado de
  `Balance`, o servidor só recusa o que o número não permite.
- Resultado: as classes clássicas de cheat (dano infinito, mana infinita,
  teleporte, item duplicado) **morrem de graça**.

O que ele **não** resolve — e a resposta honesta para cada um:

| Ameaça | Realidade | Resposta proporcional |
|---|---|---|
| Aimbot / mira assistida externa | servidor não distingue mira perfeita humana de sintética | estatística server-side (taxa de acerto anômala) + report de jogador. Aceitar que indie mobile não elimina isso |
| Cliente modificado (wallhack via remoção de oclusão etc.) | o cliente conhece o que o servidor replica | mitigar replicando só o necessário (interest management) — otimização de rede que já é boa por si |
| Lag switching | UDP tolera perda; abuso é detectável por padrão | timeout + limites de reconexão |
| Play Integrity API (atestado do Google) | é SDK/serviço do Google no pacote | **decisão futura do Diretor**, só se cheating virar problema real medido — hoje contraria a postura de pacote limpo |

Kernel anticheat, terceiros pagos (BattlEye etc.): fora de escala e de
orçamento para este projeto. Não documentar como se fosse opção.

## 5. Crash reporting compatível com 10+/LGPD

- **Camada zero, já paga com a conta da loja: Android vitals no Play
  Console.** Crashes e ANRs aparecem lá **sem nenhum SDK no pacote** — o dado
  vem de aparelhos cujos donos aceitaram telemetria do próprio Android para o
  Google. Custo zero, coleta zero no NOSSO pacote. Para o tamanho deste
  projeto, isso cobre 90% da necessidade.
- **Camada um, local:** o Godot grava log em `user://logs/`. Tela de
  Configurações pode oferecer "copiar log" para o jogador colar num report
  manual (e-mail/Discord). Coleta zero, opt-in por definição.
- **Camada dois, só se um dia precisar:** Sentry/GlitchTip **self-hosted** no
  próprio VPS (dado fica em servidor próprio, não em terceiro), com envio
  **opt-in** e sem identificadores. É decisão futura do Diretor — hoje não
  entra, porque qualquer envio automático de diagnóstico vira linha no
  formulário de Segurança de Dados e parágrafo na política de privacidade.

## 6. O que o G5 muda na declaração "nenhuma coleta" — dizer a verdade

Multiplayer conecta o aparelho a um servidor: o **endereço IP** do jogador
passa a ser processado (transitoriamente, para a conexão existir). Isso não
quebra a postura, mas muda o texto:

- Política de privacidade (ver `04-distribuicao.md`) passa a dizer: IP
  processado apenas para operar a partida, não armazenado além de logs
  técnicos de curta duração, não compartilhado. Base legal LGPD: execução do
  serviço solicitado.
- Formulário da Play: continua sem coleta de dado pessoal persistente se o
  backend seguir este documento (ID anônimo + apelido + números de jogo).
  **Verificar na política vigente** como a Play classifica ID gerado por app
  na data do envio.
- Regra de ouro operacional: **logs do servidor sem IP + apelido juntos além
  do necessário, e com rotação curta.** O melhor jeito de não vazar dado é
  não retê-lo.
