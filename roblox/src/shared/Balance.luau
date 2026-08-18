--!strict
-- ============================================================================
-- ARKANA (Roblox) — DADOS DE BALANCEAMENTO
-- Porte fiel de src/core/balance.ts do protótipo 2D. Fontes: GDD §4, §5, §14.
-- Regra do projeto: "design antes de código; números viram dados; o código lê
-- dados". Rebalancear o jogo = editar este arquivo. NUNCA constante inline.
-- Dono: COORDENADOR.
--
-- CONVERSÃO DE UNIDADES (2D → 3D):
--   protótipo: 1 célula = 32 px · aqui: 1 célula = Grid.CELL_SIZE studs (4)
--   logo, 8 px ≈ 1 stud. Valores de DANO/MANA/COOLDOWN portam 1:1 (sem unidade).
--   Valores de VELOCIDADE foram convertidos e depois AJUSTADOS para o feel 3D —
--   ver `speed` abaixo: são knobs de calibração, não verdades matemáticas.
-- ============================================================================

export type BasicSpell = {
	dmg: number,
	speed: number, -- studs/s
	manaCost: number,
	radius: number, -- studs
	fireRate: number, -- segundos entre disparos
	terrainRadius: number, -- células
}

export type TacticSpell = {
	dmg: number,
	speed: number,
	radius: number,
	cooldown: number, -- segundos
	terrainRadius: number,
}

local Balance = {}

--- 8 px do protótipo ≈ 1 stud aqui (32px/célula ÷ 4 studs/célula)
Balance.PX_PER_STUD = 8

Balance.player = {
	hp = 100,
	--- studs/s. 2D: 230 px/s ≈ 28.7 studs/s; baixado para 22 porque em 3ª pessoa
	--- a câmera exagera a sensação de velocidade. CALIBRAÇÃO — ajustar no playtest.
	speed = 22,
	jumpPower = 50,
	manaMax = 100,
	--- mana/s — regenera devagar (GDD §4 regra global 2: não dá para spammar)
	manaRegen = 16,
	dodge = {
		dist = 19, -- studs (2D: 150 px)
		duration = 0.2,
		--- HIPÓTESE DE TESTE (auditoria externa 18/08): 0,28 s de i-frame com CD
		--- de 1,6 s dava até ~17,5% do tempo em invulnerabilidade — a esquiva
		--- apagava ataque em vez de reposicionar. Valores antigos: 0.28 / 1.6.
		iframes = 0.18,
		cooldown = 2.6,
	},
}

-- Escudo de Magia Evolutivo (GDD §5)
Balance.shield = {
	--- capacidade por nível 1..4
	caps = { 50, 75, 100, 125 },
	--- dano CAUSADO acumulado para atingir cada nível (índice = nível)
	thresholds = { 0, 150, 400, 900 },
	--- nível 4 ganha perk pequeno (GDD §5): recarga tática 20% mais rápida
	lv4TacticCooldownMult = 0.8,
}

-- Magias por elemento (chaves = Elements.Id)
Balance.elements = {
	Fire = {
		basic = { dmg = 13, speed = 62, manaCost = 9, radius = 1.0, fireRate = 0.26, terrainRadius = 1 } :: BasicSpell,
		tactic = { dmg = 22, speed = 52, radius = 1.5, cooldown = 6.0, terrainRadius = 3 } :: TacticSpell,
	},
	Water = {
		basic = { dmg = 11, speed = 67, manaCost = 8, radius = 1.0, fireRate = 0.23, terrainRadius = 1 } :: BasicSpell,
		tactic = { dmg = 16, speed = 57, radius = 1.5, cooldown = 5.5, terrainRadius = 3 } :: TacticSpell,
	},
	Earth = {
		basic = { dmg = 16, speed = 52, manaCost = 11, radius = 1.0, fireRate = 0.34, terrainRadius = 1 } :: BasicSpell,
		--- a tática de Terra ergue MURO na área de impacto (GDD §14)
		tactic = { dmg = 10, speed = 47, radius = 1.5, cooldown = 7.0, terrainRadius = 2 } :: TacticSpell,
	},
	Wind = {
		basic = { dmg = 9, speed = 77, manaCost = 7, radius = 1.0, fireRate = 0.2, terrainRadius = 1 } :: BasicSpell,
		tactic = { dmg = 12, speed = 65, radius = 1.75, cooldown = 5.0, terrainRadius = 3 } :: TacticSpell,
	},
	Lightning = {
		basic = { dmg = 15, speed = 87, manaCost = 10, radius = 0.9, fireRate = 0.3, terrainRadius = 1 } :: BasicSpell,
		tactic = { dmg = 20, speed = 75, radius = 1.25, cooldown = 6.5, terrainRadius = 3 } :: TacticSpell,
	},
}

