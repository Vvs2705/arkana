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
		iframes = 0.28,
		cooldown = 1.6,
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
	--- chance por tick de propagar para CADA vizinho inflamável
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

-- Sintonia / Conjuração Combinada (GDD §9) — pilar de inovação
Balance.sintonia = {
	--- janela para o segundo conjurador entrar no combo
	window = 1.5,
	--- canalização visível/sonora antes da fusão (dá para interromper)
	channel = 1.0,
	--- cooldown compartilhado longo: combo errado = squad vulnerável
	sharedCooldown = 24.0,
	--- distância máxima entre os alvos para considerar "mesmo alvo/área"
	targetRadius = 14,
	dmgMult = 2.35,
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
	zone = {
		--- raios em studs: começa cobrindo a arena e fecha em etapas
		startRadius = 170,
		endRadius = 12,
		phases = 5,
		holdTime = 30,
		shrinkTime = 25,
		--- dano por segundo fora da zona, sobe a cada fase
		dpsPerPhase = { 2, 4, 7, 12, 20 },
	},
}

Balance.controls = {
	--- assistência do esquema Simples (GDD §19.3): a mira gruda no inimigo mais
	--- próximo dentro de um cone na direção do arrasto
	aimAssist = {
		coneDeg = 30,
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
