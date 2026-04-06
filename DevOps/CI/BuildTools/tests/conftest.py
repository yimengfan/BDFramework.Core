from __future__ import annotations

import sys
from pathlib import Path


BUILD_TOOLS_ROOT = Path(__file__).resolve().parents[1]
if str(BUILD_TOOLS_ROOT) not in sys.path:
    sys.path.insert(0, str(BUILD_TOOLS_ROOT))