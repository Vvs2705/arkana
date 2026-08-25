# Passos necessários para toda essa parte gráfica

> **O que este documento é:** o caminho técnico completo, honesto e em ordem, para
> sair do visual atual do Arkana e chegar na qualidade das concept arts do elenco —
> com os programas, formatos, orçamentos técnicos, custos e prazos reais.
>
> **Escrito em:** 21/08/2026 · **Revisado em:** 24/08/2026
> **Autor:** equipe técnica (coordenador + raias)
>
> **Status:** as seções 1 a 8 são o plano e continuam válidas como plano. A
> **seção 9 é a única que descreve o estado real** — leia-a antes de agir. O
> proxy de Blender citado no histórico foi apagado em 21/08; hoje o jogo carrega
> um modelo real da Meshy.

---

## 1. O diagnóstico honesto: onde estamos

O jogo hoje tem **duas qualidades gráficas diferentes**, e é importante separá-las
porque só uma delas é problema.

### O que já está bom (e melhorou muito em 21/08)

O **mundo** é procedural, mas com iluminação real, gama corrigida, tonemap ACES,
névoa por profundidade, oclusão de ambiente assada no vértice e cinco shaders
próprios. Isso já é qualidade de jogo de verdade. O maior ganho daquele dia foi
descobrir que a cor de vértice entrava no shader **sem conversão de gama** — os hex
do GDD (sRGB) viravam quase o dobro de intensidade em linear, e o mundo inteiro
saía lavado e dessaturado.

### O que NÃO está bom (o problema real)

Os **personagens**. Números medidos em 21/08, não estimados. **Este é o retrato do
mago procedural**, que em 25/08 vale para 18 dos 20 — Pyra e Brok tem modelo
game-ready (seção 9).

| Item | Mago procedural |
|---|---|
| Malha do mago | **2.184–2.460 vértices**, gerada por revolução de perfis (`_lathe`) em código |
| Textura do personagem | **nenhuma** — cor lisa por material |
| Mapas PBR | nenhum (sem normal, roughness, metallic, AO) |
| Rig | hierarquia de `Node3D`, sem skinning por peso |
| Animações | 3 (idle, run, cast), feitas à mão em código |

**A conclusão dura:** um corpo de 2.400 vértices feito por revolução, sem textura e
sem esqueleto com pesos, **nunca vai parecer a concept art da Pyra**. Não é questão
de ajustar shader. A concept art tem anatomia esculpida, dobras de tecido, desgaste
no metal, cicatriz com textura de pele e uma manopla de bronze com placas
articuladas. Nenhuma dessas coisas existe — nem pode existir — numa malha de
revolução sem textura.

A regra de "zero binário no repositório" foi **ótima para o protótipo** (leve,
determinística, sem dependência de artista). E era **exatamente o que impedia** o
jogo de ter os personagens que você desenhou. **A regra foi abandonada para os
personagens** — o `.glb` e a textura da Pyra estão versionados via Git LFS
(seção 9). A oficina de trabalho de cada mago continua fora do git.

---

## 2. A referência: o que Apex Legends Mobile realmente era

Vale calibrar a expectativa com fatos.

- **Lançamento:** maio de 2022. **Encerramento:** maio de 2023 — o jogo foi
  descontinuado pela EA/Respawn. É uma referência de *qualidade visual*, não de
  modelo de negócio a copiar.
- **Quem fez:** Respawn Entertainment com **Lightspeed & Quantum Studios**
  (Tencent), um dos maiores estúdios de mobile do mundo.
- **Escala de equipe:** centenas de pessoas, com departamentos separados de
  personagem, ambiente, animação, VFX, iluminação e otimização.

**O que isso significa para o Arkana:** a *qualidade de imagem* de Apex Mobile é
alcançável em peças isoladas. O *volume* (dezenas de personagens, mapas enormes,
centenas de cosméticos, tudo a 60fps em aparelho fraco) é trabalho de estúdio. O
caminho realista é **qualidade alta em escopo pequeno**: poucos personagens
excelentes valem mais que vinte medianos.

