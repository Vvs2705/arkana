# PROJETO PRISMA — Direção técnica e estética do Arkana

> Documento de estratégia visual. Criado em 17/08/2026 após o v0.1; **reescrito em
> 17/08/2026 (v2)** após o teste do Diretor em aparelho real, que puxou o gatilho
> de reavaliação de engine e elevou a régua: **o alvo estético é o nível de
> Spell Arena — 3D de verdade, mesmo que custe tempo e trabalho.**
> Regra permanente: o GDD manda no produto; este documento manda no visual.

---

## 0. Veredito do Diretor (teste em aparelho real, 17/08/2026)

Registro literal do feedback que governa esta versão do documento:

1. **"Graficamente tem que avançar muito, mas muito mesmo, para se tornar algo
   mais decente."** O PRISMA-1 (luz, autotiling, rig procedural, VFX) melhorou o
   protótipo, mas o teto do 2D procedural foi atingido — e ainda está longe da
   régua comercial.
2. **"Avançar a nível 3D, mesmo que isso custe tempo e trabalho."** A cláusula de
   reavaliação de engine (antiga seção 2 deste doc) previa exatamente este
   gatilho. Ele foi puxado pelo Diretor.
3. **"Visual é tudo para jogadores."** Vira princípio de priorização: estética
   deixa de ser polimento tardio e vira trilha de produção de primeira classe,
   em paralelo à validação de mecânicas.

Pendências de MECÂNICA registradas no mesmo teste (fora do escopo deste doc,
enfileiradas como missão de código): troca de elemento não responde ao toque;
mira e disparo em dois gestos separados é inviável — o padrão BR mobile é
**um gesto só** (arrastar no botão de ataque mira e atira). Corrigir no 2D atual
antes de qualquer port: o 2D segue sendo o laboratório de mecânica.

---

## 1. A régua: o que "nível Spell Arena" significa em termos verificáveis

Spell Arena (e a faixa Brawl Stars / Zelda-lite mobile) entrega, concretamente:

| Critério | O que o jogador vê | Critério de aceite do Arkana |
|---|---|---|
| **Mundo 3D estilizado** | terreno com relevo, água com shader, vegetação volumétrica | arena 3D com os 6 biomas do GDD §14, legíveis de câmera de gameplay |
| **Personagens 3D animados** | magos com silhueta forte, idle/run/cast/hit/morte fluidos | rig humanoide + 6 animações núcleo por mago, leitura clara a distância de gameplay |
| **Iluminação dramática** | sol direcional + sombras suaves + emissivos de magia | 1 direcional (entardecer arcano) + luzes pontuais por magia/fogo, sombras em tempo real no mid-range |
| **VFX de magia premium** | projéteis com corpo, trilha, impacto em camadas | linguagem VFX por elemento (§6) com cor+forma+som (acessibilidade preservada) |
| **UI/HUD com identidade** | tipografia, molduras, ícones coesos | manter Selo de Arkana + paleta GDD §10 sobre o HUD 3D |
| **Performance** | 60 fps em aparelho intermediário | orçamentos da §7 cumpridos; "Modo Névoa" no aparelho fraco |

"Decente" deixa de ser opinião: é esta tabela. Cada fase da §5 fecha linhas dela.

---

## 2. Decisão de rota (DECISÃO DO DIRETOR — pendente de 1 palavra)

### Rota A — **Godot 4, 3D com câmera top-down inclinada** ← RECOMENDADA
- Câmera a ~50–60° (padrão Spell Arena/Brawl Stars): é 3D de verdade — relevo,
  sombras, modelos, shaders — **mantendo 100% das mecânicas já validadas**:
  o grid do terreno reativo (a simulação atual porta 1:1 para células 3D),
  mira por projétil, bots, escudo evolutivo, e os controles de toque
  (corrigidos) portam sem redesign.
