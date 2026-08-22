# Passos necessários para toda essa parte gráfica

> **O que este documento é:** o caminho técnico completo, honesto e em ordem, para
> sair do visual atual do Arkana e chegar na qualidade das concept arts do elenco —
> com os programas, formatos, orçamentos técnicos, custos e prazos reais.
>
> **Data:** 21/08/2026 · **Autor:** equipe técnica (coordenador + raias)
> **Status:** decisão + execução técnica parcial. Em 21/08 foi montado o pipeline
> local, gerado um `pyra.glb` proxy para prova de importação, validado o fallback
> procedural e exportado APK debug. A arte final game-ready da Pyra ainda não foi
> produzida.

---

## 1. O diagnóstico honesto: onde estamos

O jogo hoje tem **duas qualidades gráficas diferentes**, e é importante separá-las
porque só uma delas é problema.

### O que já está bom (e melhorou muito em 21/08)

O **mundo** é procedural, mas com iluminação real, gama corrigida, tonemap ACES,
névoa por profundidade, oclusão de ambiente assada no vértice e cinco shaders
próprios. Isso já é qualidade de jogo de verdade. O maior ganho do dia foi
descobrir que a cor de vértice entrava no shader **sem conversão de gama** — os hex
do GDD (sRGB) viravam quase o dobro de intensidade em linear, e o mundo inteiro
saía lavado e dessaturado.

### O que NÃO está bom (o problema real)

Os **personagens**. Números medidos hoje, não estimados:

| Item | Estado atual |
|---|---|
| Malha do mago | **2.184–2.460 vértices**, gerada por revolução de perfis (`_lathe`) em código |
| Textura do personagem | **nenhuma** — cor lisa por material |
| Mapas PBR | nenhum (sem normal, roughness, metallic, AO) |
| Rig | hierarquia de `Node3D`, sem skinning por peso |
| Animações | 3 (idle, run, cast), feitas à mão em código |
| Arquivos de arte 3D no repositório | **zero** (regra "zero binário") |

**A conclusão dura:** um corpo de 2.400 vértices feito por revolução, sem textura e
sem esqueleto com pesos, **nunca vai parecer a concept art da Pyra**. Não é questão
de ajustar shader. A concept art tem anatomia esculpida, dobras de tecido, desgaste
no metal, cicatriz com textura de pele e uma manopla de bronze com placas
articuladas. Nenhuma dessas coisas existe — nem pode existir — numa malha de
revolução sem textura.

A regra de "zero binário no repositório" foi **ótima para o protótipo** (leve,
determinística, sem dependência de artista). E é **exatamente o que impede** o jogo
de ter os personagens que você desenhou. Chegar na qualidade das artes exige
abandonar essa regra para os personagens.

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

---

## 3. O pipeline de personagem — as 9 etapas, em ordem

Este é o caminho padrão da indústria. Nenhuma etapa pode ser pulada sem custo.

### Etapa 1 — Concept art ✅ JÁ TEMOS
As 20 folhas em `personagens/NN-slug/arte/concept.png` já cumprem esse papel:
frente, lado, costas, paleta e insets de detalhe. **Esta etapa está pronta** — e é
o ativo mais valioso do projeto hoje.

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

**Recomendação:** testar com **um** personagem antes de decidir qualquer coisa. Se
a malha gerada da Pyra servir de base, isso corta cerca de 40% do tempo por
personagem.

---

## 5. A decisão de estilo: realista ou stylized-PBR?

Esta escolha define tudo o que vem depois, e **é sua**.

### Opção A — PBR realista (o caminho Apex)
Materiais fisicamente corretos, luz realista, texturas de alta resolução.
- **Prós:** é literalmente a qualidade de Apex; as concept arts são pinturas
  semi-realistas e traduzem bem
- **Contras:** o mais caro em produção e em GPU; exige coerência de iluminação
  perfeita, senão vira "plástico"; o mundo cel-shaded atual teria que mudar junto

### Opção B — Stylized PBR ⭐ recomendada
Malha e texturas com qualidade PBR, mas direção de arte estilizada — silhuetas
fortes, cor saturada, detalhe onde importa. É o caminho de Overwatch, Valorant,
Genshin Impact e Fortnite.
- **Prós:** roda **muito** melhor em mobile; envelhece melhor; perdoa imperfeição;
  **casa com o mundo cel-shaded que já existe e foi calibrado hoje**; as concept
  arts do Arkana já têm essa pegada
- **Contras:** não é foto-realista — mas nenhuma das referências acima é, e todas
  parecem excelentes

**Recomendação técnica: Opção B.** O Arkana já tem cinco shaders estilizados
funcionando e um mundo calibrado. Trocar tudo por PBR realista jogaria fora o
trabalho de hoje e traria um custo de GPU que celular médio não paga.

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

