using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Arkana.World;

namespace Arkana.Tests
{
    /// <summary>
    /// ONDA 14A — o que o chao da mata promete, na classe PURA (Mata.Plantio). Cada teste fica vermelho reintroduzindo o defeito
    /// que ele guarda (anotado em cada um). Sem chamada interna do Unity: rodam tambem na sonda — por isso as "arvores" do
    /// fixture sao uma grade sintetica na mata (o PlantioDasArvores monta TRS, que e' interno) com o MESMO raio que a Vegetacao
    /// registra (tronco 0,38 m + folga = 0,6 m).
    /// </summary>
    public class WorldMataTests
    {
        static Relevo _relevo;
        static GradeDoChao _grade;
        static List<Vector4> _ocupados;
        static List<Mata.PecaPlantada> _mata;

        /// <summary>O BRILHO sobrevive ao build: o build descarta a variante de instancing que nenhum material-asset pede ("Strip
        /// Unused"). O molde do cristal/cogumelo e' um asset com _EMISSION e instancing ligados, no Lit. Vermelho se o asset sumir,
        /// perder a keyword, o instancing ou o shader.</summary>
        [Test]
        public void MoldeDoBrilho_AssetComEmissaoEInstancing()
        {
            Material m = Resources.Load<Material>(Vegetacao.MoldeBrilho);
            Assert.IsNotNull(m, "falta Resources/" + Vegetacao.MoldeBrilho + ".mat");
            Assert.AreEqual("Universal Render Pipeline/Lit", m.shader.name, "o molde tem de ser o Lit (o MaterialDaMeshy recusa outro)");
            Assert.IsTrue(m.IsKeywordEnabled("_EMISSION"), "sem _EMISSION no ASSET a variante sai do APK e o cristal apaga");
            Assert.IsTrue(m.enableInstancing, "sem instancing no asset a variante instanciada sai do APK");
        }

        static Relevo Relevo2 => _relevo ?? (_relevo = new Relevo(2f, 7));
        static GradeDoChao Grade => _grade ?? (_grade = new GradeDoChao(Relevo2));

        /// <summary>Arvores de mentira a cada 4,4 m (a distancia da mata fechada) e uns pedregulhos, em volta da floresta.</summary>
        static List<Vector4> Ocupados(Relevo r)
        {
            var l = new List<Vector4>();
            float R = Mata.RaioDaMata(r) + 6f;
            for (float x = -R; x <= R; x += 4.4f)
                for (float z = -R; z <= R; z += 4.4f)
                {
                    var p = r.Floresta + new Vector2(x + 0.7f * Mathf.Sin(z), z + 0.7f * Mathf.Cos(x));
                    if (Vector2.Distance(p, r.Floresta) > R || r.BiomaEm(p.x, p.y) != Bioma.Floresta) continue;
                    l.Add(new Vector4(p.x, p.y, 0f, 0.6f));
                }
            for (int i = 0; i < 6; i++)
            {
                var p = r.Floresta + new Vector2(Mathf.Cos(i * 1.1f), Mathf.Sin(i * 1.1f)) * (8f + 7f * i);
                l.Add(new Vector4(p.x, p.y, 0f, 1.2f + 0.2f * i));
            }
            return l;
        }

        /// <summary>O que o chao da mata respeita: as arvores e pedras do fixture e o KIT (o Plantio soma as pegadas dele sozinho).</summary>
        static List<Vector4> OcupadosDaIlha
        {
            get
            {
                if (_ocupados != null) return _ocupados;
                _ocupados = Ocupados(Relevo2);
                _ocupados.AddRange(Mata.PegadasDoKit(Relevo2));
                return _ocupados;
            }
        }
        static List<Mata.PecaPlantada> MataDaIlha => _mata ?? (_mata = Mata.Plantio(Relevo2, Ocupados(Relevo2)));

        static Vector3 Pe(Mata.PecaPlantada p) => p.M.GetColumn(3);
        static Vector2 Xz(Mata.PecaPlantada p) { Vector3 c = Pe(p); return new Vector2(c.x, c.z); }

