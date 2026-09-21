using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.Core;
using Arkana.Gameplay;
using Arkana.World;

namespace Arkana.Tests
{
    /// <summary>
    /// ONDA 18A — o que o acabamento da Sintonia promete, na LOGICA PURA da casca (VisualDaSintonia.cs): a zona DEITADA no
    /// relevo (o disco plano cortava a ladeira e a onda de choque enterrada virava o "arco branco solto"), a borda que esmaece,
    /// o chao desenhado com memoria, o pool que recicla sem duplicar e o ciclo que nao deixa NADA na tela depois do fim.
    /// Sem chamada interna do Unity (menos o ultimo, do asset): rodam tambem no mini-runner. Cada um fica vermelho com o
    /// defeito anotado nele.
    /// </summary>
    public class GameplayVisualDaSintoniaTests
    {
        const float Tol = 1e-4f;

        /// <summary>Uma LADEIRA de verdade: 40% em x, 10% em z (sobe ~7 m de um lado ao outro de uma zona de 9 m).</summary>
        static float Ladeira(float x, float z) => 3f + 0.4f * x + 0.1f * z;

        static void Deitar(Vector3 centro, float raio, System.Func<float, float, float> chao, float piso,
            out Vector3[] v, out Vector2[] uv, out Color32[] cor, out int aneis, out int gomos)
        {
            aneis = ZonaNoRelevo.Aneis(raio);
            gomos = ZonaNoRelevo.Gomos(raio);
            int n = ZonaNoRelevo.Vertices(aneis, gomos);
            v = new Vector3[n]; uv = new Vector2[n]; cor = new Color32[n];
            ZonaNoRelevo.Amostrar(centro, raio, aneis, gomos, chao, piso, v, uv, cor);
        }

        /// <summary>Vermelho se a altura for perguntada no CENTRO (o disco chapado que corta a ladeira) em vez de no vertice.</summary>
        [Test]
        public void Zona_NaLadeira_CadaVerticeNoChaoDoSeuPonto()
        {
            var centro = new Vector3(10f, 99f, -4f);   // o y do centro nao vale nada: o chao manda
            Deitar(centro, 9f, Ladeira, Relevo.Seco, out Vector3[] v, out _, out _, out _, out _);
            float min = float.MaxValue, max = float.MinValue;
            for (int i = 0; i < v.Length; i++)
            {
                float esperado = Ladeira(centro.x + v[i].x, centro.z + v[i].z) + ZonaNoRelevo.Acima;
                Assert.AreEqual(esperado, v[i].y, Tol, "vertice " + i + " fora do chao do seu ponto");
                min = Mathf.Min(min, v[i].y); max = Mathf.Max(max, v[i].y);
            }
            Assert.Greater(max - min, 6f, "a zona acompanha a ladeira inteira (sobe e desce com ela)");
        }

        /// <summary>Vermelho se o `piso` (lamina d'agua) for ignorado: a lava/lama afundaria no lago.</summary>
        [Test]
        public void Zona_NaAgua_BoiaNaLamina()
        {
            Deitar(Vector3.zero, 5f, (x, z) => -2f, 0.5f, out Vector3[] v, out _, out _, out _, out _);
            for (int i = 0; i < v.Length; i++) Assert.AreEqual(0.5f + ZonaNoRelevo.Acima, v[i].y, Tol, "vertice " + i + " no fundo do lago");
        }

