extends SceneTree
# Rebuilds only the editable visual scene, not Tank's gameplay or the user's variants.
var body: Node3D
var sand = material(Color("#ad9870"))
var light_sand = material(Color("#c1ac82"))
var dark_sand = material(Color("#807157"))
var rubber = material(Color("#252b2b"))
var steel = material(Color("#48504b"))
var glass = material(Color("#253f49"))
var accent = material(Color("#48e4d5"))

func material(color: Color) -> StandardMaterial3D:
    var value = StandardMaterial3D.new()
    value.albedo_color = color
    value.roughness = 0.88
    return value

func part(label: String, mesh: Mesh, position: Vector3, mat: Material) -> MeshInstance3D:
    var node = MeshInstance3D.new()
    node.name = label
    node.mesh = mesh
    node.position = position
    node.material_override = mat
    body.add_child(node)
    node.owner = body
    return node

func box(label: String, size: Vector3, position: Vector3, mat: Material) -> MeshInstance3D:
    var mesh = BoxMesh.new()
    mesh.size = size
    return part(label, mesh, position, mat)

func cylinder(label: String, radius: float, height: float, position: Vector3, mat: Material, sides: int = 12) -> MeshInstance3D:
    var mesh = CylinderMesh.new()
    mesh.top_radius = radius
    mesh.bottom_radius = radius
    mesh.height = height
    mesh.radial_segments = sides
    return part(label, mesh, position, mat)

func triangle(surface: SurfaceTool, a: Vector3, b: Vector3, c: Vector3, center: Vector3):
    var normal = (b - a).cross(c - a).normalized()
    if normal.dot((a + b + c) / 3.0 - center) < 0:
        normal = -normal
    # Godot uses clockwise front faces.
    var reverse = (b - a).cross(c - a).dot(normal) > 0
    surface.set_normal(normal)
    surface.add_vertex(a)
    surface.set_normal(normal)
    surface.add_vertex(c if reverse else b)
    surface.set_normal(normal)
    surface.add_vertex(b if reverse else c)

func armor(label: String, bottom: Array, top: Array, low: float, high: float, mat: Material):
    var surface = SurfaceTool.new()
    surface.begin(Mesh.PRIMITIVE_TRIANGLES)
    var center = Vector3(0, (low + high) / 2, 0)
    for i in bottom.size():
        var next = (i + 1) % bottom.size()
        var a = Vector3(bottom[i].x, low, bottom[i].y)
        var b = Vector3(bottom[next].x, low, bottom[next].y)
        var c = Vector3(top[next].x, high, top[next].y)
        var d = Vector3(top[i].x, high, top[i].y)
        triangle(surface, a, b, c, center)
        triangle(surface, a, c, d, center)
        triangle(surface, Vector3(0, high, 0), d, c, center)
        triangle(surface, Vector3(0, low, 0), b, a, center)
    return part(label, surface.commit(), Vector3.ZERO, mat)

