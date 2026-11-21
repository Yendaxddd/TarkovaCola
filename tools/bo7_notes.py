"""Resumen de animaciones BO7 convertidas (JSON): fotogramas, huesos del arma animados y notas.
Uso: python bo7_notes.py <Anim dir> <prefijo, p.ej. sat_vm_ar_condor_> [filtro regex] [-v]"""
import json, os, re, sys
d, pre = sys.argv[1], sys.argv[2]
flt = sys.argv[3] if len(sys.argv) > 3 and sys.argv[3] != '-v' else '.'
verbose = '-v' in sys.argv
for f in sorted(os.listdir(d)):
    if not (f.startswith(pre) and f.endswith('.json')): continue
    name = f[len(pre):-5]
    if not re.search(flt, name): continue
    j = json.load(open(os.path.join(d, f)))
    bones = sorted({c['bone'] for c in j['curves'] if not re.match(r'j_(wrist|elbow|shoulder|clavicle|thumb|index|mid|ring|pinky|sleeve|webbing|wristtwist|elbowtwist|shouldertwist|bicep|forearm|palm|meta|cuff|.*bulge|.*twist)', c['bone']) and not c['bone'].startswith('tag_ik') and not re.match(r'j_(ankle|ball|chest|head|hip|knee|neck|spine|main|pelvis|teeth|jaw|toe|shoulderraise|.*_le_|.*_ri_)|tag_(origin|sync|reflex|stowed|weapon_(left|right|chest)|shield|eye|accessory)', c['bone']) })
    notes = sorted(((fr, n['name']) for n in j['notes'] for fr in n['frames']))
    print('%-34s %4d f  %s' % (name, j['frames'], ' '.join(b for b in bones)[:160] if verbose else ''))
    if notes: print('      ' + ' | '.join('%d:%s' % (fr, n.replace('AudioOneShot@', '~')) for fr, n in notes)[:900])
