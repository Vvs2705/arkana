--!strict
-- ============================================================================
-- ARKANA (Roblox) — i18n PT-BR / EN
-- Porte de src/core/strings.ts do protótipo 2D. REGRA: nenhum texto solto no
-- código de UI — tudo passa por Strings.t(). Chave nova = adicionar nos DOIS
-- idiomas. Chave desconhecida volta como a própria chave (fica visível no
-- playtest em vez de sumir).
-- Dono: RAIA D (UI/VFX).
-- ============================================================================

local LocalizationService = game:GetService("LocalizationService")

export type Lang = "pt" | "en"

local Strings = {}

local PT: { [string]: string } = {
	-- HUD (raia D)
	["hud.hp"] = "VIDA",
	["hud.shield"] = "ESCUDO",
	["hud.mana"] = "MANA",
	["hud.shield.level"] = "Nv %d",
	["hud.shield.max"] = "MÁX",
	["hud.evolved"] = "ESCUDO EVOLUIU — Nv %d",
	["hud.seal"] = "SINTONIA",

	-- Anúncios do servidor (Net.Announce → { key = "...", ... })
	["announce.match.waiting"] = "Aguardando magos...",
	["announce.match.start"] = "A prova começou!",
	["announce.match.end"] = "Fim da prova",
	["announce.zone.closing"] = "A tempestade arcana se fecha",
	["announce.kill"] = "Você derrubou %s",
	["announce.death"] = "Você caiu",
	["announce.nomana"] = "Mana insuficiente",
	["announce.cooldown"] = "Ainda recarregando",
	["announce.element"] = "Elemento: %s",
	["announce.sintonia.ready"] = "Sintonia disponível",
	["announce.sintonia.proposed"] = "%s propôs Sintonia",
	["announce.sintonia.fired"] = "SINTONIA — %s",
}

local EN: { [string]: string } = {
	["hud.hp"] = "HEALTH",
	["hud.shield"] = "SHIELD",
	["hud.mana"] = "MANA",
	["hud.shield.level"] = "Lv %d",
	["hud.shield.max"] = "MAX",
	["hud.evolved"] = "SHIELD EVOLVED — Lv %d",
	["hud.seal"] = "ATTUNEMENT",

	["announce.match.waiting"] = "Waiting for mages...",
	["announce.match.start"] = "The trial has begun!",
	["announce.match.end"] = "Trial over",
	["announce.zone.closing"] = "The arcane storm is closing in",
	["announce.kill"] = "You took down %s",
	["announce.death"] = "You fell",
	["announce.nomana"] = "Not enough mana",
	["announce.cooldown"] = "Still recharging",
	["announce.element"] = "Element: %s",
	["announce.sintonia.ready"] = "Attunement available",
	["announce.sintonia.proposed"] = "%s proposed Attunement",
	["announce.sintonia.fired"] = "ATTUNEMENT — %s",
}

local tables: { [string]: { [string]: string } } = { pt = PT, en = EN }
local current: Lang = "pt"

function Strings.setLang(lang: Lang)
	if tables[lang] then
		current = lang
	end
end

function Strings.getLang(): Lang
	return current
end

--- Segue o idioma do próprio jogador (recurso nativo — sem tela de opção).
function Strings.followPlayerLocale()
	local ok, locale = pcall(function()
		return LocalizationService.RobloxLocaleId
	end)
	current = (ok and typeof(locale) == "string" and string.sub(locale, 1, 2) == "pt") and "pt" or "en"
end

--- t("hud.shield.level", 3) → "Nv 3". Chave ausente volta como a própria chave.
function Strings.t(key: string, ...): string
	local s = tables[current][key] or PT[key]
	if not s then
		return key
	end
	if select("#", ...) == 0 then
		return s
	end
	local ok, formatted = pcall(string.format, s, ...)
	return ok and formatted or s
end

return Strings