        /// <summary>Vermelho se o alfa radial inverter, sumir (borda dura = adesivo) ou o ultimo anel nao chegar ao raio.</summary>
        [Test]
        public void Zona_BordaEsmaece_MioloCheio_OAnelDeForaNoRaio()
        {
            const float raio = 10f;
            Deitar(Vector3.zero, raio, (x, z) => 0f, Relevo.Seco, out Vector3[] v, out Vector2[] uv, out Color32[] cor, out int aneis, out int gomos);
            Assert.AreEqual(255, cor[0].a, "o miolo e' cheio");
            Assert.AreEqual(0, cor[0].r, "o miolo e' o raio 0");
            int anterior = 256, raioAnt = -1;
            for (int a = 1; a <= aneis; a++)
            {
                int i = 1 + (a - 1) * gomos;
                Assert.LessOrEqual(cor[i].a, anterior, "o alfa nunca SOBE para fora (anel " + a + ")");
                Assert.Greater(cor[i].r, raioAnt, "o raio relativo cresce para fora (anel " + a + ")");
                anterior = cor[i].a; raioAnt = cor[i].r;
                float d = new Vector2(v[i].x, v[i].z).magnitude;
                Assert.AreEqual(raio * a / aneis, d, 1e-3f, "anel " + a + " no raio dele");
                Assert.AreEqual(v[i].x, uv[i].x, Tol, "o uv e' metro no plano (o ruido tem tamanho de mundo)");
            }
            int fora = 1 + (aneis - 1) * gomos;
            Assert.AreEqual(0, cor[fora].a, "a borda de fora some (alfa 0)");
            Assert.AreEqual(255, cor[fora].r, "a borda de fora e' o raio 1");
            Assert.AreEqual(1f, ZonaNoRelevo.Fade(0.5f), Tol, "metade do raio ainda e' zona cheia");
            Assert.Greater(ZonaNoRelevo.Fade(0.85f), 0.05f, "a borda desce aos poucos...");
            Assert.Less(ZonaNoRelevo.Fade(0.85f), 0.95f, "...nao de estalo");
        }

        /// <summary>Vermelho se o ultimo gomo nao fechar no primeiro (indice fora da malha) ou sobrar vertice sem triangulo.</summary>
        [Test]
        public void Zona_Triangulos_FechamOAnelSemBuraco()
        {
            foreach (float raio in new[] { 1.9f, 5f, 12f })
            {
                int an = ZonaNoRelevo.Aneis(raio), go = ZonaNoRelevo.Gomos(raio), n = ZonaNoRelevo.Vertices(an, go);
                int[] t = ZonaNoRelevo.Triangulos(an, go);
                Assert.AreEqual(0, t.Length % 3);
                Assert.AreEqual(go * (2 * an - 1), t.Length / 3, "leque + duas por gomo entre aneis");
                var usado = new bool[n];
                foreach (int i in t) { Assert.That(i, Is.InRange(0, n - 1), "indice fora da malha (raio " + raio + ")"); usado[i] = true; }
                for (int i = 0; i < n; i++) Assert.IsTrue(usado[i], "vertice " + i + " sem triangulo (raio " + raio + ")");
                // costura: triangulo so' liga aneis vizinhos, e cada vertice do 1o anel fecha DOIS gomos do leque (sem fresta)
                System.Func<int, int> anel = i => i == 0 ? 0 : (i - 1) / go + 1;
                var noLeque = new int[go + 1];
                for (int k = 0; k < t.Length; k += 3)
                {
                    int a0 = anel(t[k]), a1 = anel(t[k + 1]), a2 = anel(t[k + 2]);
                    Assert.LessOrEqual(Mathf.Max(a0, Mathf.Max(a1, a2)) - Mathf.Min(a0, Mathf.Min(a1, a2)), 1, "triangulo pulando anel (raio " + raio + ")");
                    if (a0 + a1 + a2 == 2) for (int j = 0; j < 3; j++) if (t[k + j] > 0) noLeque[t[k + j]]++;
                }
                for (int i = 1; i <= go; i++) Assert.AreEqual(2, noLeque[i], "o leque nao fecha no vertice " + i + " (raio " + raio + ")");
            }
        }

