# GDD — Battle Royale de Magos (título provisório: "Conclave Royale")

**Pilares:** Apex Legends traduzido para fantasia · Primeira pessoa · Magias com projétil (nada hitscan) · Informação e mobilidade valem tanto quanto dano · Nada apelão: toda habilidade forte tem contrapartida visível.

---

## 1. As 5 Classes (com vantagem de classe, como no Apex)

| Classe | Equivalente Apex | Papel | Vantagem de classe |
|---|---|---|---|
| **Vanguarda** | Assault | Iniciar lutas, pressão | Carrega +1 poção/pergaminho por slot; abre Baús de Guerra (loot vermelho) |
| **Errante** | Skirmisher | Mobilidade, flanco | Vê o conteúdo dos Baús Celestiais antes de caírem; recarga de suprema ao abri-los |
| **Vidente** | Recon | Informação | Lê **Torres Arcanas** espalhadas no mapa: revela a próxima zona segura E marca inimigos num raio por 5s (sua ideia do "leitor de torres") |
| **Guardião** | Support | Cura, ressurreição | Cura aliados 25% mais rápido; abre compartimento secreto dos baús de suprimentos (itens de cura extras) |
| **Dominador** | Controller | Controle de área | Vê no mapa um palpite da zona final; suas construções/armadilhas duram mais |

**Item exclusivo de Vidente (sua ideia):** o **Olho Onisciente** — item raro de mapa. Só Videntes podem usá-lo: revela TODOS os inimigos num raio grande no minimapa por 6s. Uso único, consome o item, e os inimigos revelados recebem aviso "você foi visto".

---

## 2. Mapa completo: cada lenda do Apex → arquétipo de mago

### Vanguarda (Assault)
| Apex | Mago | Conceito em 1 linha |
|---|---|---|
| Bangalore | **Pyra** | Cortina de cinzas + chuva de meteoros |
| Fuse | **Barril** | Bombardeiro rúnico, granadas alquímicas em cluster |
| Ash | **Ceifadora** | Laço de almas que prende + fenda dimensional ofensiva |
| Mad Maggie | **Fúria** | Bola de fogo rolante + velocidade com armas de fogo... vira: bônus ao usar magias de curto alcance |
| Ballistic | **Duelista** | Marca de sobrecarga (arma inimiga "esquenta" → cajado inimigo superaquece) |

### Errante (Skirmisher)
| Apex | Mago | Conceito |
|---|---|---|
| Wraith | **Véu** | Passo etéreo + portal entre dois pontos |
| Octane | **Mercúrio** | Magia de sangue: HP por velocidade; runa de salto de vento |
| Pathfinder | **Gólem** | Corrente etérea (gancho) + tirolesa de luz |
| Horizon | **Gravita** | Coluna anti-gravidade + poço gravitacional |
| Valkyrie | **Aeromante** | Vassoura de combate: voo curto + rajada de estilhaços |
| Revenant | **Nosferatu (Vampiro)** | Salto sombrio + forma vampírica (transformação) |
| Alter | **Fenda** | Portal que atravessa paredes/chão |

### Vidente (Recon)
| Apex | Mago | Conceito |
|---|---|---|
| Bloodhound | **Corvus (Lobisomem)** | Pulso revelador + pegadas + transformação em besta |
| Crypto | **Corvomante** | Familiar (corvo espectral) controlável + onda antimagia |
| Seer | **Olho-de-Éter** | Espíritos observadores que interrompem conjuração |
| Vantage | **Atiradora** | Familiar morcego (reposicionamento) + luneta arcana que marca alvos |

### Guardião (Support)
| Apex | Mago | Conceito |
|---|---|---|
| Gibraltar | **Rochedo** | Braço-escudo + domo de proteção |
| Lifeline | **Vitalis** | Fada curandeira + baú celestial |
| Loba | **Ladra** | Vê loot raro através de paredes + anel de translocação |
| Mirage | **Ilusionista** | Clones ilusórios + troca invisível |
| Newcastle | **Baluarte** | Escudo móvel ao reviver + muralha de energia |
| Conduit | **Condutora** | Transfere mana/escudo para aliados à distância |

### Dominador (Controller)
| Apex | Mago | Conceito |
|---|---|---|
| Caustic | **Vex** | Totens de gás pestilento + bomba de peste |
| Wattson | **Tessa** | Cercas de raios entre pilares + pilar que devora projéteis |
| Rampart | **Muralha** | Paredes conjuradas com lado amplificado + canhão arcano fixo |
| Catalyst | **Umbra** | Véu negro que bloqueia visão E magias de detecção |

---

## 3. Elenco de lançamento (10 magos detalhados)

> Apex lançou com 8 lendas. Começar com 10 cobre as 5 classes (2 por classe) e mantém o escopo real.
>
> **REVISADO 20/08/2026 (ordem do Diretor):** os kits foram DESCOLADOS do Apex —
> as lendas viraram ponto de partida, não gabarito. Cada mago agora tem uma
> identidade mágica própria (cicatriz, membro/órgão substituído por magia, pacto,
> maldição) que JUSTIFICA o kit. Fichas completas (aparência, história, VFX,
> paleta, modelagem) em `personagens/01..10`. Papéis de classe e counters do §4
> mantidos.

Formato: **Passiva / Tática / Suprema** + ⚖️ limitadores (o "preço" de cada poder).

### VANGUARDA

**1. Pyra, a Chama de Guerra** *(perdeu o braço esquerdo salvando a legião; um braço de chama viva numa manopla de bronze o substitui)*
- **Passiva — Coração de Fornalha:** fogo no chão (inclusive o dela) não a machuca e reacende o braço: +10% de velocidade por 2s ao atravessar chamas.
- **Tática — Muralha de Brasas:** risca uma linha de fogo baixo de 8m por 5s: bloqueia visão rasante; quem atravessa leva dano moderado e sai "aceso" (rastro visível 2s).
- **Suprema — Braço Livre:** destrava a manopla por 6s: disparos contínuos em leque curto (lança-chamas) e o dash deixa fogo no chão.
- ⚖️ Água/gelo apagam a muralha e "molham" o braço (+1s de recarga na tática); vento EMPURRA a muralha 3m (§14); a Suprema é telegrafada (rugido + brilho) e ao acabar o braço esfria: 4s sem tática e −15% de velocidade; kitá-la de longe é a contra-jogada.

**2. Ceifadora, a Voz do Vazio** *(morreu afogada no Vazio e voltou emendada em luz; não projeta sombra — a sombra ficou lá)*
- **Passiva — Ecos dos Caídos:** onde alguém morreu há <60s, ela vê o eco espectral dos últimos 3s da luta (replay fantasma, só para ela).
- **Tática — Mão do Vazio:** marca um ponto a 12m: uma mão de sombra irrompe e AGARRA o primeiro inimigo na área por 1,2s (ele ainda conjura).
- **Suprema — Travessia:** rasga o Vazio em linha reta (até 60m): ela e aliados que tocarem o rasgo em 3s atravessam juntos.
- ⚖️ A mão é cortável com 1 golpe corpo a corpo; o rasgo fica ABERTO 3s (inimigos podem entrar atrás); quem atravessa sai revelado 4s e 1s sem conjurar; o eco mostra o passado, nunca posição atual.

