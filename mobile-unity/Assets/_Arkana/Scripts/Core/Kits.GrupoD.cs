using System.Collections.Generic;

namespace Arkana.Core
{
    /// <summary>
    /// AS FICHAS DO GRUPO D — 16 Fizz, 17 Sylva, 18 Basalto, 19 Noctus, 20 Pip. Texto em design/personagens/NN-*.md; tempos
    /// oficiais na tabela do DIRECAO.md §4 (cooldown da tatica e carga base da suprema) e a telegrafia na fase ANTECIPA do §6.
    /// As leis de Kits.cs valem aqui sem excecao: nenhuma chave "mana"/"custo" (o Noctus DRENA e GANHA eter, nunca paga);
    /// tatica 5-10 s (agressao no teto, utilidade no piso); telegrafia 1-4 s; os LIMITADORES no bloco do poder que pagam.
    /// "raio" na suprema e' tambem o tamanho do aviso no chao (LeituraDosKits.RaioDoAviso): so' quem tem AREA o declara.
    /// </summary>
    public static partial class Kits
    {
        static partial void GrupoD(Dictionary<string, KitDef> m)
        {
            // ------------------------------------------------------------------ 16 FIZZ
            KitDef fizz = m["16-fizz"];
            fizz.Implementado = true;
            fizz.TaticaCd = 9f;         // teto: a torreta AGRIDE (dano no cone)
            fizz.SupremaCarga = 50f;    // dano puro carrega devagar
            fizz.Telegrafia = 2f;       // a bobina armada peca a peca, zumbindo — e' nesses 2 s que ela pode ser quebrada
            // Mola nos Calcanhares: o pulo sobe 50% mais alto (altura, nao velocidade).
            fizz.Passiva = new Dictionary<string, float>
            {
                { "pulo_mult", 1.5f },
            };
            // Torreta Faisca: 40 de vida, cone FIXO (flanqueavel), faisca FRACA (zoneia, nao mata), no maximo 2.
            fizz.Tatica = new Dictionary<string, float>
            {
                { "dist", 1.5f }, { "vida", 40f }, { "max_torretas", 2f }, { "duracao", 20f },
                { "alcance", 14f }, { "cone_graus", 40f }, { "cadencia", 0.9f }, { "dano", 6f }, { "vel", 30f },
                { "golpe_raio", 1.2f }, { "corpo_raio", 0.6f }, { "corpo_altura", 1.2f },
            };
            // MEGABOBINA: 60 de vida na carga (parada, brilha, canta); UM raio, UM alvo; depois vira sucata e as molas travam.
            fizz.Suprema = new Dictionary<string, float>
            {
                { "vida", 60f }, { "alcance", 35f }, { "dano", 60f }, { "vel", 70f }, { "passo_trajeto", 3f },
                { "golpe_raio", 1.5f }, { "corpo_raio", 0.8f }, { "corpo_altura", 2.6f },
                { "trava_dur", 2f }, { "sucata_dur", 2f },
            };

            // ------------------------------------------------------------------ 17 SYLVA
            KitDef sylva = m["17-sylva"];
            sylva.Implementado = true;
            sylva.TaticaCd = 7f;        // utilidade (cura), meio da faixa
            sylva.SupremaCarga = 45f;
            sylva.Telegrafia = 2f;      // o cajado fincado e as raizes douradas desenhando a area
            // Seiva Compartilhada: 20% do dano do aliado VINCULADO vem para ela (transfere, nao reduz — pode mata-la).
            sylva.Passiva = new Dictionary<string, float>
            {
                { "vinculo_raio", 12f }, { "vinculo_frac", 0.2f },
            };
            // Broto Guardiao: floresce em 1 s, polen cura 5 hp/s por 6 s; em GRAMA vira moita. Fogo QUEIMA o broto na hora.
            sylva.Tatica = new Dictionary<string, float>
            {
                { "dist", 2f }, { "florescer", 1f }, { "cura", 5f }, { "duracao", 6f }, { "raio", 4f },
                { "queima_raio", 1.5f }, { "moita_raio", 1.2f }, { "moita_dur", 12f },
            };
            // Coracao da Mata: 8 s de vida + escudo; quem ENTRA e' agarrado 0,8 s (teto do kernel) — aliado de fora tambem.
            // Ao acabar a seiva foi gasta: 3 s sem o vinculo.
            sylva.Suprema = new Dictionary<string, float>
            {
                { "raio", 7f }, { "duracao", 8f }, { "cura", 6f }, { "escudo", 5f },
                { "agarra", 0.8f }, { "rearme", 2f }, { "seiva_dur", 3f },
            };

            // ------------------------------------------------------------------ 18 BASALTO
            KitDef basalto = m["18-basalto"];
            basalto.Implementado = true;
            basalto.TaticaCd = 9f;      // teto: o punho AGRIDE
            basalto.SupremaCarga = 40f; // defesa de ancora, carrega rapido
            basalto.Telegrafia = 1.8f;  // cruza os bracos e afunda meio metro (parado no aviso)
            // Pele de Montanha: tiro pelas COSTAS fere 20% menos. O PRECO mora aqui: raio o atordoa +0,4 s (condutor).
            basalto.Passiva = new Dictionary<string, float>
            {
                { "costas_reducao", 0.2f }, { "costas_cos", 0.3f }, { "raio_atordoa", 0.4f },
                { "tiro_perto", 2.5f }, { "tiro_janela", 0.3f },
            };
            // Punho Sismico: onda de pedra em cone CURTO (kitar de longe e' a contra-jogada); em TERRA ergue 3 pedras.
            basalto.Tatica = new Dictionary<string, float>
            {
                { "alcance", 5f }, { "cone_graus", 40f }, { "dano", 22f }, { "empurrao", 3f }, { "vel", 14f }, { "pedras", 3f },
            };
            // Monolito: 6 s de torre — -60% de dano, marca quem esta' perto, devolve 15% em estilhacos. NAO anda nem conjura;
            // ao acabar as pernas endurecem 2 s (sem correr).
            basalto.Suprema = new Dictionary<string, float>
            {
                { "duracao", 6f }, { "reducao", 0.6f }, { "reflexo", 0.15f }, { "raio", 8f }, { "cone_graus", 60f },
                { "pulso", 0.5f }, { "marca_periodo", 1f }, { "imovel", 0.05f },
                { "endurece_dur", 2f }, { "endurece_vel", 0.5f },
            };

            // ------------------------------------------------------------------ 19 NOCTUS
            KitDef noctus = m["19-noctus"];
            noctus.Implementado = true;
            noctus.TaticaCd = 6f;       // piso: a mordida e' mobilidade + utilidade (nao fere)
            noctus.SupremaCarga = 40f;
            noctus.Telegrafia = 1.5f;   // o floreio de capa e a luz que avermelha
            // Sede de Eter: 15% do dano causado volta como ETER (a reserva do ataque); abate devolve 25 de vida.
            noctus.Passiva = new Dictionary<string, float>
            {
                { "conversao", 0.15f }, { "abate_vida", 25f },
            };
            // Mordida do Vazio: investida de 6 m SECOS; drena 20 de eter do alvo e o MARCA (proximo disparo dele +50%).
            // Errar = 1,5 s vulneravel (sem conjurar).
            noctus.Tatica = new Dictionary<string, float>
            {
                { "alcance", 6f }, { "raio_toque", 1.3f }, { "dreno", 20f },
                { "marca_dur", 4f }, { "marca_extra", 0.5f }, { "erro_dur", 1.5f },
            };
            // Forma de Nevoa: 4 s intangivel, lentidao em quem atravessa. O PRECO: nao conjura, a luz do dia o freia, o
            // rastro mostra o caminho e ao recondensar ele fica FAMINTO 3 s (a passiva nao rende).
            noctus.Suprema = new Dictionary<string, float>
            {
                { "duracao", 4f }, { "raio_toque", 1.3f }, { "lentidao", 0.6f }, { "lentidao_dur", 1.5f },
                { "vel_dia", 0.85f }, { "faminto_dur", 3f }, { "rastro_passo", 0.25f }, { "rastro_dur", 2f },
            };

            // ------------------------------------------------------------------ 20 PIP
            KitDef pip = m["20-pip"];
            pip.Implementado = true;
            pip.TaticaCd = 7f;
            pip.SupremaCarga = 45f;
            pip.Telegrafia = 1.5f;      // sobe e gira, as asas viram helice (o zumbido dobra)
            // Nunca Pousar: 2 s parada = definha 2 hp/s (agressao obrigatoria). O sino denuncia a cada 1,2 s.
            pip.Passiva = new Dictionary<string, float>
            {
                { "parada_s", 2f }, { "definha", 2f }, { "parada_dist", 0.4f }, { "sino_periodo", 1.2f },
            };
            // Zip-Zag: tres dashes em zigue-zague de ROTA TELEGRAFADA; cruzar um inimigo solta faisca no mais proximo.
            pip.Tatica = new Dictionary<string, float>
            {
                { "dashes", 3f }, { "dash_dist", 4f }, { "intervalo", 0.8f }, { "zig_graus", 35f }, { "raio_toque", 1.1f },
                { "faisca_raio", 8f }, { "faisca_dano", 10f }, { "faisca_vel", 30f },
            };
            // Supercelula: 8 s de nuvem atirando FRACO no mais proximo (a distancia e' a contra-jogada); ao acabar chove
            // nela — encharcada e lenta 2 s, e molhada CONDUZ raio.
            pip.Suprema = new Dictionary<string, float>
            {
                { "duracao", 8f }, { "raio", 12f }, { "cadencia", 0.6f }, { "dano", 6f }, { "vel", 40f }, { "altura", 3.2f },
                { "chuva_dur", 2f }, { "chuva_vel", 0.7f },
            };
        }
    }
}
