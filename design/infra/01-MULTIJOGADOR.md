# MULTIJOGADOR — o trabalho que vem antes do servidor

Este documento existe para impedir um erro caro e comum: contratar
infraestrutura achando que ela **traz** o multijogador. Não traz. Servidor é
onde o código roda, e o código que precisa rodar lá **ainda não foi escrito**.

---

## 1. O que o Arkana é hoje, tecnicamente

Um processo só, no aparelho. `gameplay/Main.tscn` cria a ilha, o jogador e 6
`Pawn` controlados por IA local. Tudo — posição, dano, zona, baú, terreno
reativo — é decidido dentro do mesmo celular, e nada precisa concordar com
ninguém.

É por isso que o jogo funciona bem e é rápido de mudar. E é exatamente por isso
que ele não vira multijogador com um botão.

---

## 2. O que muda quando entra rede

Num jogo local, **estado e desenho são a mesma coisa**: mudou a variável, mudou
a tela. Em rede isso se parte em três, e cada parte é um trabalho:

**a) Autoridade.** Alguém precisa ser o dono da verdade. Se cada celular decide
o próprio dano, o primeiro jogador que editar a memória vira imortal — e num
jogo com dano em ponto único como o nosso (`Combat.deal`), esse ponto passa a
morar no servidor, não no cliente. **Isso é anti-cheat.** Não é um produto que
se compra depois; é a forma da arquitetura.

**b) Previsão e reconciliação.** O dedo toca, e a resposta precisa ser
imediata — mas a verdade vem de longe, atrasada. O cliente simula na hora,
o servidor confirma depois, e quando discordam o cliente corrige sem o jogador
perceber. Feito mal, o personagem "borracha" e volta no tempo. Este é o item
mais difícil da lista e o que mais separa jogo bom de jogo irritante.

**c) Compensação de atraso.** O alvo que você viu já não está lá. Num jogo de
projétil como o Arkana (GDD: *"magias com projétil, nada hitscan"*) isso é mais
gentil que num jogo de tiro instantâneo — o projétil tem tempo de voo, e tempo
de voo esconde latência. **É uma vantagem real do nosso design**, e vale
lembrar dela quando a escada ficar assustadora.

---

## 3. O que dá para reaproveitar (é mais do que parece)

Nem tudo se perde. O projeto tomou decisões que envelhecem bem em rede:

- **Dano num ponto só.** Já existe. Mover para o servidor é mover um arquivo,
  não caçar cem lugares.
- **Números fora do código** (`core/Balance.gd`, `core/Kits.gd`). Servidor e
  cliente podem ler a MESMA tabela — é isso que impede o clássico "no meu
  celular dá 40 de dano e no dele dá 35".
- **Sinais centralizados** (`core/Bus.gd`). A HUD já não sabe de onde vem o
  evento; trocar a origem de "local" para "veio da rede" não mexe na HUD.
- **A zona é determinística.** Raio em função do tempo, com semente. Dois
  aparelhos com a mesma semente desenham a mesma zona sem trocar um byte.
- **A queda do castelo já é determinística e semeada** (`SEED_BOTS`), o que
  significa que a fase mais espetacular da partida é também a mais barata de
  sincronizar.

Isso não é sorte: são as regras duras do projeto pagando juros.

---

## 4. As duas arquiteturas possíveis, sem enrolação

### A) Autoritativo dedicado — o servidor roda o jogo

O Godot exporta um **servidor headless** do MESMO projeto. Ele simula a partida
inteira e manda estado; os celulares só mostram e mandam entrada.

- **A favor:** é a única forma honesta de ter anti-cheat. É o que todo BR de
  verdade faz. E o Godot já sabe fazer isso — não precisa de engine nova.
- **Contra:** custa dinheiro por partida em curso, e é o caminho mais longo.

### B) Host-cliente — um dos celulares manda

Um jogador é o servidor. Os outros conectam nele.

- **A favor:** custo de servidor perto de zero. Rápido de subir.
- **Contra:** o host **é** a autoridade — ele pode trapacear, e quando ele cai,
  a partida cai junto. Some ainda o problema de rede doméstica: quase todo
  celular está atrás de NAT, então na prática ainda é preciso um servidor de
  encontro (relay/TURN) para os dois lados se acharem. O "custo zero" é menor
  do que parece, não é zero.

### Recomendação

**Degrau 2 (arena 1v1/2v2): B.** Aprender netcode com 2 jogadores num mapa
minúsculo, sem conta de nuvem correndo. O objetivo do degrau 2 é aprender, e
aprender barato.

**Degrau 3 (mini-BR 16–20): A, obrigatoriamente.** Com 20 jogadores e itens no
chão, host-cliente vira festa de trapaça e a primeira partida com um trapaceiro
mata a reputação do jogo. Aqui não há atalho.

---

## 5. Ferramentas — o que usar e o que não usar

**Rede em si: a multiplayer de alto nível do próprio Godot** (`SceneMultiplayer`
+ `ENetMultiplayerPeer`). Está no motor, é a mesma linguagem do resto do
projeto, e não adiciona dependência. Só sair dela se um problema medido pedir.

**Contas, matchmaking, dados de jogador: adiar.** É tentador montar backend
antes de ter partida em rede. Não monte. Enquanto o degrau 2 não estiver de pé,
qualquer serviço de conta é código morto pagando hospedagem.

**Quando chegar a hora, avaliar servidor de jogo pronto** (open-source,
auto-hospedável, com SDK de Godot) **contra escrever o mínimo**. Um servidor de
jogo pronto traz conta, amigos, times, matchmaking e placar de uma vez; escrever
o mínimo evita carregar um sistema inteiro para usar 5% dele. **A escolha só é
honesta depois que o degrau 2 mostrar o que a partida realmente precisa** — e
por isso não está decidida aqui.

**Não usar:** nada que exija reescrever o jogo em outra engine, e nada que
cobre por jogador ativo antes de existir o primeiro jogador ativo.

---

## 6. Como saber que está pronto para o servidor

Três sinais, nesta ordem. Nenhum é opinião:

1. Duas instâncias do jogo, na mesma rede, jogam uma partida inteira sem
   divergir — e existe um teste que prova isso.
2. A simulação roda **sem desenhar nada** (export headless do servidor sobe e
   completa uma partida com bots).
3. O FPS foi medido **no aparelho** com a partida em rede. Este projeto nunca
   mediu FPS em aparelho nenhum; entrar em rede sem essa medida é empilhar
   incerteza sobre incerteza.

Enquanto os três não estiverem verdadeiros, [02-SERVIDORES.md](02-SERVIDORES.md)
é leitura, não compra.
