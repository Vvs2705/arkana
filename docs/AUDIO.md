# AUDIO.md — Direção sonora e implementação do Arkana v0.1

> Fonte da verdade: `docs/GDD.md` seção 13 (Áudio e música) e 18.5 (Trilha
> sonora elemental reativa).

## 1. Direção (GDD seção 13)

- **Estilo:** híbrido orquestral-eletrônico ("épico moderno") — coral + cordas
  no tema do menu, percussão tribal + synth na partida, faixa ascendente na
  queda do castelo (v0.2+).
- **Meta futura — camadas dinâmicas:** música de exploração → adiciona
  percussão quando inimigos se aproximam → clímax no top 5.

## 2. O que o v0.1 entrega: síntese WebAudio 100% procedural

Decisão do protótipo: em vez de assets CC externos (a alternativa listada no
GDD 13), o v0.1 sintetiza **todo o áudio em runtime** com WebAudio, em
`src/core/audio.ts` — **zero assets externos**, zero pendência de licença.

### SFX — síntese subtrativa simples
- Cada efeito é uma spec declarativa (tabela `SFX`): oscilador
  (`sine`/`square`/`sawtooth`) ou ruído branco filtrado (bandpass), frequência
  inicial, glide (rampa exponencial de pitch), duração, volume e categoria
  (`sfx`/`ui`).
- Cobertura: UI (click/hover/slider/back), conjuração dos 5 elementos (cada
  elemento com timbre próprio — ex.: Fogo = sawtooth descendente, Vento =
  ruído filtrado), impactos (`hit`, `hit_shield`), esquiva, troca de elemento,
  evolução do escudo, reações do terreno (`freeze`, `burn`, `wall`,
  `electrocute`), dano no player, morte de bot, pausa e confirmação do título.
- Mixagem: ganhos separados master/música/sfx/ui ligados aos sliders das
  Configurações (`Settings.onChange` aplica em tempo real).

### Música — gerativa por arpejos
- **Menu:** arpejo lento em Lá menor (8 passos, ~480ms por passo) com nota
  grave de reforço a cada 4 passos — épico contido.
- **Arena:** pulso grave (square) + lead pentatônico (triangle) a ~240ms por
  passo — mais tensa.
- Sem arquivos: as "partituras" são arrays de frequências; um `setInterval`
  agenda notas curtas com envelope exponencial.

### Restrições de navegador
- O `AudioContext` só libera som após gesto do usuário — `Audio.init()` arma
  listeners `pointerdown`/`keydown` (once) para o resume.

## 3. Roadmap

1. **v0.2 — Stems elementais (GDD 18.5):** cada elemento ganha um "stem"
   (camada instrumental); a música da luta mistura os stems dos elementos em
   uso (Fogo×Raio soa diferente de Água×Vento) — toda luta tem trilha única,
   assinatura sensorial do jogo. Tecnicamente: camadas de áudio sincronizadas,
   viável em Phaser/WebAudio.
2. **Camadas dinâmicas de intensidade** (exploração → combate → clímax),
   conforme GDD 13.
3. **Substituição por sound designer / compositor humano:** conforme
   `equipe-arkana.md` Parte 4, áudio original de qualidade não é substituível
   por agentes — o pipeline atual (specs declarativas + categorias de mixagem)
   foi desenhado para trocar a síntese por samples/faixas reais sem tocar no
   código das cenas.
4. Qualquer asset externo que entrar (mesmo temporário) deve ser registrado em
   `docs/CREDITS.md` com licença — regra do GDD desde o dia 1.
