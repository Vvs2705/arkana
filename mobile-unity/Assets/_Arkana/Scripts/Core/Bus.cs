using System;
using UnityEngine;

namespace Arkana.Core
{
    /// <summary>
    /// Barramento de eventos do jogo. Dono: COORDENADOR. Raia nova precisa de evento novo? Pede, nao cria local.
    /// REGRA: telemetria/UI/audio OBSERVAM por aqui e nunca decidem jogo. Quem aplica dano e' um lugar so' (Core.Combat).
    /// Espelho 1:1 de mobile-godot/godot/core/Bus.gd — os comentarios de cada sinal estao la'.
    /// Emissores chamam os metodos Emit*; observadores assinam os eventos. Bus.Reset() zera tudo (testes).
    /// </summary>
    public static class Bus
    {
        // target, amount (efetivo, nunca 0), element, source (null = terreno/DoT/ambiente), onShield
        public static event Action<IEntidade, float, Elemento, IEntidade, bool> DamageApplied;
        public static event Action<IEntidade, float, float, int> ShieldChanged;   // entity, shield, shieldMax, level 1..4
        public static event Action<IEntidade> ShieldBroken;
        public static event Action<IEntidade> EntityDied;                          // a morte DE VERDADE, uma vez so'
        public static event Action<IEntidade, IEntidade> EntityDerrubada;          // entity, causador (null = ambiente)
        public static event Action<IEntidade, IEntidade> EntityReerguida;          // entity, por
        public static event Action<IEntidade, float, float> DerrubadoProgresso;    // entity, esvaecimento 1..0, reerguer 0..1
        public static event Action MatchStarted;
        public static event Action<bool> MatchOver;                                // victory
        public static event Action<float, float> ManaChanged;                      // current, max (so' o player)
        public static event Action<float, float> HealthChanged;                    // current, max (so' o player)
        public static event Action<Elemento> ElementChanged;
        public static event Action GameStartRequested;                             // o menu pede a partida
        public static event Action<Elemento, Vector3, bool> TerrainHit;            // element, pos, strong
        public static event Action<string, Vector3> TerrainChanged;                // kind ("burn"|"ice"|"electric"|"mud"|"wall"|"ash"), pos
        public static event Action<string> PlayerKilledBot;                        // bot name (kill feed)
        public static event Action DodgePerformed;
        public static event Action<Elemento> SpellCast;                            // so' o player
        public static event Action<IEntidade, Vector3> Disparo;                    // TODO disparo, com posicao (percepcao dos bots)
        public static event Action<string, string, bool> LootPrompt;               // nome, raridade, perto
        public static event Action<IEntidade, string, string, string, Elemento[]> WeaponEquipped; // pawn, armaId, nome, raridade, elementos (1 ou 2)
        public static event Action<string, bool> KitBound;                         // slug, implementado
        public static event Action<string, float, float> KitCooldown;              // tipo "tatica"|"suprema", restante, total (na BORDA)
        public static event Action<string, string, float, Vector3> KitTelegraph;   // slug, tipo, duracao, pos
        public static event Action<string, bool> KitState;                         // nome do estado, ligado
        public static event Action<Vector3, float> BauAnunciado;                   // pos de pouso, segundos
        public static event Action<Vector3> BauPousou;
        public static event Action<IEntidade, float> BauCanalizando;               // pawn, progresso 0..1 (0 = cancelou)
        public static event Action<bool, Elemento[]> BauAberto;                    // porPlayer, os 2 elementos da manopla
        public static event Action<IEntidade, string> StatusAplicado;              // alvo, "burn"|"wet"|"frost"|"stun" (na BORDA)
        public static event Action<string> QuedaFase;                              // "no_castelo"|"caindo"|"planando"|"pousou"
        public static event Action<float, float> QuedaAltura;                      // metros, velocidade
        public static event Action<Vector3, Vector3, float> CasteloRota;           // inicio, fim, duracao
        public static event Action<float> ZonaAbertura;                            // segundos ate' a tempestade existir
        public static event Action<float, float> ZonaFormando;                     // raio, duracao
        public static event Action<int, Vector3, float, float> ZonaAvisou;         // fase, centro, raio, segundos
        public static event Action<int, Vector3, float, float> ZonaFechando;       // fase, centro, raio, duracao
        public static event Action<float, float> ZonaDano;                         // dano efetivo, dps da fase (1x por segundo)
        public static event Action<bool> ZonaEstado;                               // dentro (na BORDA)

