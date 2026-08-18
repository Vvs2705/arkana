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

-- câmera em 3ª pessoa (padrão da plataforma; o jogador vê os cosméticos)
local camera = workspace.CurrentCamera
camera.FieldOfView = 70

Controls.init(player)
Hud.init(player)
Vfx.init(player)

RunService.RenderStepped:Connect(function(dt)
	Controls.update(dt)
	Hud.update(dt)
	Vfx.update(dt)
end)

print("[ARKANA] cliente pronto")
