--!strict
-- ============================================================================
-- ARKANA (Roblox) — BOOTSTRAP DO CLIENTE
-- Dono: COORDENADOR. O cliente NÃO decide nada de jogo: envia intenção,
-- apresenta o que o servidor decidiu (dano, morte, terreno) e cuida do feel
-- (câmera, controles, HUD, VFX).
-- ============================================================================

local ReplicatedStorage = game:GetService("ReplicatedStorage")
local RunService = game:GetService("RunService")
local Players = game:GetService("Players")

local player = Players.LocalPlayer
local scripts = script.Parent :: Instance

local Controls = require(scripts:WaitForChild("Controls"))
local Hud = require(scripts:WaitForChild("Hud"))
local Vfx = require(scripts:WaitForChild("Vfx"))

--- Telas opcionais (R2/R3). Carregadas por FindFirstChild + pcall: se um módulo
--- faltar ou quebrar, o jogo continua de pé em vez de travar o boot do cliente.
local function optional(name: string): any?
	local mod = scripts:FindFirstChild(name)
	if not mod or not mod:IsA("ModuleScript") then
		return nil
	end
	local ok, m = pcall(require, mod)
	if not ok then
		warn("[ARKANA] módulo opcional falhou: " .. name .. " — " .. tostring(m))
		return nil
	end
	return m
end

local extras = {}
for _, name in ipairs({ "Accessibility", "CameraRig", "Atmosphere", "Settings", "Device", "SintoniaUi", "MatchUi", "TutorialUi" }) do
	local m = optional(name)
	if m then
		table.insert(extras, m)
	end
end

-- câmera em 3ª pessoa (padrão da plataforma; o jogador vê os cosméticos)
local camera = workspace.CurrentCamera
camera.FieldOfView = 70

Controls.init(player)
Hud.init(player)
Vfx.init(player)
for _, m in ipairs(extras) do
	if m.init then
		pcall(m.init, player)
	end
end

RunService.RenderStepped:Connect(function(dt)
	Controls.update(dt)
	Hud.update(dt)
	Vfx.update(dt)
	--- O `init` dos opcionais já era protegido; o `update` não era. Um erro no
	--- meio do laço ABORTAVA o frame inteiro dali para a frente: quebrar a
	--- SintoniaUi congelava, calada, a MatchUi e a TutorialUi que vêm depois —
	--- e repetia 60x/s. Aqui o módulo que quebra SAI DE CENA (aviso uma vez) e
	--- os seguintes continuam desenhando. Percorre de trás para a frente para o
	--- `table.remove` não pular o vizinho.
	for i = #extras, 1, -1 do
		local m = extras[i]
		if m.update then
			local ok, err = pcall(m.update, dt)
			if not ok then
				table.remove(extras, i)
				warn("[ARKANA] módulo opcional saiu do laço após erro: " .. tostring(err))
			end
		end
	end
end)

print("[ARKANA] cliente pronto")
