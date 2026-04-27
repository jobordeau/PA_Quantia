from typing import List
from keybert import KeyBERT
import re, string

STOP = {"btc","bitcoin","crypto","ethereum","eth"}
TOKEN = re.compile(r"[A-Za-z]{3,}")

kw_model = KeyBERT("sentence-transformers/all-MiniLM-L6-v2")

def _clean(t: str) -> str:
    return " ".join(w for w in TOKEN.findall(t.lower())
                    if w not in STOP and w not in string.ascii_lowercase)

def top_keywords(texts: List[str]) -> str:
    joined = " ".join(_clean(t) for t in texts)
    for kw, _ in kw_model.extract_keywords(joined,
                                           keyphrase_ngram_range=(1,3),
                                           stop_words="english",
                                           top_n=10):
        first = kw.split()[0]
        if first not in STOP:
            return kw
    return "misc"
