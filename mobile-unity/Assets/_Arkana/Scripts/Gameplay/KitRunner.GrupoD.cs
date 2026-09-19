using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;

namespace Arkana.Gameplay
{
    /// <summary>O GRUPO D no registro: 16 Fizz, 17 Sylva, 18 Basalto, 19 Noctus, 20 Pip (as fichas moram em Kits.GrupoD.cs).</summary>
    public sealed partial class KitRunner
    {
        static partial void RegistrarGrupoD(Dictionary<string, Func<IHabilidade>> r)
        {
            r["16-fizz"] = () => new Fizz();
            r["17-sylva"] = () => new Sylva();
            r["18-basalto"] = () => new Basalto();
            r["19-noctus"] = () => new Noctus();
            r["20-pip"] = () => new Pip();
        }
    }

    /// <summary>
    /// O APOIO DO GRUPO D — o que os 5 kits dividem, FORA do motor (ninguem edita o KitRunner para por um kit):
    ///  - LADO: quem e' inimigo/aliado — a pergunta unica Combat.MesmoTime; SOLO, aliado nao existe e os efeitos "em aliado"
    ///    caem no proprio mago.
    ///  - CORPO: o dash de kit e' o verbo IConjurador.Impulso; a logica mede na ROTA (Investida), que anda a MESMA integral
    ///    do dash do corpo (Locomocao.PercorridoNoDash), entao os dois andam juntos. O fake do teste nao anda. O empurrao de
    ///    INIMIGO (onda do Basalto) segue no canal do knockback: tranco que desliza e' o certo para quem foi empurrado.
    ///  - ESTRUTURA de kit (torreta, bobina): apanha de projetil INIMIGO que a toca (engolido como o Tear-Mae engole) e de
    ///    TerrainHit perto (o dano do muro: Dmg x Estrutura do elemento).
    /// </summary>
    public static class ApoioGrupoD
    {
        // ------------------------------------------------------------------ lado

        public static bool Inimigo(IEntidade dono, IEntidade e) =>
            e != null && e.Vital != null && e.Vital.Viva && !Combat.MesmoTime(dono, e);

        public static bool Aliado(IEntidade dono, IEntidade e) =>
            e != null && e != dono && e.Vital != null && e.Vital.Viva && Combat.MesmoTime(dono, e);

        /// <summary>O inimigo mais perto de `p` a ate' `raio` m (sem `exceto`). Aloca (AlvosPerto): so' em tique de 0,1 s.</summary>
        public static IEntidade MaisProximo(KitRunner k, Vector3 p, float raio, IEntidade exceto = null)
        {
            IEntidade melhor = null;
            float d2 = float.MaxValue;
            foreach (IEntidade e in k.AlvosPerto(p, raio, k.Dono))
            {
                if (e == exceto || !Inimigo(k.Dono, e)) continue;
                float d = (e.Pos - p).sqrMagnitude;
                if (d < d2) { d2 = d; melhor = e; }
            }
            return melhor;
        }

        /// <summary>O inimigo mais perto de `a` a ate' `raio` m do SEGMENTO a-b (no plano: encosta nao esconde ninguem), fora
        /// de `ja`. Aloca: so' em tique de 0,05 s.</summary>
        public static IEntidade NoSegmento(KitRunner k, Vector3 a, Vector3 b, float raio, ICollection<IEntidade> ja = null)
        {
            IEntidade melhor = null;
            float d2 = float.MaxValue;
            Vector3 pa = Plano(a), pb = Plano(b);
            foreach (IEntidade e in k.AlvosPerto((a + b) * 0.5f, Vector3.Distance(a, b) * 0.5f + raio + 1f, k.Dono))
            {
                if (!Inimigo(k.Dono, e) || (ja != null && ja.Contains(e))) continue;
                if (KitRunner.DistSegmento(Plano(e.Pos), pa, pb) > raio) continue;
                float d = (e.Pos - a).sqrMagnitude;
                if (d < d2) { d2 = d; melhor = e; }
            }
            return melhor;
        }

        /// <summary>Dentro do cone HORIZONTAL de meio-angulo `graus` em torno de `frente` (normalizada)?</summary>
        public static bool NoCone(Vector3 origem, Vector3 frente, Vector3 p, float graus)
        {
            Vector3 d = Plano(p - origem);
            if (d.sqrMagnitude < 1e-6f) return true;
            return Vector3.Dot(d.normalized, frente) >= Mathf.Cos(graus * Mathf.Deg2Rad);
        }

        /// <summary>Gira `d` em torno de Y (graus; positivo = horario visto de cima). A conta a mao do leque da Pyra.</summary>
        public static Vector3 Girar(Vector3 d, float graus)
        {
            float a = graus * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector3(d.x * c + d.z * s, 0f, -d.x * s + d.z * c);
        }

        public static Vector3 Plano(Vector3 v) => new Vector3(v.x, 0f, v.z);

        // ------------------------------------------------------------------ corpo

