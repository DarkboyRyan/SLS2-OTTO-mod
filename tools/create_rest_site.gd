extends SceneTree
func _initialize():
    call_deferred("build")
func add(parent, child, name, owner):
    child.name = name
    parent.add_child(child)
    child.owner = owner
    return child
func build():
    var scene := Node2D.new()
    scene.name = "OttoRestSite"
    var control = add(scene, Control.new(), "ControlRoot", scene)
    control.mouse_filter = Control.MOUSE_FILTER_IGNORE
    var hitbox = add(control, Control.new(), "Hitbox", scene)
    hitbox.unique_name_in_owner = true
    hitbox.position = Vector2(-310, -560)
    hitbox.size = Vector2(620, 670)
    hitbox.focus_mode = Control.FOCUS_ALL
    var visuals = add(control, Node2D.new(), "RestVisuals", scene)
    var texture := AtlasTexture.new()
    texture.atlas = load("res://OTTO/images/character/otto_rest_user.png")
    texture.region = texture.atlas.get_image().get_used_rect()
    assert(ResourceSaver.save(texture, "res://OTTO/images/character/otto_rest_user_body.tres") == OK)
    var body = add(visuals, Sprite2D.new(), "Otto", scene)
    body.texture = texture
    body.scale = Vector2.ONE * (500.0 / texture.get_height())
    body.position = Vector2(0, -250)
    for i in 3:
        var buddy = add(visuals, Sprite2D.new(), ["XuanDog", "ShanNiRuo", "HuoXingBao"][i], scene)
        buddy.texture = load("res://OTTO/images/companions/" + ["xuan_dog", "shan_ni_ruo", "huo_xing_bao"][i] + "_body.tres")
        buddy.scale = Vector2.ONE * (115.0 / buddy.texture.get_height())
        buddy.position = Vector2((i - 1) * 94, 45)
    var player = add(scene, AnimationPlayer.new(), "AnimationPlayer", scene)
    var lib := AnimationLibrary.new()
    player.add_animation_library("", lib)
    var idle := Animation.new()
    idle.length = 2.4
    idle.loop_mode = Animation.LOOP_LINEAR
    var tr := idle.add_track(Animation.TYPE_VALUE)
    idle.track_set_path(tr, "ControlRoot/RestVisuals/Otto:scale")
    for i in 3:
        idle.track_insert_key(tr, i * 1.2, body.scale * (1.008 if i == 1 else 1.0))
    lib.add_animation("idle", idle)
    player.autoplay = "idle"
    var packed := PackedScene.new()
    assert(packed.pack(scene) == OK)
    assert(ResourceSaver.save(packed, "res://OTTO/scenes/otto_rest_site.tscn") == OK)
    scene.free()
    quit()
