<#
.SYNOPSIS
  Previsualiza el efecto visual de PHD Flopper en Unity (copia Client/PhdVfx.cs al proyecto de Unity y renderiza 5 fotogramas
  en UnityBundle/Preview/phdvfx_*.png). Necesita la GPU (no usa -nographics).
#>
$ErrorActionPreference = 'Stop'
$root  = Split-Path -Parent $MyInvocation.MyCommand.Path
$unity = 'C:\Program Files\Unity\Hub\Editor\2022.3.43f1\Editor\Unity.exe'
$proj  = Join-Path $root 'UnityBundle'
$log   = Join-Path $proj 'preview_phdvfx.log'
if (-not (Test-Path $unity)) { throw "Falta Unity 2022.3.43f1: $unity" }
Copy-Item (Join-Path $root 'Client\PhdVfx.cs') (Join-Path $proj 'Assets\Editor\PhdVfxPreview\PhdVfx.cs') -Force
$p = Start-Process $unity -ArgumentList '-batchmode','-quit','-projectPath',"`"$proj`"",'-executeMethod','TarkovaCola.PhdVfxPreview.Run','-logFile',"`"$log`"" -PassThru -Wait
if ($p.ExitCode -ne 0) { throw "Unity fallo (codigo $($p.ExitCode)); revisa $log" }
Write-Host "Fotogramas en $proj\Preview" -ForegroundColor Green
