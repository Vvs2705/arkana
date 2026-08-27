--!strict
-- ============================================================================
-- RAIA R2 — SINTONIA / CONJURAÇÃO COMBINADA (GDD §9): O PILAR DE INOVAÇÃO.
-- É a razão mecânica de jogar em dupla e a pergunta de validação nº 1 do
-- produto (docs/ROBLOX.md §1, V1). Autoridade 100% servidor.
--
-- COMO FUNCIONA (o fluxo inteiro em 5 linhas):
--   1. todo projétil que impacta chama `registerCast` (Combat, raia B);
--   2. duas conjurações de DOIS ALIADOS (mesmo time — GDD §9: "dois magos do
--      SQUAD"), de ELEMENTOS DIFERENTES, dentro de `window` (1,5s) e a menos de
--      `targetRadius` uma da outra →
--   3. começa a CANALIZAÇÃO (`channel` ≈ 1s), anunciada a TODOS (inclusive
--      inimigos: o telegrama é o que permite interromper);
--   4. se um dos dois morrer/sair no meio → "failed" (e o custo NÃO volta);
--   5. senão → "fired": dano em área ×`dmgMult` + reação de terreno coerente
--      com a fantasia do combo.
--
-- OS LIMITADORES DO GDD §9 SÃO LEI — sem eles o jogo quebra:
--   · mesmo elemento NÃO funde (Elements.getCombo já devolve nil);
--   · canalização visível/interrompível ANTES da fusão;
--   · CUSTO COBRADO NO INÍCIO: o `sharedCooldown` (24s) entra nos DOIS quando
--     a canalização começa, não quando o combo sai. Combo errado (ou
--     interrompido) deixa a dupla seca e vulnerável — é isso que impede spam;
--   · todo combo INTERAGE COM O TERRENO (casamento dos dois pilares do jogo).
--
-- FRONTEIRA DE RESPONSABILIDADE (igual ao resto do servidor): quem aplica dano
-- é o Combat (`applyDamage`) e quem muda o mundo é o Terrain (`applyElement`).
-- Este módulo decide QUANDO, ONDE e QUANTO — e nunca toca em part de arena.
-- ============================================================================

local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local Workspace = game:GetService("Workspace")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Balance = require(Shared:WaitForChild("Balance"))
local Elements = require(Shared:WaitForChild("Elements"))
local Events = require(Shared:WaitForChild("Events"))
local Grid = require(Shared:WaitForChild("Grid"))
local Net = require(Shared:WaitForChild("Net"))

--- Times (P0): a Sintonia é do SQUAD (GDD §9). Require direto — o Teams não
--- depende de ninguém, então não há ciclo, e sem ele não existe "aliado".
local Teams = require((script.Parent :: Instance):WaitForChild("Teams") :: ModuleScript)

--- todos os números do combo saem daqui (GDD: "números viram dados")
local S = Balance.sintonia

-- --- constantes sociais / anti-abuso (NÃO são balanceamento de combate) -----
--- alcance máximo do alvo pingado: o mesmo do projétil (720 px do 2D ÷ 8)
local PING_RANGE = 720 / Balance.PX_PER_STUD
--- quem "ouve" o ping: aliado tem de estar plausivelmente na mesma briga
local PING_HEAR = 120
--- anti-spam de ping (o ping é social, não é magia — cooldown curto)
local PING_COOLDOWN = 2.0
--- proposta sem resposta morre sozinha; aceita, o alvo marcado dura mais
local PROPOSAL_TTL = 6.0
local PACT_TTL = 12.0
--- resposta do parceiro BOT ao ping (débito 2): um instante de "reação" para o
--- aceite não sair no mesmo frame do ping. Game feel, não balanceamento.
local BOT_ACCEPT_DELAY = 0.5
--- altura em que o combo acontece (peito): o impacto pode ter sido na copa de
--- uma árvore, e a explosão tem de pegar quem está no chão
local COMBO_Y = Grid.FLOOR_Y + 3
--- componente vertical do empurrão (studs/s) — game feel, não balanceamento
local PUSH_UP = 18

-- ============================================================================
-- RECEITA DE TERRENO POR COMBO — a metade "mundo" de cada uma das 10 fusões.
-- A ORDEM importa e as ausências são deliberadas (a química do Terrain é
-- sequencial: Água CONGELA o lago e o gelo isola, Vento APAGA chama solitária):
--   · Eletrocussão NÃO aplica Água — congelaria a poça e mataria a condução;
--   · Tornado Flamejante NÃO aplica Vento — apagaria a chama recém-nascida;
--   · Cortina de Vapor aplica só Água — vapor é fogo APAGADO, não fogo somado.
-- `strong` = usa a variante tática da reação (Terra ergue muro em ÁREA).
-- ============================================================================
--
-- STATUS DO COMBO (P1 da auditoria): "atordoa" e "revela" agora são MECÂNICA,
-- não texto de tabela. Quem guarda e aplica é o KERNEL do Combat
-- (`Combat.applyStatus`) — é ele que entra no mesmo cálculo de WalkSpeed e que
-- CLAMPA o atordoamento no teto de 0,8 s. Os números abaixo são fantasia/game
-- feel desta raia; o teto é lei da outra e não dá para furar daqui.
-- Lamaçal NÃO aparece com `slow`: a lentidão dele já vem do terreno encharcado
-- (Balance.terrain.mudSlowMult) e duplicar deixaria a poça duas vezes pior.
-- ============================================================================
type Recipe = {
	--- elementos aplicados ao terreno, na ordem
	apply: { string },
	strong: boolean?,
	--- fração do raio do combo usada NO TERRENO (o dano continua no raio cheio)
	scale: number?,
	--- empurrão horizontal em studs/s (nil = combo não empurra)
	push: number?,
	--- atordoamento em segundos (o Combat clampa no teto — pedir mais é inócuo)
	stun: number?,
	--- lentidão: duração em segundos + multiplicador de velocidade
	slow: number?,
	slowMult: number?,
	--- revelação em segundos (o Combat clampa em 2,0 s)
	reveal: number?,
}

local K = Elements.comboKey

local EFFECTS: { [string]: Recipe } = {
	-- Tornado Flamejante: incendeia e SUGA/empurra (GDD §9)
	[K("Fire", "Wind")] = { apply = { "Fire" }, push = 70 },
	-- Chuva de Magma: queima a área e nega terreno
	[K("Fire", "Earth")] = { apply = { "Fire" } },
	-- Explosão de Plasma: burst; o raio ainda conduz por qualquer poça
	[K("Fire", "Lightning")] = { apply = { "Lightning", "Fire" }, push = 40 },
	-- Cortina de Vapor: a água escalda e APAGA o incêndio embaixo. GDD §9 pede
	-- "névoa escaldante que cega"; cegueira é efeito de CLIENTE (raia de UI), e
	-- a metade que o servidor consegue garantir é o passo travado no vapor.
	[K("Fire", "Water")] = { apply = { "Water" }, slow = 2.0, slowMult = 0.55 },
	-- Eletrocussão: só Raio — a água conectada inteira eletriza (flood fill).
	-- O ATORDOAMENTO é a promessa da tabela do GDD §9, e agora existe de fato.
	[K("Water", "Lightning")] = { apply = { "Lightning" }, stun = 0.7 },
	-- Lamaçal: a água encharca terra/carvão e vira lama (lentidão severa)
	[K("Water", "Earth")] = { apply = { "Water" } },
	-- Tempestade Torrencial: o counter do GDD §9 — APAGA fogo e empurra
	[K("Water", "Wind")] = { apply = { "Water" }, push = 55 },
	-- Tempestade de Areia: rajada que abafa chama pequena e cega. A areia GRUDA
	-- em quem está dentro — dentro da nuvem, o marcado é o único visível. É a
	-- leitura jogável de "cegueira em área" com contra-jogada: dura 1,8 s.
	[K("Earth", "Wind")] = { apply = { "Wind" }, push = 30, reveal = 1.8 },
	-- Cristais Carregados: cacho de cristais (muros) + carga elétrica. O terreno
	-- usa METADE do raio de propósito: o GDD pede MINAS, não uma redoma — um
	-- disco cheio de muros no raio 16 prenderia o alvo por 20s (wallDuration).
	-- (o "atordoam ao quebrar" da tabela do GDD §9 vira atordoamento no estouro
	-- inicial: 0,6 s, o piso da janela da auditoria — o combo já prende com muros)
	[K("Earth", "Lightning")] = { apply = { "Earth", "Lightning" }, strong = true, scale = 0.5, stun = 0.6 },
	-- Nuvem Tempestuosa: descarga que persegue; conduz na água
	[K("Wind", "Lightning")] = { apply = { "Lightning" }, push = 25 },
}

-- ---------------------------------------------------------------------- tipos
--- PARTICIPANTE (débito 2): a Sintonia aceita `Teams.Member` (jogador OU
--- Humanoid de bot) — o parceiro bot do roster ímpar também vive o pilar.
type Cast = {
	player: Teams.Member,
	element: Elements.Id,
	pos: Vector3,
	t: number,
}

type Channel = {
	a: Teams.Member,
	b: Teams.Member,
	elA: Elements.Id,
	elB: Elements.Id,
	combo: Elements.Combo,
	pos: Vector3,
	fireAt: number,
}

--- Ping de Sintonia (GDD §18.7). `withUserId` preenchido = pacto selado: os
--- DOIS veem o mesmo alvo marcado.
type Proposal = {
	player: Teams.Member,
	element: Elements.Id,
	pos: Vector3,
	expires: number,
	withUserId: number?,
}

local Sintonia = {}

local terrain: any = nil
local combat: any = nil
local evState: RemoteEvent
local evAnnounce: RemoteEvent

--- conjurações dentro da janela (podado todo step; ~120 entradas no pior caso)
local recent: { Cast } = {}
local channels: { Channel } = {}
--- proposta indexada pelo userId de QUEM PROPÔS
local proposals: { [number]: Proposal } = {}
--- cooldown compartilhado e trava de canalização, por userId
local readyAt: { [number]: number } = {}
local busy: { [number]: boolean } = {}
local pingReadyAt: { [number]: number } = {}
--- FUNIL: pares de aliados com a janela de combo ABERTA agora (borda de subida)
local oppOpen: { [string]: boolean } = {}
local oppAcc = 0

-- ----------------------------------------------------------------- telemetria
--- Require preguiçoso e protegido (mesmo padrão do Combat→Sintonia): se o
--- coletor cair, o PILAR do jogo continua funcionando. O funil desta raia é o
--- que responde à pergunta V1 — oportunidade → ping → aceite → canalização →
--- (interrompida | disparada).
local telemetry: any = nil
local function emit(name: string, subject: any, payload: any?)
	if telemetry == nil then
		local mod = script.Parent and script.Parent:FindFirstChild("Telemetry")
		local ok, m = pcall(function()
			return require(mod :: ModuleScript)
		end)
		telemetry = (ok and m) or false
	end
	if telemetry and telemetry.emit then
		pcall(telemetry.emit, name, subject, payload)
	end
end

-- -------------------------------------------------------------------- helpers
local function isPlayer(m: any): boolean
	return typeof(m) == "Instance" and m:IsA("Player")
end

--- personagem/corpo de um participante: Character do jogador, Model do bot
local function charOf(m: Teams.Member): Instance?
	if isPlayer(m) then
		return (m :: Player).Character
	end
	return (m :: Humanoid).Parent
end

local function rootOf(m: Teams.Member): BasePart?
	local char = charOf(m)
	if not char or not char.Parent then
		return nil
	end
	return char:FindFirstChild("HumanoidRootPart") :: BasePart?
end

local function isAlive(m: Teams.Member): boolean
	if typeof(m) ~= "Instance" or not m.Parent then
		return false -- saiu do jogo / bot destruído
	end
	if m:IsA("Humanoid") then
		return (m :: Humanoid).Health > 0
	end
	local char = charOf(m)
	local hum = if char and char.Parent then char:FindFirstChildOfClass("Humanoid") else nil
	return hum ~= nil and hum.Health > 0
end

--- Chave numérica estável de um participante nas tabelas de cooldown/pacto.
--- Jogador = UserId; bot = id sintético NEGATIVO (nunca colide com UserId real
--- e o cliente, que resolve por `GetPlayerByUserId`, ignora sem erro nenhum).
--- CHAVES/VALORES FRACOS: bot destruído solta o par sozinho.
local nextBotUid = 0
local botUids: { [Humanoid]: number } = setmetatable({}, { __mode = "k" }) :: any
local botByUid: { [number]: Humanoid } = setmetatable({}, { __mode = "v" }) :: any
local function uidOf(m: Teams.Member): number
	if isPlayer(m) then
		return (m :: Player).UserId
	end
	local hum = m :: Humanoid
	local uid = botUids[hum]
	if not uid then
		nextBotUid -= 1
		uid = nextBotUid
		botUids[hum] = uid
		botByUid[uid] = hum
	end
	return uid
end

--- elemento ativo do participante (bot tem elemento fixo, via Combat)
local function elementOf(m: Teams.Member): string?
	if not combat then
		return nil
	end
	if isPlayer(m) then
		return combat.getElement(m)
	end
	return if combat.getNpcElement then combat.getNpcElement(m :: Humanoid) else nil
end

local function onCooldown(m: Teams.Member): boolean
	return os.clock() < (readyAt[uidOf(m)] or 0)
end

--- ELIMINADO NÃO AGE (P0): a trava é do Combat (quem o Match avisa), inclusive
--- para o PING — espectador não propõe nem aceita Sintonia. Bot não passa pela
--- trava (bot morto já falha o isAlive).
local function blocked(m: Teams.Member): boolean
	if isPlayer(m) then
		return combat ~= nil and combat.canAct ~= nil and combat.canAct(m) == false
	end
	return false
end

--- chave comutativa de um par de jogadores (funil de oportunidade)
local function pairKey(a: number, b: number): string
	return if a < b then a .. ":" .. b else b .. ":" .. a
end

local function flat(pos: Vector3): Vector3
	return Vector3.new(pos.X, COMBO_Y, pos.Z)
end

--- Pacto ativo entre os dois? Devolve o alvo marcado (GDD §18.7).
local function markBetween(from: number, to: number): Vector3?
	local p = proposals[from]
	if p and p.withUserId == to and os.clock() <= p.expires then
		return p.pos
	end
	return nil
end

local function pactMark(a: Teams.Member, b: Teams.Member): Vector3?
	return markBetween(uidOf(a), uidOf(b)) or markBetween(uidOf(b), uidOf(a))
end

--- Sela o pacto. O PRIMEIRO aceite vale e mais nenhum: reaceitar a mesma
--- proposta esticava `expires` para `now + PACT_TTL` a cada chamada, e o
--- `AcceptSintonia` não tem cooldown — uma dupla que spamasse o remote ficava
--- com o alvo marcado PARA SEMPRE (o pacto é o que deixa as duas conjurações
--- fundirem LONGE uma da outra, então o TTL de 12 s é limitador de jogo, não
--- enfeite) e cada chamada ainda custava dois FireClient. Devolve `false`
--- quando não havia o que selar — é assim que o `accept` sai barato.
local function sealPact(prop: Proposal, withUserId: number, now: number): boolean
	if prop.withUserId ~= nil then
		return false -- já selado: o primeiro aceite é o que vale
	end
	prop.withUserId = withUserId
	prop.expires = now + PACT_TTL
	return true
end

--- "Mesmo alvo/área" (GDD §9). Devolve o EPICENTRO do combo, ou nil.
--- ponytail: o pacto do Ping dá a única vantagem MECÂNICA do ping — as duas
--- conjurações valem se as duas caírem perto do ALVO COMBINADO, ainda que
--- longe uma da outra. Sem isso o ping seria só enfeite; com isso, combinar
--- com um desconhecido pelo botão compensa.
local function epicenter(a: Cast, bPlayer: Teams.Member, bPos: Vector3): Vector3?
	-- P0 (auditoria 18/08): SÓ ENTRE ALIADOS. Este é o funil único por onde
	-- TODA fusão passa — proximidade e pacto de ping incluídos —, então a regra
	-- vive aqui e em lugar nenhum mais. Inimigo perto conjurando outro elemento
	-- não funde, não queima cooldown de ninguém e não conta no funil da V1.
	if not Teams.areAllies(a.player, bPlayer) then
		return nil
	end
	if (a.pos - bPos).Magnitude <= S.targetRadius then
		return flat((a.pos + bPos) / 2)
	end
	local mark = pactMark(a.player, bPlayer)
	if mark and (a.pos - mark).Magnitude <= S.targetRadius and (bPos - mark).Magnitude <= S.targetRadius then
		return flat(mark)
	end
	return nil
end

--- Humanoides dentro do raio, menos os participantes. Usa a consulta espacial
--- NATIVA: pega bots (R3) e qualquer coisa com Humanoid de graça, e o filtro
--- exclui a pasta Arena (senão a esfera devolveria centenas de parts de piso).
local function humanoidsIn(pos: Vector3, radius: number, skip: { Teams.Member }): { Humanoid }
	local exclude: { Instance } = {}
	local arena = Workspace:FindFirstChild("Arena")
	if arena then
		table.insert(exclude, arena)
	end
	for _, p in ipairs(skip) do
		local char = charOf(p)
		if char then
			table.insert(exclude, char)
		end
	end

	local params = OverlapParams.new()
	params.FilterType = Enum.RaycastFilterType.Exclude
	params.FilterDescendantsInstances = exclude

	local out: { Humanoid } = {}
	local seen: { [Humanoid]: boolean } = {}
	for _, part in ipairs(Workspace:GetPartBoundsInRadius(pos, radius, params)) do
		local model = part:FindFirstAncestorOfClass("Model")
		local hum = if model then model:FindFirstChildOfClass("Humanoid") else nil
		if hum and not seen[hum] and hum.Health > 0 then
			seen[hum] = true
			table.insert(out, hum)
		end
	end
	return out
end

-- ------------------------------------------------------------------ broadcast
--- Fato decidido pelo servidor. O cliente SÓ APRESENTA (Net.SintoniaState).
--- Require preguiçoso e protegido do Tutorial (mesmo padrão do Combat→Sintonia):
--- se o módulo faltar ou explodir, a resposta é "não está treinando" e o funil
--- volta a contar tudo — nunca o contrário, e nunca derrubando a partida.
--- TELEMETRIA NÃO DECIDE JOGO: esta consulta só CARIMBA o evento; o ping em si
--- acontece igual, com as mesmas regras, treinando ou não.
local tutorialMod: any = nil
local function inTutorial(player: any): boolean
	if tutorialMod == nil then
		local mod = script.Parent and script.Parent:FindFirstChild("Tutorial")
		local ok, m = pcall(function()
			return require(mod :: ModuleScript)
		end)
		tutorialMod = (ok and m) or false
	end
	if not tutorialMod or not tutorialMod.active then
		return false
	end
	local ok, active = pcall(tutorialMod.active, player)
	return ok and active == true
end

local function announceState(payload: { [string]: any }, only: { Teams.Member }?)
	if only then
		for _, p in ipairs(only) do
			-- bot na audiência não tem cliente: só Player recebe FireClient
			if isPlayer(p) and p.Parent then
				evState:FireClient(p :: Player, payload)
			end
		end
	else
		evState:FireAllClients(payload)
	end
end

--- Nome do combo no idioma de CADA jogador. `Player.LocaleId` é legível no
--- servidor — não dá para mandar um texto só e acertar os dois idiomas.
local function announceCombo(combo: Elements.Combo)
	for _, p in ipairs(Players:GetPlayers()) do
		local isPt = string.sub(p.LocaleId or "", 1, 2) == "pt"
		evAnnounce:FireClient(p, {
			key = "announce.sintonia.fired",
			args = { if isPt then combo.pt else combo.en },
		})
	end
end

--- Placar da partida (raia R3). `Match.noteCombo` foi publicado LÁ pedindo que
--- a R2 chame na fusão — sem isso o Selo do Campeão mostra 0 combos (GDD
--- §18.6). Require preguiçoso e protegido, o mesmo padrão que o Combat usa
--- para falar com este módulo: se o Match sumir, a Sintonia continua de pé.
local match: any = nil
local function noteCombo(player: Teams.Member, comboKey: string)
	if not isPlayer(player) then
		return -- bot não entra no Selo do Campeão por aqui (crédito é do Match)
	end
	if match == nil then
		local mod = script.Parent and script.Parent:FindFirstChild("Match")
		local ok, m = pcall(function()
			return require(mod :: ModuleScript)
		end)
		match = (ok and m) or false
	end
	if match and match.noteCombo then
		pcall(match.noteCombo, player, comboKey)
	end
end

-- ----------------------------------------------------------------- canalização
--- Cobra o custo dos DOIS e abre a canalização. Ponto único onde a Sintonia
--- "gasta" — chamado antes de qualquer efeito, de propósito.
local function beginChannel(a: Cast, b: Teams.Member, elB: Elements.Id, pos: Vector3)
	local combo = Elements.getCombo(a.element, elB)
	if not combo then
		return -- mesmo elemento (ou par inexistente): não funde
	end
	local now = os.clock()

	busy[uidOf(a.player)] = true
	busy[uidOf(b)] = true
	-- CUSTO COBRADO JÁ (GDD §9): interromper NÃO devolve o cooldown. É o que
	-- torna o combo errado punitivo e o spam impossível. Vale IGUAL para o bot:
	-- o cooldown compartilhado é o custo dele (bot não tem mana).
	readyAt[uidOf(a.player)] = now + S.sharedCooldown
	readyAt[uidOf(b)] = now + S.sharedCooldown

	-- "Consome a magia dos DOIS": o cooldown compartilhado acima é a parte que
	-- vive nesta raia. A mana é do Combat e só JOGADOR tem mana — o custo do
	-- bot é o cooldown acima.
	if combat and combat.drainMana then
		if isPlayer(a.player) then
			pcall(combat.drainMana, a.player)
		end
		if isPlayer(b) then
			pcall(combat.drainMana, b)
		end
	end

	table.insert(channels, {
		a = a.player,
		b = b,
		elA = a.element,
		elB = elB,
		combo = combo,
		pos = pos,
		fireAt = now + S.channel,
	})

	-- TODOS veem a canalização, inimigo inclusive: é o telegrama que dá a
	-- janela de interrupção exigida pelo GDD §9.
	announceState({
		phase = "channeling",
		comboKey = combo.key,
		pos = pos,
		radius = combo.radius,
		duration = S.channel,
		byUserId = uidOf(a.player),
		withUserId = uidOf(b),
	})
	emit(Events.Name.SintoniaChannelStart, a.player, {
		combo_key = combo.key,
		with_user_id = uidOf(b),
		element = a.element,
		with_element = elB,
		pos = pos,
		channel = S.channel,
		--- o par saiu de um PACTO de ping ou foi coincidência de tiro? é a
		--- diferença entre "coordenaram de propósito" e "aconteceu"
		by_pact = pactMark(a.player, b) ~= nil,
	})
end

local function releaseChannel(ch: Channel)
	busy[uidOf(ch.a)] = nil
	busy[uidOf(ch.b)] = nil
end

--- INTERROMPIDA (GDD §9): um dos dois morreu, saiu ou foi eliminado no meio.
--- Ponto único para os dois caminhos (tick e saída do jogador) contarem a MESMA
--- coisa no funil.
---
--- ANTI-GRIEFING G1 (R9) — QUEM CONTINUA DE PÉ NÃO PAGA. O cooldown é
--- COMPARTILHADO e cobrado no INÍCIO (GDD §9): sem esta linha, um parceiro
--- hostil — ou que só deslogou no meio — tirava 24 s do pilar do jogo do outro
--- com um ato unilateral, e o alpha pareia ESTRANHOS em dupla. A devolução vai
--- só para os participantes ainda VIVOS; quem caiu/saiu continua pagando.
--- IMPOSSÍVEL DE CONTORNAR: a única forma de cancelar uma canalização é MORRER
--- ou SAIR, e ninguém provoca o próprio reembolso — só o do parceiro, que fica
--- sozinho e sem com quem fundir. O limitador do GDD §9 continua inteiro: o
--- combo que SAI cobra os dois (é ele que impede o spam), e o combo ERRADO
--- (fusão indesejada que dispara) segue deixando a dupla seca.
local function failChannel(ch: Channel, reason: string)
	releaseChannel(ch)
	for _, m in ipairs({ ch.a, ch.b }) do
		if isAlive(m) then
			readyAt[uidOf(m)] = nil
		end
	end
	announceState({
		phase = "failed",
		comboKey = ch.combo.key,
		pos = ch.pos,
		byUserId = uidOf(ch.a),
		withUserId = uidOf(ch.b),
	})
	emit(Events.Name.SintoniaInterrupted, ch.a, {
		combo_key = ch.combo.key,
		with_user_id = uidOf(ch.b),
		pos = ch.pos,
		reason = reason,
	})
end

--- A fusão acontece: dano em área ×dmgMult + a receita de terreno do combo.
local function fireChannel(ch: Channel)
	local combo = ch.combo
	local recipe = EFFECTS[combo.key]

	-- 1) MUNDO — todo combo interage com o terreno (GDD §9)
	--- AUTORIA DA FUSÃO (R13, pergunta V3). A fusão é o pilar do jogo e as
	--- receitas que mexem no terreno são justamente as jogadas mais
	--- espetaculares que a V3 deveria capturar — sem autor, o `applyElement`
	--- não abre `Cast` e nada disso vira desfecho tático.
	--- QUEM É O AUTOR: `ch.a`, o que conjurou PRIMEIRO. Três razões, nesta
	--- ordem:
	---   · é o MESMO sujeito de `sintonia_channel_start` e `sintonia_fired` —
	---     dar autor diferente ao terreno faria um único ato aparecer sob dois
	---     donos em eventos que se juntam pelo ator;
	---   · UM autor por fusão é o que segura a razão de funil abaixo de 100%:
	---     `crossingAt`/`openedCoverAt` consomem o disparo UMA vez, e creditar
	---     os dois dobraria o desfecho do mesmo ato;
	---   · o pacto de ping é OPCIONAL (a maioria das fusões é coincidência de
	---     tiro), então "o proponente" não existe em todo canal — autor que só
	---     às vezes existe é pior que autor sempre igual.
	--- SEM FILTRO DE `Player`: `ch.a` pode ser o parceiro BOT (uid negativo), e
	--- é o Telemetry que separa humano de bot pelo `is_bot`. Filtrar aqui
	--- quebraria o débito 2 e ainda esconderia a metade que o `all` mede.
	--- NÃO INFLA A V4: o índice de dominância se alimenta de `damage` e
	--- `spell_cast`, e nenhum dos dois muda com esta linha — o dano da fusão
	--- continua carimbado `"Combo"` (R8) e o dano de terreno continua com
	--- `fromPlayer = nil`. A autoria vive só no evento de DESFECHO.
	--- O elemento carimbado é o da RECEITA (a fonte real do que caiu no mundo),
	--- nunca o que o autor tem na mão agora — o viés que quase custou um nerf
	--- errado na R8.
	local cx, cy = Grid.worldToCell(ch.pos)
	if recipe and terrain and Grid.inBounds(cx, cy) then
		local radiusCells = combo.radius * (recipe.scale or 1) / Grid.CELL_SIZE
		for _, element in ipairs(recipe.apply) do
			terrain.applyElement(cx, cy, element, radiusCells, recipe.strong == true, ch.a)
		end
	end

	-- 2) DANO — "mais forte que a soma das partes" (GDD §9), literalmente: a
	-- soma dos dois básicos multiplicada por `dmgMult`. Os dois conjuradores
	-- ficam de fora (a magia é DELES). Efeitos persistentes (lava, poça
	-- eletrizada, lama) continuam machucando pelo terreno, via Combat.
	local dmg = (Balance.elements[ch.elA].basic.dmg + Balance.elements[ch.elB].basic.dmg) * S.dmgMult
	local victims = humanoidsIn(ch.pos, combo.radius, { ch.a, ch.b })
	for i, hum in ipairs(victims) do
		-- crédito de evolução do Escudo alternado: o combo é dos DOIS e um só
		-- número de dano por alvo continua legível na tela (GDD §5).
		local credit = if i % 2 == 0 then ch.b else ch.a
		--- crédito BOT vai pela via de autor-sem-Player do applyDamage (débito
		--- 2): a telemetria não carimba o dano do combo como "ambiente" e o
		--- escudo do bot não evolui (bot não tem escudo — regra do Combat).
		local creditPlayer: Player? = if isPlayer(credit) then credit :: Player else nil
		if combat then
			-- ATRIBUIÇÃO DA FUSÃO (correção de medição, 18/08): o dano do combo
			-- entrava no NUMERADOR da dominância por elemento sem gerar
			-- `spell_cast` para o denominador — 4,6–5,6% de todo o dano do jogo
			-- distorcendo o índice (concentrado em Terra e Vento). A fusão não
			-- é elemento nenhum: carimba `"Combo"`, que o índice (que itera os
			-- 5 elementos) ignora, e a telemetria contabiliza à parte.
			if creditPlayer then
				combat.applyDamage(hum, dmg, creditPlayer, true, nil, "Combo")
			else
				combat.applyDamage(hum, dmg, nil, true, credit, "Combo")
			end
			-- STATUS pelo KERNEL (P1): o Combat é o único que guarda efeito e o
			-- único que escreve WalkSpeed. Daqui não dá para furar o teto de
			-- atordoamento — `applyStatus` clampa e devolve o que valeu.
			if recipe then
				if recipe.stun then
					combat.applyStatus(hum, "Stun", recipe.stun, nil, creditPlayer)
				end
				if recipe.slow then
					combat.applyStatus(hum, "Slow", recipe.slow, recipe.slowMult, creditPlayer)
				end
				if recipe.reveal then
					combat.applyStatus(hum, "Reveal", recipe.reveal, nil, creditPlayer)
				end
			end
		end
		local push = recipe and recipe.push
		local root = hum.RootPart
		if push and root and root.Parent then
			local away = root.Position - ch.pos
			local dir = Vector3.new(away.X, 0, away.Z)
			dir = if dir.Magnitude > 1e-3 then dir.Unit else Vector3.new(0, 0, -1)
			-- ponytail: TETO CONHECIDO — o personagem tem network ownership do
			-- cliente dono, então isto é uma ORDEM, não garantia física (mesma
			-- ressalva do dash em Combat.step). Se o playtest mostrar empurrão
			-- borrachudo, o upgrade é root:SetNetworkOwner(nil) por ~0,3s.
			root.AssemblyLinearVelocity = dir * push + Vector3.new(0, PUSH_UP, 0)
		end
	end

	announceState({
		phase = "fired",
		comboKey = combo.key,
		pos = ch.pos,
		radius = combo.radius,
		duration = combo.duration,
		byUserId = uidOf(ch.a),
		withUserId = uidOf(ch.b),
		hits = #victims,
	})
	announceCombo(combo)
	noteCombo(ch.a, combo.key)
	noteCombo(ch.b, combo.key)
	emit(Events.Name.SintoniaFired, ch.a, {
		combo_key = combo.key,
		with_user_id = uidOf(ch.b),
		element = ch.elA,
		with_element = ch.elB,
		pos = ch.pos,
		radius = combo.radius,
		damage = dmg,
		hits = #victims,
	})
