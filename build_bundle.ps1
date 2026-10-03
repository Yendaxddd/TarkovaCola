<#
.SYNOPSIS
  Reconstruye el asset bundle de Tarkova-Cola con Unity 2022.3.43f1 (la version exacta que usa Tarkov)
  y lo copia a Server/ModFiles para que el proximo `dotnet build Server` lo despliegue.
#>
$ErrorActionPreference = 'Stop'
$root   = Split-Path -Parent $MyInvocation.MyCommand.Path
$unity  = 'C:\Program Files\Unity\Hub\Editor\2022.3.43f1\Editor\Unity.exe'
$proj   = Join-Path $root 'UnityBundle'
$log    = Join-Path $proj 'build.log'
$dest   = Join-Path $root 'Server\ModFiles\bundles\assets\content\items\consumables\tarkovacola'

if (-not (Test-Path $unity)) { throw "Falta Unity 2022.3.43f1: $unity" }
Remove-Item (Join-Path $proj 'Build') -Recurse -Force -ErrorAction SilentlyContinue   # evita reutilizar un bundle viejo si el build falla
$p = Start-Process $unity -ArgumentList '-batchmode','-nographics','-quit','-projectPath',"`"$proj`"",'-executeMethod','TarkovaCola.BuildSpeedCola.Run','-logFile',"`"$log`"" -PassThru -Wait
if ($p.ExitCode -ne 0) { throw "Unity fallo (codigo $($p.ExitCode)); revisa $log" }

if (-not (Test-Path (Join-Path $proj 'Build\speedcola.bundle'))) { throw "Unity no genero el bundle (error de compilacion?); revisa $log" }
New-Item -ItemType Directory -Force $dest | Out-Null
Copy-Item (Join-Path $proj 'Build\speedcola.bundle') $dest -Force
# copia para el modelo en mano (la carga el plugin de cliente)
$handSrc = Join-Path $proj 'Build\hand\speedcola_hand.bundle'
if (-not (Test-Path $handSrc)) { throw "Unity no genero el bundle de mano; revisa $log" }
Copy-Item $handSrc (Join-Path $root 'Client\assets\speedcola_hand.bundle') -Force

# ---- Stamin-Up (botella de perks.fbx) ----
$p2 = Start-Process $unity -ArgumentList '-batchmode','-nographics','-quit','-projectPath',"`"$proj`"",'-executeMethod','TarkovaCola.BuildStaminUp.Run','-logFile',"`"$log`"" -PassThru -Wait
if ($p2.ExitCode -ne 0) { throw "Unity fallo con Stamin-Up (codigo $($p2.ExitCode)); revisa $log" }
foreach ($f in @('Build\staminup\staminup.bundle', 'Build\staminup_hand\staminup_hand.bundle')) { if (-not (Test-Path (Join-Path $proj $f))) { throw "Unity no genero $f; revisa $log" } }
Copy-Item (Join-Path $proj 'Build\staminup\staminup.bundle') $dest -Force
Copy-Item (Join-Path $proj 'Build\staminup_hand\staminup_hand.bundle') (Join-Path $root 'Client\assets\staminup_hand.bundle') -Force

Write-Host "Bundle listo en $dest" -ForegroundColor Green