        static float DistH(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        static List<Mata.PecaPlantada> Do(Mata.Peca t) => MataDaIlha.FindAll(p => p.Tipo == t);

        /// <summary>O ponto (x, z) no referencial do molde da peca (X ao longo do tronco, Z de lado), em metros do molde.</summary>
        static Vector2 NoMolde(Mata.PecaPlantada p, Vector2 xz)
        {
            Vector3 d = new Vector3(xz.x, 0f, xz.y) - Pe(p);
            Vector3 ex = p.M.GetColumn(0), ez = p.M.GetColumn(2);
            ex.y = ez.y = 0f;
            return new Vector2(Vector3.Dot(d, ex) / ex.sqrMagnitude, Vector3.Dot(d, ez) / ez.sqrMagnitude);
        }

        [Test]
        public void Mata_SoNoBiomaFlorestaOuNaBordaDele_NuncaNaAguaNemNaPraia()
        {
            // defeitos: plantar pela distancia ao centro so' (peca na campina, na encosta de rocha, na praia baixa do canto), trocar o
            // bioma (Campina no lugar de Floresta), tirar o teto da borda (a mata escorre 20 m campina adentro)
            Relevo r = Relevo2;
            int dentro = 0;
            foreach (Mata.PecaPlantada p in MataDaIlha)
            {
                Vector2 xz = Xz(p);
                Bioma b = r.BiomaEm(xz.x, xz.y);
                Assert.IsTrue(b == Bioma.Floresta || b == Bioma.Campina, p.Tipo + " em " + b + " em " + Pe(p));
                Assert.IsTrue(Mata.NaBorda(r, xz, 0f), p.Tipo + " fora da mata e da borda dela em " + Pe(p));
                Assert.AreEqual(Relevo.Seco, r.SuperficieDaAgua(xz.x, xz.y), p.Tipo + " na agua em " + Pe(p));
                Assert.GreaterOrEqual(r.Altura(xz.x, xz.y), Relevo.PraiaY, p.Tipo + " na praia em " + Pe(p));
                if (b == Bioma.Floresta) dentro++;
            }
            Assert.Greater(dentro, MataDaIlha.Count * 0.8f, "a maior parte fica no bioma (a borda e' rala): " + dentro + " de " + MataDaIlha.Count);
            Assert.Less(dentro, MataDaIlha.Count, "a borda existe: alguma peca vaza para a campina");
        }

        [Test]
        public void Mata_ContagensDaIlhaDe600m()
        {
            // defeitos: contagem cravada sem x AREA, sorteio que desiste cedo (a mata volta a ficar vazia), cristal demais (vira feira)
            Assert.That(Do(Mata.Peca.Cristal).Count, Is.InRange(12, 20), "cristais");
            Assert.That(Do(Mata.Peca.Tronco).Count, Is.InRange(25, 40), "troncos caidos");
            Assert.That(Do(Mata.Peca.Cogumelo).Count, Is.InRange(80, 140), "cachos de cogumelo");
            Assert.That(Do(Mata.Peca.Samambaia).Count, Is.InRange(200, 300), "touceiras de samambaia");
            var pequena = new Relevo(1f, 7);
            List<Mata.PecaPlantada> m = Mata.Plantio(pequena, Ocupados(pequena));
            Assert.That(m.FindAll(p => p.Tipo == Mata.Peca.Cristal).Count, Is.InRange(3, 5), "a ilha de 300 m tem 3-5 cristais");
            Assert.That(m.FindAll(p => p.Tipo == Mata.Peca.Tronco).Count, Is.InRange(6, 10), "a ilha de 300 m tem 6-10 troncos");
        }

        [Test]
        public void Mata_DeterministicaPorSeed()
        {
            // defeito: System.Random / relogio -> duas partidas com a mesma seed com matas diferentes
            List<Mata.PecaPlantada> a = MataDaIlha, b = Mata.Plantio(new Relevo(2f, 7), Ocupados(new Relevo(2f, 7)));
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Tipo, b[i].Tipo, "#" + i);
                Assert.IsTrue(a[i].M == b[i].M, "#" + i + " em " + Pe(a[i]));
            }
            List<Mata.PecaPlantada> c = Mata.Plantio(new Relevo(2f, 8), Ocupados(new Relevo(2f, 8)));
            Assert.IsTrue(c.Count != a.Count || !(c[c.Count - 1].M == a[a.Count - 1].M), "outra ilha, outra mata");
        }

