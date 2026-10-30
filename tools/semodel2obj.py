"""SEModel (Greyhound) -> OBJ + MTL.  Uso: python semodel2obj.py <archivo.semodel> [salida.obj]

Formato SEModel v1 (DTZxPorter): cabecera, nombres y datos de huesos, mallas (posiciones, UVs, normales, colores,
pesos, caras, indices de material) y materiales. Solo se exporta la geometria estatica (los huesos se ignoran).
"""
import struct, sys, os

def read_cstr(d, o):
    e = d.index(b'\0', o)
    return d[o:e].decode('utf-8', 'replace'), e + 1

def convert(path, out=None):
    d = open(path, 'rb').read()
    assert d[:7] == b'SEModel', 'no es SEModel'
    version, header_size = struct.unpack_from('<HH', d, 7)
    data_flags, bone_flags, mesh_flags = d[11], d[12], d[13]
    bone_count, mesh_count, mat_count = struct.unpack_from('<III', d, 14)
    o = 29                                          # firma(7) + 22 bytes de cabecera
    has_uv, has_norm, has_col, has_w = mesh_flags & 1, mesh_flags & 2, mesh_flags & 4, mesh_flags & 8

    for _ in range(bone_count):
        _, o = read_cstr(d, o)
    for _ in range(bone_count):
        o += 1 + 4                                  # flags del hueso (sin uso) + padre
        if bone_flags & 1: o += 28                  # global: pos + rot (segun la cabecera, no el hueso)
        if bone_flags & 2: o += 28                  # local: pos + rot
        if bone_flags & 4: o += 12                  # escala

    bidx = 1 if bone_count <= 0xFF else (2 if bone_count <= 0xFFFF else 4)
    meshes = []
    for _ in range(mesh_count):
        o += 1                                      # flags de la malla
        layers, influence = d[o], d[o + 1]; o += 2
        vcount, fcount = struct.unpack_from('<II', d, o); o += 8
        pos = struct.unpack_from('<%df' % (vcount * 3), d, o); o += vcount * 12
        uvs = None
        if has_uv:
            uvs = struct.unpack_from('<%df' % (vcount * layers * 2), d, o); o += vcount * layers * 8
        nrm = None
        if has_norm:
            nrm = struct.unpack_from('<%df' % (vcount * 3), d, o); o += vcount * 12
        if has_col: o += vcount * 4
        if has_w: o += vcount * influence * (bidx + 4)
        fidx = 1 if vcount <= 0xFF else (2 if vcount <= 0xFFFF else 4)
        fmt = {1: 'B', 2: 'H', 4: 'I'}[fidx]
        faces = struct.unpack_from('<%d%s' % (fcount * 3, fmt), d, o); o += fcount * 3 * fidx
        mats = struct.unpack_from('<%di' % layers, d, o); o += layers * 4
        meshes.append((vcount, pos, uvs, layers, nrm, faces, mats))

    materials = []
    for _ in range(mat_count):
        name, o = read_cstr(d, o)
        simple = d[o]; o += 1
        tex = {}
        if simple:
            for k in ('diffuse', 'normal', 'specular'):
                tex[k], o = read_cstr(d, o)
        materials.append((name, tex))

    out = out or os.path.splitext(path)[0] + '.obj'
    mtl = os.path.splitext(out)[0] + '.mtl'
    base = 0
    with open(out, 'w') as f:
        f.write('mtllib %s\n' % os.path.basename(mtl))
        for i, (vc, pos, uvs, layers, nrm, faces, mats) in enumerate(meshes):
            f.write('o mesh%d\n' % i)
            # espejo en X (CoD y Unity tienen la mano contraria: sin esto los letreros salen al reves); el orden de los
            # triangulos se invierte abajo para que las caras sigan mirando hacia fuera
            for v in range(vc): f.write('v %f %f %f\n' % (-pos[v*3], pos[v*3+1], pos[v*3+2]))
            for v in range(vc):
                u, w = (uvs[v*layers*2], uvs[v*layers*2+1]) if uvs else (0, 0)
                f.write('vt %f %f\n' % (u, 1 - w))
            for v in range(vc):
                n = (-nrm[v*3], nrm[v*3+1], nrm[v*3+2]) if nrm else (0, 1, 0)
                f.write('vn %f %f %f\n' % n)
            if mats and 0 <= mats[0] < len(materials): f.write('usemtl %s\n' % materials[mats[0]][0])
            for t in range(len(faces) // 3):
                a, b, c = faces[t*3] + 1 + base, faces[t*3+1] + 1 + base, faces[t*3+2] + 1 + base
                f.write('f %d/%d/%d %d/%d/%d %d/%d/%d\n' % (a, a, a, c, c, c, b, b, b))
            base += vc
    with open(mtl, 'w') as f:
        for name, tex in materials:
            f.write('newmtl %s\n' % name)
            if tex.get('diffuse'): f.write('map_Kd %s\n' % tex['diffuse'])
    print('%s: %d mallas, %d vertices, %d materiales -> %s' % (os.path.basename(path), len(meshes), sum(m[0] for m in meshes), len(materials), out))
    return meshes, materials

if __name__ == '__main__':
    convert(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None)
