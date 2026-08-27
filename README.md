# ARKANA

Battle royale de magos. **Três implementações, uma fonte de decisão.**

---

## A estrutura, e a regra que a sustenta

```
ARKANA/
├── design/        ← A DECISÃO. Não tem engine, não tem código.
├── arte/          ← A MATÉRIA-PRIMA. .glb, .png, .wav e as ferramentas que produzem.
│
├── pc-unreal/     ← O PRODUTO (Unreal 5). Servidor autoritativo, EOS, EAC.
├── mobile-godot/  ← CONGELADO. Referência viva do que funcionou em celular.
└── roblox/        ← O projeto-mãe.
```

**A regra, decidida pelo Diretor em 27/08/2026:**

> `design/` e `arte/` **alimentam as três bases**.
> As três bases **nunca cruzam código entre si.**

Ou seja: **nada de reaproveitamento forçado.** Se portar custa mais que
reescrever, reescreve. O que se reaproveita é a decisão e a arte — porque
decisão não tem engine e `.glb` abre em qualquer lugar.

### Por que essa regra existe

Porque a alternativa é pior. Um código que serve às três acaba servindo mal às
três, e no dia em que uma delas precisa de algo que as outras não têm — no caso
do PC: **autoridade de servidor** — a mudança tem que atravessar tudo.

O código GDScript do mobile, por exemplo, roda **toda a lógica no cliente**.
Portar isso para o PC seria carregar para dentro da engine nova o defeito que
inviabiliza um competitivo em rede. Reescrever é mais barato **e melhor**.

---

## O que tem em cada pasta

| Pasta | O que é | Diretriz |
|---|---|---|
| **`design/`** | GDD, kits, dano, elenco, moeda, fichas dos 20 magos, estudo da Zona, referências, análise de mercado, infra | **Fonte da verdade.** Muda aqui primeiro, implementa depois. Nunca o contrário |
| **`arte/`** | concepts, `.glb`, áudio, prompts e `tools/` (Meshy, Blender) | Matéria-prima, não produto. Nada aqui é específico de engine |
| **`pc-unreal/`** | o jogo que vai para a Steam | Autoridade de servidor **desde a primeira linha**. Ver `design/referencias/PC-STEAM-ANALISE.md` |
| **`mobile-godot/`** | o jogo de celular, com 12 autotestes verdes | **Congelado.** Não recebe feature nova; serve de referência de gameplay e de orçamento medido |
| **`roblox/`** | o projeto-mãe | Intocado |

---

## Comece por aqui

**[`design/PROJETO.md`](design/PROJETO.md)** — a memória do projeto, com a seção
CONTINUAR DAQUI sempre no topo. É o único arquivo que precisa ser lido para saber
onde as coisas estão.

Decisão de plataforma e mercado:
**[`design/referencias/PC-STEAM-ANALISE.md`](design/referencias/PC-STEAM-ANALISE.md)**

---

## Estado (27/08/2026)

| | |
|---|---|
| **Plataforma alvo** | PC / Steam (decidido em 27/08) |
| **Engine alvo** | Unreal Engine 5 — ver a análise para o porquê |
| **mobile-godot** | 12/12 autotestes verdes, congelado |
| **Rede** | **não existe.** É o item mais caro e ainda não começou |
| **Elenco** | 20 fichas e 160 vistas de concept prontas; modelos 3D a refazer no site da Meshy |
