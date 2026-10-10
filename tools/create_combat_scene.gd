extends SceneTree
## 开发工具：生成可被 BaseLib 转换的战斗场景；不随 Mod 导出。
## 用法：MegaDot --headless --path 项目目录 --script tools/create_combat_scene.gd
## 修改动画后重新运行此工具，再执行 build.sh，即可更新游戏资源。

const TEXTURE_PATH := "res://OTTO/images/character/otto_combat.png"
const PUNCH_PATH := "res://OTTO/images/character/otto_punch_sheet.png"
const SCENE_PATH := "res://OTTO/scenes/otto_combat.tscn"
const DISPLAY_HEIGHT := 304.0
# 出拳贴图在 0.15 秒到达完全伸直，和铁甲战士的 AttackAnimDelay 保持同步。
const PUNCH_TIMES := [0.0, 0.035, 0.075, 0.115, 0.15, 0.23, 0.29, 0.36, 0.48]
const PUNCH_FRAMES := [0, 1, 1, 2, 3, 3, 2, 1, 0]
# 实际生成图为 1024×1536。横向分界位于透明间隔 y=780，避免切到上一行鞋底。
# 选取左上、左下、右下三个姿势；右上收的是另一只手，弃用以保证同一只手出拳。
const PUNCH_REGIONS := [Rect2i(0, 0, 512, 780), Rect2i(0, 780, 512, 756), Rect2i(512, 780, 512, 756)]

var scene_root: Node2D
var poses: Array[Dictionary] = []

func _initialize() -> void:
    call_deferred("build_scene")

func add_owned(parent: Node, child: Node, node_name: String, unique := false) -> Node:
    # PackedScene 只保存归属本场景的节点；unique 用于游戏通过 %名字 查找节点。
    child.name = node_name
    parent.add_child(child)
    child.owner = scene_root
    child.unique_name_in_owner = unique
    return child

func add_marker(node_name: String, pos: Vector2) -> void:
    var marker := add_owned(scene_root, Marker2D.new(), node_name, true) as Marker2D
    marker.position = pos

func opaque_bounds(source: Image, area := Rect2i()) -> Rect2i:
    # 忽略边缘极低透明度的杂点，只用主体计算脚底和显示高度；不修改原始图片。
    # area 用于在图集中检查某一格；返回坐标相对于该格，便于 AtlasTexture 定位。
    if area.size == Vector2i.ZERO:
        area = Rect2i(Vector2i.ZERO, source.get_size())
    var low := area.size
    var high := Vector2i.ZERO
    for y in area.size.y:
        for x in area.size.x:
            if source.get_pixel(x + area.position.x, y + area.position.y).a >= 0.5:
                low = low.min(Vector2i(x, y))
                high = high.max(Vector2i(x + 1, y + 1))
    assert(high.y > low.y, "人物图片必须包含非透明像素")
    return Rect2i(low, high - low)

func foot_anchor(source: Image, area: Rect2i, bounds: Rect2i) -> Vector2:
    # 只看最下方的鞋子来对齐人物；出拳让整体轮廓变宽，不能用全身包围盒的中心。
    var left := area.size.x
    var right := 0
    var shoe_top := bounds.end.y - int(bounds.size.y * 0.1)
    for y in range(shoe_top, bounds.end.y):
        for x in area.size.x:
            if source.get_pixel(x + area.position.x, y + area.position.y).a >= 0.5:
                left = mini(left, x)
                right = maxi(right, x + 1)
    return Vector2((left + right) / 2.0, bounds.end.y)

