# apaga o verde-limao (liquen neon da Meshy) da textura de um glb: matiz 40-110 graus saturada vira oliva-cinza
# uso: blender --background --python desneon.py -- entrada.glb saida.glb
import bpy, sys
import numpy as np
a = sys.argv[sys.argv.index("--") + 1:]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=a[0])
for img in bpy.data.images:
    if img.size[0] == 0:
        continue
    px = np.array(img.pixels[:], dtype=np.float32).reshape(-1, 4)
    r, g, b = px[:, 0], px[:, 1], px[:, 2]
    mx = np.max(px[:, :3], axis=1); mn = np.min(px[:, :3], axis=1); d = mx - mn + 1e-6
    h = np.where(mx == r, ((g - b) / d) % 6, np.where(mx == g, (b - r) / d + 2, (r - g) / d + 4)) * 60
    s = d / (mx + 1e-6)
    m = (h > 40) & (h < 110) & (s > 0.35)
    cinza = (r + g + b) / 3
    # oliva apagado e mais escuro: liquen seco, nao tinta fluorescente
    alvo = np.stack([cinza * 0.62 + 0.03, cinza * 0.64 + 0.03, cinza * 0.5 + 0.02], axis=1)
    px[m, :3] = alvo[m]
    img.pixels[:] = px.ravel()
    img.pack()
    print("[desneon]", img.name, int(m.sum()), "pixels")
bpy.ops.export_scene.gltf(filepath=a[1], export_format='GLB', export_apply=True)
