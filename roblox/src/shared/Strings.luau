--!strict
-- ============================================================================
-- ARKANA (Roblox) — i18n PT-BR / EN
-- REGRA: nenhum texto solto no
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
	--- CONVITE (R11): o Campo de Provas deixou de ser empurrado na cara de quem
	--- entra. Duas saídas do mesmo tamanho — quem quer aprender aceita, quem quer
	--- jogar recusa e ninguém fica esperando por ele. O texto NÃO pode sugerir
	--- que treinar é o caminho certo: onboarding que envergonha quem pula é
	--- onboarding obrigatório com outro nome.
	["tutorial.offer"] = "QUER TREINAR ANTES?",
	--- NÃO diz "3 minutos": com lobby de 25 s o treino é CORTADO pela largada e
	--- retomado no lobby seguinte, então os 3 min são acumulados, não corridos.
	--- Prometer tempo corrido seria mentir na primeira tela que o jogador lê.
	["tutorial.offer.sub"] = "Aprenda no Campo de Provas",
	["tutorial.offer.yes"] = "TREINAR",
	["tutorial.offer.no"] = "JOGAR AGORA",
	--- fica no HUD depois da recusa: recusar não pode ser porta de mão única
	["tutorial.reopen"] = "TREINAR",

	-- ------------------------------------------------- CONTROLES DE TOQUE
	--- Estavam escritos direto no `ControlsTouch.luau` desde antes da R13 —
	--- únicos textos de UI fora do `Strings` no projeto inteiro, e ninguém tinha
	--- notado porque a auditoria de i18n olhava as telas, não os botões.
	--- ABREVIADOS de propósito: cabem num alvo de 48dp sem encolher a fonte, que
	--- é o que a régua de toque do GDD §19.3 exige.
	["controls.basic"] = "ATQ",
	["controls.tactic"] = "TÁT",
	["controls.dodge"] = "ESQ",
	["controls.jump"] = "PULAR",
	["controls.sprint"] = "CORRER",
	["controls.crouch"] = "AGACHAR",
	--- explica o CORTE: a prova começou por cima do treino. Sem esta frase o
	--- jogador só via o anúncio genérico de início e não entendia que o treino
	--- não foi perdido — ele retoma de onde parou no lobby seguinte.
	["tutorial.cut"] = "A prova começou — seu treino continua depois.",
	--- só aparece quando o elemento na mão não resolve o passo
	["tutorial.switch"] = "Troque para %s",
	--- indicador de antecipação: aparece ao ERRAR, some ao acertar
	["tutorial.lead"] = "MIRE À FRENTE",

	-- Anúncios do servidor (Net.Announce → { key = "...", ... })
	["announce.match.waiting"] = "Aguardando magos...",
	--- quem entra com a partida em andamento: sem este texto a pessoa fica
	--- olhando o mapa de cima sem entender que está assistindo (achado da raia
	--- de robustez para o alpha, 18/08)
	["announce.match.spectating"] = "Partida em andamento — você entra na próxima. Assista daqui.",
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

	-- ---------------------------------------------------------- PARTIDA (UI)
	-- Vieram do bloco `L10N` local do MatchUi (débito 2). Rótulos de HUD: caixa
	-- alta e curtos porque moram numa barra de 190px no alto à direita.
	["match.alive"] = "VIVOS",
	["match.teams"] = "TIMES",
	["match.waiting"] = "AGUARDANDO MAGOS",
	["match.starting"] = "COMEÇA EM",
	["match.hold"] = "A ZONA FECHA EM",
	["match.shrink"] = "ZONA FECHANDO",
	["match.phase"] = "FASE %d/%d",
	["match.outside"] = "FORA DA ZONA",
	["match.outside.sub"] = "corra para dentro do círculo",
	["match.seal.title"] = "SELO DO CAMPEÃO",
	["match.seal.nochampion"] = "NINGUÉM SOBROU",
	["match.kills"] = "ABATES",
	["match.damage"] = "DANO",
	["match.combos"] = "SINTONIAS",
	["match.duration"] = "DURAÇÃO",

	--- ESPECTADOR (R9). Duas situações MUITO diferentes que o jogador precisa
	--- distinguir sem ler manual: quem entrou com a prova em andamento (não
	--- perdeu nada, entra na próxima) e quem foi eliminado de vez (perdeu esta).
	--- Sem esta distinção o estranho que entra no minuto 3 de um alpha público
	--- acha que o jogo está quebrado — é a primeira impressão da maioria.
	["match.spectating"] = "ASSISTINDO",
	["match.spectating.late"] = "VOCÊ ENTRA NA PRÓXIMA",
	["match.spectating.out"] = "VOCÊ CAIU NESTA PROVA",
	["match.spectating.next"] = "PRÓXIMA PARTIDA",

	-- ------------------------------------------------- ACESSIBILIDADE (menu)
	-- Vieram do bloco `L10N` local do Settings (débito 2). O menu é o do
	-- `Accessibility.luau`; aqui só existe o que o jogador LÊ.
	["a11y.title"] = "ACESSIBILIDADE E CONFORTO",
	["a11y.uisize"] = "TAMANHO DA UI",
	["a11y.lefthanded"] = "HUD CANHOTO",
	["a11y.contrast"] = "ALTO CONTRASTE",
	["a11y.shake"] = "TREMOR DE TELA",
	["a11y.flash"] = "FLASHES",
	["a11y.vfx"] = "INTENSIDADE DE VFX",
	["a11y.blur"] = "DESFOQUE (BLUR/DOF)",
	["a11y.captions"] = "LEGENDAS DOS AVISOS",
	["a11y.readycue"] = "AVISO DE SINTONIA",
	["a11y.off"] = "NÃO",
	["a11y.on"] = "SIM",
	["a11y.half"] = "MÉDIO",
	["a11y.full"] = "CHEIO",
	--- uma chave por degrau (era uma lista dentro do L10N): chave solta é o que
	--- o teste de consistência PT/EN consegue conferir.
	["a11y.size.1"] = "P",
	["a11y.size.2"] = "M",
	["a11y.size.3"] = "G",
	["a11y.size.4"] = "GG",
	["a11y.hint"] = "O tamanho do texto e a vibração seguem as opções do seu Roblox.",

	--- ÁUDIO (R9). Duas barras e não quatro: `Master` é o botão que o jogador
	--- procura quando quer silêncio, e `World` é o que ele baixa quando o combate
	--- fica cansativo sem querer perder a interface. `Ui`/`Music` continuam nos
	--- padrões — knob que ninguém mexe é knob que só ocupa tela.
	["a11y.audio"] = "SOM",
	["a11y.audio.master"] = "VOLUME GERAL",
	["a11y.audio.world"] = "SONS DO JOGO",
	["a11y.audio.mute"] = "MUDO",
	--- DENSIDADE DE HUD (R11): o Diretor cobrou informação demais na tela. É
	--- preferência, não decisão nossa — competitivo quer tudo, quem está
	--- aprendendo quer o essencial.
	["a11y.hud"] = "INFORMAÇÃO NA TELA",
	["a11y.hud.min"] = "ESSENCIAL",
	["a11y.hud.full"] = "COMPLETO",

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
	["tutorial.offer"] = "WANT TO PRACTICE FIRST?",
	["tutorial.offer.sub"] = "Learn in the Proving Grounds",
	["tutorial.offer.yes"] = "PRACTICE",
	["tutorial.offer.no"] = "PLAY NOW",
	["tutorial.reopen"] = "PRACTICE",

	["controls.basic"] = "ATK",
	["controls.tactic"] = "TAC",
	["controls.dodge"] = "DASH",
	["controls.jump"] = "JUMP",
	["controls.sprint"] = "SPRINT",
	["controls.crouch"] = "CROUCH",
	["tutorial.cut"] = "The trial began — your practice continues later.",
	["tutorial.switch"] = "Switch to %s",
	["tutorial.lead"] = "LEAD THE TARGET",

	["announce.match.waiting"] = "Waiting for mages...",
	["announce.match.spectating"] = "Match in progress — you're in the next one. Watch from here.",
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

	["match.alive"] = "ALIVE",
	["match.teams"] = "TEAMS",
	["match.waiting"] = "WAITING FOR MAGES",
	["match.starting"] = "STARTS IN",
	["match.hold"] = "ZONE CLOSES IN",
	["match.shrink"] = "ZONE CLOSING",
	["match.phase"] = "PHASE %d/%d",
	["match.outside"] = "OUTSIDE THE ZONE",
	["match.outside.sub"] = "run into the circle",
	["match.seal.title"] = "CHAMPION'S SEAL",
	["match.seal.nochampion"] = "NO ONE LEFT",
	["match.kills"] = "KILLS",
	["match.damage"] = "DAMAGE",
	["match.combos"] = "ATTUNEMENTS",
	["match.duration"] = "TIME",

	["match.spectating"] = "SPECTATING",
	["match.spectating.late"] = "YOU'RE IN THE NEXT ONE",
	["match.spectating.out"] = "YOU'RE OUT OF THIS TRIAL",
	["match.spectating.next"] = "NEXT MATCH",

	["a11y.title"] = "ACCESSIBILITY & COMFORT",
	["a11y.uisize"] = "UI SIZE",
	["a11y.lefthanded"] = "LEFT-HANDED HUD",
	["a11y.contrast"] = "HIGH CONTRAST",
	["a11y.shake"] = "SCREEN SHAKE",
	["a11y.flash"] = "FLASHES",
	["a11y.vfx"] = "VFX INTENSITY",
	["a11y.blur"] = "BLUR / DOF",
	["a11y.captions"] = "CUE CAPTIONS",
	["a11y.readycue"] = "ATTUNEMENT CUE",
	["a11y.off"] = "OFF",
	["a11y.on"] = "ON",
	["a11y.half"] = "HALF",
	["a11y.full"] = "FULL",
	["a11y.size.1"] = "S",
	["a11y.size.2"] = "M",
	["a11y.size.3"] = "L",
	["a11y.size.4"] = "XL",
	["a11y.hint"] = "Text size and vibration follow your Roblox settings.",

	["a11y.audio"] = "SOUND",
	["a11y.audio.master"] = "MASTER VOLUME",
	["a11y.audio.world"] = "GAME SOUNDS",
	["a11y.audio.mute"] = "MUTED",
	["a11y.hud"] = "ON-SCREEN INFO",
	["a11y.hud.min"] = "ESSENTIAL",
	["a11y.hud.full"] = "FULL",

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

