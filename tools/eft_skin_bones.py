"""Lee de un bundle de manos/piel de EFT los nombres de los huesos de cada SkinnedMeshRenderer.
EFT guarda esos nombres en su propio script (Skin), que Unity no puede leer sin el codigo del juego; UnityPy si lo lee con la typetree.
Uso: python eft_skin_bones.py <bundle> [salida.json]  ->  {"bones": [nombre del hueso por indice], "root": "..."}
"""
import json, sys
import UnityPy


def main(path, out=None):
    env = UnityPy.load(path)
    res = {}
    for obj in env.objects:
        if obj.type.name != 'MonoBehaviour':
            continue
        try:
            tree = obj.read_typetree()
        except Exception as e:
            print('sin typetree:', e); continue
        if '_bonePaths' in tree:
            res['bones'] = [p.split('/')[-1] for p in tree['_bonePaths']]
            res['root'] = tree.get('_rootBonePath', '').split('/')[-1]
            print('huesos:', len(res['bones']), 'raiz:', res['root'])
    if out:
        json.dump(res, open(out, 'w'), indent=1)


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None)
