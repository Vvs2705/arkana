using System.Collections.Generic;

namespace Arkana.Core
{
    /// <summary>
    /// GRUPO B dos 17 (12/09): Vitalis (07), Ilusionista (08), Vex (09), Aelion (11). Mesmas leis do Kits.cs: nada de
    /// mana/custo, tatica 5-10 s (agressao no teto, utilidade no piso), suprema por CARGA, telegrafia 1-4 s. Os tempos
    /// sao os da tabela oficial (DIRECAO.md §4); o resto e' ficha (design/personagens/NN-*.md) e KNOB de playtest.
    /// Os LIMITADORES moram no bloco do poder que pagam. Bool vira 1/0.
    /// </summary>
    public static partial class Kits
    {
        static partial void GrupoB(Dictionary<string, KitDef> m)
        {
            KitDef vitalis = m["07-vitalis"];
            vitalis.Implementado = true;
            vitalis.TaticaCd = 7f;          // cura: utilidade, meio da regua
            vitalis.SupremaCarga = 40f;     // suporte de grupo enche rapido
            vitalis.Telegrafia = 1.5f;      // a Lumen voa ao centro e desenha o circulo em luz
            // Maos Livres: a Lumen reergue o aliado CAIDO a ate' `alcance` m. Reerguer pela Lumen NAO gera escudo
            // (Derrubado.Reerguer ja' volta com escudo zero). Com a Lumen fora (curando/apagada) a passiva DESLIGA.
            vitalis.Passiva = new Dictionary<string, float>
            {
                { "alcance", 10f }, { "reerguer_s", 6f },
            };
            // Vai, Lumen: 8 hp/s por 12s. O preco: QUALQUER dano dissipa a Lumen (a cura para) e a passiva fica sem ela.
            // `alcance` e' do esquadrao (SOLO: a Lumen cura a propria Vitalis).
            vitalis.Tatica = new Dictionary<string, float>
            {
                { "cura", 8f }, { "duracao", 12f }, { "alcance", 30f }, { "dissipa_ao_dano", 1f },
            };
            // Jardim da Aurora: circulo de 6m por 10s, quem e' do time regenera e reergue 50% mais rapido; INIMIGO DENTRO
            // NAO RECEBE CURA. O preco: e' um FAROL de `farol_altura` m (visivel por cima de parede, para todos) e ao
            // murchar a Lumen volta apagada — 3s sem passiva.
            vitalis.Suprema = new Dictionary<string, float>
            {
                { "raio", 6f }, { "duracao", 10f }, { "cura", 8f }, { "reerguer_mult", 1.5f },
                { "selo_inimigo", 1f }, { "farol_altura", 28f }, { "apagada_dur", 3f },
            };

            KitDef ilu = m["08-ilusionista"];
            ilu.Implementado = true;
            ilu.TaticaCd = 8f;              // defesa que devolve dano
            ilu.SupremaCarga = 40f;
            ilu.Telegrafia = 1.8f;          // vidro trincando em acorde, bracos de maestro
            // Truque de Fuga: derrubado, quebra em cacos — invisivel 3s e um reflexo caido fica no lugar.
            ilu.Passiva = new Dictionary<string, float>
            {
                { "invisivel", 3f }, { "reflexo_caido", 3f },
            };
            // Espelho de Mao: fixo 2s a 1,5m na mira; DEVOLVE projetil inimigo com 30% do dano a quem atirou. O preco:
            // so' PROJETIL (area e corpo a corpo passam), so' o que vem pela FRENTE, e quebra na 3a devolucao.
            ilu.Tatica = new Dictionary<string, float>
            {
                { "duracao", 2f }, { "distancia", 1.5f }, { "largura", 1.6f }, { "captura", 0.8f },
                { "fracao_dano", 0.3f }, { "devolucoes", 3f },
            };
            // Baile de Espelhos: 5 reflexos que ESPELHAM o passo dele por 6s + 2,5s invisivel. O preco: reflexo nao fere;
            // invisivel quebra ao conjurar; o flash do reflexo quebrado so' ofusca quem esta' a `flash_dist` m; no fim a
            // ultima nota REVELA onde ele esta' (1s).
            ilu.Suprema = new Dictionary<string, float>
            {
                { "reflexos", 5f }, { "raio", 3f }, { "duracao", 6f }, { "invisivel", 2.5f },
                { "flash_dist", 4f }, { "ofusca", 0.5f }, { "revela_fim", 1f },
            };

            KitDef vex = m["09-vex"];
            vex.Implementado = true;
            vex.TaticaCd = 9f;              // teto: arma area que fere
            vex.SupremaCarga = 50f;         // negacao de area enorme carrega devagar
            vex.Telegrafia = 3f;            // o fole INFLA chiando, runas giram nos pes
            // Olhos do Miasma: inimigo DENTRO da nevoa dele ganha contorno verde (so' para ele).
            vex.Passiva = new Dictionary<string, float>
            {
                { "contorno_dur", 0.4f },
            };
            // Frascos de Reagente: 1 frasco por uso, ate' 6 no chao; arco de `voo` s; arma em 1s; inimigo a `gatilho` m
            // detona a nuvem — dano BAIXO + lentidao. O preco: um TIRO a `tiro_raio` m detona a distancia (desperdicada);
            // VENTO dispersa a nuvem; FOGO incendeia e consome em 2s (§14).
            vex.Tatica = new Dictionary<string, float>
            {
                { "max_frascos", 6f }, { "alcance", 8f }, { "voo", 0.5f }, { "arma", 1f }, { "poca_dur", 45f },
                { "gatilho", 1.2f }, { "nuvem_raio", 3f }, { "nuvem_dur", 4f }, { "dps", 5f }, { "lentidao", 0.7f },
                { "tiro_raio", 1.2f }, { "vento_dispersa", 1f }, { "fogo_consome", 2f },
            };
            // A Grande Obra: nevoa de 12m por 12s — lenta + dano BAIXO no inimigo e SELA a cura de TODO MUNDO dentro
            // (o time dele tambem: dois gumes). O preco: vento/fogo como nos frascos; ao acabar o fole esvazia — 2s sem
            // correr (`ofegante_vel`).
            vex.Suprema = new Dictionary<string, float>
            {
                { "raio", 12f }, { "duracao", 12f }, { "dps", 3f }, { "lentidao", 0.75f }, { "selo", 1f },
                { "vento_dispersa", 1f }, { "fogo_consome", 2f }, { "ofegante_dur", 2f }, { "ofegante_vel", 0.5f },
            };

            KitDef aelion = m["11-aelion"];
            aelion.Implementado = true;
            aelion.TaticaCd = 10f;          // teto da faixa: dano puro de sniper
            aelion.SupremaCarga = 50f;
            aelion.Telegrafia = 3f;         // a flecha ao ceu: o risco dourado que todo o servidor ve'
            // Olhar do Crepusculo: acerto a mais de 40m MARCA o alvo 3s (so' para ele: nao vira wallhack de esquadrao).
            aelion.Passiva = new Dictionary<string, float>
            {
                { "dist_marca", 40f }, { "marca_dur", 3f },
            };
            // Flecha de Eter: carrega 1,5s e sai sozinha, MUITO rapida, atravessa 1 muro fino (ate' `parede_max` m: 1 celula
            // de muro do §14 tem 3m — dois colados ja' param).
            // O preco: carregando fica 40% mais lento e brilhando; de perto rende pouco (`dano_perto` ate' `dist_cheia` m).
            aelion.Tatica = new Dictionary<string, float>
            {
                { "carga", 1.5f }, { "carga_vel", 0.6f }, { "velocidade", 70f }, { "alcance", 90f }, { "dano", 40f },
                { "dano_perto", 0.5f }, { "dist_cheia", 20f }, { "atravessa", 1f }, { "parede_max", 3.5f },
            };
            // Chuva do Crepusculo: 3 ondas de flechas numa faixa ESTREITA de 30m que comeca a `inicio` m (colado nele
            // nao pega), avisada no chao. O preco: ao acabar o arco esfria — 2s sem a tatica.
            aelion.Suprema = new Dictionary<string, float>
            {
                { "inicio", 4f }, { "comprimento", 30f }, { "largura", 3f }, { "ondas", 3f }, { "intervalo", 0.4f },
                { "dano", 25f }, { "esfria_dur", 2f },
            };
        }
    }
}
