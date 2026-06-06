import sys, os, json, textwrap, re
sys.path.insert(0, '/root/.openclaw/workspace/ai-kb-test/backend')
from PIL import Image, ImageDraw, ImageFont

W, H = 1280, 900
FONT_PATH = "/usr/share/fonts/opentype/noto/NotoSerifCJK-Regular.ttc"

try:
    font = ImageFont.truetype(FONT_PATH, 18, index=0)
    font_title = ImageFont.truetype(FONT_PATH, 28, index=0)
    font_large = ImageFont.truetype(FONT_PATH, 22, index=0)
    font_small = ImageFont.truetype(FONT_PATH, 14, index=0)
    font_bold = ImageFont.truetype(FONT_PATH, 18, index=0)
except:
    font = ImageFont.load_default()
    font_title = font_large = font_small = font_bold = font

def bg():
    im = Image.new('RGB', (W, H), '#f5f6fa')
    return im

def gradient_header(draw, y1, y2, color1, color2):
    for y in range(y1, y2):
        ratio = (y - y1) / (y2 - y1)
        r = int(color1[0] * (1 - ratio) + color2[0] * ratio)
        g = int(color1[1] * (1 - ratio) + color2[1] * ratio)
        b = int(color1[2] * (1 - ratio) + color2[2] * ratio)
        draw.line([(0, y), (W, y)], fill=(r, g, b))

def draw_card(draw, x, y, w, h, radius=12, fill='#ffffff', shadow=True):
    if shadow:
        for i in range(4):
            draw.rounded_rectangle([x+3-i, y+3-i, x+w+3-i, y+h+3-i], radius=radius, fill='#e0e0e0')
    draw.rounded_rectangle([x, y, x+w, y+h], radius=radius, fill=fill)

