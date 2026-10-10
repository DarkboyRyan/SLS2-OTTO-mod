extends SceneTree
## 在独立 Godot 工程中验证最终 PCK，确保检查的不是开发目录里未打包的资源。
## -- 后第一个参数为 OTTO.pck 绝对路径；第二个可选参数 --preview 打开动作预览。

var preview_player: AnimationPlayer
var preview_label: Label
var preview_time := 0.0
var preview_step := -1

func _initialize() -> void:
    call_deferred("verify")

func verify() -> void:
    var args := OS.get_cmdline_user_args()
    assert(args.size() >= 1, "请传入分发包路径")
    assert(ProjectSettings.load_resource_pack(args[0]), "无法加载分发包")

    # 场景节点名称和类型必须满足 BaseLib 的转换约定。
    var combat := load("res://OTTO/scenes/otto_combat.tscn").instantiate() as Node2D
    root.add_child(combat)
    for required in ["Visuals", "Bounds", "CenterPos", "IntentPos", "OrbPos", "TalkPos"]:
        assert(combat.get_node_or_null("%" + required) != null, "缺少唯一节点 " + required)
    var sprite := combat.get_node("Visuals/Body/Sprite") as Sprite2D
    var body := combat.get_node("Visuals/Body") as Node2D
    assert(sprite.texture is CompressedTexture2D)
    var pixels := sprite.texture.get_image()
    assert(pixels.get_size() == Vector2i(1024, 1536))
    assert(pixels.get_pixel(200, 700).a < 0.01, "人物外围必须透明")
    assert(pixels.get_pixel(500, 500).a > 0.95, "人物主体不应透明")
    var player := combat.get_node("AnimationPlayer") as AnimationPlayer
    player.callback_mode_process = AnimationMixer.ANIMATION_CALLBACK_MODE_PROCESS_MANUAL
    assert(player.autoplay == "idle")

    # 在真实命中时刻确认出现了伸拳贴图，而不是只有整个人前移。
    var idle_texture := sprite.texture
    var idle_sprite_position := sprite.position
    var idle_sprite_scale := sprite.scale
    for punch_name in ["attack", "heavyAttack"]:
        player.play(punch_name)
        player.advance(0)
        player.advance(0.08)
        assert(sprite.texture is AtlasTexture, "蓄力时应抬手握拳")
        assert((sprite.texture as AtlasTexture).region.position == Vector2.ZERO)
        player.advance(0.07)
        assert(sprite.texture is AtlasTexture)
        var impact := sprite.texture as AtlasTexture
        assert(impact.region == Rect2(512, 780, 512, 756), "0.15 秒必须切到完全出拳帧")
        assert(impact.atlas.get_image().get_pixel(0, 0).a < 0.01)
        player.advance(0.35)
        player.advance(0.01)
        assert(sprite.texture == idle_texture, "收拳后必须恢复待机贴图")
        assert(sprite.position.is_equal_approx(idle_sprite_position))
        assert(sprite.scale.is_equal_approx(idle_sprite_scale))
        print("OTTO_PUNCH_OK: ", punch_name, " impact=0.15s -> idle texture")

    # 攻击中受击/死亡会抢占动画，验证旧出拳贴图和锚点不会残留。
    for interruption in ["hurt", "die", "cast"]:
        player.play("attack")
        player.advance(0)
        player.advance(0.16)
        assert(sprite.texture is AtlasTexture)
        player.play(interruption)
        player.advance(0)
        assert(sprite.texture == idle_texture, "被打断后残留出拳姿势")
        assert(sprite.position.is_equal_approx(idle_sprite_position))
        assert(sprite.scale.is_equal_approx(idle_sprite_scale))
        print("OTTO_PUNCH_INTERRUPT_OK: ", interruption)

    # 分别模拟完整动作，并检查是否回待机；受击闪色应恢复正常。
    for action in ["attack", "heavyAttack", "cast", "powerup", "hurt", "revive"]:
        assert(player.has_animation(action), "缺少动画 " + action)
        assert(player.animation_get_next(action) == "idle")
        player.play(action)
        player.advance(0)
        player.advance(player.get_animation(action).length + 0.01)
        player.advance(0.01)
        assert(player.current_animation == "idle", action + " 未回到待机")
        assert(body.modulate.is_equal_approx(Color.WHITE), "动画残留颜色")
        assert(absf(body.rotation) < 0.001, "动画残留旋转")
        assert(body.position.length() < 0.001, "动画残留偏移")
        print("OTTO_ANIMATION_OK: ", action, " -> idle")
    player.play("die")
    player.advance(0)
    player.advance(0.71)
    assert(player.animation_get_next("die") == "")
    assert(absf(body.rotation + PI / 2.0) < 0.001, "死亡应保持倒地")
    assert(body.modulate.a < 0.7)
    print("OTTO_ANIMATION_OK: die remains fallen")

    # 检查选人背景在三种比例下充满父容器，且海报完整显示、不截获鼠标。
    var canvas := Control.new()
    root.add_child(canvas)
    var background := load("res://OTTO/scenes/otto_select_bg.tscn").instantiate() as Control
    canvas.add_child(background)
    for dimensions in [Vector2(1920, 1080), Vector2(1280, 720), Vector2(2560, 1080)]:
        canvas.size = dimensions
        await process_frame
        assert(background.size.is_equal_approx(dimensions))
        var poster := background.get_node("Portrait") as TextureRect
        assert(poster.texture.get_size() == Vector2(347, 576))
        assert(poster.stretch_mode == TextureRect.STRETCH_KEEP_ASPECT_CENTERED)
        for layer in background.get_children():
            assert(layer.mouse_filter == Control.MOUSE_FILTER_IGNORE)
        print("OTTO_BACKGROUND_OK: ", dimensions)
    canvas.free()
    combat.free()
    print("OTTO_PACK_ASSETS_OK")

    if "--preview" in args:
        create_preview()
    else:
        quit(0)

