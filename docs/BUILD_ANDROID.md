# Build Android (APK / AAB) — Arkana (protótipo 2D)

Pipeline web → Android via Capacitor. Empacota a frente **Protótipo 2D (Phaser)** —
não tem relação com a frente Roblox.

> **Estado real, verificado em 19/08/2026:** o APK de debug, o APK de release e o
> **.aab** compilam nesta máquina. Os caminhos e tamanhos abaixo saíram de builds que
> rodaram de verdade, não de estimativa.

| Artefato | Caminho | Tamanho | Assinado? |
|---|---|---|---|
| APK debug | `android/app/build/outputs/apk/debug/app-debug.apk` | 4,4 MB | sim, chave de debug — instala no aparelho |
| APK release | `android/app/build/outputs/apk/release/app-release-unsigned.apk` | 3,4 MB | **não** — só com keystore (§5) |
| Bundle release | `android/app/build/outputs/bundle/release/app-release.aab` | 3,3 MB | **não** — só com keystore (§5) |

---

## 1. Pré-requisitos

### Toolchain (já instalado nesta máquina)

| Item | Versão exigida pelo projeto | O que tem aqui |
|---|---|---|
| **JDK** | **21** | Temurin 21.0.12+8 em `C:\Users\VINICIUS\AppData\Local\Java\jdk-21.0.12+8` |
| Android SDK | platform **android-36**, build-tools 36.0.0 | `C:\Users\VINICIUS\AppData\Local\Android\Sdk` |
| Node.js | 18+ | v24.15.0 |

> **JDK 21, não 17.** Versões antigas deste documento pediam JDK 17 — está errado e
> quebra o build. O Capacitor 8 gera `android/app/capacitor.build.gradle` com
> `sourceCompatibility` / `targetCompatibility = VERSION_21`; com JDK 17 o Gradle
> reprova na compilação Java. O trio usado e testado é **Gradle 8.14.3 + Android
> Gradle Plugin 8.13.0 + JDK 21**.

> **Platform 36, não 34.** `android/variables.gradle` define `compileSdkVersion = 36`.
> Ter só `platforms;android-34` instalado não serve.

Numa máquina nova, o caminho curto é instalar o **Android Studio** (traz SDK Manager
e um JDK embutido) ou, sem Studio, as *command line tools* e depois:

```
sdkmanager "platform-tools" "platforms;android-36" "build-tools;36.0.0"
sdkmanager --licenses
```

### Variáveis de ambiente

| Variável | Valor nesta máquina |
|---|---|
| `JAVA_HOME` | `C:\Users\VINICIUS\AppData\Local\Java\jdk-21.0.12+8` |
| `ANDROID_HOME` | `C:\Users\VINICIUS\AppData\Local\Android\Sdk` |

O Gradle acha o SDK pelo `ANDROID_HOME`. Se preferir não depender do ambiente, crie
`android/local.properties` (não versionado) com:

```
sdk.dir=C:\\Users\\VINICIUS\\AppData\\Local\\Android\\Sdk
```

### Dependências do node

Num clone novo — ou num *worktree* recém-criado, que não herda `node_modules`:

```
npm ci
```

---

## 2. Build de debug — o que você roda para jogar no celular

```
npm run typecheck
npm run build:android
cd android
.\gradlew assembleDebug
```

- `npm run build:android` = `vite build` (gera `dist/`) + `cap sync android` (copia
  `dist/` para `android/app/src/main/assets/public/`).
- O primeiro `gradlew` baixa o Gradle 8.14.3 e as dependências — alguns minutos. Os
  seguintes levam ~15 a 60 segundos.

**Sai em:** `android\app\build\outputs\apk\debug\app-debug.apk`

> Mudou código web? Rode `npm run build:android` **antes** do `gradlew`. Sem isso o
> APK sai com a versão anterior do jogo — o Gradle não sabe que o `dist/` mudou.

---

## 3. Instalar no aparelho