end

-- --------------------------------------------------------------------- API
function Sintonia.init(terrainModule: any, combatModule: any)
	terrain = terrainModule
	combat = combatModule
	evState = Net.event(Net.Names.SintoniaState)
	evAnnounce = Net.event(Net.Names.Announce)
end

--- Chamado pelo Combat a cada conjuração que impacta: alimenta a janela.
--- Trata TUDO como hostil: elemento válido, vivo, sem cooldown, sem canalizar.
--- Aceita `Teams.Member` (débito 2): o parceiro BOT entra pelo MESMO funil.
function Sintonia.registerCast(player: Teams.Member, elementId: string, pos: Vector3)
	if not evState or typeof(pos) ~= "Vector3" or not Elements.isValid(elementId) then
		return
	end
	local uid = uidOf(player)
	if not isAlive(player) or blocked(player) or busy[uid] or onCooldown(player) then
		return
	end
	local element = elementId :: Elements.Id
	local now = os.clock()

	for i = #recent, 1, -1 do
		local c = recent[i]
		if now - c.t > S.window then
			table.remove(recent, i) -- fora da janela de 1,5s
			continue
		end
		-- mesmo jogador não combina consigo; MESMO ELEMENTO NÃO FUNDE (GDD §9)
		if c.player == player or c.element == element then
			continue
		end
		if busy[uidOf(c.player)] or onCooldown(c.player) or not isAlive(c.player) or blocked(c.player) then
			continue
		end
		local center = epicenter(c, player, pos)
		if center then
			table.remove(recent, i)
			beginChannel(c, player, element, center)
			return
		end
	end

	table.insert(recent, { player = player, element = element, pos = pos, t = now })

	-- A JANELA ABRIU (1,5 s). Até hoje isto era estado interno do servidor e o
	-- cliente não tinha como desenhar a barra certa — quem podia completar o
	-- combo não sabia que tinha uma contagem correndo. Vai para quem conjurou e
	-- para os ALIADOS (são os únicos que podem fundir; avisar o inimigo seria
	-- entregar o telegrama antes da hora — o dele é a canalização).
	-- ponytail: TETO CONHECIDO — sai a cada conjuração que impacta (~4/s no
	-- básico mais rápido), contra os 10 Hz que o StatsUpdate já manda por
	-- jogador. Cabe. Se o orçamento de rede no celular reclamar, o upgrade é
	-- limitar a 1 envio por ~0,3 s por jogador — e NÃO mandar só na borda de
	-- subida, senão a barra "fecha" enquanto a janela real ainda está renovando.
	local audience: { Teams.Member } = {}
	if isPlayer(player) then
		table.insert(audience, player)
	end
	for _, mate in ipairs(Teams.matesOf(player)) do
		if isPlayer(mate) then
			table.insert(audience, mate)
		end
	end
	announceState({
		phase = "window",
		windowLeft = S.window,
		byUserId = uid,
		element = element,
		pos = pos,
	}, audience)

	-- PARCEIRO BOT RESPONDE (débito 2): com o PACTO selado com um bot, a magia
	-- do humano caindo NA MARCA faz o bot conjurar a dele no mesmo alvo — e o
	-- funil NORMAL acima decide o resto (janela, epicentro, canalização
	-- interrompível, cooldown compartilhado cobrado no início: GDD §9 inteiro).
	-- Só o PACTO dispara isto: sem ping aceito, bot não funde — coordenação
	-- continua sendo um ATO, não um efeito colateral de atirar perto.
	local prop = proposals[uid]
	local botUid = if prop and now <= prop.expires then prop.withUserId else nil
	local bot = if botUid then botByUid[botUid :: number] else nil
	if bot and (pos - (prop :: Proposal).pos).Magnitude <= S.targetRadius then
		local elBot = elementOf(bot)
		if
			Elements.isValid(elBot)
			and Elements.getCombo(element, elBot :: Elements.Id) ~= nil
			and not busy[uidOf(bot)]
			and not onCooldown(bot)
			and isAlive(bot)
		then
			Sintonia.registerCast(bot, elBot :: string, (prop :: Proposal).pos)
		end
	end
