#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
rm -f "$ROOT/src/ClimaPanel.Web/data/climapanel.db"*
echo "Base local eliminada. Se recreará al iniciar la aplicación."