### Via cabo (adb)
1. No Android: Configurações → Sobre o telefone → toque 7x em "Número da versão" →
   volte e ative **Depuração USB** em Opções do desenvolvedor.
2. `adb install -r android\app\build\outputs\apk\debug\app-debug.apk`

### Sem cabo
Copie o `app-debug.apk` para o aparelho (Drive, WhatsApp "mensagem para mim",
pendrive OTG), toque no arquivo e aceite "instalar de fonte desconhecida". O APK de
debug já vem assinado com a chave de debug do SDK, então instala normalmente.

---

## 4. Configuração de loja — o que cada valor significa

Tudo em `android/app/build.gradle` (`defaultConfig`) e `android/variables.gradle`.

| Chave | Valor | Por quê |
|---|---|---|
| `applicationId` | `br.com.vstack.arkana` | **Identidade permanente do app na Play.** Depois da primeira publicação **não muda nunca** — trocar significa app novo, sem os usuários nem as avaliações. Já é um ID próprio (não é o placeholder `com.example.app` do Capacitor); só confirme que é o nome que você quer carregar para sempre. |
| `versionCode` | `1` | Inteiro que a Play usa para saber o que é mais novo. **Tem de subir a cada envio para a loja** (1 → 2 → 3 …). Dois envios com o mesmo número são recusados. |
| `versionName` | `0.1.0` | Texto que o usuário vê. Era `"1.0"` (padrão do Capacitor), o que anunciava como versão final um protótipo — agora acompanha o `package.json` e o CHANGELOG. |
| `minSdkVersion` | `24` (Android 7.0) | Piso do Capacitor 8. Cobre praticamente todo aparelho em uso, e a Play não impõe mínimo. Mantido. |
| `targetSdkVersion` | `36` (Android 16) | **É aqui que mora a exigência da Play**, que obriga apps novos e atualizações a mirar uma API recente. Já estava em 36, a atual — nada a fazer. |
| `compileSdkVersion` | `36` | Casa com o `target`. Definido em `variables.gradle`. |
| Nome do app | `Arkana` | `android/app/src/main/res/values/strings.xml` (`app_name`). |
| Orientação | `sensorLandscape` | `AndroidManifest.xml`. O jogo é paisagem; gira entre as duas paisagens e ignora retrato. |
| Tela cheia | `android:windowFullscreen` | Temas em `res/values/styles.xml`. |
| Permissões | só `INTERNET` | Nada de localização, câmera, armazenamento ou identificadores. Simplifica a ficha de Segurança dos Dados. |

Sem SDK de anúncio, analytics de terceiro, rastreamento ou compra dentro do app —
alinhado à política de **zero caixa aleatória / só cosmético** do GDD.

---

## 5. Release assinado — passo a passo para quem nunca fez

Hoje `assembleRelease` e `bundleRelease` **rodam e terminam com sucesso**, mas o
artefato sai **sem assinatura** (repare no nome: `app-release-unsigned.apk`). A Play
recusa arquivo não assinado. Falta só a chave — e **gerar a chave é ato seu**, não da
equipe: quem cria a chave é quem guarda a senha.

### Passo 1 — gerar a chave (uma vez na vida do app)