        [Test]
        public void Mata_NadaEmCimaDeArvorePedraOuDeOutraPeca()
        {
            // defeitos: ignorar os ocupados (tronco atravessando o pe' de uma arvore, samambaia dentro do tronco), pegada do tronco
            // como um disco de 2 m (nada chega no lado dele) ou so' o centro (a ponta do tronco entra na arvore)
            foreach (Mata.PecaPlantada p in MataDaIlha)
            {
                float e = p.Escala;
                foreach (Vector4 o in OcupadosDaIlha)
                {
                    var xz = new Vector2(o.x, o.y);
                    if (p.Tipo == Mata.Peca.Tronco)
                    {
                        // o corpo do tronco (4 x ~1 m, em metros) contra o raio FISICO do ocupado (a Vegetacao registra a arvore com
                        // 0,22 m de folga alem do tronco de 0,38): distancia do centro do ocupado ao retangulo do corpo
                        Vector2 m = NoMolde(p, xz) * e;
                        float fisico = o.w - 0.2f;
                        float fx = Mathf.Max(Mathf.Abs(m.x) - 0.5f * Mata.Tamanho[1].x * e, 0f), fz = Mathf.Max(Mathf.Abs(m.y) - 0.5f * e, 0f);
                        bool dentro = fx * fx + fz * fz < fisico * fisico;
                        Assert.IsFalse(dentro, "tronco caido atravessa o ocupado " + xz + " em " + Pe(p) + " (no molde, m: " + m + ")");
                    }
                    else
                    {
                        float raio = p.Tipo == Mata.Peca.Cristal ? 0.4f * e : 0.2f * e;
                        Assert.GreaterOrEqual(Vector2.Distance(Xz(p), xz), o.w + raio, p.Tipo + " em cima do ocupado " + xz + " em " + Pe(p));
                    }
                }
            }
            List<Mata.PecaPlantada> miudos = MataDaIlha.FindAll(p => p.Tipo == Mata.Peca.Cogumelo || p.Tipo == Mata.Peca.Samambaia);
            foreach (Mata.PecaPlantada a in miudos)
                foreach (Mata.PecaPlantada b in miudos)
                    if (!(a.M == b.M)) Assert.Greater(DistH(Pe(a), Pe(b)), 0.3f, "duas pecas miudas uma dentro da outra em " + Pe(a));
        }

        [Test]
        public void Mata_FogeDoKit_RochaPonteETorreQueMontamDepois()
        {
            // defeito: a mata sem as pegadas do kit (a sonda de 16/09 achou 29 troncos e cristais dentro da rocha de basalto, da
            // ponte de raiz e da torre); o filtro do alcance cortando o kit que encosta na borda da mata
            List<Vector4> kit = Mata.PegadasDoKit(Relevo2);
            Assert.GreaterOrEqual(kit.Count, 10, "o kit perto da mata (rochas, pontes de raiz, torre)");
            int bordas = 0;
            foreach (Vector4 k in kit)
            {
                float d = Vector2.Distance(new Vector2(k.x, k.y), Relevo2.Floresta);
                Assert.LessOrEqual(d - k.w, Mata.RaioDaMata(Relevo2), "pegada do kit longe da mata em " + k);
                if (d > Mata.RaioDaMata(Relevo2)) bordas++;
            }
            Assert.Greater(bordas, 0, "a peca do kit com o centro fora e a pegada dentro da mata tambem conta");
            foreach (Mata.PecaPlantada p in MataDaIlha)
            {
                if (!p.Colide) continue;
                float rp = 0.5f * Mata.Tamanho[(int)p.Tipo].z * p.Escala;
                foreach (Vector4 k in kit)
                    Assert.GreaterOrEqual(Vector2.Distance(Xz(p), new Vector2(k.x, k.y)), k.w + rp * 0.5f, p.Tipo + " dentro do kit " + k + " em " + Pe(p));
            }
        }

        [Test]
        public void Mata_RefazOKitComAsPegadasDasRuinas()
        {
            // defeito: PegadasDoKit ignorando os reservados (as Pegadas das ruinas que o kit real recebe): o sorteio daqui diverge
            // do real e um tronco nasce dentro da rocha de basalto (foto 51, 16/09). Uma pegada reservada em cima de uma peca do
            // kit tem de EMPURRAR a peca para outro lugar, como no KitCenario.Montar.
            List<Vector4> livre = Mata.PegadasDoKit(Relevo2);
            Vector4 peca = livre.Find(k => k.w > 0.5f && Vector2.Distance(new Vector2(k.x, k.y), Relevo2.Floresta) < Mata.RaioDaMata(Relevo2)
                && Vector2.Distance(new Vector2(k.x, k.y), Relevo2.Ruinas) > Relevo2.RuinasR);
            Assert.Greater(peca.w, 0f, "nenhuma peca do kit dentro da mata para reservar");
            var ruina = new Vector4(peca.x + 0.1f, peca.y, 0f, peca.w + 0.1f);
            List<Vector4> kit = Mata.PegadasDoKit(Relevo2, null, new List<Vector4> { ruina });
            Assert.Contains(ruina, kit, "a pegada reservada entra nas pegadas (a mata foge dela tambem)");
            foreach (Vector4 k in kit)
                if (k != ruina)
                    Assert.GreaterOrEqual(Vector2.Distance(new Vector2(k.x, k.y), new Vector2(ruina.x, ruina.y)), k.w + ruina.w - 1e-3f,
                        "peca do kit dentro da pegada reservada em " + k);
        }

