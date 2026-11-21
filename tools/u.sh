#!/bin/bash
# u.sh <metodo> [VAR=valor ...]  -> ejecuta Unity en batch sobre UnityWeaponsSdk y muestra las lineas utiles del log
M=$1; shift
for kv in "$@"; do export "$kv"; done
# La licencia la sirve el Unity.Licensing.Client del Hub (version de la Microsoft Store); no siempre esta vivo aunque el Hub este en la
# bandeja, y sin el Unity en batch dice "No valid Unity Editor license found". Si no esta, se abre el Hub y se espera a su servicio.
if ! tasklist | grep -qi "Unity.Licensing.Client"; then
  # el Hub solo arranca su servicio al iniciarse (y es app de la Store: el servicio no se puede lanzar suelto): se reinicia el Hub
  echo "[u.sh] reiniciando Unity Hub para la licencia..."
  taskkill //IM "Unity Hub.exe" //F >/dev/null 2>&1; sleep 2
  explorer.exe 'shell:AppsFolder\UnityTechnologies.UnityHub_2vrhnee42bhxm!UnityHub'
  for i in $(seq 1 60); do tasklist | grep -qi "Unity.Licensing.Client" && break; sleep 2; done
  sleep 5
fi
U="/c/Program Files/Unity/Hub/Editor/2022.3.43f1/Editor/Unity.exe"
P="D:\Escape from Tarkov\SPT v4.1.6\TarkovaCola-src\UnityWeaponsSdk"
L="$TEMP/u_sdk.log"
"$U" -batchmode -quit -projectPath "$P" -executeMethod "TarkovaCola.$M" -logFile "$L"
grep -E "^\[(BO7|BO7A|LAB|PROBE|AIM|HANDS|CMP|MEDIDA|DRINK|MELEE)\]|error CS|No valid Unity|Exception" "$L" | cut -c1-400 | awk '!seen[$0]++' | head -${LINES_MAX:-60}