func load_poses(idle_source: Image, idle_bounds: Rect2i, idle_unit: float) -> void:
    # 第 0 帧是原待机图，第 1–3 帧为抬手蓄力、开始伸臂、完全出拳。
    var idle_anchor := foot_anchor(idle_source, Rect2i(Vector2i.ZERO, idle_source.get_size()), idle_bounds)
    poses.append({"texture": load(TEXTURE_PATH), "position": -idle_anchor * idle_unit,
        "scale": Vector2.ONE * idle_unit})
    var sheet_image := Image.load_from_file(ProjectSettings.globalize_path(PUNCH_PATH))
    assert(sheet_image != null, "缺少出拳动作图集")
    assert(sheet_image.get_size() == Vector2i(1024, 1536), "换图后需重新检查图集区域")
    var sheet := load(PUNCH_PATH) as Texture2D
    var frame_bounds: Array[Rect2i] = []
    for region in PUNCH_REGIONS:
        frame_bounds.append(opaque_bounds(sheet_image, region))
    # 三帧共用同一个缩放值，保留手臂和身体的相对比例，避免动作中忽大忽小。
    var punch_unit := DISPLAY_HEIGHT / float(frame_bounds[0].size.y)
    var preview_frames: Array[Dictionary] = []
    for index in PUNCH_REGIONS.size():
        var atlas := AtlasTexture.new()
        atlas.atlas = sheet
        atlas.region = Rect2(PUNCH_REGIONS[index])
        atlas.filter_clip = true
        var anchor := foot_anchor(sheet_image, PUNCH_REGIONS[index], frame_bounds[index])
        poses.append({"texture": atlas, "position": -anchor * punch_unit,
            "scale": Vector2.ONE * punch_unit})
        preview_frames.append({"region": [PUNCH_REGIONS[index].position.x, PUNCH_REGIONS[index].position.y,
            PUNCH_REGIONS[index].size.x, PUNCH_REGIONS[index].size.y], "anchor": [anchor.x, anchor.y], "unit": punch_unit})
        print("OTTO_PUNCH_FRAME: ", index + 1, " bounds=", frame_bounds[index], " foot=", anchor)
    # 网页预览读取同一份锚点和帧时间数据，方便以实际尺寸检查，不额外处理 PNG。
    var preview := {"sheet_size": [sheet_image.get_width(), sheet_image.get_height()],
        "idle_size": [idle_source.get_width(), idle_source.get_height()],
        "idle_anchor": [idle_anchor.x, idle_anchor.y], "idle_unit": idle_unit,
        "frames": preview_frames, "times": PUNCH_TIMES, "sequence": PUNCH_FRAMES}
    var output := FileAccess.open("res://art/punch-preview.json", FileAccess.WRITE)
    output.store_string(JSON.stringify(preview, "  ") + "\n")
    output.close()

func add_pose_tracks(animation: Animation, times: Array, frame_indices: Array) -> void:
    # 贴图与贴图锚点必须同步离散切换。其他动作也写回原图，支持攻击途中受击/死亡。
    for property in ["texture", "position", "scale"]:
        var track := animation.add_track(Animation.TYPE_VALUE)
        animation.track_set_path(track, NodePath("Visuals/Body/Sprite:" + property))
        animation.value_track_set_update_mode(track, Animation.UPDATE_DISCRETE)
        for index in times.size():
            animation.track_insert_key(track, times[index], poses[frame_indices[index]][property])

func make_animation(duration: float, times: Array, positions: Array,
        rotations: Array, scales: Array, colors: Array, loop := false) -> Animation:
    # 每段动画都完整写入四种属性，避免受击的红色或倒地旋转残留到下一动作。
    var animation := Animation.new()
    animation.length = duration
    if loop:
        animation.loop_mode = Animation.LOOP_LINEAR
    var paths := ["position", "rotation", "scale", "modulate"]
    var values := [positions, rotations, scales, colors]
    for channel in paths.size():
        var track := animation.add_track(Animation.TYPE_VALUE)
        animation.track_set_path(track, NodePath("Visuals/Body:" + paths[channel]))
        animation.track_set_interpolation_type(track, Animation.INTERPOLATION_LINEAR)
        for frame in times.size():
            animation.track_insert_key(track, times[frame], values[channel][frame])
    return animation

