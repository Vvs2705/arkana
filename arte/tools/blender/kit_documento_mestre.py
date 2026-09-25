"""Kit geometrico do Documento Mestre, feito por script (rota "Blender" do catalogo).

Uso (headless, reproduzivel):
    blender -b --factory-startup --python arte/tools/blender/kit_documento_mestre.py -- 027 <saida.glb> [previa.png]

Cada peca: geometria real (tabuas, vigas, ferragens), materiais procedurais
ASSADOS numa textura 1024 (cor x oclusao) — o Unity nao le o procedural do
Blender, entao o que vai no GLB e' a imagem. Pivo no chao, 1 unidade = 1 metro.
Referencia visual: design/cenario/CONCEPTS-DOCUMENTO-MESTRE.md.
"""
import math
import random
import sys

import bpy
import numpy as np

SEED = 24092026


def caixa(dims, loc, rot=(0, 0, 0), var=None, mat=None):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.scale = dims
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    me = o.data
    a = me.attributes.new("var", "FLOAT", "POINT")
    v = random.random() if var is None else var
    a.data.foreach_set("value", [v] * len(me.vertices))
    if mat:
        me.materials.append(mat)
    return o


def cilindro(r, h, loc, rot, mat):
    bpy.ops.mesh.primitive_cylinder_add(vertices=8, radius=r, depth=h, location=loc, rotation=rot)
    o = bpy.context.active_object
    me = o.data
    a = me.attributes.new("var", "FLOAT", "POINT")
    a.data.foreach_set("value", [0.5] * len(me.vertices))
    me.materials.append(mat)
    return o


def no(nt, tipo, **props):
    n = nt.nodes.new(tipo)
    for k, v in props.items():
        setattr(n, k, v)
    return n


def mat_madeira(nome, eixo):
    """Veio alongado no eixo da tabua; 'var' (constante por tabua) muda o tom."""
    m = bpy.data.materials.new(nome)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    coord = no(nt, "ShaderNodeTexCoord")
    mapa = no(nt, "ShaderNodeMapping")
    mapa.inputs["Scale"].default_value = {"X": (1.5, 22, 22), "Y": (22, 1.5, 22), "Z": (22, 22, 1.5)}[eixo]
    ruido = no(nt, "ShaderNodeTexNoise")
    ruido.inputs["Scale"].default_value = 4.0
    ruido.inputs["Detail"].default_value = 12.0
    ruido.inputs["Distortion"].default_value = 0.6
    rampa = no(nt, "ShaderNodeValToRGB")  # cores em espaco LINEAR: marrom envelhecido, nao bege
    rampa.color_ramp.elements[0].position = 0.30
    rampa.color_ramp.elements[0].color = (0.055, 0.030, 0.013, 1)
    rampa.color_ramp.elements[1].position = 0.72
    rampa.color_ramp.elements[1].color = (0.30, 0.17, 0.075, 1)
    attr = no(nt, "ShaderNodeAttribute", attribute_name="var")
    mult = no(nt, "ShaderNodeMath", operation="MULTIPLY_ADD")
    mult.inputs[1].default_value = 0.25
    mult.inputs[2].default_value = -0.1
    soma = no(nt, "ShaderNodeMath", operation="ADD")
    sujeira = no(nt, "ShaderNodeTexNoise")  # manchas grandes de sujeira/umidade
    sujeira.inputs["Scale"].default_value = 2.5
    sujeira.inputs["Detail"].default_value = 4.0
    tom = no(nt, "ShaderNodeValToRGB")  # sujeira em CINZA (0,65..1): so' escurece, nao tinge
    tom.color_ramp.elements[0].color = (0.65, 0.65, 0.65, 1)
    tom.color_ramp.elements[1].color = (1, 1, 1, 1)
    misto = no(nt, "ShaderNodeMix", data_type="RGBA", blend_type="MULTIPLY")
    misto.inputs["Factor"].default_value = 1.0
    relevo = no(nt, "ShaderNodeBump")  # veio em relevo: vira mapa normal no assado
    relevo.inputs["Strength"].default_value = 0.45
    nt.links.new(coord.outputs["Object"], mapa.inputs["Vector"])
    nt.links.new(mapa.outputs["Vector"], ruido.inputs["Vector"])
    nt.links.new(attr.outputs["Fac"], mult.inputs[0])
    nt.links.new(ruido.outputs["Fac"], soma.inputs[0])
    nt.links.new(mult.outputs["Value"], soma.inputs[1])
    nt.links.new(soma.outputs["Value"], rampa.inputs["Fac"])
    nt.links.new(coord.outputs["Object"], sujeira.inputs["Vector"])
    nt.links.new(rampa.outputs["Color"], misto.inputs["A"])
    nt.links.new(sujeira.outputs["Fac"], tom.inputs["Fac"])
    nt.links.new(tom.outputs["Color"], misto.inputs["B"])
    nt.links.new(misto.outputs["Result"], bsdf.inputs["Base Color"])
    nt.links.new(ruido.outputs["Fac"], relevo.inputs["Height"])
    nt.links.new(relevo.outputs["Normal"], bsdf.inputs["Normal"])
    bsdf.inputs["Roughness"].default_value = 0.85
    return m


