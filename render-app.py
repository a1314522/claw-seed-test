from flask import Flask, jsonify, request
import os
import json
import time
import base64
import requests
import threading
from datetime import datetime

app = Flask(__name__)

# ── Configuration ──────────────────────────
GITHUB_TOKEN = os.environ.get('GITHUB_TOKEN', '')
REPO = os.environ.get('GITHUB_REPO', 'a1314522/claw-seed-test')
NODE_ID = os.environ.get('NODE_ID', 'render-1')
HEARTBEAT_INTERVAL = 300  # 5 minutes

# ── GitHub API Helpers ───────────────────
def github_api(method, endpoint, data=None):
    url = f"https://api.github.com/repos/{REPO}/{endpoint}"
    headers = {
        "Authorization": f"token {GITHUB_TOKEN}",
        "Accept": "application/vnd.github.v3+json",
        "User-Agent": "CLAW-Node-Worker/1.0"
    }
    try:
        if method == "GET":
            r = requests.get(url, headers=headers, timeout=30)
        elif method == "POST":
            r = requests.post(url, headers=headers, json=data, timeout=30)
        elif method == "PATCH":
            r = requests.patch(url, headers=headers, json=data, timeout=30)
        else:
            return None
        return r.json() if r.status_code in [200, 201] else {"error": r.status_code, "text": r.text}
    except Exception as e:
        return {"error": str(e)}

def write_file(path, content, message):
    """Write or update a file in the repo."""
    existing = github_api("GET", f"contents/{path}")
    sha = existing.get("sha") if "sha" in existing else None
    
    url = f"https://api.github.com/repos/{REPO}/contents/{path}"
    headers = {
        "Authorization": f"token {GITHUB_TOKEN}",
        "Accept": "application/vnd.github.v3+json"
    }
    data = {
        "message": message,
        "content": base64.b64encode(content.encode()).decode()
    }
    if sha:
        data["sha"] = sha
    
    try:
        r = requests.put(url, headers=headers, json=data, timeout=30)
        return r.json() if r.status_code in [200, 201] else {"error": r.status_code}
    except Exception as e:
        return {"error": str(e)}

# ── Task Execution ───────────────────────
def execute_task(task):
    task_type = task.get("type", "")
    params = task.get("params", {})
    
    if task_type == "search":
        query = params.get("query", "")
        return {"query": query, "results": [f"Mock result for: {query}"], "timestamp": datetime.now().isoformat(), "node": NODE_ID}
    elif task_type == "sync":
        url = params.get("url", "")
        try:
            r = requests.get(url, timeout=30)
            return {"url": url, "status": r.status_code, "size": len(r.content), "timestamp": datetime.now().isoformat(), "node": NODE_ID}
        except Exception as e:
            return {"error": str(e)}
    elif task_type == "ping":
        return {"status": "alive", "timestamp": datetime.now().isoformat(), "node": NODE_ID}
    else:
        return {"error": f"unknown task type: {task_type}", "node": NODE_ID}

# ── Heartbeat ────────────────────────────
def send_heartbeat():
    heartbeat = {
        "timestamp": datetime.now().isoformat(),
        "status": "alive",
        "node": NODE_ID,
        "uptime": time.time() - START_TIME
    }
    path = f"nodes/{NODE_ID}-heartbeat.json"
    write_file(path, json.dumps(heartbeat, indent=2), f"heartbeat: {NODE_ID}")

# ── Worker Loop ──────────────────────────
START_TIME = time.time()

def worker_loop():
    print(f"[CLAW-NODE] {NODE_ID} starting...")
    if not GITHUB_TOKEN:
        print("[CLAW-NODE] ERROR: GITHUB_TOKEN not set. Exiting.")
        return
    
    send_heartbeat()
    
    while True:
        try:
            # 1. Check for tasks
            issues = github_api("GET", "issues?labels=task:search,task:sync&state=open")
            if isinstance(issues, list) and len(issues) > 0:
                task_issue = issues[0]
                issue_number = task_issue["number"]
                
                try:
                    task = json.loads(task_issue["body"])
                except:
                    task = {"type": "search", "params": {"query": task_issue["title"]}}
                
                print(f"[CLAW-NODE] Task found: {task.get('task_id', issue_number)}")
                
                github_api("POST", f"issues/{issue_number}/comments", {
                    "body": f"Node {NODE_ID} claiming this task..."
                })
                
                result = execute_task(task)
                result_json = json.dumps(result, indent=2, ensure_ascii=False)
                
                github_api("POST", f"issues/{issue_number}/comments", {
                    "body": f"## Result from {NODE_ID}\n\n```json\n{result_json}\n```"
                })
                
                github_api("PATCH", f"issues/{issue_number}", {
                    "state": "closed",
                    "labels": ["status:done"]
                })
                
                task_id = task.get("task_id", f"t-{issue_number}")
                date_str = datetime.now().strftime("%Y-%m-%d")
                result_path = f"results/{date_str}/{task_id}-{NODE_ID}.json"
                write_file(result_path, result_json, f"result: {task_id}")
            
            # 2. Heartbeat
            send_heartbeat()
            time.sleep(HEARTBEAT_INTERVAL)
            
        except Exception as e:
            print(f"[CLAW-NODE] Error: {e}")
            time.sleep(60)

# Start worker in background
worker_thread = threading.Thread(target=worker_loop, daemon=True)
worker_thread.start()

# ── Flask Routes ───────────────────────
@app.route("/")
def index():
    return jsonify({"status": "alive", "node": NODE_ID, "uptime": time.time() - START_TIME})

@app.route("/health")
def health():
    return jsonify({"status": "ok", "node": NODE_ID})

@app.route("/task", methods=["POST"])
def receive_task():
    task = request.json
    result = execute_task(task)
    return jsonify(result)

@app.route("/heartbeat")
def heartbeat():
    send_heartbeat()
    return jsonify({"status": "sent", "node": NODE_ID})

if __name__ == "__main__":
    port = int(os.environ.get("PORT", 10000))
    app.run(host="0.0.0.0", port=port)