end

-- ------------------------------------------------- Ping de Sintonia (§18.7)
--- Posição pingada pelo cliente NUNCA entra crua: rejeita NaN/infinito e
--- prende o alvo ao alcance de uma magia a partir de quem pingou.
local function sanitizePos(raw: any, from: Vector3): Vector3
	if typeof(raw) ~= "Vector3" then
		return from
	end
	local m = raw.Magnitude
	if m ~= m or m == math.huge then
		return from
	end
	local d = raw - from
	if d.Magnitude > PING_RANGE then
		return from + d.Unit * PING_RANGE
	end
	return raw
end

--- "COMBO?" — propõe Sintonia ao ALIADO que estiver por perto, com o elemento
--- ativo. Só aliado ouve: pingar inimigo não é combinar, é dar informação.
function Sintonia.propose(player: Player, rawPos: any)
	if not evState or not isAlive(player) or blocked(player) then
		return
	end
	local uid = player.UserId
	local now = os.clock()
	if now < (pingReadyAt[uid] or 0) then
		return -- anti-spam de ping
	end
	local root = rootOf(player)
	if not root then
		return
	end
	local element = combat and combat.getElement(player)
	if not Elements.isValid(element) then
		return
	end

	pingReadyAt[uid] = now + PING_COOLDOWN
	local pos = flat(sanitizePos(rawPos, root.Position))
	proposals[uid] = {
		player = player,
		element = element :: Elements.Id,
		pos = pos,
		expires = now + PROPOSAL_TTL,
		withUserId = nil,
	}

	local payload = {
		phase = "proposed",
		byUserId = uid,
		byName = player.DisplayName,
		element = element,
		pos = pos,
		ttl = PROPOSAL_TTL,
	}
	--- o proponente vê a própria marca; os ALIADOS por perto veem a proposta
	local audience: { Player } = { player }
	local heard = 0
	for _, other in ipairs(Players:GetPlayers()) do
		local r = if Teams.areAllies(player, other) then rootOf(other) else nil
		if r and (r.Position - root.Position).Magnitude <= PING_HEAR then
			table.insert(audience, other)
			heard += 1
			evAnnounce:FireClient(other, {
				key = "announce.sintonia.proposed",
				args = { player.DisplayName },
			})
		end
	end
	announceState(payload, audience)
	--- `tutorial` CARIMBA a origem: o Campo de Provas usa o ping DE VERDADE (é o
	--- que torna o passo honesto), mas quem está aprendendo não está respondendo
	--- à pergunta "duas pessoas coordenam sozinhas?" — e antes da R11 TODO
	--- jogador era empurrado ao tutorial, então cada sessão injetava ping de
	--- treino no numerador da pergunta central do produto.
	--- CARIMBAR e não SUPRIMIR: o evento aconteceu, e apagá-lo tiraria do Diretor
	--- a única medida de quantas pessoas praticam o combo. Quem decide o que
	--- entra no funil é o Telemetry, que é onde a pergunta V1 é respondida.
	emit(
		Events.Name.SintoniaPing,
		player,
		{ element = element, pos = pos, allies_reached = heard, tutorial = inTutorial(player) }
	)

	-- PARCEIRO BOT (débito 2): duo ímpar tem bot de parceiro, e o pilar do jogo
	-- não pode ser exclusivo de dupla humana. O bot ALIADO ao alcance do ping
	-- responde como um jogador responderia: aceita pelo MESMO `accept` (mesmas
	-- validações), um instante depois. A fusão sai quando a magia do humano
	-- cair na marca (hook no registerCast).
	for _, m in ipairs(Teams.matesOf(player)) do
		if typeof(m) == "Instance" and m:IsA("Humanoid") then
			local r = rootOf(m)
			if r and (r.Position - root.Position).Magnitude <= PING_HEAR then
				task.delay(BOT_ACCEPT_DELAY, function()
					Sintonia.accept(m, uid)
				end)
				break -- o pacto é de DOIS: um parceiro responde
			end
		end
	end
