using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// BAU CELESTIAL — a UNICA porta da MANOPLA (GDD §16.2). Nao e' presente, e' IMA: queda TELEGRAFADA
    /// (feixe QUEDA_S antes de pousar), pouso em campo ABERTO (anel como fracao do raio de terra) e abrir
    /// CANALIZA parado por CANALIZAR_S — a janela de contra-ataque. Relogio FIXO (quem leva e' quem planejou),
    /// POSICAO e PAR pelo seed.
    /// </summary>
    public sealed class BauCelestial
    {
        public enum Fase { Esperando, Caindo, Pousado, Aberto }

        /// <summary>Anuncia aos 45 s, pousa aos 51 s. KNOB: subir encurta o reinado da manopla.</summary>
        public const float ANUNCIO_S = 45f;
        public const float QUEDA_S = 6f;
        public const float ALTURA_QUEDA = 90f;
        /// <summary>s parado abrindo — a janela de contra-ataque.</summary>
        public const float CANALIZAR_S = 3f;
        /// <summary>m — precisa CHEGAR no bau.</summary>
        public const float RAIO_ABRIR = 2.6f;
        /// <summary>Anel de pouso como FRACAO do raio de terra (eram 18/48 m cravados).</summary>
        public const float RAIO_MIN_F = 0.14f;
        public const float RAIO_MAX_F = 0.36f;
        public const float RAIO_TERRA_PADRAO = 132f;
        public const float ATRAIR_BOT_M = 55f;

        public Fase FaseAtual { get; private set; } = Fase.Esperando;
        public readonly Vector3 Pos;
        /// <summary>Qual par fixo esta' dentro (deterministico pelo seed).</summary>
        public readonly int Indice;
        public float Progresso => Mathf.Clamp01(_progresso / CANALIZAR_S);
        public float Restante { get; private set; } = ANUNCIO_S;

        private readonly Loot _loot;
        private readonly Func<IEntidade, ArmaSlot> _slotDe;
        private float _progresso;
        private bool _playerCanalizou;
        private IEntidade _quemCanalizava;

        /// <summary>
        /// Agenda a queda da partida. `slotDe` resolve o ArmaSlot de quem abre (a casca injeta).
        /// O indice sai PRIMEIRO do rng, antes do ponto: o par nao muda se a ilha mudar o relevo.
        /// </summary>
        public BauCelestial(IRelevo relevo, Loot loot, Func<IEntidade, ArmaSlot> slotDe, int seed = Loot.SEED_LOOT)
        {
            _loot = loot;
            _slotDe = slotDe;
            var rng = new System.Random(seed);
            Indice = rng.Next(Arma.PARES_MANOPLA.Length);
            Pos = Ponto(relevo, rng);
        }

        /// <summary>Anel de campo aberto, longe do centro e da agua (a ilha diz se o chao serve).</summary>
        private static Vector3 Ponto(IRelevo relevo, System.Random rng)
        {
            float rt = relevo != null && relevo.RaioTerra > 0f ? relevo.RaioTerra : RAIO_TERRA_PADRAO;
            for (int t = 0; t < 16; t++)
            {
                float ang = (float)rng.NextDouble() * Mathf.PI * 2f;
                float raio = (RAIO_MIN_F + (float)rng.NextDouble() * (RAIO_MAX_F - RAIO_MIN_F)) * rt;
                float x = Mathf.Cos(ang) * raio, z = Mathf.Sin(ang) * raio;
                float h = 1f;
                if (relevo != null)
                {
                    if (!relevo.PodePousar(x, z)) continue;
                    h = relevo.Altura(x, z);
                }
                return new Vector3(x, h, z);
            }
            return new Vector3(0f, 1f, 0f);  // ilha impossivel: cai no centro e o jogo segue
        }

        /// <summary>Os 2 elementos FIXOS da manopla deste bau.</summary>
        public Elemento[] Elementos => Arma.PARES_MANOPLA[Indice % Arma.PARES_MANOPLA.Length];
        public bool PodeAbrir => FaseAtual == Fase.Pousado;
        public bool NoRaio(Vector3 p) => (p - Pos).sqrMagnitude <= RAIO_ABRIR * RAIO_ABRIR;

        /// <summary>Um passo. `presentes` = os pawns da arena; quem esta' no raio, vivo e de pe' canaliza.</summary>
        public void Tick(float dt, IList<IEntidade> presentes = null)
        {
            if (dt <= 0f) return;
            switch (FaseAtual)
            {
                case Fase.Esperando:
                    Restante -= dt;
                    if (Restante <= 0f) Cair();
                    break;
                case Fase.Caindo:
                    Restante -= dt;
                    if (Restante <= 0f) Pousar();
                    break;
                case Fase.Pousado:
                    Canalizar(dt, presentes);
                    break;
            }
        }

        /// <summary>1. A QUEDA: o feixe acende no ponto EXATO de pouso. Todo mundo ve', todo mundo decide.</summary>
        private void Cair()
        {
            FaseAtual = Fase.Caindo;
            Restante = QUEDA_S;
            Bus.EmitBauAnunciado(Pos, QUEDA_S);
        }

        /// <summary>2. O POUSO: a partir daqui da' pra abrir. O feixe FICA aceso (o farol denuncia quem esta' la').</summary>
        private void Pousar()
        {
            FaseAtual = Fase.Pousado;
            Restante = 0f;
            Bus.EmitBauPousou(Pos);
        }

        /// <summary>3. ABRIR: so' anda com alguem DENTRO do raio e DE PE'; sair, cair ou morrer zera.</summary>
        private void Canalizar(float dt, IList<IEntidade> presentes)
        {
            List<IEntidade> ativos = Ativos(presentes);
            if (ativos.Count == 0) { Cancelar(); return; }
            _progresso += dt;
            IEntidade player = null;
            for (int i = 0; i < ativos.Count; i++) if (ativos[i].EhPlayer) { player = ativos[i]; break; }
            if (player != null)
            {
                _playerCanalizou = true;
                _quemCanalizava = player;
                Bus.EmitBauCanalizando(player, Progresso);
            }
            if (_progresso >= CANALIZAR_S) Abrir(ativos);
        }

        /// <summary>Derrubado e morto nao abrem bau: abrir e' um ato. E' o que faz "levou dano e caiu" cancelar.</summary>
        private List<IEntidade> Ativos(IList<IEntidade> presentes)
        {
            var saida = new List<IEntidade>();
            if (presentes == null) return saida;
            for (int i = 0; i < presentes.Count; i++)
            {
                IEntidade n = presentes[i];
                if (n == null || n.Vital == null || !n.Vital.Viva) continue;
                if (!NoRaio(n.Pos) || !Derrubado.PodeAgir(n)) continue;
                saida.Add(n);
            }
            return saida;
        }

        /// <summary>CANCELAR e' estado de 1a classe: volta do ZERO e a HUD e' avisada na BORDA, uma vez.</summary>
        private void Cancelar()
        {
            if (_progresso <= 0f && !_playerCanalizou) return;
            _progresso = 0f;
            if (_playerCanalizou)
            {
                _playerCanalizou = false;
                Bus.EmitBauCanalizando(_quemCanalizava, 0f);
                _quemCanalizava = null;
            }
        }

        /// <summary>Quem abre e' o MAIS PERTO no instante em que a barra enche — da' pra ROUBAR no ultimo segundo.</summary>
        private void Abrir(List<IEntidade> ativos)
        {
            FaseAtual = Fase.Aberto;
            IEntidade quem = null;
            float d2 = float.PositiveInfinity;
            for (int i = 0; i < ativos.Count; i++)
            {
                float dd = (ativos[i].Pos - Pos).sqrMagnitude;
                if (dd < d2) { d2 = dd; quem = ativos[i]; }
            }
            Elemento[] els = (Elemento[])Elementos.Clone();  // captura ANTES: pegar troca o par do loot pela arma velha
            if (_loot != null)
            {
                LootItem item = _loot.BauCelestial(Pos, Indice);
                ArmaSlot slot = _slotDe != null && quem != null ? _slotDe(quem) : null;
                if (slot != null) _loot.Pegar(item, slot);  // abriu = equipou; a arma velha fica no chao (padrao BR)
            }
            Bus.EmitBauAberto(quem != null && quem.EhPlayer, els);
        }
    }
}