-- Terreno reativo (GDD §14; modelo de 4 estados do fogo — GDD §16.1)
Balance.terrain = {
	burnDmgMin = 4,
	burnDmgMax = 10,
	burnTick = 0.5,
	--- CORREÇÃO CRÍTICA (auditoria externa 18/08): antes era 30% por tick, por
	--- vizinho, durante 8s = até 16 rolagens → 1-(1-0.3)^16 ≈ **99,67%**. Ou seja,
	--- "floresta conectada pega fogo inteira, com certeza" — não é propagação,
	--- é carbonização garantida. Agora é UMA ROLAGEM POR ARESTA: quando uma
	--- célula começa a queimar, cada vizinho combustível recebe UMA chance.
	spreadChancePerEdge = 0.5,
	--- teto de células por incêndio (rede de segurança independente do sorteio):
	--- ao esgotar, a ignição para de propagar mesmo com floresta em volta.
	---
	--- DECISÃO DO COORDENADOR (18/08): em floresta densa o orçamento esgota em
	--- 100% das ignições, então **este é o botão que define o tamanho do
	--- incêndio** — a rolagem por aresta decide só a FORMA da frente. Medido:
	--- ~44 células por tática de Fogo. É deliberado: incêndio previsível é
	--- aprendível ("uma bola de fogo abre mais ou menos ISTO de clareira") e o
	--- jogador consegue PLANEJAR a jogada, que é o pilar do GDD §14. Tamanho
	--- aleatório (36–100) daria drama e tiraria estratégia.
	--- Para regular incêndio: mexa AQUI, não no sorteio.
	fuelBudget = 16,
	--- OBSOLETO — mantido só para não quebrar leitura antiga; não usar.
	propagateChance = 0.30,
	--- célula queima ~8s e vira carvão (cobertura DESAPARECE)
	burnDuration = 8.0,
	--- lago congelado vira ponte por ~10s
	freezeDuration = 10.0,
	electrifyDuration = 1.6,
	electrifyDps = 30,
	mudDuration = 12.0,
	mudSlowMult = 0.45,
	--- muro de pedra ~200hp (GDD §14)
	wallHp = 200,
	wallDuration = 20.0,
	--- dano de projétil em muros/árvores ao colidir
	projectileCellDmg = 34,
	tallGrassSpeedMult = 0.92,
	--- LIÇÃO DO PROTÓTIPO 2D (docs/HISTORICO.md): fogo consome COBERTURA
	--- (floresta e grama alta), nunca o campo aberto — senão carboniza o mapa.
	grassBurns = false,
}

-- ============================================================================
-- EXPERIMENTO A/B (auditoria externa 18/08) — o playtest de duplas compara duas
-- configurações de Sintonia. Trocar aqui é a ÚNICA coisa que muda entre coortes:
-- nenhum código lê variante, todos leem `Balance.sintonia` já resolvido.
--   A = configuração atual (punitiva, telegrafada)
--   B = mais permissiva: canaliza mais rápido, volta antes, explode menos
-- Hipótese: B aumenta uso intencional sem virar spam nem "botão de deletar".
-- ============================================================================
Balance.experiment = {
	sintoniaVariant = "A", -- "A" | "B"
	--- Ritmo da partida. O cronograma atual (5 fases × [30s parada + 25s
	--- fechando]) dá 4min35 — a auditoria externa julgou agressivo demais para
	--- um jogo que precisa ensinar tempo de viagem, esquiva, terreno E
	--- coordenação de dupla ao mesmo tempo. Testar contra durações maiores.
	---   "blitz" = 4min35 (atual) · "media" = 5min30 · "longa" = 7min15
	zoneVariant = "media",
}

local ZONE_VARIANTS = {
	blitz = { holdTime = 30, shrinkTime = 25 }, -- 275 s
	media = { holdTime = 36, shrinkTime = 30 }, -- 330 s
	longa = { holdTime = 47, shrinkTime = 40 }, -- 435 s
}

