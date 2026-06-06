from config import LLM_MODEL

def build_prompt(question, context):
    if context:
        return f"""你是一个专业的企业知识助手。请基于以下参考信息回答用户问题。
如果参考信息不足以回答问题，请明确说明"根据现有资料无法回答"。

参考信息：
{context}

用户问题：{question}

请用中文回答，保持简洁准确："""
    else:
        return f"""你是一个专业的企业知识助手。请回答用户问题。

用户问题：{question}

请用中文回答，保持简洁准确："""

def generate_answer(question, context):
    if not context:
        return {"answer": "[测试模式]\n\n当前知识库中没有相关文档，请先上传文档。\n\n（实际部署后，此处会由本地大模型 Ollama 生成完整回答。）", "model": LLM_MODEL, "source": "mock"}
    
    # Mock answer based on context keywords
    lines = context.split('\n')
    summary = "根据检索到的参考信息，简要回答如下：\n\n"
    summary += context[:300] + "\n\n"
    summary += "（实际部署后，此处会由本地大模型 Ollama 根据完整上下文生成自然语言回答。）"
    return {"answer": summary, "model": LLM_MODEL, "source": "mock"}