### Onde os arquivos vão morar

```
godot/characters/modelos/
├── pyra.glb                 malha + rig + animações
├── pyra_albedo.png          cor
├── pyra_normal.png          relevo
└── pyra_orm.png             oclusão / rugosidade / metal
```

**Isso quebra a regra de "zero binário"** — é a consequência inevitável da decisão.
Recomenda-se **Git LFS** para os binários: `git lfs track "*.glb" "*.png"`.

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

## 9. O plano recomendado, em ordem de execução

### Fase 1 — Prova de conceito (1 personagem)
1. Escolher a **Pyra** — a mais icônica: braço de chama + manopla de bronze
2. Testar geração 3D por IA a partir da `concept.png`; avaliar se serve de base
3. Refinar: retopologia para ~15k triângulos, UV, bake, texturizar
4. Riggar no Mixamo, ajustar pesos no Blender
5. Exportar `.glb`, importar no Godot, **rodar no celular**
6. **Julgar: chegou na qualidade da concept art?**

**Só depois de responder essa pergunta faz sentido decidir o resto.**

**Execução técnica em 21/08:** Pyra foi escolhida e recebeu um proxy local
`godot/characters/modelos/pyra.glb`, gerado por Blender via
um proxy descartável (removido em 21/08, quando o modelo real da Meshy
chegou). O proxy provou importação `.glb`, aliases de
animação (`Idle`, `Armature|Running`, `Spell Cast`), materiais, esqueleto simples,
fallback e export Android. Ele **não** substitui escultura, retopologia, UV, bake e
texturização final.

Métricas do proxy validado:

| Item | Resultado |
|---|---:|
| MeshInstances | 1 |
| Superfícies / draw calls aprox. | 5 |
| Materiais | 5 |
| Vértices | 996 |
| Ossos | 5 |
| Animações | 3 (`idle`, `run`, `cast` via aliases) |
| APK debug | `godot/build/arkana3d.apk` (~32 MB) |
| Device físico | pendente — nenhum aparelho apareceu em `adb devices` |

### Fase 2 — Pipeline e infraestrutura *(trabalho de código — a equipe técnica faz)*
7. Sistema de carregamento de modelo por personagem (substituir o `_lathe`)
8. Esqueleto compartilhado: um set de animação para todos os magos humanoides
9. Git LFS configurado
10. LOD e atlas automáticos no import
11. Medição de FPS em device com 7 personagens

Status técnico em 21/08:

- [x] Sistema de carregamento por personagem com fallback procedural em `Mage.gd`.
- [x] Aliases de animação externa para `idle`, `run` e `cast`.
- [x] `cast_fired` externo sincronizado por adaptador e protegido contra interrupção.
- [x] `get_model_report()` com métricas de vértices, superfícies, materiais, ossos e animações.
- [x] Git LFS configurado para `godot/characters/modelos/*.{glb,png,ktx2}`.
- [x] Import Godot headless validado com `pyra.glb`.
- [x] APK debug exportado e verificado.
- [ ] Esqueleto compartilhado final humanoide — proxy tem 5 ossos; alvo final é 60–90.
- [ ] Atlas/texturas finais — proxy usa materiais de cor; sem albedo/normal/ORM finais.
- [ ] Medição de FPS em device com 7 personagens — bloqueada por ausência de aparelho conectado.

### Fase 3 — Produção do elenco
12. Os 10 magos de lançamento, um a um, com o pipeline já provado
13. Os 10 de temporada depois

### Fase 4 — Ambiente à altura
14. Substituir a ilha procedural por assets modelados nos POIs principais
15. Iluminação assada (lightmaps) para o cenário estático
16. Reflection probes nos pontos de interesse

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
6. **Escolher stylized-PBR**, não realismo puro — casa com o mundo atual e roda em
   celular
7. **Começar por UM personagem** e julgá-lo no aparelho antes de escalar

**O gargalo não é técnico — é de produção de arte 3D.** Todo o trabalho de código
(pipeline, importação, rig compartilhado, LOD, troca de personagem, otimização)
pode ser feito pela equipe técnica. A **escultura e a texturização** exigem você,
um modelador contratado, ou uma ferramenta de geração 3D com refino humano.

---

## 12. A pergunta que decide o próximo passo

> **Modelar de verdade a Pyra e julgá-la no celular — ou espremer o máximo do
> procedural enquanto isso?**

Enquanto a decisão não vem, o procedural continua evoluindo: o shading híbrido, o
sistema de identidade por mago e o mundo com gama corrigida já entregaram ganho
real hoje. Mas há um teto, e ele está bem abaixo das concept arts.
