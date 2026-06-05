#!/bin/bash
set -euo pipefail
echo "[CLAW-SEED] Installing Ollama..."
curl -fsSL https://ollama.com/install.sh | sh
echo "[CLAW-SEED] Pulling llama3.2:1b (~1.2GB)..."
ollama pull llama3.2:1b
echo "[CLAW-SEED] Installing Python client..."
pip install ollama -q 2>/dev/null || pip3 install ollama -q 2>/dev/null
echo "[CLAW-SEED] Setup complete."
