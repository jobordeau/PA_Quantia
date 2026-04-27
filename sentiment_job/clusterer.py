from __future__ import annotations
import logging, re
from typing import Dict, List
import hdbscan, numpy as np
from sentence_transformers import SentenceTransformer

log = logging.getLogger(__name__)
_sbert = SentenceTransformer("sentence-transformers/all-MiniLM-L6-v2")

def _clean(t:str) -> str:
    t = re.sub(r"https?://\S+", " ", t)
    t = re.sub(r"\b(bitcoin|btc|eth|crypto|ethereum)\b", " ", t, flags=re.I)
    return re.sub(r"\s+", " ", t).strip().lower()

class Clusterer:
    def __init__(self, min_cluster_size:int|None=None):
        self.mcs = min_cluster_size

    def cluster(self, texts:List[str]) -> Dict[int, List[int]]:
        if not texts:
            return {}
        mcs = self.mcs or max(3, int(0.01*len(texts)))   # 1 % du volume
        log.info("Encodage SBERT… N=%d, min_cluster_size=%d", len(texts), mcs)
        emb = _sbert.encode([_clean(t) for t in texts], normalize_embeddings=True)
        cl = hdbscan.HDBSCAN(min_cluster_size=mcs, metric="euclidean").fit(emb)
        clusters:dict[int,list[int]]={}
        for idx,lab in enumerate(cl.labels_):
            clusters.setdefault(int(lab),[]).append(idx)
        clusters.pop(-1, None)
        log.info("→ %d clusters", len(clusters))
        return clusters
