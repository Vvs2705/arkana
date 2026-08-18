--!strict
-- ============================================================================
-- ARKANA (Roblox) — i18n PT-BR / EN
-- Porte de src/core/strings.ts do protótipo 2D. REGRA: nenhum texto solto no
-- código de UI — tudo passa por Strings.t(). Chave nova = adicionar nos DOIS
-- idiomas. Chave desconhecida volta como a própria chave (fica visível no
-- playtest em vez de sumir).
-- Dono: RAIA D (UI/VFX) — bloco `grimoire.*` entrou pela raia do Grimório.
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

	-- Presença da dupla (raia D). O parceiro é a metade do jogo que o jogador
	-- não controla: nome, elemento e vida ficam na tela o tempo todo.
	["hud.partner"] = "PARCEIRO",
	["hud.partner.down"] = "CAÍDO",

	-- Sintonia (raia D). Convite em 1 toque — GDD §18.7.
	["sintonia.ask"] = "COMBO?",
	["sintonia.accept"] = "ACEITAR",
	["sintonia.decline"] = "×",
	["sintonia.proposed"] = "%s QUER SINTONIA",
	["sintonia.same"] = "TROQUE DE ELEMENTO",
	["sintonia.marked"] = "ALVO MARCADO",
	["sintonia.together"] = "CONJUREM JUNTOS",
	["sintonia.channeling"] = "CANALIZANDO SINTONIA",
	["sintonia.broken"] = "SINTONIA INTERROMPIDA",

	-- Onboarding / Campo de Provas (3 min). O roteiro INTEIRO da auditoria cabe
	-- em cinco frases: se o playtest precisar de uma sexta, o problema é de
	-- design, não de texto. Nada de "clique aqui para continuar".
	["tutorial.aim"] = "Arraste, mire, solte.",
	["tutorial.burn"] = "Queime a passagem.",
	["tutorial.freeze"] = "Congele o lago.",
	["tutorial.combo"] = "Pingue COMBO.",
	["tutorial.done"] = "Você já sabe o necessário.",
	["tutorial.enter"] = "ENTRAR NO ROYALE",
	--- respeita quem já sabe jogar: sair é um toque, sem confirmação
	["tutorial.skip"] = "PULAR",
	--- só aparece quando o elemento na mão não resolve o passo
	["tutorial.switch"] = "Troque para %s",
	--- indicador de antecipação: aparece ao ERRAR, some ao acertar
	["tutorial.lead"] = "MIRE À FRENTE",

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

	-- RETORNO CONTESTÁVEL (raia do revive). Cair não é o fim: vira espírito, e a
	-- dupla tem um ritual PÚBLICO para trazer de volta — o anúncio do canal é a
	-- denúncia que dá ao inimigo a chance de contestar.
	["announce.revive.spirit"] = "Você virou espírito. Seu parceiro pode trazê-lo de volta num Círculo de Invocação.",
	["announce.revive.carrying"] = "Essência recolhida — leve até um Círculo de Invocação.",
	["announce.revive.channel"] = "Um ritual de invocação começou!",
	["announce.revive.done"] = "%s retornou com o kit base.",
	--- o arrepio é CONTRA-JOGADA: quem é visto pelo espírito sabe que foi visto
	["announce.spirit.chill"] = "Um arrepio percorre sua espinha — você foi visto.",

	-- ------------------------------------------------------------ GRIMÓRIO
	-- (GDD §18.3) Progressão por EXPERIMENTAÇÃO, não por poder: cada página é
	-- uma interação que o jogador DESCOBRIU sozinho. Regra de escrita: o título
	-- nomeia a jogada, a descrição diz o que ela FAZ pelo jogador — nunca o
	-- botão que se aperta. Página trancada não revela nada (é caça ao tesouro).
	["grimoire.title"] = "GRIMÓRIO DE DESCOBERTAS",
	["grimoire.open"] = "GRIMÓRIO",
	["grimoire.close"] = "FECHAR",
	["grimoire.count"] = "%d de %d páginas",
	["grimoire.new"] = "NOVA PÁGINA DO GRIMÓRIO",
	["grimoire.locked"] = "? ? ?",
	["grimoire.locked.hint"] = "Uma página em branco. Descubra jogando.",
	["grimoire.empty"] = "Nenhuma página ainda. Experimente.",

	["grimoire.page.frozen_bridge.t"] = "Ponte de Gelo",
	["grimoire.page.frozen_bridge.d"] = "Água congelada vira caminho: você atravessou o que era intransponível.",
	["grimoire.page.conduction.t"] = "Condução",
	["grimoire.page.conduction.d"] = "Raio na água percorre o lago inteiro — e quem estiver dentro sente.",
	["grimoire.page.douse.t"] = "Fogo Apagado",
	["grimoire.page.douse.d"] = "Água sobre as chamas devolve o terreno inteiro: apagar não carboniza.",
	["grimoire.page.earth_cover.t"] = "Muro de Pedra",
	["grimoire.page.earth_cover.d"] = "Terra ergue cobertura onde não havia nenhuma. Destrutível — e sua.",
	["grimoire.page.quagmire.t"] = "Lamaçal",
	["grimoire.page.quagmire.d"] = "Água em chão de terra: quem entrar vai sair devagar.",
	["grimoire.page.smoke_out.t"] = "Fumaçar o Esconderijo",
	["grimoire.page.smoke_out.d"] = "Grama alta queimada não esconde mais ninguém.",
	["grimoire.page.sintonia_done.t"] = "Sintonia Completa",
	["grimoire.page.sintonia_done.d"] = "Dois elementos, um feitiço só — mais forte que a soma das partes.",
	["grimoire.page.sintonia_broken.t"] = "Pacto Quebrado",
	["grimoire.page.sintonia_broken.d"] = "A canalização inimiga é um telegrama. Você leu e cortou.",
	["grimoire.page.revive.t"] = "Retorno Contestável",
	["grimoire.page.revive.d"] = "No Círculo de Invocação você trouxe seu parceiro de volta.",
	["grimoire.page.terrain_master.t"] = "O Mapa é Arma",
	["grimoire.page.terrain_master.d"] = "Venceu depois de dobrar o terreno três vezes na mesma prova.",
	["grimoire.page.red_zone.t"] = "No Vermelho",
	["grimoire.page.red_zone.d"] = "A tempestade arcana mordeu fundo e você continuou de pé.",
	["grimoire.page.shield_slayer.t"] = "Escudo Coroado",
	["grimoire.page.shield_slayer.d"] = "Derrubou alguém com o escudo já evoluído ao nível 3.",
}

