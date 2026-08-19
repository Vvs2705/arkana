// ============================================================================
// ARKANA v0.1 — i18n PT-BR/EN (dono: COORDENADOR)
// Uso: t('menu.play'). Worker A chama setLanguage() quando o idioma muda.
// Chaves novas: adicionar nos DOIS idiomas.
// ============================================================================
export type Lang = 'pt' | 'en';

let current: Lang = 'pt';

export function setLanguage(l: Lang): void { current = l; }
export function getLanguage(): Lang { return current; }

const PT: Record<string, string> = {
  // Título / menu
  'title.press': 'PRESSIONE ENTER',
  'menu.play': 'JOGAR',
  'menu.play.sub': 'Treino Solo — Campo de Provas',
  'menu.mages': 'MAGOS',
  'menu.arsenal': 'ARSENAL',
  'menu.settings': 'CONFIGURAÇÕES',
  'menu.credits': 'CRÉDITOS',
  'menu.quit': 'SAIR',
  'menu.soon': 'Em breve — v0.2',
  'menu.quit.web': 'Feche a aba do navegador para sair.',

  // Loading / splash
  'loading.label': 'CARREGANDO',
  'splash.presents': 'apresenta',

  // Dicas rotativas (ensinam a química do jogo — GDD seções 11/16.4)
  'tip.1': 'Dica: Água conduz Raio — cuidado onde pisa.',
  'tip.2': 'Dica: Fogo em grama alta revela quem estava escondido.',
  'tip.3': 'Dica: Congele o lago com Água para criar uma rota nova.',
  'tip.4': 'Dica: Vento sufoca chamas pequenas, mas ALIMENTA incêndios grandes.',
  'tip.5': 'Dica: Terra ergue um muro de pedra — cobertura instantânea.',
  'tip.6': 'Dica: Água no chão de terra vira lamaçal: lentidão severa.',
  'tip.7': 'Dica: O fogo se propaga de árvore em árvore. Queime com sabedoria.',
  'tip.8': 'Dica: A esquiva tem instantes de invulnerabilidade. Use no impacto.',
  'tip.9': 'Dica: Cause dano para evoluir seu Escudo de Magia (branco → dourado).',
  'tip.10': 'Dica: Pedra isola eletricidade. Terra é o counter do Raio.',

  // Configurações
  'settings.title': 'CONFIGURAÇÕES',
  'settings.tab.video': 'VÍDEO',
  'settings.tab.audio': 'ÁUDIO',
  'settings.tab.controls': 'CONTROLES',
  'settings.tab.game': 'JOGO',
  'settings.back': 'VOLTAR',
  'settings.restore': 'Restaurar padrão',
  'settings.fullscreen': 'Tela cheia',
  'settings.resolution': 'Resolução',
  'settings.quality': 'Qualidade',
  'settings.quality.low': 'Baixa',
  'settings.quality.medium': 'Média',
  'settings.quality.high': 'Alta',
  'settings.fps': 'Limite de FPS',
  'settings.fps.unlimited': 'Ilimitado',
  'settings.fps.note': 'Aplicado ao reiniciar o jogo',
  'settings.vsync': 'VSync',
  'settings.vsync.note': 'Controlado pelo navegador',
  'settings.showfps': 'Contador de FPS',
  'settings.vol.master': 'Volume Geral',
  'settings.vol.music': 'Música',
  'settings.vol.sfx': 'Efeitos',
  'settings.vol.ui': 'Interface',
  'settings.sens.mouse': 'Sensibilidade do mouse',
  'settings.sens.aim': 'Sensibilidade ao mirar',
  'settings.inverty': 'Inverter eixo Y',
  'settings.keys': 'Teclas',
  'settings.keys.press': 'Pressione uma tecla…',
  'settings.key.up': 'Mover para cima',
  'settings.key.down': 'Mover para baixo',
  'settings.key.left': 'Mover para a esquerda',
  'settings.key.right': 'Mover para a direita',
  'settings.key.dodge': 'Esquiva',
  'settings.key.element': 'Trocar elemento',
  'settings.key.supreme': 'Suprema (v0.2)',
  'settings.key.scoreboard': 'Placar (v0.2)',
  'settings.language': 'Idioma',
  'settings.colorblind': 'Modo daltonismo',
  'settings.colorblind.off': 'Desligado',
  'settings.colorblind.prot': 'Protanopia',
  'settings.colorblind.deut': 'Deuteranopia',
  'settings.colorblind.trit': 'Tritanopia',
  'settings.dmgnumbers': 'Números de dano',
  'settings.combotips': 'Dicas de combo',
  'settings.on': 'Ligado',
  'settings.off': 'Desligado',

  // Controles de toque (Fase 2 — GDD 19.3)
  'settings.touch': 'Controles de toque',
  'settings.touch.auto': 'Auto',
  'settings.touch.note': 'Aplicado ao entrar na arena',
  'settings.touch.gesture': 'Gesto de disparo',
  'settings.touch.gesture.unified': 'Unificado (arrasta e solta)',
  'settings.touch.gesture.legacy': 'Clássico (mira na tela)',
  'settings.touch.gesture.note': 'Voltar ao centro cancela o disparo',
  'settings.touch.scheme': 'Esquema de mira',
  'settings.touch.scheme.simple': 'Simples (assistida)',
  'settings.touch.scheme.adv': 'Avançado (manual)',
  'settings.touch.scale': 'Escala dos botões',
  'settings.touch.edit': 'Editar layout ›',
  'touch.edit.title': 'EDITAR LAYOUT',
  'touch.edit.hint': 'Arraste os controles para reposicionar',
  'touch.edit.done': 'CONCLUIR',

  // Pausa
  'pause.title': 'PAUSA',
  'pause.resume': 'RETOMAR',
  'pause.settings': 'CONFIGURAÇÕES',
  'pause.quit': 'ABANDONAR PARTIDA',

  // HUD / arena
  'hud.shield': 'ESCUDO',
  'hud.mana': 'MANA',
  'hud.evolved': 'ESCUDO EVOLUIU!',
  'hud.dead': 'VOCÊ CAIU',
  'hud.dead.sub': 'Pressione ENTER para voltar ao Campo de Provas',
  'hud.dummy': 'Dummy de Treino',

  // Magos (galeria v0.1 — 1 mago)
  'mages.title': 'MAGOS',
  'mages.evoker.name': 'EVOCADOR',
  'mages.evoker.title': 'O Aprendiz dos Cinco Caminhos',
  'mages.evoker.desc': 'Domina os 5 elementos no Campo de Provas. Ataque básico, magia tática e esquiva etérea — a base de todo mago de Arkana.',

  // Créditos
  'credits.title': 'CRÉDITOS',
  'credits.design': 'Direção e Game Design',
  'credits.design.name': 'Vinicius (Diretor de Jogo)',
  'credits.dev': 'Desenvolvimento',
  'credits.dev.name': 'Equipe Arkana — agentes Claude Code (ORQUESTRADOR)',
  'credits.tech': 'Tecnologia',
  'credits.tech.name': 'Phaser 3 · TypeScript · Vite',
  'credits.assets': 'Arte e áudio procedurais (sem assets externos) — ver docs/CREDITS.md',
};

