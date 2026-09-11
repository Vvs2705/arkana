using System;
using System.Collections.Generic;
using UnityEngine;
using Arkana.Core;
using Arkana.World;

namespace Arkana.Gameplay
{
    /// <summary>Uma luva no chao: o que se ve', se anda ate' e se pega.</summary>
    public sealed class LootItem
    {
        public string ArmaId;
        public Elemento[] Par;
        public Elemento? ElementoDaLuva;
        public Vector3 Pos;

        public LootItem(string armaId, Elemento[] par, Elemento? elemento, Vector3 pos)
        {
            ArmaId = armaId; Par = par ?? new Elemento[0]; ElementoDaLuva = elemento; Pos = pos;
        }

        public ArmaDef Dados => Arma.Dados(ArmaId);
        public Elemento[] Elementos => Par.Length > 0 ? Par : (ElementoDaLuva.HasValue ? new[] { ElementoDaLuva.Value } : new Elemento[0]);
        public string Rotulo => Textos.ArmaRotulo(Dados.Nome, Elementos);
    }

    /// <summary>
    /// LOOT NO CHAO (GDD §16.2) — distribuicao deterministica por seed e o ato de pegar.
    /// Janela de altura NAO cravada: quem diz se o chao serve e' a ilha (PodePousar). Raios como FRACAO
    /// do raio de terra e do raio do POI: metro cravado envelhece calado (a ilha dobrou e dois loots sumiram).
    /// </summary>
    public sealed class Loot
    {
        /// <summary>m — raio em que o prompt aparece. KNOB.</summary>
        public const float RAIO_PEGAR = 2.2f;
        /// <summary>Auto-iluminacao do corpo do item (apresentacao): em contra-luz o modelo vira vulto preto.</summary>
        public const float EMISSAO_CORPO = 0.35f;
        public const float ALTURA = 0.85f;
        /// <summary>Onde as luvas comuns nascem, como FRACAO do raio de terra.</summary>
        public const float VARINHA_R_MIN = 0.11f;
        public const float VARINHA_R_MAX = 0.47f;
        /// <summary>Comum e ESPALHADA (ninguem fica desarmado); cajado raro, 1 por POI; manopla NAO nasce no chao.</summary>
        public const int QTD_VARINHA = 12;
        public const int QTD_CAJADO = 4;
        public const int SEED_LOOT = 1301;
        public const float RAIO_TERRA_PADRAO = 132f;

        /// <summary>FALLBACK de POIs, so' sem ilha (teste). A fonte de verdade e' relevo.Pois.</summary>
        public static readonly Poi[] POIS_PADRAO =
        {
            new Poi("alagado", new Vector2(-70, 60), 22f / RAIO_TERRA_PADRAO, RAIO_TERRA_PADRAO),
            new Poi("floresta", new Vector2(-60, -66), 40f / RAIO_TERRA_PADRAO, RAIO_TERRA_PADRAO),
            new Poi("lago", new Vector2(75, 30), 24f / RAIO_TERRA_PADRAO, RAIO_TERRA_PADRAO),
            new Poi("ruinas", new Vector2(63, -73), 20f / RAIO_TERRA_PADRAO, RAIO_TERRA_PADRAO),
        };

        public readonly List<LootItem> Itens = new List<LootItem>();
        private readonly IRelevo _relevo;
        private LootItem _pertoDoPlayer;   // borda: so' emite LootPrompt quando MUDA

        public Loot(IRelevo relevo) { _relevo = relevo; }

        private float RaioTerra => _relevo != null && _relevo.RaioTerra > 0f ? _relevo.RaioTerra : RAIO_TERRA_PADRAO;
        private Poi[] Pois => _relevo != null && _relevo.Pois != null && _relevo.Pois.Length > 0 ? _relevo.Pois : POIS_PADRAO;

        // ---------------------------------------------------------------- distribuicao

        /// <summary>Espalha o loot da partida. DETERMINISTICO: mesmo seed = mesmas posicoes. Devolve quantos nasceram.</summary>
        public int Espalhar(int seed = SEED_LOOT)
        {
            Itens.Clear();
            var rng = new System.Random(seed);
            int n = 0;
            float rt = RaioTerra;
            // varinhas: aneis largos cobrindo a ilha; o ELEMENTO cicla pelos 5 — quem quer um, ANDA
            for (int i = 0; i < QTD_VARINHA; i++)
            {
                float ang = Mathf.PI * 2f * i / QTD_VARINHA + Faixa(rng, -0.22f, 0.22f);
                float raio = Faixa(rng, VARINHA_R_MIN, VARINHA_R_MAX) * rt;
                Vector2 alvo = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * raio;
                if (Por(rng, Arma.VARINHA, alvo, Elementos.Todos[i % Elementos.Todos.Length], 24f)) n++;
            }
            // cajados: 1 por POI. Alvo e busca saem do RAIO DO POI (lago/alagado sao agua: pousa na beira seca)
            Poi[] pois = Pois;
            int qtd = Mathf.Min(QTD_CAJADO, pois.Length);
            for (int i = 0; i < qtd; i++)
            {
                Poi poi = pois[i];
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                Vector2 alvo = poi.Centro + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * Faixa(rng, 0.25f, 0.62f) * poi.Raio;
                if (Por(rng, Arma.CAJADO, alvo, Elementos.Todos[(i + 2) % Elementos.Todos.Length], poi.Raio)) n++;
            }
            return n;
        }

