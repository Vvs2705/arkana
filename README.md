# ARKANA

Battle royale de magos, em terceira pessoa, **para celular, feito em Unity 6**.

---

## A estrutura

```
ARKANA/
├── design/        ← A DECISÃO. Não tem engine, não tem código.
├── arte/          ← A MATÉRIA-PRIMA. .glb, .png, .wav e as ferramentas que produzem.
│
└── mobile-unity/  ← O PRODUTO (Unity 6, Android). Ver mobile-unity/00-LEIA.md.
```

**A regra, decidida pelo Diretor em 27/08/2026 e mantida em 09/09:**

> `design/` e `arte/` **alimentam as implementações**.
> As implementações **nunca cruzam código entre si.**

As versões anteriores (protótipo em Roblox e o jogo em Godot que serviu de
referência para a reescrita) saíram da árvore em 01/10/2026 e seguem no
**histórico do git** (`git log -- roblox mobile-godot`).

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

---

## Para quem vai analisar o repositório

Modelos, texturas e áudio (~1,4 GB) ficam no **Git LFS**. Para ler só código e documentos, clone sem baixá-los:

```bash
GIT_LFS_SKIP_SMUDGE=1 git clone https://github.com/Vvs2705/arkana.git
```

Para abrir o projeto no Unity é preciso o clone completo (`git lfs install` e depois `git clone`).
Unity **6000.3.23f1** com o módulo Android; o projeto fica em `mobile-unity/`.

## Comece por aqui

**[`design/PROJETO.md`](design/PROJETO.md)** — a memória do projeto, com a seção
CONTINUAR DAQUI sempre no topo. É o único arquivo que precisa ser lido para saber
onde as coisas estão.

## Estado (04/10/2026)

| | |
|---|---|
| **Plataforma alvo** | Android (aparelho de teste: Poco F4) |
| **Engine** | Unity 6000.3.23f1 (URP), com o módulo Android |
| **Ilha** | a do **Documento Mestre** ([`design/cenario/DOCUMENTO-MESTRE.md`](design/cenario/DOCUMENTO-MESTRE.md)): 4,8 × 4,4 km, 12 regiões, zona industrial, base militar e subterrâneo, montada por código em `mobile-unity/Assets/_Arkana/Scripts/World/IlhaMestre*.cs` |
| **Arte 3D** | kit geométrico por script no Blender (`arte/tools/blender/`) + peças orgânicas geradas no Tripo Studio e otimizadas no Blender ([`design/cenario/TRIPO-STUDIO.md`](design/cenario/TRIPO-STUDIO.md)) |
| **Elenco** | os 20 magos no jogo, com kit e 11 clipes cada (`Resources/magos/`) |
| **Testes** | Unity Test Framework em `-batchmode` (`mobile-unity/portao.ps1`): 526 testes EditMode + PlayMode verdes |
| **Rede** | **ainda não existe** — é o item mais caro do roadmap |

O histórico detalhado de cada fase, com o que falta, está em
[`design/PROJETO.md`](design/PROJETO.md) (seção CONTINUAR DAQUI).
