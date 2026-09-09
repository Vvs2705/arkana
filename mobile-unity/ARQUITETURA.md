# ARQUITETURA mobile-unity/ — contratos entre raias

Dono de `Assets/_Arkana/Scripts/Core/Bus.cs`, `Elemento.cs`, `IEntidade.cs`,
`ProjectSettings/`, `Packages/` e deste arquivo: COORDENADOR.

Unity 6000.3.23f1 · URP · Input System · uGUI · Test Framework. Sem pacote novo
sem pedir. C# 9. Namespaces por pasta: `Arkana.Core`, `Arkana.World`,
`Arkana.Gameplay`, `Arkana.Characters`, `Arkana.UI`, `Arkana.Menu`,
`Arkana.Audio`, `Arkana.Terrain`. Um assembly (`Arkana.asmdef`); testes em
`Arkana.Tests` (EditMode); editor em `Arkana.Editor`.

| Pasta (`Assets/_Arkana/Scripts/`) | Dona | Entrega |
|---|---|---|
| `Core/` | raia CORE | `Balance`, `Kits`, `Textos`, `Combat`, `Vitalidade`, `Velocidade`, `Status` |
| `World/` | raia MUNDO | `Ilha` (relevo procedural, agua, POIs, nascimentos, malha + colisor), `Castelo` (rota), `Sol` |
| `Gameplay/` | raia GAMEPLAY | `Zona`, `Queda`, `Loot`, `Arma`/`ArmaSlot`, `BauCelestial`, `Derrubado`, `Projetil`, `Pawn`, `Player`, `Bot`, `KitRunner`, `Habilidades/` |
| `Terrain/` | raia GAMEPLAY (2ª leva) | `TerrenoReativo` (grade de celulas, fogo por orcamento, gelo, eletrico, lama, muro) |
| `Characters/` | raia PERSONAGEM (2ª leva) | `Mago` (procedural + modelo externo com aliases de clipe), `IdentidadeMago` |
| `UI/` | raia UI | `Hud`, `JoystickVirtual`, `GestoDeDisparo`, `BotaoAcao`, `BotaoDisparo`, `CarrosselElementos`, `AreaSegura`, `Dp`, `FiltroDaltonismo`, `HudAviso` |
| `Menu/` | raia UI | `Menu`, `Config`, `SelecaoPersonagem`, `Elenco`, `Estilo`, `Selo` |
| `Audio/` | raia UI | `Sfx` (timbres sintetizados em `AudioClip`, zero arquivo de audio) |
| `Assets/_Arkana/Editor/` | COORDENADOR + raia CENA | `Build`, `MainSceneBuilder` (monta a cena por codigo) |

## O PORTAO

```
powershell -File mobile-unity\portao.ps1
```

Compila e roda TODOS os testes EditMode headless. Sucesso e' a linha
`ARKANA: N testes, 0 falhas`. **Um unico Unity por vez**: o projeto tem trava de
instancia, entao raias em paralelo NAO rodam o portao — escrevem codigo e teste,
e o coordenador compila e roda na integracao. Escreva C# que compila de
primeira: tipos explicitos, `using` completos, nada de API que voce nao tem
certeza que existe no Unity 6.

**Todo teste novo se prova reintroduzindo o defeito.** Se nao fica vermelho com
o defeito de volta, e' decorativo. Logica de jogo vive em classes PURAS (sem
`MonoBehaviour`), com `Tick(float dt)` e estado explicito, para o teste EditMode
instanciar sem cena. `MonoBehaviour` e' so' a casca que liga a classe pura ao
`GameObject`.

## Contrato do Core (a raia CORE implementa EXATAMENTE esta forma)