end

--- 1 toque do aliado: o alvo passa a ser COMUM aos dois (GDD §18.7).
--- Aceita `Teams.Member` (débito 2): o bot aliado responde por este MESMO funil.
function Sintonia.accept(player: Teams.Member, fromUserId: any)
	if not evState or typeof(fromUserId) ~= "number" then
		return
	end
	local prop = proposals[fromUserId]
	if not prop or prop.player == player or os.clock() > prop.expires then
		return
	end
	if not isAlive(player) or not isAlive(prop.player) or blocked(player) then
		return
	end
	if not Teams.areAllies(player, prop.player) then
		return -- pacto é entre aliados: aceitar ping de inimigo não sela nada
	end
	local mine, theirs = rootOf(player), rootOf(prop.player)
	if not mine or not theirs or (mine.Position - theirs.Position).Magnitude > PING_HEAR then
		return -- aceitar do outro lado do mapa não é combinar
	end

	--- o `expires` vira PACT_TTL dentro do `sealPact`: mede antes de sobrescrever
	local proposedAt = prop.expires - PROPOSAL_TTL
	if not sealPact(prop, uidOf(player), os.clock()) then
		return -- pacto já selado: nada a anunciar, nada a renovar
	end
	announceState({
		phase = "proposed",
		byUserId = fromUserId,
		byName = prop.player.DisplayName,
		withUserId = uidOf(player),
		withName = player.DisplayName,
		element = prop.element,
		--- elemento de QUEM ACEITOU. Sem ele, o proponente não consegue mostrar
		--- o nome do combo que vai sair — e o card fica dizendo menos do que
		--- sabe. Costura do coordenador (pedido da raia de UI, 18/08).
		withElement = elementOf(player),
		pos = prop.pos,
		ttl = PACT_TTL,
	}, { player, prop.player })
	emit(Events.Name.SintoniaAccept, player, {
		from_user_id = fromUserId,
		element = prop.element,
		pos = prop.pos,
		--- quanto tempo o aliado levou para topar: é a leitura de "o ping é
		--- entendido?" (aceite lento = a UI não está comunicando)
		latency = os.clock() - proposedAt,
	})