### Orçamentos técnicos que um BR mobile desse nível usa

| Item | Faixa em BR mobile AAA | Arkana hoje |
|---|---|---|
| Personagem jogável (triângulos) | 15.000–40.000 | ~2.400 vértices |
| Texturas por personagem | 2048² ou 4096² (albedo, normal, ORM) | nenhuma |
| Ossos por personagem | 60–120 | 0 (sem skinning) |
| Draw calls por personagem | 3–8 (atlas + poucos materiais) | ~18 |
| Personagens simultâneos | 6–20 | 7 |
| Alvo de FPS | 60 (com modo 30 para aparelho fraco) | não medido em device |

A coluna "Arkana hoje" é o mago procedural. A Pyra já saiu dela: 15.492 triângulos,
textura 2k, PBR, riggada. O Brok seguiu o mesmo caminho em 25/08. Faltam 18.

---

## 3. O pipeline de personagem — as 9 etapas, em ordem

Este é o caminho padrão da indústria. Nenhuma etapa pode ser pulada sem custo.

### Etapa 1 — Concept art ✅ JÁ TEMOS
As 8 vistas por mago em `personagens/NN-slug/arte/_originais/` cumprem esse papel.
**Esta etapa está pronta** — e é o ativo mais valioso do projeto hoje. Com uma
ressalva medida: **não existe perfil de 90° no lote** (ver `ART.md`), o que limita
o fluxo multi-imagem da Meshy.

A folha única `arte/concept.png` **não existe mais** — foi substituída pelas vistas
separadas em 24/08. Script ou documento que ainda a procure está quebrado.

### Etapa 2 — Escultura de alta densidade (high-poly)
Modelar com todo o detalhe: dobras de pano, placas de metal, cicatrizes, anatomia.
Milhões de polígonos — não vai para o jogo, serve de **fonte do detalhe**.

- **Programas:** **Blender** (grátis, escultura competente) · **ZBrush** (padrão da
  indústria) · **Nomad Sculpt** (iPad, barato e surpreendentemente bom)
- **Tempo:** 6–20 h por personagem

### Etapa 3 — Retopologia (low-poly)
Reconstruir com poucos polígonos e topologia limpa — quads seguindo o fluxo dos
músculos e das dobras, para deformar bem na animação.

- **Programas:** **Blender** (RetopoFlow ou manual) · **Maya** · **InstaLOD /
  Simplygon** (automático, resultado inferior)
- **Alvo para o Arkana:** 12.000–20.000 triângulos por mago
- **Tempo:** 3–8 h por personagem

### Etapa 4 — Mapeamento UV
"Desdobrar" o modelo num plano 2D para pintar a textura, sem esticamento e
aproveitando o espaço.

- **Programas:** **Blender** (nativo) · **RizomUV** (muito mais rápido)
- **Tempo:** 1–3 h por personagem

### Etapa 5 — Baking (assar os mapas)
Transferir o detalhe do high-poly para texturas que o low-poly usa: **normal map**
(finge o relevo), **ambient occlusion**, **curvature**, **position**.

- **Programas:** **Blender** (nativo) · **Marmoset Toolbag** (o melhor) ·
  **Substance Painter** (assa junto ao pintar)
- **Tempo:** 1–2 h por personagem

### Etapa 6 — Texturização PBR
Pintar as texturas seguindo a concept art. O padrão moderno é **PBR** com três
mapas: **albedo** (cor), **normal** (relevo) e **ORM** (oclusão + rugosidade +
metalicidade empacotados num RGB só).

- **Programas:** **Adobe Substance 3D Painter** — **é o padrão da indústria** ·
  **Blender + Krita** (grátis, mais trabalhoso) · **ArmorPaint** (alternativa aberta)
