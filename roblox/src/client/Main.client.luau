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
	for _, m in ipairs(extras) do
		if m.update then
			m.update(dt)
		end
	end
end)

print("[ARKANA] cliente pronto")
