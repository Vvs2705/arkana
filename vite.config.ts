import { defineConfig, type Plugin } from 'vite';
import { readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';

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

export default defineConfig({
  base: './',
  plugins: [offlineFontsGate()],
  server: { port: 5173, host: '127.0.0.1' },
  build: { target: 'es2020', chunkSizeWarningLimit: 1600 },
});