        /// <summary>Tenta pousar perto de `alvo`; a busca ABRE em espiral ate' ~2,4x o escopo. Quem decide o chao e' a ilha.</summary>
        private bool Por(System.Random rng, string armaId, Vector2 alvo, Elemento el, float escopo)
        {
            for (int t = 0; t < 12; t++)
            {
                float busca = escopo * (0.17f + 0.13f * t);
                Vector2 p = t == 0 ? alvo : alvo + new Vector2(Faixa(rng, -busca, busca), Faixa(rng, -busca, busca));
                float h = 1f;
                if (_relevo != null)
                {
                    if (!_relevo.PodePousar(p.x, p.y)) continue;
                    h = _relevo.Altura(p.x, p.y);
                }
                Itens.Add(new LootItem(armaId, null, el, new Vector3(p.x, h, p.y)));
                return true;
            }
            return false;
        }

        private static float Faixa(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

        /// <summary>GANCHO DO BAU CELESTIAL — a UNICA porta da manopla. O par e' FIXO pelo indice (deterministico).</summary>
        public LootItem BauCelestial(Vector3 pos, int indice = 0)
        {
            Elemento[] par = Arma.PARES_MANOPLA[Mod(indice, Arma.PARES_MANOPLA.Length)];
            var l = new LootItem(Arma.MANOPLA, (Elemento[])par.Clone(), null, pos);
            Itens.Add(l);
            return l;
        }

        // ---------------------------------------------------------------- perto / pegar

        /// <summary>O loot mais proximo dentro de `raio` (distancia 3D), ou null.</summary>
        public LootItem Perto(Vector3 pos, float raio = RAIO_PEGAR)
        {
            LootItem melhor = null;
            float d2 = raio * raio;
            foreach (LootItem l in Itens)
            {
                float dd = (l.Pos - pos).sqrMagnitude;
                if (dd <= d2) { d2 = dd; melhor = l; }
            }
            return melhor;
        }

        /// <summary>
        /// O relogio do PLAYER: quem entra/sai do raio. LootPrompt sai na BORDA (entrou/saiu/trocou de item),
        /// nunca por frame. So' o player emite: prompt de bot e' ruido.
        /// </summary>
        public void Atualizar(Vector3 posPlayer)
        {
            LootItem agora = Perto(posPlayer);
            if (agora == _pertoDoPlayer) return;
            if (_pertoDoPlayer != null) Bus.EmitLootPrompt(_pertoDoPlayer.Rotulo, _pertoDoPlayer.Dados.Raridade, false);
            _pertoDoPlayer = agora;
            if (agora != null) Bus.EmitLootPrompt(agora.Rotulo, agora.Dados.Raridade, true);
        }

        /// <summary>
        /// A porta UNICA do "pegar" (botao da HUD, auto-upgrade do bot, bau). TROCA, nao consome: a arma velha
        /// fica no chao no lugar desta. EXCECAO: de maos nuas o loot e' CONSUMIDO (sem luva fantasma).
        /// Devolve false se ja' e' a mesma arma (nada acontece, sem sinal duplicado).
        /// </summary>
        public bool Pegar(LootItem item, ArmaSlot slot)
        {
            if (item == null || slot == null || !Itens.Contains(item)) return false;
            bool trocou;
            LootItem velha = slot.Trocar(item.ArmaId, item.Par, item.ElementoDaLuva, out trocou);
            if (!trocou) return false;
            if (velha == null)
                Itens.Remove(item);
            else
            {
                item.ArmaId = velha.ArmaId;
                item.Par = velha.Par;
                item.ElementoDaLuva = velha.ElementoDaLuva;
            }
            if (slot.Dono != null && slot.Dono.EhPlayer) Atualizar(slot.Dono.Pos);  // o prompt some: ja' pegou
            return true;
        }

        /// <summary>Pega o mais proximo do dono do slot. Devolve true se equipou algo.</summary>
        public bool Pegar(ArmaSlot slot) => slot != null && slot.Dono != null && Pegar(Perto(slot.Dono.Pos), slot);

        /// <summary>AUTO-UPGRADE (bot): ninguem recusa arma de tier estritamente melhor.</summary>
        public bool TentarAutoUpgrade(ArmaSlot slot)
        {
            if (slot == null || !slot.AutoUpgrade || slot.Dono == null) return false;
            LootItem l = Perto(slot.Dono.Pos);
            return l != null && Arma.Tier(l.ArmaId) > slot.Tier && Pegar(l, slot);
        }

        private static int Mod(int a, int n) => ((a % n) + n) % n;
    }
}
