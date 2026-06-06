from PIL import Image, ImageDraw, ImageFont
import os

def get_font(size=16):
    for name, idx in [
        ('/usr/share/fonts/opentype/noto/NotoSerifCJK-Regular.ttc', 0),
        ('/usr/share/fonts/truetype/noto/NotoSansCJK-Regular.ttc', 0),
        ('WenQuanYi Micro Hei', None),
        ('DejaVuSans', None),
    ]:
        try:
            if idx is not None:
                return ImageFont.truetype(name, size, index=idx)
            return ImageFont.truetype(name, size)
        except:
            pass
    return ImageFont.load_default()

def draw_rounded_rect(draw, xy, radius, fill, outline=None, width=1):
    x1, y1, x2, y2 = xy
    draw.rounded_rectangle([x1, y1, x2, y2], radius=radius, fill=fill, outline=outline, width=width)

def make_login_screenshot():
    w, h = 960, 700
    img = Image.new('RGB', (w, h), '#667eea')
    draw = ImageDraw.Draw(img)
    for y in range(h):
        r = int(102 + (118-102)*y/h)
        g = int(126 + (75-126)*y/h)
        b = int(234 + (162-234)*y/h)
        draw.line([(0,y),(w,y)], fill=(r,g,b))
    card_x, card_y = 280, 160
    card_w, card_h = 400, 380
    draw_rounded_rect(draw, [card_x, card_y, card_x+card_w, card_y+card_h], 20, '#ffffff')
    draw.text((card_x+card_w//2, card_y+40), "AI 知识库", fill='#333333', font=get_font(28), anchor='mm')
    input_y = card_y + 100
    draw.text((card_x+40, input_y), "用户名", fill='#888888', font=get_font(14))
    draw_rounded_rect(draw, [card_x+40, input_y+22, card_x+card_w-40, input_y+60], 10, '#f8f9fa', '#e0e0e0')
    draw.text((card_x+55, input_y+30), "admin", fill='#333333', font=get_font(16))
    input_y += 70
    draw.text((card_x+40, input_y), "密码", fill='#888888', font=get_font(14))
    draw_rounded_rect(draw, [card_x+40, input_y+22, card_x+card_w-40, input_y+60], 10, '#f8f9fa', '#e0e0e0')
    draw.text((card_x+55, input_y+30), "••••••••", fill='#333333', font=get_font(16))
    btn_y = card_y + 260
    draw_rounded_rect(draw, [card_x+40, btn_y, card_x+card_w-40, btn_y+48], 10, '#667eea')
    draw.text((card_x+card_w//2, btn_y+24), "登录", fill='#ffffff', font=get_font(16), anchor='mm')
    draw.text((card_x+card_w//2, card_y+330), "测试账号: admin / admin123", fill='#999999', font=get_font(13), anchor='mm')
    return img

def make_chat_screenshot():
    w, h = 960, 700
    img = Image.new('RGB', (w, h), '#f5f5f5')
    draw = ImageDraw.Draw(img)
    for y in range(70):
        r = int(102 + (118-102)*y/70)
        g = int(126 + (75-126)*y/70)
        b = int(234 + (162-234)*y/70)
        draw.line([(0,y),(w,y)], fill=(r,g,b))
    draw.text((w//2, 30), "AI 知识库", fill='#ffffff', font=get_font(22), anchor='mm')
    draw.text((w//2, 52), "智能文档检索与问答系统", fill='#ffffff', font=get_font(13), anchor='mm')
    draw.text((320, 90), "智能问答", fill='#667eea', font=get_font(14))
    draw.text((440, 90), "文档管理", fill='#667eea', font=get_font(14))
    draw.text((560, 90), "退出", fill='#667eea', font=get_font(14))
    
    chat_y = 130
    chat_h = 420
    draw_rounded_rect(draw, [30, chat_y, w-30, chat_y+chat_h], 12, '#ffffff')
    
    # AI welcome
    bubble_y = chat_y + 20
    draw.ellipse([45, bubble_y, 75, bubble_y+30], fill='#667eea')
    draw.text((60, bubble_y+15), "AI", fill='#ffffff', font=get_font(12), anchor='mm')
    msg = "你好！我是 AI 知识库助手。\n\n我已学习4份文档，可以回答关于打印机、IT运维、\n入职流程和财务报销的问题。"
    lines = msg.split('\n')
    max_w = 0
    for line in lines:
        bbox = draw.textbbox((0,0), line, font=get_font(13))
        max_w = max(max_w, bbox[2]-bbox[0])
    bubble_h = len(lines)*20 + 20
    draw_rounded_rect(draw, [85, bubble_y, 85+max_w+30, bubble_y+bubble_h], 16, '#ffffff', '#e0e0e0')
    for i, line in enumerate(lines):
        draw.text((100, bubble_y+12+i*20), line, fill='#333333', font=get_font(13))
    
    # User question
    bubble_y += bubble_h + 20
    q_text = "新员工入职第一天需要做什么？"
    bbox = draw.textbbox((0,0), q_text, font=get_font(14))
    qw = bbox[2]-bbox[0]
    draw_rounded_rect(draw, [w-40-qw-30, bubble_y, w-40, bubble_y+36], 16, '#667eea')
    draw.text((w-55-qw//2, bubble_y+18), q_text, fill='#ffffff', font=get_font(14), anchor='mm')
    
    # AI answer with sources
    bubble_y += 50
    answer = "根据检索到的参考信息，回答如下：\n\n入职第一天需要完成：\n1. 到人事办公室（康桥2F）领取工牌和电脑\n2. IT部门配置域账号和邮箱\n3. 修改默认密码（Welcome@2024）\n4. 访问内网门户：portal.company.local\n\n【参考来源】\n文档: 员工入职指南 (相似度: 1.0)\n文档: IT运维手册 (相似度: 0.48)\n\n（实际部署后，由 Ollama 生成自然语言回答）"
    lines = answer.split('\n')
    max_w = 0
    for line in lines:
        bbox = draw.textbbox((0,0), line, font=get_font(13))
        max_w = max(max_w, bbox[2]-bbox[0])
    bubble_h = len(lines)*18 + 25
    draw_rounded_rect(draw, [85, bubble_y, 85+max_w+30, bubble_y+bubble_h], 16, '#ffffff', '#e0e0e0')
    draw.rectangle([85+max_w-100, bubble_y, 85+max_w+20, bubble_y+18], fill='#ff9800')
    draw.text((85+max_w-40, bubble_y+9), "多文档检索", fill='#ffffff', font=get_font(10), anchor='mm')
    for i, line in enumerate(lines):
        draw.text((100, bubble_y+22+i*18), line, fill='#333333', font=get_font(13))
    
    # Input
    input_y = chat_y + chat_h + 20
    draw_rounded_rect(draw, [30, input_y, w-130, input_y+50], 12, '#ffffff', '#e0e0e0')
    draw.text((50, input_y+25), "输入问题...", fill='#aaaaaa', font=get_font(13), anchor='lm')
    draw_rounded_rect(draw, [w-120, input_y, w-30, input_y+50], 12, '#667eea')
    draw.text((w-75, input_y+25), "发送", fill='#ffffff', font=get_font(14), anchor='mm')
    return img

def make_docs_screenshot():
    w, h = 960, 700
    img = Image.new('RGB', (w, h), '#f5f5f5')
    draw = ImageDraw.Draw(img)
    for y in range(70):
        r = int(102 + (118-102)*y/70)
        g = int(126 + (75-126)*y/70)
        b = int(234 + (162-234)*y/70)
        draw.line([(0,y),(w,y)], fill=(r,g,b))
    draw.text((w//2, 30), "AI 知识库", fill='#ffffff', font=get_font(22), anchor='mm')
    draw.text((w//2, 52), "智能文档检索与问答系统", fill='#ffffff', font=get_font(13), anchor='mm')
    draw.text((320, 90), "智能问答", fill='#667eea', font=get_font(14))
    draw.text((440, 90), "文档管理", fill='#667eea', font=get_font(14))
    draw.text((560, 90), "退出", fill='#667eea', font=get_font(14))
    
    uz_y = 130
    draw_rounded_rect(draw, [30, uz_y, w-30, uz_y+100], 12, '#ffffff', '#c0c0c0', 2)
    draw.text((w//2, uz_y+35), "📁 点击上传或拖拽文件到此处", fill='#888888', font=get_font(15), anchor='mm')
    draw.text((w//2, uz_y+65), "支持 PDF, Word, Excel, TXT, Markdown", fill='#aaaaaa', font=get_font(12), anchor='mm')
    
    list_y = uz_y + 120
    draw.text((40, list_y), "已上传文档 (4)", fill='#333333', font=get_font(16))
    
    docs = [
        ("test_doc.txt", "text | 2.1 KB | 2025-06-01 | 3 分块", "完成"),
        ("IT运维手册.txt", "text | 1.5 KB | 2025-06-01 | 2 分块", "完成"),
        ("员工入职指南.txt", "text | 1.4 KB | 2025-06-01 | 2 分块", "完成"),
        ("财务报销制度.txt", "text | 1.3 KB | 2025-06-01 | 2 分块", "完成"),
    ]
    item_y = list_y + 35
    for name, meta, status in docs:
        draw_rounded_rect(draw, [30, item_y, w-30, item_y+60], 8, '#ffffff')
        draw.text((50, item_y+18), name, fill='#333333', font=get_font(15))
        draw.text((50, item_y+38), meta, fill='#888888', font=get_font(11))
        color = '#d4edda'
        text_color = '#155724'
        draw.rounded_rectangle([w-100, item_y+15, w-40, item_y+40], radius=12, fill=color)
        draw.text((w-70, item_y+27), status, fill=text_color, font=get_font(11), anchor='mm')
        item_y += 70
    return img

def make_architecture_screenshot():
    w, h = 960, 500
    img = Image.new('RGB', (w, h), '#1a1a2e')
    draw = ImageDraw.Draw(img)
    draw.text((w//2, 30), "AI 知识库系统架构", fill='#ffffff', font=get_font(24), anchor='mm')
    boxes = [
        (80, 100, 220, 160, "用户交互层", "React/Vue/HTML", "前端界面", '#e94560'),
        (380, 100, 540, 160, "API 网关层", "FastAPI + JWT", "REST API", '#0f3460'),
        (680, 100, 840, 160, "知识检索层", "TF-IDF + 向量", "语义检索", '#533483'),
        (230, 280, 390, 340, "文档处理层", "分块 + 解析", "PDF/Word/Excel", '#16c79a'),
        (530, 280, 690, 340, "大模型层", "Ollama / Qwen", "本地推理", '#e94560'),
    ]
    for x1, y1, x2, y2, title, sub, desc, color in boxes:
        draw_rounded_rect(draw, [x1, y1, x2, y2], 12, color)
        draw.text((x1+10, y1+15), title, fill='#ffffff', font=get_font(16))
        draw.text((x1+10, y1+38), sub, fill='#ffffff', font=get_font(12))
        draw.text((x1+10, y1+58), desc, fill='#ffffff', font=get_font(11))
    arrow_color = '#4a4e69'
    draw.line([(220, 130), (380, 130)], fill=arrow_color, width=2)
    draw.polygon([(375, 125), (385, 130), (375, 135)], fill=arrow_color)
    draw.line([(540, 130), (680, 130)], fill=arrow_color, width=2)
    draw.polygon([(675, 125), (685, 130), (675, 135)], fill=arrow_color)
    draw.line([(460, 160), (460, 280)], fill=arrow_color, width=2)
    draw.polygon([(455, 275), (460, 285), (465, 275)], fill=arrow_color)
    draw.line([(460, 310), (530, 310)], fill=arrow_color, width=2)
    draw.polygon([(525, 305), (535, 310), (525, 315)], fill=arrow_color)
    return img

os.makedirs('/root/.openclaw/workspace/ai-kb-test/screenshots', exist_ok=True)
make_login_screenshot().save('/root/.openclaw/workspace/ai-kb-test/screenshots/01-login.png')
make_chat_screenshot().save('/root/.openclaw/workspace/ai-kb-test/screenshots/02-chat.png')
make_docs_screenshot().save('/root/.openclaw/workspace/ai-kb-test/screenshots/03-docs.png')
make_architecture_screenshot().save('/root/.openclaw/workspace/ai-kb-test/screenshots/04-arch.png')
print("Screenshots generated")
