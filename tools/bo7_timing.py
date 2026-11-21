# bo7_timing.py <cod> <clip> [huesos...] [--step N]
# Desplazamiento (cm) de cada hueso respecto a su reposo (clip idle) fotograma a fotograma: sirve para medir cuando sale/entra el
# cargador (j_mag1 viejo, j_mag2 nuevo), cuando se mueve el cerrojo (j_bolt), la bomba (j_pump), etc. y fijar los tiempos de Jobs2.
import json, sys, math

A = 'UnityWeaponsSdk/Assets/TarkovaCola/Anim/'
args = [a for a in sys.argv[1:] if not a.startswith('--')]
step = 2
if '--step' in sys.argv: step = int(sys.argv[sys.argv.index('--step') + 1]); args = [a for a in args if a != str(step)]
cod, clip = args[0], args[1]
bones = args[2:] or ['j_mag1', 'j_mag2', 'j_bolt']

def load(name):
    return json.load(open(A + 'sat_vm_%s_%s.json' % (cod, name), encoding='utf-8'))

def track(d, bone, prop='rt'):
    for c in d['curves']:
        if c['bone'] == bone and c['prop'] == prop: return c
    return None

def at(c, f):
    fr, vs = c['frames'], [v if isinstance(v, list) else [v] for v in c['values']]
    if f <= fr[0]: return vs[0]
    for i in range(1, len(fr)):
        if f <= fr[i]:
            t = (f - fr[i - 1]) / max(1e-9, fr[i] - fr[i - 1])
            return [a + (b - a) * t for a, b in zip(vs[i - 1], vs[i])]
    return vs[-1]

idle, d = load('idle'), load(clip)
print('%s %s: %d fotogramas' % (cod, clip, d['frames']))
for n in d['notes']:
    if not n['name'].startswith(('AudioOneShot', 'fingers', 'ik_', 'viewmodel')): print('  nota %s @ %s' % (n['name'], n['frames']))
def pos(dd, b, f):
    v = []
    for ax in ('tx', 'ty', 'tz'):
        c = track(dd, b, ax); v.append(at(c, f)[0] if c else 0.0)
    return v

def ang(q1, q2):
    dot = abs(sum(a * b for a, b in zip(q1, q2))); return math.degrees(2 * math.acos(min(1.0, dot)))

for b in bones:
    ct = any(track(d, b, ax) for ax in ('tx', 'ty', 'tz')); cq = track(d, b, 'rq')
    if not ct and not cq: print('  %s: sin pista' % b); continue
    rp = pos(idle, b, 0); rq = track(idle, b, 'rq'); rq0 = at(rq, 0) if rq else (at(cq, 0) if cq else None)
    row = []
    for f in range(0, d['frames'], step):
        cell = '%d:' % f
        if ct: cell += '%.1f' % (100 * math.dist(pos(d, b, f), rp))
        if cq: cell += '/%.0f' % ang(at(cq, f), rq0)
        row.append(cell)
    print('  %-14s %s' % (b, ' '.join(row)))