func label_at(parent: Node, words: String, pos: Vector2, font_size: int) -> Label:
    var label := Label.new()
    label.text = words
    label.position = pos
    label.add_theme_font_size_override("font_size", font_size)
    parent.add_child(label)
    return label

func create_preview() -> void:
    # 此窗口只用于资源验收，真正运行的是 PCK 内的场景与 AnimationPlayer。
    root.title = "OTTO v0.1.3 - asset preview"
    root.size = Vector2i(1280, 800)
    var back := ColorRect.new()
    back.color = Color("282631")
    back.size = Vector2(1280, 800)
    root.add_child(back)
    label_at(root, "OTTO / CHARACTER SELECT POSTER", Vector2(32, 22), 24)
    var panel := Control.new()
    panel.position = Vector2(32, 80)
    panel.size = Vector2(768, 432)
    root.add_child(panel)
    panel.add_child(load("res://OTTO/scenes/otto_select_bg.tscn").instantiate())
    label_at(panel, "OTTO", Vector2(42, 145), 46)
    label_at(panel, "Ironclad deck & stats", Vector2(46, 205), 20)

    label_at(root, "COMBAT / 304 PX", Vector2(860, 80), 24)
    var combat := load("res://OTTO/scenes/otto_combat.tscn").instantiate() as Node2D
    combat.position = Vector2(1030, 510)
    root.add_child(combat)
    preview_player = combat.get_node("AnimationPlayer")
    preview_label = label_at(root, "idle", Vector2(940, 550), 26)
    label_at(root, "Transparent PNG + Godot AnimationPlayer", Vector2(32, 570), 24)
    label_at(root, "idle / attack / cast / hurt / die / revive", Vector2(32, 615), 22)
    label_at(root, "Animations cycle automatically. Close window to exit.", Vector2(32, 700), 19)

func _process(delta: float) -> bool:
    if is_instance_valid(preview_player):
        preview_time += delta
        var step := int(preview_time / 2.5) % 6
        if step != preview_step:
            preview_step = step
            var action: String = ["idle", "attack", "cast", "hurt", "die", "revive"][step]
            preview_player.play(action)
            preview_label.text = action
    return false