- **Você já usa Adobe:** o Substance faz parte do ecossistema Adobe 3D — vale
  checar se seu plano já cobre.
- **Tempo:** 4–10 h por personagem

### Etapa 7 — Rigging (esqueleto)
Criar ossos e articulações. Para mobile, 60–90 ossos por personagem.

- **Programas:** **Blender** (Rigify, grátis e excelente) · **Mixamo Auto-Rigger**
  (Adobe, **grátis**, rigga automaticamente em 2 minutos) · **AccuRig** (grátis)
- **Atalho real:** Mixamo resolve a maioria dos humanoides sem custo
- **Tempo:** 1–4 h — ou 5 minutos com Mixamo

### Etapa 8 — Skinning (pesos)
Definir quanto cada osso influencia cada vértice. É o que faz o cotovelo dobrar sem
"amassar" o braço.

- **Programas:** **Blender** (pesos automáticos + correção manual)
- **Tempo:** 2–6 h por personagem

### Etapa 9 — Animação
Andar, correr, atacar, conjurar, cair, reviver, emotes.

- **Programas:** **Blender** (manual) · **Mixamo** (biblioteca **grátis** de
  centenas de animações prontas para qualquer rig humanoide) · **Cascadeur**
  (assistido por IA, tem versão grátis) · **Rokoko / Move.ai** (captura por vídeo
  de celular)
- **Tempo:** 20–60 h para um set completo — **mas o set é COMPARTILHADO** entre
  todos os magos com o mesmo esqueleto: faz uma vez, usa nos 20

---

## 4. Atalho: geração 3D por IA

Existe hoje um caminho que corta as etapas 2 e 3 pela metade: gerar uma malha base
a partir da própria concept art.

- **Ferramentas:** as MCPs de geração 3D já conectadas nesta sessão (imagem → GLB) ·
  **Meshy** · **Tripo3D** · **Rodin** · **Luma Genie**
- **O que entregam:** malha com textura, tipicamente 10k–50k triângulos
- **O que NÃO entregam:** topologia limpa (é "sopa de triângulos"), UV aproveitável,
  detalhe fiel a uma concept complexa, ou modelo pronto para riggar bem
- **Uso correto:** **base para refinar**, nunca produto final. Gera-se, faz-se a
  retopologia por cima (etapa 3), e segue o pipeline normal.

**Feito em 21–24/08:** a rota por IA foi testada com a Pyra via Meshy e o modelo
está no jogo (seção 9). A ressalva se confirmou — a manopla de bronze, peça de
assinatura, saiu ausente.

---

## 5. A decisão de estilo — ABERTA, mora no ART.md

A escolha do estilo de renderização dos personagens **não foi tomada** e **não é
decidida aqui**. O estado da questão, a evidência medida no elenco e a decisão
pendente do Diretor estão em [ART.md](ART.md).

O que este documento tem a dizer sobre ela é só a parte técnica:

- **PBR realista** custa mais em produção e em GPU, exige coerência de iluminação
  perfeita, e obrigaria a trocar o mundo cel-shaded já calibrado.
- **Stylized PBR** (Overwatch, Valorant, Fortnite) roda muito melhor em mobile e
  casa com os cinco shaders estilizados que já existem.

**Recomendação da equipe técnica: stylized PBR** — por custo de GPU e por
compatibilidade com o mundo atual. É recomendação, não decisão.

**Cuidado ao traduzir isso como "menos realismo".** O `ART.md` mediu que esse
enunciado empurra o gerador para anime, que é justamente o que quebra o elenco.

---

## 6. Formatos, importação e integração no Godot

### Formato de entrega
- **glTF 2.0 binário (`.glb`)** — formato nativo do Godot 4: importa malha,
  materiais, esqueleto e animação num arquivo só.
- **Nunca usar FBX** — precisa de conversor externo e dá problema.
- **Texturas:** PNG na origem; o Godot converte para **ETC2/ASTC** no export
  Android. A linha `import_etc2_astc=true` já está no `project.godot` — é o que faz
  o APK não falhar em silêncio.

