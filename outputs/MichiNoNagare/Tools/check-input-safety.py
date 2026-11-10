#!/usr/bin/env python3
"""Release input must not expose destructive development shortcuts."""
from pathlib import Path
import re
import unittest

ROOT = Path(__file__).resolve().parents[1]

class ReleaseInputSafety(unittest.TestCase):
    def test_network_input_has_no_development_shortcuts(self):
        source = (ROOT / "Assets/Michi/Scripts/Runtime/NetworkGame.cs").read_text()
        self.assertNotRegex(source, r"KeyCode\.F[23]\b",
                            "F2/F3 must not replace or erase a player's network")

    def test_runtime_does_not_expose_preset_loader(self):
        source = (ROOT / "Assets/Michi/Scripts/Runtime/NetworkGame.cs").read_text()
        self.assertNotRegex(source, r"public\s+void\s+Load\s*\(",
                            "Unconfirmed preset loading must not be a runtime API")

if __name__ == "__main__":
    unittest.main()
