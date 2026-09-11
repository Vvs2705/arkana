# mobile-unity/portao.ps1 — O PORTAO: compila e roda os testes EditMode, headless.
# Uso: powershell -File mobile-unity\portao.ps1
# Sucesso e' a linha "ARKANA: N testes, 0 falhas" no fim. Qualquer falha sai com codigo 1.
$ErrorActionPreference = "Stop"
$Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"
$Proj = $PSScriptRoot

# LICENCA: a Personal e' resolvida online pelo cliente de licenca que o Unity HUB sobe. Com o Hub
# fechado o batchmode morre com "No valid Unity Editor license found" (exit 198) — medido em
# 09/09 e 11/09/2026. Entao: Hub fechado -> abre e espera o cliente subir.
if (-not (Get-Process "Unity Hub" -ErrorAction SilentlyContinue)) {
    # NUNCA por "unityhub://": sem caminho o Hub entende "instalar editor" e abre o aviso
    # "Nao e' possivel instalar / versao do Editor arquivado" (visto pelo Diretor em 11/09/2026).
    Start-Process "shell:AppsFolder\UnityTechnologies.UnityHub_2vrhnee42bhxm!UnityHub"
    Start-Sleep -Seconds 25
}
$Logs = Join-Path $Proj "Logs"
New-Item -ItemType Directory -Force $Logs | Out-Null
$Log = Join-Path $Logs "portao.log"
$Xml = Join-Path $Logs "portao-resultados.xml"
if (Test-Path $Xml) { Remove-Item $Xml }

$UnityArgs = @("-batchmode", "-nographics", "-projectPath", "`"$Proj`"",
               "-runTests", "-testPlatform", "EditMode",
               "-testResults", "`"$Xml`"", "-logFile", "`"$Log`"")
$P = Start-Process -FilePath $Unity -ArgumentList $UnityArgs -Wait -PassThru -NoNewWindow

if (-not (Test-Path $Xml)) {
    "PORTAO VERMELHO: nao houve resultado de teste (compilou?). Unity exit $($P.ExitCode). Erros do log:"
    Select-String -Path $Log -Pattern "error CS|Exception|Error:" | Select-Object -First 30 | ForEach-Object { $_.Line }
    exit 1
}
[xml]$R = Get-Content $Xml
$Run = $R.'test-run'
$Falhas = [int]$Run.failed
$Total = [int]$Run.total
$R.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
    "FALHOU: $($_.fullname)"
    $_.failure.message.'#cdata-section'
}
"ARKANA: $Total testes, $Falhas falhas (Unity exit $($P.ExitCode))"
if ($Falhas -gt 0 -or $Total -eq 0) { exit 1 }
exit 0
