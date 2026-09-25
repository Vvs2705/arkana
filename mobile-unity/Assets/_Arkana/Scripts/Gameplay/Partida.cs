using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;
using Arkana.Terrain;

namespace Arkana.Gameplay
{
    /// <summary>
    /// A PARTIDA, pura (porte de Main.gd sem a cena): dona do relogio, da arena (quem esta' vivo), da zona, do loot,
    /// do bau, dos projeteis em voo e do veredito. O fim de verdade e' ULTIMO TIME EM PE' (PONTE A6: com dupla, a dupla vence
    /// junta — mesmo com o player fora); SOLO e' o ultimo em pe' de sempre. Match.DurationS e' rede de seguranca (derrota).
    /// A cena cria os corpos e os REGISTRA aqui (o time e' o do Combat.DefinirTime); por frame chama `Tick(dt)`.
    /// `Combat.TickDot` e os relogios de Zona/Loot/Bau/Efeitos/Derrubado rodam AQUI, uma vez, e nao no Pawn.
    /// TREINO (DIRECAO.md §8): sem zona, sem relogio, sem queda; 3 luvas no spawn; bonecos que regeneram; suprema em 5 s.
    /// Consome `Arkana.Menu.Menu.PedidoDeTreino` e ZERA: partida normal nenhuma herda o treino por engano.
    /// </summary>
    public sealed class Partida
    {
        /// <summary>Regen dos bonecos de treino (vida/s).</summary>
        public const float BONECO_REGEN = 15f;
        /// <summary>Suprema enche em 5 s no treino (esperar 50 s nao e' treino, e' fila). O KitRunner le' daqui.</summary>
        public const float SUPREMA_TREINO_S = 5f;
        /// <summary>Capsula de acerto do projetil (mesma do CharacterController do Pawn).</summary>
        public const float RAIO_CORPO = 0.35f, ALTURA_CORPO = 1.8f;

        /// <summary>A partida em curso (o Pawn registra projeteis e pega loot por aqui). Null fora de partida.</summary>
        public static Partida Atual { get; private set; }

        public readonly List<IEntidade> Arena = new List<IEntidade>();
        public readonly List<IEntidade> Bonecos = new List<IEntidade>();
        public readonly List<Projetil> Projeteis = new List<Projetil>();
        public IEntidade Player { get; private set; }
        public Zona Zona { get; private set; }
        public Loot Loot { get; private set; }
        public BauCelestial Bau { get; private set; }
        /// <summary>Terreno reativo da partida (opcional; a cena ou o teste escreve). Null = o da cena (TerrenoReativoBehaviour.Atual).</summary>
        public TerrenoReativo Terreno { get; set; }
        public int Seed { get; private set; }
        public bool Treino { get; private set; }
        public bool Rodando { get; private set; }
        public bool Acabou { get; private set; }
        public int BotsVivos { get; private set; }
        public float Restante { get; private set; }

        /// <summary>Times com alguem vivo (de pe' OU derrubado), contando o do player. A HUD do modo DUPLA le' ("DUPLAS n").</summary>
        public int TimesVivos
        {
            get
            {
                _contados.Clear();   // reusado: a HUD pergunta por frame, sem lixo
                for (int i = 0; i < Arena.Count; i++) if (Vivo(Arena[i])) _contados.Add(Combat.TimeDe(Arena[i]));
                return _contados.Count;
            }
        }

        /// <summary>O player foi ELIMINADO mas o time dele segue vivo: espectador (a camera segue o ParceiroVivo).</summary>
        public bool PlayerFora => Player != null && !Vivo(Player) && ParceiroVivo() != null;

        /// <summary>O 1o vivo (de pe' ou derrubado) do time do player que nao e' o player; null se nenhum.</summary>
        public IEntidade ParceiroVivo() => AliadoVivo(Player);
        /// <summary>O k-esimo (0 = o 1o) aliado vivo do player: no TRIO, k = 1 e' o segundo parceiro (marca, minimapa).</summary>
        public IEntidade ParceiroVivo(int k) => AliadoVivo(Player, k);

        /// <summary>Segundos para a suprema encher (o KitRunner pergunta aqui em vez de a Kits no treino).</summary>
        public float SupremaCargaS(float padrao) => Treino ? SUPREMA_TREINO_S : padrao;

        private readonly IRelevo _relevo;
        private readonly Dictionary<IEntidade, ArmaSlot> _slots = new Dictionary<IEntidade, ArmaSlot>();
        private readonly Dictionary<IEntidade, IEntidade> _ultimoAtacante = new Dictionary<IEntidade, IEntidade>();
        private readonly HashSet<IEntidade> _ultimoDanoAmbiente = new HashSet<IEntidade>();
        private readonly HashSet<int> _contados = new HashSet<int>();

