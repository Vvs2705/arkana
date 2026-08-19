--!strict
-- ============================================================================
-- ARKANA (Roblox) — HARNESS DE PLAYTEST (raia 6.1: Analista de Dados)
-- Ferramenta do DIRETOR, não do jogo. Nada aqui roda sozinho: este módulo só
-- existe quando alguém o requer na Command Bar. O servidor nunca o carrega.
--
-- COMO USAR (Studio: aperte Play, abra a Command Bar e troque o contexto de
-- "Cliente" para "SERVIDOR" no seletor da barra — sem isso nada disto existe):
--
--   local P = require(game.ServerScriptService.Server.Playtest)
--
--   P.duo()          -- começa a partida AGORA, sem esperar o lobby, com os
--                       humanos que estiverem no servidor e ZERO bots. É o
--                       modo do teste de duplas: nenhum bot contamina o dado.
--   P.duo(3)         -- idem, mas com exatamente 3 bots (teste sozinho precisa
--                       de pelo menos 1 — com 0 bots e 1 humano a partida acaba
--                       na hora, porque só há um vivo).
--   P.stop()         -- ENCERRA a partida em andamento: fecha a janela da
--                       telemetria (o relatório sai sozinho) e cai na tela de
--                       fim, como uma vitória. É o que permite repetir uma
--                       condição sem reiniciar o Studio.
--   P.report()       -- imprime o relatório da telemetria AGORA (pode chamar no
--                       meio da partida; a coleta continua).
--   P.variant()      -- mostra qual coorte de Sintonia está ativa (A ou B).
--   P.variant("B")   -- explica como trocar (é edição de arquivo + reinício —
--                       ver a nota de honestidade no fim deste cabeçalho).
--   P.trace(true)    -- imprime cada evento de telemetria conforme acontece.
--   P.reset()        -- DESCARTA o que foi coletado, abre janela nova e devolve
--                       os valores de Balance.match que o `duo()` mexeu.
--
-- HISTÓRICO (R10): este cabeçalho dizia que o Match não expunha `forceStart` /
-- `forceEnd` e que por isso `duo()` precisava EMPURRAR o lobby mexendo em
-- `Balance.match`. A R8 criou as duas funções e anotou no Match que "era a
-- pendência declarada no cabeçalho do Playtest" — mas ninguém voltou aqui. O
-- resultado é que a ferramenta do Diretor passou duas fases mentindo que não
-- dava para encerrar uma partida, e mexendo em números de balanceamento para
-- fazer o que o Match já fazia direito. Agora são uma linha cada, como previsto,
-- e `Balance` não é mais tocado por este módulo.
--
-- NOTA DE HONESTIDADE (A/B da Sintonia): `Balance.sintonia` é RESOLVIDA no
-- require, a partir de `Balance.experiment.sintoniaVariant`, e a tabela de
-- variantes (`SINTONIA_VARIANTS`) é local do Balance — não é exportada. Trocar
-- de coorte em runtime exigiria repetir os números da variante B aqui dentro, o
-- que quebra a regra do projeto ("números viram dados; o código lê dados") e
-- deixaria dois lugares para rebalancear. Além disso, coorte trocada no meio da
-- sessão contamina as DUAS. Logo: variante se troca editando
-- `roblox/src/shared/Balance.luau` → `Balance.experiment.sintoniaVariant` e
-- rodando de novo. `P.variant()` só diz o que está valendo — e o relatório
-- carimba a variante no cabeçalho para o dado nunca ficar órfão da condição.
-- ============================================================================

local Players = game:GetService("Players")
local ReplicatedStorage = game:GetService("ReplicatedStorage")
local RunService = game:GetService("RunService")

local Shared = ReplicatedStorage:WaitForChild("Shared")
local Balance = require(Shared:WaitForChild("Balance"))

local Playtest = {}

local function sibling(name: string): any
	local mod = script.Parent and script.Parent:FindFirstChild(name)
	local ok, m = pcall(function()
		return require(mod :: ModuleScript)
	end)
	return if ok then m else nil
end

local function telemetry(): any
	return sibling("Telemetry")
end

local function match(): any
	return sibling("Match")
end

