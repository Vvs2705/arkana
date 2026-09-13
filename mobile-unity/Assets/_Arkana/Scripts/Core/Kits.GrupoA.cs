using System.Collections.Generic;

namespace Arkana.Core
{
    /// <summary>
    /// FICHAS DO GRUPO A (02 Ceifadora, 04 Corvus, 05 Corvomante, 06 Olho-de-Eter) — GDD §3, DIRECAO.md §4 (tabela dos
    /// 20) e §6, e as fichas design/personagens/NN-*.md. As mesmas duas leis de Kits.cs: tatica paga COOLDOWN, suprema
    /// paga CARGA, NENHUMA chave "mana"/"custo"; toda suprema avisa 1-4 s antes. Os LIMITADORES moram no bloco que pagam.
    /// Tudo aqui e' KNOB de playtest: o comentario diz o que AUMENTAR faz.
    /// </summary>
    public static partial class Kits
    {
        static partial void GrupoA(Dictionary<string, KitDef> m)
        {
            // ------------------------------------------------------------------ 02 CEIFADORA
            KitDef c = m["02-ceifadora"];
            c.Implementado = true;
            c.TaticaCd = 8f;          // agarra (controle), nao fere: meio-alto da regua
            c.SupremaCarga = 40f;     // mobilidade de grupo enche rapido
            c.Telegrafia = 2f;        // o mundo perde cor + coro grave (DIRECAO §6)
            // Ecos dos Caidos: quem morreu perto ha' < 60s deixa o replay dos ultimos 3s. O preco: e' PASSADO, nunca o agora.
            c.Passiva = new Dictionary<string, float>
            {
                { "eco_janela", 60f }, { "eco_replay", 3f }, { "eco_raio", 40f }, { "eco_amostra", 0.25f },
            };
            // Mao do Vazio: ponto a ate' 12m (mira ajuda 2m de lado). O preco: a rachadura AVISA 0,5s, o agarrado ainda
            // conjura, e a mao e' cortavel (1 golpe — hoje a esquiva do agarrado).
            c.Tatica = new Dictionary<string, float>
            {
                { "alcance", 12f }, { "mira_lado", 2f }, { "raio", 1.6f }, { "agarra", 1.2f },
                { "atraso", 0.5f }, { "golpes_corte", 1f },
            };
            // Travessia: rasgo reto ate' 60m. O preco: fica ABERTO 3s (quem tocar a entrada vem atras), quem atravessa sai
            // REVELADO 4s e 1s SEM CONJURAR.
            c.Suprema = new Dictionary<string, float>
            {
                { "alcance", 60f }, { "raio_toque", 1.5f },
                { "aberto", 3f }, { "revelado", 4f }, { "silencio", 1f },
            };

            // ------------------------------------------------------------------ 04 CORVUS
            KitDef v = m["04-corvus"];
            v.Implementado = true;
            v.TaticaCd = 7f;          // informacao que ja' se paga no anuncio
            v.SupremaCarga = 50f;     // transformacao de 30s: carga longa (GDD §4.4)
            v.Telegrafia = 2f;        // uivo num raio enorme + a coluna verga
            // Mundo de Cheiros: fitas dos ultimos 60s. O preco: so' o PASSADO — a ponta da fita fica 2s atras do agora.
            v.Passiva = new Dictionary<string, float>
            {
                { "trilha_janela", 60f }, { "trilha_raio", 35f }, { "trilha_amostra", 1f }, { "trilha_atraso", 2f },
            };
            // Uivo de Caca: quem se MEXE no raio fica aceso 4s. O preco: o uivo DENUNCIA ele (os bots ouvem) e parado
            // ninguem e' pego. AUMENTAR movimento_min: andar devagar vira esconderijo.
            v.Tatica = new Dictionary<string, float>
            {
                { "raio", 25f }, { "aceso_dur", 4f }, { "movimento_min", 0.6f }, { "janela", 0.3f }, { "denuncia", 1f },
            };
            // Forma de Lobisomem: 30s, +40%, garras, cura ao abater, faro total. O preco: SEM MAGIAS (nem tiro nem tatica),
            // silhueta 30% maior, uivo ouvido a 60m, e ao desmanchar 2s ofegante (lento e sem conjurar).
            v.Suprema = new Dictionary<string, float>
            {
                { "duracao", 30f }, { "buff_vel", 1.4f }, { "cura_abate", 40f },
                { "garra_dano", 16f }, { "garra_alcance", 2.4f }, { "garra_cadencia", 0.5f }, { "faro_raio", 70f },
                { "uivo_raio", 60f }, { "silhueta", 1.3f }, { "ofegante_dur", 2f }, { "ofegante_vel", 0.6f },
            };

            // ------------------------------------------------------------------ 05 CORVOMANTE
            KitDef o = m["05-corvomante"];
            o.Implementado = true;
            o.TaticaCd = 5f;          // piso: utilidade pura (DIRECAO §4)
            o.SupremaCarga = 45f;
            o.Telegrafia = 2.5f;      // o corvo sobe em espiral grasnando
            // Meu Olho Voa: o corvo FORA do ombro marca quem ve' a 20m dele. AUMENTAR visao: o voo vira radar.
            o.Passiva = new Dictionary<string, float>
            {
                { "visao", 20f }, { "marca_dur", 4f },
            };
            // Voo do Olho: o corvo voa 4s na mira (o stick/camera guiam). O preco: o CORPO fica parado e sem conjurar; o
            // corvo tem 60 de vida e bate asas que os bots OUVEM (no corvo, nao no corpo).
            o.Tatica = new Dictionary<string, float>
            {
                { "duracao", 4f }, { "vel", 12f }, { "altura", 6f },
                { "corvo_vida", 60f }, { "raio_acerto", 1.5f }, { "asas_intervalo", 1f },
            };
            // Grasnido do Fim: 50 so' no ESCUDO de quem esta' no raio. O preco: o corvo despenca exausto — 5s sem tatica.
            o.Suprema = new Dictionary<string, float>
            {
                { "raio", 12f }, { "dano_escudo", 50f }, { "exausto_dur", 5f },
            };

            // ------------------------------------------------------------------ 06 OLHO-DE-ETER
            KitDef e = m["06-olho-de-eter"];
            e.Implementado = true;
            e.TaticaCd = 8f;          // interrompe: agressao de informacao
            e.SupremaCarga = 35f;     // informacao carrega rapido (DIRECAO §4: "Crisalida 35 s")
            e.Telegrafia = 2f;        // o casulo pulsando E' o aviso (destrutivel)
            // Po de Eter: quem conjurou nos ultimos 5s brilha a ate' 40m. O preco: so' sente quem CONJUROU.
            e.Passiva = new Dictionary<string, float>
            {
                { "raio", 40f }, { "janela", 5f },
            };
            // Enxame Perscrutador: linha de 24m. O preco: 1,4s de atraso e tunel estreito (1,2m do eixo: erravel); FOGO a
            // 2,5m de um marcado queima as mariposas (vale para a marca da Crisalida tambem). AUMENTAR tunel: vira hitscan.
            e.Tatica = new Dictionary<string, float>
            {
                { "comprimento", 24f }, { "vel", 30f }, { "interrompe", 0.8f }, { "revela_dur", 6f },
                { "atraso", 1.4f }, { "tunel", 1.2f }, { "fogo_raio", 2.5f },
            };
            // Crisalida: eclode no fim do aviso e pousa mariposas em todo inimigo a 30m. O preco: o casulo tem 60 de vida
            // ANTES de eclodir (destruido = nada) e ele fica 1s "cego" (a passiva satura).
            e.Suprema = new Dictionary<string, float>
            {
                { "raio", 30f }, { "revela_dur", 6f },
                { "casulo_vida", 60f }, { "casulo_dist", 2f }, { "casulo_raio", 1.2f }, { "cego_dur", 1f },
            };
        }
    }
}
