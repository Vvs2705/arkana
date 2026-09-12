# mobile-unity — o produto

O jogo de celular, em **Unity 6000.3.23f1** (URP, Input System, uGUI, Test
Framework). Reescrito a partir de `design/` e do que o `mobile-godot/` provou —
**nenhuma linha de GDScript foi traduzida**: se releu a decisão e se escreveu em C#.

## Como rodar

```
powershell -File mobile-unity\portao.ps1      # O PORTAO: compila + testes EditMode + PlayMode
powershell -File mobile-unity\foto.ps1        # FOTOS do jogo rodando (com GPU) em Logs/fotos/ + diag.txt
powershell -File mobile-unity\build_apk.ps1   # APK de teste (datado, em mobile-unity/Builds/testes/)
```

**Toda leva visual termina olhando as fotos.** Teste verde não diz se o mago
saiu rosa ou se a câmera entrou na pedra; a foto diz — e o `diag.txt` diz o que
a câmera e o corpo estão tocando, para não virar palpite.

Sucesso do portão é a linha `ARKANA: N testes, 0 falhas`. Os dois scripts abrem
o Unity Hub sozinhos se ele estiver fechado (a licença Personal só resolve com
ele aberto). **Um Unity por vez**: o projeto tem trava de instância.

Abrir no editor: Unity Hub → Add → esta pasta. A cena principal se monta por
código (`Arkana/Montar cena Main` no menu do editor); tudo o mais nasce em runtime.

## Onde está cada coisa

`ARQUITETURA.md` é o contrato entre raias: pastas, donos, a forma pública do
Core, as regras que atravessaram. Leia antes de mexer.

| Pasta (`Assets/_Arkana/`) | O que é |
|---|---|
| `Scripts/Core/` | Bus, Balance (todos os números), Kits (20 magos), Combat (ponto único de dano), Velocidade, Vitalidade, Textos |
| `Scripts/World/` | Relevo procedural, Ilha (malha + colisor), Vegetação, Castelo, Sol |
| `Scripts/Gameplay/` | Partida, Pawn/Player/Bot, Locomoção, câmera, Zona, Queda, luvas/loot/Baú, Derrubado, Projétil, Efeitos, Água, KitRunner + `Habilidades/` |
| `Scripts/Terrain/` | terreno reativo (fogo por orçamento, gelo, elétrico, lama, muro) |
| `Scripts/Characters/` | Mago procedural (poses por código), identidade dos 20, luva visual |
| `Scripts/UI/`, `Menu/`, `Audio/` | HUD, gesto de disparo, joystick, avisos; menu/config/seleção/selo; 48 timbres sintetizados |
| `Scripts/Main.cs` | o ciclo de vida: Menu → Partida → Fim → Menu |
| `Editor/` | `Build` (settings + APK) e `MainSceneBuilder` |
| `Tests/EditMode/`, `Tests/PlayMode/` | o portão |

## A regra que este projeto carrega

**Lógica em classe pura, `MonoBehaviour` só como casca.** É o que deixa cada
sistema testável sem cena, e é por isso que o portão roda headless em minutos.
Todo teste novo se prova reintroduzindo o defeito.
