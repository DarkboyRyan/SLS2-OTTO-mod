# 文字能量图标使用原版的24×24画布；保留人物比例和透明背景。
# 大费用图标与战斗计数器仍使用原始纹理。
extends SceneTree
func _initialize() -> void:
    var source := Image.load_from_file("res://OTTO/images/ui/otto_energy.png")
    if source == null:
        quit(1)
        return
    source.convert(Image.FORMAT_RGBA8)
    var scale: float = 24.0 / max(source.get_width(), source.get_height())
    source.resize(roundi(source.get_width() * scale), roundi(source.get_height() * scale), Image.INTERPOLATE_LANCZOS)
    var canvas := Image.create(24, 24, false, Image.FORMAT_RGBA8)
    canvas.fill(Color.TRANSPARENT)
    canvas.blit_rect(source, Rect2i(Vector2i.ZERO, source.get_size()), (canvas.get_size() - source.get_size()) / 2)
    var result := canvas.save_png("res://OTTO/images/ui/otto_energy_text.png")
    quit(0 if result == OK else 1)