local SINTONIA_VARIANTS = {
	A = { channel = 1.0, sharedCooldown = 24.0, dmgMult = 2.35 },
	B = { channel = 0.75, sharedCooldown = 18.0, dmgMult = 2.0 },
}

-- Sintonia / Conjuração Combinada (GDD §9) — pilar de inovação
local sintoniaVariant = SINTONIA_VARIANTS[Balance.experiment.sintoniaVariant] or SINTONIA_VARIANTS.A
Balance.sintonia = {
	--- janela para o segundo conjurador entrar no combo (igual nas duas coortes:
	--- não se dificulta a coordenação antes de provar que ela é entendida)
	window = 1.5,
	--- canalização visível/sonora antes da fusão (dá para interromper)
	channel = sintoniaVariant.channel,
	--- cooldown compartilhado longo: combo errado = squad vulnerável
	sharedCooldown = sintoniaVariant.sharedCooldown,
	--- distância máxima entre os alvos para considerar "mesmo alvo/área"
	targetRadius = 14,
	dmgMult = sintoniaVariant.dmgMult,
}

Balance.bots = {
	hp = 100,
	shield = 50,
	speed = 15,
	sightRadius = 55,
	sightConcealed = 16,
	attackRange = 62,
	fireRate = 0.95,
	dmg = 8,
	projSpeed = 54,
	projRadius = 1.0,
	--- distância (células) de fogo que dispara a fuga
	fleeRadiusCells = 2,
	respawn = 6.0,
}

-- Loop de battle royale (GDD §19.2: 20 jogadores no lançamento, não 60)
Balance.match = {
	maxPlayers = 20,
	--- espera por jogadores antes de começar (segundos)
	lobbyWait = 25,
	minPlayersToStart = 2,
	--- preenche a partida com bots (GDD §19.6 — retenção de dia 1)
	fillWithBots = true,
	--- RETORNO CONTESTÁVEL (GDD §16.6 + §18.6). Estes números moravam no
	--- Match.luau esperando o campo existir aqui — regra do projeto: "números
	--- viram dados". O ritual traz de volta o JOGADOR, nunca o poder.
	revive = {
		--- canalização no altar: longa o bastante para ser contestada, curta o
		--- bastante para valer a tentativa (GDD §16.6 diz 7s; auditoria pediu ~6)
		channel = 6.0,
		--- sair do raio ZERA a canalização (não pausa) — o ritual tem de doer
		radius = 12,
		pickup = 10,
		--- pontos FIXOS: o inimigo sabe onde o revive acontece. É isso que
		--- transforma o ritual em disputa em vez de menu de espera.
		altars = 4,
		ringFrac = 0.45,
		hoverY = 12,
		--- alcance do arrepio, medido do PRÓPRIO espírito (senão o morto vira
		--- radar do mapa inteiro)
		pingRange = 70,
		pingHold = 2.0,
	},

	zone = {
		--- raios em studs: começa cobrindo a arena e fecha em etapas
		startRadius = 170,
		endRadius = 12,
		phases = 5,
		--- resolvidos por `Balance.experiment.zoneVariant` (ver acima): o código
		--- continua lendo holdTime/shrinkTime e não sabe que existe variante
		holdTime = (ZONE_VARIANTS[Balance.experiment.zoneVariant] or ZONE_VARIANTS.blitz).holdTime,
		shrinkTime = (ZONE_VARIANTS[Balance.experiment.zoneVariant] or ZONE_VARIANTS.blitz).shrinkTime,
		--- dano por segundo fora da zona, sobe a cada fase
		dpsPerPhase = { 2, 4, 7, 12, 20 },
	},
}

Balance.controls = {
	--- assistência do esquema Simples (GDD §19.3): a mira gruda no inimigo mais
	--- próximo dentro de um cone na direção do arrasto
	aimAssist = {
		--- HIPÓTESE DE TESTE (auditoria externa 18/08): 30° é permissivo demais
		--- como valor universal — vira aquisição de alvo, não assistência, e
		--- contamina a leitura de skill. Valor antigo: 30.
		coneDeg = 18,
		range = 65,
	},
	stickDeadzone = 0.15,
	--- arrasto mínimo (px de tela) para valer como mira direcional
	aimDeadzonePx = 14,
	--- LIÇÃO DO TESTE EM APARELHO (18/08): mirar e atirar são UM gesto.
	--- Soltar o dedo antes disto = disparo rápido na direção da câmera.
	tapMaxDuration = 0.18,
}

return Balance