- Por que Godot 4 e não Unity: open-source sem royalties/licença por instalação,
  export Android maduro, GDScript com curva curta (e C# disponível), leve na
  máquina; a avaliação de 17/08 (v1 deste doc) já o apontava como sucessor
  natural. Unity só ganharia por asset store — que não usaremos (identidade
  própria, §4).
- O código Phaser vira o que o GDD sempre disse que era: protótipo descartável.
  O que migra: GDD, balance (dados JSON), layout da arena, specs de toque,
  e TODA a direção de arte deste documento.

### Rota B — Primeira pessoa (a visão completa, Degrau 4 do GDD §8)
- Máximo impacto visual, custo máximo: anima­ções de mãos/cajado, netcode FP,
  level design refeito, mira totalmente nova. Invalida parte da validação atual.
- Recomendação: continua sendo o destino final, mas **depois** do mini-BR
  top-down 3D ter tração — exatamente a escada do GDD.

### Rota C — Permanecer 2D premium (PRISMA-2/3 originais)
- Registrada e **descartada pelo veredito do Diretor** (§0). Fica no histórico.

### Rota D — Construir dentro do Roblox (avaliada em 17/08 a pedido do Diretor)
- **O que resolve:** o item mais caro do roadmap — multiplayer (servidor
  autoritativo, replicação, salas) — vem pronto; hosting grátis; distribuição
  instantânea a uma base enorme (Brasil é mercado-chave do Roblox); monetização
  embutida. Produção drasticamente mais fácil.
- **A verdade dura:** NÃO existe "migrar para fora" — Luau, assets e sistemas
  são proprietários e inexportáveis. Só o GDD e o aprendizado saem. A audiência
  pertence à plataforma; corte efetivo de receita ~75%; e o teto visual fica
  ABAIXO da régua Spell Arena da §1 — colide de frente com "visual é tudo" (§0).
- **Enquadramento correto:** não é rampa para o jogo próprio — é um PRODUTO
  PARALELO de validação. Uma versão Arkana-Roblox testaria diversão, meta e
  multiplayer com jogadores reais a custo quase zero, enquanto o jogo-troféu
  (Rota A) é construído. O GDD serve os dois — essa é a vantagem estrutural
  do "documento manda, código obedece".
- Compatível com a Rota A (paralelo ou sequência), não substituta dela.

> **Default na ausência de resposta: Rota A.** Ao confirmar, a decisão entra no
> GDD como "Decisão travada" e o roadmap da §5 vira o plano de execução.

---

## 3. Direção de arte 3D — o style guide que evita o "3D genérico"

O erro clássico do salto para 3D é cair no realismo barato (assets prontos,
texturas fotográficas) — envelhece mal, pesa no mobile e mata a identidade.
A direção do Arkana é **low-poly estilizado com iluminação forte**:

- **Formas:** silhuetas exageradas e legíveis (chapéus, mantos, cajados — os
  cosméticos-assinatura do GDD §7 JÁ são shape language). Um mago identificável
  pela silhueta preta a distância de gameplay — teste obrigatório por modelo.
- **Materiais:** cores chapadas com gradientes sutis e rimlight; **zero textura
  fotorrealista**; emissivos reservados para magia (o que brilha = o que é
  mágico = o que é gameplay). Metal/vidro estilizados por ramp, não por PBR caro.
- **Paleta:** a do GDD §10 — azul-noite `#0B1026` como base do mundo, dourado
  `#F0C75E` como acento sagrado da marca, os 5 elementais saturados por cima.
  Regra: ambiente dessaturado o suficiente para VFX de magia SEMPRE vencerem.
- **Iluminação:** 1 direcional "entardecer arcano" (âmbar baixo, sombras longas
  e suaves) + pontuais de magia/fogo. Nada de GI cara: lightmap/probe simples.
- **Câmera:** top-down inclinada 50–60°, FOV 40–50, altura fixa com leve zoom
  dinâmico em combate (juice do PRISMA-1 porta: shake por trauma, kick, hitstop).
- **Biomas (GDD §14) em 3D:** floresta = cones/esferas low-poly queimáveis
  (visível: verde → em chamas → carvão); lago = plano com shader de onda +
  espuma, congela para gelo fosco andável; rocha = cristais facetados; grama
  alta = billboards/instâncias que escondem de verdade. **A legibilidade dos
  estados do terreno é inegociável — é o pilar nº 2 do jogo.**
- **Anti-metas:** nada de estilo "asset flip", nada de anime-realista, nada de
  voxel Minecraft-like. A referência de sensação: *Zelda-lite arcano*.

---

## 4. Pipeline de produção de assets (IA dirigida + curadoria dura)

Evolução do antigo PRISMA-2 — que continua válido e é **independente de engine**
(retratos e key art servem o lobby 3D igualmente):

| Asset | Pipeline | Ferramenta/na prática |
|---|---|---|
| **Concepts & style frames** | gerar variações → Diretor escolhe → vira referência canônica do style guide | geração de imagem por IA com prompt derivado da §3 |
| **Retratos dos magos (lobby)** | qualidade gacha 2D — caso de uso ideal de IA (v1 deste doc) | mantém plano original; entra no jogo 3D sem mudança |
| **Key art / telas** | idem retratos | idem |
| **Props e cenário** | experimento: imagem → malha 3D (GLB) para props secundários; retopo/estilização manual quando necessário | ferramenta imagem→3D disponível no ambiente do estúdio; validar num prop antes de escalar |
| **Magos (modelos hero)** | concept IA → modelagem low-poly dirigida (Blender) → rig humanoide padrão → 6 animações núcleo | aqui IA ajuda no concept, NÃO na malha final — modelo hero exige topologia limpa p/ deformação (EQUIPE.md 4.3) |
| **VFX** | shaders + partículas nativas da engine, nunca sprites de terceiros | linguagem da §6 |
| **Regra de registro** | todo asset gerado/derivado entra em `docs/CREDITS.md` com origem e data | inalterada |

Curadoria dura = o Diretor aprova cada asset contra o style guide da §3; arte
boa fora da linguagem é rejeitada (papel 4.1 do EQUIPE.md).

---

## 5. Roadmap PRISMA-3D (execução por levas, se Rota A confirmada)

Cada fase fecha linhas da tabela da §1 e só abre com a anterior aceita pelo
Diretor. O 2D atual permanece jogável como laboratório de mecânica até a 3D-2.

- **3D-0 · Fundação** — projeto Godot 4; import dos dados de balance/layout;
  grid do terreno reativo portado (mesma máquina de estados, células 3D);
  câmera + controles (toque de 1 gesto, lição da §0); cubo-mago jogável.
  *Aceite: arena greybox com fogo se propagando em 3D a 60fps no aparelho do Diretor.*
- **3D-1 · Blockout jogável** — 5 elementos com projéteis 3D placeholder,
  bots portados, escudo evolutivo, HUD mínimo. *Aceite: o "Campo de Provas"
  inteiro jogável em 3D greybox.*
- **3D-2 · Style guide aplicado ao mundo** — biomas da §3 com materiais e
  iluminação finais, água/gelo com shader, skybox do entardecer arcano.
  *Aceite: screenshot da arena indistinguível de um jogo comercial da faixa.*
- **3D-3 · Personagens** — 1º mago hero completo (Evocador): modelo, rig,
  6 animações; depois bots com variações. *Aceite: teste de silhueta + leitura
  em combate.*
- **3D-4 · VFX & juice 3D** — linguagem da §6 nos 5 elementos + juice portado
  (hitstop, shake, kick). *Aceite: cada elemento identificável por forma com
  som desligado e daltonismo simulado.*
- **3D-5 · Mobile hardening** — orçamentos da §7, LODs, Modo Névoa, APK Godot.
  *Aceite: 60fps no aparelho do Diretor, tabela da §1 fechada.*

Paralelizável pelo ORQUESTRADOR: 3D-2 (mundo) e 3D-3 (personagens) são raias
disjuntas; VFX entra depois do blockout. Retratos/key art (§4) correm em
paralelo desde já — não dependem da engine.

---

## 6. Linguagem VFX por elemento (cor + FORMA + som — acessibilidade do GDD §10)

| Elemento | Forma 3D assinatura | Movimento | Impacto |
|---|---|---|---|
| Fogo | cone/lágrima com núcleo branco | ondula, solta brasas | explosão radial + queimadura no chão |
| Água | esfera/ribbon translúcido | serpenteia | splash + poça persistente |
| Terra | cristal/lasca facetada | reta e pesada, gravidade | estilhaço + cratera |
| Vento | anel/hélice quase invisível | espiral rápida | distorção de ar + empurrão visível |
| Raio | zigue-zague segmentado | instantâneo com jitter | flash + arco residual na água |

Combos de Sintonia (GDD §9) = fusão literal das duas formas (ex.: Tornado
Flamejante = hélice de vento + núcleo de fogo). A forma É o tell competitivo.

---

## 7. Orçamentos técnicos mobile (invariantes de 60fps)

| Recurso | Orçamento | Fonte |
|---|---|---|
| Tris por mago (LOD0) | 5–8k (hero), 3–5k (bots/LOD1) | EQUIPE.md 4.3 |
| Draw calls por frame | < 100 (atlas por bioma, instancing na vegetação) | GDD §19.2 |
| Texturas | 1 atlas 2048 por bioma; personagens 1024 | — |
| Luzes em tempo real | 1 direcional + ~8 pontuais com culling por distância | lição do PRISMA-1 (pool de luzes) |
| Partículas | teto por qualidade (Baixa/Média/Alta) — padrão já provado no 2D | PRISMA-1 |
| Sombras | 1 cascata no mid-range; blob shadow no Modo Névoa | — |
| Aparelho fraco | "Modo Névoa": sem sombras dinâmicas, sem pós-FX, LODs agressivos | GDD §19.2 |

---

## 8. O que o 2D atual continua valendo

1. **Laboratório de mecânica** até a 3D-1: os fixes de toque (§0) e o ajuste de
   feel acontecem NELE — barato de iterar, e as specs migram.
2. **Prova das invenções**: terreno reativo e (na Fase 3) Sintonia — a simulação
   é a mesma máquina de estados nas duas engines.
3. **PRISMA-1 como manual**: pool de luzes, gate de qualidade, detecção por
   prefixo, juice por trauma — os padrões (não o código) portam todos.

---

## 9. Estado de execução

- [x] PRISMA-1 — fundação de render 2D, CONCLUÍDO 17/08/2026 (v0.1.2, commit
      49ac98e). Diagnóstico pós-teste: teto do 2D procedural atingido; serviu
      de manual de padrões para o 3D (§8).
- [ ] **DECISÃO DE ROTA (§2) — aguardando o Diretor** (default: Rota A).
- [ ] PRISMA-2 → absorvido na §4 (retratos/key art por IA — independe de engine,
      pode iniciar imediatamente).
- [ ] PRISMA-3 (Spine/animação 2D) → **substituído** pela rota 3D (personagens
      na 3D-3).
- [ ] PRISMA-4 (otimização mobile 2D) → **absorvido** na 3D-5; as notas técnicas
      (RT 3840px exige MAX_TEXTURE_SIZE ≥ 4096; teto de 10 luzes do Phaser)
      permanecem válidas enquanto o 2D for o laboratório.
- [ ] PRISMA-3D fases 3D-0 a 3D-5 (§5) — abrem com a Rota A confirmada.
