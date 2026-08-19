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
--- schema de telemetria: só para SANEAR o modo de entrada do disparo (drag/tap/
--- key). Nada aqui decide jogo — ver o handler de CastSpell.
local Events = require(Shared:WaitForChild("Events"))

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

-- ONBOARDING (Campo de Provas de 3 min) — OPCIONAL DE VERDADE (R11). Opcional e
-- protegido em dois sentidos: se o módulo faltar, o jogador entra direto na
-- partida; e mesmo com o módulo presente ninguém é EMPURRADO para dentro do
-- Campo de Provas — o servidor faz um convite e a sessão só abre se o jogador
-- aceitar (ver `Tutorial.offer`).
--
-- O REMOTE DO TUTORIAL NÃO É MAIS ROTEADO AQUI. Este handler existia em
-- paralelo com o do próprio módulo, e foram esses DOIS contratos em desacordo
-- (aqui `payload.skip`, no cliente `{ step = "skip" }`) que travaram o servidor
-- inteiro em 18/08: ninguém saía do onboarding e o lobby não começava partida
-- nenhuma. Agora o protocolo tem UM dono — `Tutorial.wire`, ao lado da tabela
-- de intenções que o autoteste dos dois lados interroga.
local okTutorial, Tutorial = pcall(require, ServerModules:WaitForChild("Tutorial") :: ModuleScript)
if not okTutorial then
	warn("[ARKANA] tutorial indisponível: " .. tostring(Tutorial))
	Tutorial = nil
end

-- GRIMÓRIO DE DESCOBERTAS (GDD §18.3) — progressão por experimentação.
-- Opcional e protegido: sem ele, o jogo segue; só as páginas não acendem.
local okGrimoire, Grimoire = pcall(require, ServerModules:WaitForChild("Grimoire") :: ModuleScript)
if not okGrimoire then
	warn("[ARKANA] grimório indisponível: " .. tostring(Grimoire))
	Grimoire = nil
end
if Grimoire and Grimoire.init then
	pcall(Grimoire.init, Terrain, Combat)
end

-- PREFERÊNCIAS que atravessam o rejoin (R10). Opcional e protegido como o
-- Grimório: sem ele o jogador entra com os padrões e joga — que é exatamente o
-- comportamento de antes desta fase. `Prefs.init` se auto-fia (PlayerAdded,
-- PlayerRemoving, BindToClose e quem já estava dentro), e as funções de ciclo de
-- vida são idempotentes: fiar aqui não faz carregar nem salvar duas vezes.
-- Nada aqui decide jogo — são opções de conforto do próprio jogador.
local okPrefs, Prefs = pcall(require, ServerModules:WaitForChild("Prefs") :: ModuleScript)
if not okPrefs then
	warn("[ARKANA] preferências não persistem nesta sessão: " .. tostring(Prefs))
	Prefs = nil
end
if Prefs and Prefs.init then
	pcall(Prefs.init)
end

-- Telemetria por último: observa todos os outros, não é observada por ninguém.
-- Protegida por pcall — se o coletor quebrar, a partida continua de pé.
local okTelemetry, Telemetry = pcall(require, ServerModules:WaitForChild("Telemetry") :: ModuleScript)
if not okTelemetry then
	--- normaliza para nil (o pcall devolve a MENSAGEM de erro no lugar do
	--- módulo): quem consultar `Telemetry` embaixo pergunta uma coisa só.
	Telemetry = nil
end
if Telemetry and Telemetry.init then
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
	--- MODO DE ENTRADA (drag/tap/key) — a régua do requisito de celular
	--- (docs/ROBLOX.md §4: "mira e disparo em UM gesto"). Viaja de carona neste
	--- payload em vez de num remote novo: é telemetria PURA (mentir não dá
	--- vantagem) e um remote a mais seria superfície de ataque a mais.
	--- Categoria fora de Events.INPUT_KINDS vira nil — nunca "unknown", nunca
	--- coluna inventada por cliente hostil no relatório do Diretor.
	--- NÃO ENTRA EM DECISÃO NENHUMA: o Combat recebe como 4º argumento e o
	--- ignora até publicar a assinatura nova (ver relatório da raia B/R10).
	local input = Events.sanitizeInput((payload :: any).input)
	Combat.handleCast(player, kind, dir, input)
