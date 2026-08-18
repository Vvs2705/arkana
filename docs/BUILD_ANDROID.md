# Build Android (APK) — Arkana

Pipeline web → APK via Capacitor. O projeto nativo já está gerado em `android/` (orientação travada em landscape, tela cheia). Falta só o SDK na máquina para compilar.

## 1. Pré-requisitos (uma vez só)

Escolha UMA das opções:

### Opção A — Android Studio (recomendado, mais simples)
1. Instale o [Android Studio](https://developer.android.com/studio) (já inclui JDK 17 e Android SDK).
2. Abra o Android Studio → More Actions → SDK Manager → confirme que **Android SDK Platform 34+** e **Android SDK Build-Tools** estão instalados.

### Opção B — Só linha de comando (sem Android Studio)
1. Instale o **JDK 17** (ex.: [Adoptium Temurin 17](https://adoptium.net/)).
2. Baixe as **command line tools** do Android em https://developer.android.com/studio#command-line-tools-only e extraia em `C:\Android\cmdline-tools\latest\`.
3. Instale os pacotes do SDK:
   ```
   C:\Android\cmdline-tools\latest\bin\sdkmanager "platform-tools" "platforms;android-34" "build-tools;34.0.0"
   ```
   (aceite as licenças quando pedir, ou rode `sdkmanager --licenses`)

### Variáveis de ambiente (as duas opções)
| Variável | Valor típico |
|---|---|
| `JAVA_HOME` | pasta do JDK 17 (Android Studio: `C:\Program Files\Android\Android Studio\jbr`) |
| `ANDROID_HOME` | `C:\Users\VINICIUS\AppData\Local\Android\Sdk` (Studio) ou `C:\Android` (opção B) |

Adicione ao `PATH`: `%ANDROID_HOME%\platform-tools`.

Alternativa sem mexer no ambiente: crie `android/local.properties` com uma linha:
```
sdk.dir=C:\\Users\\VINICIUS\\AppData\\Local\\Android\\Sdk
```

## 2. Build de debug (1 comando)

```
npm run build:android
cd android
.\gradlew assembleDebug
```

`npm run build:android` = `vite build` (gera `dist/`) + `cap sync android` (copia para o projeto nativo). O primeiro `gradlew` baixa o Gradle e dependências — demora alguns minutos; os próximos são rápidos.

**O APK aparece em:**
```
android\app\build\outputs\apk\debug\app-debug.apk
```

## 3. Instalar no aparelho

### Via USB (adb)
1. No Android: Configurações → Sobre o telefone → toque 7x em "Número da versão" → ative **Depuração USB** em Opções do desenvolvedor.
2. Conecte o cabo e rode:
   ```
   adb install -r android\app\build\outputs\apk\debug\app-debug.apk
   ```

### Sem cabo
Copie o `app-debug.apk` para o aparelho (WhatsApp "mensagem para mim", Google Drive, pendrive OTG), toque no arquivo e aceite "instalar de fonte desconhecida". APK de debug já vem assinado com chave de debug — instala normal.

## 4. Release assinado (futuro — só quando for publicar)

1. Gerar keystore (guarde o arquivo e as senhas — perder = nunca mais atualizar o app na Play Store):
   ```
   keytool -genkey -v -keystore arkana-release.keystore -alias arkana -keyalg RSA -keysize 2048 -validity 10000
   ```
2. Criar `android/keystore.properties` (NÃO commitar) com `storeFile`, `storePassword`, `keyAlias`, `keyPassword` e referenciar no `android/app/build.gradle` (bloco `signingConfigs`).
3. Build: `.\gradlew assembleRelease` → APK em `android\app\build\outputs\apk\release\`. Para Play Store, prefira `.\gradlew bundleRelease` (gera `.aab`).

## Notas

- `capacitor.config.ts`: appId `br.com.vstack.arkana`, webDir `dist`.
- Orientação travada: `android:screenOrientation="sensorLandscape"` no `AndroidManifest.xml`.
- Tela cheia: `android:windowFullscreen` nos temas em `android/app/src/main/res/values/styles.xml`.
- Sempre que mudar código web, rode `npm run build:android` antes do `gradlew` — senão o APK sai com a versão antiga.
- O `index.html` carrega fontes do Google Fonts via internet; offline o jogo abre, mas com fontes fallback.