        /// <summary>EMPURRA um corpo inimigo `metros` pelo canal do knockback (decai a KNOCK_DECAY: v0 = raiz de 2ad). O tranco
        /// que desliza e' o de quem apanhou; dash de quem conjura e' IConjurador.Impulso.
        /// ponytail: `as Pawn` — o IConjurador nao tem Empurrar (o Gromm usa a mesma porta pela casca dele).</summary>
        public static void Empurrar(IEntidade e, Vector3 dir, float metros)
        {
            Pawn p = e as Pawn;
            dir = Plano(dir);
            if (p == null || dir.sqrMagnitude < 1e-6f) return;
            p.Empurrar(dir.normalized * Mathf.Sqrt(2f * Locomocao.KNOCK_DECAY * Mathf.Max(metros, 0f)));
        }

        // ------------------------------------------------------------------ vida

        /// <summary>DEVOLVE `q` de EHP no mesmo tique (o molde do Coracao de Fornalha: o Combat ja' cobrou, o kit repoe o que a
        /// regra dele nega): vida primeiro (grampeada em HpMax), a sobra no escudo (grampeado no nivel). Nada passa pelo Combat.</summary>
        public static void Devolver(KitRunner k, IEntidade e, float q)
        {
            if (e == null || e.Vital == null || !(q > 0f)) return;
            float cura = Mathf.Min(q, e.Vital.HpMax - e.Vital.Hp);
            Curar(k, e, cura);
            if (q - cura > 0.0001f) KitRunner.RegenerarEscudo(e, q - cura);
        }

        /// <summary>Cura de VIDA: a do dono pela porta do motor (avisa a HUD); a do aliado direto na Vitalidade (a HUD e' do player).</summary>
        public static void Curar(KitRunner k, IEntidade e, float q)
        {
            if (e == null || e.Vital == null || !(q > 0f)) return;
            if (e == k.Dono) k.DevolverDano(q);
            else e.Vital.Curar(q);
        }

        /// <summary>O corpo saiu da cena (Pawn destruido; o `==` do Unity). Fake de teste nunca "morre na cena".</summary>
        public static bool MortoNaCena(IEntidade e)
        {
            UnityEngine.Object o = e as UnityEngine.Object;
            return !ReferenceEquals(o, null) && o == null;
        }

        // ------------------------------------------------------------------ estrutura e tiros

        /// <summary>Dano de um TerrainHit numa estrutura de kit: o do muro (Dmg x Estrutura do elemento), dobrado se forte.</summary>
        public static float DanoDeGolpe(Elemento el, bool forte)
        {
            Balance.PerfilElemento p = Balance.Perfil(el);
            return p.Dmg * p.Estrutura * (forte ? 2f : 1f);
        }

        /// <summary>Projetil INIMIGO dentro do cilindro (raio x altura) da estrutura: sai da arena como o Tear-Mae engole
        /// (AoAbsorver apaga o VFX) e o dano dele x Estrutura volta. Varre por indice: zero lixo por quadro.</summary>
        public static float EngolirTiros(KitRunner k, Vector3 base_, float raio, float altura)
        {
            IList<Projetil> vivos = k.ProjeteisVivos != null ? k.ProjeteisVivos() : null;
            if (vivos == null) return 0f;
            float dano = 0f, r2 = raio * raio;
            for (int i = vivos.Count - 1; i >= 0; i--)
            {
                Projetil p = vivos[i];
                if (p == null || !p.Vivo || Combat.MesmoTime(k.Dono, p.Atirador)) continue;
                Vector3 d = p.Pos - base_;
                if (d.y < -0.6f || d.y > altura + 0.3f || d.x * d.x + d.z * d.z > r2) continue;
                vivos.RemoveAt(i);
                k.AoAbsorver?.Invoke(p);
                dano += p.Dano * Balance.Perfil(p.ElementoDoTiro).Estrutura;
            }
            return dano;
        }

        /// <summary>O projetil INIMIGO mais perto CHEGANDO no peito do dono (a ate' `raio` m e vindo na direcao dele). Por indice.</summary>
        public static Projetil TiroChegando(KitRunner k, float raio)
        {
            IList<Projetil> vivos = k.ProjeteisVivos != null ? k.ProjeteisVivos() : null;
            if (vivos == null) return null;
            Vector3 peito = k.Pos + Vector3.up;
            Projetil melhor = null;
            float d2 = raio * raio;
            for (int i = 0; i < vivos.Count; i++)
            {
                Projetil p = vivos[i];
                if (p == null || !p.Vivo || Combat.MesmoTime(k.Dono, p.Atirador)) continue;
                Vector3 d = peito - p.Pos;
                float dd = d.sqrMagnitude;
                if (dd > d2 || Vector3.Dot(d, p.Dir) <= 0f) continue;
                d2 = dd;
                melhor = p;
            }
            return melhor;
        }