### Compressão de textura (crítico para o tamanho do APK)

| Formato | Uso |
|---|---|
| **ASTC** | padrão moderno, melhor qualidade — Android 8+ |
| **ETC2** | fallback universal para aparelho antigo |
| **VRAM Compressed** | marcar no import do Godot para tudo que é 3D |

### Otimizações obrigatórias em mobile
1. **Atlas de textura** — juntar peças numa textura só derruba draw calls
2. **LOD** — o Godot 4 gera automaticamente no import do `.glb`
3. **GPU Instancing** — para vegetação e objetos repetidos (já usado na grama)
4. **Limite de ossos** — 60–90; acima disso o custo de skinning explode
5. **Um material por personagem** sempre que possível — hoje são ~18 draw calls por
   mago; com atlas cai para 3–5

### Onde os arquivos moram (verificado em 24/08)

```
godot/characters/modelos/
├── pyra.glb                 malha + rig + animações — VERSIONADO (Git LFS)
├── pyra_texture_0.png       textura                 — VERSIONADO (Git LFS)
└── 01-pyra/                 OFICINA — fora do git, regenerável
    ├── 01-pyra.glb          malha crua da Meshy
    ├── 01-pyra_rigged.glb   saída do rigger
    ├── 01-pyra_*.png/.jpg   albedo, normal, roughness, metallic soltos
    └── anim/                animações separadas
```

**A regra que isso estabelece:** vai para o git só o que o jogo carrega. A oficina
(`modelos/<slug>/`) pesa ~102 MB só na Pyra e é regenerável por ~41 créditos da
Meshy — versioná-la seria pagar peso permanente por um arquivo descartável.
O `.gitignore` já implementa isso (`godot/characters/modelos/*/` com exceção para
os `.glb` da raiz).

**Isso quebrou a regra de "zero binário"** — consequência inevitável da decisão.
Git LFS está configurado para `godot/characters/modelos/*.{glb,png,ktx2}`.

---

## 7. Software: a pilha completa

### Caminho grátis (viável de verdade)

| Etapa | Programa | Custo |
|---|---|---|
| Escultura, retopo, UV, bake, rig, skin, animação | **Blender 4.x** | **R$ 0** |
| Texturização | **Blender + Krita** | **R$ 0** |
| Rig automático + animações prontas | **Mixamo** (Adobe) | **R$ 0** |
| Engine | **Godot 4.4** | **R$ 0** |

### Caminho profissional (onde o dinheiro compra tempo)

| Etapa | Programa | Faixa de custo |
|---|---|---|
| Escultura | ZBrush | assinatura ou licença perpétua |
| Texturização | **Substance 3D Painter** | assinatura ou perpétua |
| UV | RizomUV | licença única |
| Bake e look dev | Marmoset Toolbag | licença única |
| Roupas com simulação | Marvelous Designer | assinatura |
| Geração 3D por IA | Meshy / Tripo | mensalidade baixa |

**O item que mais compra tempo:** Substance Painter. É onde a diferença entre
"parece amador" e "parece jogo" realmente se decide.

---

## 8. Custo e prazo reais

### Por personagem (do zero, sozinho, aprendendo)

| Etapa | Tempo |
|---|---|
| Escultura | 6–20 h |
| Retopologia | 3–8 h |
| UV | 1–3 h |
| Bake | 1–2 h |
| Texturização | 4–10 h |
| Rig + skin | 3–10 h |
| **Total por mago** | **18–53 h** |

**Os 20 magos:** 360–1.060 horas. A **primeira vez é a mais lenta** — do terceiro
personagem em diante o tempo costuma cair pela metade.

### Terceirizar
- **Freelancer de personagem 3D game-ready:** faixa ampla por personagem, conforme
  fidelidade e portfólio (ArtStation, Fiverr Pro, Upwork)