        /// <summary>Vermelho se a memoria pular a divisao de triangulo da Ilha (Altura exata ou a outra diagonal): a zona
        /// boiaria/enterraria ate' 0,3 m nas dobras da malha desenhada.</summary>
        [Test]
        public void ChaoMemo_IgualAMalhaDesenhada()
        {
            var r = new Relevo(2f, 7);
            var grade = new GradeDoChao(r);
            var memo = new ChaoDesenhadoMemo(r);
            float pior = 0f;
            for (int k = 0; k < 400; k++)
            {
                float x = Mathf.Sin(k * 12.9898f) * 180f, z = Mathf.Sin(k * 78.233f + 1.3f) * 180f;
                pior = Mathf.Max(pior, Mathf.Abs(memo.Altura(x, z) - grade.AlturaNaMalha(x, z)));
            }
            Assert.Less(pior, 1e-3f, "a memoria difere da malha desenhada");
            Assert.AreEqual(memo.Altura(33.3f, -12.1f), memo.Altura(33.3f, -12.1f), "a 2a pergunta (da memoria) e' igual a 1a");
        }

        /// <summary>Vermelho se o Devolver repetido empilhar o item duas vezes: dois efeitos pegariam o MESMO objeto.</summary>
        [Test]
        public void Pool_Recicla_ENaoDuplica()
        {
            var mostrados = new List<bool>();
            var pool = new PoolVfx<object>(tipo => new object(), (o, on) => mostrados.Add(on));
            object a = pool.Pegar(3);
            Assert.AreEqual(1, pool.Emprestados);
            Assert.IsTrue(pool.Devolver(3, a), "devolve");
            Assert.IsFalse(pool.Devolver(3, a), "devolver de novo nao faz nada");
            Assert.IsFalse(pool.Devolver(3, null), "nulo nao faz nada");
            Assert.AreEqual(0, pool.Emprestados);
            object b = pool.Pegar(3), c = pool.Pegar(3);
            Assert.AreSame(a, b, "o devolvido volta (pool)");
            Assert.AreNotSame(b, c, "o mesmo item nunca sai para dois");
            Assert.AreEqual(2, pool.Criados, "so' cria quando falta");
            Assert.AreNotSame(pool.Pegar(4), b, "tipo diferente nao recicla o outro");
            CollectionAssert.AreEqual(new[] { true, false, true, true, true }, mostrados, "mostra ao pegar, esconde ao devolver (uma vez)");
        }

        /// <summary>Vermelho se o golpe vencido esquecer a FORMA (ou a onda) fora do pool: ficaria na tela para sempre.</summary>
        [Test]
        public void Golpe_Vencido_DevolveOndaEForma()
        {
            var pool = new PoolVfx<object>(tipo => new object(), null);
            var golpes = new List<GolpeDaSintonia<object>>
            {
                new GolpeDaSintonia<object> { Combo = ComboSintonia.Eletrocussao, Dur = 2f, Resta = 2f, TipoOnda = 14, Onda = pool.Pegar(14), TipoForma = 24, Forma = pool.Pegar(24) },
                new GolpeDaSintonia<object> { Combo = ComboSintonia.ExplosaoDePlasma, Dur = 0.7f, Resta = 0.7f, TipoOnda = 12, Onda = pool.Pegar(12) },
            };
            Assert.AreEqual(0, CicloDaSintonia.Vencer(golpes, 0.5f, pool));
            Assert.AreEqual(3, pool.Emprestados, "no meio, tudo na tela");
            Assert.AreEqual(1, CicloDaSintonia.Vencer(golpes, 0.5f, pool), "o plasma venceu");
            Assert.AreEqual(2, pool.Emprestados);
            Assert.AreEqual(1, CicloDaSintonia.Vencer(golpes, 1.1f, pool), "a eletrocussao venceu");
            Assert.AreEqual(0, golpes.Count);
            Assert.AreEqual(0, pool.Emprestados, "depois do fim NADA sobra");
        }

        static SintoniaEfeitos.Persistente Zona(ComboSintonia c, float dur) =>
            new SintoniaEfeitos.Persistente { Combo = c, Raio = 9f, Duracao = dur, Restante = dur };

