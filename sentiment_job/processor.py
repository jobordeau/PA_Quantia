from __future__ import annotations
import pandas as pd
from spacy.language import Language
from spacy.lang.en import English as SpacyEnglish
from spacy_langdetect import LanguageDetector

# ------------------------------------------------------------------
def _ensure_lang_detector(nlp: SpacyEnglish):
    """Ajoute 'language_detector' une seule fois."""
    if "language_detector" not in nlp.pipe_names:
        Language.factory("language_detector",
                         func=lambda nlp, name: LanguageDetector())
        nlp.add_pipe("language_detector", last=True)

class TitleProcessor:
    def __init__(self, nlp: SpacyEnglish):
        _ensure_lang_detector(nlp)
        self.nlp = nlp

    
    def _has_verb(self, sent: str) -> bool:
        return any(t.pos_ == "VERB" for t in self.nlp(sent))

    def _is_english(self, sent: str, min_conf=0.8) -> bool:
        doc = self.nlp(sent, disable=["ner", "tok2vec"])
        return (doc._.language["language"] == "en" and
                doc._.language["score"] >= min_conf)

    
    def filter_titles(self, df: pd.DataFrame,
                      text_col: str = "title",
                      min_words: int = 4) -> pd.DataFrame:
        df = df.loc[~df[text_col].str.contains(r"\?")]
        df = df.loc[df[text_col].str.split().str.len() >= min_words]
        df = df.loc[df[text_col].apply(self._has_verb)]
        df = df.loc[df[text_col].apply(self._is_english)]
        return df.reset_index(drop=True)
