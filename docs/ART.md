# ART — direção visual

**O que este documento governa e o que não governa.**

| Assunto | Quem manda | Situação |
|---|---|---|
| Logo, tipografia, paleta, key art | **GDD §10** | normatizado |
| Regras permanentes de legibilidade e cosmético | este documento | normatizado |
| **Estilo de renderização dos personagens** | Diretor (aprovado FORA do repositório) | **norma ainda não fechada aqui** — ver abaixo |

## A nova direção de arte dos personagens

**Estado em 25/08/2026:** o Diretor **aprovou** uma nova direção artística para
os personagens **fora deste repositório**, junto com uma nova entrega de
referências. A norma **não está fechada aqui** porque as entradas ainda não
chegaram ao projeto.

**O que falta receber para fechar a norma:**

1. o documento mestre aprovado da direção de arte;
2. os pacotes de imagens da entrega, com manifesto e checksums.

**Regra ao receber:** copiar o conteúdo do documento mestre **exatamente como
ele veio**. Não reconstruir de memória, não reinterpretar, não "melhorar". A
direção é decisão do Diretor; este arquivo só a registra.

**Enquanto isso não chega**, o que vale como evidência técnica é a auditoria
abaixo — que descreve o lote ANTERIOR e continua útil para não repetir os mesmos
erros de vista e de coerência. Ela **não** é a norma nova.

O ART.md afirmava antes que sua fonte de verdade era "GDD seção 10". Isso estava
errado e é a razão desta tabela existir: a §10 trata de **marca** — logo,
tipografia, paleta e key art. Ela não diz uma palavra sobre estilo de personagem.
Apontar para ela como autoridade do assunto fazia o documento prometer uma decisão
que nunca foi tomada.

---

## Paleta (GDD §10 — vale, é norma)

| Uso | Cor |
|---|---|
| Azul-noite arcano | `#0B1026` |
| Fundo profundo | `#05070F` |
| Dourado da marca | `#F0C75E` |
| Fogo | `#FF5A2A` |
| Água | `#2AA7FF` |
| Terra | `#A8763E` |
| Vento | `#8FE8C9` |
| Raio | `#F5D90A` |

## Regras permanentes

Cada uma existe porque evita um problema concreto.

- **Cada elemento usa cor, forma e som distintos; nunca apenas cor.**
  Evita que daltônico perca a informação de qual magia vem vindo.
- **O clima usa cor e contraste, não falta de luz.**
  Cena escura em celular ao sol vira tela preta; o jogador deixa de ver o inimigo.
- **Materiais e VFX precisam permanecer legíveis em tela pequena.**
  Detalhe que só aparece em zoom é custo de GPU sem retorno de jogo.
- **Cosmético altera apenas aparência, nunca poder.**
  É o juramento anti-P2W; quebrá-lo custa a confiança do jogador.

**Céu e luz** (o `ROADMAP_3D.md` G1 aponta para cá): o que existe de norma são o
azul-noite `#0B1026` e o fundo profundo `#05070F` como base, mais a regra "clima
por cor, não por falta de luz". **Não há especificação de céu, hora do dia ou
esquema de iluminação** — se G1 precisar de uma, ela ainda tem que ser escrita.

---

## Onde a arte vive (verificado em 24/08)

| Caminho | Conteúdo | No git? |
|---|---|---|
| `personagens/NN-slug/arte/_originais/` | as 8 vistas por mago — ateliê, ~307 MB no elenco | **não** |
| `personagens/NN-slug/arte/paleta.svg` e `prompt.txt` | receita da peça | sim |
| `godot/menu/art/NN.png` | retrato 512px — **o que o jogo carrega** | sim |
| `godot/characters/modelos/*.glb` | modelo game-ready | sim (Git LFS) |
| `docs/pipeline-arte/` | pacote de pipeline da Meshy | sim |

**Por que o ateliê fica fora do git:** 307 MB de matéria-prima que o jogo nunca
carrega. A representação leve canônica é `godot/menu/art/NN.png`. Backup do ateliê
são os `.zip` da entrega original, guardados pelo Diretor fora do repositório.

O pacote de pipeline foi achatado para `docs/pipeline-arte/` — antes era uma pasta
dentro de outra de mesmo nome, na raiz do repositório.

---

## Direção de arte dos personagens — EM ABERTO

**Nada aqui está decidido.** Isto é o estado da questão e a evidência levantada.
A decisão é do Diretor.

### O pedido

Sair do "muito realismo" e chegar num "3D mais detalhado alto padrão".

### O que a auditoria dos 20 mediu

O elenco fala **três linguagens visuais diferentes**:

| Linguagem | Magos |
|---|---|
| Escultura 3D | Corvus, Vex, Brok, Gromm, Fizz, Sylva, Basalto (7) |
| Pintura semi-realista | Pyra, Véu, Corvomante, Olho-de-Éter, Vitalis, Tessa, Umbra (7) |
| Anime / manhwa | Ceifadora, Ilusionista, Aelion, Maris, Noctus, Pip (6) |

### O eixo NÃO é realismo

Isto foi medido, não suposto: o rosto do **Brok** — o alvo aprovado — é **mais**
realista e enrugado que o da **Pyra**, a peça rejeitada. Se o eixo fosse realismo,
a aprovação teria ido para o lado contrário.

O que separa de verdade os três grupos:

1. **Superfície pintada × superfície esculpida.** É a diferença dominante.
2. **Sombra de contato.** Os 7 do grupo alvo pisam num chão. Os outros 13 flutuam.
3. **Proporção exagerada.** Mão e bota aumentadas no grupo alvo.

**Perigo registrado:** instruir um gerador com "menos realismo" empurra o resultado
para **anime** — que já é o grupo que mais quebra a coerência do elenco. O pedido
do Diretor, traduzido errado, produz o oposto do que ele quer.

### Defeito medido nas vistas (vale para os 20)

- A `vista-3-4.png` é **a frontal repetida** — 12 de 12 conferidos.
- A `vista-lateral.png` é um três-quartos de ~60–70°.
- **Não existe perfil de 90° no lote.**

**Por que importa:** o fluxo multi-imagem → 3D da Meshy usa o perfil justamente
para resolver a **espessura lateral** do corpo. Sem perfil verdadeiro, mandar as
quatro vistas não melhora o modelo — ver `MESHY.md` §3.

### O que ainda falta para fechar

- Decisão do Diretor sobre a linguagem única do elenco.
- Se a escolha for unificar, decidir o que acontece com os 13 fora do padrão:
  regerar arte, ou aceitar a variação como identidade.
- Reposição do lote de vistas com perfil real de 90°, se a rota for multi-imagem.

Existe uma **proposta** de direção em
`docs/pipeline-arte/PERSONAGENS/00-DIRECAO_VISUAL_PERSONAGENS.md` ("stylized
premium"). É proposta da equipe, **não decisão**. Não trate como norma.

---

## Estado atual

- **Mundo e ilha:** procedurais, com iluminação, tonemap e shaders próprios.
- **Personagens:** Pyra e Brok tem modelo game-ready. Os outros 18 usam o mago
  procedural genérico.

O plano de fases (G1–G4) mora em `ROADMAP_3D.md`; o caminho técnico de personagem
mora em `PASSOS_GRAFICOS.md`. Este documento não repete nenhum dos dois.
