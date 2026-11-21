#!/bin/bash
# bo7_gun_import.sh <cod, p.ej. ar_heron> <piezas: rec bar mag pgrip stock ...>   (primera = receptor, y debe haber "mag")
# Importa modelos, convierte las animaciones sat_vm_<cod>_* y arma el esqueleto (receptor + cargador + resto de piezas)
cod=$1; shift
PRE=${BO7_PREFIX:-sat_vm_}   # prefijo de los modelos (att_sat_vm_ en armas cuya base solo esta como accesorio)
S=${SALUKI_BO7:-$HOME/Downloads/saluki/exported_files/bo7}; A=UnityWeaponsSdk/Assets/TarkovaCola/Anim; M=UnityWeaponsSdk/Assets/TarkovaCola/Models/bo7
models=(); extra=()
for p in "$@"; do models+=("${PRE}${cod}_$p"); [ "$p" != "${1}" ] && [ "$p" != "mag" ] && extra+=("$S/models/${PRE}${cod}_$p/${PRE}${cod}_${p}_LOD0.cast"); done
python tools/bo6_import.py $S/models $M "${models[@]}" | tr '\n' ' '; echo
n=0; for f in $S/animations/sat_vm_${cod}_*.cast; do b=$(basename $f .cast); [ -f "$A/$b.json" ] || python tools/cast2json.py "$f" "$A/$b.json" >/dev/null 2>&1; n=$((n+1)); done; echo "$n animaciones"
magp=$(for p in "$@"; do case $p in mag*) echo $p; break;; esac; done); [ -z "$magp" ] && magp=$2   # sin cargador (escopeta): la segunda pieza
python tools/bo7_skel.py UnityBundle/Assets/Anim/viewhands_skel.json $S/models/${PRE}${cod}_$1/${PRE}${cod}_${1}_LOD0.cast $S/models/${PRE}${cod}_$magp/${PRE}${cod}_${magp}_LOD0.cast $A/bo7_${cod}_skel.json "${extra[@]}"
python - <<PY
import sys; sys.path.insert(0,'tools')
from cast2json import read_nodes, walk
for part in "$*".split():
    bs=[n['props']['n'] for n in walk(read_nodes('${S}/models/${PRE}${cod}_%s/${PRE}${cod}_%s_LOD0.cast'%(part,part))) if n['id']=='bone']
    print(part, ' '.join(b for b in bs if not b.startswith('tag_') or 'ik_loc' in b or 'sight' in b)[:300])
PY