end

-- ------------------------------------------- FUNIL: o denominador (§V1)
--- "Deu para combinar?" — sem este número, `sintonia_fired` não tem com o que
--- ser dividido e a pergunta V1 fica sem resposta: 3 combos por partida é muito
--- ou pouco? Só dá para saber contra quantas VEZES o combo esteve disponível.
---
--- OPORTUNIDADE = dois ALIADOS prontos (vivos, sem cooldown, sem canalizar, não
--- eliminados), com ELEMENTOS DIFERENTES, perto um do outro, e um INIMIGO comum
--- ao alcance do ponto entre eles.
--- Emitido na BORDA DE SUBIDA (quando a janela abre), nunca a cada tick: assim
--- o número conta OPORTUNIDADES, não segundos de dupla parada olhando o mesmo
--- inimigo — que inflaria o denominador e faria a coordenação parecer rara.
local OPP_SCAN = 0.5

local function ready(player: Player): boolean
	return isAlive(player) and not busy[player.UserId] and not onCooldown(player) and not blocked(player)
end

--- Existe alvo de OUTRO time ao alcance do epicentro? Bot conta (o Teams dá
--- time ao Humanoid dele), e quem está sem time conta como inimigo.
--- ALCANCE DA OPORTUNIDADE (correção do coordenador, 18/08 — achado do harness
--- headless: 0 oportunidades em 191 s de partida, com a dupla junta atirando no
--- mesmo alvo). A varredura usava `S.targetRadius` (14 studs), que é o raio que
--- decide se duas magias caíram NO MESMO ALVO — critério certo para FUNDIR,
--- errado para "havia chance de fundir": o inimigo teria de estar praticamente
--- em cima do ponto médio da dupla, e projétil voa até 90 studs. Com o
--- denominador zerado, a taxa de conversão do funil — a resposta à pergunta V1
--- do projeto — ficava impossível de calcular.
--- CALIBRAÇÃO: é este número se o denominador vier inflado no playtest.
local OPP_RANGE = PING_RANGE