        [Test]
        public void Mata_CogumeloAoPeDeArvoreOuDeTronco()
        {
            // defeito: sortear o cacho solto na mata (cogumelo no meio do nada nao le' como mata viva)
            List<Mata.PecaPlantada> troncos = Do(Mata.Peca.Tronco), cachos = Do(Mata.Peca.Cogumelo);
            int encostados = 0;
            foreach (Mata.PecaPlantada c in cachos)
            {
                float raio = 0.4f * c.Escala, perto = float.MaxValue;
                Vector2 xz = Xz(c);
                foreach (Vector4 o in OcupadosDaIlha) perto = Mathf.Min(perto, Vector2.Distance(xz, new Vector2(o.x, o.y)) - o.w);
                foreach (Mata.PecaPlantada t in troncos)
                {
                    Vector2 m = NoMolde(t, xz);
                    perto = Mathf.Min(perto, Mathf.Max(Mathf.Abs(m.x) - 0.5f * Mata.Tamanho[1].x * t.Escala, Mathf.Abs(m.y) - 0.55f * t.Escala));
                }
                if (perto < raio + 0.7f) encostados++;
            }
            Assert.Greater(encostados, cachos.Count * 0.85f, "cachos ao pe' de algo: " + encostados + " de " + cachos.Count);
        }

        [Test]
        public void Mata_SamambaiaEmManchas_NaoEspalhadaPorIgual()
        {
            // defeito: sortear touceira solta na mata inteira -> 250 pontinhos a ~6 m um do outro (plantacao, nao sub-bosque)
            List<Mata.PecaPlantada> s = Do(Mata.Peca.Samambaia);
            float soma = 0f;
            foreach (Mata.PecaPlantada a in s)
            {
                float perto = float.MaxValue;
                foreach (Mata.PecaPlantada b in s)
                    if (!(a.M == b.M)) perto = Mathf.Min(perto, DistH(Pe(a), Pe(b)));
                soma += perto;
            }
            Assert.Less(soma / s.Count, 2.2f, "vizinha mais perto media da samambaia");
        }

        [Test]
        public void Mata_PecaAssentada_BaseAbaixoDaMalha_ParteAMostra()
        {
            // defeitos: assentar pelo centro (na ladeira a quina de baixo boia), pela Altura() exata (ate' 0,23 m fora da malha
            // desenhada), afundar pela altura inteira (o tronco some no humus), samambaia com o disco de terra boiando
            foreach (Mata.PecaPlantada p in MataDaIlha)
            {
                Vector3 tam = Mata.Tamanho[(int)p.Tipo];
                for (int i = 0; i < 3; i++)
                    for (int k = 0; k < 3; k++)
                    {
                        Vector3 o = p.M.MultiplyPoint3x4(new Vector3((i - 1) * 0.5f * tam.x, 0f, (k - 1) * 0.5f * tam.z));
                        Assert.LessOrEqual(o.y, Grade.AlturaNaMalha(o.x, o.z) - 0.02f, p.Tipo + " com a base boiando em " + Pe(p));
                    }
                Vector3 topo = p.M.MultiplyPoint3x4(new Vector3(0f, tam.y, 0f));
                Assert.Greater(topo.y - Grade.AlturaNaMalha(topo.x, topo.z), 0.3f * tam.y * p.Escala, p.Tipo + " engolido pelo chao em " + Pe(p));
            }
        }

