from __future__ import annotations
from typing import List, Dict
import pandas as pd

from processor import TitleProcessor


class RedditProcessor:
    def __init__(self, spacy_model):
        self.tp = TitleProcessor(spacy_model)

    def process(self, rows: List[Dict]) -> List[Dict]:
        if not rows:
            return []
        df = pd.DataFrame(rows)
        df["title"] = df["text"]
        df = self.tp.filter_titles(df)
        keep = ["id", "text", "up", "ts", "ratio", "url", "sub"]
        return df[keep].to_dict("records")