const EN: Record<string, string> = {
  'title.press': 'PRESS ENTER',
  'menu.play': 'PLAY',
  'menu.play.sub': 'Solo Training — Proving Grounds',
  'menu.mages': 'MAGES',
  'menu.arsenal': 'ARSENAL',
  'menu.settings': 'SETTINGS',
  'menu.credits': 'CREDITS',
  'menu.quit': 'QUIT',
  'menu.soon': 'Coming soon — v0.2',
  'menu.quit.web': 'Close the browser tab to quit.',

  'loading.label': 'LOADING',
  'splash.presents': 'presents',

  'tip.1': 'Tip: Water conducts Lightning — watch your step.',
  'tip.2': 'Tip: Fire on tall grass reveals whoever was hiding.',
  'tip.3': 'Tip: Freeze the lake with Water to open a new route.',
  'tip.4': 'Tip: Wind smothers small flames but FEEDS big fires.',
  'tip.5': 'Tip: Earth raises a stone wall — instant cover.',
  'tip.6': 'Tip: Water on dirt becomes a mud pit: severe slow.',
  'tip.7': 'Tip: Fire spreads tree to tree. Burn wisely.',
  'tip.8': 'Tip: Your dodge has invulnerability frames. Time it.',
  'tip.9': 'Tip: Deal damage to evolve your Spell Shield (white → gold).',
  'tip.10': 'Tip: Stone insulates electricity. Earth counters Lightning.',

  'settings.title': 'SETTINGS',
  'settings.tab.video': 'VIDEO',
  'settings.tab.audio': 'AUDIO',
  'settings.tab.controls': 'CONTROLS',
  'settings.tab.game': 'GAME',
  'settings.back': 'BACK',
  'settings.restore': 'Restore defaults',
  'settings.fullscreen': 'Fullscreen',
  'settings.resolution': 'Resolution',
  'settings.quality': 'Quality',
  'settings.quality.low': 'Low',
  'settings.quality.medium': 'Medium',
  'settings.quality.high': 'High',
  'settings.fps': 'FPS limit',
  'settings.fps.unlimited': 'Unlimited',
  'settings.fps.note': 'Applied on restart',
  'settings.vsync': 'VSync',
  'settings.vsync.note': 'Controlled by the browser',
  'settings.showfps': 'FPS counter',
  'settings.vol.master': 'Master Volume',
  'settings.vol.music': 'Music',
  'settings.vol.sfx': 'Effects',
  'settings.vol.ui': 'Interface',
  'settings.sens.mouse': 'Mouse sensitivity',
  'settings.sens.aim': 'Aim sensitivity',
  'settings.inverty': 'Invert Y axis',
  'settings.keys': 'Key bindings',
  'settings.keys.press': 'Press a key…',
  'settings.key.up': 'Move up',
  'settings.key.down': 'Move down',
  'settings.key.left': 'Move left',
  'settings.key.right': 'Move right',
  'settings.key.dodge': 'Dodge',
  'settings.key.element': 'Switch element',
  'settings.key.supreme': 'Supreme (v0.2)',
  'settings.key.scoreboard': 'Scoreboard (v0.2)',
  'settings.language': 'Language',
  'settings.colorblind': 'Colorblind mode',
  'settings.colorblind.off': 'Off',
  'settings.colorblind.prot': 'Protanopia',
  'settings.colorblind.deut': 'Deuteranopia',
  'settings.colorblind.trit': 'Tritanopia',
  'settings.dmgnumbers': 'Damage numbers',
  'settings.combotips': 'Combo tips',
  'settings.on': 'On',
  'settings.off': 'Off',

  // Touch controls (Phase 2 — GDD 19.3)
  'settings.touch': 'Touch controls',
  'settings.touch.auto': 'Auto',
  'settings.touch.note': 'Applied when entering the arena',
  'settings.touch.gesture': 'Fire gesture',
  'settings.touch.gesture.unified': 'Unified (drag & release)',
  'settings.touch.gesture.legacy': 'Classic (aim on screen)',
  'settings.touch.gesture.note': 'Slide back to center to cancel',
  'settings.touch.scheme': 'Aim scheme',
  'settings.touch.scheme.simple': 'Simple (assisted)',
  'settings.touch.scheme.adv': 'Advanced (manual)',
  'settings.touch.scale': 'Button scale',
  'settings.touch.edit': 'Edit layout ›',
  'touch.edit.title': 'EDIT LAYOUT',
  'touch.edit.hint': 'Drag the controls to reposition them',
  'touch.edit.done': 'DONE',

  'pause.title': 'PAUSED',
  'pause.resume': 'RESUME',
  'pause.settings': 'SETTINGS',
  'pause.quit': 'LEAVE MATCH',

  'hud.shield': 'SHIELD',
  'hud.mana': 'MANA',
  'hud.evolved': 'SHIELD EVOLVED!',
  'hud.dead': 'YOU FELL',
  'hud.dead.sub': 'Press ENTER to return to the Proving Grounds',
  'hud.dummy': 'Training Dummy',

  'mages.title': 'MAGES',
  'mages.evoker.name': 'EVOKER',
  'mages.evoker.title': 'Apprentice of the Five Paths',
  'mages.evoker.desc': 'Masters the 5 elements in the Proving Grounds. Basic attack, tactical spell and ethereal dodge — the foundation of every Arkana mage.',

  'credits.title': 'CREDITS',
  'credits.design': 'Direction & Game Design',
  'credits.design.name': 'Vinicius (Game Director)',
  'credits.dev': 'Development',
  'credits.dev.name': 'Arkana Team — Claude Code agents (ORQUESTRADOR)',
  'credits.tech': 'Technology',
  'credits.tech.name': 'Phaser 3 · TypeScript · Vite',
  'credits.assets': 'Procedural art & audio (no external assets) — see docs/CREDITS.md',
};

const DICT: Record<Lang, Record<string, string>> = { pt: PT, en: EN };

/** traduz uma chave; devolve a própria chave se ausente (bug visível > invisível) */
export function t(key: string): string {
  return DICT[current][key] ?? DICT.pt[key] ?? key;
}

/** dicas rotativas do carregamento */
export function tips(): string[] {
  return Array.from({ length: 10 }, (_, i) => t(`tip.${i + 1}`));
}
