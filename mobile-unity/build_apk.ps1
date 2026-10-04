# mobile-unity/build_apk.ps1 — APK de teste por linha de comando.
# Uso: powershell -File mobile-unity\build_apk.ps1
# O APK datado vai SEMPRE para a pasta do clone principal (mobile-unity/Builds/testes/),
# venha de worktree ou nao — ordem do Diretor de 27/08/2026.
$ErrorActionPreference = "Stop"
$Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"
$Proj = $PSScriptRoot
$Builds = Join-Path $Proj "Builds"
$Log = Join-Path $Builds "build.log"
$Apk = Join-Path $Builds "arkana.apk"
New-Item -ItemType Directory -Force $Builds | Out-Null

# LICENCA: sem o Unity Hub aberto o batchmode sai com 198 (ver portao.ps1).
if (-not (Get-Process "Unity Hub" -ErrorAction SilentlyContinue)) {
    # NUNCA por "unityhub://": sem caminho o Hub entende "instalar editor" (ver portao.ps1).
    Start-Process "shell:AppsFolder\UnityTechnologies.UnityHub_2vrhnee42bhxm!UnityHub"
    Start-Sleep -Seconds 25
}

# O BUILD NAO PODE SUJAR O REPO (04/10/2026: 22 commits de "Main.unity regravada pelo build"): fotografa o git antes e
# compara depois; o que o build mudou em arquivo VERSIONADO falha com 6 (os artefatos esperados ja' estao no .gitignore).
$Antes = @(git -C $Proj status --porcelain -- .)
$UnityArgs = @("-batchmode", "-nographics", "-projectPath", "`"$Proj`"", "-buildTarget", "Android",
               "-executeMethod", "Arkana.EditorTools.Build.Android", "-logFile", "`"$Log`"", "-quit")
$P = Start-Process -FilePath $Unity -ArgumentList $UnityArgs -PassThru -NoNewWindow
$null = $P.Handle   # sem tocar no handle antes do fim, o ExitCode volta vazio (quirk do PowerShell)
$P.WaitForExit()   # nunca -Wait: espera a arvore, e o VBCSCompiler do Unity fica vivo ~10 min (ver portao.ps1)
"---- build.log (ultimas 25 linhas) ----"
Get-Content $Log -Tail 25
if (Test-Path $Apk) {
    $Common = (git -C $Proj rev-parse --git-common-dir).Trim()
    $Root = Split-Path -Parent (Resolve-Path (Join-Path $Proj $Common))
    $Dest = Join-Path $Root "mobile-unity\Builds\testes"
    New-Item -ItemType Directory -Force $Dest | Out-Null
    # So o APK atual fica: os anteriores sao apagados (ordem do Diretor de 01/10/2026 — disco cheio).
    Remove-Item (Join-Path $Dest "*.apk") -Force -ErrorAction SilentlyContinue
    $Stamped = Join-Path $Dest ("arkana-" + (Get-Date -Format "yyyy-MM-dd_HHmm") + ".apk")
    Move-Item $Apk $Stamped -Force
    "APK: $Stamped ($([math]::Round((Get-Item $Stamped).Length / 1MB, 1)) MB)"
} else {
    "APK NAO GERADO (Unity exit code $($P.ExitCode)). Leia $Log."
}
$Sujou = @(git -C $Proj status --porcelain -- . | Where-Object { $Antes -notcontains $_ })
if ($Sujou.Count -gt 0) {
    "BUILD SUJOU O REPO (arquivo versionado regravado pelo build):"
    $Sujou
    if ($P.ExitCode -eq 0) { exit 6 }
}
exit $P.ExitCode