- **Recomendação:** contratar **um** primeiro, com a concept art e este documento
  como briefing, e usar o resultado como referência de qualidade para os demais

---

## 9. Estado real (24/08) — leia esta seção antes de agir

As seções acima são plano. **Esta é a única que descreve o que existe.**

### O que o jogo carrega hoje

Verificado no disco em 24/08:

| Caminho | Peso | No git? | O que é |
|---|---:|---|---|
| `godot/characters/modelos/pyra.glb` | 7,3 MB | **sim** (Git LFS) | o modelo real da Meshy — **é o que o jogo carrega** |
| `godot/characters/modelos/pyra_texture_0.png` | 5,9 MB | **sim** (Git LFS) | a textura |
| `godot/characters/modelos/01-pyra/` | 102 MB | **não** | ateliê local: malha crua, riggada, animações separadas |

`Mage.gd` resolve o slug `01-pyra` para `res://characters/modelos/pyra.glb` —
confirmado pelo `characters/selftest.gd`. Quem não tem `.glb` cai no mago
procedural. **Métricas da Pyra:** 15.492 triângulos, PBR, riggada, 3 animações.

O ateliê fica fora do git porque é **regenerável**, por cerca de 41 créditos da
Meshy:

```bash
python tools/meshy/meshy.py tudo 01-pyra
python tools/meshy/montar_glb.py 01-pyra pyra
```

### HISTÓRICO — o proxy de Blender (existiu, foi apagado em 21/08)

Antes do modelo da Meshy chegar, a Pyra teve um **proxy descartável** gerado no
Blender. **Ele não existe mais.** Está registrado aqui porque as lições que ele
pagou continuam valendo — e porque este documento afirmou o proxy como estado
atual por três dias depois de ele ter sido apagado.

**O que o proxy provou (continua válido):**

- importação de `.glb` no Godot 4.4 funciona;
- aliases de animação resolvem nomes de exportador diferentes
  (`Idle`, `Armature|Running`, `Spell Cast` → `idle`, `run`, `cast`);
- o fallback procedural entra corretamente para quem não tem modelo;
- o export Android aceita o pipeline e gera APK debug.

**O que era só do proxy (não vale mais):** 996 vértices, 5 ossos, 5 materiais.
Esses números descreviam um manequim de teste, nunca a Pyra. **Se você os
encontrar citados em qualquer outro documento, estão errados.**

### Fidelidade do modelo — três caminhos, PENDENTE DO DIRETOR

A geração acertou o corpo da Pyra mas **perdeu a manopla de bronze**, que é a
assinatura dela. São três saídas possíveis. **Nenhuma foi escolhida** — são
opções levantadas pela equipe, não decisão.

| Caminho | Custo | O que se perde |
|---|---|---|
| **A — aceitar** e resolver a manopla em textura e VFX | zero | a silhueta da assinatura; de longe a Pyra vira "maga genérica" |
| **B — modelar a peça à mão** no Blender e acoplar ao rig | horas de modelagem por mago com adereço | nada visual; vira trabalho manual recorrente no elenco |
| **C — regerar** com a peça-assinatura isolada como referência | créditos de Meshy | tempo, e não há garantia de acerto |

O ateliê já tem `equipamento-isolado.png` por mago, e o padrão da pipeline manda
gerar peça-assinatura isoladamente — o caminho C tem insumo pronto.

### O que continua pendente de verdade

- [ ] **FPS em aparelho, com vários personagens em cena — nunca foi medido.**
      Nenhum device apareceu em `adb devices`. Este é o número que decide se o
      orçamento de 15k triângulos por mago se sustenta; sem ele, tudo aqui é
      estimativa.
- [ ] **Esqueleto compartilhado final humanoide.** Sem ele, cada mago precisa do
      próprio set de animação — o custo multiplica por 20 em vez de somar.
- [ ] **Atlas e texturas finais.** Sem atlas, as draw calls por mago não caem para
      a faixa de 3–5.
