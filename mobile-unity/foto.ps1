# mobile-unity/foto.ps1 — FOTOS do jogo rodando, COM GPU (sem -nographics).
# Uso: powershell -File mobile-unity\foto.ps1
# Saida: mobile-unity/Logs/fotos/*.png (fora do git). Julgar pelo quadro, nao so' pelo numero (regua do Godot).
# Nao roda junto com o portao: um Unity por vez (trava de instancia).
$ErrorActionPreference = "Stop"
$Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"
$Proj = $PSScriptRoot

# LICENCA: sem o Unity Hub aberto o batchmode sai com 198 (ver portao.ps1). Abrir pelo AppID, nunca por "unityhub://".
if (-not (Get-Process "Unity Hub" -ErrorAction SilentlyContinue)) {
    Start-Process "shell:AppsFolder\UnityTechnologies.UnityHub_2vrhnee42bhxm!UnityHub"
    Start-Sleep -Seconds 25
}
$Logs = Join-Path $Proj "Logs"
$Fotos = Join-Path $Logs "fotos"
New-Item -ItemType Directory -Force $Fotos | Out-Null
Get-ChildItem $Fotos -Filter *.png -ErrorAction SilentlyContinue | Remove-Item
$Log = Join-Path $Logs "foto.log"
$Xml = Join-Path $Logs "foto-resultados.xml"
if (Test-Path $Xml) { Remove-Item $Xml }

$UnityArgs = @("-batchmode", "-projectPath", "`"$Proj`"",
               "-runTests", "-testPlatform", "PlayMode", "-testFilter", "FotoTests",
               "-testResults", "`"$Xml`"", "-logFile", "`"$Log`"")
$P = Start-Process -FilePath $Unity -ArgumentList $UnityArgs -Wait -PassThru -NoNewWindow

if (Test-Path $Xml) {
    [xml]$R = Get-Content $Xml
    $R.SelectNodes("//test-case") | ForEach-Object { Write-Host "$($_.result): $($_.name)" }
    $R.SelectNodes("//test-case[@result='Failed']") | ForEach-Object { Write-Host ($_.failure.message.'#cdata-section') }
} else {
    Write-Host "SEM RESULTADO (Unity exit $($P.ExitCode)). Leia $Log."
}
Get-ChildItem $Fotos -Filter *.png | ForEach-Object { Write-Host "FOTO: $($_.FullName) ($([math]::Round($_.Length / 1KB)) KB)" }
exit $P.ExitCode