func build_scene() -> void:
    # 开发工具读取原 PNG 的 alpha；转换为磁盘路径，避免被误认为运行时资源加载。
    var source := Image.load_from_file(ProjectSettings.globalize_path(TEXTURE_PATH))
    assert(source != null, "缺少人物 PNG")
    var bounds := opaque_bounds(source)
    var unit := DISPLAY_HEIGHT / float(bounds.size.y)
    load_poses(source, bounds, unit)

    # 根节点保留为普通 Node2D；BaseLib 加载后会补上游戏需要的 C# 视觉组件。
    scene_root = Node2D.new()
    scene_root.name = "OttoCombatVisuals"
    scene_root.texture_filter = CanvasItem.TEXTURE_FILTER_LINEAR
    var visuals := add_owned(scene_root, Node2D.new(), "Visuals", true) as Node2D

    # 阴影单独留在脚下。人物前冲、呼吸或倒地时不会把地面阴影一起旋转。
    var shadow := add_owned(visuals, Polygon2D.new(), "Shadow") as Polygon2D
    var ellipse := PackedVector2Array()
    for index in 40:
        var angle := TAU * index / 40.0
        ellipse.append(Vector2(cos(angle) * 57.0, sin(angle) * 9.0))
    shadow.polygon = ellipse
    shadow.color = Color(0.02, 0.015, 0.02, 0.28)

    # Body 的原点就是脚底中心，缩放时脚不漂浮，倒地时也绕脚底旋转。
    var body := add_owned(visuals, Node2D.new(), "Body") as Node2D
    var sprite := add_owned(body, Sprite2D.new(), "Sprite") as Sprite2D
    sprite.texture = poses[0].texture
    sprite.centered = false
    sprite.scale = poses[0].scale
    sprite.position = poses[0].position

    # Bounds 是游戏的悬停/点击与血条布局参考区域，并不是物理碰撞体。
    var hit_bounds := add_owned(scene_root, Control.new(), "Bounds", true) as Control
    var half_width := maxf(70.0, bounds.size.x * unit * 0.56)
    hit_bounds.position = Vector2(-half_width, -DISPLAY_HEIGHT - 6.0)
    hit_bounds.size = Vector2(half_width * 2.0, DISPLAY_HEIGHT + 10.0)
    hit_bounds.mouse_filter = Control.MOUSE_FILTER_IGNORE
    add_marker("CenterPos", Vector2(0, -170)) # 命中特效中心。
    add_marker("IntentPos", Vector2(0, -340)) # 意图图标参考点。
    add_marker("TalkPos", Vector2(65, -260)) # 对话气泡参考点。
    add_marker("OrbPos", Vector2(0, -370)) # 若获得充能球，显示在头顶上方。

    var player := add_owned(scene_root, AnimationPlayer.new(), "AnimationPlayer") as AnimationPlayer
    player.root_node = NodePath("..")
    var library := AnimationLibrary.new()
    player.add_animation_library("", library)
    var white := Color.WHITE
    var neutral := Vector2.ONE

    # 待机：仅轻微伸缩模拟呼吸，脚底仍保持在地面。
    var idle := make_animation(2.4, [0.0, 1.2, 2.4],
        [Vector2.ZERO, Vector2.ZERO, Vector2.ZERO], [0.0, 0.0, 0.0],
        [neutral, Vector2(1.008, 1.014), neutral], [white, white, white], true)
    library.add_animation("idle", idle)
    library.add_animation("relaxed", idle)

    # 攻击：抬手→收拳蓄力→伸臂→完全出拳→收回。0.15 秒的命中帧明确伸直手臂。
    # 身体仅轻微前移，动作主要由手臂姿势变化表现；脚底使用各帧鞋底锚点对齐。
    var attack := make_animation(0.48, [0.0, 0.075, 0.15, 0.28, 0.48],
        [Vector2.ZERO, Vector2(-5, 0), Vector2(20, 0), Vector2(12, 0), Vector2.ZERO],
        [0.0, -0.012, 0.02, 0.01, 0.0],
        [neutral, neutral, neutral, neutral, neutral],
        [white, white, white, white, white])
    add_pose_tracks(attack, PUNCH_TIMES, PUNCH_FRAMES)
    library.add_animation("attack", attack)
    library.add_animation("heavyAttack", attack)

    # 施放/力量：轻微抬升并亮起暖金色；不新增伤害或卡牌逻辑。
    var cast := make_animation(0.55, [0.0, 0.2, 0.35, 0.55],
        [Vector2.ZERO, Vector2(0, -7), Vector2(0, -4), Vector2.ZERO],
        [0.0, -0.02, 0.01, 0.0], [neutral, neutral, neutral, neutral],
        [white, Color(1.22, 1.12, 0.92), Color(1.10, 1.04, 0.96), white])
    library.add_animation("cast", cast)
    library.add_animation("powerup", cast)

    # 受击：短暂后仰、闪红，结束后自动恢复正常待机。
    library.add_animation("hurt", make_animation(0.3, [0.0, 0.06, 0.16, 0.3],
        [Vector2.ZERO, Vector2(-11, 0), Vector2(-4, 0), Vector2.ZERO],
        [0.0, -0.065, -0.02, 0.0], [neutral, neutral, neutral, neutral],
        [white, Color(1.0, 0.5, 0.5), Color(1.0, 0.8, 0.8), white]))

    # 死亡：向左倒地并稍微变暗；不接回 idle，避免死亡后又站起来。
    library.add_animation("die", make_animation(0.7, [0.0, 0.16, 0.5, 0.6, 0.7],
        [Vector2.ZERO, Vector2(-7, -4), Vector2(-18, -70), Vector2(-18, -76), Vector2(-18, -70)],
        [0.0, -0.18, -PI / 2.0, -1.48, -PI / 2.0], [neutral, neutral, neutral, neutral, neutral],
        [white, white, Color(0.8, 0.8, 0.8, 1), Color(0.75, 0.75, 0.75, 1), Color(0.7, 0.7, 0.7, 1)]))
    # 复活：从倒地状态起身，再回到循环待机。
    library.add_animation("revive", make_animation(0.5, [0.0, 0.35, 0.5],
        [Vector2(-18, -70), Vector2.ZERO, Vector2.ZERO], [-PI / 2.0, -0.1, 0.0],
        [neutral, neutral, neutral], [Color(0.7, 0.7, 0.7, 1), white, white]))
    # 多个名字可能共用同一个 Animation 资源，因此去重后再添加还原轨道。
    var reset_animations: Array[Animation] = []
    for action in library.get_animation_list():
        var animation := library.get_animation(action)
        if animation != attack and animation not in reset_animations:
            add_pose_tracks(animation, [0.0], [0])
            reset_animations.append(animation)
    for action in ["attack", "heavyAttack", "cast", "powerup", "hurt", "revive"]:
        player.animation_set_next(action, "idle")
    player.autoplay = "idle"

    var packed := PackedScene.new()
    assert(packed.pack(scene_root) == OK)
    assert(ResourceSaver.save(packed, SCENE_PATH) == OK)
    # 资源文件附上中文索引说明，实际动作逻辑请在本生成脚本中调整。
    var serialized := FileAccess.get_file_as_string(SCENE_PATH)
    var output := FileAccess.open(SCENE_PATH, FileAccess.WRITE)
    output.store_string("; OTTO 透明战斗人物与基础动作，由 tools/create_combat_scene.gd 生成。\n"
        + "; Visuals 为显示容器，Bounds 为交互区域，各 Pos 节点定位特效/气泡。\n"
        + "; AnimationPlayer 由 BaseLib 触发；普通动作接回 idle，die 停在倒地状态。\n"
        + serialized)
    output.close()
    print("OTTO_SCENE_CREATED: alpha_bounds=", bounds, " height=", DISPLAY_HEIGHT)
    scene_root.free()
    quit(0)