func _initialize():
    body = Node3D.new()
    body.name = "Body"
    body.scale = Vector3.ONE * 0.86
    box("LowerHull", Vector3(1.92, .42, 3.55), Vector3(0,.43,0), dark_sand)
    armor("SlopedHull",
        [Vector2(-.87,-2.03),Vector2(.87,-2.03),Vector2(1.05,-1.75),Vector2(1.05,1.83),Vector2(.85,2.02),Vector2(-.85,2.02),Vector2(-1.05,1.83),Vector2(-1.05,-1.75)],
        [Vector2(-.85,-1.43),Vector2(.85,-1.43),Vector2(1.0,-1.2),Vector2(1.0,1.8),Vector2(.82,1.92),Vector2(-.82,1.92),Vector2(-1.0,1.8),Vector2(-1.0,-1.2)],
        .55,.94,sand)
    for side in [-1,1]:
        var label = "Left" if side == -1 else "Right"
        box(label + "Track",Vector3(.43,.62,3.78),Vector3(side*1.08,.37,0),rubber)
        for i in 7:
            var z = -1.44 + i*.48
            var wheel = cylinder(label+"RoadWheel"+str(i),.255,.085,Vector3(side*1.32,.38,z),dark_sand,14)
            wheel.rotation_degrees.z = 90
            var hub = cylinder(label+"WheelHub"+str(i),.105,.09,Vector3(side*1.37,.38,z),steel)
            hub.rotation_degrees.z = 90
        for i in 18:
            var z = -1.8 + i*.21
            box(label+"Tread"+str(i),Vector3(.45,.045,.07),Vector3(side*1.08,.065,z),steel)
        for i in 5:
            var skirt = box(label+"Skirt"+str(i),Vector3(.15,.46,.69),Vector3(side*1.36,.79,-1.38+i*.7),sand)
            skirt.rotation_degrees.z = side*5
        box(label+"FrontMudguard",Vector3(.5,.13,.35),Vector3(side*1.1,.68,-1.86),sand)
        box(label+"RearMudguard",Vector3(.5,.3,.12),Vector3(side*1.1,.32,1.96),rubber)
        var trim = box(label+"Trim",Vector3(.018,.09,.38),Vector3(side*1.46,.84,.95),accent)
        trim.set_meta("team_role","accent")
        box(label+"HeadlampHousing",Vector3(.24,.14,.13),Vector3(side*.79,.7,-1.9),rubber)
        box(label+"Headlamp",Vector3(.14,.065,.025),Vector3(side*.79,.72,-1.97),light_sand)

    cylinder("TurretRing",.72,.16,Vector3(0,1.0,-.15),steel,24)
    armor("Turret",
        [Vector2(-.37,-1.23),Vector2(.37,-1.23),Vector2(1.04,-.6),Vector2(1.0,.72),Vector2(.78,1.15),Vector2(-.78,1.15),Vector2(-1.0,.72),Vector2(-1.04,-.6)],
        [Vector2(-.34,-.98),Vector2(.34,-.98),Vector2(.88,-.5),Vector2(.82,.68),Vector2(.66,.97),Vector2(-.66,.97),Vector2(-.82,.68),Vector2(-.88,-.5)],
        1.09,1.59,light_sand)
    box("GunMantlet",Vector3(.52,.32,.42),Vector3(0,1.29,-1.15),sand)
    var barrel = cylinder("Barrel",.073,2.56,Vector3(0,1.31,-2.42),sand,12)
    barrel.rotation_degrees.x = 90
    var evacuator = cylinder("BoreEvacuator",.112,.44,Vector3(0,1.31,-2.14),dark_sand)
    evacuator.rotation_degrees.x = 90
    var muzzle = cylinder("Muzzle",.08,.13,Vector3(0,1.31,-3.72),steel)
    muzzle.rotation_degrees.x = 90
    var bore = cylinder("MuzzleOpening",.055,.012,Vector3(0,1.31,-3.79),rubber)
    bore.rotation_degrees.x = 90
    for z in [-1.5,-2.65,-3.25]:
        var band = cylinder("BarrelBand"+str(z),.079,.04,Vector3(0,1.31,z),dark_sand)
        band.rotation_degrees.x = 90

    cylinder("CommanderCupola",.29,.13,Vector3(.46,1.64,.32),sand,16)
    cylinder("CommanderHatch",.24,.035,Vector3(.46,1.72,.32),dark_sand,16)
    cylinder("LoaderHatch",.25,.06,Vector3(-.43,1.63,.32),sand,16)
    box("GunnerSight",Vector3(.26,.2,.31),Vector3(.52,1.66,-.47),sand)
    box("GunnerOptic",Vector3(.2,.08,.025),Vector3(.52,1.7,-.64),glass)
    cylinder("PanoramicSightMount",.17,.22,Vector3(-.51,1.68,-.43),sand)
    box("PanoramicSight",Vector3(.28,.2,.25),Vector3(-.51,1.87,-.43),dark_sand)
    box("PanoramicOptic",Vector3(.2,.09,.025),Vector3(-.51,1.9,-.56),glass)
    cylinder("MachineGunPedestal",.055,.23,Vector3(.53,1.86,.25),steel)
    box("MachineGunReceiver",Vector3(.1,.11,.33),Vector3(.53,1.97,.10),steel)
    box("MachineGunBarrel",Vector3(.035,.035,.48),Vector3(.53,1.99,-.29),rubber)
    box("MachineGunAmmo",Vector3(.16,.14,.2),Vector3(.68,1.94,.09),dark_sand)
    for side in [-1,1]:
        cylinder("Antenna"+str(side),.012,.95,Vector3(side*.65,2.04,.77),rubber,6)
        for i in 3:
            var launcher = cylinder("SmokeLauncher"+str(side)+"_"+str(i),.045,.17,Vector3(side*1.0,1.4,-.14+i*.13),steel,8)
            launcher.rotation_degrees.z = side*60
    box("BustleRackBase",Vector3(1.7,.1,.45),Vector3(0,1.18,1.28),dark_sand)
    box("BustleRackBack",Vector3(1.76,.055,.055),Vector3(0,1.51,1.53),steel)
    for side in [-1,1]:
        box("RackSide"+str(side),Vector3(.045,.05,.49),Vector3(side*.86,1.51,1.3),steel)
    for i in 7:
        box("RackUpright"+str(i),Vector3(.026,.34,.026),Vector3(-.82+i*.273,1.35,1.53),steel)
    box("StowageLeft",Vector3(.53,.22,.3),Vector3(-.47,1.36,1.27),sand)
    box("StowageRight",Vector3(.45,.25,.3),Vector3(.38,1.38,1.27),dark_sand)
    box("EngineGrilleBase",Vector3(1.52,.025,.45),Vector3(0,.967,1.65),rubber)
    for i in 10:
        box("EngineLouver"+str(i),Vector3(.055,.035,.41),Vector3(-.68+i*.15,.99,1.65),dark_sand)
    box("RearExhaust",Vector3(1.3,.25,.06),Vector3(0,.58,2.045),rubber)
    for i in 7:
        box("ExhaustLouver"+str(i),Vector3(.04,.22,.045),Vector3(-.55+i*.18,.58,2.08),steel)
    box("DriverHatch",Vector3(.4,.04,.35),Vector3(0,.97,-1.1),dark_sand)
    box("DriverPeriscope",Vector3(.2,.06,.1),Vector3(0,1.01,-1.31),glass)
    var packed = PackedScene.new()
    assert(packed.pack(body) == OK)
    assert(ResourceSaver.save(packed,"res://Scenes/Units/Models/AbramsBody.tscn") == OK)
    body.free()
    print("ABRAMS BODY SAVED")
    quit()