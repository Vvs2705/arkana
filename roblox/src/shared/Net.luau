--!strict
-- ============================================================================
-- ARKANA (Roblox) — CONTRATO DE REDE (RemoteEvents)
--
-- REGRA DE OURO DO MULTIPLAYER (docs/ROBLOX.md §3): o SERVIDOR É AUTORITATIVO.
-- O cliente envia INTENÇÃO ("mirei nesta direção", "quero conjurar agora").
-- O servidor decide dano, cooldown, mana, alcance e estado do terreno, e
-- VALIDA tudo. Nenhum remote aceita número de dano vindo do cliente — é a
-- base do anti-cheat (EQUIPE.md 3.3) e não é opcional nem no protótipo.
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
