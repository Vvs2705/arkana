using System.Collections.Generic;

namespace Arkana.Core
{
    /// <summary>
    /// NUMEROS DAS HABILIDADES — espelho de mobile-godot/godot/core/Kits.gd (GDD §3 e §4).
    /// AS DUAS LEIS NO FORMATO:
    ///  1. ECONOMIA (§4.2): ataque paga MANA; tatica paga COOLDOWN; suprema paga CARGA (0-100%, enche com
    ///     tempo E dano causado). Por isso NAO existe chave "mana"/"custo" em nenhuma entrada — o teste reprova.
    ///  2. TELEGRAFIA (§4.3): toda suprema avisa entre 1 e 4 s ANTES do efeito.
    /// Os LIMITADORES (o preco de cada poder) moram no MESMO bloco do poder que pagam.
    /// Bool vira 1/0 (ex.: "sino"); tudo aqui e' numero.
    /// </summary>
    public static partial class Kits
    {
        // OS 17 (12/09): cada GRUPO de magos poe a ficha num arquivo PROPRIO (Kits.GrupoX.cs) — ninguem edita este para
        // implementar um kit. Grupo sem arquivo: o compilador apaga a chamada e os magos dele seguem inertes.
        static partial void GrupoA(Dictionary<string, KitDef> m);
        static partial void GrupoB(Dictionary<string, KitDef> m);
        static partial void GrupoC(Dictionary<string, KitDef> m);
        static partial void GrupoD(Dictionary<string, KitDef> m);

        /// <summary>Piso e teto da telegrafia. O KitRunner GRAMPEIA nesta faixa em runtime: dado ruim nao burla a lei.</summary>
        public const float TelegrafiaMin = 1f;
        public const float TelegrafiaMax = 4f;
        /// <summary>Modelo Apex: 100 de dano adianta 15% da barra. AUMENTAR: agressao vira spam; DIMINUIR: esconder-se carrega quase igual.</summary>
        public const float CargaPorDano = 0.0015f;

        /// <summary>Faixa oficial 5-10s (DIRECAO.md §4): agressao no teto, utilidade no piso.</summary>
        public const float PadraoTaticaCd = 7f;
        /// <summary>Segundos ate a carga passiva encher 100%. Cooldown de suprema morreu em 26/08.</summary>
        public const float PadraoSupremaCarga = 45f;
        public const float PadraoTelegrafia = 1.5f;

        public sealed class KitDef
        {
            public string Slug, Nome;
            public bool Implementado;
            public float TaticaCd, SupremaCarga, Telegrafia;
            public Dictionary<string, float> Passiva, Tatica, Suprema;
        }

        /// <summary>Ordem 01..20.</summary>
        public static readonly string[] Slugs =
        {
            "01-pyra", "02-ceifadora", "03-veu", "04-corvus", "05-corvomante",
            "06-olho-de-eter", "07-vitalis", "08-ilusionista", "09-vex", "10-tessa",
            "11-aelion", "12-umbra", "13-brok", "14-gromm", "15-maris",
            "16-fizz", "17-sylva", "18-basalto", "19-noctus", "20-pip",
        };

        public static readonly IReadOnlyDictionary<string, KitDef> Magos;

        /// <summary>Ficha completa. Slug desconhecido devolve o padrao inerte: kit que nao existe nao quebra partida.</summary>
        public static KitDef De(string slug)
        {
            KitDef k;
            if (slug != null && Magos.TryGetValue(slug, out k)) return k;
            return Novo(slug ?? "", "?");
        }

        private static KitDef Novo(string slug, string nome)
        {
            return new KitDef
            {
                Slug = slug, Nome = nome, Implementado = false,
                TaticaCd = PadraoTaticaCd, SupremaCarga = PadraoSupremaCarga, Telegrafia = PadraoTelegrafia,
                Passiva = new Dictionary<string, float>(),
                Tatica = new Dictionary<string, float>(),
                Suprema = new Dictionary<string, float>(),
            };
        }