local function enemyNear(pos: Vector3, teamId: number?, skip: { Player }, radius: number?): boolean
	for _, hum in ipairs(humanoidsIn(pos, radius or S.targetRadius, skip)) do
		local char = hum.Parent
		local asPlayer = if char then Players:GetPlayerFromCharacter(char :: Instance) else nil
		if Teams.teamOf(asPlayer or hum) ~= teamId then
			return true
		end
	end
	return false
end

local function scanOpportunities()
	local players = Players:GetPlayers()
	local seen: { [string]: boolean } = {}

	for i = 1, #players do
		for j = i + 1, #players do
			local a, b = players[i], players[j]
			if not Teams.areAllies(a, b) then
				continue
			end
			local key = pairKey(a.UserId, b.UserId)
			seen[key] = true

			local open = false
			local elA: string? = nil
			local elB: string? = nil
			local mid: Vector3? = nil
			if ready(a) and ready(b) and combat then
				elA, elB = combat.getElement(a), combat.getElement(b)
				local ra, rb = rootOf(a), rootOf(b)
				-- elementos IGUAIS não fundem (GDD §9): não é oportunidade
				if elA and elB and elA ~= elB and ra and rb and (ra.Position - rb.Position).Magnitude <= PING_HEAR then
					mid = flat((ra.Position + rb.Position) / 2)
					-- alcance de ENGAJAMENTO, não de fusão (ver OPP_RANGE acima)
					open = enemyNear(mid :: Vector3, Teams.teamOf(a), { a, b }, OPP_RANGE)
				end
			end

			if open and not oppOpen[key] then
				oppOpen[key] = true
				emit(Events.Name.SintoniaOpportunity, a, {
					with_user_id = b.UserId,
					element = elA,
					with_element = elB,
					pos = mid,
				})
			elseif not open then
				oppOpen[key] = nil
			end
		end
	end

	for key in pairs(oppOpen) do
		if not seen[key] then
			oppOpen[key] = nil -- o par deixou de existir (saiu, trocou de time)
		end
	end
end

-- -------------------------------------------------------------------- tick
function Sintonia.step(dt: number)
	local now = os.clock()

	oppAcc += dt
	if oppAcc >= OPP_SCAN then
		oppAcc = 0
		scanOpportunities()
	end

	for i = #recent, 1, -1 do
		if now - recent[i].t > S.window then
			table.remove(recent, i)
		end
	end
	for uid, p in pairs(proposals) do
		if now > p.expires then
			proposals[uid] = nil
		end
	end

	for i = #channels, 1, -1 do
		local ch = channels[i]
		if not isAlive(ch.a) or not isAlive(ch.b) then
			table.remove(channels, i)
			failChannel(ch, "died")
		elseif now >= ch.fireAt then
			table.remove(channels, i)
			releaseChannel(ch)
			fireChannel(ch)
		end
	end
end

