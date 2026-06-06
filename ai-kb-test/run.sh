#!/bin/bash
cd /root/.openclaw/workspace/ai-kb-test/backend
source ../venv/bin/activate
python main.py > /tmp/aikb.log 2>&1 &
echo $!