### ERRANTE

**3. Véu, a Andarilha** *(caiu no plano espectral aos 12, voltou 7 anos depois sem envelhecer; a mão esquerda é permanentemente semi-espectral, presa por uma luva rúnica)*
- **Passiva — Entrelinha:** após 4s sem atacar/tomar dano, desfoca: semi-translúcida a mais de 20m (nítida de perto).
- **Tática — Atravessar:** 1,5s no plano espectral: invulnerável, mais rápida e atravessa paredes finas (até 2m). Ativação de 0,8s.
- **Suprema — Maré Espectral:** por 5s, ela e aliados num raio de 6m no cast entram JUNTOS no plano espectral: velozes e intangíveis.
- ⚖️ Atravessar deixa um eco visível na entrada e ela sai 1s sem conjurar; no plano vê só silhuetas; na Maré ninguém conjura e um SINO espectral toca no mundo real na posição do grupo (counter sonoro); qualquer dano quebra a passiva.

**4. Corvus, o Caçador** *(cego pelo espírito-lobo com quem hoje divide o corpo; "vê" cheiros como cores)*
- **Passiva — Mundo de Cheiros:** vê trilhas de cheiro dos últimos 60s como fitas de cor (pegadas, portas, resíduo de magia por elemento).
- **Tática — Uivo de Caça:** uivo num raio de 25m: inimigos EM MOVIMENTO ficam com o cheiro aceso (contorno enquanto se moverem, 4s).
- **Suprema — Forma de Lobisomem (transformação):** 30s como besta — corre 40% mais rápido, cura ao abater, todos os rastros do raio brilham.
- ⚖️ O uivo denuncia a posição dele para todo o raio; ficar PARADO esconde do uivo (contra-jogada de disciplina); na forma: **sem magias** (só garras), silhueta maior, uivo de transformação ouvido num raio enorme; trilhas mostram o passado, nunca o agora.

### VIDENTE

**5. Corvomante, o Olho Distante** *(trocou o olho direito pelo pacto: o corvo carrega o olho dele — tudo que o corvo vê, ele vê; na órbita, uma pedra de obsidiana)*
- **Passiva — Meu Olho Voa:** o corvo marca para o esquadrão os inimigos que vê.
- **Tática — Voo do Olho:** assume o corvo (voa, escaneia baús e Torres Arcanas) OU o pousa no ombro de um aliado: em modo sentinela, marca automaticamente inimigos a 20m daquele aliado.
- **Suprema — Grasnido do Fim:** pulso antimagia na área: 50 de dano em escudo, **derruba 1 nível de escudo evolutivo** e destrói armadilhas/construções.
- ⚖️ Enquanto voa o corvo, o corpo fica parado e indefeso; o corvo tem 60 de vida e bate asas audivelmente; em modo sentinela perde o controle manual (recon OU guarda-costas); o Grasnido quebra construções aliadas também.

**6. Olho-de-Éter, o Observador** *(surdo desde a febre da infância; as mariposas-de-éter são os ouvidos dele — sentem vibração e a traduzem em luz)*
- **Passiva — Pó de Éter:** inimigos que conjuraram nos últimos 5s carregam poeira luminosa visível para ele a até 40m.
- **Tática — Enxame Perscrutador:** enxame em linha; quem for tocado tem a conjuração/cura **interrompida** e fica revelado 6s.
- **Suprema — Crisálida:** casulo que eclode após 2s: mariposas pousam nos inimigos num raio grande — revelados por 6s.
- ⚖️ A passiva só sente quem CONJUROU (segurar a magia é a contra-jogada); o enxame tem 1,4s de atraso e túnel estreito; o casulo é destrutível antes de eclodir (60 de vida); fogo em área queima as mariposas e limpa a marca (§14).

### GUARDIÃO

**7. Vitalis, a Mão que Cura** *(a fada Lúmen é a irmã gêmea dela, presa entre mundos desde o afogamento; o Círculo do §16.6 é o projeto de vida de Vitalis)*
- **Passiva — Mãos Livres:** Lúmen reergue aliados caídos enquanto Vitalis continua lutando.
- **Tática — Vai, Lúmen:** envia Lúmen a um aliado a até 30m: cura 8 hp/s por 12s.
- **Suprema — Jardim da Aurora:** círculo de luz por 10s: aliados dentro regeneram 8 hp/s e reerguer/reviver é 50% mais rápido; inimigos dentro não recebem NENHUMA cura.
- ⚖️ Lúmen reerguendo não gera escudo (lição do Apex, mantida); qualquer dano dissipa Lúmen (volta para Vitalis; a cura para); com Lúmen longe, Vitalis fica SEM a passiva (escolha real); o Jardim é visível através de paredes para todos.

**8. Ilusionista, o Espelho** *(passou 3 anos preso num espelho e saiu invertido; um dos reflexos do Baile é o original — nem ele sabe qual)*
- **Passiva — Truque de Fuga:** ao ser derrubado, quebra em cacos de luz: invisível 3s + um reflexo caído no lugar.
- **Tática — Espelho de Mão:** espelho de corpo inteiro fixo por 2s: DEVOLVE até 3 projéteis mágicos como reflexos com 30% do dano.
- **Suprema — Baile de Espelhos:** 5 reflexos que ESPELHAM os movimentos dele em tempo real (invertidos) + 2,5s invisível; reflexo quebrado ofusca 0,5s quem o quebrou de perto.
- ⚖️ O espelho não bloqueia corpo a corpo/área e quebra após 3 devoluções (som de vidro); reflexos não causam dano e movem-se invertidos (observador atento nota); invisibilidade quebra ao conjurar; shimmer visível de perto.

### DOMINADOR

**9. Vex, o Alquimista da Peste** *(perdeu os pulmões na Grande Obra fracassada; um fole alquímico de latão no peito respira por ele — o "gás" é o ar que ele exala, transmutado)*
- **Passiva — Olhos do Miasma:** vê inimigos dentro da própria névoa com contorno verde.
- **Tática — Frascos de Reagente:** até 6 frascos que viram poças inertes (armam em 1s); pisar detona a nuvem local — dano baixo + lentidão.
- **Suprema — A Grande Obra:** transmuta o ar numa área enorme por 12s: névoa que desacelera e SELA consumíveis — ninguém dentro (nem aliado) usa poção, cura ou pergaminho.
- ⚖️ A névoa causa dano BAIXO (nega área, não mata); **vento dispersa** e **fogo incendeia e consome em 2s** (§14); um tiro no frasco detona a nuvem à distância (desperdiçada); o selo da Grande Obra vale para o time DELE também (dois gumes máximo).

