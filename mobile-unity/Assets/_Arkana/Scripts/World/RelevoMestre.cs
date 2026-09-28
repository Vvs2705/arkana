using System.Collections.Generic;
using UnityEngine;
using Arkana.Gameplay;

namespace Arkana.World
{
    /// <summary>
    /// A ilha do Documento Mestre (IlhaMestre, 4,8 x 4,4 km) vista pelo gameplay: o IRelevo que a Queda, a Agua, a Zona,
    /// o Loot e o Bau consomem. Os eixos ja' batem (doc x -> Unity x, doc y -> Unity z), entao Altura e' direto.
    /// A agua e' a mesma regra que a IlhaMestre desenha: mar em 0, lago no nivel do doc, rio pela polilinha do doc.
    /// </summary>
    public sealed class RelevoMestre : IRelevo
    {
        readonly IlhaMestre m;
        readonly IlhaMestre.Dados d;

        public RelevoMestre(IlhaMestre mestre)
        {
            m = mestre;
            d = mestre.D;
            Lado = d.x1 - d.x0;
            RaioTerra = Mathf.Min(d.x1 - d.x0, d.y1 - d.y0) * 0.5f;
            var pois = new List<Poi>();
            var nasc = new List<Vector3>();
            foreach (IlhaMestre.Regiao r in d.regioes)
            {
                if (r.ex <= 0f) continue;                                   // R11 (rede de tuneis) nao tem envelope
                pois.Add(new Poi(r.nome, new Vector2(r.x, r.y), Mathf.Min(r.ex, r.ey) * 0.5f / RaioTerra, RaioTerra));
                // nascimento do treino: centro da regiao, se for chao seco (o lago e as cavernas ficam de fora)
                if (PodePousar(r.x, r.y)) nasc.Add(new Vector3(r.x, Altura(r.x, r.y) + 0.5f, r.y));
            }
            Pois = pois.ToArray();
            Nascimentos = nasc.ToArray();
        }

        /// <summary>Lado do mapa em metros (4.800): a rota do castelo e' fracao disto.</summary>
        public float Lado { get; }
        public float RaioTerra { get; }
        public Poi[] Pois { get; }
        /// <summary>Pontos de chao seco (centros das regioes) para o treino; a partida cai do castelo.</summary>
        public Vector3[] Nascimentos { get; }

        public float Altura(float x, float z) => m.Altura(x, z);
        public Color CorDoChao(float x, float z) => m.CorDoChao(x, z);

        /// <summary>Abaixo disto e' o VAZIO em volta da ilha flutuante: o corpo morre (Pawn). O leito antigo do mar esta' a -40.</summary>
        public const float VazioY = -25f;

        /// <summary>O fundo do TITULO: morro plano a 178 m, 73 m acima do lago, a leste (medido no heightmap). Vetavel.</summary>
        public static readonly Vector2 Mirante = new Vector2(700f, 500f);

        public float SuperficieDaAgua(float x, float z)
        {
            float h = m.Altura(x, z);
            if (h < Relevo.AguaY) return IlhaMestre.IlhaFlutuante ? Relevo.Seco : Relevo.AguaY;   // sem mar: a borda e' queda
            IlhaMestre.Lago l = d.lago;
            float th = Mathf.Atan2((z - l.cy) / l.b, (x - l.cx) / l.a);
            float q = Mathf.Sqrt(((x - l.cx) / l.a) * ((x - l.cx) / l.a) + ((z - l.cy) / l.b) * ((z - l.cy) / l.b)) /
                      (1f + 0.05f * Mathf.Sin(3f * th + 1.1f) + 0.035f * Mathf.Sin(5f * th + 2.6f));
            if (q < 1f && h < l.nivel) return l.nivel;
            // rio: segmento mais proximo da polilinha do doc; dentro da meia-largura e abaixo do nivel = agua
            float melhor = float.MaxValue, nivel = Relevo.Seco;
            for (int i = 0; i + 1 < d.rio.Length; i++)
            {
                IlhaMestre.Ponto a = d.rio[i], b = d.rio[i + 1];
                float vx = b.x - a.x, vy = b.y - a.y;
                float u = Mathf.Clamp01(((x - a.x) * vx + (z - a.y) * vy) / (vx * vx + vy * vy));
                float dist = Mathf.Sqrt((x - (a.x + u * vx)) * (x - (a.x + u * vx)) + (z - (a.y + u * vy)) * (z - (a.y + u * vy)));
                float meia = a.meia + u * (b.meia - a.meia);
                if (dist < melhor && dist < meia) { melhor = dist; nivel = a.z + u * (b.z - a.z); }
            }
            return nivel != Relevo.Seco && h < nivel ? nivel : Relevo.Seco;
        }

        /// <summary>Dentro de um vazio do subterraneo (tunel, salao ou poco), com folga de 1-2 m.</summary>
        public bool Subterraneo(Vector3 p)
        {
            IlhaMestre.Sub s = m.S;
            if (s == null) return false;
            foreach (IlhaMestre.Tunel t in s.tuneis)
                for (int i = 0; i + 4 < t.p.Length; i += 5)
                {
                    float dx = p.x - t.p[i], dz = p.z - t.p[i + 1];
                    float r = t.p[i + 3] * 0.5f + 1f;
                    if (dx * dx + dz * dz < r * r && p.y >= t.p[i + 2] - 2f && p.y <= t.p[i + 2] + t.p[i + 4] + 1f) return true;
                }
            foreach (IlhaMestre.Salao sl in s.saloes)
            {
                float g = -sl.giro * Mathf.Deg2Rad, dx = p.x - sl.x, dz = p.z - sl.y;
                float lx = dx * Mathf.Cos(g) - dz * Mathf.Sin(g), lz = dx * Mathf.Sin(g) + dz * Mathf.Cos(g);
                float ex = sl.largura * 0.5f + 1f, ez = sl.comprimento * 0.5f + 1f;
                if (lx * lx / (ex * ex) + lz * lz / (ez * ez) < 1f && p.y >= sl.piso - 2f && p.y <= sl.piso + sl.altura + 1f) return true;
            }
            foreach (IlhaMestre.Poco pc in s.pocos)
            {
                float dx = p.x - pc.x, dz = p.z - pc.y, r = pc.raio + 1f;
                if (dx * dx + dz * dz < r * r && p.y >= pc.piso - 2f && p.y <= pc.topo + 6f) return true;
            }
            return false;
        }

        public bool PodePousar(float x, float z) => SuperficieDaAgua(x, z) == Relevo.Seco && Altura(x, z) >= Relevo.PraiaY;
    }
}
