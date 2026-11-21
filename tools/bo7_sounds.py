"""Sonidos de las armas de BO7 (MK35 = ar_finch, VMP = sm_civet) para el bundle bo7_sounds.bundle.
Uso: python bo7_sounds.py <saluki exported_files/bo7> <UnityWeaponsSdk/Assets/TarkovaCola/Sounds>

Los bancos de sonido de Saluki (sndbanks/csv/weapon_sat_<arma>_plr[_sup].all.csv) dan, por alias, sus variantes (una fila por
archivo) y el alias "secundario" que suena a la vez (disparo -> mecanica -> graves -> casquillo). Se copian los .wav de:
  - los alias de las notas "AudioOneShot@..." de las animaciones de BO7 convertidas (Anim/sat_vm_<arma>_*.json)
  - el disparo (normal, ultima bala, con silenciador) y su cadena de secundarios
y se escribe bo7_sounds.json: { alias: { "files": [nombres sin .wav], "next": alias secundario o null } }.
Los sonidos son de BO7: van solo compilados en el bundle (la carpeta Sounds esta en .gitignore).
"""
import csv, glob, json, os, shutil, sys

GUNS = {'ar_finch': ('wpn_ar_finch_fire_plr_shot', 'wpn_ar_finch_fire_plr_last_shot', 'wpn_ar_finch_sup_plr_shot', 'wpn_ar_finch_sup_plr_last_shot'),
        'ar_heron': ('wpn_ar_heron_fire_plr_shot', 'wpn_ar_heron_fire_plr_last_shot', 'wpn_ar_heron_sup_plr_shot', 'wpn_ar_heron_sup_plr_last_shot'),
        'ar_eagle': ('wpn_ar_eagle_fire_plr_shot', 'wpn_ar_eagle_fire_plr_last_shot', 'wpn_ar_eagle_sup_plr_shot', 'wpn_ar_eagle_sup_plr_last_shot'),
        'ar_condor': ('wpn_ar_condor_fire_plr_shot', 'wpn_ar_condor_fire_plr_last_shot', 'wpn_ar_condor_sup_plr_shot', 'wpn_ar_condor_sup_plr_last_shot'),
        'sm_bear': ('wpn_sm_bear_fire_plr', 'wpn_sm_bear_fire_plr_last', 'wpn_sm_bear_sup_plr_shot', 'wpn_sm_bear_sup_plr_last_shot'),
        'sh_corso': ('wpn_sh_corso_fire_plr', 'wpn_sh_corso_fire_plr_last', 'wpn_sh_corso_sup_plr', 'wpn_sh_corso_sup_plr_last'),
        'sn_aden': ('wpn_sn_aden_fire_plr_shot', 'wpn_sn_aden_fire_plr_last_shot', 'wpn_sn_aden_fire_sup_plr_shot', 'wpn_sn_aden_fire_sup_plr_last_shot'),
        'sm_civet': ('wpn_sm_civet_fire_plr', 'wpn_sm_civet_fire_plr_last', 'wpn_sm_civet_sup_plr_shot', 'wpn_sm_civet_sup_plr_last_shot')}


def main(src, out):
    anim_dir = os.path.join(os.path.dirname(os.path.abspath(out)), 'Anim')
    os.makedirs(out, exist_ok=True)
    manifest, missing = {}, set()
    for gun, fire in GUNS.items():
        rows = {}
        for f in glob.glob(os.path.join(src, 'sndbanks', 'csv', 'weapon_sat_%s_plr*.all.csv' % gun)):
            for r in csv.DictReader(open(f, encoding='utf-8')):
                e = rows.setdefault(r['name'], {'files': [], 'next': r['secondary'] or None})
                e['files'].append(r['file'])
        want = list(fire)
        for j in glob.glob(os.path.join(anim_dir, 'sat_vm_%s_*.json' % gun)):
            want += [n['name'].split('@', 1)[1] for n in json.load(open(j))['notes'] if n['name'].startswith('AudioOneShot@')]
        seen = set()
        while want:
            a = want.pop()
            if a in seen or a not in rows: continue
            seen.add(a)
            files = []
            for rel in rows[a]['files']:
                wav = os.path.join(src, 'sounds', rel.replace('/', os.sep) + '.wav')
                if not os.path.exists(wav): missing.add(rel); continue
                name = os.path.basename(rel).split('.')[0]
                shutil.copyfile(wav, os.path.join(out, name + '.wav'))
                files.append(name)
            nxt = rows[a]['next']
            manifest[a] = {'files': sorted(set(files)), 'next': nxt}
            if nxt: want.append(nxt)
    json.dump(manifest, open(os.path.join(out, 'bo7_sounds.json'), 'w'), indent=1, sort_keys=True)
    print('%d alias, %d .wav, %d archivos que faltan en la exportacion' % (len(manifest), len(os.listdir(out)) - 1, len(missing)))
    for a, e in sorted(manifest.items()):
        if not e['files']: print('  sin sonido:', a)


if __name__ == '__main__':
    main(sys.argv[1], sys.argv[2])
