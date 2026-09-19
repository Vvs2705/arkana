using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>
    /// KIT DO CORVOMANTE (05) — Vidente. GDD §3, ficha em design/personagens/05-corvomante.md, DIRECAO.md §6.
    ///   Passiva  Meu Olho Voa    — o corvo FORA do ombro marca todo inimigo a 20m dele (4s, renova enquanto ve')
    ///   Tatica   Voo do Olho     — o corvo voa 4s na direcao da mira (o stick e a camera o guiam) escaneando o caminho
    ///   Suprema  Grasnido do Fim — pulso antimagia de 12m: 50 de dano SO' NO ESCUDO de cada inimigo
    /// OS LIMITADORES SAO PARTE DO KIT: enquanto o corvo voa o CORPO fica parado (piso de velocidade) e SEM CONJURAR — o
    /// preco e' o proprio corpo; o corvo tem 60 de vida (tiro inimigo que passa por ele e' engolido e doi nele) e bate asas
    /// que os bots OUVEM no corvo; o Grasnido avisa 2,5s (o corvo sobe em espiral) e ao acabar o corvo despenca exausto:
    /// 5s sem tatica.
    /// ponytail: "assumir o corvo" com camera propria pede o motor (Player.Camera seguir outro alvo, stick no corvo); hoje
    /// o corvo segue a MIRA do corpo parado. SOLO: o modo sentinela no ombro de um ALIADO (marca a 20m dele) espera o
    /// esquadrao; "marca para o esquadrao" = marca para ele. O Grasnido nao derruba nivel de escudo (Vitalidade.Nivel tem
    /// set privado: pede Vitalidade.Regredir()) nem quebra construcao (pede um evento de antimagia no Bus que muro,
    /// fio e muralha escutem) — e hoje nenhuma construcao alheia existe (bot nao usa kit).
    /// </summary>
    public sealed class Corvomante : IHabilidade
    {
        public const string VOO = "corvomante_voo";
        public const string EXAUSTO = "corvomante_exausto";
        /// <summary>s entre duas olhadas do corvo (marcar a cada quadro seria busca na arena por quadro).</summary>
        const float OLHAR_S = 0.2f;

        /// <summary>O corvo fora do ombro (o voo da tatica). Vida propria; Visual.Pos anda com ele.</summary>
        public sealed class Corvo
        {
            public Vector3 Pos, Dir;
            public float Vida, Asas;
            public EfeitoVisual Visual;
        }

        private Corvo _voo;
        private EfeitoVisual _espiral;
        private Vector3 _centro;
        private float _espiralT, _olharAcc;
        private readonly Dictionary<IEntidade, EfeitoVisual> _marcas = new Dictionary<IEntidade, EfeitoVisual>();

        public Corvo CorvoEmVoo => _voo;
        /// <summary>O corvo esta' fora do ombro (voando ou subindo no Grasnido): o desenho do ombro some.</summary>
        public bool CorvoFora => _voo != null || _espiral != null;
        public bool Marcado(IEntidade e)
        {
            EfeitoVisual v;
            return e != null && _marcas.TryGetValue(e, out v) && v.Restante > 0f;
        }

        public void Tick(KitRunner k, float dt)
        {
            if (_voo != null) Voar(k, dt);
            // a TELEGRAFIA do Grasnido: o corvo sobe em espiral sobre o ponto do aviso — e ve' tudo de la' de cima
            if (k.Telegrafia > 0f)
            {
                if (_espiral == null) { _centro = k.Pos; _espiralT = 0f; _espiral = k.Visual("corvomante_corvo", _centro, _centro, 0.8f, k.Telegrafia); }
                _espiralT += dt;
                float a = _espiralT * 5f;
                Vector3 antes = _espiral.Pos;
                _espiral.Pos = _centro + new Vector3(Mathf.Cos(a) * 2.5f, 2f + _espiralT * 3f, Mathf.Sin(a) * 2.5f);
                _espiral.Pos2 = _espiral.Pos + (_espiral.Pos - antes).normalized;   // o bico aponta para onde voa
                Olhar(k, _espiral.Pos, dt);
            }
            else _espiral = null;
        }

        // ------------------------------------------------------------------ tatica

        /// <summary>Voo do Olho: o corpo fica parado e sem conjurar pelo MESMO relogio do voo (o chip da HUD sai sozinho).</summary>
        public void Tatica(KitRunner k)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            float dur = t["duracao"];
            k.LigarEstado(VOO, dur);
            k.Silenciar(dur);
            k.BuffVelocidade(Velocidade.Piso, dur);
            Vector3 p = k.Pos + Vector3.up * t["altura"];
            Vector3 d = k.Mira();
            _voo = new Corvo { Pos = p, Dir = d, Vida = t["corvo_vida"] };
            _voo.Visual = k.Visual("corvomante_corvo", p, p + d, 0.8f, dur);
        }

        private void Voar(KitRunner k, float dt)
        {
            Dictionary<string, float> t = k.Dados.Tatica;
            // o corvo tem VIDA: tiro inimigo que passa por ele e' engolido e doi nele (a casca nao acerta o que nao e' corpo)
            _voo.Vida -= TirosNoCorvo(k, _voo.Pos, t["raio_acerto"]);
            if (_voo.Vida <= 0f)
            {
                // abatido: cai em penas; o CORPO segue parado ate' o fim do voo (o elo do pacto ainda arde)
                k.Visual("corvomante_grasnido", _voo.Pos, _voo.Pos, 1.5f, 0.6f);
                Recolher();
                return;
            }
            Vector3 d = k.Mira();   // o stick/camera viram o corpo parado — e o corvo obedece (o "assumir" possivel hoje)
            _voo.Dir = d;
            _voo.Pos += d * t["vel"] * dt;
            _voo.Visual.Pos = _voo.Pos;
            _voo.Visual.Pos2 = _voo.Pos + d;
            _voo.Asas += dt;
            if (_voo.Asas >= t["asas_intervalo"])
            {
                _voo.Asas = 0f;
                Bus.EmitDisparo(k.Dono, _voo.Pos);   // bate asas AUDIVELMENTE: os bots escutam o corvo, nao o corpo
            }
            Olhar(k, _voo.Pos, dt);
        }

        private void Recolher()
        {
            if (_voo == null) return;
            _voo.Visual.Restante = 0f;
            _voo = null;
        }

        /// <summary>Meu Olho Voa: quem esta' a `visao` m do corvo (no plano — ele voa alto) fica marcado `marca_dur` s.</summary>
        private void Olhar(KitRunner k, Vector3 olho, float dt)
        {
            _olharAcc += dt;
            if (_olharAcc < OLHAR_S) return;
            _olharAcc = 0f;
            Dictionary<string, float> p = k.Dados.Passiva;
            float r2 = p["visao"] * p["visao"];
            foreach (IEntidade e in k.AlvosPerto(olho, p["visao"] + 12f, k.Dono))
            {
                if (!Inimigo(k.Dono, e) || Plano(e.Pos, olho) > r2) continue;
                EfeitoVisual v;
                if (_marcas.TryGetValue(e, out v) && v.Restante > 0f) { v.Duracao = p["marca_dur"]; v.Restante = p["marca_dur"]; }
                else _marcas[e] = k.Visual("corvomante_marca", e.Pos, e.Pos, 0.5f, p["marca_dur"], e);
            }
        }

        // ------------------------------------------------------------------ suprema

        /// <summary>
        /// Grasnido do Fim, no ponto do AVISO (onde o corvo subiu), nao onde ele parou. SO' NO ESCUDO: pede ao Combat
        /// exatamente o que o escudo tem (ate' 50), dividido pelo fator do elemento — nada transborda para a vida.
        /// </summary>
        public void Suprema(KitRunner k)
        {
            Dictionary<string, float> s = k.Dados.Suprema;
            Vector3 c = _espiral != null ? _centro : k.Pos;
            if (_espiral != null) { _espiral.Restante = 0f; _espiral = null; }
            float esc = Balance.Perfil(Elemento.Vento).Esc, r2 = s["raio"] * s["raio"];
            foreach (IEntidade e in k.AlvosPerto(c, s["raio"] + 5f, k.Dono))
            {
                if (!Inimigo(k.Dono, e) || Plano(e.Pos, c) > r2 || Efeitos.De(e).IframesLeft > 0f) continue;
                float q = Mathf.Min(s["dano_escudo"], e.Vital.Escudo);
                if (q > 0f) Combat.AplicarDano(e, q / esc, Elemento.Vento, k.Dono);   // fonte = ele: credita escudo e carga
            }
            k.Visual("corvomante_grasnido", c, c, s["raio"], 1.2f);
            // ENCERRA: o corvo despenca exausto no ombro e a obsidiana apaga — 5s sem tatica
            k.ForcarCdTatica(s["exausto_dur"]);
            k.LigarEstado(EXAUSTO, s["exausto_dur"]);
        }

        public void EstadoAcabou(KitRunner k, string nome)
        {
            if (nome == VOO) Recolher();   // o voo acabou: o olho volta para o ombro
        }

        public void DanoRecebido(KitRunner k, float quanto) { }
        public void TerrenoAtingido(KitRunner k, Elemento el, Vector3 pos, bool forte) { }

        /// <summary>Tiro inimigo a `raio` do corvo e' ENGOLIDO por ele (sai da arena sem impacto, o caminho do Tear-Mae).
        /// Devolve o dano somado.</summary>
        private static float TirosNoCorvo(KitRunner k, Vector3 centro, float raio)
        {
            IList<Projetil> vivos = k.ProjeteisVivos != null ? k.ProjeteisVivos() : null;
            if (vivos == null) return 0f;
            float dano = 0f, r2 = raio * raio;
            for (int i = vivos.Count - 1; i >= 0; i--)
            {
                Projetil p = vivos[i];
                if (p == null || !p.Vivo || !Inimigo(k.Dono, p.Atirador) || (p.Pos - centro).sqrMagnitude > r2) continue;
                vivos.RemoveAt(i);
                if (k.AoAbsorver != null) k.AoAbsorver(p);
                dano += p.Dano;
            }
            return dano;
        }

        /// <summary>Tiro sem dono (null) nao e' inimigo; o resto e' a pergunta unica do Combat.</summary>
        private static bool Inimigo(IEntidade eu, IEntidade e) => e != null && !Combat.MesmoTime(eu, e);

        private static float Plano(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
