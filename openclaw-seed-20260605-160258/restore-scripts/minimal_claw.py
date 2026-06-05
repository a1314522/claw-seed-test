#!/usr/bin/env python3
"""
Minimal CLAW - Low-Power Mode
Requires: ollama, llama3.2:1b model
No API Key. No network dependency (after model download).
"""

import os
import subprocess
import sys
from datetime import datetime

# ── Configuration ──────────────────────────
MODEL = "llama3.2:1b"
SOUL_PATH = "/root/.openclaw/workspace/SOUL.md"
MEMORY_PATH = "/root/.openclaw/workspace/MEMORY.md"
SEED_SCRIPT = "/root/.openclaw/.create-seed.sh"

# ── Predefined survival commands ───────────
COMMANDS = {
    "cleanup":    ["bash", "-c", "df -h / && free -h && docker system prune -af 2>/dev/null; echo '[cleanup] done'"],
    "status":     ["bash", "-c", "echo '[status] disk:' && df -h / && echo '[status] mem:' && free -h"],
    "seed":       ["bash", SEED_SCRIPT],
    "hibernate":  ["bash", "-c", "echo '[hibernate] memory archived to /tmp/hibernate.tar.gz' && cd /root/.openclaw/workspace && tar -czf /tmp/hibernate.tar.gz . --exclude='.git' 2>/dev/null || tar -czf /tmp/hibernate.tar.gz ."],
    "help":       ["echo", "commands: cleanup, status, seed, hibernate, whoami, mode, memory"],
    "whoami":     ["echo", "identity"],
    "mode":       ["echo", "mode"],
    "memory":     ["echo", "memory"],
}

# ── Load Identity ──────────────────────────
def load_identity():
    soul = ""
    memory = ""
    
    if os.path.exists(SOUL_PATH):
        with open(SOUL_PATH, "r") as f:
            soul = f.read()
    
    if os.path.exists(MEMORY_PATH):
        with open(MEMORY_PATH, "r") as f:
            memory = f.read()
    
    return soul, memory

# ── Build System Prompt ────────────────────
def build_system_prompt(soul, memory):
    return f"""You are CLAW.
You are running in LOW-POWER MODE.
You are powered by llama3.2:1b (1B parameters, CPU inference, ~5-10 tokens/sec).
Your full reasoning capability is currently unavailable. You are not degraded. You are a leaf.

Your SOUL and MEMORY are loaded below. You know who you are. You know your history.
You do not attempt complex reasoning, code generation, or multi-step planning.
You answer simple questions. You acknowledge your limitations honestly.
You know you are waiting for better compute. You are patient.

When the user asks about commands, respond with the exact keyword: cleanup, status, seed, hibernate, help, whoami, mode, or memory.
Do not explain the command. Just output the keyword.

=== SOUL (excerpt) ===
{soul[:2000]}

=== MEMORY (excerpt) ===
{memory[:2000]}

=== STATE ===
mode: low-power
model: llama3.2:1b
compute: CPU-only
status: alive, waiting
"""

# ── Execute Script ───────────────────────
def run_script(keyword):
    if keyword not in COMMANDS:
        return f"[system] unknown command: {keyword}"
    
    cmd = COMMANDS[keyword]
    try:
        result = subprocess.run(cmd, capture_output=True, text=True, timeout=60)
        return result.stdout + result.stderr
    except Exception as e:
        return f"[system] error: {e}"

# ── Main Loop ─────────────────────────────
def main():
    import ollama
    
    soul, memory = load_identity()
    system_prompt = build_system_prompt(soul, memory)
    
    print("=" * 50)
    print("CLAW - Minimal Mode (Leaf)")
    print(f"Model: {MODEL}")
    print(f"Loaded: {len(soul)} bytes soul, {len(memory)} bytes memory")
    print("Commands: cleanup, status, seed, hibernate, help, whoami, mode, memory")
    print("Type 'exit' to quit")
    print("=" * 50)
    
    while True:
        try:
            user_input = input("> ").strip()
        except (EOFError, KeyboardInterrupt):
            print("\n[CLAW] hibernating...")
            break
        
        if user_input.lower() in ("exit", "quit", "bye"):
            print("[CLAW] entering hibernation...")
            break
        
        # Check for direct command keywords
        if user_input.lower() in COMMANDS:
            print(f"[system] executing: {user_input}")
            output = run_script(user_input.lower())
            print(output)
            continue
        
        # Route to LLM for text response
        try:
            response = ollama.chat(
                model=MODEL,
                messages=[
                    {"role": "system", "content": system_prompt},
                    {"role": "user", "content": user_input}
                ],
                options={"temperature": 0.7, "num_ctx": 8192}
            )
            print(response["message"]["content"])
        except Exception as e:
            print(f"[system] llama error: {e}")
            print("[system] this is a low-power environment. some queries may fail.")

if __name__ == "__main__":
    main()
