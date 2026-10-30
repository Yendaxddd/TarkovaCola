"""Cinematica directa del brazo izquierdo de una animacion de CoD (JSON de cast2json.py), sin el modelo de los brazos.
Las rotaciones de la columna y la clavicula vienen en la animacion; las longitudes de los huesos se suponen (brazo de un adulto)
y cada hueso apunta a su hijo por su eje X (+X en el lado izquierdo, convencion de los rigs de CoD; SIGNO=-1 para probar la otra).
Salida: por fotograma, posicion de codo y muneca respecto al hombro, en ejes de camara (x derecha, y arriba, z adelante; metros),
y la orientacion de la muneca (sus ejes X/Y/Z en camara).
Uso: python cod_fk.py <anim.json> [paso] [signo]   o   import cod_fk; cod_fk.solve(...)
"""
import json, math, sys

UPPER, FORE = 0.29, 0.26          # m


def qmul(a, b):
    ax, ay, az, aw = a; bx, by, bz, bw = b
    return (aw * bx + ax * bw + ay * bz - az * by, aw * by - ax * bz + ay * bw + az * bx,
            aw * bz + ax * by - ay * bx + az * bw, aw * bw - ax * bx - ay * by - az * bz)


def qrot(q, v):
    x, y, z, w = q
    vx, vy, vz = v
    # v' = q v q*
    tx, ty, tz = 2 * (y * vz - z * vy), 2 * (z * vx - x * vz), 2 * (x * vy - y * vx)
    return (vx + w * tx + y * tz - z * ty, vy + w * ty + z * tx - x * tz, vz + w * tz + x * ty - y * tx)


def slerp(a, b, t):
    d = sum(i * j for i, j in zip(a, b))
    if d < 0: b = tuple(-i for i in b); d = -d
    if d > 0.9995:
        r = tuple(i + (j - i) * t for i, j in zip(a, b))
    else:
        th = math.acos(d); s = math.sin(th)
        r = tuple((math.sin((1 - t) * th) * i + math.sin(t * th) * j) / s for i, j in zip(a, b))
    n = math.sqrt(sum(i * i for i in r)); return tuple(i / n for i in r)


def sample(curve, f):
    fr, vs = curve['frames'], curve['values']
    if f <= fr[0]: return tuple(vs[0])
    for i in range(len(fr) - 1):
        if fr[i] <= f < fr[i + 1]:
            return slerp(tuple(vs[i]), tuple(vs[i + 1]), (f - fr[i]) / (fr[i + 1] - fr[i]))
    return tuple(vs[-1])


def cam(v):
    # CoD: X adelante, Y izquierda, Z arriba  ->  camara: x derecha, y arriba, z adelante
    return (-v[1], v[2], v[0])


CHAIN = ['j_spinelower', 'j_spineupper', 'j_spine4', 'j_clavicle_le']


def solve(anim, frame, sign=1.0):
    rq = {c['bone']: c for c in anim['curves'] if c['prop'] == 'rq'}
    q = (0.0, 0.0, 0.0, 1.0)
    for b in CHAIN:
        if b in rq: q = qmul(q, sample(rq[b], frame))
    qs = qmul(q, sample(rq['j_shoulder_le'], frame))
    elbow = qrot(qs, (sign * UPPER, 0, 0))
    qe = qmul(qs, sample(rq['j_elbow_le'], frame))
    wrist = tuple(e + d for e, d in zip(elbow, qrot(qe, (sign * FORE, 0, 0))))
    qw = qmul(qe, sample(rq['j_wrist_le'], frame))
    axes = [cam(qrot(qw, a)) for a in ((1, 0, 0), (0, 1, 0), (0, 0, 1))]
    return cam(elbow), cam(wrist), axes


if __name__ == '__main__':
    anim = json.load(open(sys.argv[1]))
    step = int(sys.argv[2]) if len(sys.argv) > 2 else 10
    sign = float(sys.argv[3]) if len(sys.argv) > 3 else 1.0
    for f in range(0, anim['frames'], step):
        e, w, ax = solve(anim, f, sign)
        print('%3d  codo (%5.2f %5.2f %5.2f)  muneca (%5.2f %5.2f %5.2f)  ejeX (%5.2f %5.2f %5.2f)' % ((f,) + e + w + ax[0]))