--- FIM DE PARTIDA (costura do coordenador, 18/08): zera TODO o estado social e
--- de cooldown. Sem isto, o cooldown compartilhado de 24 s atravessava para a
--- partida seguinte e quem canalizou no fim entrava travado na próxima — estado
--- de uma partida contaminando a medição de outra. Chamado pelo Match no
--- `toLobby`. Não toca em nada do mundo: quem limpa arena é o Terrain.
function Sintonia.resetMatch()
	table.clear(recent)
	table.clear(channels)
	table.clear(proposals)
	table.clear(readyAt)
	table.clear(busy)
	table.clear(pingReadyAt)
end

function Sintonia.onPlayerRemoving(player: Player)
	local uid = player.UserId
	proposals[uid] = nil
	readyAt[uid] = nil
	busy[uid] = nil
	pingReadyAt[uid] = nil
	for i = #recent, 1, -1 do
		if recent[i].player == player then
			table.remove(recent, i)
		end
	end
	-- os canais dele morrem no próximo step (isAlive falha sem Parent), mas
	-- soltar a referência aqui evita segurar o Player por um frame inteiro.
	for i = #channels, 1, -1 do
		local ch = channels[i]
		if ch.a == player or ch.b == player then
			table.remove(channels, i)
			failChannel(ch, "left")
		end
	end
end

