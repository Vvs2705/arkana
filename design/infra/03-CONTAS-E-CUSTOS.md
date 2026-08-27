# CONTAS E CUSTOS — o que abrir, quem abre, quanto custa

> **Toda conta desta página é ato do Diretor.** Registro de empresa/pessoa,
> cartão, aceite de termos e resposta a questionário legal têm consequência
> jurídica e financeira em nome dele. A equipe prepara, orienta e verifica —
> **não clica em "aceito" e não digita dado de pagamento.**

---

## 1. O que precisa existir HOJE (degrau 1, sem rede)

Estas três não dependem de servidor nenhum e já têm prazo próprio.

### 1.1 Conta de publicação na loja Android

- **Custo:** taxa **única** de registro, na casa de dezenas de dólares. Não é
  mensalidade. *(anotado 26/08/2026 — confira em play.google.com/console antes
  de pagar)*
- **Prazo escondido, e é o que dói:** contas novas passam por **verificação de
  identidade**, e contas de pessoa física criadas recentemente precisam de um
  **período de teste fechado com um número mínimo de testadores por um número
  mínimo de dias** antes de poderem publicar para o público. Isso não se compra
  nem se acelera: **conta-se em semanas.**
- **Portanto:** abrir esta conta é a coisa mais cedo da lista, mesmo com o jogo
  longe de pronto. O relógio dela corre em paralelo ao desenvolvimento, e é o
  único item aqui que **atrasa o lançamento se for deixado para o fim**.
- **Quem faz:** o Diretor. Precisa de documento e dado de pagamento dele.

### 1.2 Chave de assinatura de release

O APK de hoje é **debug**, assinado com a chave pública de depuração do Android
(`androiddebugkey`) — a mesma do mundo inteiro. Serve para testar no celular
dele e para nada além disso.

Para a loja é preciso uma chave de release. E vale saber antes:

- **Perder essa chave é perder o app.** Sem ela não existe atualização; o
  caminho é publicar outro app, com outro endereço, e pedir para todo mundo
  reinstalar. Não há suporte que resolva.
- **Custo:** zero.
- **Quem faz:** o Diretor gera e guarda **fora deste repositório**, em pelo
  menos dois lugares. Nunca em pasta sincronizada com o projeto, nunca em
  conversa, nunca em anexo de e-mail.
- **Estado:** `godot/export_presets.cfg` tem `keystore/release` **vazio de
  propósito**, e o cabeçalho do arquivo proíbe criar preset de release sem
  ordem dele. Isso continua assim até ele decidir.

### 1.3 Classificação etária

O GDD trava o público em **10+** (§9, decisão do Diretor de 19/08). A loja
exige um questionário de classificação, e no Brasil a classificação é atribuída
por órgão oficial.

- **Custo:** zero.
- **Quem responde:** **o Diretor, pessoalmente.** É declaração formal sobre o
  conteúdo do jogo, e responder errado tem consequência — inclusive remoção.
- **Já é verdade a favor:** combate de fantasia, sem sangue, sem gore e sem
  caixa aleatória (GDD §19.1). O jogo foi desenhado para caber nessa faixa.
- **Cuidado:** a faixa 10+ **muda o backend inteiro.** Ver
  [04-DADOS-E-MENORES.md](04-DADOS-E-MENORES.md) — isso não é papelada, é
  arquitetura.

---

## 2. O que só existe do degrau 2 em diante

| Conta | Para quê | Quando | Custo |
|---|---|---|---|
| Provedor de servidor | rodar a partida | degrau 2 (grátis) / degrau 3 (pago) | ver [02](02-SERVIDORES.md) |
| Registro de domínio | endereço fixo do serviço | degrau 3 | poucas dezenas de reais por ano |
| Certificado TLS | conexão cifrada | degrau 3 | zero (Let's Encrypt) |
| Relatório de erro | saber que travou no aparelho alheio | degrau 3 | faixa gratuita costuma bastar |
| Banco de dados de jogador | progresso, cosmético, estatística | degrau 3 | faixa gratuita no começo |

**Nada disso deve ser aberto antes da hora.** Conta aberta cedo é conta que
expira, que cobra sem uso, ou que fica com senha esquecida — e todas as três
custam mais do que abrir na hora certa.

---

## 3. O dinheiro, em três cenários

Valores em ordem de grandeza, para decidir — não para orçar. **Confira cada um
na fonte antes de gastar.** *(anotado em 26/08/2026)*

### Cenário A — hoje (degrau 1)
| Item | Por mês |
|---|---|
| Servidor | R$ 0 |
| Ferramentas (Godot, Blender, Git) | R$ 0 |
| **Total recorrente** | **R$ 0** |

Só a taxa única da conta de loja, quando ele decidir abri-la. **O Arkana hoje
não tem custo recorrente nenhum, e isso é uma posição boa** — dá para
desenvolver o tempo que for preciso sem sangrar.

### Cenário B — degrau 2 (arena, poucos testadores)
| Item | Por mês |
|---|---|
| Servidor (camada gratuita ou VPS mínima) | R$ 0 a ~R$ 40 |
| Domínio (opcional aqui) | ~R$ 5 |
| **Total** | **até ~R$ 45** |

### Cenário C — degrau 3 (mini-BR, jogadores reais)
| Item | Por mês |
|---|---|
| VPS em São Paulo | ~R$ 60 a R$ 250, conforme simultâneos |
| Domínio + TLS | ~R$ 5 |
| Erro + banco (faixa gratuita) | R$ 0 até crescer |
| **Total** | **~R$ 65 a R$ 260** |

**A linha que ninguém prevê é a transferência de dados de saída.** Um BR manda
estado dezenas de vezes por segundo para cada jogador. Numa VPS com franquia
generosa isso some no preço fixo; numa nuvem grande que cobra por gigabyte, é o
item que dobra a conta. **É o motivo do passo 2 do plano de servidores ser VPS
de preço fixo**, e não a nuvem "profissional".

---

## 4. Ferramentas de desenvolvimento — o que já se usa

Registro do que o projeto depende hoje, para não se descobrir dependência
paga no meio de uma entrega:

| Ferramenta | Papel | Custo |
|---|---|---|
| Godot 4.4.1 | motor | grátis, código aberto |
| Android SDK + JDK 21 | gerar o APK | grátis |
| Git + Git LFS | versão e binários | grátis; LFS tem cota (ver abaixo) |
| GitHub Actions | rodar os 12 autotestes a cada PR | grátis em repositório público |
| Blender | refino de malha | grátis, código aberto |
| Meshy | concept → 3D riggado | crédito por personagem |

**A cota de LFS é a única que já tem risco real e data marcada.** Com as 160
referências de arte versionadas em 26/08, o repositório passou a puxar ~350 MB
num clone limpo. A faixa gratuita do GitHub é de 1 GB de armazenamento e 1 GB
de banda por mês — **cabem uns 3 clones limpos por mês.** Enquanto for só o
Diretor e a máquina de CI, sobra. No dia em que entrar mais gente, ou compra-se
pacote de dados, ou as artes saem para armazenamento externo. **Não há terceira
saída, e é melhor escolher antes de estourar do que depois.**
