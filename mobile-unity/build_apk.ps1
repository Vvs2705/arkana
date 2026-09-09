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

$UnityArgs = @("-batchmode", "-nographics", "-projectPath", "`"$Proj`"", "-buildTarget", "Android",
               "-executeMethod", "Arkana.EditorTools.Build.Android", "-logFile", "`"$Log`"", "-quit")
$P = Start-Process -FilePath $Unity -ArgumentList $UnityArgs -Wait -PassThru -NoNewWindow
"---- build.log (ultimas 25 linhas) ----"
Get-Content $Log -Tail 25
if (Test-Path $Apk) {
    $Common = (git -C $Proj rev-parse --git-common-dir).Trim()
    $Root = Split-Path -Parent (Resolve-Path (Join-Path $Proj $Common))
    $Dest = Join-Path $Root "mobile-unity\Builds\testes"
    New-Item -ItemType Directory -Force $Dest | Out-Null
    $Stamped = Join-Path $Dest ("arkana-" + (Get-Date -Format "yyyy-MM-dd_HHmm") + ".apk")
    Copy-Item $Apk $Stamped -Force
    "APK: $Stamped ($([math]::Round((Get-Item $Apk).Length / 1MB, 1)) MB)"
} else {
    "APK NAO GERADO (Unity exit code $($P.ExitCode)). Leia $Log."
}
exit $P.ExitCode
