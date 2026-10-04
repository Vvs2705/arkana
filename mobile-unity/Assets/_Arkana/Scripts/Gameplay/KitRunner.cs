using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// O que a cena DESENHA de um kit (muralha, poca, eco, fio, tear, alvo aceso). Classe pura: a habilidade
    /// escreve, o KitRunner apaga quando Restante zera, a casca le' Visuais e instancia/atualiza o VFX.
    /// Segmento = Pos..Pos2 (Raio e' a espessura); ponto = Pos (Raio e' o raio). Alvo != null = preso ao alvo.
    /// </summary>
    public sealed class EfeitoVisual
    {
        public string Tipo;
        public Vector3 Pos, Pos2;
        public float Raio, Duracao, Restante;
        public IEntidade Alvo;

        public EfeitoVisual(string tipo, Vector3 pos, Vector3 pos2, float raio, float duracao, IEntidade alvo = null)
        {
            Tipo = tipo; Pos = pos; Pos2 = pos2; Raio = raio; Duracao = duracao; Restante = duracao; Alvo = alvo;
        }

        public bool Segmento => (Pos2 - Pos).sqrMagnitude > 0.0001f;
    }

    /// <summary>
    /// MOTOR DAS HABILIDADES (GDD §3 e §4) — espelho de gameplay/KitRunner.gd. Classe PURA: o pawn (raia PAWN)
    /// cria uma e chama Tick; a HUD observa pelo Bus. AS DUAS LEIS que este arquivo cumpre e o teste prova:
    ///  1. ECONOMIA (§4.2 + DIRECAO.md §4): tatica paga COOLDOWN; suprema paga CARGA 0->100% (tempo + dano
    ///     causado, so' NO CHAO). Nenhuma linha daqui nem dos kits escreve Mana.
    ///  2. TELEGRAFIA (§4.3): UsarSuprema NAO executa o efeito, AGENDA — grampeado em Kits.TelegrafiaMin..Max.
    /// Mago sem kit registrado joga normal (ataque), sem tatica/suprema: KitBound(slug, false).
    /// </summary>
    public sealed partial class KitRunner
    {
        /// <summary>O REGISTRO: slug -> fabrica do kit. Os 17 entram pelos GRUPOS (KitRunner.GrupoX.cs), um arquivo cada.</summary>
        public static readonly Dictionary<string, Func<IHabilidade>> Registro = new Dictionary<string, Func<IHabilidade>>
        {
            { "01-pyra", () => new Pyra() },
            { "03-veu", () => new Veu() },
            { "10-tessa", () => new Tessa() },
        };

        static partial void RegistrarGrupoA(Dictionary<string, Func<IHabilidade>> r);
        static partial void RegistrarGrupoB(Dictionary<string, Func<IHabilidade>> r);
        static partial void RegistrarGrupoC(Dictionary<string, Func<IHabilidade>> r);
        static partial void RegistrarGrupoD(Dictionary<string, Func<IHabilidade>> r);

        static KitRunner()
        {
            RegistrarGrupoA(Registro); RegistrarGrupoB(Registro); RegistrarGrupoC(Registro); RegistrarGrupoD(Registro);
        }

        public readonly string Slug;
        public readonly Kits.KitDef Dados;
        public readonly IConjurador Dono;
        /// <summary>Null = mago sem kit implementado.</summary>
        public readonly IHabilidade Impl;

        public float TaticaCd { get; private set; }
        /// <summary>0..1. Comeca VAZIA na queda; enche com tempo e dano causado; usar zera.</summary>
        public float CargaSuprema { get; private set; }
        /// <summary>> 0 = suprema avisada, ainda nao aconteceu.</summary>
        public float Telegrafia { get; private set; }
        /// <summary>> 0 = "sem conjurar" (o preco da Veu) — vale para o ATAQUE tambem: o pawn le' PodeConjurar.</summary>
        public float Silencio { get; private set; }
        /// <summary>Ha' quanto tempo nao ataca / nao apanha. Zerados no spawn: quietude se conquista.</summary>
        public float DesdeAtaque { get; private set; }
        public float DesdeDano { get; private set; }
        /// <summary>A casca liga no frame em que a esquiva comecou (a Pyra deixa fogo no dash).</summary>
        public bool DashIniciou;
        /// <summary>Arma equipada (o leque da Pyra sai com o tier dela). Null = luva comum.</summary>
        public ArmaSlot Slot;
        /// <summary>Casca: registra o projetil lancado por um kit na arena (voo e acerto sao dela).</summary>
        public Action<Projetil> AoLancar;
        /// <summary>Casca: os projeteis vivos da arena (o Tear-Mae da Tessa engole os inimigos; ele REMOVE da lista).</summary>
        public Func<IList<Projetil>> ProjeteisVivos;
        /// <summary>Casca: um projetil foi absorvido (apaga o VFX dele).</summary>
        public Action<Projetil> AoAbsorver;
        /// <summary>Casca: dps do terreno no ponto (TerrenoReativo.DpsEm). Null = 0 — a passiva da Pyra le' daqui.</summary>
        public static Func<Vector3, float> DpsDoTerreno;

        private readonly Dictionary<string, float> _estados = new Dictionary<string, float>();
        private readonly List<string> _expirados = new List<string>();
        /// <summary>Copia das chaves reaproveitada (era `new List` por corpo por quadro: lixo constante para o GC do celular).</summary>
        private readonly List<string> _chaves = new List<string>();
        private readonly List<EfeitoVisual> _visuais = new List<EfeitoVisual>();
        private float _vidaAntes;
        private bool _ligado;

        public IReadOnlyList<EfeitoVisual> Visuais => _visuais;

        /// <summary>Segundos para a suprema encher: a PARTIDA decide (no treino, 5 s — Partida.SupremaCargaS); sem partida
        /// (teste puro), o dado do kit. O treino tinha o numero e o teste dele, mas o runner nunca perguntava: a Pyra
        /// esperava os 50 s de partida no treino (diag da foto 18 de 12/09).</summary>
        public float SupremaCargaS => Partida.Atual != null ? Partida.Atual.SupremaCargaS(Dados.SupremaCarga) : Dados.SupremaCarga;

        /// <summary>`dados` so' para teste (grampo da telegrafia com ficha ruim); em jogo vem de Kits.De(slug).</summary>
        public KitRunner(string slug, IConjurador dono, Kits.KitDef dados = null)
        {
            Slug = slug ?? "";
            Dono = dono;
            Dados = dados ?? Kits.De(Slug);
            Func<IHabilidade> fab;
            // Bots NAO usam kit (nem passiva) — como no Godot; o dia em que usarem e' decisao do Diretor.
            Impl = Dono != null && Dono.EhPlayer && Registro.TryGetValue(Slug, out fab) ? fab() : null;
            _vidaAntes = VidaTotal();
            Bus.TerrainHit += AoTerrenoAtingido;
            _ligado = true;
            // A HUD OBSERVA: qual mago entrou e se ha' botao de tatica/suprema. So' o player (bots nao poluem o Bus).
            if (Dono != null && Dono.EhPlayer) Bus.EmitKitBound(Slug, Impl != null);
        }

        /// <summary>Solta o Bus (a casca chama no OnDestroy). Bus.Reset() tambem limpa.</summary>
        public void Desligar()
        {
            if (!_ligado) return;
            Bus.TerrainHit -= AoTerrenoAtingido;
            _ligado = false;
        }

        // ------------------------------------------------------------------ tique

        /// <summary>
        /// 1x por frame. `danoCausadoDelta` = dano EFETIVO que o dono causou desde o tique anterior (o pawn le'
        /// Vital.DanoCausado e diferencia) — e' o canal Apex da carga. Dano RECEBIDO se detecta pela queda de
        /// vida+escudo (sem assinar DamageApplied: nada vaza).
        /// </summary>
        public void Tick(float dt, float danoCausadoDelta = 0f)
        {
            if (Dono == null || Dono.Vital == null || !Dono.Vital.Viva || !(dt > 0f)) return;
            float taticaAntes = TaticaCd;
            TaticaCd = Mathf.Max(TaticaCd - dt, 0f);
            Carregar(dt / Mathf.Max(SupremaCargaS, 0.001f));
            Carregar(danoCausadoDelta * Kits.CargaPorDano);
            Silencio = Mathf.Max(Silencio - dt, 0f);
            DesdeAtaque += dt;
            DesdeDano += dt;
            if (taticaAntes > 0f && TaticaCd <= 0f) AvisarCd("tatica", 0f);

            float vida = VidaTotal();
            if (vida < _vidaAntes - 0.0001f)
            {
                DesdeDano = 0f;
                if (Impl != null) Impl.DanoRecebido(this, _vidaAntes - vida);
            }
            _vidaAntes = vida;

            _expirados.Clear();
            _chaves.Clear();
            _chaves.AddRange(_estados.Keys);
            foreach (string nome in _chaves)
            {
                float t = _estados[nome] - dt;
                if (t <= 0f) _expirados.Add(nome);
                else _estados[nome] = t;
            }
            foreach (string nome in _expirados)
            {
                _estados.Remove(nome);
                AvisarEstado(nome, false);
                if (Impl != null) Impl.EstadoAcabou(this, nome);
            }
            // A TELEGRAFIA (§4.3): o efeito so' DEPOIS do aviso.
            if (Telegrafia > 0f)
            {
                Telegrafia = Mathf.Max(Telegrafia - dt, 0f);
                if (Telegrafia <= 0f && Impl != null) Impl.Suprema(this);
            }
            if (Impl != null) Impl.Tick(this, dt);
            _vidaAntes = VidaTotal();   // o kit pode ter curado/regenerado: nao e' dano

            for (int i = _visuais.Count - 1; i >= 0; i--)
            {
                _visuais[i].Restante -= dt;
                if (_visuais[i].Restante <= 0f) _visuais.RemoveAt(i);
            }
            DashIniciou = false;
        }

        /// <summary>O pawn avisa quando um ATAQUE sai: quebra a Entrelinha da Veu.</summary>
        public void NotificarAtaque() => DesdeAtaque = 0f;

        // -------------------------------------------------------------- as 3 portas

        public bool PodeConjurar => Silencio <= 0f && Dono != null && Dono.Vital != null && Dono.Vital.Viva;
        public bool ProntoTatica => Impl != null && TaticaCd <= 0f && PodeConjurar;
        public bool ProntoSuprema => Impl != null && CargaSuprema >= 1f && Telegrafia <= 0f && PodeConjurar;

        /// <summary>TATICA — paga COOLDOWN, nunca mana.</summary>
        public bool UsarTatica()
        {
            if (!ProntoTatica) return false;
            TaticaCd = Dados.TaticaCd;
            AvisarCd("tatica", TaticaCd);
            Impl.Tatica(this);
            return true;
        }

        /// <summary>SUPREMA — so' a 100%; gasta a carga inteira e AVISA. O efeito e' do Tick, quando o aviso acaba.</summary>
        public bool UsarSuprema()
        {
            if (!ProntoSuprema) return false;
            CargaSuprema = 0f;
            Telegrafia = Mathf.Clamp(Dados.Telegrafia, Kits.TelegrafiaMin, Kits.TelegrafiaMax);
            AvisarCd("suprema", SupremaCargaS);
            // "Se mata rapido, avisa antes" — vale para TODOS, inclusive bot (a suprema inimiga muda mata a contrajogada).
            Bus.EmitKitTelegraph(Slug, "suprema", Telegrafia, Dono.Pos);
            return true;
        }

        /// <summary>Toda carga entra AQUI. NO AR nao anda (fecha os dois canais de uma vez). Borda dos 100% avisa 1x.</summary>
        private void Carregar(float fracao)
        {
            if (!(fracao > 0f) || Impl == null) return;   // sem kit nao ha' barra: o botao esta' apagado
            if (!Dono.NoChao) return;
            float antes = CargaSuprema;
            CargaSuprema = Mathf.Min(CargaSuprema + fracao, 1f);
            if (antes < 1f && CargaSuprema >= 1f) AvisarCd("suprema", 0f);
        }

        // --------------------------------------------------------- leitura da HUD

        /// <summary>0..1 do cooldown (1 = acabou de usar, 0 = pronto).</summary>
        public float FracTatica => TaticaCd / Mathf.Max(Dados.TaticaCd, 0.001f);
        /// <summary>Quanto FALTA (0 = pronta); o botao mostra 100 - falta.</summary>
        public float FracSuprema => 1f - CargaSuprema;

        /// <summary>Sinais SO' do player e na BORDA (usou / ficou pronto), nunca por frame.</summary>
        public void AvisarCd(string tipo, float restante)
        {
            if (Dono == null || !Dono.EhPlayer) return;
            Bus.EmitKitCooldown(tipo, restante, tipo == "tatica" ? Dados.TaticaCd : SupremaCargaS);
        }

        public void AvisarEstado(string nome, bool ligado)
        {
            if (Dono != null && Dono.EhPlayer) Bus.EmitKitState(nome, ligado);
        }

        // ------------------------------------------------------- servicos dos kits

        public Vector3 Pos => Dono.Pos;

        /// <summary>Direcao de mira horizontal, normalizada; sem mira = frente.</summary>
        public Vector3 Mira()
        {
            Vector3 d = Dono.DirecaoDaMira;
            d.y = 0f;
            return d.sqrMagnitude > 0.0001f ? d.normalized : Vector3.forward;
        }

        /// <summary>Buff e' lentidao com fator > 1: a mesma porta do produto unico (substitui, nao soma).</summary>
        public void BuffVelocidade(float mult, float dur) => Efeitos.Lentificar(Dono, mult, dur);

        public void Silenciar(float s)
        {
            Silencio = Mathf.Max(Silencio, s);
            LigarEstado("silencio", Silencio);
        }

        /// <summary>Soma tempo ao cooldown JA' em curso (o braco molhado da Pyra).</summary>
        public void AcrescentarCdTatica(float s)
        {
            TaticaCd += s;
            AvisarCd("tatica", TaticaCd);
        }

        /// <summary>Soma ate' no minimo `s` (o braco frio nao ENCURTA um cooldown maior).</summary>
        public void ForcarCdTatica(float s)
        {
            TaticaCd = Mathf.Max(TaticaCd, s);
            AvisarCd("tatica", TaticaCd);
        }

        /// <summary>Estado com RELOGIO: expira sozinho e a HUD recebe o desligamento (o chip nunca fica preso).</summary>
        public void LigarEstado(string nome, float dur)
        {
            _estados[nome] = dur;
            AvisarEstado(nome, true);
        }

        public bool EstadoAtivo(string nome) => _estados.ContainsKey(nome);

        /// <summary>
        /// IMUNIDADE SEM TOCAR NO PONTO UNICO DE DANO: o Combat ja' cobrou; o kit DEVOLVE no mesmo tique exatamente
        /// o que a regra dele nega (Coracao de Fornalha). Cura grampeada em HpMax pela Vitalidade.
        /// </summary>
        public void DevolverDano(float quanto)
        {
            if (!(quanto > 0f) || Dono.Vital == null) return;
            Dono.Vital.Curar(quanto);
            if (Dono.EhPlayer) Bus.EmitHealthChanged(Dono.Vital.Hp, Dono.Vital.HpMax);
        }

        /// <summary>Entidades vivas perto do ponto, sem `excluir`. Sem AlvosNoRaio na casca = ninguem.</summary>
        public List<IEntidade> AlvosPerto(Vector3 ponto, float raio, IEntidade excluir = null)
        {
            var saida = new List<IEntidade>();
            IEntidade[] todos = Dono.AlvosNoRaio != null ? Dono.AlvosNoRaio(ponto, raio) : null;
            if (todos == null) return saida;
            float r2 = raio * raio;
            for (int i = 0; i < todos.Length; i++)
            {
                IEntidade e = todos[i];
                if (e == null || e == excluir || e.Vital == null || !e.Vital.Viva) continue;
                if ((e.Pos - ponto).sqrMagnitude <= r2) saida.Add(e);
            }
            return saida;
        }

        /// <summary>Tiro de kit: o ponto unico e' Projetil.Lancar (Disparo/SpellCast saem de la'). Sem mana (§4.2).</summary>
        public Projetil Lancar(Vector3 origem, Vector3 dir, Elemento el)
        {
            Projetil p = Projetil.Lancar(Dono, origem, dir, el, Slot);
            AoLancar?.Invoke(p);
            return p;
        }

        public EfeitoVisual Visual(string tipo, Vector3 pos, Vector3 pos2, float raio, float dur, IEntidade alvo = null)
        {
            var v = new EfeitoVisual(tipo, pos, pos2, raio, dur, alvo);
            _visuais.Add(v);
            return v;
        }

        /// <summary>
        /// ESCUDO (GDD §5): o Compasso da Tessa REGENERA e o Tear-Mae TECE — nao e' dano, entao nao passa pelo
        /// Combat. UNICO ponto do sistema de kits que escreve Escudo; grampeado no teto do nivel.
        /// </summary>
        public static float RegenerarEscudo(IEntidade alvo, float quanto)
        {
            if (alvo == null || alvo.Vital == null || !(quanto > 0f)) return 0f;
            Vitalidade v = alvo.Vital;
            v.Escudo = Mathf.Min(v.Escudo + quanto, v.EscudoMax);
            Bus.EmitShieldChanged(alvo, v.Escudo, v.EscudoMax, v.Nivel);
            return v.Escudo;
        }

        /// <summary>Distancia de um ponto ao SEGMENTO a-b (fio e muralha sao segmentos, nao esferas).</summary>
        public static float DistSegmento(Vector3 p, Vector3 a, Vector3 b)
        {
            Vector3 ab = b - a;
            float len2 = ab.sqrMagnitude;
            if (len2 < 0.0001f) return Vector3.Distance(p, a);
            float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / len2);
            return Vector3.Distance(p, a + ab * t);
        }

        private float VidaTotal() => Dono != null && Dono.Vital != null ? Dono.Vital.Hp + Dono.Vital.Escudo : 0f;

        private void AoTerrenoAtingido(Elemento el, Vector3 pos, bool forte)
        {
            if (Impl != null) Impl.TerrenoAtingido(this, el, pos, forte);
        }
    }
}
