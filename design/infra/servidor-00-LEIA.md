# INFRA — o mapa da pasta

> Escrito em 20/08/2026 por ordem do Diretor: *"Desenvolva uma pasta com
> arquivos técnicos, como servidores para usar, tudo para quando o jogo ficar
> pronto, termos por trás as ferramentas que são necessárias."*
>
> Esta pasta **documenta**; não contrata. Criar conta, assinar serviço e gastar
> dinheiro é **ato exclusivo do Diretor** — a mesma fronteira que o
> `docs/ANDROID.md` já aplica a loja e o `docs/ROBLOX.md` §7 a publicação.

## O princípio: cada peça entra QUANDO a fase pede

O roadmap ativo é `docs/ROADMAP_3D.md`. A infraestrutura obedece a ele, não o
contrário:

| Fase | O que a infra entrega | Custo |
|---|---|---|
| G0–G4 (bots, offline) | **Nada.** O jogo roda inteiro no aparelho. | R$ 0 |
| **G5** (multiplayer real) | Servidor dedicado (1 VPS em São Paulo) + o mínimo de backend | primeiro real gasto |
| **G6** (loja) | Conta Google Play, site com política de privacidade, build assinado | taxa única + R$ 0/mês |

**Nada se contrata antes da fase que usa.** Servidor parado é dinheiro queimado
e superfície de manutenção; conta de loja aberta cedo demais só faz o relógio
de política correr sem build para enviar (a exceção está anotada em
`04-distribuicao.md`: o teste fechado obrigatório é a dependência mais longa —
quando G6 se aproximar, a conta vem primeiro).

## Os arquivos

| Arquivo | Tema | Fase que ativa |
|---|---|---|
| [01-servidores-de-jogo.md](01-servidores-de-jogo.md) | **O tema central**: Godot dedicated server, quanto cabe por vCPU, onde hospedar (SP), recomendação para o primeiro teste | G5 |
| [02-backend-servicos.md](02-backend-servicos.md) | Contas/login, matchmaking, leaderboard, anti-cheat, crash reporting — tudo na postura "nenhuma coleta" | G5–G6 |
| [03-pipeline-build.md](03-pipeline-build.md) | Export automatizado, assinatura de release, versionamento, quando CI passa a valer | G5 (server build) / G6 (release) |
| [04-distribuicao.md](04-distribuicao.md) | Conta de loja, teste fechado, site mínimo com política de privacidade | G6 |
| [05-custos.md](05-custos.md) | **A tabela única**: item · fase · custo hoje · recorrente/único · total por fase | referência |
| [06-ferramentas-locais.md](06-ferramentas-locais.md) | O que a máquina já tem e o que pode faltar adiante | referência |

## Regras que atravessam todos os arquivos

- **Preço muda.** Todo número de dinheiro nesta pasta é o de hoje (20/08/2026)
  e carrega **"verificar preço vigente"**. Documento de infra com preço velho
  custa decisão errada, não uma linha de código.
- **As decisões do projeto mandam** (GDD §9/§19.1): 10+, anti-P2W, zero caixa
  aleatória, **nenhum SDK de anúncio ou rastreamento**, "nenhuma coleta" como
  postura de dados. Nenhuma escolha de infra aqui pode quebrá-las — e várias
  escolhas ficam mais BARATAS por causa delas (menos a declarar, menos a
  proteger, menos a auditar).
- **O motor é Godot 4.4** (`godot/project.godot`). O servidor dedicado é um
  export do MESMO projeto — não existe "backend em outra linguagem" para a
  partida. O que for serviço fora da partida (leaderboard, lobby) é o mínimo
  que couber no mesmo VPS.
- Onde algo depende de decisão futura do Diretor, o arquivo diz **qual
  decisão** — não decide por ele.
