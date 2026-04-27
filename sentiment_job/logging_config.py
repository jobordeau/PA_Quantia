from __future__ import annotations
from pathlib import Path
import logging, os, sys
from logging.handlers import RotatingFileHandler

# ───────────────────────────────────────────────────────
LOG_PATH = Path(__file__).with_name("sentiment.log")
LOG_PATH.parent.mkdir(exist_ok=True)

# --- couleurs basiques ---------------------------------------------------
RESET = "\033[0m"
COLORS = dict(INFO="\033[32m", WARNING="\033[33m",
              ERROR="\033[31m", CRITICAL="\033[41m")

class ColorFormatter(logging.Formatter):
    def format(self, record):
        base = super().format(record)
        if sys.stderr.isatty() and record.levelname in COLORS:
            return f"{COLORS[record.levelname]}{base}{RESET}"
        return base

# -------------------------------------------------------------------------
def setup_logging(level: int | None = None) -> None:

    level = level or getattr(logging, os.getenv("LOG_LEVEL", "INFO").upper(), logging.INFO)

    fmt     = "%(asctime)s | %(levelname)-8s | %(name)s | %(message)s"
    datefmt = "%Y-%m-%d %H:%M:%S"

    # Console
    con = logging.StreamHandler()
    con.setFormatter(ColorFormatter(fmt, datefmt))

    # Fichier tournant
    rot = RotatingFileHandler(LOG_PATH, maxBytes=5*1024*1024, backupCount=5,
                              encoding="utf-8")
    rot.setFormatter(logging.Formatter(fmt, datefmt))

    logging.basicConfig(level=level, handlers=[con, rot], force=True)
