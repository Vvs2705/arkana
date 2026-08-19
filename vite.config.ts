import { defineConfig, type Plugin } from 'vite';
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { GAME_WIDTH, TOUCH, minHitRadiusPx } from './src/core/config';

/** fontes que TÊM de estar dentro do dist (empacotadas de public/fonts) */
const FONT_FILES = [
  'cinzel-latin.woff2',
  'chakra-petch-400-latin.woff2',
  'chakra-petch-700-latin.woff2',
  'inter-latin.woff2',
];
/** hosts que o build NÃO pode voltar a referenciar */
const BANNED_HOSTS = ['fonts.googleapis.com', 'fonts.gstatic.com'];

/**
 * Portão de offline/privacidade (docs/ANDROID.md, Fase 2).
 * O jogo tem de abrir em modo avião com a tipografia correta, e a ficha de
 * Segurança de Dados da Play depende de o protótipo não fazer NENHUMA chamada
 * externa. Build que compila ainda buscando fonte na rede é falha calada —
 * então o build quebra aqui, não no aparelho do jogador.
 */
function offlineFontsGate(): Plugin {
  return {
    name: 'arkana-offline-fonts-gate',
    apply: 'build',
    closeBundle() {
      const walk = (dir: string): string[] =>
        readdirSync(dir, { withFileTypes: true }).flatMap((e) =>
          e.isDirectory() ? walk(join(dir, e.name)) : [join(dir, e.name)],
        );
      const files = walk('dist');

      for (const f of files.filter((n) => /\.(html|css|js|json)$/i.test(n))) {
        const src = readFileSync(f, 'utf8');
        for (const host of BANNED_HOSTS) {
          if (src.includes(host)) {
            throw new Error(`[offline] ${f} ainda referencia ${host} — o jogo não abre igual em modo avião.`);
          }
        }
      }

      for (const name of FONT_FILES) {
        const hit = files.find((f) => f.endsWith(name));
        if (!hit || statSync(hit).size < 1000) {
          throw new Error(`[offline] fonte ausente ou vazia no dist: ${name}`);
        }
      }

      console.log(`[offline] ok — ${FONT_FILES.length} fontes empacotadas, 0 referência a CDN de fonte`);
    },
  };
}

/**
 * Portão do alvo de toque (GDD §19.3, 48dp).
 *
 * Dois defeitos já chegaram ao aparelho do Diretor por este caminho e nenhum
 * dos dois aparece no desktop: (R12) o piso de 48dp virava 48px de canvas fixos
 * — metade do alvo num telefone; (R14) a conversão dp→px era calculada UMA vez
 * no build da cena, então girar o aparelho ou entrar em tela dividida deixava
 * todos os alvos errados no meio da partida. Sem framework de teste no 2D, o
 * build é onde a regressão pode ser barrada.
 *
 * 1. Roda a função REAL do jogo (a mesma que a TouchControls chama) em várias
 *    larguras de tela e exige que o alvo continue medindo TOUCH.minDp na tela.
 *    Quebra se alguém voltar a fixar o raio em px de canvas.
 * 2. Confere que a TouchControls reassina o RESIZE (e desassina) — sem isso a
 *    conversão certa é calculada uma vez e congela.
 */
function touchTargetGate(): Plugin {
  return {
    name: 'arkana-touch-target-gate',
    apply: 'build',
    buildStart() {
      // larguras CSS de canvas: telefone estreito → retrato → paisagem → 2K
      for (const dispW of [320, 360, 375, 412, 640, 720, 1080, 1280, 1920, 2560]) {
        const dp = minHitRadiusPx(dispW) * 2 * (dispW / GAME_WIDTH);
        if (Math.abs(dp - TOUCH.minDp) > 0.01) {
          throw new Error(
            `[toque] alvo mínimo = ${dp.toFixed(1)}dp numa tela de ${dispW}px CSS ` +
            `(GDD §19.3 exige ${TOUCH.minDp}dp) — a conversão dp→px parou de acompanhar a tela.`,
          );
        }
      }

      const src = readFileSync(join('src', 'ui', 'TouchControls.ts'), 'utf8');
      for (const hook of ['scale.on(Phaser.Scale.Events.RESIZE', 'scale.off(Phaser.Scale.Events.RESIZE']) {
        if (!src.includes(hook)) {
          throw new Error(
            `[toque] TouchControls.ts não tem "${hook}" — a tela muda durante a sessão ` +
            '(rotação, tela dividida, WebView) e os alvos ficariam no tamanho do primeiro frame.',
          );
        }
      }

      console.log(`[toque] ok — piso de ${TOUCH.minDp}dp em 10 larguras de tela, RESIZE assinado`);
    },
  };
}

export default defineConfig({
  base: './',
  plugins: [offlineFontsGate(), touchTargetGate()],
  server: { port: 5173, host: '127.0.0.1' },
  build: { target: 'es2020', chunkSizeWarningLimit: 1600 },
});