**10. Tessa, a Tecelã de Raios** *(sobreviveu a um raio aos 9: cicatrizes de Lichtenberg no braço e um coração sem compasso — corrigido por um marca-passo rúnico que ela mesma forjou)*
- **Passiva — Compasso Rúnico:** o marca-passo regenera escudo lentamente; pergaminhos de escudo restauram tudo; quando o escudo QUEBRA, +15% de velocidade por 2s.
- **Tática — Fio do Tear:** fio de raio entre 2 pontos (até 6 fios): tocar = dano + lentidão + revela; brilha e zumbe a menos de 5m.
- **Suprema — Tear-Mãe:** tear rúnico giratório que **absorve projéteis mágicos inimigos** e TECE o absorvido em escudo para aliados próximos.
- ⚖️ Fios quebram com 1 golpe em qualquer âncora; brilho + zumbido a 5m (counter em cor + forma + som, §10); o Tear-Mãe não absorve curtíssimo alcance nem corpo a corpo, máx. 1 por vez; água no chão conduz o raio dos fios para TODOS, inclusive o time dela (§14).

---

## 4. Regras globais de balanceamento (os "limitadores de sistema")

1. **Tudo é projétil com tempo de viagem** — mirar exige prever movimento; mata o problema de hitscan + habilidade.
2. **Mana única:** magias de ataque consomem mana (regenera devagar em combate). Táticas/Supremas usam **cooldown**, não mana — igual Apex. Impossível "spammar" as duas economias ao mesmo tempo.
3. **Toda Suprema é telegrafada:** som alto + efeito visual de 1 a 4s antes do impacto. Regra de ouro: *se mata rápido, avisa antes*.
4. **Transformações (lobisomem/vampiro) sempre trocam poder por três coisas:** perdem acesso às magias normais, ganham silhueta/som que denuncia, e têm duração fixa com cooldown longo.
5. **Kamikaze balanceada (Suprema de um futuro mago Vanguarda):** canalização de 3s com o corpo brilhando e um zumbido crescente que todos ouvem; ao explodir causa dano enorme em área **mas o conjurador cai no estado "derrubado"** (aliados podem reerguer). Risco real, counter real (atordoar/matar durante a canalização cancela).
6. **Pedra-papel-tesoura elemental:** vento dispersa gás/fumaça · fogo consome gelo e gás · gelo apaga fogo no chão · raio conduz pela água · água amplifica gelo. Todo controle de área tem uma resposta elemental.
7. **Interromper > anular:** habilidades anti-informação (Umbra) e anti-magia (Corvomante) existem para todo scan ter counter.

## 5. Escudo de Magia Evolutivo (números iniciais)

| Nível | Cor | Escudo | Dano causado p/ evoluir (acumulado) |
|---|---|---|---|
| 1 | Branco | 50 | 0 (inicial) |
| 2 | Azul | 75 | 150 |
| 3 | Roxo | 100 | 400 |
| 4 | Dourado | 125 | 900 |

- Vida base: 100. TTK alvo: **~1,5–2,5s contra a VIDA BASE** em duelo parelho
  (mobile-friendly, um pouco mais rápido que Apex).
  **Contra alvo já escudado no nível 1 (EHP 150) a faixa equivalente é ~2,75–3,5s** —
  é a mesma régua vista de outro alvo, não uma segunda régua. O escudo soma 50 sobre
  100 de vida: +50% de alvo, 1,5× mais tempo com a mesma arma.
  *(Explicitado em 19/08 por delegação do Diretor. Os dois números conviviam no
  projeto sem dizer contra o quê mediam, e o relatório comparava a faixa daqui com o
  número do escudo — de onde saíam "5/5 no alvo" e "0/5 no alvo" para o mesmo jogo.
  Medido: os 5 elementos estão DENTRO das duas leituras. Nenhum número de `Balance`
  foi alterado.)*
- Dano em **QUALQUER** aliado nunca conta para evolução (anti-farm). *(Revisto em 19/08: o recorte original — "aliados sendo revividos" — deixava o exploit aberto. Medido: uma dupla subia os dois escudos ao nível 4 atirando um no outro num canto. **Regra geral:** todo sistema que recompensa "dano causado" precisa excluir dano em aliado, ou vira farm sem risco.)*
- Nível 4 adiciona um perk pequeno (ex.: recarga tática 20% mais rápida) — como o Red Evo.

## 6. Lobby e apresentação dos personagens

- **Grade de seleção:** retrato 2D estático de cada mago (barato de produzir, carrega instantâneo no mobile).
- **Tela de detalhes:** o retrato "ganha vida" — animação de apresentação em loop: sorrir, acenar, ajeitar o chapéu, girar o cajado, a fada da Vitalis voando em volta.
- 💡 **Dica de produção solo:** usar **Spine 2D ou Live2D** (mesma técnica de gacha games). Um único artwork bem feito + rigging 2D custa uma fração de um modelo 3D animado e parece premium. As animações de detalhe podem ser 1 idle + 1 apresentação por mago no lançamento.

## 7. Cosméticos e monetização

**Categorias de cosméticos** (do comum ao lendário):
- Roupas/vestes (skins de corpo) · Chapéus (categoria própria — assinatura visual do jogo) · **Vassouras/tapetes/montarias de queda** (equivalente às skins de glider) · Cajados/grimórios (skins de "arma") · **Efeitos de magia** (cor/partícula dos feitiços — o equivalente às skins de arma do Apex, alto valor percebido) · Emotes/poses de vitória · Estandartes e molduras.

**Passe de Batalha — 60 níveis, duas trilhas:**

| | Trilha Grátis | Trilha Arcana (paga: 950 cristais) |
|---|---|---|
| Cosméticos | itens comuns/raros, 1 épico | skins épicas + 2 lendárias (nv. 1 e 60) |
| Cristais de volta | **300** ao longo da trilha | **950** ao longo da trilha (100% de volta) |
| Resultado | jogador free compra o passe a cada ~3 temporadas | quem completa os 60 níveis **nunca mais paga** — modelo Fortnite/Apex clássico, comprovadíssimo em retenção |

- XP do passe por desafios diários/semanais + tempo de partida (nunca só por vitória — protege o jogador casual).
- Nada de vantagem competitiva à venda. Só cosmético. Sempre.

## 8. Caminhos de escopo para dev solo (derivados da ideia)

O jogo completo (BR 3D primeira pessoa, 60 jogadores, netcode) é escopo de estúdio. Escada realista:

1. **Degrau 1 — Roguelike single-player com os kits** *(0 netcode)*: valida o combate elemental, as passivas/táticas/supremas e o "feel" das magias. Os 10 kits acima viram personagens jogáveis contra hordas. Se o combate não for gostoso aqui, não será no BR.
2. **Degrau 2 — Arena 1v1/2v2 por rounds** *(netcode mínimo, salas pequenas)*: testa balanceamento PvP real com 2–4 jogadores por sala. Mapas minúsculos.
3. **Degrau 3 — Mini-BR top-down, 16–20 jogadores** *(estilo Spell Arena, que como vimos já valida esse formato no mobile)*: castelo voador, queda de vassoura, zona fechando, escudo evolutivo — a ideia inteira, em 2D top-down, escopo atingível solo.
4. **Degrau 4 — a visão completa em 3D/primeira pessoa**: só com tração dos degraus anteriores (ou equipe/publisher).

Cada degrau é um jogo lançável que financia e valida o próximo — e o universo, os magos e os cosméticos são 100% reaproveitados entre eles.

---

## 9. DECISÕES OFICIAIS + Sistema de Conjuração Combinada

### Decisões travadas
- **Estratégia de plataforma (atualizada em 20/08/2026):** o produto principal é
  **Arkana 3D em Godot 4.4 para Android**, em terceira pessoa. O projeto Roblox
  continua como **Campo de Provas multiplayer** para validar Sintonia, TTK,
  terreno reativo e equilíbrio com jogadores reais. As duas frentes compartilham
  regras pelo GDD e por `docs/PONTE.md`; código específico de engine não atravessa.
  Modelos da Roblox Toolbox permanecem proibidos.
- **Desenvolvimento em duas velocidades:** personagens, ambientes, pipeline e
  apresentação do Godot podem avançar; mudanças em combate, `Balance` e formato
  de partida dependentes de V1-V5 esperam o playtest humano do Roblox.
- **Direção visual e de câmera:** terceira pessoa sobre o ombro, magia legível,
  mobilidade alta e qualidade de jogo 3D publicado em aparelho intermediário.
- **PÚBLICO-ALVO ETÁRIO: 10+ (Diretor, 19/08).** Combate de fantasia sem sangue,
  sem gore e sem caixa aleatória (§19.1). É a faixa que a ficha de loja declara e a
  base para o questionário IARC/ClassInd — **o questionário é respondido pelo Diretor
  e a classificação final é atribuída pelos órgãos, não por nós**. Postura de dados
  coerente com a faixa: nenhuma coleta de dados entra sem necessidade de produto,
  documentação e aprovação explícita. Minimizar dados é a forma mais barata de
  cumprir o ECA Digital.
- **Nome:** `Arkana: Magos Battle Royale` no lançamento → encurta para `Arkana Royale` quando a marca se sustentar (caminho Free Fire).
- **Descrição curta (80c):** *"Caia do castelo voador, domine os 5 elementos e seja o último mago de pé."*
- **Posicionamento:** não é clone — esqueleto comprovado do gênero + camada de inovação elemental própria. A pergunta-guia do design: **"qual é a invenção que vão copiar DE NÓS?"** Resposta: a Conjuração Combinada.

### Os 5 Elementos (proposta)
**Fogo · Água · Terra · Vento · Raio** (gelo = estado da Água; magma = Fogo+Terra). Cada mago tem afinidade primária, mas encontra pergaminhos de qualquer elemento no loot.

### Conjuração Combinada ("Sintonia") — o pilar de inovação
Dois magos do squad conjuram no mesmo alvo/área dentro de uma janela de 1,5s → as magias se fundem numa **Magia Combinada**, mais forte que a soma das partes.

**Matriz de combos (10 pares):**

| Combo | Magia Combinada | Efeito |
|---|---|---|
| Fogo + Vento | **Tornado Flamejante** | tornado que anda e suga inimigos próximos |
| Fogo + Terra | **Chuva de Magma** | área de lava persistente que nega terreno |
| Fogo + Raio | **Explosão de Plasma** | burst de dano alto em ponto único |
| Fogo + Água | **Cortina de Vapor** | névoa escaldante: cega e causa dano leve |
| Água + Raio | **Eletrocussão** | a água conduz: atordoa todos na poça |
| Água + Terra | **Lamaçal** | lentidão severa em área grande |
| Água + Vento | **Tempestade Torrencial** | apaga fogo, revela pegadas, empurra |
| Terra + Vento | **Tempestade de Areia** | cegueira em área + dano contínuo leve |
| Terra + Raio | **Cristais Carregados** | minas de cristal que atordoam ao quebrar |
| Vento + Raio | **Nuvem Tempestuosa** | nuvem que persegue o alvo marcado por 6s |

**Limitadores (para não quebrar o jogo):**
- Consome a magia dos DOIS conjuradores + cooldown compartilhado longo (os dois ficam "secos" depois — combo errado = squad vulnerável).
  **O custo é cobrado no INÍCIO da canalização, e a INTERRUPÇÃO devolve o cooldown a quem continua VIVO** (revisto em 19/08): a canalização só é cancelável morrendo ou saindo, então, sem a devolução, bastava o parceiro morrer de propósito para tirar 24 s do pilar do jogo do outro — e o alpha pareia DESCONHECIDOS. O limitador anti-spam continua inteiro porque mora no combo que **dispara**, que segue cobrando os dois.
- Canalização visível/sonora de ~1s antes da fusão (dá para interromper os conjuradores)
- Interação com o mapa: todo combo tem counter elemental (Torrencial apaga Magma, etc.)
- Jogador solo/random: **pareado com um parceiro BOT**, que entra na canalização real da Sintonia como um humano entraria (revisto em 19/08 — substitui a **Runa de Eco**, que nunca foi implementada). Razão: o bot exercita o pilar do jogo de verdade, enquanto um item de auto-combinação daria o resultado sem a coordenação, que é justamente o que a pergunta V1 mede.
  ⚠️ **Consequência operacional para playtest:** número ÍMPAR de humanos entrega o último a um bot, e **essa dupla não responde V1** (`docs/ROBLOX.md` §11).

**Por que é o pilar:** é o motivo mecânico para jogar em squad (não só "somar dano"), gera os clipes virais ("olha o combo que a gente fez"), cria teto de habilidade competitivo (times treinam rotações de combo) e é estruturalmente impossível no Apex — identidade que nenhum processo alcança e nenhum concorrente copia rápido.

---

## 10. Identidade Visual

### Logo — "Selo de Arkana"
- **Símbolo:** um pentágono rúnico com **5 gemas elementais** nas pontas (Fogo, Água, Terra, Vento, Raio) e o "A" de Arkana gravado no centro.
- **Sacada de design:** o mesmo selo vira o **medidor de Sintonia no HUD** (as gemas acendem quando um combo está disponível) e o **ícone da loja**. Uma marca, três usos — identidade que o jogador aprende jogando.
- **Wordmark:** ARKANA em caixa alta, com o traço do "K" estilizado como um relâmpago sutil.