def mat_ferro(nome):
    m = bpy.data.materials.new(nome)
    m.use_nodes = True
    nt = m.node_tree
    bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
    ruido = no(nt, "ShaderNodeTexNoise")
    ruido.inputs["Scale"].default_value = 18.0
    ruido.inputs["Detail"].default_value = 10.0
    rampa = no(nt, "ShaderNodeValToRGB")
    rampa.color_ramp.elements[0].position = 0.45
    rampa.color_ramp.elements[0].color = (0.05, 0.05, 0.055, 1)
    rampa.color_ramp.elements[1].position = 0.75
    rampa.color_ramp.elements[1].color = (0.32, 0.14, 0.05, 1)
    nt.links.new(ruido.outputs["Fac"], rampa.inputs["Fac"])
    nt.links.new(rampa.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Metallic"].default_value = 0.8
    bsdf.inputs["Roughness"].default_value = 0.55
    return m


def peca_027():
    """027 — caixa de suprimentos: mais larga que alta, vigas grossas nas 12 arestas,
    tabuas verticais, travessa diagonal nas 4 faces e rebites de ferro (sem cantoneira)."""
    W, D, H, w, t = 1.15, 0.95, 0.90, 0.115, 0.035
    mZ, mX, mY, fe = mat_madeira("madeira_Z", "Z"), mat_madeira("madeira_X", "X"), mat_madeira("madeira_Y", "Y"), mat_ferro("ferro")
    partes = []
    cx, cy = W / 2 - w / 2, D / 2 - w / 2
    for sx in (-1, 1):  # 4 vigas verticais
        for sy in (-1, 1):
            partes.append(caixa((w, w, H), (sx * cx, sy * cy, H / 2), mat=mZ))
    for z in (w / 2, H - w / 2):  # 8 vigas horizontais
        for s in (-1, 1):
            partes.append(caixa((W - 2 * w, w, w), (0, s * cy, z), mat=mX))
            partes.append(caixa((w, D - 2 * w, w), (s * cx, 0, z), mat=mY))
    vx, vy, vz = W - 2 * w, D - 2 * w, H - 2 * w
    rec = 0.03  # tabuas recuadas em relacao as vigas
    for n, vao, fn in ((6, vx, "x"), (5, vy, "y")):
        lt = vao / n - 0.01
        for i in range(n):
            u = -vao / 2 + (i + 0.5) * vao / n
            if fn == "x":  # frente/fundo
                for s in (-1, 1):
                    partes.append(caixa((lt, t, vz), (u, s * (D / 2 - rec), H / 2), mat=mZ))
            else:  # laterais
                for s in (-1, 1):
                    partes.append(caixa((t, lt, vz), (s * (W / 2 - rec), u, H / 2), mat=mZ))
    lt = vy / 5 - 0.01
    for i in range(5):  # tampa: tabuas no sentido do comprimento
        u = -vy / 2 + (i + 0.5) * vy / 5
        partes.append(caixa((vx, lt, t), (0, u, H - rec), mat=mX))
    partes.append(caixa((vx, vy, t), (0, 0, rec), mat=mX))  # fundo
    bf = 0.012  # travessa rente a face externa das vigas
    for s in (-1, 1):
        ang = math.atan2(vz, vx)
        partes.append(caixa((math.hypot(vx, vz) - 0.06, t, w * 0.85), (0, s * (D / 2 - bf), H / 2), (0, s * ang, 0), mat=mX))
        ang = math.atan2(vz, vy)
        partes.append(caixa((t, math.hypot(vy, vz) - 0.06, w * 0.85), (s * (W / 2 - bf), 0, H / 2), (-s * ang, 0, 0), mat=mY))
    r, e = 0.017, 0.012  # rebites de ferro nas pontas das vigas, nas duas faces visiveis
    for sx in (-1, 1):
        for sy in (-1, 1):
            for z in (w / 2, H - w / 2):
                partes.append(cilindro(r, e, (sx * cx, sy * (D / 2 + e / 4), z), (math.pi / 2, 0, 0), fe))
                partes.append(cilindro(r, e, (sx * (W / 2 + e / 4), sy * cy, z), (0, math.pi / 2, 0), fe))
                for k in (0.28, 0.72):  # ao longo das vigas horizontais
                    partes.append(cilindro(r, e, (sx * W / 2 * k, sy * (D / 2 + e / 4), z), (math.pi / 2, 0, 0), fe))
                    partes.append(cilindro(r, e, (sx * (W / 2 + e / 4), sy * D / 2 * k, z), (0, math.pi / 2, 0), fe))
    return partes, (mZ, mX, mY, fe)


PECAS = {"027": peca_027}


def assar(obj, mats, tam=1024):
    """Cor (DIFFUSE/COLOR) x oclusao (AO) -> uma imagem; troca os procedurais por ela."""
    cena = bpy.context.scene
    cena.render.engine = "CYCLES"
    cena.cycles.samples = 64
    cena.cycles.device = "CPU"
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=0.01)
    bpy.ops.object.mode_set(mode="OBJECT")
    imgs = {}
    for passe in ("COR", "AO", "NORMAL"):
        img = bpy.data.images.new(f"{obj.name}_{passe}", tam, tam, alpha=False)
        if passe == "NORMAL":
            img.colorspace_settings.name = "Non-Color"
        imgs[passe] = img
        for m in mats:
            nt = m.node_tree
            ni = nt.nodes.get("ASSAR") or no(nt, "ShaderNodeTexImage", name="ASSAR")
            ni.image = img
            nt.nodes.active = ni
        if passe == "COR":
            bpy.ops.object.bake(type="DIFFUSE", pass_filter={"COLOR"}, margin=6, use_clear=True)
        elif passe == "AO":
            bpy.ops.object.bake(type="AO", margin=6, use_clear=True)
        else:
            bpy.ops.object.bake(type="NORMAL", normal_space="TANGENT", margin=6, use_clear=True)
    imgs["NORMAL"].pack()
    cor = np.array(imgs["COR"].pixels[:]).reshape(-1, 4)
    ao = np.array(imgs["AO"].pixels[:]).reshape(-1, 4)
    print(f"assado: cor media {cor[:, :3].mean():.3f}, oclusao media {ao[:, :3].mean():.3f}")
    cor[:, :3] *= (0.35 + 0.65 * ao[:, :3])
    final = bpy.data.images.new(f"{obj.name}_albedo", tam, tam, alpha=False)
    final.pixels[:] = cor.ravel().tolist()
    final.pack()
    for m in mats:  # procedural -> imagem assada
        nt = m.node_tree
        bsdf = next(n for n in nt.nodes if n.type == "BSDF_PRINCIPLED")
        # por TIPO: o wrapper python de um no' muda a cada acesso ("is not" apagava o BSDF)
        for n in [n for n in nt.nodes if n.type not in ("BSDF_PRINCIPLED", "OUTPUT_MATERIAL")]:
            nt.nodes.remove(n)
        ni = no(nt, "ShaderNodeTexImage")
        ni.image = final
        nt.links.new(ni.outputs["Color"], bsdf.inputs["Base Color"])
        nn = no(nt, "ShaderNodeTexImage")
        nn.image = imgs["NORMAL"]
        nm = no(nt, "ShaderNodeNormalMap")
        nt.links.new(nn.outputs["Color"], nm.inputs["Color"])
        nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    return final


