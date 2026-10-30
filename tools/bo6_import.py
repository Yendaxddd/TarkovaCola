"""Copia modelos exportados de BO6 (Saluki: <modelo>/<modelo>_LOD0.fbx + _images + _mat_info) al proyecto de Unity
y escribe mats.json con las texturas de cada material:
    semantica 47 = color, 48 = normal, 49 = emisiva, 4a = brillo (las 59/5a son ruido de detalle y se ignoran)
Uso: python bo6_import.py <carpeta models de Saluki> <destino Assets/Models/bo6> <modelo> [<modelo> ...]
"""
import sys, os, re, json, shutil, glob

SEM = {'47': 'albedo', '48': 'normal', '49': 'emissive', '4a': 'gloss'}

def import_model(src_root, dst_root, name):
    src = os.path.join(src_root, name)
    dst = os.path.join(dst_root, name)
    os.makedirs(dst, exist_ok=True)
    fbx = glob.glob(os.path.join(src, '*_LOD0.fbx'))[0]
    shutil.copy2(fbx, os.path.join(dst, name + '.fbx'))
    mats = {}
    for info in glob.glob(os.path.join(src, '_mat_info', '*.txt')):
        mat = os.path.splitext(os.path.basename(info))[0]
        entry = {}
        for sem, img in re.findall(r'unk_semantic_(\w+),(image_\w+)', open(info, encoding='utf-8', errors='replace').read()):
            kind = SEM.get(sem)
            if not kind or kind in entry: continue
            hits = glob.glob(os.path.join(src, '_images', '*', mat, img + '.png')) + glob.glob(os.path.join(src, '_images', mat, img + '.png'))
            if not hits: continue
            os.makedirs(os.path.join(dst, 'tex'), exist_ok=True)
            out = os.path.join(dst, 'tex', mat + '_' + kind + '.png')
            shutil.copy2(hits[0], out)
            entry[kind] = 'tex/' + os.path.basename(out)
        mats[mat] = entry
    json.dump(mats, open(os.path.join(dst, 'mats.json'), 'w'), indent=1)
    print('%-50s %d materiales' % (name, len(mats)))

if __name__ == '__main__':
    for n in sys.argv[3:]:
        import_model(sys.argv[1], sys.argv[2], n)
