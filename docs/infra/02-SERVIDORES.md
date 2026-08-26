# SERVIDORES — onde hospedar, e o que cada escolha esconde

> Leia [01-MULTIJOGADOR.md](01-MULTIJOGADOR.md) antes. Enquanto o jogo não
> tiver rede, nada aqui deve ser contratado.

---

## 1. A variável que decide, e não é o preço

**Latência até o jogador.** Um servidor barato na Alemanha e um servidor caro em
São Paulo não são o mesmo produto com preços diferentes — são um jogo injogável
e um jogo jogável.

Da maior parte do Brasil:

| Servidor em | Ida-e-volta típica | O que o jogador sente |
|---|---|---|
| São Paulo | 10–40 ms | normal |
| Virgínia / Miami (EUA) | 110–160 ms | perceptível, tolerável em jogo de projétil |
| Europa | 180–250 ms | ruim |

O Arkana ajuda aqui: as magias são **projétil com tempo de voo**, não hitscan
(GDD §pilares). Tempo de voo esconde latência — o mesmo atraso que arruinaria um
jogo de tiro instantâneo passa despercebido quando a bola de fogo leva 300 ms
para chegar. Isso compra folga. **Não compra 200 ms.**

**Regra:** o servidor fica onde estão os jogadores. Para um jogo brasileiro,
começa em São Paulo. Só isso já elimina metade das opções baratas do mercado,
e é melhor descobrir agora do que depois de migrar.

### Como medir de verdade, em vez de acreditar nesta tabela

Antes de assinar qualquer coisa, meça da SUA conexão para a região candidata:

```bash
ping -n 20 <endereco-de-teste-da-regiao>
```

Quase todo provedor publica um arquivo de teste ou um IP de *looking glass* por
região. Meça as três candidatas no mesmo dia, no horário em que o jogo será
jogado (à noite, quando a rede está cheia). Uma medida sua vale mais que
qualquer número deste documento.

---

## 2. Os quatro caminhos, comparados de frente

### A) VPS única, você administra

Uma máquina virtual. Você instala, você atualiza, você reinicia.

- **Custo:** a faixa mais baixa do mercado. Uma máquina pequena roda várias
  partidas simultâneas de 16–20 jogadores, porque um BR sem física pesada é
  mais barato do que parece.
- **A favor:** preço fixo e previsível, sem surpresa no fim do mês. Total
  controle. É onde se aprende de verdade como o servidor se comporta.
- **Contra:** você é o plantão. Se cair às 23h de sábado, quem levanta é você.
  E não escala sozinha: cheia é cheia.
- **Quando:** degrau 2, e o começo do degrau 3. **É por aqui que se começa.**

### B) Orquestrador de partidas (sobe um servidor por partida)

Serviços que ligam uma instância quando a partida começa e desligam quando
acaba, cobrando pelo tempo em que rodou.

- **Custo:** por minuto de partida. Com pouca gente, sai barato; a conta cresce
  junto com o sucesso, que é o formato certo de risco.
- **A favor:** escala sem você acordar. Costumam ter várias regiões, o que
  resolve latência de graça.
- **Contra:** amarra o jogo ao jeito daquele serviço, e o preço por minuto só
  é comparável depois que você sabe **quanto tempo dura uma partida e quantas
  rodam juntas** — números que hoje não existem.
- **Quando:** degrau 3, quando houver jogador real o bastante para a máquina
  fixa encher.

### C) Nuvem grande (AWS / Google Cloud / Azure)

- **A favor:** todas têm São Paulo. Ferramenta para tudo.
- **Contra:** a conta é uma linguagem própria, e a parte que pega ninguém é a
  **transferência de dados de saída** — barata por gigabyte, cara quando o jogo
  manda estado 20 vezes por segundo para 20 jogadores durante meses. É o item
  que estoura orçamento de dev solo.
- **Quando:** só com tração de verdade, e com alguém olhando a fatura toda
  semana.

### D) Camada gratuita permanente

Alguns provedores mantêm uma faixa grátis para sempre, e pelo menos um deles
oferece máquina ARM com folga real numa região brasileira.

- **A favor:** zero reais para o degrau 2 inteiro. Para aprender netcode, é
  difícil bater.
- **Contra, e é sério:** capacidade grátis é a primeira a faltar e a primeira a
  ser recuperada. **Serve para aprender e testar, nunca para o dia do
  lançamento.** Tratar isso como produção é como confiar num carro emprestado.
- **Quando:** degrau 2. **Confira a disponibilidade da região no dia** — esta é
  a informação que mais envelhece nesta pasta.

---

## 3. O caminho recomendado

1. **Degrau 2 — arena.** Camada gratuita ou a VPS mais barata com região
   brasileira. Objetivo: aprender netcode sem mensalidade. Meça latência real
   entre dois celulares antes de qualquer outra coisa.
2. **Degrau 3, começo.** Uma VPS paga em São Paulo, dimensionada pelo que a
   medição do degrau 2 mostrar — não por chute. **Só aqui existe o primeiro
   custo recorrente honesto do projeto.**
3. **Degrau 3, com jogador de verdade.** Se e quando a máquina fixa encher, aí
   sim comparar orquestrador, com número de partida simultânea na mão.

**Nunca pule para o passo 3.** Sem duração de partida e pico de simultâneos
medidos, qualquer comparação de preço é ficção.

---

## 4. O que precisa existir junto com o servidor

Não é só a máquina. Estas quatro coisas aparecem no dia 1 do degrau 3 e
costumam ser esquecidas no orçamento:

| Peça | Para quê | Pode esperar? |
|---|---|---|
| **Matchmaking** | juntar 16–20 pessoas numa partida | não — sem isso não há partida |
| **Registro de partida** | saber quantas rodaram, quanto duraram, quantas caíram | não — é o que dimensiona tudo |
| **Relatório de erro** | saber que travou no aparelho do jogador, não no seu | não — sem isso você conserta às cegas |
| **Atualização de números sem republicar** | corrigir equilíbrio sem esperar a loja | sim, mas cedo demais é tarde demais |

Os três primeiros são pequenos e obrigatórios. O quarto é conforto, e conforto
espera.

---

## 5. Segurança, no mínimo indispensável

Quando existir servidor, existe superfície de ataque. O mínimo que **não** é
paranoia:

- **O servidor é a autoridade.** Nunca aceitar do cliente posição, dano ou
  morte — só entrada. Isso é arquitetura (ver 01), não configuração.
- **Limite de taxa por conexão.** Um cliente que manda 10.000 pacotes por
  segundo derruba a partida de todo mundo, e não precisa ser mal-intencionado
  para isso: basta estar quebrado.
- **Nada de segredo dentro do APK.** Qualquer chave que o app carrega é
  pública, porque o APK é um arquivo `.zip` que qualquer um abre. Chave de
  serviço fica no servidor e só lá.
- **Uma conta de administração por pessoa, com segundo fator.** Contas
  compartilhadas não têm dono, e o que não tem dono não tem responsável.