- [ ] **19 dos 20 magos sem modelo.** Usam o procedural.

### Infraestrutura de código — o que já está pronto

- [x] Carregamento de modelo por personagem com fallback procedural em `Mage.gd`.
- [x] Aliases de animação externa para `idle`, `run` e `cast`.
- [x] `cast_fired` externo sincronizado por adaptador e protegido contra interrupção.
- [x] `get_model_report()` com métricas de vértices, superfícies, materiais, ossos e animações.
- [x] Git LFS para `godot/characters/modelos/*.{glb,png,ktx2}`.
- [x] Import Godot headless validado com `pyra.glb`.
- [x] APK debug exportado e verificado.

### O que vem depois, na ordem

1. **Medir FPS em aparelho** com vários magos em cena. Trava tudo o mais.
2. **Fechar a direção de arte do elenco** (`ART.md` — pendente do Diretor).
3. **Elenco:** os 10 magos de lançamento, um a um; os 10 de temporada depois.
4. **Ambiente à altura:** assets modelados nos POIs, lightmaps no cenário
   estático, reflection probes nos pontos de interesse.

Os passos 1 e 2 são portões. Escalar o elenco antes deles é assinar retrabalho
de 20 peças.

---

## 10. O que NÃO fazer (erros que custam caro)

1. **Modelar os 20 antes de testar 1 no celular.** Se a qualidade ou o FPS não
   servirem, são 20 retrabalhos.
2. **Usar FBX.** Dá problema no Godot. Sempre `.glb`.
3. **Textura 4K em mobile.** 2048² é o teto sensato; 1024² serve para a maioria.
4. **Ligar SSAO, SSIL, SSR, SDFGI ou névoa volumétrica.** **Não existem no renderer
   mobile do Godot** — ligam sem erro e simplesmente não aparecem. O self-test do
   mundo já barra isso.
5. **Commitar binário grande sem Git LFS.** O repositório incha e não tem volta fácil.
6. **Misturar PBR realista com cel-shading** sem uma decisão de arte consciente.
7. **Copiar assets de Apex.** Violação de direito autoral — as concept arts do
   Arkana são originais, e essa é a maior força do projeto.

---

## 11. Resumo executivo

**Para chegar na qualidade das concept arts é preciso:**

1. **Abandonar a malha procedural** para os personagens (mantendo-a no mundo)
2. **Adotar o pipeline padrão:** escultura → retopo → UV → bake → PBR → rig → skin
3. **Blender + Substance Painter + Mixamo** cobrem tudo — Blender e Mixamo, grátis
4. **Entregar em `.glb`** com texturas comprimidas em ASTC/ETC2
5. **Mirar 12–20k triângulos** e 3–5 draw calls por mago
6. **Fechar a direção de arte do elenco** — pendente do Diretor, ver `ART.md`
7. **Começar por UM personagem** e julgá-lo **no aparelho** antes de escalar —
   o personagem existe (Pyra); o julgamento no aparelho ainda não aconteceu

**O gargalo não é técnico — é de produção de arte 3D.** Todo o trabalho de código
(pipeline, importação, rig compartilhado, LOD, troca de personagem, otimização)
pode ser feito pela equipe técnica. A **escultura e a texturização** exigem você,
um modelador contratado, ou uma ferramenta de geração 3D com refino humano.

---

## 12. A pergunta que decide o próximo passo

A pergunta antiga era "modelar de verdade a Pyra ou espremer o procedural?".
**Ela foi respondida:** a Pyra está modelada e no jogo. A pergunta atual é outra.

> **A Pyra roda a 60 FPS num celular com mais seis magos em cena — e chegou na
> qualidade da concept art?**

Ninguém sabe. Nunca houve um aparelho conectado. Enquanto essa resposta não vier,
qualquer número de orçamento neste documento é estimativa, e escalar o elenco é
apostar 20 peças num palpite.