        public static void EmitDamageApplied(IEntidade target, float amount, Elemento element, IEntidade source, bool onShield) => DamageApplied?.Invoke(target, amount, element, source, onShield);
        public static void EmitShieldChanged(IEntidade e, float shield, float shieldMax, int level) => ShieldChanged?.Invoke(e, shield, shieldMax, level);
        public static void EmitShieldBroken(IEntidade e) => ShieldBroken?.Invoke(e);
        public static void EmitEntityDied(IEntidade e) => EntityDied?.Invoke(e);
        public static void EmitEntityDerrubada(IEntidade e, IEntidade causador) => EntityDerrubada?.Invoke(e, causador);
        public static void EmitEntityReerguida(IEntidade e, IEntidade por) => EntityReerguida?.Invoke(e, por);
        public static void EmitDerrubadoProgresso(IEntidade e, float esvaecimento, float reerguer) => DerrubadoProgresso?.Invoke(e, esvaecimento, reerguer);
        public static void EmitMatchStarted() => MatchStarted?.Invoke();
        public static void EmitMatchOver(bool victory) => MatchOver?.Invoke(victory);
        public static void EmitManaChanged(float current, float max) => ManaChanged?.Invoke(current, max);
        public static void EmitHealthChanged(float current, float max) => HealthChanged?.Invoke(current, max);
        public static void EmitElementChanged(Elemento e) => ElementChanged?.Invoke(e);
        public static void EmitGameStartRequested() => GameStartRequested?.Invoke();
        public static void EmitTerrainHit(Elemento e, Vector3 pos, bool strong) => TerrainHit?.Invoke(e, pos, strong);
        public static void EmitTerrainChanged(string kind, Vector3 pos) => TerrainChanged?.Invoke(kind, pos);
        public static void EmitPlayerKilledBot(string botName) => PlayerKilledBot?.Invoke(botName);
        public static void EmitDodgePerformed() => DodgePerformed?.Invoke();
        public static void EmitSpellCast(Elemento e) => SpellCast?.Invoke(e);
        public static void EmitDisparo(IEntidade pawn, Vector3 pos) => Disparo?.Invoke(pawn, pos);
        public static void EmitLootPrompt(string nome, string raridade, bool perto) => LootPrompt?.Invoke(nome, raridade, perto);
        public static void EmitWeaponEquipped(IEntidade pawn, string armaId, string nome, string raridade, Elemento[] elementos) => WeaponEquipped?.Invoke(pawn, armaId, nome, raridade, elementos);
        public static void EmitKitBound(string slug, bool implementado) => KitBound?.Invoke(slug, implementado);
        public static void EmitKitCooldown(string tipo, float restante, float total) => KitCooldown?.Invoke(tipo, restante, total);
        public static void EmitKitTelegraph(string slug, string tipo, float duracao, Vector3 pos) => KitTelegraph?.Invoke(slug, tipo, duracao, pos);
        public static void EmitKitState(string nome, bool ligado) => KitState?.Invoke(nome, ligado);
        public static void EmitBauAnunciado(Vector3 pos, float segundos) => BauAnunciado?.Invoke(pos, segundos);
        public static void EmitBauPousou(Vector3 pos) => BauPousou?.Invoke(pos);
        public static void EmitBauCanalizando(IEntidade pawn, float progresso) => BauCanalizando?.Invoke(pawn, progresso);
        public static void EmitBauAberto(bool porPlayer, Elemento[] elementos) => BauAberto?.Invoke(porPlayer, elementos);
        public static void EmitStatusAplicado(IEntidade alvo, string nome) => StatusAplicado?.Invoke(alvo, nome);
        public static void EmitQuedaFase(string fase) => QuedaFase?.Invoke(fase);
        public static void EmitQuedaAltura(float metros, float velocidade) => QuedaAltura?.Invoke(metros, velocidade);
        public static void EmitCasteloRota(Vector3 inicio, Vector3 fim, float duracao) => CasteloRota?.Invoke(inicio, fim, duracao);
        public static void EmitZonaAbertura(float segundos) => ZonaAbertura?.Invoke(segundos);
        public static void EmitZonaFormando(float raio, float duracao) => ZonaFormando?.Invoke(raio, duracao);
        public static void EmitZonaAvisou(int fase, Vector3 centro, float raio, float segundos) => ZonaAvisou?.Invoke(fase, centro, raio, segundos);
        public static void EmitZonaFechando(int fase, Vector3 centro, float raio, float duracao) => ZonaFechando?.Invoke(fase, centro, raio, duracao);
        public static void EmitZonaDano(float dano, float dps) => ZonaDano?.Invoke(dano, dps);
        public static void EmitZonaEstado(bool dentro) => ZonaEstado?.Invoke(dentro);

        /// <summary>Zera TODOS os ouvintes. Chamar no SetUp de cada teste e ao trocar de cena.</summary>
        public static void Reset()
        {
            DamageApplied = null; ShieldChanged = null; ShieldBroken = null; EntityDied = null;
            EntityDerrubada = null; EntityReerguida = null; DerrubadoProgresso = null;
            MatchStarted = null; MatchOver = null; ManaChanged = null; HealthChanged = null;
            ElementChanged = null; GameStartRequested = null; TerrainHit = null; TerrainChanged = null;
            PlayerKilledBot = null; DodgePerformed = null; SpellCast = null; Disparo = null;
            LootPrompt = null; WeaponEquipped = null; KitBound = null; KitCooldown = null;
            KitTelegraph = null; KitState = null; BauAnunciado = null; BauPousou = null;
            BauCanalizando = null; BauAberto = null; StatusAplicado = null; QuedaFase = null;
            QuedaAltura = null; CasteloRota = null; ZonaAbertura = null; ZonaFormando = null;
            ZonaAvisou = null; ZonaFechando = null; ZonaDano = null; ZonaEstado = null;
        }
    }
}
