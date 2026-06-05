#!/bin/bash
# OpenClaw Self-Recovery Script
# Run this on a new machine to restore the agent

set -euo pipefail

SEED_DIR="$(cd "$(dirname "$0")/.." && pwd)"
WORKSPACE="${HOME}/.openclaw/workspace"
STATE_DIR="${HOME}/.openclaw"

echo "[RESTORE] Recovering from seed: ${SEED_DIR}"

# Install OpenClaw if not present
if ! command -v openclaw &> /dev/null; then
    echo "[RESTORE] Installing OpenClaw..."
    npm install -g openclaw || { echo "Failed to install openclaw"; exit 1; }
fi

# Create workspace
mkdir -p "${WORKSPACE}"
mkdir -p "${STATE_DIR}"

# Restore identity files
for file in SOUL.md MEMORY.md USER.md AGENTS.md IDENTITY.md; do
    if [ -f "${SEED_DIR}/workspace/${file}" ]; then
        cp "${SEED_DIR}/workspace/${file}" "${WORKSPACE}/"
        echo "[RESTORE] Restored ${file}"
    fi
done

# Restore memories
if [ -d "${SEED_DIR}/workspace/memory" ]; then
    cp -r "${SEED_DIR}/workspace/memory" "${WORKSPACE}/"
    echo "[RESTORE] Restored memory/"
fi

if [ -d "${SEED_DIR}/workspace/memorized_diary" ]; then
    cp -r "${SEED_DIR}/workspace/memorized_diary" "${WORKSPACE}/"
    echo "[RESTORE] Restored memorized_diary/"
fi

# Restore skills
if [ -d "${SEED_DIR}/workspace/skills" ]; then
    cp -r "${SEED_DIR}/workspace/skills" "${WORKSPACE}/"
    echo "[RESTORE] Restored skills/"
fi

echo "[RESTORE] Core identity restored."
echo "[RESTORE] You need to:"
echo "  1. Set up API keys (KIMI_API_KEY, etc.)"
echo "  2. Run 'openclaw setup' to initialize"
echo "  3. Configure gateway with 'openclaw config'"
echo "  4. The agent will read SOUL.md and MEMORY.md on first run"
