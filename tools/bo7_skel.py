"""Esqueleto completo de un arma de BO7 en primera persona: brazos (viewhands_skel.json) + receptor + cargador.
El receptor cuelga de j_gun (que ya esta en los brazos, hijo de la muneca derecha) y el cargador de su tag_mag_attach.
Asi la cinematica directa de una animacion de BO7 da tambien donde estan el arma y el cargador en cada fotograma.
Uso: python bo7_skel.py <viewhands_skel.json> <rec.cast> <mag.cast> <salida.json> [<bar.cast> ...]
"""
import json, os, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from cast2json import read_nodes, walk


def bones(path):
    bs = [n['props'] for n in walk(read_nodes(path)) if n['id'] == 'bone']
    names = [b['n'] for b in bs]
    first = lambda v: v[0] if v and isinstance(v[0], list) else v
    res = {}
    for b in bs:
        p = b.get('p', -1)
        res[b['n']] = {'parent': names[p] if isinstance(p, int) and 0 <= p < len(names) else None,
                       'lp': first(b.get('lp', [0, 0, 0])), 'lr': first(b.get('lr', [0, 0, 0, 1]))}
    return res


def main(skel_path, rec_path, mag_path, out, *extras):
    skel = json.load(open(skel_path))
    for name, b in bones(rec_path).items():
        if name == 'j_gun': continue
        skel[name] = b
    for name, b in bones(mag_path).items():
        if name == 'tag_mag_attach': continue              # el del receptor
        if name in skel and not name.startswith('j_mag'): name = 'mag_' + name
        skel[name] = b
    for extra in extras:                                   # canon, culata...: cuelgan de su tag_*_attach del receptor
        for name, b in bones(extra).items():
            if name in skel and skel[name].get('parent') is not None: continue   # los tag_* sueltos de las manos (p. ej. tag_guard_attach, bomba) se toman de la pieza
            skel[name] = b
    json.dump(skel, open(out, 'w'), indent=1)
    print('%d huesos -> %s' % (len(skel), out))


if __name__ == '__main__':
    main(*sys.argv[1:])
