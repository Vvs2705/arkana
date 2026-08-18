--!strict
-- ============================================================================
-- ARKANA (Roblox) — BOOTSTRAP DO SERVIDOR (autoridade da partida)
-- Dono: COORDENADOR. As raias preenchem os módulos; esta fiação é fixa.
--
-- Ordem de boot: Net → Arena (layout) → Terrain (mundo + química) →
-- Combat (magias/dano) → [R2 Sintonia] → [R3 Match/Bots] → loop.
-- ============================================================================

local ReplicatedStorage = game:GetService("ReplicatedStorage")
local RunService = game:GetService("RunService")
local Players = game:GetService("Players")
local ServerScriptService = game:GetService("ServerScriptService")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Net = require(Shared:WaitForChild("Net"))
local Balance = require(Shared:WaitForChild("Balance"))

local ServerModules = ServerScriptService:WaitForChild("Server")
local Arena = require(ServerModules:WaitForChild("Arena"))
local Terrain = require(ServerModules:WaitForChild("Terrain"))
local Combat = require(ServerModules:WaitForChild("Combat"))
local Sintonia = require(ServerModules:WaitForChild("Sintonia"))
local Match = require(ServerModules:WaitForChild("Match"))

Net.initServer()

-- mundo: layout imutável → grade de células vivas
local layout = Arena.generate()
Terrain.build(layout)
Combat.init(Terrain)
Sintonia.init(Terrain, Combat)
Match.init(Terrain, Combat)

-- ONBOARDING (Campo de Provas de 3 min). Opcional e protegido: se o módulo
-- faltar, o jogador simplesmente entra direto na partida.
local okTutorial, Tutorial = pcall(require, ServerModules:WaitForChild("Tutorial") :: ModuleScript)
if not okTutorial then
	warn("[ARKANA] tutorial indisponível: " .. tostring(Tutorial))
	Tutorial = nil
end
if Tutorial then
	-- o cliente avisa quando quer PULAR — é a única intenção do tutorial, e
	-- pular é sempre permitido (respeitar quem já sabe jogar é parte da spec)
	-- CONTRATO DE SAÍDA DO TUTORIAL. Este handler nasceu esperando
	-- `payload.skip`, mas o cliente manda `{ step = "skip" }` nos DOIS botões
	-- (PULAR e ENTRAR NO ROYALE). Resultado: `Tutorial.skip` nunca era chamado,
	-- a sessão não terminava, `Tutorial.active` ficava true para sempre e o
	-- lobby — que eu mesmo mandei esperar o tutorial — segurava a partida.
	-- NENHUMA PARTIDA COMEÇAVA, em nenhum servidor. Achado na revisão cruzada.
	-- Agora aceita as duas formas; `skip` é idempotente, escuta dupla é inócua.
	Net.event(Net.Names.Tutorial).OnServerEvent:Connect(function(player, payload)
		if typeof(payload) ~= "table" then
			return
		end
		local p = payload :: any
		if p.skip == true or p.step == "skip" or p.step == "enter" or p.step == "done" then
			pcall(Tutorial.skip, player)
		end
	end)
end

-- Telemetria por último: observa todos os outros, não é observada por ninguém.
-- Protegida por pcall — se o coletor quebrar, a partida continua de pé.
local okTelemetry, Telemetry = pcall(require, ServerModules:WaitForChild("Telemetry") :: ModuleScript)
if okTelemetry and Telemetry and Telemetry.init then
	pcall(Telemetry.init, Terrain, Combat, Match)
else
	warn("[ARKANA] telemetria indisponível — o jogo segue, os dados não")
end

-- ------------------------------------------------------------- intenções C→S
-- Todo handler abaixo trata o payload como HOSTIL até validar (Net.sanitize*).
Net.event(Net.Names.CastSpell).OnServerEvent:Connect(function(player, payload)
	if typeof(payload) ~= "table" then
		return
	end
	local kind = (payload :: any).kind
	if kind ~= "basic" and kind ~= "tactic" then
		return
	end
	local dir = Net.sanitizeDirection((payload :: any).dir)
	if not dir then
		return
	end
	Combat.handleCast(player, kind, dir)
end)

