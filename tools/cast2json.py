"""Convierte una animacion Cast (.cast, de Saluki/Greyhound) a JSON para el visor de Unity.
Uso: python cast2json.py <anim.cast> [salida.json]

Formato Cast: cabecera (cast, version, raices, flags) y nodos (id, longitud, hash, nprops, nhijos) con propiedades
(tipo de 2 letras, nombre, cantidad, datos). Una animacion es un nodo 'anim' con hijos 'curv' (una curva por hueso y canal):
  nn = nombre del hueso, kp = canal (rq rotacion cuaternion, tx/ty/tz posicion...), m = modo (absolute | additive | relative),
  kb = indices de los fotogramas con clave, kv = valores (cuaternion xyzw o numero).
Los fotogramas entre claves se interpolan al reproducir.
"""
import json, os, struct, sys

SIZES = {'b': 1, 'h': 2, 'i': 4, 'l': 8, 'f': 4, 'd': 8}
FMT = {'b': 'B', 'h': 'H', 'i': 'I', 'l': 'Q', 'f': 'f', 'd': 'd'}


def read_nodes(path):
    d = open(path, 'rb').read()
    magic, version, roots, flags = struct.unpack_from('<4sIII', d, 0)
    assert magic == b'cast', 'no es un .cast'
    tree = []

    def node(o, parent):
        ident, length, h, pc, cc = struct.unpack_from('<IIQII', d, o)
        n = {'id': struct.pack('<I', ident).decode('latin1'), 'props': {}, 'kids': []}
        p = o + 24
        for _ in range(pc):
            t, nl, ac = struct.unpack_from('<HHI', d, p); p += 8
            name = d[p:p + nl].decode('latin1'); p += nl
            tc = struct.pack('<H', t).decode('latin1').rstrip('\0')
            if tc == 's':
                e = d.index(b'\0', p); val = d[p:e].decode('latin1'); p = e + 1
            elif tc in ('2v', '3v', '4v'):
                k = int(tc[0]); val = list(struct.unpack_from('<%df' % (ac * k), d, p)); p += ac * k * 4
                val = [val[i * k:(i + 1) * k] for i in range(ac)]
            elif tc in SIZES:
                val = list(struct.unpack_from('<%d%s' % (ac, FMT[tc]), d, p)); p += ac * SIZES[tc]
                if ac == 1: val = val[0]
            else:
                raise ValueError('tipo de propiedad desconocido: %r' % tc)
            n['props'][name] = val
        parent.append(n)
        for _ in range(cc): p = node(p, n['kids'])
        return o + length

    o = 16
    for _ in range(roots): o = node(o, tree)
    return tree


def walk(nodes):
    for n in nodes:
        yield n
        for k in walk(n['kids']): yield k


def convert(path, out=None):
    tree = read_nodes(path)
    anim = next(n for n in walk(tree) if n['id'] == 'anim')
    fps = anim['props'].get('fr', 30.0)
    curves, frames = [], 0
    for n in walk(tree):
        if n['id'] != 'curv': continue
        p = n['props']
        kb = p['kb'] if isinstance(p['kb'], list) else [p['kb']]
        kv = p['kv']
        frames = max(frames, max(kb) + 1)
        curves.append({'bone': p['nn'], 'prop': p['kp'], 'mode': p.get('m', 'absolute'), 'frames': kb,
                       'values': kv if isinstance(kv, list) else [kv]})
    # eventos / notificaciones (ntif): nombre y fotograma
    notes = []
    for n in walk(tree):
        if n['id'] == 'ntif':
            kb = n['props'].get('kb')
            notes.append({'name': n['props'].get('n', ''), 'frames': kb if isinstance(kb, list) else [kb]})
    res = {'name': os.path.splitext(os.path.basename(path))[0], 'fps': fps, 'frames': frames,
           'loop': bool(anim['props'].get('lo', 0)), 'curves': curves, 'notes': notes}
    out = out or os.path.splitext(path)[0] + '.json'
    json.dump(res, open(out, 'w'), indent=1)
    props = sorted({c['prop'] for c in curves})
    print('%s: %d fotogramas a %g fps, %d curvas (%s), %d huesos, %d notas -> %s'
          % (res['name'], frames, fps, len(curves), ','.join(props), len({c['bone'] for c in curves}), len(notes), out))
    return res


if __name__ == '__main__':
    convert(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None)
