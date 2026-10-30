# Renderiza un clip de primera persona de EFT (esqueleto, manos y arma reales) a una hoja PNG.
#   .\tools\lab.ps1 -Weapon m48_kukri/weapon_united_cutlery_m48_kukri_container -Clip kukri_fire1 -Frames 0-end/2 -Out lab.png
#   -List muestra los clips del arma y los huesos que mueve el clip.  -View fps|side|both
param(
    [string]$Weapon = 'm48_kukri/weapon_united_cutlery_m48_kukri_container',
    [string]$Clip = 'kukri_fire1',
    [string]$Frames = '0-end/2',
    [string]$View = 'fps',
    [int]$Cols = 4,
    [string]$Out = "$PSScriptRoot\..\lab.png",
    [switch]$List
)
$unity = 'C:\Program Files\Unity\Hub\Editor\2022.3.43f1\Editor\Unity.exe'
$env:LAB_WEAPON = "assets/content/weapons/$Weapon.bundle"
$env:LAB_CLIP = $Clip; $env:LAB_FRAMES = $Frames; $env:LAB_VIEW = $View; $env:LAB_COLS = "$Cols"
$env:LAB_LIST = if ($List) { '1' } else { '0' }
$env:LAB_OUT = [IO.Path]::GetFullPath($Out)
$log = Join-Path $env:TEMP 'tarkovacola_lab.log'
& $unity -batchmode -quit -projectPath "$PSScriptRoot\..\UnityBundle" -executeMethod TarkovaCola.EftLab.Run -logFile $log | Out-Null
Select-String -Path $log -Pattern '\[LAB\]|\[EFT\]|error CS' | ForEach-Object { $_.Line }