--- Começa a partida AGORA, sem esperar os 25 s de lobby.
--- `bots` = quantos bots entram (0 = só humanos, que é o teste de duplas).
function Playtest.duo(bots: number?): boolean
	local M = match()
	if not M or not M.forceStart then
		warn("[Playtest] Match indisponível — este módulo só existe com o servidor de pé")
		return false
	end
	local botCount = math.max(0, math.floor(tonumber(bots) or 0))
	local humans = #Players:GetPlayers()
	if humans == 0 then
		warn("[Playtest] ninguém no servidor — aperte Play antes")
		return false
	end
	if humans + botCount < 2 then
		warn("[Playtest] partida de 1 acaba sozinha: chame P.duo(1) ou entre com um segundo jogador")
	end
	if not M.forceStart(botCount) then
		-- a única recusa possível: já existe partida rodando. Dizer QUAL é o
		-- caminho vale mais do que um `false` mudo na Command Bar.
		warn("[Playtest] já há uma partida em andamento — chame P.stop() antes de começar outra")
		return false
	end
	-- NÃO abrimos a janela de telemetria aqui: o próprio Match chama
	-- `Telemetry.matchStart(matchId)` dentro do `startMatch`. Chamar de novo
	-- zeraria a coleta que acabou de abrir (e trocaria o id por um apelido).
	print(
		string.format(
			"[Playtest] partida forçada — %d humano(s) + %d bot(s), variante Sintonia %s",
			humans,
			botCount,
			tostring(Balance.experiment.sintoniaVariant)
		)
	)
	return true
end

--- Encerra a partida em andamento. O relatório da telemetria sai sozinho no
--- `matchEnd` — é o momento em que o Diretor lê.
function Playtest.stop(): boolean
	local M = match()
	if not M or not M.forceEnd then
		warn("[Playtest] Match indisponível")
		return false
	end
	if not M.forceEnd() then
		warn("[Playtest] não há partida em andamento para encerrar")
		return false
	end
	print("[Playtest] partida encerrada — o relatório saiu acima (matchEnd)")
	return true
end

--- Imprime o relatório da telemetria (a coleta segue aberta).
function Playtest.report(): boolean
	local tel = telemetry()
	if not tel or not tel.report then
		warn("[Playtest] telemetria indisponível")
		return false
	end
	local ok, text = pcall(tel.report)
	if not ok then
		warn("[Playtest] o relatório falhou: " .. tostring(text))
		return false
	end
	print(text)
	return true
end

--- Coorte de Sintonia ativa. Com argumento, ensina a trocar (ver NOTA DE
--- HONESTIDADE no cabeçalho: é edição de arquivo, não interruptor de runtime).
function Playtest.variant(want: string?): string
	local active = tostring(Balance.experiment.sintoniaVariant)
	local S = Balance.sintonia
	print(string.format("[Playtest] variante ATIVA: %s — canalização %.2fs, cooldown compartilhado %.1fs, dano x%.2f", active, S.channel, S.sharedCooldown, S.dmgMult))
	if typeof(want) == "string" and want ~= active then
		print("[Playtest] para rodar a coorte " .. want .. ":")
		print("   1) pare o Play;")
		print('   2) edite roblox/src/shared/Balance.luau → Balance.experiment.sintoniaVariant = "' .. want .. '";')
		print("   3) Play de novo e rode P.duo() — o relatório carimba a variante sozinho.")
		print("   (trocar em runtime repetiria os números da variante em outro arquivo e")
		print("    contaminaria as duas coortes na mesma sessão)")
	end
	return active
end

--- Traço ao vivo dos eventos (útil para ver se a instrumentação está disparando).
function Playtest.trace(on: boolean?): boolean
	local tel = telemetry()
	if not tel then
		return false
	end
	tel.VERBOSE = on ~= false
	print("[Playtest] traço de telemetria " .. (if tel.VERBOSE then "LIGADO" else "desligado"))
	return tel.VERBOSE
end

--- Descarta a coleta, abre janela nova e devolve o lobby ao que era.
function Playtest.reset(): boolean
	-- nada de Balance para devolver: desde a R10 o `duo()` chama
	-- `Match.forceStart` e este módulo não escreve mais em balanceamento.
	local tel = telemetry()
	if tel and tel.matchStart then
		pcall(tel.matchStart, "reset")
	end
	print("[Playtest] telemetria zerada (o que foi coletado FOI DESCARTADO)")
	return true
end

return Playtest
