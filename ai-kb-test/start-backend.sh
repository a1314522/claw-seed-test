cd /root/.openclaw/workspace/ai-kb-test/backend
source ../venv/bin/activate
python -c "import sys; sys.path.insert(0,'.')" -m uvicorn main:app --host 0.0.0.0 --port 8000 --reload 2>&1 | tee /tmp/ai-kb.log