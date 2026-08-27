# DADOS E MENORES — por que o público 10+ desenha o backend

> **Este não é um documento de papelada.** A faixa etária escolhida no GDD é uma
> restrição de **arquitetura**. Descobrir isso depois de o backend estar pronto
> significa refazer o backend — e, no pior caso, ter o app removido da loja com
> a base de jogadores já formada.

---

## 1. A decisão que já foi tomada

**GDD §9, Diretor, 19/08/2026: público-alvo 10+.**

No momento em que um jogo declara que crianças são público, ele entra num regime
diferente do resto dos aplicativos — em quase todo lugar do mundo, e no Brasil
também. Não é opcional e não depende de o jogo ser bonzinho: **depende de quem
ele diz que quer alcançar.**

---

## 2. O que isso proíbe, na prática

Estas são as amarras que mudam código, e não texto de política:

**a) Identificador de publicidade.** Em jogo dirigido a crianças, não se coleta
o identificador de anúncio do aparelho. Muitas ferramentas de análise pegam esse
identificador **por padrão, sem perguntar**. Instalar um kit de análise "só para
ver quantas pessoas jogam" pode violar a regra sem ninguém perceber — o kit faz
sozinho.

**b) Anúncio comportamental.** Se um dia houver anúncio, ele não pode ser
baseado em perfil. Só anúncio contextual.

**c) Coleta mínima.** Nome real, e-mail, telefone, foto, localização precisa,
lista de contatos: **nada disso deve entrar no jogo.** E o melhor jeito de
cumprir uma regra sobre um dado é **não ter o dado**.

**d) Chat livre é o item mais perigoso da lista.** Texto livre entre menores
exige moderação de verdade, com pessoas — não com filtro de palavrão. O GDD já
resolve isso a favor: a comunicação do Arkana é o **gesto de Sintonia**, e
existe um sistema de falas prontas. **Manter assim é uma decisão de produto boa
E uma decisão jurídica boa.** Abrir chat livre depois é abrir um problema que
não fecha.

**e) Nenhuma caixa aleatória paga.** Já está travado no GDD §19.1. Além de ser
regra de loja para o público infantil, é lei em vários países.

---

## 3. O que isso obriga

- **Consentimento de responsável**, se algum dia houver conta com dado pessoal.
  Caro de fazer certo. **A saída barata e honesta é não precisar dele:** conta
  anônima, identificador gerado pelo próprio aparelho, apelido escolhido sem
  ligação com identidade real.
- **Declarar na ficha da loja** o que se coleta e para quê — e a declaração
  precisa bater com o que o app faz de verdade. Declaração que não bate é
  motivo de remoção.
- **Política de privacidade pública**, num endereço estável, escrita em
  português claro. É exigência da loja, não formalidade.
- **Caminho para apagar dados.** Se existe dado de jogador, precisa existir um
  jeito de apagá-lo mediante pedido. É a LGPD, e vale para menores com mais
  força.

---

## 4. A saída que custa menos e vale mais

**Projetar o backend para não ter dado pessoal nenhum.**

| Em vez de | Usar |
|---|---|
| Login com e-mail/rede social | identificador anônimo gerado no primeiro boot |
| Nome do jogador | apelido escolhido, sem verificação, sem ligação com identidade |
| Chat de texto livre | gesto de Sintonia + falas prontas (já é o design) |
| Análise com identificador de anúncio | contagem agregada, sem identificar aparelho |
| Relatório de erro com dado do usuário | só pilha de execução e modelo do aparelho |

Com essas cinco escolhas, quase toda a obrigação acima deixa de se aplicar —
**não porque foi contornada, mas porque o dado não existe.** É a diferença entre
proteger um cofre e não ter o que guardar.

E há um bônus prático: **isso é mais barato e mais rápido de construir.** A
opção juridicamente segura aqui é também a de menos código.

---

## 5. O que decidir antes de escrever a primeira linha de backend

Estas quatro perguntas mudam o desenho. Respondê-las depois custa reescrita:

1. **O jogador vai ter conta?** Se sim, o progresso sobrevive à troca de
   aparelho — e entra consentimento. Se não, o progresso vive no aparelho e
   morre com ele. *Recomendação: começar sem conta.*
2. **Vai existir apelido visível para outros?** Se sim, precisa de filtro de
   nome ofensivo e de um jeito de denunciar. *Recomendação: sim, mas com lista
   de nomes gerados no começo.*
3. **Vai existir amizade ou grupo?** Traz o problema de adulto encontrar
   criança. *Recomendação: adiar até depois do degrau 3.*
4. **Vai existir compra dentro do app?** Muda tudo: ficha da loja, imposto,
   e regra específica para menores. *Recomendação: nada de compra antes do
   jogo estar de pé.*

**Nenhuma dessas quatro está decidida.** São do Diretor, e estão listadas aqui
para não serem decididas por inferência no meio de uma implementação — que é
exatamente como o projeto já se machucou antes.