        /// <summary>
        /// A ROTA de uma investida de kit (Mordida do Noctus, cada dash do Zip-Zag) — e o DASH que leva o corpo por ela: nascer
        /// a investida pede IConjurador.Impulso com a mesma distancia e o mesmo tempo, e a FRENTE anda a mesma integral do dash
        /// (Locomocao.PercorridoNoDash). Quem esta' a `raio` m do trecho JA' percorrido foi tocado — a investida viaja, nao
        /// nasce no alvo (GDD §4.1).
        /// </summary>
        public sealed class Investida
        {
            public readonly Vector3 Origem, Dir;
            public readonly float Alcance, Duracao;
            /// <summary>Null = sem desenho proprio (o Zip-Zag desenha a rota inteira no toque).</summary>
            public readonly EfeitoVisual Visual;
            public float Idade { get; private set; }

            public float Frente => Locomocao.PercorridoNoDash(Alcance, Duracao, Idade);
            public Vector3 Ponta => Origem + Dir * Frente;
            public bool Acabou => Idade >= Duracao;

            public Investida(KitRunner k, string tipo, Vector3 origem, Vector3 dir, float alcance, float largura)
            {
                Origem = origem; Dir = dir; Alcance = alcance;
                Duracao = Locomocao.DuracaoDoImpulso(alcance);
                k.Dono.Impulso(dir, alcance, Duracao);
                if (tipo != null) Visual = k.Visual(tipo, origem, origem, largura, Duracao + 0.35f);
            }

            public void Andar(float dt)
            {
                Idade += dt;
                if (Visual != null) Visual.Pos2 = Ponta;
            }

            public IEntidade Tocou(KitRunner k, float raio, ICollection<IEntidade> ja = null) => ApoioGrupoD.NoSegmento(k, Origem, Ponta, raio, ja);
        }

        /// <summary>
        /// RAIO GUIADO de kit (faisca da torreta, MEGABOBINA, Supercelula, faisca do Zip-Zag): VIAJA da origem ao PEITO do alvo,
        /// que persegue (GDD §4.1; raio e' quase-hitscan, nunca hitscan). Dano da FICHA pelo Combat, com a reacao do elemento
        /// antes (molhado conduz: +50% e atordoa). A contra-jogada do guiado: esquivar NA CHEGADA (i-frames = o raio passa),
        /// sair do alcance ANTES do disparo, ou quebrar quem dispara. Chegou: publica Raio no chao (agua conduz, §14).
        /// </summary>
        public sealed class RaioGuiado
        {
            /// <summary>s que o canal fica aceso depois de chegar: raio que some no quadro do acerto nao se ve'.</summary>
            public const float Lampejo = 0.25f;

            public readonly IEntidade Alvo;
            public readonly EfeitoVisual Visual;
            public Vector3 Pos { get; private set; }
            public bool Vivo { get; private set; } = true;
            public bool Acertou { get; private set; }
            private readonly IEntidade _dono;
            private readonly Vector3 _origem;
            private readonly float _dano, _vel, _rastro;
            private Vector3 _dirMov = Vector3.forward;

            /// <summary>`rastro` 0 = o CANAL inteiro da origem a' cabeca (raio de verdade); > 0 = faisca curta de `rastro` m.</summary>
            public RaioGuiado(KitRunner k, string tipo, Vector3 origem, IEntidade alvo, float dano, float vel, float largura, float rastro)
            {
                _dono = k.Dono; _origem = origem; Pos = origem; Alvo = alvo;
                _dano = dano; _vel = Mathf.Max(vel, 1f); _rastro = rastro;
                Visual = k.Visual(tipo, origem, origem, largura, 1f);
            }

            public static Vector3 Peito(IEntidade e) => e.Pos + Vector3.up;

            /// <summary>False = acabou (chegou ou o alvo sumiu). O desenho fica o Lampejo e o motor apaga.</summary>
            public bool Tick(float dt)
            {
                if (!Vivo) return false;
                if (Alvo == null || Alvo.Vital == null || !Alvo.Vital.Viva) { Fim(); return false; }
                Vector3 d = Peito(Alvo) - Pos;
                float passo = _vel * dt;
                if (d.sqrMagnitude <= passo * passo) { Pos = Peito(Alvo); Chegar(); return false; }
                _dirMov = d.normalized;
                Pos += _dirMov * passo;
                Desenhar();
                Visual.Restante = Visual.Duracao;   // vivo nao apaga (o motor desconta por tique)
                return true;
            }

            private void Desenhar()
            {
                Visual.Pos2 = Pos;
                Visual.Pos = _rastro > 0f ? Pos - _dirMov * Mathf.Min(_rastro, Vector3.Distance(Pos, _origem)) : _origem;
            }

            private void Chegar()
            {
                Desenhar();
                Fim();
                if (Efeitos.De(Alvo).IframesLeft > 0f) return;   // esquivou na hora: o raio passa
                float mult = Efeitos.Aplicar(Alvo, Elemento.Raio, _dano, _dono);
                Acertou = Combat.AplicarDano(Alvo, _dano * mult, Elemento.Raio, _dono) > 0f;
                Bus.EmitTerrainHit(Elemento.Raio, Alvo.Pos, false);
            }

            private void Fim()
            {
                Vivo = false;
                Visual.Restante = Mathf.Min(Visual.Restante, Lampejo);
            }
        }
    }
}
