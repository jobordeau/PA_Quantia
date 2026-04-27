"""
Sentiment 0‑1 (0 = fear, 1 = greed) – CryptoBERT only
"""
from __future__ import annotations
import logging, os, torch, numpy as np
from transformers import AutoTokenizer, AutoModelForSequenceClassification, pipeline

log = logging.getLogger(__name__)
MODEL_ID = "ElKulako/cryptobert"

device = 0 if torch.cuda.is_available() else -1
log.info("Chargement CryptoBERT sur %s…", "CUDA" if device==0 else "CPU")

tok = AutoTokenizer.from_pretrained(MODEL_ID)
mdl = AutoModelForSequenceClassification.from_pretrained(MODEL_ID)
pipe = pipeline("sentiment-analysis", model=mdl, tokenizer=tok,
                device=device, truncation=True, max_length=256)

def _score(res: dict) -> float:
    p = res["score"]
    return p if res["label"].lower().startswith("pos") else 1 - p

def score_text(text: str) -> float:
    res = pipe(text, truncation=True, max_length=256)[0]
    return round(float(_score(res)), 3)