### Tipografia (Google Fonts, licença OFL — livre para uso comercial)
| Uso | Fonte | Por quê |
|---|---|---|
| Logo/títulos | **Cinzel** (ou Cinzel Decorative) | serifada épica, "mágica" sem ser medieval demais |
| UI/HUD/números | **Chakra Petch** | angular, legível pequena, cara de competitivo |
| Textos corridos | **Inter** | neutra, perfeita para menus e descrições |

### Paleta
- **Base:** azul-noite arcano `#0B1026` (fundos) + **dourado arcano** `#F0C75E` (marca, raridade lendária, barra de carregamento)
- **Elementos:** Fogo `#FF5A2A` · Água `#2AA7FF` · Terra `#A8763E` · Vento `#8FE8C9` · Raio `#F5D90A`
- Regra de acessibilidade: cada elemento tem **cor + ícone + forma de projétil** distintos (nunca só cor — daltonismo).

### Key art de referência (descrição para gerar/encomendar)
Vista de baixo para cima: o castelo voador rasgando nuvens ao entardecer, dezenas de magos saltando em vassouras e tapetes, deixando **5 trilhas de luz elementais**; ao fundo, a tempestade arcana se formando em anel. Logo centralizada no terço inferior.

---

## 11. Experiência "jogo oficial" — boot, splash e fluxo de menus

### Sequência de abertura (o que dá a sensação de jogo de verdade)
1. **Splash do estúdio** (2s, fundo preto, seu selo de estúdio, som sutil)
2. **Tela de carregamento:** key art em tela cheia → logo ARKANA no centro → **barra de carregamento fina dourada + %** abaixo → **dica rotativa** no rodapé (*"Dica: Água conduz Raio — cuidado onde pisa."* — as dicas ensinam a matriz de combos de graça)
3. **Tela de título:** logo + "PRESSIONE ENTER" pulsando + tema musical do menu
4. **Menu principal**

### Fluxo de menus
```
Título → MENU PRINCIPAL
         ├─ JOGAR (v0.1: Treino Solo na arena)
         ├─ MAGOS (galeria: retrato estático → detalhe com animação de apresentação)
         ├─ ARSENAL (cosméticos — placeholder no protótipo)
         ├─ CONFIGURAÇÕES
         ├─ CRÉDITOS
         └─ SAIR
Em partida: ESC → PAUSA (Retomar / Configurações / Abandonar partida)
```
- Fundo do menu: partículas elementais flutuando lentamente + o Selo de Arkana girando sutilmente. Barato de fazer, caro de aparência.

---

## 12. Configurações (spec completa)

| Aba | Opções |
|---|---|
| **Vídeo** | Tela cheia/Janela · Resolução · Qualidade (Baixa/Média/Alta) · Limite de FPS (30/60/120/ilimitado) · VSync · Contador de FPS on/off |
| **Áudio** | Volume Geral · Música · Efeitos · Interface (sliders 0–100, com preview sonoro ao ajustar) |
| **Controles** | Sensibilidade do mouse (0.1–10.0, slider + campo numérico) · Sensibilidade ao mirar (multiplicador) · Inverter eixo Y · **Remapeamento de teclas** (padrão: WASD mover · mira no mouse · M1 ataque · M2 tática · ESPAÇO esquiva · Q trocar elemento · R suprema · TAB placar) |
| **Jogo** | Idioma (PT-BR/EN) · Modo daltonismo (Protanopia/Deuteranopia/Tritanopia) · Números de dano on/off · Dicas de combo on/off |

- **Persistência:** tudo salvo em `settings.json` local, carregado no boot. Botão "Restaurar padrão" por aba.

---

## 13. Áudio e música

- **Direção:** híbrido orquestral-eletrônico ("épico moderno"): coral + cordas para o tema do menu, percussão tribal + synth na partida, faixa ascendente na queda do castelo.
- **Camadas dinâmicas (meta futura):** música de exploração → adiciona percussão quando inimigos próximos → clímax no top 5.
- **Para o protótipo (fontes gratuitas e legais):** música CC de Kevin MacLeod (incompetech.com, CC-BY com crédito), efeitos e sprites CC0 de **Kenney.nl** e **OpenGameArt.org**, sons avulsos no freesound.org (checar licença por arquivo). Documentar cada asset + licença em `docs/CREDITS.md` desde o dia 1.

---

## 14. Terreno Reativo — regras do sistema (pilar nº 2)

O mapa é dividido numa grade de células, cada uma com **material** (grama, grama alta, árvore, água, rocha, terra) e **estado** (normal, queimando, queimado, congelado, lama, eletrizado). Magias mudam estados:

| Ação | Resultado | Contra-jogada |
|---|---|---|
| Fogo em árvore/grama | incendeia; **propaga** para células vizinhas por **ORÇAMENTO DE COMBUSTÍVEL** (uma rolagem por aresta, nunca chance por tique — ver nota abaixo); após ~8s vira carvão — a cobertura DESAPARECE | Água/Torrencial apaga; Vento espalha (arma de dois gumes) |
| Fogo em grama alta | queima e **revela** quem estava escondido | — |
| Água em lago | **congela a superfície** por ~10s: vira ponte/rota nova | Fogo derrete; quem estiver em cima cai |
| Raio em água/lago | **eletrocuta** todos em contato com a água | sair da água; Terra isola |
| Terra em qualquer chão | ergue **muro de pedra** (cobertura destrutível, ~200hp) | qualquer dano destrói; Raio racha mais rápido |
| Água + chão de terra | **lamaçal**: lentidão severa na área | Fogo seca; Vento não afeta |
| Vento em fogo/névoa/gás | **espalha ou dissipa** (decisão tática) | — |

