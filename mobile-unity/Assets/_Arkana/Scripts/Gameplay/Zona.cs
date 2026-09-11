using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// A TEMPESTADE ARCANA — o circulo que fecha. Sem ela o jogo e' deathmatch com cronometro.
    /// Ela nao e' cronometro disfarcado, e' COMPRESSOR: cada fase tem ESPERA (parada, proximo circulo ja'
    /// desenhado — "se mata rapido, avisa antes") e FECHA (a parede anda; quem ficou fora sangra, e o dano
    /// cresce por fase). Tudo sai do seed, calculado de uma vez (`Planejar`).
    /// A ZONA NASCE INERTE e liga no sinal de pouso: na queda nao ha' limite (ordem do Diretor, 27/08).
    /// Raios como FRACAO do raio do mapa: metro cravado envelhece calado.
    /// Classe PURA: `Tick(dt, alvos)`; a casca so' desenha a parede com Centro/Raio.
    /// </summary>
    public sealed class Zona
    {
        public struct Fase
        {
            public float Espera, Fecha, Frac, Dps;
            public Fase(float espera, float fecha, float frac, float dps) { Espera = espera; Fecha = fecha; Frac = frac; Dps = dps; }
        }

        public struct Circulo
        {
            public Vector3 Centro;
            public float Raio, Dps;
        }

        public enum Estado { Inerte, Abertura, Formando, Espera, Fecha }

        /// <summary>Seed de FALLBACK (teste). Em partida e' sorteado — ver `SeedDaPartida`.</summary>
        public const int SEED_ZONA = 2707;
        public const float RAIO_MAPA_PADRAO = 132f;
        /// <summary>1:10 de mapa INTEIRO aberto depois do pouso (Apex 1:15, PUBG 2:00, Fortnite 2:30).</summary>
        public const float ABERTURA_S = 70f;
        /// <summary>A tempestade vem de fora e PARA na costa. Anuncio, nao punicao.</summary>
        public const float FORMACAO_S = 10f;
        /// <summary>
        /// As 5 fases: janelas 1:00/50/40/30 (a partida ACELERA), raios ~0.65 por fase, a ULTIMA FECHA EM ZERO
        /// (PUBG fase 9, Apex ring 6, Fortnite zona final) — sem isso a partida acaba por cronometro com dois vivos.
        /// </summary>
        public static readonly Fase[] FASES =
        {
            new Fase(60f, 30f, 0.62f, 1.5f),
            new Fase(50f, 26f, 0.42f, 3.0f),
            new Fase(40f, 22f, 0.26f, 6.0f),
            new Fase(30f, 18f, 0.13f, 11.0f),
            new Fase(15f, 25f, 0.00f, 26.0f),
        };
        /// <summary>Quanto o centro novo foge do velho, como fracao da FOLGA (r_velho - r_novo): contencao por construcao.</summary>
        public const float DESLOCAMENTO = 0.75f;
        /// <summary>s por aplicacao de dano. NUNCA por frame (dano fracionario vira zero e 60 sinais/s).</summary>
        public const float TICK_S = 1f;
        public const float ALTURA_PAREDE = 55f;
        /// <summary>Fracao do raio a partir da qual o bot rotaciona.</summary>
        public const float BOT_MARGEM = 0.85f;

        public readonly int SeedDaPartida;
        public readonly float RaioMapa;
        public readonly Circulo[] Plano;

        public Estado EstadoAtual { get; private set; } = Estado.Inerte;
        /// <summary>0 = antes da 1a fase; 1..5.</summary>
        public int FaseAtual { get; private set; }
        public Vector3 Centro { get; private set; } = Vector3.zero;
        public float Raio { get; private set; }
        /// <summary>Segundos que faltam no cronometro corrente (a HUD desenha isto).</summary>
        public float Restante { get; private set; }

        private float _dur;                // duracao do cronometro corrente (para interpolar)
        private Vector3 _centroDe, _centroPara;
        private float _raioDe, _raioPara;
        private float _tickAcc;
        private bool _playerFora;          // borda: ZonaEstado so' quando MUDA

        /// <summary>Cria a zona da partida. `seed &lt; 0` = sorteia (e guarda) o seed desta partida.</summary>
        public Zona(IRelevo relevo, int seed = -1)
        {
            // Guid, nao `new Random()`: em Mono o sem-seed usa TickCount e duas partidas no mesmo ms sairiam iguais.
            SeedDaPartida = seed < 0 ? (System.Guid.NewGuid().GetHashCode() & 0x7FFFFFFF) | 1 : seed;
            RaioMapa = RaioDoMapa(relevo);
            Plano = Planejar(relevo, SeedDaPartida);
            // NASCE NA BORDA: `Dentro()` responde SIM para todo mundo durante a queda.
            Raio = RaioMapa;
        }

        public static float RaioDoMapa(IRelevo relevo) =>
            relevo != null && relevo.RaioTerra > 0f ? relevo.RaioTerra : RAIO_MAPA_PADRAO;

        /// <summary>O SORTEIO INTEIRO, DE UMA VEZ. Lei: o circulo novo esta' SEMPRE contido no anterior.</summary>
        public static Circulo[] Planejar(IRelevo relevo, int seed = SEED_ZONA)
        {
            var rng = new System.Random(seed);
            float mapa = RaioDoMapa(relevo);
            Vector3 c = Vector3.zero;
            float r = mapa;
            var saida = new Circulo[FASES.Length];
            for (int i = 0; i < FASES.Length; i++)
            {
                float novoR = FASES[i].Frac * mapa;
                float folga = Mathf.Max(r - novoR, 0f) * DESLOCAMENTO;
                c = SortearCentro(relevo, rng, c, folga);
                r = novoR;
                saida[i] = new Circulo { Centro = c, Raio = r, Dps = FASES[i].Dps };
            }
            return saida;
        }

        /// <summary>sqrt(rand) distribui por AREA; rejeita agua perguntando a ilha (so' o ponto central).</summary>
        private static Vector3 SortearCentro(IRelevo relevo, System.Random rng, Vector3 c, float folga)
        {
            Vector3 ultimo = c;
            for (int t = 0; t < 12; t++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float d = Mathf.Sqrt((float)rng.NextDouble()) * folga;
                var p = new Vector3(c.x + Mathf.Cos(ang) * d, 0f, c.z + Mathf.Sin(ang) * d);
                ultimo = p;
                if (relevo == null || relevo.PodePousar(p.x, p.z)) return p;
            }
            return ultimo;  // ilha impossivel: aceita o ultimo palpite e o jogo segue
        }

        // ------------------------------------------------------------------- consultas

        public bool Ligada => EstadoAtual != Estado.Inerte;
        /// <summary>A tempestade JA' EXISTE? Falso na queda e nos 70 s de abertura.</summary>
        public bool Ativa => EstadoAtual != Estado.Inerte && EstadoAtual != Estado.Abertura;
        public bool Fechando => EstadoAtual == Estado.Fecha;
        /// <summary>Antes da 1a fase fechar, a zona nao doi.</summary>
        public float DpsAtual => FaseAtual <= 0 ? 0f : FASES[Mathf.Min(FaseAtual, FASES.Length) - 1].Dps;
        /// <summary>CILINDRO, nao esfera: subir no plato nao tira ninguem do circulo.</summary>
        public bool Dentro(Vector3 pos) => Dist(pos) <= Raio;
        private float Dist(Vector3 pos) => new Vector2(pos.x - Centro.x, pos.z - Centro.z).magnitude;
        /// <summary>Bot fora da margem deve rotacionar para o centro.</summary>
        public bool DevePuxar(Vector3 pos) => FaseAtual > 0 && Dist(pos) > Raio * BOT_MARGEM;

        // ------------------------------------------------------------------- o relogio

        /// <summary>LIGA A TEMPESTADE (no pouso). Idempotente: sinal de pouso repetido nao reinicia.</summary>
        public void LigarNoPouso()
        {
            if (EstadoAtual != Estado.Inerte) return;
            EstadoAtual = Estado.Abertura;
            Armar(ABERTURA_S);
            Bus.EmitZonaAbertura(ABERTURA_S);
        }

        /// <summary>Um passo. `alvos` = os pawns da arena (o dano varre 1x por TICK_S). Inerte = nada acontece.</summary>
        public void Tick(float dt, IList<IEntidade> alvos = null)
        {
            if (!Ligada || dt <= 0f) return;
            if (Restante > 0f)
            {
                Restante = Mathf.Max(Restante - dt, 0f);
                Interpolar();
                if (Restante <= 0f) NoTempo();
            }
            _tickAcc += dt;
            if (_tickAcc >= TICK_S)
            {
                _tickAcc -= TICK_S;
                Tique(alvos);
            }
        }

        private void Armar(float s) { _dur = s; Restante = s; }

        private void Interpolar()
        {
            if (EstadoAtual != Estado.Fecha && EstadoAtual != Estado.Formando) return;
            float p = _dur > 0f ? 1f - Restante / _dur : 1f;
            Raio = Mathf.Lerp(_raioDe, _raioPara, p);
            Centro = Vector3.Lerp(_centroDe, _centroPara, p);
        }

        private void NoTempo()
        {
            switch (EstadoAtual)
            {
                case Estado.Abertura: Formar(); break;
                case Estado.Formando: Chegou(); break;
                case Estado.Fecha: Chegou(); break;
                case Estado.Espera: Fechar(); break;
            }
        }

        /// <summary>A parede vem de fora do mapa e para na costa. Nao doi.</summary>
        private void Formar()
        {
            EstadoAtual = Estado.Formando;
            _raioDe = RaioMapa * 1.6f; _raioPara = RaioMapa;
            _centroDe = _centroPara = Vector3.zero;
            Raio = _raioDe;
            Armar(FORMACAO_S);
            Bus.EmitZonaFormando(RaioMapa, FORMACAO_S);
        }

        /// <summary>ESPERA: parada, com o PROXIMO circulo publico (a HUD poe na bussola).</summary>
        private void Avisar()
        {
            EstadoAtual = Estado.Espera;
            if (FaseAtual >= Plano.Length) { Restante = 0f; return; }  // a tempestade tomou o mapa
            Circulo alvo = Plano[FaseAtual];
            float espera = FASES[FaseAtual].Espera;
            Armar(espera);
            Bus.EmitZonaAvisou(FaseAtual + 1, alvo.Centro, alvo.Raio, espera);
        }

        /// <summary>FECHA: centro e raio caminham JUNTOS, na mesma duracao — a contencao vale em todo instante.</summary>
        private void Fechar()
        {
            Circulo alvo = Plano[FaseAtual];
            FaseAtual++;
            EstadoAtual = Estado.Fecha;
            float dur = FASES[FaseAtual - 1].Fecha;
            _raioDe = Raio; _raioPara = alvo.Raio;
            _centroDe = Centro; _centroPara = alvo.Centro;
            Armar(dur);
            Bus.EmitZonaFechando(FaseAtual, alvo.Centro, alvo.Raio, dur);
        }

        /// <summary>A parede chegou: crava os valores exatos e abre a proxima espera.</summary>
        private void Chegou()
        {
            Raio = _raioPara;
            Centro = _centroPara;
            Avisar();
        }

        /// <summary>Cenario controlado (teste/depuracao): crava fase, centro e raio, parada em espera.</summary>
        public void ForcarCirculo(int fase, Vector3 centro, float raio)
        {
            FaseAtual = Mathf.Clamp(fase, 0, FASES.Length);
            Centro = centro;
            Raio = raio;
            EstadoAtual = Estado.Espera;
            Restante = 0f;
        }

        // ------------------------------------------------------------------- o dano

        /// <summary>
        /// O TIQUE (1x por TICK_S). Dano SO' por Combat (ponto unico), ignoraEscudo: a tempestade nao e' magia
        /// de ninguem. Fonte null = ambiente. ZonaDano so' para o player; ZonaEstado na BORDA.
        /// </summary>
        public void Tique(IList<IEntidade> alvos)
        {
            if (alvos == null) return;
            float dps = DpsAtual;
            bool playerFora = false;
            for (int i = 0; i < alvos.Count; i++)
            {
                IEntidade p = alvos[i];
                if (p == null || p.Vital == null || !p.Vital.Viva) continue;
                if (Dentro(p.Pos)) continue;
                if (p.EhPlayer) playerFora = true;
                float dano = dps * TICK_S;
                if (dano <= 0f) continue;
                // Fogo e' a linha NEUTRA (esc 1, vida 1) e o escudo e' ignorado: equivale ao "zona" do Godot.
                float efetivo = Combat.AplicarDano(p, dano, Elemento.Fogo, null, false, true);
                if (efetivo > 0f && p.EhPlayer) Bus.EmitZonaDano(efetivo, dps);
            }
            if (playerFora != _playerFora)
            {
                _playerFora = playerFora;
                Bus.EmitZonaEstado(!playerFora);
            }
        }
    }
}
