extends "res://tools/create_combat_scene.gd"
## 独立测试场景；不改正式版的站姿场景。
const WHEELCHAIR_SCENE := "res://OTTO/scenes/otto_wheelchair_combat.tscn"
const WHEELCHAIR_DIR := "res://OTTO/images/character/wheelchair/"
func wheelchair_pose(file: String) -> Dictionary:
    var image := Image.load_from_file(ProjectSettings.globalize_path(WHEELCHAIR_DIR+file))
    assert(image != null, "缺少轮椅动作素材："+file)
    var rect := opaque_bounds(image)
    var unit := DISPLAY_HEIGHT/float(rect.size.y)
    if file == "sleep.png":
        # 垂头降低轮廓高度，不应因此把整个轮椅放大。
        var idle_image := Image.load_from_file(ProjectSettings.globalize_path(WHEELCHAIR_DIR+"idle.png"))
        unit = poses[0].scale.y * float(idle_image.get_height())/float(image.get_height())
    return {"texture":load(WHEELCHAIR_DIR+file), "scale":Vector2.ONE*unit,
        "position":-Vector2(rect.position.x+rect.size.x/2.0,rect.end.y)*unit}
func build_scene() -> void:
    poses.clear()
    poses.append(wheelchair_pose("idle.png"))
    poses.append(wheelchair_pose("attack.png"))
    var sheet_image := Image.load_from_file(ProjectSettings.globalize_path(WHEELCHAIR_DIR+"defense.png"))
    assert(sheet_image.get_width()%4 == 0)
    var cell_width := sheet_image.get_width()/4
    var regions: Array[Rect2i] = []
    var bounds: Array[Rect2i] = []
    for i in 4:
        regions.append(Rect2i(i*cell_width,0,cell_width,sheet_image.get_height()))
        bounds.append(opaque_bounds(sheet_image,regions[i]))
    var unit := DISPLAY_HEIGHT/float(bounds[0].size.y)
    for i in 4:
        var atlas := AtlasTexture.new()
        atlas.atlas = load(WHEELCHAIR_DIR+"defense.png")
        atlas.region = regions[i]
        atlas.filter_clip = true
        poses.append({"texture":atlas,"scale":Vector2.ONE*unit,
            "position":-Vector2(bounds[i].position.x+bounds[i].size.x/2.0,bounds[i].end.y)*unit})
    poses.append(wheelchair_pose("sleep.png"))
    scene_root = Node2D.new()
    scene_root.name = "OttoWheelchairCombatVisuals"
    scene_root.texture_filter = CanvasItem.TEXTURE_FILTER_LINEAR
    var visuals := add_owned(scene_root,Node2D.new(),"Visuals",true) as Node2D
    var shadow := add_owned(visuals,Polygon2D.new(),"Shadow") as Polygon2D
    var points := PackedVector2Array()
    for i in 40:
        var a := TAU*i/40.0
        points.append(Vector2(cos(a)*92.0,sin(a)*10.0))
    shadow.polygon = points
    shadow.color = Color(0.02,0.015,0.02,0.28)
    var body := add_owned(visuals,Node2D.new(),"Body") as Node2D
    add_owned(body,Node2D.new(),"WheelchairPresentation")
    var sprite := add_owned(body,Sprite2D.new(),"Sprite") as Sprite2D
    sprite.texture = poses[0].texture
    sprite.centered = false
    sprite.scale = poses[0].scale
    sprite.position = poses[0].position
    var hit_bounds := add_owned(scene_root,Control.new(),"Bounds",true) as Control
    hit_bounds.position = Vector2(-130,-310)
    hit_bounds.size = Vector2(260,314)
    hit_bounds.mouse_filter = Control.MOUSE_FILTER_IGNORE
    add_marker("CenterPos",Vector2(0,-160))
    add_marker("IntentPos",Vector2(0,-340))
    add_marker("TalkPos",Vector2(85,-260))
    add_marker("OrbPos",Vector2(0,-370))
    var player := add_owned(scene_root,AnimationPlayer.new(),"AnimationPlayer") as AnimationPlayer
    player.root_node = NodePath("..")
    var library := AnimationLibrary.new()
    player.add_animation_library("",library)
    var white := Color.WHITE
    var neutral := Vector2.ONE
    var idle := make_animation(2.4,[0.0,1.2,2.4],
        [Vector2.ZERO,Vector2.ZERO,Vector2.ZERO],[0.0,0.0,0.0],
        [neutral,Vector2(1.003,1.006),neutral],[white,white,white],true)
    add_pose_tracks(idle,[0.0],[0])
    library.add_animation("idle",idle)
    library.add_animation("relaxed",idle)
    # 0.42秒砸地，和测试构建的AttackAnimDelay严格对齐。
    var attack := make_animation(0.78,[0.0,0.10,0.24,0.34,0.42,0.47,0.52,0.64,0.78],
        [Vector2.ZERO,Vector2(-8,0),Vector2(16,-100),Vector2(30,-112),Vector2(34,0),Vector2(34,-9),Vector2(32,0),Vector2(16,0),Vector2.ZERO],
        [0.0,-0.13,-0.25,-0.14,0.06,-0.02,0.015,0.0,0.0],
        [neutral,neutral,neutral,neutral,neutral,neutral,neutral,neutral,neutral],
        [white,white,white,white,white,white,white,white,white])
    add_pose_tracks(attack,[0.0,0.06,0.64,0.78],[0,1,1,0])
    library.add_animation("attack",attack)
    library.add_animation("heavyAttack",attack)
    var defend := make_animation(0.70,[0.0,0.10,0.26,0.42,0.56,0.70],
        [Vector2.ZERO,Vector2(1,0),Vector2(5,0),Vector2(9,0),Vector2(5,0),Vector2.ZERO],
        [0.0,-0.008,-0.012,0.0,0.0,0.0],
        [neutral,neutral,neutral,neutral,neutral,neutral],[white,white,white,white,white,white])
    add_pose_tracks(defend,[0.0,0.08,0.20,0.32,0.44,0.56,0.70],[0,2,3,4,5,2,0])
    for name in ["cast","powerup","defend","block"]:
        library.add_animation(name,defend)
    var hurt := make_animation(0.30,[0.0,0.06,0.16,0.30],
        [Vector2.ZERO,Vector2(-10,0),Vector2(-3,0),Vector2.ZERO],
        [0.0,-0.045,-0.015,0.0],[neutral,neutral,neutral,neutral],
        [white,Color(1.0,0.5,0.5),Color(1.0,0.8,0.8),white])
    add_pose_tracks(hurt,[0.0],[0])
    library.add_animation("hurt",hurt)
    # 死亡留在轮椅上闭眼睡着，不翻倒、不落地，末帧持续保留。
    var death := make_animation(0.70,[0.0,0.20,0.45,0.70],
        [Vector2.ZERO,Vector2(0,2),Vector2(0,3),Vector2(0,3)],
        [0.0,0.0,0.0,0.0],[neutral,neutral,neutral,neutral],
        [white,white,Color(0.94,0.94,0.94,1),Color(0.92,0.92,0.92,1)])
    add_pose_tracks(death,[0.0,0.20,0.70],[0,6,6])
    library.add_animation("die",death)
    var revive := make_animation(0.50,[0.0,0.25,0.50],
        [Vector2(0,3),Vector2.ZERO,Vector2.ZERO],[0.0,0.0,0.0],
        [neutral,neutral,neutral],[Color(0.92,0.92,0.92,1),white,white])
    add_pose_tracks(revive,[0.0,0.25,0.50],[6,0,0])
    library.add_animation("revive",revive)
    for name in ["attack","heavyAttack","cast","powerup","defend","block","hurt","revive"]:
        player.animation_set_next(name,"idle")
    player.autoplay = "idle"
    var packed := PackedScene.new()
    assert(packed.pack(scene_root) == OK)
    assert(ResourceSaver.save(packed,WHEELCHAIR_SCENE) == OK)
    print("OTTO_WHEELCHAIR_SCENE_OK poses=",poses.size()," attack_hit=0.42 death=sleep")
    scene_root.free()
    quit()