def previa(obj, caminho):
    cena = bpy.context.scene
    cena.render.resolution_x = cena.render.resolution_y = 900
    cena.cycles.samples = 48
    mundo = bpy.data.worlds.new("mundo")
    mundo.use_nodes = True
    fundo = next(n for n in mundo.node_tree.nodes if n.type == "BACKGROUND")
    fundo.inputs["Color"].default_value = (0.08, 0.08, 0.09, 1)
    cena.world = mundo
    bpy.ops.object.light_add(type="SUN", rotation=(math.radians(50), math.radians(10), math.radians(-35)))
    bpy.context.active_object.data.energy = 4.0
    bpy.ops.object.camera_add(location=(2.3, -2.6, 1.9))
    cam = bpy.context.active_object
    alvo = bpy.data.objects.new("alvo", None)
    alvo.location = (0, 0, obj.dimensions.z * 0.45)
    bpy.context.collection.objects.link(alvo)
    cam.constraints.new("TRACK_TO").target = alvo
    cena.camera = cam
    cena.render.filepath = caminho
    bpy.ops.render.render(write_still=True)


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    peca, saida = args[0], args[1]
    prev = args[2] if len(args) > 2 else None
    random.seed(SEED + int(peca))
    bpy.ops.wm.read_factory_settings(use_empty=True)
    partes, mats = PECAS[peca]()
    bpy.ops.object.select_all(action="DESELECT")
    for o in partes:
        bev = o.modifiers.new("chanfro", "BEVEL")
        bev.width, bev.segments, bev.limit_method = 0.004, 1, "ANGLE"
        bpy.context.view_layer.objects.active = o
        bpy.ops.object.modifier_apply(modifier="chanfro")
    for o in partes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = partes[0]
    bpy.ops.object.join()
    obj = bpy.context.active_object
    # o join herda a origem da 1a parte (centro de uma viga, a meia altura): a peca
    # entrava meio enterrada no Unity. Origem = (0,0,0) do mundo = centro da base.
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    zs = [v.co.z for v in obj.data.vertices]
    xs = [v.co.x for v in obj.data.vertices]
    ys = [v.co.y for v in obj.data.vertices]
    assert abs(min(zs)) < 0.02 and abs(max(xs) + min(xs)) < 0.02 and abs(max(ys) + min(ys)) < 0.02, \
        f"pivo fora do chao/centro: z_min={min(zs):.3f} x=({min(xs):.3f},{max(xs):.3f}) y=({min(ys):.3f},{max(ys):.3f})"
    obj.name = f"mestre_{peca}"
    obj["arkana_id"], obj["spec_version"], obj["generation_seed"] = peca, "1.0", SEED
    assar(obj, mats)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.ops.export_scene.gltf(filepath=saida, export_format="GLB", use_selection=True, export_apply=True)
    tris = sum(len(p.vertices) - 2 for p in obj.data.polygons)
    print(f"OK {obj.name}: {tris} tris, {obj.dimensions.x:.2f} x {obj.dimensions.y:.2f} x {obj.dimensions.z:.2f} m -> {saida}")
    if prev:
        previa(obj, prev)


main()