```csharp
namespace Arkana.Core
// Elemento.cs (pronto): enum Elemento { Fogo, Agua, Raio, Terra, Vento }; Elementos.Todos/Id/Nome/TryParse
// IEntidade.cs (pronto): string Nome; Vector3 Pos; bool EhPlayer; Vitalidade Vital
// Bus.cs (pronto): eventos estaticos + Emit* + Reset()

public static class Balance {
  public static class Player { public const float Hp=100, ManaMax=100, ManaRegen=16, Speed=7.5f, JumpV=5.2f; }
  public sealed class PerfilElemento { public float Dmg, ManaCost, FireRate, ProjectileSpeed, Range, Esc, Vida, Empurrao, Estrutura; }
  public static PerfilElemento Perfil(Elemento e);          // os 5 perfis de Balance.gd
  public static class Escudo { public static readonly float[] Niveis={50,75,100,125}; Evoluir={0,150,400,900}; Cores (hex); public const bool Transbordo=true; public const float Regen=0; }
  public static class Status { BurnDps, BurnDur, BurnFannedDps, BurnFannedBonus, WetDur, WetSlow, HypothermiaSlow, ConductMult, ConductStun, ConductArcM, ConductArcMult, StunCap=0.8f }
  public static class Dot { TetoDps=12, IgnoraEscudo=true, Tick=0.25f }
  public static class Feedback { ... os campos de FEEDBACK }
  public static class Combate { Knockback=2.2f, RefRangeM=12 }
  public static class Terrain { CellSize=3, FuelBudget=16, EdgeChance=0.5f, BurnDuration=8, BurnDps=6, FreezeDuration=10, ElectrifyDuration=3, ElectrifyDps=8, MudDuration=6, MudSlow=0.55f, WallHp=60, WallDuration=12, WallHeight=3.5f }
  public static class Dodge { Distance=5, Duration=0.18f, Cooldown=2.6f, Iframes=0.12f, Burst=1.85f, ExitMomentum=1 }
  public static class Flutuar { ManaPorS=22, DurMax=2, DescV=1.2f }
  public static class Move { Accel=45, Brake=60, TurnAccel=95, AirControl=0.35f, StickDeadzone=0.12f, StickCurve=1.4f, RunAnimEnter=1.3f, RunAnimExit=0.5f, TurnRateAim=15, TurnRateFree=10, TurnPivotBonus=0.7f, BankGain=0.035f, BankMax=0.13f, BankRate=9 }
  public static class Anim { RunStrideM=7.91f, ScaleMin=0.55f, ScaleMax=1.9f }
  public static class Touch { AimDeadzoneDp=14, TapMaxMs=220 }
  public static class Match { DurationS=480, Bots=12 }
}

public sealed class Vitalidade {           // estado puro de vida/escudo de UM pawn
  float Hp, HpMax, Escudo, EscudoMax; int Nivel /*1..4*/; float DanoCausado;
  bool Viva; // Hp > 0
  Vitalidade(float hpMax) // nasce com escudo nivel 1 cheio
}

public static class Combat {               // O UNICO lugar que aplica dano
  // Devolve o dano EFETIVO (escudo + vida). NaN/<=0 = 0 e nao emite. Ordem: escudo primeiro (Esc x), vida (Vida x).
  // Emite Bus.DamageApplied, ShieldChanged/ShieldBroken, EntityDied (uma vez), credita DanoCausado na fonte e evolui o escudo dela.
  public static float AplicarDano(IEntidade alvo, float dano, Elemento el, IEntidade fonte, bool forte=false, bool ignoraEscudo=false);
  public static float AplicarDot(IEntidade alvo, float dps, float dt, string tipo, IEntidade fonte); // teto Dot.TetoDps somado, direto na vida
  public static void Empurrar(...) // opcional
}

public static class Velocidade {           // PRODUTO UNICO: base x terreno x status x postura. Ninguem escreve velocidade por fora.
  public static float Produto(float baseMs, float terreno, float status, float postura);
}

public static class Kits {                 // dados dos 20 magos (Kits.gd): slug "01-pyra"...; TelegrafiaMin=1, TelegrafiaMax=4, CargaPorDano=0.0015f
  public sealed class KitDef { string Slug, Nome; bool Implementado; float TaticaCd, SupremaCarga, Telegrafia; Dictionary<string,float> Passiva, Tatica, Suprema; }
  public static IReadOnlyDictionary<string, KitDef> Magos; public static KitDef De(string slug); public static string[] Slugs; // ordem 01..20
}

public static class Textos { /* rotulos da HUD e do menu, em portugues: ArmaRotulo(nome, elementos), etc. — espelho de Textos.gd */ }
```

## Regras que atravessaram (o PROVADO do Godot e do Roblox)

- Dano passa por UM lugar (`Core.Combat`); NaN se barra com `!(x > 0)`.
- Velocidade e' produto unico (`Velocidade.Produto`) — ninguem escreve direto.
- Numero que o DEDO sente vive em **dp** (`Balance.Touch`, `UI.Dp`); do MUNDO, em metros.
- Cancelar e' estado de 1ª classe: o botao MOSTRA se soltar dispara (anel/X).
- Cor + FORMA sempre (GDD §10). Clima com COR, nunca falta de luz.
- Fogo propaga por ORCAMENTO (`Terrain.FuelBudget`), nunca chance por tique.
- Metro cravado envelhece calado: raio, altura e distancia sao FRACAO do mapa (`Ilha.RaioTerra`).
- A Zona nasce INERTE e liga no sinal de pouso. Seeds sorteados POR PARTIDA, guardados para a rede.
- Estado sustentado (derrubado) se resolve na fonte da decisao (`Pawn.AnimDeLocomocao()`), nao driblando ela.
- Sinal que diz "de quem" leva o `IEntidade` PRIMEIRO. Sinal na BORDA, nunca por frame.
- Fiacao defensiva: cada peca boota sem as outras (modelo externo ausente -> mago procedural; ilha ausente -> plano).

## Como o Godot resolveu (leitura obrigatoria da raia)

`mobile-godot/godot/<pasta>/*.gd` e o `selftest.gd` da pasta: o selftest diz O
QUE se cobra. O teste EditMode novo cobra os MESMOS invariantes, em C#. Nao se
traduz linha a linha: se le' a decisao em `design/`, se olha como o Godot fez,
e se escreve de novo.