        static Kits()
        {
            Dictionary<string, KitDef> m = new Dictionary<string, KitDef>();
            string[] nomes =
            {
                "Pyra", "Ceifadora", "Véu", "Corvus", "Corvomante",
                "Olho-de-Éter", "Vitalis", "Ilusionista", "Vex", "Tessa",
                "Aelion", "Umbra", "Brok", "Gromm", "Maris",
                "Fizz", "Sylva", "Basalto", "Noctus", "Pip",
            };
            for (int i = 0; i < Slugs.Length; i++) m[Slugs[i]] = Novo(Slugs[i], nomes[i]);

            // ---------------------------------------------------------- IMPLEMENTADOS
            KitDef pyra = m["01-pyra"];
            pyra.Implementado = true;
            pyra.TaticaCd = 9f;         // teto da faixa: a Muralha AGRIDE (dano+area)
            pyra.SupremaCarga = 50f;    // dano puro carrega devagar
            pyra.Telegrafia = 1.4f;     // rugido + brilho antes do Braco Livre
            // Coracao de Fornalha: fogo NO CHAO nao a fere e reacende o braco.
            pyra.Passiva = new Dictionary<string, float>
            {
                { "buff_vel", 1.10f }, { "buff_dur", 2f },
            };
            // Muralha de Brasas: linha de fogo baixo de 8m por 5s. Agua APAGA e molha o braco; vento EMPURRA 3m.
            pyra.Tatica = new Dictionary<string, float>
            {
                { "comprimento", 8f }, { "duracao", 5f }, { "espessura", 1f },
                { "dano", 18f }, { "rearme", 1f }, { "aceso_dur", 2f },
                { "molhado_cd_extra", 1f }, { "apaga_raio", 3f },
                { "vento_empurra", 3f }, { "vento_raio", 4f },
            };
            // Braco Livre: 6s de leque continuo + dash que deixa fogo. Ao acabar, o braco ESFRIA.
            pyra.Suprema = new Dictionary<string, float>
            {
                { "duracao", 6f }, { "leque_tiros", 3f }, { "leque_graus", 17f }, { "cadencia", 0.16f },
                { "fogo_dash_raio", 1.6f }, { "fogo_dash_dur", 3f }, { "fogo_dash_dano", 8f },
                { "esfria_dur", 4f }, { "esfria_vel", 0.85f },
            };

            KitDef veu = m["03-veu"];
            veu.Implementado = true;
            veu.TaticaCd = 6f;          // piso da faixa: Atravessar e' mobilidade pura
            veu.SupremaCarga = 40f;     // utilidade de grupo enche mais rapido
            veu.Telegrafia = 1.6f;      // o SINO espectral (counter sonoro)
            // Entrelinha: desfoca depois de 4s parada de briga.
            veu.Passiva = new Dictionary<string, float>
            {
                { "quietude", 4f }, { "dist_desfoque", 20f },
            };
            // Atravessar: 0,8s de ativacao + 1,5s no plano espectral. O preco: eco na entrada, silencio na saida.
            veu.Tatica = new Dictionary<string, float>
            {
                { "ativacao", 0.8f }, { "duracao", 1.5f }, { "buff_vel", 1.35f }, { "parede_max", 2f },
                { "eco_dur", 2f }, { "silencio_saida", 1f },
            };
            // Mare Espectral: 5s de plano espectral para o grupo. NINGUEM conjura, e o sino toca no mundo real.
            veu.Suprema = new Dictionary<string, float>
            {
                { "duracao", 5f }, { "raio", 6f }, { "buff_vel", 1.25f },
                { "silencio", 5f }, { "sino", 1f },
            };

            KitDef tessa = m["10-tessa"];
            tessa.Implementado = true;
            tessa.TaticaCd = 7f;        // baixo de proposito: ela TECE varios fios
            tessa.SupremaCarga = 45f;   // defesa que absorve projeteis: meio da regua
            tessa.Telegrafia = 1.8f;    // o tear rune girando antes de abrir
            // Compasso Runico: regenera escudo. A CAPACIDADE e' do kernel (Balance.Escudo), nao daqui.
            tessa.Passiva = new Dictionary<string, float>
            {
                { "escudo_regen", 4f }, { "quebra_buff_vel", 1.15f }, { "quebra_buff_dur", 2f },
            };
            // Fio do Tear: fio de raio entre 2 pontos. O preco: zumbido a 5m e ancora que cai com UM golpe.
            tessa.Tatica = new Dictionary<string, float>
            {
                { "comprimento", 6f }, { "duracao", 20f }, { "max_fios", 6f }, { "raio_toque", 0.8f },
                { "dano", 14f }, { "rearme", 0.8f }, { "lentidao", 0.65f }, { "lentidao_dur", 1.5f },
                { "revela_dur", 4f }, { "zumbido_dist", 5f }, { "ancora_raio", 1.5f },
            };
            // Tear-Mae: absorve projetil inimigo e TECE escudo. Nao absorve curtissimo alcance; 1 por vez.
            tessa.Suprema = new Dictionary<string, float>
            {
                { "duracao", 8f }, { "raio", 6f }, { "escudo_por_projetil", 12f }, { "raio_aliado", 8f },
                { "zona_morta", 2f }, { "max_simultaneo", 1f },
            };

            // Os outros 17: declarados e inertes (nome + PADRAO) ate' o grupo deles escrever a ficha. Texto: design/personagens.
            GrupoA(m); GrupoB(m); GrupoC(m); GrupoD(m);
            Magos = m;
        }
    }
}
