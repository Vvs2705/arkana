# mobile-unity/emulador.ps1 - build da variante EMULADOR, instala, roda SEM DEDO e julga (PASS/FAIL) pelo logcat.
# Uso: powershell -File mobile-unity\emulador.ps1 [-Build] [-Apk caminho] [-Serial X] [-Auto partida|treino|nenhum]
#                                                [-Fps 300] [-Espera 90] [-Bancada] [-Adb caminho]
#   -Build    gera Builds\arkana-emulador.apk pelo Unity (Arkana.EditorTools.Build.AndroidEmulador: ARM64+x86_64,
#             GLES3, Development) e copia datada para <clone principal>\mobile-unity\Builds\testes\. Sem -Build usa o
#             APK que ja' existe. Serve para o AVD x86_64 e para o aparelho no cabo (o APK leva as duas ABIs).
#   -Auto     extra de intent que o jogo ja' entende (PartidaPeloAdb.cs): a partida comeca sozinha, sem toque.
#   -Espera   segundos de jogo antes de coletar (o MedidorDeFps escreve "ARKANA FPS ..." a cada 5 s).
# Saida: Logs\emulador\yyyy-MM-dd_HHmm\ (resultado.txt, tela.png, logcat.txt, unity.txt, meminfo.txt) - fora do git.
# Codigos: 0 PASS | 1 FAIL | 2 sem aparelho/emulador | 3 sem APK | 4 install falhou | 5 build do Unity falhou.
# Nao apaga dados do app (sem pm clear / uninstall). Um Unity por vez; nao roda junto com o portao.
param(
    [switch]$Build,
    [string]$Apk = "",
    [string]$Serial = "",
    [ValidateSet("partida", "treino", "nenhum")][string]$Auto = "partida",
    [int]$Fps = 300,
    [int]$Espera = 90,
    [switch]$Bancada,
    [string]$Adb = (Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe")
)
$ErrorActionPreference = "Stop"
$Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"
$Proj = $PSScriptRoot
$Pacote = "br.com.vstack.arkana"
$Atividade = "$Pacote/com.unity3d.player.UnityPlayerGameActivity"
$Builds = Join-Path $Proj "Builds"
if (-not $Apk) { $Apk = Join-Path $Builds "arkana-emulador.apk" }
if (-not (Test-Path $Adb)) { $Adb = "adb" }   # sem o SDK do usuario, o adb do PATH
$Carimbo = Get-Date -Format "yyyy-MM-dd_HHmm"
$Saida = Join-Path $Proj "Logs\emulador\$Carimbo"
New-Item -ItemType Directory -Force $Saida | Out-Null
$Resultado = Join-Path $Saida "resultado.txt"
$Registro = New-Object System.Collections.Generic.List[string]
$Passo = 0

function Passo([string]$Msg) { $script:Passo++; Write-Host "[$script:Passo/9] $Msg"; $Registro.Add("[$script:Passo/9] $Msg") }
function Anotar([string]$Msg) { Write-Host "      $Msg"; $Registro.Add("      $Msg") }
function Sair([int]$Rc, [string]$Veredito) {
    Write-Host $Veredito
    $Registro.Insert(0, $Veredito)
    $Registro.Insert(1, "quando=$Carimbo  exit=$Rc")
    $Registro | Set-Content $Resultado -Encoding utf8
    Write-Host "resultado: $Resultado"
    exit $Rc
}

# adb escreve o erro no stderr; no PowerShell 5.1 um "2>&1" direto vira ErrorRecord e, com Stop, derruba o script.
# Entao: Process do .NET, stderr lido em paralelo (sem deadlock), stdout em texto ou - com -ParaArquivo - em bytes crus
# (screencap/logcat). Devolve stdout+stderr juntos; o codigo de saida fica em $AdbRc.
function Adb([string]$Linha, [string]$ParaArquivo = "") {
    $psi = New-Object System.Diagnostics.ProcessStartInfo($Adb, $Linha)
    $psi.UseShellExecute = $false; $psi.RedirectStandardOutput = $true; $psi.RedirectStandardError = $true
    $p = [System.Diagnostics.Process]::Start($psi)
    $err = $p.StandardError.ReadToEndAsync()
    if ($ParaArquivo) {
        $fs = [System.IO.File]::Create($ParaArquivo)
        $p.StandardOutput.BaseStream.CopyTo($fs); $fs.Close(); $out = ""
    } else { $out = $p.StandardOutput.ReadToEnd() }
    $p.WaitForExit()
    $script:AdbRc = $p.ExitCode
    return ($out + $err.Result).TrimEnd()
}
function NoAparelho([string]$Linha, [string]$ParaArquivo = "") { return Adb "-s $Serial $Linha" $ParaArquivo }

# ---- (opcional) build da variante pelo Unity - mesma receita do build_apk.ps1 -------------------------------------
if ($Build) {
    Write-Host "[build] Unity -executeMethod Arkana.EditorTools.Build.AndroidEmulador (5-12 min: IL2CPP para 2 ABIs)"
    New-Item -ItemType Directory -Force $Builds | Out-Null
    $Log = Join-Path $Builds "build-emulador.log"
    $Apk = Join-Path $Builds "arkana-emulador.apk"
    if (Test-Path $Apk) { Remove-Item $Apk }
    # LICENCA: sem o Unity Hub aberto o batchmode sai com 198 (ver portao.ps1). Abrir pelo AppID, nunca por "unityhub://".
    if (-not (Get-Process "Unity Hub" -ErrorAction SilentlyContinue)) {
        Start-Process "shell:AppsFolder\UnityTechnologies.UnityHub_2vrhnee42bhxm!UnityHub"
        Start-Sleep -Seconds 25
    }
    $T0 = Get-Date
    $UnityArgs = @("-batchmode", "-nographics", "-projectPath", "`"$Proj`"", "-buildTarget", "Android",
                   "-executeMethod", "Arkana.EditorTools.Build.AndroidEmulador", "-logFile", "`"$Log`"", "-quit")
    $P = Start-Process -FilePath $Unity -ArgumentList $UnityArgs -PassThru -NoNewWindow
    $null = $P.Handle   # sem tocar no handle antes do fim, o ExitCode volta vazio (quirk do PowerShell)
    $P.WaitForExit()    # nunca -Wait: espera a arvore, e o VBCSCompiler do Unity fica vivo ~10 min (ver portao.ps1)
    $Dur = [int]((Get-Date) - $T0).TotalSeconds
    if (-not (Test-Path $Apk)) {
        Write-Host "APK NAO GERADO (Unity exit $($P.ExitCode), ${Dur}s). Erros de ${Log}:"
        Select-String -Path $Log -Pattern "error CS|Exception|Error:" | Select-Object -First 30 | ForEach-Object { Write-Host $_.Line }
        Sair 5 "FAIL: build do Unity falhou (exit $($P.ExitCode)); leia $Log"
    }
    # O APK datado vai SEMPRE para a pasta do clone principal, venha de worktree ou nao (ordem do Diretor de 27/08/2026).
    $Common = (git -C $Proj rev-parse --git-common-dir).Trim()
    $Root = Split-Path -Parent (Resolve-Path (Join-Path $Proj $Common))
    $Dest = Join-Path $Root "mobile-unity\Builds\testes"
    New-Item -ItemType Directory -Force $Dest | Out-Null
    $Stamped = Join-Path $Dest ("arkana-emulador-" + $Carimbo + ".apk")
    Copy-Item $Apk $Stamped -Force
    Write-Host "[build] OK em ${Dur}s (Unity exit $($P.ExitCode)); copia: $Stamped"
    $Registro.Add("build=${Dur}s unity_exit=$($P.ExitCode) copia=$Stamped")
}

# ---- 1. APK ----------------------------------------------------------------------------------------------------
Passo "APK"
if (-not (Test-Path $Apk)) { Sair 3 "FAIL: sem APK em $Apk (rode com -Build ou aponte -Apk)" }
$ApkMb = [math]::Round((Get-Item $Apk).Length / 1MB, 1)
$Sha = (Get-FileHash $Apk -Algorithm SHA256).Hash.Substring(0, 12).ToLower()
Anotar "$Apk ($ApkMb MB, sha256 $Sha)"

# ---- 2. adb: uma cadeia so' -----------------------------------------------------------------------------------------
Passo "adb"
$AdbVersao = (Adb "version") -split "`n" | Select-Object -First 1
if ($AdbRc -ne 0 -or -not $AdbVersao) { Sair 2 "FAIL: adb nao roda ($Adb)" }
Anotar "$AdbVersao ($Adb)"
# O adb do Unity (36.0.0) reinicia o servidor durante o build; aqui a cadeia e' unificada UMA vez, no adb do SDK.
$null = Adb "kill-server"; $null = Adb "start-server"

# ---- 3. aparelho -------------------------------------------------------------------------------------------------
Passo "aparelho"
$Lista = (Adb "devices -l") -split "`n" | Where-Object { $_ -match "^(\S+)\s+device\b" } | ForEach-Object { $Matches[1] }
$Lista = @($Lista)
if ($Serial) {
    if ($Lista -notcontains $Serial) { Sair 2 "FAIL: serial $Serial nao esta' em 'device' (vistos: $($Lista -join ', '))" }
} elseif ($Lista.Count -eq 0) {
    Sair 2 "FAIL: nenhum aparelho/emulador: inicie o AVD ou ligue o cabo"
} elseif ($Lista.Count -gt 1) {
    Sair 2 "FAIL: mais de um aparelho ($($Lista -join ', ')): escolha com -Serial"
} else { $Serial = $Lista[0] }
$Modelo = NoAparelho "shell getprop ro.product.model"
$Sdk = NoAparelho "shell getprop ro.build.version.sdk"
$Abis = NoAparelho "shell getprop ro.product.cpu.abilist"
Anotar "serial=$Serial modelo=$Modelo sdk=$Sdk abilist=$Abis"

# ---- 4. install ---------------------------------------------------------------------------------------------------
Passo "install -r"
$Inst = NoAparelho "install -r `"$Apk`""
Anotar ($Inst -replace "`n", " | ")
if ($Inst -match "INSTALL_FAILED_NO_MATCHING_ABIS") {
    Sair 4 "FAIL: o aparelho ($Abis) nao aceita as ABIs do APK - para o AVD x86_64 o APK precisa vir de -Build (variante ARM64+x86_64), nao do build_apk.ps1 (so' ARM64)"
}
if ($AdbRc -ne 0 -or $Inst -notmatch "Success") { Sair 4 "FAIL: install falhou (adb exit $AdbRc): $Inst" }

# ---- 5. start ----------------------------------------------------------------------------------------------------
Passo "start"
$null = NoAparelho "logcat -c"
$null = NoAparelho "shell am force-stop $Pacote"
# Aparelho/AVD novo mostra o aviso "Viewing full screen / Got it" na 1a vez em modo imersivo; o Unity perde o foco e
# PAUSA (runInBackground=0) - zero quadros, zero "ARKANA FPS" (visto no AVD em 23/09). Marcar como confirmado resolve.
$null = NoAparelho "shell settings put secure immersive_mode_confirmations confirmed"
# Aparelho no cabo costuma estar com a tela dormindo: acorda e dispensa a tela de bloqueio (so' funciona sem PIN;
# com PIN o Diretor destrava na mao). Sem tela acesa o Unity nao desenha e a rodada sai FAIL sem "ARKANA FPS".
$null = NoAparelho "shell input keyevent KEYCODE_WAKEUP"
$null = NoAparelho "shell wm dismiss-keyguard"
$Extras = "--ei arkana_fps $Fps"
if ($Auto -ne "nenhum") { $Extras = "--es arkana_auto $Auto $Extras" }
if ($Bancada) { $Extras += " --ei arkana_bancada 1" }
$Start = NoAparelho "shell am start -n $Atividade $Extras"
Anotar ($Start -replace "`n", " | ")
if ($Start -match "Error") { Sair 1 "FAIL: am start recusou: $Start" }
Start-Sleep -Seconds 3
$Pid_ = (NoAparelho "shell pidof $Pacote").Trim()
Anotar "pid=$Pid_ extras: $Extras"

# ---- 6. espera ---------------------------------------------------------------------------------------------------
Passo "espera ${Espera}s (pidof a cada 15 s)"
$Morreu = 0
$T = 0
while ($T -lt $Espera) {
    $Fatia = [math]::Min(15, $Espera - $T)
    Start-Sleep -Seconds $Fatia
    $T += $Fatia
    $Vivo = (NoAparelho "shell pidof $Pacote").Trim()
    if (-not $Vivo) { $Morreu = $T; Anotar "processo morreu aos ${T}s"; break }
}
$VivoNoFim = -not $Morreu

# ---- 7. coleta ---------------------------------------------------------------------------------------------------
Passo "coleta"
$Png = Join-Path $Saida "tela.png"; $LogTudo = Join-Path $Saida "logcat.txt"; $LogUnity = Join-Path $Saida "unity.txt"
$null = NoAparelho "exec-out screencap -p" $Png
$null = NoAparelho "logcat -d -v time" $LogTudo
$null = NoAparelho "logcat -d -v time -s Unity" $LogUnity
$Mem = (NoAparelho "shell dumpsys meminfo $Pacote") -split "`n" | Select-Object -First 12
$Mem | Set-Content (Join-Path $Saida "meminfo.txt") -Encoding utf8
Anotar "tela=$Png ($([math]::Round((Get-Item $Png).Length / 1KB)) KB) logcat=$LogTudo unity=$LogUnity"

# ---- 8. veredito -------------------------------------------------------------------------------------------------
Passo "veredito"
$Fps_ = @(Select-String -Path $LogUnity -Pattern "ARKANA FPS|ARKANA BANCADA" | ForEach-Object { $_.Line })
# Linha de crash SO' conta se for do pacote: pelo nome ou pelo pid (formato -v time: "TAG( 1234):").
$PadraoCrash = "FATAL EXCEPTION|Fatal signal|SIGSEGV|SIGABRT|tombstone|CRASH|Unable to instantiate|DllNotFoundException"
$Crash = @(Select-String -Path $LogTudo -Pattern $PadraoCrash -CaseSensitive | ForEach-Object { $_.Line } |
           Where-Object { $_ -match [regex]::Escape($Pacote) -or ($Pid_ -and $_ -match "\(\s*$Pid_\)") })
$Falhas = @()
if (-not $VivoNoFim) { $Falhas += "processo morreu aos ${Morreu}s" }
if ($Fps_.Count -eq 0) { $Falhas += "nenhuma linha 'ARKANA FPS' no logcat (o jogo nao chegou a rodar quadros)" }
if ($Crash.Count -gt 0) { $Falhas += "crash no logcat ($($Crash.Count) linha(s))" }
Anotar "vivo_no_fim=$VivoNoFim linhas_fps=$($Fps_.Count) linhas_crash=$($Crash.Count)"
$Fps_ | Select-Object -Last 10 | ForEach-Object { Anotar $_ }
$Crash | Select-Object -First 5 | ForEach-Object { Anotar "CRASH: $_" }

# ---- 9. resultado -------------------------------------------------------------------------------------------------
Passo "resultado"
$Registro.Add("serial=$Serial modelo=$Modelo sdk=$Sdk abilist=$Abis")
$Registro.Add("apk=$Apk ($ApkMb MB) sha256=$Sha adb=$AdbVersao auto=$Auto fps=$Fps espera=${Espera}s bancada=$Bancada")
$Registro.Add("criterios: vivo_no_fim=$VivoNoFim linhas_fps=$($Fps_.Count) linhas_crash=$($Crash.Count)")
$Registro.Add("arquivos: $Png ; $LogTudo ; $LogUnity")
if ($Falhas.Count -eq 0) { Sair 0 "PASS: $Modelo ($Serial) rodou ${Espera}s, $($Fps_.Count) medicoes de FPS, sem crash" }
Sair 1 ("FAIL: " + ($Falhas -join "; "))
