# mobile-unity/importar_mago.ps1 — o zip que o SITE da Meshy baixa (FBX, "Todos Adicionados", "Arquivo unico")
# vira Resources/magos/<slug>.fbx + <slug>-cor.png + <slug>-normal.png (a convencao que a ImportacaoArkana liga no material).
# Uso: powershell -File mobile-unity\importar_mago.ps1 <zip> <slug>     ex.: ... Downloads\Meshy_AI_X.zip 02-ceifadora
# Metallic/roughness do zip ficam de fora: o Lit do mago nao usa e em Resources iriam para o APK.
param([Parameter(Mandatory)][string]$Zip, [Parameter(Mandatory)][string]$Slug)
$ErrorActionPreference = "Stop"
$Dest = Join-Path $PSScriptRoot "Assets\_Arkana\Resources\magos"
$Tmp = Join-Path $env:TEMP ("arkana-mago-" + $Slug)
if (Test-Path $Tmp) { Remove-Item $Tmp -Recurse -Force }
Expand-Archive -Path $Zip -DestinationPath $Tmp
$Fbx = Get-ChildItem $Tmp -Recurse -Filter *.fbx | Select-Object -First 1
$Cor = Get-ChildItem $Tmp -Recurse -Filter *texture_0.png | Select-Object -First 1
$Nrm = Get-ChildItem $Tmp -Recurse -Filter *texture_0_normal.png | Select-Object -First 1
if (-not $Fbx -or -not $Cor) { throw "zip sem FBX ou sem texture_0.png: $Zip" }
Copy-Item $Fbx.FullName (Join-Path $Dest "$Slug.fbx") -Force
Copy-Item $Cor.FullName (Join-Path $Dest "$Slug-cor.png") -Force
if ($Nrm) { Copy-Item $Nrm.FullName (Join-Path $Dest "$Slug-normal.png") -Force }
# takes do FBX (nomes dos clipes), para conferir com os aliases do PoseMago
$Txt = [Text.Encoding]::ASCII.GetString([IO.File]::ReadAllBytes($Fbx.FullName))
$Takes = [regex]::Matches($Txt, '[\x20-\x7e]{2,60}(?=\x00\x01AnimStack)') | ForEach-Object Value
Remove-Item $Tmp -Recurse -Force
"$Slug : $([math]::Round($Fbx.Length / 1MB, 1)) MB, clipes: $($Takes -join ', ')"
