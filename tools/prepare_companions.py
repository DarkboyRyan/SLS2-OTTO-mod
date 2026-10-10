"""只读取 PNG 的透明通道做验收，写 Godot AtlasTexture 裁切参数；不修改生成图像。"""
import json
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
folder = root / 'OTTO/images/companions'
report = {}
for key in ('xuan_dog', 'shan_ni_ruo', 'huo_xing_bao'):
    im = Image.open(folder / f'{key}.png')
    assert im.mode == 'RGBA', f'{key} 缺少透明通道'
    alpha = im.getchannel('A')
    assert alpha.getextrema()[0] == 0, f'{key} 背景不是透明的'
    x0, y0, x1, y1 = alpha.point(lambda a: 255 if a >= 128 else 0).getbbox()
    # 半透明阈值用于寻找人物实体，四周留 4 像素保护抗锯齿边缘。
    x0, y0, x1, y1 = max(0,x0-4), max(0,y0-4), min(im.width,x1+4), min(im.height,y1+4)
    width, height = x1-x0, y1-y0
    for suffix, region in [('body',(x0,y0,width,height)), ('icon',(x0,y0,width,min(width,height)))]:
        text = '[gd_resource type="AtlasTexture" load_steps=2 format=3]\n\n'
        text += f'[ext_resource type="Texture2D" path="res://OTTO/images/companions/{key}.png" id="1"]\n\n'
        text += '[resource]\natlas = ExtResource("1")\nregion = Rect2(%s)\nfilter_clip = true\n' % ', '.join(map(str, region))
        (folder/f'{key}_{suffix}.tres').write_text(text)
    report[key] = {'size': im.size, 'alpha_range': alpha.getextrema(), 'body_region': [x0,y0,width,height], 'display_height':172}
(root/'art/zoo-asset-report.json').write_text(json.dumps(report,indent=2)+'\n')
print(json.dumps(report,indent=2))
