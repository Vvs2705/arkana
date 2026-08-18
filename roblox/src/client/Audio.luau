--!strict
-- ============================================================================
-- RAIA D (ÁUDIO) — o jogo tinha SILÊNCIO ABSOLUTO até aqui.
--
-- APRESENTAÇÃO PURA, ZERO AUTORIDADE: nada neste arquivo decide jogo. Ele só
-- escuta os remotes que o servidor JÁ emite (Net.Names.*) e toca. Não cria
-- remote, não fala com o servidor, não lê estado de combate.
--
-- ------------------------------------------------------------------ ASSETS
-- PROIBIDO `rbxassetid://` (id de upload) — id inventado = som que não existe.
-- Tudo aqui é `rbxasset://`, os arquivos que VÊM DENTRO do cliente Roblox.
-- A lista abaixo foi CONFERIDA no cliente instalado desta máquina em 18/08/2026
-- (`%LOCALAPPDATA%\Roblox\Versions\version-*\content\sounds\`), nas 3 versões
-- presentes — 2 do Player e 1 do Studio, todas com o MESMO conteúdo. São
-- ONZE arquivos, e é com eles que o jogo inteiro soa:
--
--   action_falling.ogg            10,00s   rajada de ar sustentada (loopável)
--   action_footsteps_plastic.mp3   2,49s   passos — NÃO usado (o Roblox já dá)
--   action_get_up.mp3              0,58s   raspagem/atrito
--   action_jump.mp3                0,35s   whoosh curto
--   action_jump_land.mp3           0,27s   baque seco
--   action_swim.mp3                4,89s   braçada n'água (loopável)
--   impact_explosion_03.mp3        2,59s   explosão
--   impact_water.mp3               2,39s   splash
--   oof.ogg                        0,33s   dor, curtíssimo
--   ouch.ogg                       0,45s   dor
--   volume_slider.ogg              0,85s   tique de UI
--
-- Não existe biblioteca embutida além destas onze. Então a IDENTIDADE não vem
-- de ter o som certo — vem de SINTETIZAR a sensação: `PlaybackSpeed` (o mesmo
-- whoosh a 0,62 é rugido e a 1,7 é assobio), `Volume`, camadas curtas e EQ por
-- `SoundGroup`. Todo efeito usado aqui (Equalizer/PitchShift/Distortion) é
-- nativo e NÃO precisa de upload.
--
-- CONFERIR EM 1 LINHA no Studio (Command Bar do CLIENTE) antes do playtest —
-- é o único jeito de provar que o `rbxasset://` resolve neste build:
--   require(game.Players.LocalPlayer.PlayerScripts.Client.Audio).probe()
-- Toda linha tem que sair com `TimeLength > 0`.
--
-- --------------------------------------------------------------- MIXAGEM
-- GDD §13 + realidade de celular: alto-falante de telefone não reproduz grave.
-- Cada grupo corta o baixo e reforça o médio — é onde a informação mora e é a
-- faixa que o aparelho realmente toca. Três grupos (Mundo / UI / Música), como
-- pede o GDD, para volume independente; um quarto (Fusão) existe só para dar
-- timbre próprio ao momento mais importante da partida.
--
-- ACESSIBILIDADE (GDD §10): cor + forma + SOM. Os 5 elementos usam FONTES
-- DIFERENTES (não só pitch diferente) — rugido · borbulha · baque · assobio ·
-- estalo. O `selfTest` trava isso: dois elementos com o mesmo par
-- (arquivo, velocidade) reprovam o teste.
--
-- ORÇAMENTO (é celular): teto DURO de vozes. Estourou, o som novo é
-- DESCARTADO — nunca acumula, nunca faz fila. Igual ao teto de projéteis do
-- Vfx, e pelo mesmo motivo.
-- ============================================================================

local Debris = game:GetService("Debris")
local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local SoundService = game:GetService("SoundService")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Balance = require(Shared:WaitForChild("Balance"))
local Elements = require(Shared:WaitForChild("Elements"))
local Net = require(Shared:WaitForChild("Net"))

local Audio = {}

-- ------------------------------------------------------------------ paleta
--- As ÚNICAS strings de asset do arquivo. O `selfTest` confere que nenhum
--- `SoundId` do módulo saiu daqui — é a trava contra um `rbxassetid://`
--- inventado entrar de carona numa edição futura.
local SFX = {
	WIND = "rbxasset://sounds/action_falling.ogg",
	SCRAPE = "rbxasset://sounds/action_get_up.mp3",
	WHOOSH = "rbxasset://sounds/action_jump.mp3",
	THUD = "rbxasset://sounds/action_jump_land.mp3",
	SWIM = "rbxasset://sounds/action_swim.mp3",
	BOOM = "rbxasset://sounds/impact_explosion_03.mp3",
	SPLASH = "rbxasset://sounds/impact_water.mp3",
	OOF = "rbxasset://sounds/oof.ogg",
	OUCH = "rbxasset://sounds/ouch.ogg",
	TICK = "rbxasset://sounds/volume_slider.ogg",
}

-- ------------------------------------------------------------------ orçamento
local CAP = {
	oneShot = 14, -- vozes curtas simultâneas
	loops = 6, -- camas posicionais (fogo/gelo/eletricidade) simultâneas
}
--- alcance padrão de som de mundo. Curto de propósito: o combate é próximo e
--- 40 fontes audíveis do outro lado do mapa viram ruído branco.
local ROLL_MIN, ROLL_MAX = 8, 130

-- ------------------------------------------------------------------ vozes
type Voice = {
	id: string,
	speed: number,
	vol: number,
	life: number, -- s até o som ser destruído (destruir = parar)
	jitter: number?, -- variação aleatória de PlaybackSpeed (±)
}

--- CONJURAÇÃO — cada elemento com FONTE própria, não só pitch próprio. É esta
--- tabela que cumpre "reconhecível de ouvido" do GDD §10.
--- Camada 2 (`layer`) é opcional e curta: dá textura sem dobrar o custo.
local CAST: { [string]: { Voice } } = {
	-- rugido grave + chiado: fogo
	Fire = {
		{ id = SFX.WHOOSH, speed = 0.62, vol = 0.55, life = 0.6, jitter = 0.05 },
		{ id = SFX.WIND, speed = 1.9, vol = 0.22, life = 0.35 },
	},
	-- borbulha: água
	Water = {
		{ id = SFX.SPLASH, speed = 1.75, vol = 0.42, life = 0.4, jitter = 0.06 },
	},
	-- baque + raspagem de pedra: terra
	Earth = {
		{ id = SFX.THUD, speed = 0.7, vol = 0.6, life = 0.45, jitter = 0.04 },
		{ id = SFX.SCRAPE, speed = 0.85, vol = 0.28, life = 0.5 },
	},
	-- assobio rápido e agudo: vento
	Wind = {
		{ id = SFX.WHOOSH, speed = 1.7, vol = 0.45, life = 0.3, jitter = 0.08 },
	},
	-- estalo seco: raio
	Lightning = {
		{ id = SFX.TICK, speed = 2.4, vol = 0.5, life = 0.25, jitter = 0.1 },
		{ id = SFX.WHOOSH, speed = 2.1, vol = 0.22, life = 0.2 },
	},
}

--- Tinta de pitch do IMPACTO por elemento: a explosão é a mesma, o timbre não.
local IMPACT_TINT: { [string]: number } = {
	Fire = 0.9,
	Water = 1.05,
	Earth = 0.8,
	Wind = 1.25,
	Lightning = 1.35,
}

--- Fraco × forte: o básico é um estalo curto e agudo, a tática é um estouro
--- grave e longo. É a diferença que o jogador precisa ouvir SEM olhar.
local IMPACT_WEAK: Voice = { id = SFX.BOOM, speed = 1.9, vol = 0.3, life = 0.45, jitter = 0.07 }
local IMPACT_STRONG: Voice = { id = SFX.BOOM, speed = 0.95, vol = 0.6, life = 1.6, jitter = 0.05 }

--- CAMAS DE TERRENO (posicionais, em loop, com teto). O cliente não conhece a
--- grade do terreno — nenhum remote a replica — então a cama nasce do EVENTO
--- que a produz no servidor, com a MESMA duração do `Balance.terrain`:
--- ponytail: TETO CONHECIDO — se o Fogo cair em pedra, o servidor não acende
--- nada e a gente crepita mesmo assim (e a Água só congela se pegar lago). É
--- o preço de não inventar remote; o upgrade, se incomodar no playtest, é o
--- servidor mandar `burned=true` dentro do SpellImpact que já existe.
local T = Balance.terrain
local BED_FIRE: Voice = { id = SFX.WIND, speed = 2.6, vol = 0.17, life = T.burnDuration }
local BED_SHOCK: Voice = { id = SFX.TICK, speed = 2.9, vol = 0.16, life = T.electrifyDuration }
local BED_FROST: Voice = { id = SFX.SWIM, speed = 0.55, vol = 0.14, life = math.min(3.5, T.freezeDuration) }

-- ------------------------------------------------------------------ estado
local localPlayer: Player? = nil
local folder: Folder? = nil
local conns: { RBXScriptConnection } = {}
local groups: { [string]: SoundGroup } = {}
local oneShots, loopsAlive = 0, 0

--- MIX DE FÁBRICA por categoria (0..1) — o "cheio" de cada uma. É a mixagem do
--- GDD §13, não a preferência do jogador: quem GUARDA a escolha é o
--- `Accessibility` (loja única, atributo `Arkana_volMaster`/`Arkana_volWorld`).
--- Aqui embaixo `volume` é só o ganho APLICADO agora — estado do mixer, nunca
--- persistido. Sem Accessibility (módulo fora do build, erro no require) o som
--- toca no mix de fábrica: o áudio nunca depende de outra raia para existir.
local VOL_DEFAULT: { [string]: number } = { Master = 1, World = 0.9, Ui = 0.7, Music = 0.6 }
local volume: { [string]: number } = table.clone(VOL_DEFAULT)

--- riser da canalização da Sintonia (GDD §9: o telegrama tem de ser SONORO)
local channel: { sound: Sound, t: number, dur: number }? = nil
--- estado de partida observado (só para detectar TRANSIÇÃO, nunca para decidir)
local zone = { center = Vector3.zero, radius = math.huge, shrinking = false, state = "" }
local outsideBeep = 0
local lastHp, hurtCooldown = -1, 0

-- ------------------------------------------------------- modo leve (celular)
--- Mesmo padrão do Vfx: o knob de qualidade mora no Atmosphere, require
--- preguiçoso e protegido. Sem o módulo, o palpite é "não é leve".
local atmos: any = nil
local function lite(): boolean
	if atmos == nil then
		local mod = script.Parent and script.Parent:FindFirstChild("Atmosphere")
		local ok, m = pcall(function()
			return require(mod :: ModuleScript)
		end)
		atmos = (ok and m) or false
	end
	if atmos and atmos.isLite then
		local ok, v = pcall(atmos.isLite)
		if ok then
			return v == true
		end
	end
	return false
end

--- Preferência de acessibilidade, se o módulo existir. Só LEITURA — a loja de
--- preferências é de outra raia.
local a11y: any = nil
local function readyCue(): boolean
	if a11y == nil then
		local mod = script.Parent and script.Parent:FindFirstChild("Accessibility")
		local ok, m = pcall(function()
			return require(mod :: ModuleScript)
		end)
		a11y = (ok and m) or false
	end
	if a11y and a11y.readyCue then
		local ok, v = pcall(a11y.readyCue)
		if ok then
			return v ~= false
		end
	end
	return true
end

-- ------------------------------------------------------------------ grupos
local function bin(): Folder
	local f = folder
	if f and f.Parent then
		return f
	end
	f = Instance.new("Folder")
	f.Name = "ArkanaAudio" -- criado no cliente: existe só aqui, não replica
	f.Parent = workspace
	folder = f
	return f
end

--- EQ de celular: corta o grave que o alto-falante não faz e abre o médio.
local function eq(parent: Instance, low: number, mid: number, high: number)
	local e = Instance.new("EqualizerSoundEffect")
	e.LowGain = low
	e.MidGain = mid
	e.HighGain = high
	e.Parent = parent
end

local function ensureGroups()
	if groups.World and groups.World.Parent then
		return
	end
	for _, name in ipairs({ "World", "Ui", "Music", "Fusion" }) do
		local g = Instance.new("SoundGroup")
		g.Name = "Arkana" .. name
		g.Volume = 1
		g.Parent = SoundService
		groups[name] = g
	end
	eq(groups.World, -12, 2, -3)
	eq(groups.Ui, -20, 3, 0) -- UI é 100% médio: tem de passar em qualquer aparelho
	eq(groups.Music, -6, 0, -2)

	-- FUSÃO: o timbre que nenhum outro som do jogo tem. Grave MENOS cortado
	-- (é para parecer maior), oitava puxada para baixo e uma pitada de
	-- distorção — sem asset novo, só DSP nativo.
	eq(groups.Fusion, -8, 4, -2)
	local shift = Instance.new("PitchShiftSoundEffect")
	shift.Octave = 0.72
	shift.Parent = groups.Fusion
	local dist = Instance.new("DistortionSoundEffect")
	dist.Level = 0.22
	dist.Parent = groups.Fusion

	Audio.applyVolumes()
end

function Audio.applyVolumes()
	local master = volume.Master
	for name, g in pairs(groups) do
		local cat = if name == "Fusion" then "World" else name
		g.Volume = master * (volume[cat] or 1)
	end
end

-- ------------------------------------------------------------------ tocar
--- ponytail: o harness headless (roblox/tools/harness.luau) não implementa
--- `Sound:Play()`. Um nil-check é mais barato que `pcall` por voz e mantém o
--- selfTest rodando fora do Studio sem eu mexer em arquivo de outra raia.
local function start(s: Sound)
	local f = (s :: any).Play
	if f then
		f(s)
	end
end

local function build(v: Voice, group: SoundGroup, loop: boolean): Sound
	local s = Instance.new("Sound")
	s.SoundId = v.id
	s.Volume = v.vol
	local j = v.jitter or 0
	s.PlaybackSpeed = v.speed + (if j > 0 then (math.random() * 2 - 1) * j else 0)
	s.Looped = loop
	s.SoundGroup = group
	return s
end

--- Som 2D (UI, dor, vitória): fala no ouvido do jogador, não sai do mundo.
local function play2d(v: Voice, category: string?): Sound?
	ensureGroups()
	local cap = if lite() then CAP.oneShot // 2 else CAP.oneShot
	if oneShots >= cap then
		return nil -- teto: DESCARTA a voz nova. Nunca enfileira.
	end
	oneShots += 1
	local s = build(v, groups[category or "Ui"] or groups.Ui, false)
	s.Parent = bin()
	start(s)
	Debris:AddItem(s, v.life)
	task.delay(v.life, function()
		oneShots -= 1
	end)
	return s
end

--- Som de MUNDO: sai da posição do evento. O `Sound` mora num part âncora
--- invisível — mesmo truque (e mesmas flags de decoração) do Vfx.
local function play3d(pos: Vector3, v: Voice, group: SoundGroup?, maxDist: number?): Sound?
	ensureGroups()
	local cap = if lite() then CAP.oneShot // 2 else CAP.oneShot
	if oneShots >= cap then
		return nil
	end
	oneShots += 1

	local anchor = Instance.new("Part")
	anchor.Size = Vector3.new(0.2, 0.2, 0.2)
	anchor.Transparency = 1
	anchor.Anchored = true
	anchor.CanCollide = false
	anchor.CanQuery = false
	anchor.CanTouch = false
	anchor.CastShadow = false
	anchor.Position = pos
	anchor.Parent = bin()

	local s = build(v, group or groups.World, false)
	s.RollOffMode = Enum.RollOffMode.InverseTapered
	s.RollOffMinDistance = ROLL_MIN
	s.RollOffMaxDistance = maxDist or ROLL_MAX
	s.Parent = anchor
	start(s)
	Debris:AddItem(anchor, v.life)
	task.delay(v.life, function()
		oneShots -= 1
	end)
	return s
end

--- Cama posicional em LOOP (fogo crepitando, gelo, eletricidade). Teto próprio
--- e mais apertado: cama é som contínuo, e contínuo é o que satura o mixer.
local function playBed(pos: Vector3, v: Voice)
	ensureGroups()
	local cap = if lite() then CAP.loops // 2 else CAP.loops
	if loopsAlive >= cap then
		return
	end
	loopsAlive += 1

	local anchor = Instance.new("Part")
	anchor.Size = Vector3.new(0.2, 0.2, 0.2)
	anchor.Transparency = 1
	anchor.Anchored = true
	anchor.CanCollide = false
	anchor.CanQuery = false
	anchor.CanTouch = false
	anchor.CastShadow = false
	anchor.Position = pos
	anchor.Parent = bin()

	local s = build(v, groups.World, true)
	s.RollOffMode = Enum.RollOffMode.InverseTapered
	s.RollOffMinDistance = 4
	s.RollOffMaxDistance = 70 -- cama é ambiente: só quem está perto ouve
	s.Parent = anchor
	start(s)
	Debris:AddItem(anchor, v.life)
	task.delay(v.life, function()
		loopsAlive -= 1
	end)
end

-- ------------------------------------------------------------------ eventos
local function onCast(payload: any)
	if typeof(payload) ~= "table" or typeof(payload.origin) ~= "Vector3" then
		return
	end
	local layers = CAST[tostring(payload.element)] or CAST.Fire
	for _, v in ipairs(layers) do
		play3d(payload.origin, v)
	end
end

local function onImpact(payload: any)
	if typeof(payload) ~= "table" or typeof(payload.pos) ~= "Vector3" then
		return
	end
	local pos = payload.pos
	local el = tostring(payload.element)
	local strong = payload.strong == true
	local base = if strong then IMPACT_STRONG else IMPACT_WEAK
	play3d(pos, {
		id = base.id,
		speed = base.speed * (IMPACT_TINT[el] or 1),
		vol = base.vol,
		life = base.life,
		jitter = base.jitter,
	})

	-- consequência no terreno (§14): o que o elemento DEIXA no chão
	if el == "Fire" then
		playBed(pos, BED_FIRE)
	elseif el == "Lightning" then
		playBed(pos, BED_SHOCK)
	elseif el == "Water" and strong then
		playBed(pos, BED_FROST) -- só a tática de Água congela lago
	end
end

--- A CANALIZAÇÃO (GDD §9): "visível E SONORA". Riser posicional que sobe de
--- tom durante a janela — quem está perto ouve a fusão CHEGANDO e tem tempo de
--- interromper. Alcance grande de propósito: é um telegrama, não um sussurro.
local function stopChannel()
	local c = channel
	channel = nil
	if c and c.sound.Parent then
		local anchor = c.sound.Parent
		if anchor then
			anchor:Destroy()
		end
		loopsAlive = math.max(0, loopsAlive - 1)
	end
end

local function startChannel(pos: Vector3, dur: number)
	stopChannel()
	ensureGroups()
	local anchor = Instance.new("Part")
	anchor.Size = Vector3.new(0.2, 0.2, 0.2)
	anchor.Transparency = 1
	anchor.Anchored = true
	anchor.CanCollide = false
	anchor.CanQuery = false
	anchor.CanTouch = false
	anchor.CastShadow = false
	anchor.Position = pos
	anchor.Parent = bin()

	local s = build({ id = SFX.WIND, speed = 0.9, vol = 0.45, life = dur }, groups.World, true)
	s.RollOffMode = Enum.RollOffMode.InverseTapered
	s.RollOffMinDistance = 20
	s.RollOffMaxDistance = 320
	s.Parent = anchor
	start(s)
	loopsAlive += 1
	channel = { sound = s, t = 0, dur = math.max(0.2, dur) }
	-- rede de segurança: se o "fired"/"failed" se perder, o riser não fica eterno
	Debris:AddItem(anchor, dur + 1.5)
end

local function onSintonia(payload: any)
	if typeof(payload) ~= "table" then
		return
	end
	local phase = tostring(payload.phase)
	local pos = if typeof(payload.pos) == "Vector3" then payload.pos else nil

	if phase == "channeling" and pos then
		startChannel(pos, tonumber(payload.duration) or 1)
	elseif phase == "fired" and pos then
		stopChannel()
		-- A FUSÃO. Grupo próprio (oitava abaixo + distorção) e alcance de mapa:
		-- o GDD §9 quer que o outro lado da arena saiba que aconteceu.
		play3d(pos, { id = SFX.BOOM, speed = 0.62, vol = 0.9, life = 3.0 }, groups.Fusion, 480)
		play3d(pos, { id = SFX.WIND, speed = 0.5, vol = 0.5, life = 2.2 }, groups.Fusion, 480)
	elseif phase == "failed" then
		stopChannel()
		play2d({ id = SFX.THUD, speed = 0.5, vol = 0.4, life = 0.6 }) -- baque de ritual quebrado
	elseif (phase == "proposed" or phase == "window") and readyCue() then
		play2d({ id = SFX.TICK, speed = 1.9, vol = 0.4, life = 0.35 })
	end
end

--- Dano RECEBIDO: o servidor só manda `StatsUpdate` para o dono, então a queda
--- de hp+escudo é o sinal pessoal confiável (o `DamageNumber` é de todo mundo).
local function onStats(payload: any)
	if typeof(payload) ~= "table" then
		return
	end
	local hp = (tonumber(payload.hp) or 0) + (tonumber(payload.shield) or 0)
	local was = lastHp
	lastHp = hp
	if was < 0 or hp >= was or hurtCooldown > 0 then
		return
	end
	hurtCooldown = 0.25 -- sem isto o dano contínuo do terreno vira metralhadora
	local hurt = math.clamp((was - hp) / 30, 0.25, 1)
	play2d({ id = SFX.OUCH, speed = 1.05, vol = 0.3 + 0.35 * hurt, life = 0.5, jitter = 0.08 })
end

local function onAnnounce(payload: any)
	if typeof(payload) ~= "table" then
		return
	end
	local key = tostring(payload.key)
	if key == "announce.death" then
		play2d({ id = SFX.OOF, speed = 0.75, vol = 0.8, life = 0.5 })
		play2d({ id = SFX.WIND, speed = 0.45, vol = 0.4, life = 1.6 }) -- a queda
	elseif key == "announce.kill" then
		play2d({ id = SFX.TICK, speed = 1.8, vol = 0.45, life = 0.3 })
	elseif key == "announce.nomana" or key == "announce.cooldown" then
		play2d({ id = SFX.THUD, speed = 0.55, vol = 0.22, life = 0.3 }) -- "não saiu"
	elseif key == "announce.zone.closing" then
		play2d({ id = SFX.WIND, speed = 0.55, vol = 0.35, life = 2.5 })
	elseif key == "announce.revive.done" then
		play2d({ id = SFX.TICK, speed = 1.4, vol = 0.5, life = 0.4 })
	end
end

--- Jingle de 3 notas a partir de UM tique de 0,85s. Subindo = vitória, descendo
--- = fim sem título. É todo o "tema" que onze arquivos permitem — e resolve.
local function jingle(steps: { number }, vol: number)
	for i, speed in ipairs(steps) do
		task.delay((i - 1) * 0.16, function()
			play2d({ id = SFX.TICK, speed = speed, vol = vol, life = 0.45 }, "Music")
		end)
	end
end

local function onMatch(payload: any)
	if typeof(payload) ~= "table" then
		return
	end
	if typeof(payload.zoneCenter) == "Vector3" then
		zone.center = payload.zoneCenter
	end
	zone.radius = tonumber(payload.zoneRadius) or zone.radius

	local shrinking = payload.shrinking == true
	if shrinking and not zone.shrinking then
		play2d({ id = SFX.WIND, speed = 0.6, vol = 0.3, life = 2.2 })
	end
	zone.shrinking = shrinking

	local state = tostring(payload.state)
	if state == zone.state then
		return
	end
	zone.state = state
	if state == "Playing" then
		play2d({ id = SFX.BOOM, speed = 1.4, vol = 0.4, life = 1.2 }, "Music")
	elseif state == "Ended" then
		local seal = payload.seal
		local me = localPlayer
		local won = typeof(seal) == "table" and me ~= nil and tostring(seal.champion) == me.DisplayName
		if won then
			jingle({ 1.0, 1.26, 1.5 }, 0.6)
		else
			jingle({ 1.0, 0.84 }, 0.4)
		end
	end
end

-- ------------------------------------------------------------------ UI
--- Clique de UI em TODA a interface do jogo sem tocar em arquivo de outra raia:
--- um listener na PlayerGui pega qualquer `GuiButton` que já exista ou que
--- apareça depois — Hud, MatchUi, Settings, Grimório, Tutorial, o que vier.
local function hookButton(d: Instance)
	if d:IsA("GuiButton") then
		table.insert(
			conns,
			d.Activated:Connect(function()
				play2d({ id = SFX.TICK, speed = 1.5, vol = 0.35, life = 0.14 })
			end)
		)
	end
end

-- ------------------------------------------------------------------ volume
--- APLICA o volume — não o guarda. Quem chama é o `Accessibility` (dono da
--- preferência do jogador), a cada mudança e no init. Categoria desconhecida é
--- ignorada em silêncio. Categorias: Master · World · Ui · Music.
function Audio.setVolume(category: string, value: number)
	if VOL_DEFAULT[category] == nil then
		return
	end
	volume[category] = math.clamp(tonumber(value) or 1, 0, 1)
	Audio.applyVolumes()
end

function Audio.getVolume(category: string): number
	return volume[category] or VOL_DEFAULT[category] or 1
end

--- O "cheio" de fábrica da categoria. O `Accessibility` lê daqui em vez de
--- carregar um segundo número que pode divergir do mixer.
function Audio.defaultVolume(category: string): number
	return VOL_DEFAULT[category] or 1
end

function Audio.categories(): { string }
	return { "Master", "World", "Ui", "Music" }
end

-- ------------------------------------------------------------------ API
function Audio.init(player: Player)
	if localPlayer then
		return
	end
	localPlayer = player
	ensureGroups()

	-- sem leitura de atributo aqui: o `Accessibility.init` (que roda antes, ver
	-- Main.client) já empurrou a preferência salva para dentro do `volume`. Ler
	-- de novo seria a segunda loja voltando pela porta dos fundos.
	Audio.applyVolumes()

	table.insert(conns, Net.event(Net.Names.SpellSpawned).OnClientEvent:Connect(onCast))
	table.insert(conns, Net.event(Net.Names.SpellImpact).OnClientEvent:Connect(onImpact))
	table.insert(conns, Net.event(Net.Names.SintoniaState).OnClientEvent:Connect(onSintonia))
	table.insert(conns, Net.event(Net.Names.StatsUpdate).OnClientEvent:Connect(onStats))
	table.insert(conns, Net.event(Net.Names.MatchState).OnClientEvent:Connect(onMatch))
	table.insert(conns, Net.event(Net.Names.Announce).OnClientEvent:Connect(onAnnounce))

	local pg = player:FindFirstChildOfClass("PlayerGui")
	if pg then
		for _, d in ipairs(pg:GetDescendants()) do
			hookButton(d)
		end
		table.insert(conns, pg.DescendantAdded:Connect(hookButton))
	end
end

function Audio.update(dt: number)
	if hurtCooldown > 0 then
		hurtCooldown -= dt
	end

	-- riser da Sintonia: o tom sobe até a fusão. É o que transforma "tem um
	-- efeito ali" em "vai estourar AGORA" (janela de interrupção do GDD §9).
	local c = channel
	if c then
		c.t += dt
		local k = math.clamp(c.t / c.dur, 0, 1)
		c.sound.PlaybackSpeed = 0.9 + 1.1 * k
		c.sound.Volume = 0.45 + 0.35 * k
		if c.t > c.dur + 1.2 then
			stopChannel()
		end
	end

	-- FORA DA ZONA: bipe curto que repete enquanto durar. Mesma conta da
	-- MatchUi (distância no plano), feita aqui para não depender da raia dela.
	local p = localPlayer
	local char = p and p.Character
	local root = char and char:FindFirstChild("HumanoidRootPart") :: BasePart?
	if root and zone.radius < math.huge then
		local d = root.Position - zone.center
		if math.sqrt(d.X * d.X + d.Z * d.Z) > zone.radius then
			outsideBeep -= dt
			if outsideBeep <= 0 then
				outsideBeep = 1.1
				play2d({ id = SFX.TICK, speed = 0.7, vol = 0.3, life = 0.35 })
			end
			return
		end
	end
	outsideBeep = 0
end

function Audio.destroy()
	for _, c in ipairs(conns) do
		c:Disconnect()
	end
	table.clear(conns)
	channel = nil
	for _, g in pairs(groups) do
		g:Destroy()
	end
	table.clear(groups)
	if folder then
		folder:Destroy()
		folder = nil
	end
	oneShots, loopsAlive = 0, 0
	lastHp, hurtCooldown, outsideBeep = -1, 0, 0
	zone.state, zone.shrinking, zone.radius = "", false, math.huge
	localPlayer = nil
end

-- ============================================================================
-- PROVA DE ASSET — rode NO STUDIO (Command Bar do cliente) antes do playtest:
--   require(game.Players.LocalPlayer.PlayerScripts.Client.Audio).probe()
-- É a única verificação que o harness headless NÃO consegue fazer: se um
-- `rbxasset://` não resolver neste build do cliente, sai TimeLength 0 aqui em
-- vez de silêncio inexplicável no meio da partida.
-- ============================================================================
function Audio.probe()
	for name, id in pairs(SFX) do
		local s = Instance.new("Sound")
		s.SoundId = id
		s.Parent = SoundService
		task.wait(0.2)
		print(string.format("[Audio] %-8s %-42s TimeLength=%.2f", name, id, s.TimeLength))
		s:Destroy()
	end
end

-- ============================================================================
-- AUTOTESTE — roda no harness (roblox/tools/run.sh) e no Studio:
--   require(game.Players.LocalPlayer.PlayerScripts.Client.Audio).selfTest()
-- Cobre as três coisas que quebram CALADAS: asset inventado, elemento que
-- perdeu a identidade sonora, e teto de vozes que deixou de ser teto.
-- ============================================================================
function Audio.selfTest()
	-- 1) NENHUM asset fora da paleta conferida. Um `rbxassetid://` de upload
	-- (ou um caminho embutido que não existe) reprova aqui, não no console do
	-- jogador. Esta é a regra dura da missão.
	local allowed: { [string]: boolean } = {}
	for _, id in pairs(SFX) do
		assert(string.sub(id, 1, 17) == "rbxasset://sounds", "asset fora de rbxasset://sounds: " .. id)
		allowed[id] = true
	end
	local function checkVoice(v: Voice, where: string)
		assert(allowed[v.id], where .. " usa asset desconhecido: " .. tostring(v.id))
		assert(v.vol > 0 and v.vol <= 1, where .. " com volume fora de 0..1")
		assert(v.speed > 0 and v.life > 0, where .. " com speed/life inválido")
	end
	for id, layers in pairs(CAST) do
		for i, v in ipairs(layers) do
			checkVoice(v, "CAST." .. id .. "[" .. i .. "]")
		end
	end
	for _, entry in ipairs({ IMPACT_WEAK, IMPACT_STRONG, BED_FIRE, BED_SHOCK, BED_FROST }) do
		checkVoice(entry, "voz fixa")
	end

	-- 2) IDENTIDADE POR ELEMENTO (GDD §10: cor + forma + SOM). Os 5 elementos
	-- existem, e nenhum par (arquivo, velocidade) se repete entre eles — se
	-- alguém colar duas linhas iguais, Fogo e Terra passam a soar idênticos e
	-- o pilar de acessibilidade cai sem ninguém perceber.
	local seen: { [string]: string } = {}
	for _, elId in ipairs(Elements.Order) do
		local layers = CAST[elId]
		assert(layers and #layers > 0, "elemento sem voz de conjuração: " .. elId)
		local sig = string.format("%s@%.2f", layers[1].id, layers[1].speed)
		assert(not seen[sig], "elementos indistinguíveis de ouvido: " .. elId .. " e " .. tostring(seen[sig]))
		seen[sig] = elId
		assert(IMPACT_TINT[elId] ~= nil, "elemento sem tinta de impacto: " .. elId)
	end

	-- fraco ≠ forte: o impacto tem de ser reconhecível SEM olhar a tela
	assert(IMPACT_STRONG.speed < IMPACT_WEAK.speed, "tática tem de soar mais GRAVE que o básico")
	assert(IMPACT_STRONG.life > IMPACT_WEAK.life, "tática tem de soar mais LONGA que o básico")

	-- 3) TETO DE VOZES: estourar tem de DESCARTAR, nunca acumular. É a conta
	-- que protege o celular; se ela vazar, a partida vira ruído e o fps some.
	local wasOne, wasLoop = oneShots, loopsAlive
	oneShots, loopsAlive = 0, 0
	local pos = Vector3.new(0, 0, 0)
	local created = 0
	for _ = 1, CAP.oneShot + 8 do
		if play3d(pos, IMPACT_WEAK) then
			created += 1
		end
	end
	assert(oneShots <= CAP.oneShot, "teto de vozes vazou: " .. oneShots)
	assert(created <= CAP.oneShot, "criou mais vozes que o teto: " .. created)

	for _ = 1, CAP.loops + 5 do
		playBed(pos, BED_FIRE)
	end
	assert(loopsAlive <= CAP.loops, "teto de camas vazou: " .. loopsAlive)

	if folder then
		folder:Destroy()
		folder = nil
	end
	oneShots, loopsAlive = wasOne, wasLoop

	-- 4) volume: categoria desconhecida não corrompe o mixer
	Audio.setVolume("NaoExiste", 0.5)
	assert(Audio.getVolume("NaoExiste") == 1, "categoria inventada entrou na loja")
	Audio.setVolume("World", 2)
	assert(Audio.getVolume("World") == 1, "volume não foi limitado a 0..1")

	-- 5) UMA LOJA SÓ (R9): quem PERSISTE volume é o `Accessibility`. Se este
	-- módulo voltar a escrever o atributo, passam a existir dois donos da mesma
	-- chave e a preferência do jogador vira corrida entre eles — reprova aqui.
	local p = localPlayer or Players.LocalPlayer
	if p then
		local sentinel = -7 -- valor que NENHUM caminho legítimo escreveria
		p:SetAttribute("Arkana_volWorld", sentinel)
		Audio.setVolume("World", 0.3)
		assert(p:GetAttribute("Arkana_volWorld") == sentinel, "o Audio voltou a persistir volume")
		p:SetAttribute("Arkana_volWorld", nil)
	end
	Audio.setVolume("World", VOL_DEFAULT.World)

	print("[Audio] autoteste OK — 10 assets embutidos · 5 elementos distintos · teto de vozes · volume sem dono duplo")
	return true
end

return Audio
