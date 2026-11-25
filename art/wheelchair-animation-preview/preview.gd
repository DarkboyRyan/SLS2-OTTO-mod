extends SceneTree
var stage: Node2D
var attack: Sprite2D
var defense: Sprite2D
var idle_texture: Texture2D
var attack_texture: Texture2D
var defense_texture: Texture2D
var anim: AnimationPlayer
var glow: Line2D
var ground_y := 727.0
var capture := false
var frame := 0
var total := 144
var elapsed := 0.0
func _initialize():
    capture = "--capture" in OS.get_cmdline_user_args()
    call_deferred("setup")
func png(path: String) -> Texture2D:
    return ImageTexture.create_from_image(Image.load_from_file(path))
func caption(text: String, pos: Vector2, size: int, color: Color) -> void:
    var l := Label.new()
    var font := SystemFont.new()
    font.font_names = PackedStringArray(["PingFang SC", "Microsoft YaHei", "Arial"])
    l.add_theme_font_override("font",font)
    l.text = text
    l.position = pos
    l.add_theme_font_size_override("font_size", size)
    l.add_theme_color_override("font_color", color)
    stage.add_child(l)
func setup():
    root.content_scale_size = Vector2i(1280,820)
    stage = Node2D.new()
    root.add_child(stage)
    var background := ColorRect.new()
    background.color = Color("161d28")
    background.size = Vector2(1280,820)
    stage.add_child(background)
    for x in [20,650]:
        var panel := ColorRect.new()
        panel.color = Color("202a38")
        panel.position = Vector2(x,100)
        panel.size = Vector2(610,670)
        stage.add_child(panel)
        var line := Line2D.new()
        line.points = PackedVector2Array([Vector2(x+25,ground_y),Vector2(x+585,ground_y)])
        line.width = 2.0
        line.default_color = Color("506176")
        stage.add_child(line)
    caption("OTTO 轮椅动作预览",Vector2(36,20),28,Color("ead5a3"))
    caption("A 版造型 · 动作预览",Vector2(38,60),18,Color("9daec2"))
    caption("攻击 · 轮椅下砸",Vector2(50,119),24,Color("f4bc7a"))
    caption("防御 · 手推轮圈",Vector2(680,119),24,Color("88cbe0"))
    caption("独立动作预览 · 尚未替换游戏模型",Vector2(38,783),17,Color("9daec2"))
    caption("蓄力 → 抬起 → 下砸 → 回弹",Vector2(50,157),17,Color("9daec2"))
    caption("抓轮圈 → 向前下方推 → 松手回位",Vector2(680,157),17,Color("9daec2"))
    idle_texture = png("res://assets/idle.png")
    attack_texture = png("res://assets/attack.png")
    defense_texture = png("res://assets/defense.png")
    attack = Sprite2D.new()
    attack.texture = attack_texture
    var used := attack_texture.get_image().get_used_rect()
    attack.region_enabled = true
    attack.region_rect = used
    attack.offset = Vector2(0,-used.size.y/2.0)
    attack.scale = Vector2.ONE * (405.0/used.size.y)
    attack.position = Vector2(305,ground_y)
    stage.add_child(attack)
    defense = Sprite2D.new()
    defense.texture = defense_texture
    defense.hframes = 4
    defense.offset = Vector2(0,-defense_texture.get_height()/2.0)
    defense.scale = Vector2.ONE * (435.0/defense_texture.get_height())
    defense.position = Vector2(940,ground_y+12)
    stage.add_child(defense)
    glow = Line2D.new()
    glow.width = 3.0
    glow.antialiased = true
    glow.default_color = Color(1.0,0.7,0.35,0.0)
    var points := PackedVector2Array()
    for i in range(33):
        var a := TAU*i/32.0
        points.append(Vector2(cos(a)*135,sin(a)*15))
    glow.points = points
    glow.position = Vector2(355,ground_y)
    stage.add_child(glow)
    anim = AnimationPlayer.new()
    stage.add_child(anim)
    attack.name = "Attack"
    anim.root_node = ".."
    var library := AnimationLibrary.new()
    anim.add_animation_library("",library)
    var clip := Animation.new()
    clip.length = 3.0
    var keys = [0.0,0.5,0.72,1.04,1.20,1.31,1.37,1.43,1.60,1.88,2.20,3.0]
    var xs = [305,305,292,330,345,370,373,368,370,350,305,305]
    var ys = [ground_y,ground_y,ground_y,ground_y-145,ground_y-152,ground_y,ground_y-14,ground_y,ground_y,ground_y,ground_y,ground_y]
    var angles = [0,0,-0.16,-0.25,-0.17,0.08,-0.04,0.025,0,0,0,0]
    for property in ["position","rotation"]:
        var tr := clip.add_track(Animation.TYPE_VALUE)
        clip.track_set_path(tr,NodePath("Attack:"+property))
        for i in keys.size():
            clip.track_insert_key(tr,keys[i],Vector2(xs[i],ys[i]) if property == "position" else angles[i])
    library.add_animation("slam",clip)
    anim.play("slam")
    anim.pause()
    if capture:
        DirAccess.make_dir_recursive_absolute("res://frames")
        await RenderingServer.frame_post_draw
        await save_frame()
func _process(delta):
    if stage == null or anim == null: return
    if not capture:
        elapsed += delta
        pose(fmod(elapsed,3.0))
func pose(t: float):
    anim.seek(t,true)
    var active := t >= 0.5 and t <= 2.3
    defense.frame = int(floor((t-0.5)*7.5))%4 if active else 0
    defense.position.x = 940.0 + (sin((t-0.5)*PI*2.0)*16.0 if active else 0.0)
    var hit := clampf(1.0-abs(t-1.34)/0.18,0.0,1.0)
    glow.modulate.a = hit
    glow.default_color.a = 0.7*hit
    glow.scale = Vector2.ONE*(1.0+(1.0-hit)*0.8)
func save_frame():
    for i in total:
        pose(float(i)/48.0)
        await process_frame
        await RenderingServer.frame_post_draw
        var err := root.get_texture().get_image().save_png("res://frames/%04d.png"%i)
        if err != OK:
            push_error("Frame capture failed")
            quit(1)
            return
    quit()
