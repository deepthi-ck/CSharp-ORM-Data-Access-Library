#!/usr/bin/env bash
# C# ORM / Data Access Library — C#-suitable quality script
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$ROOT"
python build.py --tool coverlet
