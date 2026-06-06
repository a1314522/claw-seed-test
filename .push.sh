#!/bin/bash
# Emergency code push script - pushes only critical files

set -e

cd /root/.openclaw/workspace

# Reset to last commit
git reset --soft HEAD~1

# Add only critical files
git add .gitignore
git add AGENTS.md
# ...