        public Partida(IRelevo relevo) { _relevo = relevo; }

        /// <summary>Comeca a partida. `bots` = quantos a cena vai registrar (o veredito conta por aqui).</summary>
        public void Iniciar(int seed, int bots, bool treino, Vector3 spawnPlayer = default(Vector3))
        {
            Encerrar();
            Treino = treino || Arkana.Menu.Menu.PedidoDeTreino;
            Arkana.Menu.Menu.PedidoDeTreino = false;   // consumido: a proxima partida nasce limpa
            Seed = seed;
            Arena.Clear(); Bonecos.Clear(); Projeteis.Clear(); _slots.Clear(); _ultimoAtacante.Clear(); _ultimoDanoAmbiente.Clear();
            Player = null;
            Rodando = true; Acabou = false;
            BotsVivos = Treino ? 0 : bots;
            Restante = Zona.RaioDoMapa(_relevo) > Zona.RAIO_GRANDE ? Balance.Match.DurationGrandeS : Balance.Match.DurationS;
            Combat.Reset(); Efeitos.Reset(); Derrubado.Reset();
            Derrubado.Instalar();
            Derrubado.Arena = Arena;
            Sintonia.Reset(); SintoniaEfeitos.Reset(); SintoniaEfeitos.Instalar();   // no TREINO tambem: e' onde se aprende o combo
            Loot = new Loot(_relevo);
            if (Treino)
            {
                Zona = null; Bau = null;
                MontarLootDeTreino(spawnPlayer);
            }
            else
            {
                Loot.Espalhar(seed);
                Zona = new Zona(_relevo, seed);
                Bau = new BauCelestial(_relevo, Loot, SlotDe, seed);
            }
            Bus.EntityDied += AoMorrer;
            Bus.DamageApplied += AoDanar;
            Bus.QuedaFase += AoMudarQueda;
            Atual = this;
            Bus.EmitMatchStarted();
        }

        /// <summary>As tres luvas em fila a passos do spawn (a manopla so' aqui: no jogo real e' so' pelo bau).</summary>
        private void MontarLootDeTreino(Vector3 base_)
        {
            string[] ids = { Arma.VARINHA, Arma.CAJADO, Arma.MANOPLA };
            Elemento?[] els = { Elemento.Fogo, Elemento.Raio, null };
            for (int i = 0; i < ids.Length; i++)
            {
                var pos = base_ + new Vector3(3f + i * 1.6f, 0f, 2.5f);
                pos.y = (_relevo != null ? _relevo.Altura(pos.x, pos.z) : 0f) + 0.4f;
                Elemento[] par = ids[i] == Arma.MANOPLA ? (Elemento[])Arma.PARES_MANOPLA[0].Clone() : null;
                Loot.Itens.Add(new LootItem(ids[i], par, els[i], pos));
            }
        }

        /// <summary>A cena registra cada corpo (player, bot). O player e' o EhPlayer.</summary>
        public void Registrar(IEntidade e, ArmaSlot slot)
        {
            if (e == null || Arena.Contains(e)) return;
            Arena.Add(e);
            if (slot != null) _slots[e] = slot;
            if (e.EhPlayer) Player = e;
        }

        /// <summary>Boneco de treino: leva tiro, regenera, e a morte dele nao conta.</summary>
        public void RegistrarBoneco(IEntidade e)
        {
            Registrar(e, null);
            if (!Bonecos.Contains(e)) Bonecos.Add(e);
        }

        public ArmaSlot SlotDe(IEntidade e)
        {
            ArmaSlot s;
            return e != null && _slots.TryGetValue(e, out s) ? s : null;
        }

        /// <summary>Todo tiro em voo passa por aqui (Pawn.Atirar registra). A cena desenha a partir de `Projeteis`.</summary>
        public void Registrar(Projetil p) { if (p != null && p.Vivo) Projeteis.Add(p); }

        /// <summary>
        /// Entidades VIVAS a ate' `raio` m de `pos` (o servico que o IConjurador.AlvosNoRaio dos pawns entrega aos kits).
        /// `excluir` tira alguem (o proprio conjurador) — sem ele, o pawn vem junto: quem exclui a dona e' o kit.
        /// </summary>
        public IEntidade[] AlvosNoRaio(Vector3 pos, float raio, IEntidade excluir = null)
        {
            var saida = new List<IEntidade>();
            float r2 = raio * raio;
            for (int i = 0; i < Arena.Count; i++)
            {
                IEntidade e = Arena[i];
                if (e == null || e == excluir || e.Vital == null || !e.Vital.Viva) continue;
                if ((e.Pos - pos).sqrMagnitude <= r2) saida.Add(e);
            }
            return saida.ToArray();
        }

