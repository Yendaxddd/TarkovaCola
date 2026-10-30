# Banco de pruebas de la interfaz: copia Client/UiKit.cs y Client/UiFx.cs al proyecto UnityWeaponsSdk (reloj de Unity -> reloj simulado,
# internal -> public), y renderiza la pantalla de ejemplo con sus animaciones en Preview/uilab_*.png
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$proj = Join-Path $root "UnityWeaponsSdk"
$dst = Join-Path $proj "Assets\TarkovaCola\UiLab\Src"
New-Item -ItemType Directory -Force $dst | Out-Null
foreach ($f in "UiKit.cs", "UiFx.cs") {
    $c = [IO.File]::ReadAllText((Join-Path $root "Client\$f"))
    $c = $c.Replace("Time.unscaledTime", "LabTime.Now").Replace("Time.unscaledDeltaTime", "LabTime.Dt").Replace("internal ", "public ")
    [IO.File]::WriteAllText((Join-Path $dst $f), $c, (New-Object Text.UTF8Encoding($false)))
}
$unity = (Get-ChildItem "C:\Program Files\Unity\Hub\Editor" -Directory | Where-Object Name -like '2022.3.43*' | Select-Object -First 1).FullName + "\Editor\Unity.exe"
if (-not (Test-Path (Join-Path $proj "Assets\TextMesh Pro"))) {
    & $unity -batchmode -quit -projectPath $proj -executeMethod TarkovaCola.UiLabRun.Essentials -logFile (Join-Path $proj "uilab_tmp.log") | Out-Null
}
& $unity -batchmode -quit -projectPath $proj -executeMethod TarkovaCola.UiLabRun.Run -logFile (Join-Path $proj "uilab.log") | Out-Null
Select-String (Join-Path $proj "uilab.log") -Pattern "\[UILAB\]|error CS|Exception" | Select-Object -First 15 | ForEach-Object { $_.Line }