local EN: { [string]: string } = {
	["hud.hp"] = "HEALTH",
	["hud.shield"] = "SHIELD",
	["hud.mana"] = "MANA",
	["hud.shield.level"] = "Lv %d",
	["hud.shield.max"] = "MAX",
	["hud.evolved"] = "SHIELD EVOLVED — Lv %d",
	["hud.seal"] = "ATTUNEMENT",

	["hud.partner"] = "PARTNER",
	["hud.partner.down"] = "DOWN",

	["sintonia.ask"] = "COMBO?",
	["sintonia.accept"] = "ACCEPT",
	["sintonia.decline"] = "×",
	["sintonia.proposed"] = "%s WANTS ATTUNEMENT",
	["sintonia.same"] = "SWITCH ELEMENT",
	["sintonia.marked"] = "TARGET MARKED",
	["sintonia.together"] = "CAST TOGETHER",
	["sintonia.channeling"] = "CHANNELING ATTUNEMENT",
	["sintonia.broken"] = "ATTUNEMENT BROKEN",

	["tutorial.aim"] = "Drag, aim, release.",
	["tutorial.burn"] = "Burn the passage.",
	["tutorial.freeze"] = "Freeze the lake.",
	["tutorial.combo"] = "Ping COMBO.",
	["tutorial.done"] = "You already know what you need.",
	["tutorial.enter"] = "ENTER THE ROYALE",
	["tutorial.skip"] = "SKIP",
	["tutorial.switch"] = "Switch to %s",
	["tutorial.lead"] = "LEAD THE TARGET",

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

	["announce.revive.spirit"] = "You are a spirit now. Your partner can bring you back at a Summoning Circle.",
	["announce.revive.carrying"] = "Essence collected — carry it to a Summoning Circle.",
	["announce.revive.channel"] = "A summoning ritual has begun!",
	["announce.revive.done"] = "%s came back with the base kit.",
	["announce.spirit.chill"] = "A chill runs down your spine — you have been seen.",

	-- GRIMOIRE (GDD §18.3) — see the PT block for the writing rule.
	["grimoire.title"] = "BOOK OF DISCOVERIES",
	["grimoire.open"] = "GRIMOIRE",
	["grimoire.close"] = "CLOSE",
	["grimoire.count"] = "%d of %d pages",
	["grimoire.new"] = "NEW GRIMOIRE PAGE",
	["grimoire.locked"] = "? ? ?",
	["grimoire.locked.hint"] = "A blank page. Discover it by playing.",
	["grimoire.empty"] = "No pages yet. Go experiment.",

	["grimoire.page.frozen_bridge.t"] = "Ice Bridge",
	["grimoire.page.frozen_bridge.d"] = "Frozen water becomes a road: you crossed what could not be crossed.",
	["grimoire.page.conduction.t"] = "Conduction",
	["grimoire.page.conduction.d"] = "Lightning in water runs the whole lake — everyone standing in it feels it.",
	["grimoire.page.douse.t"] = "Doused",
	["grimoire.page.douse.d"] = "Water over flame gives the ground back whole: dousing does not char.",
	["grimoire.page.earth_cover.t"] = "Stone Wall",
	["grimoire.page.earth_cover.d"] = "Earth raises cover where there was none. Destructible — and yours.",
	["grimoire.page.quagmire.t"] = "Quagmire",
	["grimoire.page.quagmire.d"] = "Water on bare dirt: whoever walks in walks out slowly.",
	["grimoire.page.smoke_out.t"] = "Smoked Out",
	["grimoire.page.smoke_out.d"] = "Burnt tall grass hides nobody any more.",
	["grimoire.page.sintonia_done.t"] = "Attunement Complete",
	["grimoire.page.sintonia_done.d"] = "Two elements, one spell — stronger than the sum of its parts.",
	["grimoire.page.sintonia_broken.t"] = "Broken Pact",
	["grimoire.page.sintonia_broken.d"] = "The enemy channel is a telegram. You read it and cut it.",
	["grimoire.page.revive.t"] = "Contested Return",
	["grimoire.page.revive.d"] = "At the Summoning Circle you brought your partner back.",
	["grimoire.page.terrain_master.t"] = "The Map is a Weapon",
	["grimoire.page.terrain_master.d"] = "Won after bending the terrain three times in a single trial.",
	["grimoire.page.red_zone.t"] = "In the Red",
	["grimoire.page.red_zone.d"] = "The arcane storm bit deep and you were still standing.",
	["grimoire.page.shield_slayer.t"] = "Crowned Shield",
	["grimoire.page.shield_slayer.d"] = "Took someone down with your shield already evolved to level 3.",
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