        private TerrenoReativo TerrenoAtivo =>
            Terreno ?? (TerrenoReativoBehaviour.Atual != null ? TerrenoReativoBehaviour.Atual.Terreno : null);

        /// <summary>Um frame da partida. Ordem: DoT, terreno, zona, loot, bau, estados, derrubados, projeteis, relogio.</summary>
        public void Tick(float dt)
        {
            if (!Rodando || Acabou || dt <= 0f) return;
            Combat.TickDot(dt);
            Sintonia.Tick(dt); SintoniaEfeitos.Tick(dt);
            TerrenoReativo terreno = TerrenoAtivo;
            if (terreno != null) terreno.Tick(dt, Arena);   // o DoT do chao (fogo, agua eletrificada) sai daqui, UMA vez
            if (Zona != null) Zona.Tick(dt, Arena);
            if (Player != null) Loot.Atualizar(Player.Pos);
            if (Bau != null) Bau.Tick(dt, Arena);
            for (int i = 0; i < Arena.Count; i++)
            {
                IEntidade e = Arena[i];
                if (e == null || e.Vital == null) continue;
                if (e.Vital.Viva) Efeitos.Tick(e, dt);
                Derrubado d = Derrubado.De(e);
                if (d != null) d.Tick(dt);
            }
            for (int i = 0; i < Bonecos.Count; i++)
            {
                Vitalidade v = Bonecos[i].Vital;
                if (v == null) continue;
                if (!v.Viva) v.Reset(); else v.Curar(BONECO_REGEN * dt);   // apanhar sem culpa e' o servico deles
            }
            // MURO E' COBERTURA (GDD §14): o tiro que entra no muro PARA ali — o corpo atras nem e' consultado. O muro
            // apanha pelo caminho que ja' existe: Impacto -> TerrainHit na posicao do tiro -> TerrenoReativo.Reagir (hp x Estrutura).
            // ponytail: testa o ponto de cada passo; passo > CellSize (Raio abaixo de ~12 fps) atravessa — sub-passo se medir isso.
            Func<Vector3, IEntidade> acerto = Acerto;
            if (terreno != null) acerto = pos => NoMuro(terreno, pos) ? null : Acerto(pos);
            for (int i = Projeteis.Count - 1; i >= 0; i--)
            {
                Projetil p = Projeteis[i];
                bool voa = p.Tick(dt, acerto, Arena);
                if (voa && terreno != null && NoMuro(terreno, p.Pos)) { p.Impacto(null); voa = false; }
                // O GELO PARA O TIRO como o chao (onda 18E): quem mira no gelo acerta o gelo — o fogo derrete ONDE bateu, nao no fundo adiante
                if (voa && terreno != null && LeituraDoTerreno.TiroNoGelo(terreno, p.Pos, Arkana.World.Ilha.SuperficieDaAgua(p.Pos.x, p.Pos.z))) { p.Impacto(null); voa = false; }
                // O CHAO PARA O TIRO (GDD §14): sem isto ele varava morro e o terreno reativo so' via o tiro que acertava
                // corpo. Agora o erro acende a mata, faz lama, congela/eletrifica o lago (a celula e' x,z; o fundo conta).
                // ponytail: o fundo e nao a lamina — quem nada atira com a mao abaixo da agua e nao pode matar o proprio tiro.
                if (voa && Arkana.World.Ilha.Atual != null && p.Pos.y < Arkana.World.Ilha.AlturaDoChao(p.Pos.x, p.Pos.z))
                { p.Impacto(null); voa = false; }
                if (!voa) Projeteis.RemoveAt(i);
            }
            if (Acabou) return;   // um projetil pode ter fechado a partida
            if (!Treino)
            {
                Restante = Mathf.Max(Restante - dt, 0f);
                if (Restante <= 0f) Fim(false);   // rede de seguranca: tempo esgotado e' derrota
            }
        }

        /// <summary>Quem esta' na hitbox: capsula do corpo, vivo e sem i-frames (a esquiva e' imunidade TOTAL).</summary>
        public IEntidade Acerto(Vector3 pos)
        {
            float r = RAIO_CORPO + Projetil.RAIO_HITBOX;
            for (int i = 0; i < Arena.Count; i++)
            {
                IEntidade e = Arena[i];
                if (e == null || e.Vital == null || !e.Vital.Viva) continue;
                if (Efeitos.De(e).IframesLeft > 0f) continue;
                Vector3 p = e.Pos;
                float dy = pos.y - p.y;
                if (dy < -Projetil.RAIO_HITBOX || dy > ALTURA_CORPO + Projetil.RAIO_HITBOX) continue;
                float dx = pos.x - p.x, dz = pos.z - p.z;
                if (dx * dx + dz * dz <= r * r) return e;
            }
            return null;
        }

