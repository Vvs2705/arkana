--!strict
-- ============================================================================
-- ARKANA (Roblox) — CONTRATO DE REDE (RemoteEvents)
--
-- REGRA DE OURO DO MULTIPLAYER (docs/ROBLOX.md §3): o SERVIDOR É AUTORITATIVO.
-- O cliente envia INTENÇÃO ("mirei nesta direção", "quero conjurar agora").
-- O servidor decide dano, cooldown, mana, alcance e estado do terreno, e
-- VALIDA tudo. Nenhum remote aceita número de dano vindo do cliente — é a
-- base do anti-cheat e não é opcional no Campo de Provas.
-- Dono: COORDENADOR.
-- ============================================================================

local ReplicatedStorage = game:GetService("ReplicatedStorage")
local RunService = game:GetService("RunService")

local Net = {}

--- nomes dos remotes. C→S = intenção do cliente · S→C = fato decidido pelo servidor
Net.Names = {
	-- C→S (intenções — sempre validadas no servidor)
	CastSpell = "CastSpell", -- { kind: "basic"|"tactic", dir: Vector3 }
	SwitchElement = "SwitchElement", -- { id: Elements.Id }
	Dodge = "Dodge", -- { dir: Vector3 }
	--- postura (comandos básicos de BR): { sprint: boolean, crouch: boolean }.
	--- ROTEAMENTO: quem escuta este remote é o próprio Combat (em Combat.init),
	--- não o Main — o servidor precisa aplicar a postura dentro do mesmo cálculo
	--- de WalkSpeed. Está aqui para o `initServer` criá-lo cedo e para o contrato
	--- ficar visível; NÃO adicionar um segundo handler no Main (dupla escuta).
	Stance = "Stance",
	--- o cliente conta em que aparelho está rodando: { tier: "low"|"mid"|"high",
	--- touch: boolean }. É SÓ telemetria — nada de jogo depende disto, então
	--- mentir aqui não dá vantagem nenhuma. Sem este dado, "60 fps no aparelho
	--- alvo" vira sensação em vez de número (gate mobile da auditoria).
	DeviceInfo = "DeviceInfo",
	--- "arrepio" do espírito (GDD §18.6): o caído marca UMA posição para a dupla.
	--- Roteado dentro do Match (que é quem sabe quem é espírito), como a postura
	--- é roteada dentro do Combat. Validado lá: só espírito, uma vez, e a posição
	--- tem de estar perto do PRÓPRIO espírito — senão o morto vira radar do mapa.
	SpiritPing = "SpiritPing",
	--- fluxo do onboarding (Campo de Provas de 3 min): { step: string } C→S para
	--- registrar progresso/abandono por passo, e S→C para conduzir o roteiro.
	Tutorial = "Tutorial",
	--- PREFERÊNCIAS que atravessam o rejoin. C→S: { prefs: { [string]: any } }
	--- (o jogador mudou algo) · S→C: { prefs } no login. O servidor é quem
	--- persiste — o cliente não tem DataStore. Nada aqui decide jogo: são
	--- opções de conforto, então o pior que um cliente hostil faz é estragar as
	--- PRÓPRIAS opções. Mesmo assim o servidor valida chave e tipo contra a
	--- lista conhecida: chave livre vinda do cliente vira armazenamento grátis.
	Settings = "Settings",
	ProposeSintonia = "ProposeSintonia", -- { pos: Vector3 } (Ping de Sintonia, GDD §18.7)
	AcceptSintonia = "AcceptSintonia", -- { fromUserId: number }

	-- S→C (fatos — o cliente só apresenta)
	SpellSpawned = "SpellSpawned", -- projétil visual: { id, element, origin, dir, speed }
	SpellImpact = "SpellImpact", -- { id, element, pos, strong: boolean }
	StatsUpdate = "StatsUpdate", -- dono: { hp, shield, shieldLv, evo, mana, cooldowns }
	DamageNumber = "DamageNumber", -- { pos: Vector3, amount: number, crit: boolean }
	SintoniaState = "SintoniaState", -- { phase, comboKey?, pos?, byUserId, withUserId? }
	MatchState = "MatchState", -- { phase, zoneCenter, zoneRadius, alive, timeLeft }
	Announce = "Announce", -- { key: string, ... } — feedback textual (i18n no cliente)
}

local remotesFolder: Folder? = nil

local function ensureFolder(): Folder
	if remotesFolder and remotesFolder.Parent then
		return remotesFolder
	end
	if RunService:IsServer() then
		local existing = ReplicatedStorage:FindFirstChild("Remotes")
		if existing and existing:IsA("Folder") then
			remotesFolder = existing
		else
			local f = Instance.new("Folder")
			f.Name = "Remotes"
			f.Parent = ReplicatedStorage
			remotesFolder = f
		end
	else
		remotesFolder = ReplicatedStorage:WaitForChild("Remotes", 30) :: Folder
	end
	return remotesFolder :: Folder
end

--- Pega (servidor: cria se faltar) o RemoteEvent pelo nome.
function Net.event(name: string): RemoteEvent
	local folder = ensureFolder()
	local ev = folder:FindFirstChild(name)
	if ev and ev:IsA("RemoteEvent") then
		return ev
	end
	if RunService:IsServer() then
		local new = Instance.new("RemoteEvent")
		new.Name = name
		new.Parent = folder
		return new
	end
	return folder:WaitForChild(name, 30) :: RemoteEvent
end

--- Cria todos os remotes de uma vez (chamado no boot do servidor).
function Net.initServer()
	assert(RunService:IsServer(), "Net.initServer só roda no servidor")
	for _, name in pairs(Net.Names) do
		Net.event(name)
	end
end

-- ------------------------------------------------- validação de entrada (S)
--- Direção enviada pelo cliente NUNCA entra crua: normaliza, rejeita NaN/zero
--- e achata no plano do chão (o combate é top-down/3ª pessoa, não vertical).
function Net.sanitizeDirection(raw: any): Vector3?
	if typeof(raw) ~= "Vector3" then
		return nil
	end
	local flat = Vector3.new(raw.X, 0, raw.Z)
	local m = flat.Magnitude
	if m ~= m or m < 1e-3 or m == math.huge then
		return nil -- NaN, zero ou infinito
	end
	return flat.Unit
end

return Net