        [Test]
        public void Mata_QuemColideFicaLongeDosNascimentos_ECristaisLongeUnsDosOutros()
        {
            // defeitos: tirar a folga -> o mago nasce dentro de um tronco caido (dois nascimentos ficam NA mata); cristal sem colisor
            // (o mago atravessa 1,5 m de cristal); cristais em cacho (vira feira, nao tempero)
            foreach (Mata.PecaPlantada p in MataDaIlha)
            {
                Assert.AreEqual(p.Tipo == Mata.Peca.Tronco || p.Tipo == Mata.Peca.Cristal, p.Colide, p.Tipo + " colide?");
                if (!p.Colide) continue;
                foreach (Vector3 n in Relevo2.Nascimentos)
                    Assert.GreaterOrEqual(DistH(Pe(p), n), 6f + 0.5f * Mata.Tamanho[(int)p.Tipo].x * p.Escala, p.Tipo + " em cima do nascimento " + n);
            }
            int naMata = 0;
            foreach (Vector3 n in Relevo2.Nascimentos) if (Relevo2.BiomaEm(n.x, n.z) == Bioma.Floresta) naMata++;
            Assert.GreaterOrEqual(naMata, 1, "o teste so' vale se algum nascimento fica na mata");
            List<Mata.PecaPlantada> c = Do(Mata.Peca.Cristal);
            foreach (Mata.PecaPlantada a in c)
            {
                Assert.That(a.Escala, Is.InRange(0.6f, 1.4f), "escala do cristal");
                foreach (Mata.PecaPlantada b in c)
                    if (!(a.M == b.M)) Assert.GreaterOrEqual(DistH(Pe(a), Pe(b)), Mata.EntreCristais - 0.01f, "dois cristais juntos em " + Pe(a));
            }
        }

        [Test]
        public void Mata_LodECortePelaDistancia3D()
        {
            // defeitos: LOD ou corte pela horizontal (da queda, bem em cima da mata, 250 samambaias de 1,4K em LOD0); tronco e
            // cristal com LOD1 que nao existe; cristal cortado antes do tronco (ele e' a referencia de longe)
            var p = new Vector3(-120f, 3f, -132f);
            Assert.IsFalse(Mata.Lod1(Mata.Peca.Samambaia, p + new Vector3(Mata.Lod1Samambaia - 1f, 1.6f, 0f), p), "perto: LOD0");
            Assert.IsTrue(Mata.Lod1(Mata.Peca.Samambaia, p + new Vector3(0f, Mata.Lod1Samambaia + 1f, 0f), p), "de cima: LOD1");
            Assert.IsFalse(Mata.Lod1(Mata.Peca.Cogumelo, p + new Vector3(Mata.Lod1Cogumelo - 1f, 1.6f, 0f), p), "cogumelo logo antes do LOD1: LOD0");
            Assert.IsTrue(Mata.Lod1(Mata.Peca.Cogumelo, p + new Vector3(0f, 60f, 0f), p), "de cima: LOD1");
            Assert.Less(Mata.Lod1Samambaia, Mata.Lod1Cogumelo, "a samambaia (1,4K) troca antes do cogumelo (1,2K, o brilho da mata)");
            Assert.IsFalse(Mata.Lod1(Mata.Peca.Tronco, p + new Vector3(Mata.Lod1Tronco - 1f, 1.6f, 0f), p), "tronco perto: o remesh inteiro");
            Assert.IsTrue(Mata.Lod1(Mata.Peca.Tronco, p + new Vector3(0f, 100f, 0f), p), "tronco longe: o de 1,5 K");
            Assert.IsFalse(Mata.Lod1(Mata.Peca.Cristal, p + new Vector3(100f, 0f, 0f), p), "o cristal e' LOD unico");
            Assert.IsTrue(Mata.Visivel(Mata.Peca.Samambaia, p + new Vector3(0f, 1.6f, Mata.CorteSamambaia - 2f), p), "logo antes do corte ainda desenha");
            Assert.IsFalse(Mata.Visivel(Mata.Peca.Samambaia, p + new Vector3(0f, 1.6f, Mata.CorteSamambaia + 1f), p), "samambaia alem do corte");
            Assert.IsTrue(Mata.Visivel(Mata.Peca.Cogumelo, p + new Vector3(0f, 1.6f, Mata.CorteSamambaia + 5f), p), "o cogumelo (o brilho) vai mais longe");
            Assert.IsFalse(Mata.Visivel(Mata.Peca.Cogumelo, p + new Vector3(0f, 120f, 0f), p), "da queda, bem em cima: cortado");
            Assert.IsTrue(Mata.Visivel(Mata.Peca.Tronco, p + new Vector3(100f, 0f, 100f), p), "tronco a 141 m: desenha");
            Assert.IsFalse(Mata.Visivel(Mata.Peca.Tronco, p + new Vector3(0f, 0f, Mata.CorteTronco + 1f), p), "tronco alem do corte");
            Assert.IsTrue(Mata.Visivel(Mata.Peca.Cristal, p + new Vector3(0f, 0f, Mata.CorteTronco + 1f), p), "o cristal vai mais longe que o tronco");
            Assert.Greater(Mata.CorteCristal, Mata.CorteTronco);
            Assert.Greater(Mata.CorteTronco, Mata.CorteCogumelo);
            Assert.Less(Mata.CorteSamambaia, 80f, "a samambaia nao chega a' queda");
        }
    }
}
