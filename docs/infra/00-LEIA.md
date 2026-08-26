# INFRA — o que existe por trás do jogo quando ele ficar pronto

> **Pedido do Diretor (26/08/2026):** *"quero também que desenvolva uma pasta com
> arquivos técnicos, como servidores para usar, tudo para quando o jogo ficar
> pronto, termos por detrás as ferramentas que são necessárias."*

Esta pasta responde a isso. Ela **não é um plano de compra** — é o mapa do que
precisa existir, em que ordem, quanto custa, e o que só o Diretor pode fazer.

---

## O fato que muda todas as respostas

**O Arkana em Godot não tem UMA linha de rede.**

Conferido em 26/08 com busca no código inteiro: nenhum `@rpc`, nenhum
`multiplayer`, nenhum `ENetMultiplayerPeer`, nenhum `HTTPRequest`, nenhum
`WebSocket`. A partida de hoje é o jogador mais **6 bots que rodam no próprio
aparelho**. Não há cliente, não há servidor, não há nada para hospedar.

Isso importa porque a pergunta *"qual servidor a gente usa?"* tem uma resposta
inútil enquanto isso for verdade: **nenhum**. Contratar servidor agora é alugar
garagem para um carro que ainda não foi construído — e a mensalidade começa a
correr no mesmo dia.

O que **de fato** separa o Arkana do multijogador não é a escolha do provedor.
É reescrever a camada de partida para que o servidor mande e o aparelho apenas
mostre. Isso está em [01-MULTIJOGADOR.md](01-MULTIJOGADOR.md), e é a leitura
que deve vir antes de qualquer decisão de infraestrutura.

---

## A ordem certa das coisas

O GDD §8 já traz uma escada de escopo, e ela continua valendo. Esta pasta se
apoia nela em vez de inventar outra:

| Degrau | O que é | Rede? | Infra que precisa |
|---|---|---|---|
| **1 — hoje** | Partida contra bots, tudo no aparelho | nenhuma | **nada** |
| **2** | Arena 1v1/2v2, salas pequenas | mínima | 1 servidor pequeno, ou nem isso (ver 01) |
| **3** | Mini-BR, 16–20 jogadores | de verdade | servidor dedicado por partida + matchmaking |
| **4** | A visão completa em 3D | pesada | escopo de estúdio |

**O Arkana está no degrau 1.** Todo custo recorrente de servidor pertence ao
degrau 2 em diante. O que pertence a HOJE são as contas de publicação e as
ferramentas de build — que custam pouco e têm prazo próprio (ver
[03-CONTAS-E-CUSTOS.md](03-CONTAS-E-CUSTOS.md)).

---

## O que tem nesta pasta

| Arquivo | Responde |
|---|---|
| [01-MULTIJOGADOR.md](01-MULTIJOGADOR.md) | O que precisa ser construído ANTES de qualquer servidor importar. A arquitetura, e por que ela não é opcional. |
| [02-SERVIDORES.md](02-SERVIDORES.md) | Onde hospedar, comparado de frente, com o que cada opção cobra e o que cada uma esconde. |
| [03-CONTAS-E-CUSTOS.md](03-CONTAS-E-CUSTOS.md) | Cada conta a abrir, quanto custa, quem abre, e o dinheiro real em três escalas. |
| [04-DADOS-E-MENORES.md](04-DADOS-E-MENORES.md) | O público 10+ do GDD é uma restrição JURÍDICA que desenha o backend. Ignorar isso derruba o app da loja. |

---

## Regra desta pasta: preço tem data

Todo valor aqui está marcado com **quando foi anotado** e **onde conferir**.
Preço de nuvem muda sem aviso e sem changelog. Nenhum número desta pasta deve
ser usado para pagar nada sem abrir a página oficial antes.

Se um documento daqui disser um preço sem data e sem link, ele está errado —
conserte em vez de confiar.