-- ============================================================================
-- AUTOTESTE — cole no Command Bar do Studio:
--   require(game.ReplicatedStorage.Shared.Strings).selfTest()
-- CONSISTÊNCIA PT/EN (débito 3). Chave que existe num idioma e falta no outro
-- não estoura nada: o `t()` devolve o fallback e o jogador do idioma faltante lê
-- a CHAVE CRUA na tela ("match.alive"). É o tipo de erro que só o playtest
-- encontra — e que custa este laço para barrar antes.
-- ============================================================================
function Strings.selfTest()
	local missing: { string } = {}
	for key in pairs(PT) do
		if EN[key] == nil then
			table.insert(missing, "EN falta " .. key)
		end
	end
	for key in pairs(EN) do
		if PT[key] == nil then
			table.insert(missing, "PT falta " .. key)
		end
	end
	table.sort(missing) -- `pairs` não tem ordem: sem isto a mensagem dança
	assert(#missing == 0, "i18n desemparelhado (o jogador veria a chave crua): " .. table.concat(missing, " · "))

	--- contrato do fallback: chave desconhecida VOLTA como a chave, em vez de
	--- sumir. É o que torna a falha acima visível no playtest.
	assert(Strings.t("chave.que.nao.existe") == "chave.que.nao.existe", "chave desconhecida tem de voltar crua")

	local n = 0
	for _ in pairs(PT) do
		n += 1
	end
	print(string.format("[Strings] autoteste OK — %d chaves emparelhadas PT/EN", n))
	return true
end

return Strings