        /// <summary>Celula de muro E abaixo do topo dele: cobertura tem ALTURA (tiro por cima do muro segue voando).</summary>
        private static bool NoMuro(TerrenoReativo t, Vector3 pos) =>
            t.BloqueiaTiro(pos) && pos.y <= t.Centro(t.CelulaEm(pos)).y + Balance.Terrain.WallHeight;

        private void AoMudarQueda(string fase)
        {
            if (fase == Queda.POUSOU && Zona != null) Zona.LigarNoPouso();   // a tempestade conta no chao, nao no ceu
        }

        private void AoDanar(IEntidade alvo, float dano, Elemento el, IEntidade fonte, bool escudo)
        {
            if (alvo == null) return;
            if (fonte != null && fonte != alvo) { _ultimoAtacante[alvo] = fonte; _ultimoDanoAmbiente.Remove(alvo); }
            else _ultimoDanoAmbiente.Add(alvo);
        }

        private void AoMorrer(IEntidade e)
        {
            if (!Rodando || Acabou || e == null) return;
            if (Bonecos.Contains(e)) return;
            if (!e.EhPlayer && !Arena.Contains(e)) return;
            // O TIME DO PLAYER: o player fora com o parceiro vivo vira espectador; DERROTA so' quando o time inteiro saiu
            // (AliadoVivo do player e' o parceiro; o do parceiro e' o player). Solo: ninguem no time — a morte dele e' o fim.
            if (e.EhPlayer || Combat.MesmoTime(e, Player))
            {
                if (!e.EhPlayer) BotsVivos--;   // o parceiro e' bot
                if (AliadoVivo(e) == null) Fim(false);
                return;
            }
            // Kill feed: o tiro direto ja' emite em Projetil.Impacto. Aqui so' a morte por DoT/zona (fonte null)
            // cujo ULTIMO atacante foi o player — senao o abate some do feed (ou duplica).
            IEntidade ultimo;
            if (_ultimoDanoAmbiente.Contains(e) && _ultimoAtacante.TryGetValue(e, out ultimo) && ultimo != null && ultimo.EhPlayer)
                Bus.EmitPlayerKilledBot(e.Nome);
            BotsVivos--;
            // VITORIA: DUPLA (alguem registrado no time do player) = resta so' o time do player, mesmo com ele fora;
            // SOLO = a contagem de sempre (ultimo em pe').
            if (!Treino && (TemParceiro() ? TimesVivos <= 1 : BotsVivos <= 0)) Fim(true);
        }

        /// <summary>Em jogo: com vida (o derrubado conta — a reserva de esvaecer e' Hp) e nao e' boneco. So' o Combat mata, e
        /// zera o Hp ANTES do EntityDied: quem esta' saindo ja' nao conta aqui.</summary>
        private bool Vivo(IEntidade e) => e != null && e.Vital != null && e.Vital.Viva && !Bonecos.Contains(e);

        /// <summary>O 1o vivo do time de `de` que nao e' ele (null se nenhum, ou `de` null).</summary>
        private IEntidade AliadoVivo(IEntidade de, int k = 0)
        {
            for (int i = 0; i < Arena.Count; i++)
            {
                IEntidade e = Arena[i];
                if (e != de && Vivo(e) && Combat.MesmoTime(e, de) && k-- == 0) return e;
            }
            return null;
        }

        /// <summary>Modo DUPLA: alguem alem do player registrado no time dele (vivo ou nao).</summary>
        private bool TemParceiro()
        {
            for (int i = 0; i < Arena.Count; i++) if (Arena[i] != Player && Combat.MesmoTime(Arena[i], Player)) return true;
            return false;
        }

        /// <summary>O veredito, UMA vez. MatchOver e' na borda; o resto observa.</summary>
        public void Fim(bool vitoria)
        {
            if (Acabou) return;
            Acabou = true;
            Rodando = false;
            Bus.EmitMatchOver(vitoria);
        }

        /// <summary>Solta os ouvintes (restart / troca de cena). Idempotente.</summary>
        public void Encerrar()
        {
            Bus.EntityDied -= AoMorrer;
            Bus.DamageApplied -= AoDanar;
            Bus.QuedaFase -= AoMudarQueda;
            Sintonia.Reset(); SintoniaEfeitos.Reset();
            Rodando = false;
            if (Atual == this) Atual = null;
        }
    }
}
