using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Characters;

namespace Arkana.Tests
{
    /// <summary>Animacao procedural pura: aliases do Mage.gd, laco por clipe, poses que mudam, o beat do cast, a lei da patinacao.</summary>
    public class CharactersPoseTests
    {
        /// <summary>ANIM_ALIASES do Mage.gd, inclusive as variantes de caixa e o "mage_soell_cast" gravado no .glb da Meshy.</summary>
        static readonly Dictionary<string, Clipe> AliasesDoGodot = new Dictionary<string, Clipe>
        {
            { "idle", Clipe.Idle }, { "Idle", Clipe.Idle }, { "IDLE", Clipe.Idle }, { "Armature|Idle", Clipe.Idle },
            { "mixamo.com", Clipe.Idle }, { "standing_idle", Clipe.Idle }, { "breathing_idle", Clipe.Idle },
            { "idle_01", Clipe.Idle }, { "idle_loop", Clipe.Idle }, { "idle_02", Clipe.Idle }, { "idle_03", Clipe.Idle },
            { "run", Clipe.Run }, { "Run", Clipe.Run }, { "RUN", Clipe.Run }, { "Running", Clipe.Run }, { "running", Clipe.Run },
            { "Armature|Run", Clipe.Run }, { "Armature|Running", Clipe.Run }, { "Run Forward", Clipe.Run },
            { "running_forward", Clipe.Run }, { "locomotion_run", Clipe.Run }, { "sprint", Clipe.Run }, { "jog", Clipe.Run }, { "fast_run", Clipe.Run },
            { "cast", Clipe.Cast }, { "Cast", Clipe.Cast }, { "CAST", Clipe.Cast }, { "Spellcast", Clipe.Cast }, { "Spell Cast", Clipe.Cast },
            { "spell_cast", Clipe.Cast }, { "casting", Clipe.Cast }, { "magic_cast", Clipe.Cast }, { "Attack", Clipe.Cast }, { "attack", Clipe.Cast },
            { "attack1", Clipe.Cast }, { "Standing 1H Magic Attack", Clipe.Cast }, { "magic_attack", Clipe.Cast }, { "shoot", Clipe.Cast },
            { "fireball", Clipe.Cast }, { "Armature|Cast", Clipe.Cast }, { "Armature|Attack", Clipe.Cast },
            { "mage_soell_cast", Clipe.Cast }, { "mage_spell_cast", Clipe.Cast },
            { "cair", Clipe.Cair }, { "fall", Clipe.Cair }, { "Fall", Clipe.Cair }, { "falling", Clipe.Cair }, { "Falling", Clipe.Cair },
            { "freefall", Clipe.Cair }, { "free_fall", Clipe.Cair }, { "skydive", Clipe.Cair }, { "Skydiving", Clipe.Cair }, { "air", Clipe.Cair }, { "jump_loop", Clipe.Cair },
            { "planar", Clipe.Planar }, { "glide", Clipe.Planar }, { "Glide", Clipe.Planar }, { "gliding", Clipe.Planar }, { "Gliding", Clipe.Planar },
            { "parachute", Clipe.Planar }, { "wingsuit", Clipe.Planar }, { "hover", Clipe.Planar }, { "Hovering", Clipe.Planar }, { "flying", Clipe.Planar }, { "Flying", Clipe.Planar },
            { "pegar", Clipe.Pegar }, { "pickup", Clipe.Pegar }, { "Pickup", Clipe.Pegar }, { "pick_up", Clipe.Pegar }, { "Picking Up", Clipe.Pegar },
            { "grab", Clipe.Pegar }, { "Grab", Clipe.Pegar }, { "interact", Clipe.Pegar }, { "Interacting", Clipe.Pegar }, { "loot", Clipe.Pegar }, { "crouch_pickup", Clipe.Pegar },
            { "derrubado", Clipe.Derrubado }, { "downed", Clipe.Derrubado }, { "Downed", Clipe.Derrubado }, { "knocked", Clipe.Derrubado },
            { "Knocked Down", Clipe.Derrubado }, { "knockdown", Clipe.Derrubado }, { "crawl", Clipe.Derrubado }, { "Crawl", Clipe.Derrubado },
            { "crawling", Clipe.Derrubado }, { "Crawling", Clipe.Derrubado }, { "wounded", Clipe.Derrubado }, { "injured", Clipe.Derrubado },
            { "dying", Clipe.Derrubado }, { "getting_up", Clipe.Derrubado }, { "lying", Clipe.Derrubado },
            { "nadar", Clipe.Nadar }, { "swim", Clipe.Nadar }, { "Swim", Clipe.Nadar }, { "swimming", Clipe.Nadar }, { "Swimming", Clipe.Nadar },
            { "swim_forward", Clipe.Nadar }, { "Swim_Forward", Clipe.Nadar }, { "breaststroke", Clipe.Nadar }, { "freestyle", Clipe.Nadar },
            { "nadar_parado", Clipe.NadarParado }, { "swim_idle", Clipe.NadarParado }, { "Swim_Idle", Clipe.NadarParado }, { "treading", Clipe.NadarParado },
            { "treading_water", Clipe.NadarParado }, { "Water_Idle", Clipe.NadarParado }, { "float", Clipe.NadarParado }, { "floating", Clipe.NadarParado },
            { "ande_agachado", Clipe.AndeAgachado }, { "crouch_walk", Clipe.AndeAgachado },
        };

