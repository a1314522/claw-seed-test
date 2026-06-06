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

# Screenshot 7: User Management
im = bg()
draw = ImageDraw.Draw(im)
gradient_header(draw, 0, 70, (102, 126, 234), (118, 75, 162))
draw.text((W//2 - 140, 18), "AI 知识库 - 用户管理", fill='#ffffff', font=font_title)

draw_card(draw, 20, 90, 1240, 760, fill='#ffffff')

draw.text((40, 110), "用户管理", fill='#333', font=font_large)
draw.rounded_rectangle([40, 150, 160, 185], radius=8, fill='#667eea')
draw.text((55, 158), "+ 新建用户", fill='#ffffff', font=font)

# Header row
y = 200
draw.text((40, y), "ID", fill='#666', font=font_bold)
draw.text((120, y), "用户名", fill='#666', font=font_bold)
draw.text((300, y), "权限", fill='#666', font=font_bold)
draw.text((480, y), "创建时间", fill='#666', font=font_bold)
draw.text((700, y), "操作", fill='#666', font=font_bold)
draw.line([(40, y+30), (1200, y+30)], fill='#e0e0e0', width=2)

users = [
    (1, "admin", True, "2026-06-01 12:23:27"),
    (2, "test1", False, "2026-06-01 13:41:16"),
]
y = 240
for uid, name, is_admin, created in users:
    draw.text((40, y), str(uid), fill='#333', font=font)
    draw.text((120, y), name, fill='#333', font=font)
    if is_admin:
        draw.text((300, y), "管理员", fill='#667eea', font=font_bold)
    else:
        draw.text((300, y), "普通用户", fill='#888', font=font)
    draw.text((480, y), created, fill='#888', font=font_small)
    if uid == 1:
        draw.text((700, y), "不可删除", fill='#aaa', font=font_small)
    else:
        draw.rounded_rectangle([700, y-2, 760, y+28], radius=6, fill='#dc3545')
        draw.text((710, y+2), "删除", fill='#ffffff', font=font_small)
    y += 40

im.save('/root/.openclaw/workspace/ai-kb-test/screenshots/07-users.png')
print("Saved 07-users.png")

# Screenshot 8: Chat with History
im2 = bg()
draw2 = ImageDraw.Draw(im2)
gradient_header(draw2, 0, 70, (102, 126, 234), (118, 75, 162))
draw2.text((W//2 - 160, 18), "AI 知识库 - 智能问答（历史记录）", fill='#ffffff', font=font_title)

# Sidebar history
draw_card(draw2, 20, 90, 260, 760, fill='#ffffff')
draw2.text((40, 110), "搜索历史", fill='#333', font=font_large)
draw2.text((220, 115), "清空", fill='#667eea', font=font_small)

history = [
    ("打印机安装", "2026-06-01 13:46"),
    ("入职流程", "2026-06-01 13:42"),
    ("报销制度", "2026-06-01 13:38"),
]
y = 155
for q, time in history:
    draw2.text((40, y), q, fill='#667eea', font=font)
    draw2.text((40, y+22), time, fill='#aaa', font=font_small)
    y += 55

# Main chat area
draw_card(draw2, 300, 90, 960, 760, fill='#ffffff')

messages = [
    ("user", "打印机安装时遇到端口冲突怎么办？"),
    ("ai", "根据 IT运维手册 中的相关内容..."),
]
y = 120
for sender, text in messages:
    if sender == "user":
        lines = textwrap.wrap(text, width=50)
        h = len(lines) * 26 + 20
        draw2.rounded_rectangle([1160 - 280, y, 1240, y + h], radius=16, fill='#667eea')
        for i, line in enumerate(lines):
            draw2.text((1160 - 270, y + 12 + i*26), line, fill='#ffffff', font=font)
        y += h + 20
    else:
        lines = textwrap.wrap(text, width=60)
        h = len(lines) * 24 + 30
        draw2.rounded_rectangle([320, y, 900, y + h], radius=16, fill='#ffffff', outline='#e0e0e0')
        for i, line in enumerate(lines):
            draw2.text((330, y + 12 + i*24), line, fill='#333', font=font)
        y += h + 30

y += 20
draw2.text((320, y), "参考来源: IT运维手册.txt (相似度 1.0) | test_doc.txt (相似度 0.72)", fill='#888', font=font_small)

y += 40
draw2.text((320, y), "检索范围:", fill='#666', font=font)
draw2.rounded_rectangle([400, y-4, 500, y+24], radius=6, fill='#ffffff', outline='#667eea')
draw2.text((410, y), "IT运维 ▼", fill='#667eea', font=font)

y += 50
draw2.rounded_rectangle([320, y, 1180, y+50], radius=12, fill='#ffffff', outline='#667eea')
draw2.text((340, y+14), "输入问题...", fill='#aaa', font=font)
draw2.rounded_rectangle([1200, y, 1250, y+50], radius=12, fill='#667eea')
draw2.text((1210, y+14), "发送", fill='#ffffff', font=font)

im2.save('/root/.openclaw/workspace/ai-kb-test/screenshots/08-chat-history.png')
print("Saved 08-chat-history.png")

print("Done! Generated 2 new screenshots.")
