import sys
sys.path.insert(0, '/root/.openclaw/workspace/ai-kb-test/backend')

from main import app
from fastapi.testclient import TestClient

try:
    client = TestClient(app)
    r = client.post('/api/auth/login', json={'username':'admin','password':'admin123'})
    print('Status:', r.status_code)
    print('Response:', r.text[:500])
except Exception as e:
    print('Error:', e)
    import traceback
    traceback.print_exc()