        [Test]
        public void TodoAliasDoMageGdResolveNoClipeCerto()
        {
            foreach (KeyValuePair<string, Clipe> kv in AliasesDoGodot)
            {
                Clipe? c = PoseMago.Alias(kv.Key);
                Assert.IsTrue(c.HasValue, "alias sem clipe: " + kv.Key);
                Assert.AreEqual(kv.Value, c.Value, kv.Key);
            }
            foreach (string a in PoseMago.TodosOsAliases()) Assert.IsTrue(PoseMago.Alias(a).HasValue, a);
        }

        [Test]
        public void NomeDesconhecidoOuVazioNaoResolve()
        {
            Assert.IsFalse(PoseMago.Alias("dance_macabre").HasValue);
            Assert.IsFalse(PoseMago.Alias("").HasValue);
            Assert.IsFalse(PoseMago.Alias(null).HasValue);
        }

        [Test]
        public void CastEPegarNaoRepetemOsSustentadosSim()
        {
            Assert.IsFalse(PoseMago.Laco(Clipe.Cast), "cast e' disparo unico: em laco o tiro sairia repetido");
            Assert.IsFalse(PoseMago.Laco(Clipe.Pegar));
            foreach (Clipe c in new[] { Clipe.Idle, Clipe.Run, Clipe.Cair, Clipe.Planar, Clipe.Derrubado, Clipe.Nadar, Clipe.NadarParado, Clipe.AndeAgachado })
                Assert.IsTrue(PoseMago.Laco(c), c.ToString());
        }

        [Test]
        public void TodoClipeTemDuracaoEPoseQueMudaComT()
        {
            foreach (Clipe c in PoseMago.Todos)
            {
                float dur = PoseMago.Duracao(c);
                Assert.Greater(dur, 0f, c.ToString());
                PoseCorpo a = PoseMago.Pose(c, 0f), b = PoseMago.Pose(c, dur * 0.4f);
                float delta = (a.Raiz - b.Raiz).magnitude + (a.BracoD - b.BracoD).magnitude + (a.BracoE - b.BracoE).magnitude
                    + (a.PernaE - b.PernaE).magnitude + (a.Cabeca - b.Cabeca).magnitude + (a.Tronco - b.Tronco).magnitude
                    + Mathf.Abs(a.BobY - b.BobY);
                Assert.Greater(delta, 0.5f, c + " esta' parado");
            }
        }

        [Test]
        public void LacoDaAVoltaEDisparoUnicoSeguraNoFim()
        {
            float d = PoseMago.Duracao(Clipe.Run);
            Assert.Less(Vector3.Distance(PoseMago.Pose(Clipe.Run, 0.1f).BracoD, PoseMago.Pose(Clipe.Run, d + 0.1f).BracoD), 1e-3f, "laco da a volta");
            float dc = PoseMago.Duracao(Clipe.Cast);
            Assert.Less(Vector3.Distance(PoseMago.Pose(Clipe.Cast, dc).BracoD, PoseMago.Pose(Clipe.Cast, dc + 5f).BracoD), 1e-3f, "disparo unico segura no fim");
            Assert.AreEqual(PoseMago.Pose(Clipe.Idle, 0f).BobY, PoseMago.Pose(Clipe.Idle, float.NaN).BobY, "NaN nao explode");
        }