> **NOTA DE MEDIÇÃO (19/08) — por que ORÇAMENTO e não chance por tique.** O modelo
> original ("chance por tick") foi implementado e **medido**: 30% por vizinho × 16
> tiques = **99,67% acumulado**, e o incêndio acendia **380 de 380 células em 100% das
> rodadas**. O mapa inteiro virava carvão toda partida e o terreno deixava de ser
> escolha. O modelo atual faz **uma rolagem por aresta** e gasta um **orçamento de
> combustível** (`fuelBudget`), que o Vento também paga: mede **~44 células**.
> **Para regular o tamanho do incêndio, mexa no ORÇAMENTO — nunca na chance.**
> Previsível é aprendível, e o jogador planeja a jogada. Quem implementar esta seção
> em outra engine e voltar à chance por tique reimplementa a carbonização.
>
> **NOTA DE DESIGN (19/08) — fogo amigo é ASSIMÉTRICO.** A magia **mirada** não atinge
> o parceiro; o **terreno atinge todos** (a arma de dois gumes desta seção continua
> valendo integralmente). Razão: o produto pareia **desconhecidos** em dupla, e com
> fogo amigo direto ligado um estranho arruína a sessão do outro — a pergunta V1 ("a
> Sintonia é divertida entre dois jogadores?") ficaria ilegível por comportamento, não
> por design. A contrapartida é que a dupla ainda pode se queimar pelo mapa, que é
> onde a lição de §14 deve doer.

**Por que isso importa:** junto com a Sintonia, o terreno reativo é a segunda invenção que o Apex não tem. O mapa deixa de ser cenário e vira **recurso** — queimar a floresta do inimigo é uma jogada, congelar o lago é uma rotação. O Godot implementa isso com uma grade lógica separada da apresentação 3D, preservando previsibilidade e orçamento mobile.

---

## 15. Implementação atual

### Stack
- **Godot 4.4 / GDScript:** produto principal 3D e export Android.
- **Roblox / Luau / Rojo:** Campo de Provas multiplayer e telemetria de playtest.
- **Git:** fonte de verdade do código, documentos e assets canônicos.

### Estado entregue
1. ☑ Boot, título, menu e vitrine de personagens.
2. ☑ Ilha 3D, câmera sobre o ombro e controles de toque/desktop.
3. ☑ Partida curta contra bots com HUD, vitória e derrota.
4. ☑ Cinco elementos com cor, forma, mana e cadência próprias.
5. ☑ Terreno reativo 3D com fogo, gelo, eletricidade, Terra e Vento.
6. ☑ Áudio procedural e feedback de combate.
7. ☑ APK Android de debug exportado e verificado.
8. ☐ G3: personagem com rig completo, três biomas e VFX de qualidade final.
9. ☐ G4: loop BR completo no Godot, incluindo squads e Sintonia.
10. ☐ G5: multiplayer dedicado no Godot.

Regra de processo: **toda decisão de design entra no GDD antes de virar código.**
O documento manda; as engines implementam.

---

## 16. Pesquisa aplicada — Armas Arcanas, Runas, Química e Alquimia

### 16.1 Referências validadas pela pesquisa
- **Zelda: Breath of the Wild ("chemistry engine")** — o padrão-ouro do nosso terreno reativo. 3 regras formais: (1) elementos mudam o estado de materiais; (2) elementos mudam o estado de outros elementos (água apaga fogo); (3) materiais não mudam materiais. Resultado: "gameplay multiplicativo" — adotar essas 3 regras como arquitetura do nosso sistema.
- **Spellbreak (manoplas)** — validou a fantasia da manopla elemental num BR: manopla primária da classe + segunda manopla saqueada, raridades comum→lendária, combos entre elementos (bola de fogo + tornado = tornado flamejante), runas e talentos. O jogo morreu por retenção/monetização, não pela mecânica — o mercado está aberto e a mecânica é livre.
- **Noita (construção de varinhas)** — o sistema mais profundo já feito de "magia modular": varinhas com stats (atraso de conjuração, recarga, mana, dispersão, capacidade) + feitiços e MODIFICADORES encaixáveis em slots. É o molde do nosso sistema de Runas.
- **GitHub — simulação de terreno:** repositórios open-source de "falling sand"/autômatos celulares (sand_sim em JS/p5 sob MIT, sandspiel, simulake) e modelos de propagação de incêndio florestal com 4 estados (sem combustível / intacto / queimando / queimado) que consideram vento e relevo — literalmente a máquina de estados da nossa seção 14, já resolvida academicamente. Estudar antes de codar o v0.1.

### 16.2 Armas Arcanas (tiers de conjurador)
Separação limpa: **personagem = habilidades (passiva/tática/suprema) · arma arcana = ataque e elementos.**

| Arma | Papel (analogia) | Perfil |
|---|---|---|
| **Varinha** | SMG/pistola | comum, conjuração rápida, curto alcance, dano baixo — todo mundo dropa com uma |
| **Cajado** | rifle/sniper | médio/raro, alcance e dano maiores, conjuração mais lenta, canaliza supremas com bônus |
| **Manopla** | arma lendária | SÓ em **Baús Celestiais** (drop do céu); porta **2 elementos simultâneos** e conjura com o **estalar de dedos** (cast quase instantâneo). A fantasia Roy Mustang — assinatura sonora do estalo = terror psicológico no mapa |

⚖️ Limitador da manopla: 2 elementos fixos por manopla (ex.: Fogo+Vento), sem troca — poder alto, flexibilidade menor; brilho visível nas mãos denuncia o portador.
⚠️ Nota legal: o CONCEITO estalar-dedos/luva é mecânica (livre); nunca copiar o símbolo/desenho específico da luva do personagem de FMA.

### 16.3 Runas de Aprimoramento (attachments de magia)
Encontráveis no loot, encaixam na arma arcana (slots por raridade: varinha 1 · cajado 2 · manopla 3):

| Runa | Equivalente em armas de fogo | Efeito |
|---|---|---|
| **Lente de Foco** | mira | −dispersão, +velocidade de projétil |
| **Reservatório** | carregador estendido | +conjurações antes da recarga de mana da arma |
| **Catalisador** | empunhadura | −atraso entre conjurações |
| **Estabilizador** | coronha | −recuo arcano, projétil não "cai" com a distância |
| **Prisma** (rara) | — sem equivalente, nossa invenção | o ataque básico ganha efeito secundário fraco do elemento (fogo deixa brasa, gelo deixa lentidão leve) |

### 16.4 Química real como camada de profundidade (o "conhecimento é poder" dos isekai)
Regras cientificamente verdadeiras = intuitivas de aprender e honestas de sofrer:
- **Triângulo do fogo:** fogo precisa de oxigênio → **Vento sufoca chamas pequenas** (rouba o ar) mas **ALIMENTA incêndios grandes**. Decisão de risco real a cada uso.
- **Explosão de vapor:** Água jogada em magma/área de lava → explosão de vapor que empurra e escalda os dois lados. Counter com custo.
- **Fulgurito:** Raio em área de areia → vitrifica: cria **cobertura de vidro frágil** (1 tiro destrói, mas bloqueia visão). Invenção nossa a partir de ciência real.
- **Condução:** já na seção 14 — água conduz raio; Terra (pedra) isola.
- **Hipotermia:** ficar molhado (pós-Torrencial) faz o gelo aplicar lentidão dobrada por 5s.
A dica rotativa do carregamento ensina uma regra científica por vez — o jogador aprende química jogando.

### 16.5 Alquimia — Troca Equivalente (crafting temático)
**Mesas de Transmutação** espalhadas no mapa (nosso "replicador", com identidade FMA-inspirada mas círculos alquímicos genéricos/próprios):
- Sacrifique loot de valor equivalente → forje uma Runa, melhore a arma 1 raridade, ou crie pergaminhos de cura.
- **Regra de ouro da alquimia: nada é criado do zero** — todo ganho custa algo do inventário. Balanceamento embutido na própria fantasia.
- Futuro: um mago Alquimista (Dominador) cuja tática transmuta o terreno (pedra↔lama) e cuja suprema é uma Troca Equivalente ofensiva (converte o escudo do inimigo em cura para o squad, com canalização longa e interrompível).

### 16.6 Ressurreição — Círculo de Invocação
Validada: é o "respawn" comprovado do gênero, tematizado. Aliado morto deixa uma **Essência**; leve-a a um **Círculo de Invocação** (pontos fixos no mapa, ritual de 7s, visível e sonoro) → o aliado **retorna apenas com a varinha comum e as magias base** — sem runas, sem escudo evoluído, sem loot. Traz de volta o jogador, não o poder. (Exatamente a sua ideia; exatamente o que 7 anos de Apex provaram que funciona.)

---

## 17. Escolas de Magia — arquitetura para necromancia e além

### A regra de arquitetura (protege o balanceamento para sempre)
O jogo tem **duas camadas de magia** com papéis separados:

1. **Evocação (os 5 elementos)** = a "física do mundo". É a camada das ARMAS: todo dano direto, toda Sintonia, toda reação de terreno. Universal — qualquer mago usa qualquer elemento.
2. **Escolas Ocultas** = a camada dos PERSONAGENS. Definem passiva/tática/suprema. Nunca competem com os elementos em dano direto — fazem o que elemento não faz: informação, controle, invocação, sustain, ilusão.

Isso já está implícito no elenco (e agora vira regra explícita):

| Escola | Magos que já a usam | Espaço para novos |
|---|---|---|
| Sangue | Mercúrio (HP por velocidade) | ✔ |
| Ilusão | Ilusionista | ✔ |
| Luz/Cura | Vitalis | ✔ |
| Sombra/Vazio | Véu, Nosferatu | ✔ |
| Adivinhação | Corvus, Olho-de-Éter | ✔ |
| Invocação | Corvomante (familiar) | ✔ |
| Transmutação/Alquimia | (futuro Alquimista, seção 16.5) | ✔ |
| **Necromancia** | — | **primeiro conceito abaixo** |

Benefício de roadmap: cada escola é uma trilha infinita de personagens novos por temporada — o "motor de lendas" do Apex, com tema nosso.

### Conceito: Ossaria, a Necromante (Dominadora)
A sacada BR-nativa: **o poder dela cresce onde houve morte.**
- **Passiva — Colheita de Almas:** cada jogador que morre num raio de 50m deixa uma alma visível só para ela; absorver almas recarrega a tática mais rápido. Ela "sente" os locais de massacre do mapa.
- **Tática — Erguer os Caídos:** ergue até 2 esqueletos NOS LOCAIS onde jogadores reais morreram naquela partida. Esqueletos perseguem e atacam (dano baixo), servem de distração, escudo de carne e "scanner" (revelam quem atacam).
- **Suprema — Marcha Fúnebre:** por 12s, todos os esqueleteos possíveis se erguem na área e uma névoa mortuária reduz a cura inimiga pela metade dentro dela.
- ⚖️ Limitadores: esqueletos têm 50hp e quebram com 1–2 golpes; **Fogo os destrói instantaneamente** e Vento os empurra (interação com a camada elemental); som de ossos constante denuncia; em início de partida (poucas mortes) ela é propositalmente a mais fraca — sua curva é inversa: fraca cedo, aterrorizante no fim.
- **Emergência narrativa de graça:** zonas de grandes lutas viram "cemitérios" que ela domina — o mapa conta a história da partida e vira recurso tático dela.

### Regras para TODA nova escola entrar no jogo
1. Não pode causar mais dano direto que os elementos (dano é papel da Evocação)
2. Precisa de pelo menos 1 interação com a camada elemental (ex.: fogo queima esqueletos)
3. Precisa de counterplay visível/audível (som de ossos, névoa visível)
4. Entra primeiro como 1 personagem — se a escola engajar, ganha mais magos nas temporadas seguintes

---

## 18. Ideias de Assinatura [STATUS: ✅ DECIDIDO — todas as 7 aprovadas em 17/08/2026]

### 18.1 ⭐ Escola do Folclore Brasileiro (a joia da coroa)
Nenhum jogo do gênero no mundo tem isso — e o maior mercado de BR mobile é o Brasil:
- **Curupira (Errante):** passiva "Pés Virados" — suas pegadas apontam na DIREÇÃO OPOSTA. É o counter temático perfeito ao rastreio do Corvus: mecânica real, não skin. Tática: cipós que prendem; suprema: a floresta luta por ele (árvores próximas atacam).
- **Saci (Errante):** redemoinho como esquiva, rouba 1 item do inimigo atingido, travessuras (troca posições de baús).
- **Boitatá (Vanguarda):** serpente de fogo que protege área; **Iara (Guardiã):** canto que atrai/cura; **Cuca (Dominadora):** sono em área.
- Por que importa: identidade impossível de copiar, orgulho do público BR (marketing orgânico garantido), e diplomacia cultural no mercado global — o jogo "do Brasil" como Genshin é "da China".

### 18.2 Castelo Voador como lobby vivo
Antes da queda, os 60 magos andam LIVRES pelo castelo (treino de mira na biblioteca, lojinha, mural da temporada) enquanto ele sobrevoa o mapa real — a rota de voo é visível pelas janelas e muda o planejamento da queda. O castelo vira a "casa" da comunidade e elimina tela de espera morta.

### 18.3 Grimório de Descobertas (progressão por experimentação)
Além de XP: um grimório pessoal onde cada interação descoberta pela 1ª vez vira uma página iluminada ("Congelou um lago", "Fez o combo Eletrocussão", "Apagou fogo grande com Torrencial... ops, alimentou"). É tutorial disfarçado, colecionismo de longo prazo (Grimório 100%) e transforma a química do jogo em caça ao tesouro — filosofia BotW/Noita aplicada à progressão.

### 18.4 Presságios (modificador por partida)
No carregamento, uma carta de presságio anuncia a condição arcana da partida: "Lua de Sangue" (curas +20%), "Maré Alta" (mais lagos — buff indireto de Água/Raio), "Noite Sem Vento" (fumaças duram mais). Custo baixo (ajustes numéricos), variedade infinita, e dá pauta diária para criadores de conteúdo ("o presságio de hoje quebrou o meta").

### 18.5 Trilha sonora elemental reativa
Cada elemento tem um "stem" musical (camada instrumental). Durante a luta, a música mistura os stems dos elementos em uso: luta de Fogo×Raio soa diferente de Água×Vento. Toda luta tem trilha única — assinatura sensorial do jogo, e cada clipe compartilhado carrega um som que nenhum outro jogo tem. Tecnicamente: áudio em camadas sincronizadas no Godot.

### 18.6 Espírito Errante (pós-morte) + Selo do Campeão
- **Espírito Errante:** ao morrer em squad, vira um espírito: não ataca, mas pode dar UM "arrepio" (revela 1 inimigo por 2s para os aliados). O morto continua participando — retenção mobile — com counterplay (o inimigo sente o arrepio).
  **Sem prazo em segundos** (revisto em 19/08): o espírito dura até ser resgatado, até a dupla gastar sua única volta, ou até a zona entrar na fase final. Relógio de 60 s transformava a morte em espera; consumo + fase transforma em **disputa por altar**, que é o que a feature existe para criar.
- **Selo do Campeão:** o vencedor carimba o Selo de Arkana no ponto final do mapa e o jogo gera automaticamente um card compartilhável (elementos usados, combos, dano) — o loop viral de fim de partida embutido no produto.

### 18.7 Ping de Sintonia
Extensão social da mecânica central: um ping dedicado "COMBO?" onde você propõe o elemento — o aliado aceita com 1 toque e os dois veem o mesmo alvo marcado. Faz a Sintonia funcionar entre desconhecidos sem microfone — a lição do ping do Apex aplicada à NOSSA invenção.

### Priorização sugerida (custo × impacto)
1. **Selo do Campeão** e **Presságios** — baratos, impacto viral imediato (v0.2/v0.3)
2. **Grimório** — médio custo, segura o jogador solo (v0.3)
3. **Ping de Sintonia** — obrigatório no primeiro multiplayer
4. **Folclore BR** — 1º personagem (Curupira) já no elenco de lançamento como bandeira
5. **Castelo-lobby** e **trilha reativa** — pós-validação, quando houver multiplayer real

---

## 19. Realidade Mobile, Juramento Anti-P2W e Ordem de Execução

### 19.1 Juramento de Arkana (política inegociável — e ativo de marketing)
**Este jogo nunca será pay-to-win.** Formalizado como regra de produto:
1. Todo item que afeta poder (armas arcanas, runas, pergaminhos) SÓ existe dentro da partida, achado no loot — nunca vendido, nunca em passe, nunca em caixa.
2. Loja e passe vendem exclusivamente cosméticos (seção 7). Zero atributo em skin.
3. Novos magos: desbloqueáveis com moeda GRATUITA ganha jogando. **Dinheiro não
   compra nem acelera acesso a mago** — todo mago é balanceado para o mesmo teto.
   *(Corrigido em 18/08/2026: a versão anterior permitia pagar para acelerar o
   desbloqueio, o que contradizia o próprio Juramento. Auditoria externa pegou a
   contradição; a regra passa a ser literal — dinheiro nunca compra poder,
   personagem, cooldown, loot, reroll, boost competitivo nem acesso antecipado.)*
4. **Nenhuma caixa aleatória paga, em nenhuma plataforma.** Além de coerente com
   a marca, evita a complexidade jurídica do ECA Digital (em vigor desde
   17/03/2026), que restringe mecânicas de item aleatório pago a menores de 18
   anos no Brasil. Venda direta resolve o mesmo problema comercial sem isso.
5. Publicar o Juramento na página da loja — o compromisso vira diferencial de marketing num mercado mobile saturado de P2W.

### 19.2 Guardrails de escopo mobile (o que NÃO fazer)
O inimigo nº 1 de um projeto solo/equipe pequena é feature demais. Regras duras:
- **20 jogadores por partida** no lançamento, não 60. (Spell Arena valida 20; menos netcode, partidas de 4–6 min ideais para mobile. O castelo-lobby com 60 é visão de futuro.)
- **Orçamento de performance:** 60fps em aparelho intermediário e jogável em aparelho fraco com "Modo Névoa" (gráficos lite). Free Fire conquistou o Brasil rodando em celular fraco — alcance É estratégia de viralidade aqui.
- Teto de efeitos: máx. de partículas por magia definido em config; terreno reativo por células (barato) e nunca por pixel (Noita é referência de design, não de tech mobile).
- Toda feature nova responde antes: "funciona num toque de polegar em tela de 6 polegadas? roda no aparelho fraco?" Se não, vai para o backlog de futuro, não para o build.

### 19.3 Controles de toque (spec para o teste de APK)
- **TOQUE — mira e disparo em UM gesto (obrigatório):** joystick virtual esquerdo
  (movimento); do lado direito, **arrastar a partir do botão da magia mira e soltar
  dispara**; toque curto dispara na direção da câmera; voltar ao centro cancela.
  Tática, Esquiva e Suprema seguem o mesmo padrão · troca de elemento em carrossel
  acima dos botões.
  *(Promovido em 19/08 por delegação do Diretor, vindo do que funcionou no Roblox.
  Dois gestos separados — mirar e depois atirar — foram REPROVADOS em aparelho real
  no teste de APK de 17/08. Esta linha é a ponte oficial entre o aprendizado e
  todas as implementações.)*
- **TECLADO/MOUSE é um esquema DIFERENTE, não uma adaptação do de toque** (Diretor,
  19/08): no PC a mira é contínua pelo cursor e o disparo é um botão próprio. Não se
  força um esquema no outro — o que se compartilha é a REGRA do jogo (dano, mana,
  cooldown, alcance), nunca o gesto.
- Alvos de toque ≥ 48dp, HUD com zonas seguras para os polegares, layout dos botões **editável e escalável** pelo jogador (padrão dos BRs mobile)
- **Dois esquemas:** Simples (conjuração assistida leve ao tocar) e Avançado (mira 100% manual, maior teto de habilidade) — ambos gratuitos para todos, competitivo pareia por esquema. Habilidade decide, sempre.

### 19.4 Pipeline PC → APK
1. O mesmo projeto Godot roda no desktop para desenvolvimento e no Android para aceite.
2. `godot/export/build_apk.sh` importa, exporta e verifica o APK de debug.
3. Cada marco fecha com selftests, boot headless, export limpo e teste no aparelho.
4. Release usa AAB e keystore própria somente na fase de publicação.

### 19.5 Ordem de execução
O contrato ativo é `docs/ROADMAP_3D.md`: G0-G2 concluídos; G3 é o próximo marco,
seguido por G4 (loop BR), G5 (multiplayer dedicado) e G6 (loja). Roblox continua
em paralelo até responder V1-V5 com jogadores reais.

### 19.6 Últimas sugestões — ideias "de contenção" (as mais valiosas agora)
1. **Modo Treino Offline como produto, não só teste:** a partida Godot contra bots permanece como modo do jogo final. Jogável sem internet, reduz dependência de dados móveis e transforma a validação local em feature entregue.
2. **Bots nas primeiras partidas do jogador novo** (padrão Fortnite/Free Fire): as 3 primeiras partidas misturam bots discretamente — o novato ganha confiança antes de encarar veteranos. Retenção de dia 1, tecnologia que já teremos pronta.
3. **Uma única arena no lançamento, profundamente polida**, em vez de mapa gigante: arena média com todos os biomas reativos (lago, floresta, areal, ruínas). Mapa grande é a feature mais cara de um BR e a menos sentida em partidas de 20. Crescer o mapa vem com a base de jogadores.
