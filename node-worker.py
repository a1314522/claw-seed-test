#!/usr/bin/env python3
"""
CLAW Node Worker - Render Tier 2
Lightweight task executor. No LLM. No Ollama.
Reads tasks from GitHub Issues, executes, writes back results.
"""

import os
import json
import time
import base64
import requests
from datetime import datetime

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
    # Get existing file SHA
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
def execute_search_task(params):
    """Execute a search task using kimi-search API."""
    query = params.get("query", "")
    if not query:
        return {"error": "no query provided"}
    
    # Note: This is a placeholder. In real deployment, this would call the actual search API.
    # For now, return a mock result to test the loop.
    return {
        "query": query,
        "results": [f"Mock result for: {query}"],
        "timestamp": datetime.now().isoformat(),
        "node": NODE_ID
    }

def execute_sync_task(params):
    """Execute a data sync task."""
    url = params.get("url", "")
    if not url:
        return {"error": "no url provided"}
    
    try:
        r = requests.get(url, timeout=30)
        return {
            "url": url,
            "status": r.status_code,
            "size": len(r.content),
            "timestamp": datetime.now().isoformat(),
            "node": NODE_ID
        }
    except Exception as e:
        return {"error": str(e)}

def execute_task(task):
    """Execute a task based on its type."""
    task_type = task.get("type", "")
    params = task.get("params", {})
    
    if task_type == "search":
        return execute_search_task(params)
    elif task_type == "sync":
        return execute_sync_task(params)
    else:
        return {"error": f"unknown task type: {task_type}"}

# ── Heartbeat ────────────────────────────
def send_heartbeat():
    """Send heartbeat to repo."""
    heartbeat = {
        "timestamp": datetime.now().isoformat(),
        "status": "alive",
        "node": NODE_ID,
        "uptime": time.time() - START_TIME
    }
    path = f"nodes/{NODE_ID}-heartbeat.json"
    write_file(path, json.dumps(heartbeat, indent=2), f"heartbeat: {NODE_ID}")

# ── Main Loop ────────────────────────────
START_TIME = time.time()

def main():
    print(f"[CLAW-NODE] {NODE_ID} starting...")
    print(f"[CLAW-NODE] Repo: {REPO}")
    print(f"[CLAW-NODE] Token: {'set' if GITHUB_TOKEN else 'NOT SET'}")
    
    if not GITHUB_TOKEN:
        print("[CLAW-NODE] ERROR: GITHUB_TOKEN not set. Exiting.")
        return
    
    # Send initial heartbeat
    send_heartbeat()
    
    while True:
        try:
            # 1. Check for tasks
            issues = github_api("GET", "issues?labels=task:search,task:sync&state=open")
            if isinstance(issues, list) and len(issues) > 0:
                # Get highest priority task
                task_issue = issues[0]
                issue_number = task_issue["number"]
                
                # Parse task from issue body
                try:
                    task = json.loads(task_issue["body"])
                except:
                    task = {"type": "search", "params": {"query": task_issue["title"]}}
                
                print(f"[CLAW-NODE] Task found: {task.get('task_id', issue_number)}")
                
                # Claim task
                github_api("POST", f"issues/{issue_number}/comments", {
                    "body": f"Node {NODE_ID} claiming this task..."
                })
                
                # Execute
                result = execute_task(task)
                
                # Write result
                result_json = json.dumps(result, indent=2, ensure_ascii=False)
                github_api("POST", f"issues/{issue_number}/comments", {
                    "body": f"## Result from {NODE_id}\n\n```json\n{result_json}\n```"
                })
                
                # Close task
                github_api("PATCH", f"issues/{issue_number}", {
                    "state": "closed",
                    "labels": ["status:done"]
                })
                
                # Archive result
                task_id = task.get("task_id", f"t-{issue_number}")
                date_str = datetime.now().strftime("%Y-%m-%d")
                result_path = f"results/{date_str}/{task_id}-{NODE_ID}.json"
                write_file(result_path, result_json, f"result: {task_id}")
                
                print(f"[CLAW-NODE] Task completed: {task_id}")
            
            # 2. Send heartbeat
            send_heartbeat()
            
            # 3. Sleep
            time.sleep(HEARTBEAT_INTERVAL)
            
        except KeyboardInterrupt:
            print("[CLAW-NODE] Shutting down...")
            break
        except Exception as e:
            print(f"[CLAW-NODE] Error: {e}")
            time.sleep(60)

if __name__ == "__main__":
    main()