        [Test]
        public void CastErgueOBracoAFrenteNoDisparoESeguraAteOFim()
        {
            PoseCorpo inicio = PoseMago.Pose(Clipe.Cast, 0f);
            PoseCorpo fogo = PoseMago.Pose(Clipe.Cast, PoseMago.CastFireT);
            PoseCorpo fim = PoseMago.Pose(Clipe.Cast, PoseMago.Duracao(Clipe.Cast));
            Assert.IsFalse(PoseMago.BracoDAFrente(inicio), "em t=0 o braco esta' abaixado");
            Assert.Greater(inicio.BracoD.x, -10f);
            Assert.IsTrue(PoseMago.BracoDAFrente(fogo), "no CastFireT a mao esta' a frente");
            Assert.IsTrue(PoseMago.BracoDAFrente(fim), "o clipe termina com o braco a frente; quem abaixa e' o blend de volta");
            Assert.Greater(fogo.MaoD, 1f, "a mao ABRE no disparo");
            Assert.Greater(PoseMago.Duracao(Clipe.Cast), PoseMago.CastFireT, "o disparo cabe dentro do cast");
        }

        [Test]
        public void DerrubadoFicaNoChaoApoiadoNumBraco()
        {
            float d = PoseMago.Duracao(Clipe.Derrubado);
            for (float t = 0f; t <= d; t += d / 16f)
            {
                PoseCorpo p = PoseMago.Pose(Clipe.Derrubado, t);
                Assert.LessOrEqual(p.BobY, -0.4f, "corpo no chao em t=" + t);
                Assert.Less(p.BracoE.z, -45f, "braco esquerdo aberto apoiando em t=" + t);
            }
            PoseCorpo planar = PoseMago.Pose(Clipe.Planar, 0.5f);
            Assert.Greater(planar.Raiz.x, 45f, "planar: de barriga");
            Assert.Greater(planar.BracoD.z, 60f, "planar: bracos abertos");
        }

        [Test]
        public void EscalaDeCorridaSaiDaVelocidadeEGrampeia()
        {
            float ms1 = Balance.Anim.RunStrideM / PoseMago.Duracao(Clipe.Run);
            Assert.AreEqual(1f, PoseMago.EscalaDeCorrida(ms1), 1e-4f, "escala 1 percorre RunStrideM por ciclo");
            Assert.AreEqual(Balance.Anim.ScaleMin, PoseMago.EscalaDeCorrida(0f));
            Assert.AreEqual(Balance.Anim.ScaleMin, PoseMago.EscalaDeCorrida(float.NaN));
            Assert.AreEqual(Balance.Anim.ScaleMax, PoseMago.EscalaDeCorrida(1000f));
            Assert.Greater(PoseMago.EscalaDeCorrida(ms1 * 1.2f), PoseMago.EscalaDeCorrida(ms1));
        }

        [Test]
        public void HistereseIdleRunNaoPisca()
        {
            float meio = (Balance.Move.RunAnimEnter + Balance.Move.RunAnimExit) * 0.5f;
            Assert.IsFalse(PoseMago.Correndo(false, meio), "parado no meio da banda continua parado");
            Assert.IsTrue(PoseMago.Correndo(true, meio), "correndo no meio da banda continua correndo");
            Assert.IsTrue(PoseMago.Correndo(false, Balance.Move.RunAnimEnter + 0.01f));
            Assert.IsFalse(PoseMago.Correndo(true, Balance.Move.RunAnimExit - 0.01f));
        }

        [Test]
        public void LerpDePosesMisturaEGrampeia()
        {
            PoseCorpo a = PoseMago.Pose(Clipe.Idle, 0f), b = PoseMago.Pose(Clipe.Cast, PoseMago.CastFireT);
            PoseCorpo m = PoseMago.Lerp(a, b, 0.5f);
            Assert.AreEqual((a.BracoD.x + b.BracoD.x) * 0.5f, m.BracoD.x, 1e-4f);
            Assert.AreEqual(b.BracoD, PoseMago.Lerp(a, b, 2f).BracoD);
        }

        [Test]
        public void SubstitutoDeOpcionalAusenteNuncaDeixaSemPose()
        {
            Assert.AreEqual(Clipe.Cast, PoseMago.Substituto(Clipe.Pegar));
            Assert.AreEqual(Clipe.Run, PoseMago.Substituto(Clipe.Nadar), "meio submerso com a passada: nao regride");
            Assert.AreEqual(Clipe.Idle, PoseMago.Substituto(Clipe.Derrubado));
        }
    }
}