Num terminal, **fora do repositório** (ex.: `C:\Users\VINICIUS\Documentos\chaves\`):

```
"C:\Users\VINICIUS\AppData\Local\Java\jdk-21.0.12+8\bin\keytool" -genkeypair -v -keystore arkana-upload.keystore -alias arkana -keyalg RSA -keysize 2048 -validity 10000
```

O `keytool` vai pedir, em ordem:
1. **Senha do keystore** — invente uma e anote.
2. Nome, unidade, organização, cidade, estado, país — pode ser seu nome / VStack /
   sua cidade / `BR`. Não aparece para o usuário final.
3. Confirmação: digite `sim` (ou `yes`).
4. **Senha da chave** — Enter reusa a do keystore, que é o mais simples.

Resultado: o arquivo `arkana-upload.keystore`.

> **Guarde esse arquivo e as senhas em pelo menos dois lugares** (gerenciador de
> senhas + backup offline). A chave vale ~27 anos (`-validity 10000`).

### Passo 2 — apontar o build para a chave

Crie `android/keystore.properties`. O `.gitignore` já bloqueia esse arquivo, o
`*.keystore` e o `*.jks` — **nada disso entra no Git**.

```
storeFile=C:/Users/VINICIUS/Documentos/chaves/arkana-upload.keystore
storePassword=SUA_SENHA_DO_KEYSTORE
keyAlias=arkana
keyPassword=SUA_SENHA_DA_CHAVE
```

Use barras normais `/` mesmo no Windows. Caminho absoluto, ou relativo à pasta
`android/`.

Não precisa editar nada no Gradle: `android/app/build.gradle` já lê esse arquivo e
liga a assinatura quando ele existe. Quando ele **não** existe, o build de release
continua funcionando e sai sem assinatura — ninguém fica travado por falta de chave.

### Passo 3 — gerar o artefato assinado

```
npm run build:android
cd android
.\gradlew bundleRelease
```

**Sai em:** `android\app\build\outputs\bundle\release\app-release.aab`

O `.aab` (**Android App Bundle**) é o formato **exigido** para apps novos na Play; o
APK só serve para instalar direto no aparelho. Para gerar também um APK assinado
(instalação manual, teste com amigos), rode `.\gradlew assembleRelease`: com a chave
configurada o arquivo passa a se chamar `app-release.apk`, sem o `-unsigned`.

### Passo 4 — conferir que assinou de verdade

```
"C:\Users\VINICIUS\AppData\Local\Android\Sdk\build-tools\36.0.0\apksigner.bat" verify -v --print-certs android\app\build\outputs\apk\release\app-release.apk
```

Tem de responder `Verifies` e mostrar o **seu** nome no `Signer #1 certificate DN`.
Se aparecer `CN=Android Debug`, o `keystore.properties` está apontando para a chave
de debug e esse arquivo **não** serve para a loja.

### Passo 5 — subir (fora do escopo da equipe)

Criar a conta de desenvolvedor, aceitar termos, preencher ficha da loja, Segurança
dos Dados, classificação indicativa e enviar o `.aab` são atos seus no Play Console.
Na primeira subida a Play oferece o **Play App Signing**: você envia assinado com a
sua chave de *upload* e o Google reassina com a chave final de distribuição. Aceitar
é o padrão, e é o que dá recuperação caso a chave de upload se perca.

> Antes de cada nova subida: **incremente o `versionCode`** em
> `android/app/build.gradle`. É o erro nº 1 de quem está começando.

---

## 6. Limitações conhecidas

- **Fontes vêm da internet.** `index.html` carrega Google Fonts (Cinzel, Chakra
  Petch, Inter) por rede. Offline o jogo abre, mas com fontes de fallback, e o app
  faz uma chamada externa no primeiro carregamento. Para um app de loja o certo é
  embutir as fontes no `dist/` — pendência do dono do protótipo 2D, não do build.
- **Bundle JS de ~1,6 MB** (381 kB gzip), quase tudo Phaser. Aceitável para APK,
  já que o asset vai local dentro do pacote; sem urgência de *code splitting*.
- `minifyEnabled false` no release: o código de jogo é JS dentro do WebView, então o
  R8 não encolheria nada relevante — só adicionaria risco de build.

---

## 7. Referência rápida

```
npm ci                    # uma vez por clone/worktree
npm run typecheck         # tem de passar limpo
npm run build:android     # vite build + cap sync

cd android
.\gradlew assembleDebug     # APK para jogar
.\gradlew assembleRelease   # APK de release (sem chave: -unsigned)
.\gradlew bundleRelease     # .aab para a Play (precisa da chave)
.\gradlew clean             # quando o build ficar estranho
```