Net.event(Net.Names.SwitchElement).OnServerEvent:Connect(function(player, payload)
	if typeof(payload) ~= "table" then
		return
	end
	Combat.handleSwitch(player, (payload :: any).id)
end)

Net.event(Net.Names.Dodge).OnServerEvent:Connect(function(player, payload)
	local dir = if typeof(payload) == "table" then Net.sanitizeDirection((payload :: any).dir) else nil
	Combat.handleDodge(player, dir)
end)

Net.event(Net.Names.ProposeSintonia).OnServerEvent:Connect(function(player, payload)
	if typeof(payload) ~= "table" then
		return
	end
	Sintonia.propose(player, (payload :: any).pos)
end)

Net.event(Net.Names.AcceptSintonia).OnServerEvent:Connect(function(player, payload)
	if typeof(payload) ~= "table" then
		return
	end
	Sintonia.accept(player, (payload :: any).fromUserId)
end)

-- ------------------------------------------------------------------ jogadores
--- Presença do personagem (escala + silhueta de mago). Opcional e protegido:
--- se o módulo faltar ou quebrar, o jogador ainda nasce jogável.
local avatarModule: any = nil
local avatarLoaded = false
local function avatarSetup(player: Player, character: Model, humanoid: Humanoid)
	if not avatarLoaded then
		avatarLoaded = true
		local mod = ServerModules:FindFirstChild("Avatar")
		if mod and mod:IsA("ModuleScript") then
			local ok, m = pcall(require, mod)
			avatarModule = ok and m or nil
			if not ok then
				warn("[ARKANA] Avatar falhou: " .. tostring(m))
			end
		end
	end
	if avatarModule and avatarModule.setup then
		pcall(avatarModule.setup, player, character, humanoid)
	end
end

local function onCharacter(player: Player, character: Model)
	local humanoid = character:WaitForChild("Humanoid") :: Humanoid
	humanoid.WalkSpeed = Balance.player.speed
	-- COMANDO BÁSICO DE BR: o pulo precisa existir. O Roblox moderno ignora
	-- JumpPower quando UseJumpPower está desligado — sem esta linha o valor de
	-- Balance não vale nada. (O Espaço chegou a ser "engolido" pela esquiva no
	-- primeiro build; reprovado no teste do Diretor em 18/08.)
	humanoid.UseJumpPower = true
	humanoid.JumpPower = Balance.player.jumpPower
	humanoid.MaxHealth = Balance.player.hp
	humanoid.Health = Balance.player.hp
	avatarSetup(player, character, humanoid)
	Combat.onCharacterAdded(player, character, humanoid)
	-- Campo de Provas no PRIMEIRO corpo do jogador: é a primeira coisa que
	-- alguém de fora vê. O módulo devolve false para quem já passou na sessão.
	if Tutorial then
		task.defer(function()
			pcall(Tutorial.start, player)
		end)
	end
end

local function onPlayer(player: Player)
	Combat.onPlayerAdded(player)
	player.CharacterAdded:Connect(function(char)
		onCharacter(player, char)
	end)
	if player.Character then
		onCharacter(player, player.Character)
	end
end

Players.PlayerAdded:Connect(onPlayer)
for _, p in ipairs(Players:GetPlayers()) do
	onPlayer(p)
end

Players.PlayerRemoving:Connect(function(player)
	Combat.onPlayerRemoving(player)
	Sintonia.onPlayerRemoving(player)
	if Tutorial and Tutorial.onPlayerRemoving then
		pcall(Tutorial.onPlayerRemoving, player)
	end
	Match.onPlayerRemoving(player)
end)

-- ---------------------------------------------------------------- loop mestre
RunService.Heartbeat:Connect(function(dt)
	Terrain.step(dt)
	Combat.step(dt)
	Sintonia.step(dt)
	Match.step(dt)
	if Tutorial then
		Tutorial.step(dt)
	end
end)

print("[ARKANA] servidor pronto — Campo de Provas")