        /// <summary>O que ficou: entra com item, sai esmaecendo e devolve. Vermelho se a saida so' olhar o Restante — o fim de
        /// partida ZERA a lista sem mexer no Restante e a zona ficaria desenhada para sempre (o anel "que nao some").</summary>
        [Test]
        public void Persistente_QueSai_SomeSemSobrar_InclusiveNoFimDaPartida()
        {
            var pool = new PoolVfx<object>(tipo => new object(), null);
            var vivos = new Dictionary<SintoniaEfeitos.Persistente, object>();
            var mortos = new List<SintoniaEfeitos.Persistente>();
            var golpes = new List<GolpeDaSintonia<object>>();
            var magma = Zona(ComboSintonia.ChuvaDeMagma, 8f);
            var lama = Zona(ComboSintonia.Lamacal, 12f);
            var mina = Zona(ComboSintonia.CristaisCarregados, 15f);
            var ativos = new List<SintoniaEfeitos.Persistente> { magma, lama, mina };

            CicloDaSintonia.Sincronizar(ativos, vivos, mortos, golpes, pool);
            CicloDaSintonia.Sincronizar(ativos, vivos, mortos, golpes, pool);
            Assert.AreEqual(3, pool.Emprestados, "um item por zona, sem repetir no 2o quadro");
            Assert.AreEqual(0, golpes.Count);

            // o magma ACABOU (a regra o tirou da lista): esmaece SumirS e devolve
            magma.Restante = 0f;
            ativos.Remove(magma);
            CicloDaSintonia.Sincronizar(ativos, vivos, mortos, golpes, pool);
            CollectionAssert.AreEqual(new[] { magma }, mortos);
            Assert.AreEqual(1, golpes.Count, "sai ESMAECENDO, nao de estalo");
            Assert.AreSame(magma, golpes[0].Fonte);
            CicloDaSintonia.Vencer(golpes, CicloDaSintonia.SumirS * 0.5f, pool);
            Assert.AreEqual(3, pool.Emprestados, "no meio do esmaecer, ainda desenhado");
            CicloDaSintonia.Vencer(golpes, CicloDaSintonia.SumirS * 0.5f + 0.01f, pool);
            Assert.AreEqual(2, pool.Emprestados, "esmaeceu: devolvido");

            // a mina QUEBROU: some no ato (o estilhaco e' da casca)
            mina.Quebrou = true;
            mina.Restante = 0f;
            CicloDaSintonia.Sincronizar(ativos, vivos, mortos, golpes, pool);
            CicloDaSintonia.Vencer(golpes, 0.001f, pool);
            Assert.AreEqual(1, pool.Emprestados, "a mina quebrada sai no mesmo quadro");

            // FIM DA PARTIDA: SintoniaEfeitos.Reset zera a lista; a lama ainda tem Restante > 0
            ativos.Clear();
            CicloDaSintonia.Sincronizar(ativos, vivos, mortos, golpes, pool);
            CicloDaSintonia.Vencer(golpes, CicloDaSintonia.SumirS + 0.01f, pool);
            Assert.Greater(lama.Restante, 0f, "(a Persistente nem sabe que acabou)");
            Assert.AreEqual(0, vivos.Count);
            Assert.AreEqual(0, golpes.Count);
            Assert.AreEqual(0, pool.Emprestados, "depois do fim NADA sobra na tela");
        }

        /// <summary>O shader novo chega ao APK: o "Strip Unused" leva so' o que um material-ASSET pede (Shader.Find em runtime
        /// devolve null no aparelho). Vermelho se o asset sumir, apontar outro shader (meta regenerado) ou o shader perder uma
        /// propriedade que a casca escreve. Interno do Unity: roda no EditMode do editor (no mini-runner e' falha de ambiente).</summary>
        [Test]
        public void MaterialDaZona_AssetNoResources_ComOShaderEAsPropriedades()
        {
            Material m = Resources.Load<Material>(VisualDaSintonia.MaterialZona);
            Assert.IsNotNull(m, "falta Resources/" + VisualDaSintonia.MaterialZona + ".mat");
            Assert.IsNotNull(m.shader);
            Assert.AreEqual(VisualDaSintonia.ShaderZona, m.shader.name, "o material tem de apontar o Arkana/SintoniaZona");
            foreach (string p in VisualDaSintonia.PropsDaZona) Assert.IsTrue(m.HasProperty(p), "o shader nao tem " + p);
        }
    }
}
