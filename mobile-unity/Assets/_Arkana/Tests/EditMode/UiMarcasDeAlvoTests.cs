using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.UI;

namespace Arkana.Tests
{
    /// <summary>Marcas de alvo, puras: aparecem no acerto (e sob a mira, sem acerto), seguram, esvaecem e somem; o rastro
    /// segura o valor velho e desce; nada atras da camera, alem de 60 m ou morto; as mais perto primeiro e no maximo seis.</summary>
    public class UiMarcasDeAlvoTests
    {
        [Test]
        public void MarcaApareceNoAcertoENaMiraOrdenaPorDistanciaETemTeto()
        {
            var l = new MarcasLogica();
            var eu = new FakeEntidade("Eu", Vector3.zero, true);
            var arena = new List<IEntidade> { eu };
            Vector3 olho = new Vector3(0f, 0.9f, 0f), frente = Vector3.forward;   // a camera no meio do corpo, olhando +z
            void Quadro(float dt) => l.Atualizar(dt, arena, eu, olho, frente);

            // 1) fora da mira e sem acerto: nada; acertou: aparece, segura SeguraS e esvaece em SomeS
            var a = new FakeEntidade("Pyra", new Vector3(8f, 0f, 10f));
            arena.Add(a);
            Quadro(0.1f);
            Assert.AreEqual(0, l.Visiveis.Count, "fora da mira e sem acerto: nada na tela");
            l.Acertou(a);
            float acerto = l.Agora;
            Quadro(0.1f);
            Assert.AreEqual(1, l.Visiveis.Count);
            Assert.AreSame(a, l.Visiveis[0].Alvo);
            Assert.AreEqual(1f, l.Visiveis[0].Alfa, 1e-4f);
            Assert.IsFalse(l.Visiveis[0].NaMira, "8 m de lado nao e' mira");

            // 2) o RASTRO: o golpe derruba a vida, o fantasma segura o valor velho e depois desce ate' ela
            a.Vital.Hp = 60f;
            Quadro(0.1f);
            MarcasLogica.Marca m = l.Visiveis[0];
            Assert.AreEqual(0.6f, m.Vida.Frac, 1e-4f);
            Assert.AreEqual(1f, m.Vida.Fantasma, 1e-4f, "o rastro segura o valor de antes do golpe");
            Quadro(MarcasLogica.RastroEsperaS * 0.5f);
            Assert.AreEqual(1f, m.Vida.Fantasma, 1e-4f, "ainda segurando");
            Quadro(MarcasLogica.RastroEsperaS);
            Quadro(0.1f);
            Assert.Less(m.Vida.Fantasma, 1f, "passou a espera: desce");
            Assert.Greater(m.Vida.Fantasma, 0.6f);
            Quadro(1f);
            Assert.AreEqual(0.6f, m.Vida.Fantasma, 1e-4f, "e assenta no valor atual");
            a.Vital.Hp = 90f;
            Quadro(0.1f);
            Assert.AreEqual(0.9f, m.Vida.Fantasma, 1e-4f, "cura sobe sem rastro");

            // 3) some sozinha: SeguraS inteiro depois do acerto e o esvaecer
            Quadro(acerto + MarcasLogica.SeguraS + MarcasLogica.SomeS * 0.5f - l.Agora);
            Assert.AreEqual(1, l.Visiveis.Count);
            Assert.AreEqual(0.5f, m.Alfa, 1e-3f, "no meio do esvaecer");
            Quadro(MarcasLogica.SomeS);
            Assert.AreEqual(0, l.Visiveis.Count, "o acerto velho some");

            // 4) a MIRA: no raio da camera ate' MiraM aparece sem acerto; alem disso, nao
            var b = new FakeEntidade("Veu", new Vector3(0.4f, 0f, 30f));
            arena.Add(b);
            Quadro(0.1f);
            Assert.AreEqual(1, l.Visiveis.Count, "sob a mira aparece sem acerto");
            Assert.AreSame(b, l.Visiveis[0].Alvo);
            Assert.IsTrue(l.Visiveis[0].NaMira);
            l.Visada = (de, ate) => false;   // uma pedra entre a camera e ele
            Quadro(MarcasLogica.MiraSeguraS + MarcasLogica.SomeS + 0.01f);
            Assert.AreEqual(0, l.Visiveis.Count, "atras da pedra a mira nao acende ninguem (nada de wallhack)");
            l.Visada = null;
            b.Pos = new Vector3(0f, 0f, MarcasLogica.MiraM + 5f);
            Quadro(0.1f);
            Assert.AreEqual(0, l.Visiveis.Count, "mira alem de MiraM nao conta");

            // 5) nem acertado: atras da camera ou alem de LongeM
            b.Pos = new Vector3(0f, 0f, MarcasLogica.LongeM + 5f);
            a.Pos = new Vector3(2f, 0f, -10f);
            l.Acertou(a); l.Acertou(b);
            Quadro(0.1f);
            Assert.AreEqual(0, l.Visiveis.Count, "atras da camera e alem de 60 m: nada");

            // 6) ordem por distancia e TETO: oito acertados embaralhados, ficam os seis mais perto, do perto para o longe
            arena.Clear(); arena.Add(eu);
            float[] zs = { 35f, 12f, 50f, 5f, 28f, 44f, 19f, 9f };
            var alvos = new List<FakeEntidade>();
            foreach (float z in zs)
            {
                var f = new FakeEntidade("z" + z, new Vector3(3f, 0f, z));
                arena.Add(f); alvos.Add(f);
                l.Acertou(f);
            }
            Quadro(0.1f);
            Assert.AreEqual(MarcasLogica.Max, l.Visiveis.Count, "teto de marcas");
            for (int i = 1; i < l.Visiveis.Count; i++) Assert.LessOrEqual(l.Visiveis[i - 1].Dist, l.Visiveis[i].Dist, "da mais perto para a mais longe");
            Assert.AreEqual("z5", l.Visiveis[0].Alvo.Nome);
            Assert.AreEqual("z35", l.Visiveis[MarcasLogica.Max - 1].Alvo.Nome, "as duas mais longe (44 e 50 m) ficam de fora");

            // 7) MORREU: sai na hora, e a mira nao traz de volta quem acabou de levantar (o boneco do treino)
            var perto = alvos[3];   // z5
            perto.Pos = new Vector3(0f, 0f, 5f);   // bem no raio da camera
            perto.Vital.Hp = 0f;
            l.Esquecer(perto);
            Quadro(0.1f);
            Assert.AreEqual("z9", l.Visiveis[0].Alvo.Nome, "morto nao tem marca");
            perto.Vital.Reset();
            Quadro(0.1f);
            Assert.AreNotSame(perto, l.Visiveis[0].Alvo, "levantou sob a mira, mas acabou de morrer: calado");
            Quadro(MarcasLogica.SeguraS);
            Assert.AreSame(perto, l.Visiveis[0].Alvo, "passou o calado: a mira volta a mostrar");
            Assert.IsTrue(l.Visiveis[0].NaMira);
        }
    }
}
