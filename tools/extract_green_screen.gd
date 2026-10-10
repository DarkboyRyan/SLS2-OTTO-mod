extends SceneTree

# 精确移除高饱和绿色背景，并对半透明边缘做简单去绿处理。
# 用法：MegaDot --headless --script tools/extract_green_screen.gd -- 输入.png 输出.png
func _initialize() -> void:
	var args := OS.get_cmdline_user_args()
	if args.size() != 2:
		push_error("用法：extract_green_screen.gd 输入.png 输出.png")
		quit(2)
		return

	var image := Image.load_from_file(args[0])
	if image.is_empty():
		push_error("无法读取图片：%s" % args[0])
		quit(1)
		return

	image.convert(Image.FORMAT_RGBA8)
	for y in image.get_height():
		for x in image.get_width():
			var color := image.get_pixel(x, y)
			var non_green: float = maxf(color.r, color.b)
			var green_excess: float = color.g - non_green
			# 原图背景的绿色优势很高；人物本身低于该范围。
			var alpha: float = 1.0 - smoothstep(0.08, 0.42, green_excess)
			if alpha < 0.01:
				image.set_pixel(x, y, Color(0.0, 0.0, 0.0, 0.0))
				continue

			# 压掉边缘混色带来的绿色溢色，不改变主体内部颜色。
			if alpha < 0.999:
				color.g = minf(color.g, non_green)
			color.a = alpha
			image.set_pixel(x, y, color)

	var output_dir := args[1].get_base_dir()
	DirAccess.make_dir_recursive_absolute(output_dir)
	var error := image.save_png(args[1])
	if error != OK:
		push_error("无法保存图片：%s" % args[1])
		quit(1)
		return

	print("已保存透明抠图：%s" % args[1])
	quit()
