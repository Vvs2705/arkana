# mobile-unity — o produto

O jogo de celular, em **Unity 6000.3.23f1** (URP, Input System, uGUI, Test
Framework). Reescrito a partir de `design/` e do que o `mobile-godot/` provou —
**nenhuma linha de GDScript foi traduzida**: se releu a decisão e se escreveu em C#.

## Como rodar

```
powershell -File mobile-unity\portao.ps1      # O PORTAO: compila + testes EditMode + PlayMode
powershell -File mobile-unity\foto.ps1        # FOTOS do jogo rodando (com GPU) em Logs/fotos/ + diag.txt
powershell -File mobile-unity\build_apk.ps1   # APK de teste (datado, em mobile-unity/Builds/testes/)
powershell -File mobile-unity\emulador.ps1 -Build   # variante EMULADOR (ARM64+x86_64, GLES3, Development): instala, roda sem dedo, julga PASS/FAIL
```

**Emulador (desde 23/09):** existe um AVD `arkana_api35` (Android 15, x86_64 com
tradução ARM64, 2400×1080 paisagem, GPU do host, aceleração WHPX — funciona nesta
máquina). Ele **não mede FPS** (a régua é o Poco F4); serve para instalar, abrir,
jogar a partida automática e ler o logcat sem depender do cabo. Um de cada vez
com o Unity: são 8 GB de RAM.

```
%LOCALAPPDATA%\Android\Sdk\emulator\emulator.exe -avd arkana_api35 -gpu host                       # com janela
%LOCALAPPDATA%\Android\Sdk\emulator\emulator.exe -avd arkana_api35 -gpu host -no-window -no-audio   # headless
powershell -File mobile-unity\emulador.ps1                     # usa o Builds\arkana-emulador.apk que já existe
powershell -File mobile-unity\emulador.ps1 -Espera 180 -Bancada   # mais tempo de partida; roda a bancada de cortes
adb emu kill                                                    # encerrar o AVD
```

Saída em `Logs/emulador/<data>/` (`resultado.txt`, `tela.png`, `logcat.txt`,
`unity.txt`). PASS = processo vivo no fim, linhas `ARKANA FPS` e nenhum crash do
pacote. O mesmo `emulador.ps1` serve para o Poco F4 no cabo (o APK leva as duas
ABIs). O adb da cadeia é o do SDK em `%LOCALAPPDATA%\Android\Sdk` (37.0.1); o
Unity tem outro (36.0.0) que reinicia o servidor durante o build — por isso o
script faz `kill-server`/`start-server` uma vez antes de tocar no aparelho.
`habilitar_whpx.bat` é contingência (admin + reinício), só se o
`emulator -accel-check` deixar de responder "WHPX ... usable".

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
| `Editor/` | `Build` (settings + APK de produção, só ARM64; `AndroidEmulador` = variante ARM64+x86_64/GLES3/Development que devolve o `.asset` ao estado de produção) e `MainSceneBuilder` |
| `Tests/EditMode/`, `Tests/PlayMode/` | o portão |

## A regra que este projeto carrega

**Lógica em classe pura, `MonoBehaviour` só como casca.** É o que deixa cada
sistema testável sem cena, e é por isso que o portão roda headless em minutos.
Todo teste novo se prova reintroduzindo o defeito.
