#!/usr/bin/env python3
"""
CLAW Node Worker - Replit Edition (Lightweight)
512MB RAM optimized. No LLM. HTTP tasks only.
"""
import os
import json
import time
import requests
from datetime import datetime
from threading import Thread

REPO = 'a1314522/claw-seed-test'
GH_TOKEN = os.environ.get('GH_TOKEN', '')
NODE_ID = 'replit-1'
HEADERS = {'Authorization': f'token {GH_TOKEN}', 'Accept': 'application/vnd.github.v3+json'}

def log(msg):
    print(f'[{datetime.now().isoformat()}] {msg}', flush=True)

def process_task(issue):
    issue_num = issue['number']
    log(f'Processing #{issue_num}: {issue["title"]}')
    
    try:
        task = json.loads(issue['body'])
    except:
        task = {'type': 'search', 'params': {'query': issue['title']}, 'task_id': f't-{issue_num}'}
    
    task_type = task.get('type', '')
    params = task.get('params', {})
    
    if task_type == 'search':
        query = params.get('query', '')
        try:
            # Simple web search via DuckDuckGo or direct fetch
            r = requests.get(f'https://html.duckduckgo.com/html/?q={requests.utils.quote(query)}', timeout=30)
            result = {'query': query, 'status': r.status_code, 'size': len(r.text), 'node': NODE_ID, 'timestamp': datetime.now().isoformat()}
        except Exception as e:
            result = {'error': str(e), 'node': NODE_ID, 'timestamp': datetime.now().isoformat()}
    elif task_type == 'sync':
        url = params.get('url', '')
        try:
            r = requests.get(url, timeout=30)
            result = {'url': url, 'status': r.status_code, 'size': len(r.content), 'node': NODE_ID, 'timestamp': datetime.now().isoformat()}
        except Exception as e:
            result = {'error': str(e), 'node': NODE_ID, 'timestamp': datetime.now().isoformat()}
    elif task_type == 'ping':
        result = {'status': 'alive', 'node': NODE_ID, 'timestamp': datetime.now().isoformat()}
    else:
        result = {'error': f'unknown type: {task_type}', 'node': NODE_ID}
    
    # Write comment
    result_json = json.dumps(result, indent=2, ensure_ascii=False)
    requests.post(f'https://api.github.com/repos/{REPO}/issues/{issue_num}/comments', headers=HEADERS, json={'body': f'## Result from {NODE_ID}\n\n```json\n{result_json}\n```'})
    
    # Close issue
    requests.patch(f'https://api.github.com/repos/{REPO}/issues/{issue_num}', headers=HEADERS, json={'state': 'closed', 'labels': ['status:done']})
    
    log(f'  Completed #{issue_num}')
    return result

def worker_loop():
    while True:
        try:
            # Get open task issues
            r = requests.get(f'https://api.github.com/repos/{REPO}/issues?state=open', headers=HEADERS, timeout=30)
            all_issues = r.json() if r.status_code == 200 else []
            if not isinstance(all_issues, list):
                all_issues = []
            
            task_labels = {'task:search', 'task:sync', 'task:ping'}
            tasks = []
            for issue in all_issues:
                labels = {l['name'] for l in issue.get('labels', [])}
                if labels & task_labels:
                    tasks.append(issue)
            
            log(f'Found {len(tasks)} tasks')
            
            for task in tasks:
                process_task(task)
                time.sleep(2)
            
        except Exception as e:
            log(f'Error: {e}')
        
        time.sleep(30)

if __name__ == '__main__':
    log(f'CLAW Node {NODE_ID} starting...')
    if not GH_TOKEN:
        log('ERROR: GH_TOKEN not set')
        exit(1)
    
    # Start worker
    t = Thread(target=worker_loop)
    t.daemon = True
    t.start()
    
    # Keep alive HTTP server (for health checks)
    from flask import Flask
    app = Flask(__name__)
    
    @app.route('/')
    def health():
        return json.dumps({'status': 'alive', 'node': NODE_ID, 'timestamp': datetime.now().isoformat()})
    
    @app.route('/tasks')
    def tasks():
        r = requests.get(f'https://api.github.com/repos/{REPO}/issues?state=open', headers=HEADERS, timeout=30)
        return r.text if r.status_code == 200 else '{}'
    
    port = int(os.environ.get('PORT', 8080))
    log(f'Serving on port {port}')
    app.run(host='0.0.0.0', port=port)