-- ============================================================================
-- AUTOTESTE — cole no Command Bar do Studio (servidor):
--   require(game.ServerScriptService.Server.Sintonia).selfTest()
-- Cobre o que não dá para ver a olho: a matriz completa, a regra de "mesmo
-- elemento não funde", a de "inimigo não funde", a geometria da janela, a
-- conta do dano, a FUSÃO COM PARCEIRO BOT (débito 2) e a AUTORIA que a fusão
-- entrega ao terreno (R13 — sem ela o pilar do jogo some da pergunta V3).
-- ⚠ RODE NO LOBBY: o item 3.1 monta e limpa o Teams DE VERDADE — no meio de uma
-- partida isto apagaria os duos e desligaria a Sintonia até a próxima.
-- ============================================================================
function Sintonia.selfTest()
	-- 1) os 10 pares existem e TODOS têm receita de terreno (combo sem receita
	--    seria uma fusão que não muda o mundo — quebra o pilar do GDD §9)
	local pairs10 = 0
	for _, a in ipairs(Elements.Order) do
		for _, b in ipairs(Elements.Order) do
			local combo = Elements.getCombo(a, b)
			if a == b then
				assert(combo == nil, "mesmo elemento NÃO pode fundir: " .. a)
			else
				assert(combo, "par sem combo na matriz: " .. a .. "+" .. b)
				local r = EFFECTS[(combo :: Elements.Combo).key]
				assert(r and #r.apply > 0, "combo sem reação de terreno: " .. (combo :: Elements.Combo).key)
			end
		end
	end
	for key in pairs(Elements.Combos) do
		assert(EFFECTS[key], "receita faltando para " .. key)
		pairs10 += 1
	end
	assert(pairs10 == 10, "a matriz do GDD §9 tem 10 pares, achei " .. pairs10)

	-- 2) receitas que dependem da ORDEM da química do Terrain
	assert(#EFFECTS[K("Water", "Lightning")].apply == 1, "Eletrocussão não pode aplicar Água (congelaria a poça)")
	assert(EFFECTS[K("Water", "Lightning")].apply[1] == "Lightning", "Eletrocussão conduz pelo Raio")
	assert(EFFECTS[K("Fire", "Wind")].apply[1] == "Fire", "Tornado Flamejante incendeia (Vento apagaria)")
	assert(EFFECTS[K("Water", "Wind")].apply[1] == "Water", "Tempestade Torrencial tem de APAGAR fogo")

	-- 3) janela/área: a mesma conta do `epicenter`, sem estado de jogo
	local o = Vector3.new(0, 0, 0)
	local near = Vector3.new(S.targetRadius - 1, 0, 0)
	local far = Vector3.new(S.targetRadius + 1, 0, 0)
	assert((o - near).Magnitude <= S.targetRadius, "dentro do raio deve fundir")
	assert((o - far).Magnitude > S.targetRadius, "fora do raio NÃO pode fundir")

	-- 3.1) P0 — SÓ ALIADOS FUNDEM. Os sujeitos são Humanoids de mentira porque o
	--      Teams aceita qualquer membro (jogador ou bot) e o `epicenter` só olha
	--      time e distância. Roda o funil de verdade, não uma cópia da regra.
	local dummies: { Humanoid } = {}
	for _ = 1, 4 do
		table.insert(dummies, Instance.new("Humanoid"))
	end
	Teams.build(dummies :: any, 2) -- duos: {1,2} aliados · {3,4} aliados
	assert(Teams.areAllies(dummies[1], dummies[2]), "1 e 2 são do mesmo duo")
	assert(not Teams.areAllies(dummies[1], dummies[3]), "1 e 3 são de times diferentes")
	assert(not Teams.areAllies(dummies[1], dummies[1]), "ninguém é aliado de si mesmo")

	local castA = { player = dummies[1] :: any, element = "Fire" :: Elements.Id, pos = o, t = os.clock() }
	assert(epicenter(castA, dummies[2] :: any, near) ~= nil, "ALIADOS colados no alvo TÊM de fundir")
	assert(epicenter(castA, dummies[3] :: any, near) == nil, "INIMIGOS NÃO FUNDEM — nem colados no mesmo alvo")
	-- (o caso "aliado longe" cai no pacto de ping, que precisa de UserId real —
	-- a regra de distância já está provada no item 3 acima)

	Teams.clear()
	assert(epicenter(castA, dummies[2] :: any, near) == nil, "sem times montados, nada funde (lobby)")
	for _, d in ipairs(dummies) do
		d:Destroy()
	end

	-- 3.2) ORÇAMENTO DE CC (P1 da auditoria): com TTK de ~3 s, nenhuma receita
	--      pode PEDIR atordoamento fora da janela 0,6–0,8 s. O Combat clampa de
	--      qualquer jeito; isto pega a intenção errada aqui, onde ela nasce.
	local withStun = 0
	for key, r in pairs(EFFECTS) do
		if r.stun then
			withStun += 1
			assert(r.stun >= 0.6 and r.stun <= 0.8, "atordoamento fora da janela 0,6–0,8 s em " .. key)
		end
		if r.reveal then
			assert(r.reveal >= 1.5 and r.reveal <= 2.0, "Reveal fora da janela 1,5–2,0 s em " .. key)
		end
		if r.slow then
			assert(r.slowMult and r.slowMult > 0.3 and r.slowMult < 1, "lentidão sem multiplicador sadio em " .. key)
		end
	end
	assert(withStun == 2, "só Eletrocussão e Cristais Carregados atordoam, achei " .. withStun)
	assert(EFFECTS[K("Water", "Lightning")].stun, "Eletrocussão TEM de atordoar (promessa da tabela do GDD §9)")
	assert(not EFFECTS[K("Water", "Earth")].slow, "Lamaçal já é lento pelo TERRENO: não duplicar no status")

	-- 3.3) PACTO DO PING: só o PRIMEIRO aceite sela. Sem isto, `AcceptSintonia`
	--      (que não tem cooldown) renovava o TTL a cada chamada e o alvo marcado
	--      — que é o que permite fundir de LONGE — virava permanente.
	local fakeProp: any = { player = nil, element = "Fire", pos = o, expires = os.clock() + PROPOSAL_TTL }
	assert(sealPact(fakeProp, 7, os.clock()), "o primeiro aceite tem de selar o pacto")
	local sealedUntil = fakeProp.expires
	assert(not sealPact(fakeProp, 8, os.clock() + 5), "PACTO JÁ SELADO não pode ser reselado (aceite spamado = marca eterna)")
	assert(fakeProp.expires == sealedUntil, "reaceitar não pode esticar o TTL do pacto")
	assert(fakeProp.withUserId == 7, "quem aceitou primeiro é quem fica no pacto")

	-- 3.4) PARCEIRO BOT (débito 2): o bot aliado responde ao ping e completa a
	--      fusão pelo funil REAL — pacto → magia do humano na marca → magia do
	--      bot → canalização, com o cooldown compartilhado cobrado no início.
	--      Os corpos ficam FORA da arena (1e5): a fusão de teste não toca
	--      terreno nem acerta ninguém.
	assert(evState, "3.4 precisa do init (rode depois do boot do servidor)")
	Sintonia.resetMatch()
	local function mkRig(name: string, pos: Vector3): (Model, Humanoid)
		local m = Instance.new("Model")
		m.Name = name
		local root = Instance.new("Part")
		root.Name = "HumanoidRootPart"
		root.Anchored = true
		root.Position = pos
		root.Parent = m
		local h = Instance.new("Humanoid")
		h.Parent = m
		m.Parent = Workspace
		return m, h
	end
	local FAR = Vector3.new(1e5, Grid.FLOOR_Y + 3, 1e5)
	local rigA, humA = mkRig("SintoniaTesteA", FAR)
	local rigB, humB = mkRig("SintoniaTesteB", FAR + Vector3.new(4, 0, 0))
	Teams.build({ humA :: any, humB :: any }, 2) -- dupla: "humano" A + bot B

	local uidA, uidB = uidOf(humA), uidOf(humB)
	assert(uidB < 0 and uidOf(humB) == uidB and uidA ~= uidB, "uid de bot é negativo, estável e único")

	-- o elemento do bot vem do registro que o Bots já faz no Combat
	combat.registerTarget(humB, "Water")

	-- ping de A → aceite do BOT pelo MESMO accept (mesmas validações)
	proposals[uidA] = {
		player = humA,
		element = "Fire" :: Elements.Id,
		pos = flat(FAR),
		expires = os.clock() + PROPOSAL_TTL,
		withUserId = nil,
	}
	Sintonia.accept(humB, uidA)
	assert(proposals[uidA].withUserId == uidB, "bot aliado TEM de conseguir aceitar o Ping de Sintonia")

	-- magia do humano NA MARCA → o bot responde e a canalização abre
	Sintonia.registerCast(humA, "Fire", flat(FAR))
	assert(#channels == 1, "magia na marca do pacto TEM de abrir canalização com o parceiro bot")
	assert(busy[uidA] == true and busy[uidB] == true, "os dois participantes ficam ocupados canalizando")
	assert(onCooldown(humA) and onCooldown(humB), "cooldown compartilhado cobrado NO INÍCIO, nos dois (GDD §9)")

	-- canalização INTERROMPÍVEL: bot morre no meio → failed.
	-- ANTI-GRIEFING G1 (R9): quem CAIU continua pagando o cooldown compartilhado;
	-- quem ficou DE PÉ é reembolsado. É o que tira do parceiro hostil (ou do que
	-- só deslogou) o poder de deixar o outro 24 s sem o pilar do jogo.
	humB.Health = 0
	Sintonia.step(0)
	assert(#channels == 0, "bot morto no meio INTERROMPE a canalização (GDD §9)")
	assert(not onCooldown(humA), "G1: quem continuou DE PÉ não paga a canalização que o parceiro não terminou")
	assert(onCooldown(humB), "G1: quem CAIU no meio continua pagando — é o que mantém o anti-spam do GDD §9")
	assert(not busy[uidA] and not busy[uidB], "canal interrompido solta os dois")

	-- fusão completa: revive o bot, zera o cooldown e deixa o canal DISPARAR
	humB.Health = 100
	table.clear(readyAt)
	Sintonia.registerCast(humA, "Fire", flat(FAR))
	assert(#channels == 1, "com o pacto ainda de pé, a dupla com bot funde de novo")
	channels[1].fireAt = 0 -- adianta o relógio do canal: dispara neste step
	Sintonia.step(0)
	assert(#channels == 0 and not busy[uidA] and not busy[uidB], "fusão com parceiro BOT dispara e libera os dois")
	-- o reembolso do G1 vale SÓ para canalização interrompida: o combo que SAI
	-- continua cobrando os dois, senão o limitador anti-spam do GDD §9 cai junto
	assert(onCooldown(humA) and onCooldown(humB), "combo DISPARADO cobra os DOIS (custo do GDD §9 intacto)")

	combat.unregisterTarget(humB)
	Sintonia.resetMatch()
	Teams.clear()
	rigA:Destroy()
	rigB:Destroy()

	-- 3.5) AUTORIA DA FUSÃO NO TERRENO (R13 — pergunta V3). Roda o `fireChannel`
	--      DE VERDADE com o terreno ESPIONADO (nada é aplicado no mundo) e no
	--      epicentro do mapa: fora da grade o `Grid.inBounds` faria o bloco
	--      inteiro ser pulado e o teste passaria por vacuidade.
	--      Combo escolhido: Cristais Carregados — é o único que aplica DOIS
	--      elementos SEM empurrão (com `push` a fusão de teste sacudiria quem
	--      estivesse perto da origem).
	local seenApply: { { element: string, author: any } } = {}
	local prevTerrain, prevCombat = terrain, combat
	terrain = {
		applyElement = function(_cx: number, _cy: number, element: string, _r: number, _s: boolean, author: any)
			table.insert(seenApply, { element = element, author = author })
		end,
	}
	combat = nil -- a fusão de teste não machuca, não atordoa e não drena ninguém
	local spyA, spyB = Instance.new("Humanoid"), Instance.new("Humanoid")
	local fakeCh: any = {
		a = spyA,
		b = spyB,
		elA = "Earth" :: Elements.Id,
		elB = "Lightning" :: Elements.Id,
		combo = Elements.getCombo("Earth", "Lightning"),
		pos = Vector3.new(0, COMBO_Y, 0),
		fireAt = 0,
	}
	local okFire, errFire = pcall(fireChannel, fakeCh)
	terrain, combat = prevTerrain, prevCombat
	assert(okFire, "fireChannel explodiu no autoteste: " .. tostring(errFire))
	assert(Grid.inBounds(Grid.worldToCell(fakeCh.pos)), "o epicentro do teste TEM de cair na grade (senão o teste é vazio)")
	assert(#seenApply == 2, "Cristais Carregados aplica DOIS elementos no terreno, vi " .. #seenApply)
	--- UMA asserção só, de propósito: um `author ~= spyB` em seguida NUNCA
	--- conseguiria ficar vermelho (a igualdade abaixo já pega qualquer autor
	--- errado, spyB inclusive) — seria cobertura de mentira.
	for _, call in ipairs(seenApply) do
		assert(
			call.author == spyA,
			"a fusão tem de chegar ao terreno com UM autor, o que conjurou primeiro (nil = sem `Cast`, e o pilar do jogo some da V3; "
				.. "o outro = desfecho dobrado para o mesmo ato). elemento: "
				.. call.element
		)
	end
	assert(
		seenApply[1].element == "Earth" and seenApply[2].element == "Lightning",
		"o carimbo é a FONTE REAL (o elemento da receita, na ordem), nunca o que o autor tem na mão"
	)
	spyA:Destroy()
	spyB:Destroy()

	-- 4) "mais forte que a soma das partes" (GDD §9)
	local a, b = Balance.elements.Fire.basic.dmg, Balance.elements.Lightning.basic.dmg
	assert((a + b) * S.dmgMult > a + b, "dmgMult tem de ser > 1")
	assert(S.channel > 0 and S.window > 0 and S.sharedCooldown > S.window, "limitadores do GDD §9 inconsistentes")

	print(string.format("[Sintonia] autoteste OK — %d combos, dmgMult %.2f", pairs10, S.dmgMult))
	return true
end

return Sintonia