end)

--- RELATO DO APARELHO (Net.Names.DeviceInfo). O cliente MANDA isto desde a R5
--- (client/Device.luau) — o que nunca existiu foi este ouvinte, então o dado
--- chegava e era jogado fora, e `device_tier` saía "unknown" em TODO evento.
--- Sem ele não dá para saber quantos do playtest estavam num telefone, que é
--- exatamente o que o gate de celular precisa medir.
--- É telemetria pura: nada de jogo depende disto (docs/ROBLOX.md §3).
--- Payload HOSTIL até validar: `tier` só pode ser uma das três classes e
--- `touch` tem de ser booleano. A raia A valida de novo lá dentro — validar
--- dos dois lados é barato e este handler não pode confiar no que chega.
local deviceReported: { [Player]: boolean } = {}
Net.event(Net.Names.DeviceInfo).OnServerEvent:Connect(function(player, payload)
	if typeof(payload) ~= "table" or deviceReported[player] then
		return -- o contrato é UMA vez por sessão; repetição é ruído ou flood
	end
	local p = payload :: any
	if p.tier ~= "low" and p.tier ~= "mid" and p.tier ~= "high" then
		return
	end
	if typeof(p.touch) ~= "boolean" then
		return
	end
	deviceReported[player] = true
	--- TELEMETRIA NÃO DERRUBA O JOGO: o módulo pode ter faltado no boot e a API
	--- pode ainda não estar publicada. Nos dois casos o relato se perde e a
	--- partida continua idêntica.
	if Telemetry and Telemetry.setDevice then
		pcall(Telemetry.setDevice, player, p.tier, p.touch)
	end
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
	-- CONVITE (não início automático) no primeiro corpo do jogador. `offer` só
	-- PERGUNTA — e pergunta UMA vez por jogador por sessão de servidor, então
	-- este caminho, que roda em TODO corpo novo (todo respawn, toda largada de
	-- partida, que recarrega o personagem de todo mundo), não vira spam.
	if Tutorial then
		task.defer(function()
			pcall(Tutorial.offer, player)
		end)
	end
end

--- SESSÃO (R10) — o par que responde a pergunta V5 ("o combate segura o jogador
--- até a próxima prova?"). É o ÚNICO recorte de telemetria que atravessa
--- partida: `matchStart`/`matchEnd` zeram os agregados da prova, e a sessão
--- precisa sobreviver a isso — quem jogou três provas é a resposta.
--- POR QUE AQUI e não dentro do Telemetry: o coletor OBSERVA, não se inscreve em
--- ciclo de vida de jogador — é o Main que fia todo mundo. Protegido por pcall
--- pela regra de sempre: telemetria não decide jogo e não pode derrubar o boot.
local function noteSession(player: Player, event: string)
	if Telemetry and Telemetry.emit then
		pcall(Telemetry.emit, event, player)
	end
end

local function onPlayer(player: Player)
	noteSession(player, Events.Name.SessionStart)
	Combat.onPlayerAdded(player)
	if Grimoire and Grimoire.onPlayerAdded then
		pcall(Grimoire.onPlayerAdded, player)
	end
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
	--- ANTES de qualquer desmontagem: o `session_end` é enriquecido com o
	--- contexto de quem ainda ESTÁ na partida (fase da zona, vivos). Emitido
	--- depois de o Match soltar o jogador, "onde ele desistiu" — o número mais
	--- acionável da V5 — sairia vazio justamente na hora que interessa.
	noteSession(player, Events.Name.SessionEnd)
	deviceReported[player] = nil -- senão a tabela segura Player de quem já saiu
	Combat.onPlayerRemoving(player)
	Sintonia.onPlayerRemoving(player)
	if Tutorial and Tutorial.onPlayerRemoving then
		pcall(Tutorial.onPlayerRemoving, player)
	end
	if Grimoire and Grimoire.onPlayerRemoving then
		pcall(Grimoire.onPlayerRemoving, player)
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
	if Grimoire and Grimoire.step then
		Grimoire.step(dt)
	end
end)

print("[ARKANA] servidor pronto — Campo de Provas")