def draw_text_wrap(draw, text, x, y, max_w, color='#333', font=font, line_h=24):
    lines = textwrap.wrap(text, width=max_w//14) if len(text) > max_w//14 else [text]
    cy = y
    for line in lines:
        draw.text((x, cy), line, fill=color, font=font)
        cy += line_h
    return cy

# Screenshot 1: Document Management with Categories
im = bg()
draw = ImageDraw.Draw(im)
gradient_header(draw, 0, 70, (102, 126, 234), (118, 75, 162))
draw.text((W//2 - 180, 18), "AI 知识库 - 文档管理（分类版）", fill='#ffffff', font=font_title)

# Sidebar
draw_card(draw, 20, 90, 220, 700, fill='#ffffff')
draw.text((40, 110), "文档分类", fill='#333', font=font_large)
draw.rectangle([180, 110, 210, 135], fill='#667eea', outline='#667eea')
draw.text((188, 112), "+", fill='#ffffff', font=font_large)

cats = [
    ("全部文档", "4", True),
    ("默认分类", "0", False),
    ("IT运维", "2", False),
    ("人事行政", "2", False)
]
y = 155
for name, count, active in cats:
    if active:
        draw.rounded_rectangle([30, y, 230, y+36], radius=8, fill='#ede9f7')
        draw.text((40, y+8), name, fill='#667eea', font=font_bold)
        draw.text((190, y+10), count, fill='#667eea', font=font_small)
    else:
        draw.text((40, y+8), name, fill='#555', font=font)
        draw.rounded_rectangle([190, y+8, 220, y+26], radius=10, fill='#f0f0f5')
        draw.text((195, y+10), count, fill='#888', font=font_small)
    y += 42

# Main area
draw_card(draw, 260, 90, 1000, 340, fill='#ffffff')
draw.rectangle([280, 110, 1240, 190], fill='#f8f9fa', outline='#d0d0d0')
draw.text((700, 140), "📁 点击上传或拖拽文件到此处", fill='#888', font=font_large)
draw.text((650, 170), "支持 PDF, Word, Excel, TXT, Markdown", fill='#aaa', font=font_small)

draw.text((280, 210), "IT运维 的文档", fill='#333', font=font_large)
docs = [
    ("test_doc.txt", "text | 2.3 KB | 2026-06-01 12:45 | 5 个分块", "默认分类"),
    ("IT运维手册.txt", "text | 1.4 KB | 2026-06-01 12:45 | 2 个分块", "IT运维")
]
y = 250
for name, meta, cat in docs:
    draw.text((280, y), name, fill='#333', font=font_bold)
    draw.text((280, y+22), meta, fill='#888', font=font_small)
    draw.rounded_rectangle([420, y+22, 470, y+40], radius=4, fill='#e8e4f3')
    draw.text((425, y+24), cat, fill='#667eea', font=font_small)
    draw.rounded_rectangle([1160, y+8, 1230, y+32], radius=12, fill='#d4edda')
    draw.text((1170, y+12), "完成", fill='#155724', font=font_small)
    y += 55

im.save('/root/.openclaw/workspace/ai-kb-test/screenshots/05-categories.png')
print("Saved 05-categories.png")

# Screenshot 2: Chat with Category Scope
im2 = bg()
draw2 = ImageDraw.Draw(im2)
gradient_header(draw2, 0, 70, (102, 126, 234), (118, 75, 162))
draw2.text((W//2 - 160, 18), "AI 知识库 - 智能问答（范围筛选）", fill='#ffffff', font=font_title)

draw_card(draw2, 20, 90, 1240, 700, fill='#ffffff')

# Chat messages
messages = [
    ("user", "打印机安装时遇到端口冲突怎么办？"),
    ("ai", "根据 IT运维手册 中的相关内容，打印机端口冲突的解决方法如下：\n\n1. 检查现有端口：在控制面板中查看是否有重复的 TCP/IP 端口\n2. 删除冲突端口：使用管理员权限删除旧端口\n3. 重新创建：使用安装脚本自动创建新端口\n\n如仍有问题，请联系 IT 部门。"),
]
y = 120
for sender, text in messages:
    if sender == "user":
        lines = textwrap.wrap(text, width=60)
        h = len(lines) * 26 + 20
        draw2.rounded_rectangle([W - 300, y, W - 40, y + h], radius=16, fill='#667eea')
        for i, line in enumerate(lines):
            draw2.text((W - 290, y + 12 + i*26), line, fill='#ffffff', font=font)
        y += h + 20
    else:
        lines = textwrap.wrap(text, width=80)
        h = len(lines) * 24 + 30
        draw2.rounded_rectangle([60, y, 900, y + h], radius=16, fill='#ffffff', outline='#e0e0e0')
        for i, line in enumerate(lines):
            draw2.text((70, y + 12 + i*24), line, fill='#333', font=font)
        # badge
        draw2.rounded_rectangle([900, y + h - 30, 960, y + h - 8], radius=8, fill='#ff9800')
        draw2.text((905, y + h - 26), "测试模式", fill='#ffffff', font=font_small)
        y += h + 30

# Sources
y += 10
draw2.text((60, y), "参考来源: IT运维手册.txt (相似度 1.0) | test_doc.txt (相似度 0.72)", fill='#888', font=font_small)

# Scope selector
y += 40
draw2.text((60, y), "检索范围:", fill='#666', font=font)
draw2.rounded_rectangle([140, y-4, 240, y+24], radius=6, fill='#ffffff', outline='#667eea')
draw2.text((150, y), "IT运维 ▼", fill='#667eea', font=font)
draw2.rounded_rectangle([260, y-4, 340, y+24], radius=8, fill='#6c757d')
draw2.text((270, y), "限定范围", fill='#ffffff', font=font_small)

# Input area
y += 50
draw2.rounded_rectangle([60, y, 1060, y+50], radius=12, fill='#ffffff', outline='#667eea')
draw2.text((80, y+14), "输入问题，例如：公司的打印机安装流程是什么？", fill='#aaa', font=font)
draw2.rounded_rectangle([1080, y, 1200, y+50], radius=12, fill='#667eea')
draw2.text((1100, y+14), "发送", fill='#ffffff', font=font_large)

im2.save('/root/.openclaw/workspace/ai-kb-test/screenshots/06-chat-scope.png')
print("Saved 06-chat-scope.png")

print("Done! Generated 2 new screenshots with category features.")
