# ARKANA

Battle royale de magos, em terceira pessoa, **para celular, feito em Unity 6**.

---

## A estrutura

```
ARKANA/
├── design/        ← A DECISÃO. Não tem engine, não tem código.
├── arte/          ← A MATÉRIA-PRIMA. .glb, .png, .wav e as ferramentas que produzem.
│
├── mobile-unity/  ← O PRODUTO (Unity 6, Android). Nasce no passo 0 do CONTINUAR DAQUI.
├── mobile-godot/  ← REFERÊNCIA. O jogo que funcionou em Godot; a fonte da reescrita. Sai quando o Unity o alcançar.
└── roblox/        ← O projeto-mãe. Intocado.
```

**A regra, decidida pelo Diretor em 27/08/2026 e mantida em 09/09:**

> `design/` e `arte/` **alimentam as implementações**.
> As implementações **nunca cruzam código entre si.**

O GDScript do Godot **não se traduz** para C#: se relê a decisão em `design/`,
se olha como o Godot resolveu, e se escreve de novo em Unity. O que atravessa
é a decisão, a arte e o **número medido**.

---

## A decisão de 09/09/2026: Unity, e só Unity

Entre 27/08 e 09/09 o projeto tentou virar PC/Steam em Unreal 5. Foi desfeito
por ordem do Diretor: uma pessoa, dois jogos (este e o Limiar), **uma engine**.
Unity faz os dois. O Unreal, o Lyra e a ilha montada lá foram apagados; o que
sobrou daquele desvio está registrado em `design/PROJETO.md` como lição.

*"Não pretendo mudar mais."* Esta é a última troca de engine do projeto.

---

## O que tem em cada pasta

| Pasta | O que é | Diretriz |
|---|---|---|
| **`design/`** | GDD, kits, dano, elenco, moeda, fichas dos 20 magos, estudo da Zona, referências, infra | **Fonte da verdade.** Muda aqui primeiro, implementa depois. Nunca o contrário |
| **`arte/`** | concepts, `.glb`, áudio, prompts e `tools/` (Meshy, Blender) | Matéria-prima, não produto. Nada aqui é específico de engine |
| **`mobile-unity/`** | o jogo | Unity 6000.3, URP, Android. Portão: testes do Unity Test Framework em `-batchmode` |
| **`mobile-godot/`** | o jogo anterior, 12 autotestes verdes, orçamento medido | **Referência de leitura.** Não recebe feature. Apaga-se quando o Unity alcançar a paridade |
| **`roblox/`** | o projeto-mãe | Intocado |

---

## Comece por aqui

**[`design/PROJETO.md`](design/PROJETO.md)** — a memória do projeto, com a seção
CONTINUAR DAQUI sempre no topo. É o único arquivo que precisa ser lido para saber
onde as coisas estão.

## Estado (09/09/2026)

| | |
|---|---|
| **Plataforma alvo** | Android (aparelho de teste: Poco F4) |
| **Engine** | Unity 6000.3.23f1, instalado com o módulo Android |
| **mobile-unity** | ainda não existe: é o passo 0 |
| **mobile-godot** | referência, 12/12 autotestes verdes na última execução (04/09) |
| **Rede** | **não existe.** É o item mais caro e ainda não começou |
| **Elenco** | 20 fichas e 160 vistas de concept prontas; modelos 3D a refazer no site da Meshy, um por um |
