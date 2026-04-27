from __future__ import annotations

import datetime as dt
import logging
import os
import time
from typing import Any, Dict, List

import numpy as np
import psycopg2
import spacy
from psycopg2.extras import Json
from tqdm.auto import tqdm

from dotenv import load_dotenv
load_dotenv()

PG_DSN = os.environ["PG_CONN"]

from logging_config import setup_logging
from reddit_crawler import RedditCrawler
from reddit_processor import RedditProcessor
from sentiment_scorer import score_text
from clusterer import Clusterer
from keywords import top_keywords
from summarizer import summarize
from price_fetcher import fetch_price
from tech_indicators import market_composite
from utils import amplify

try:
    from pytrends.request import TrendReq
except ModuleNotFoundError:
    TrendReq = None

setup_logging()
log = logging.getLogger(__name__)

HOURS_BACK = int(os.getenv("HOURS_BACK", "24"))
MAX_TOTAL = int(os.getenv("MAX_TOTAL", "20000"))
MIN_REQUIRED = int(os.getenv("MIN_REQUIRED", "3000"))
COMMENT_LIMIT = int(os.getenv("COMMENT_LIMIT", "1000"))
TOP_K_DISPLAY = int(os.getenv("TOP_K_DISPLAY", "8"))
TAU_HOURS = float(os.getenv("TAU_HOURS", "6"))
SUBREDDITS_FILE = os.getenv("SUBREDDITS_FILE", "subreddits.txt")
ASSET = os.getenv("ASSET", "bitcoin").lower()
START_DATE = os.getenv("START_DATE", "2025-06-14")


def google_trend_score(keyword: str = "bitcoin") -> float:
    if TrendReq is None:
        return 0.5
    try:
        pt = TrendReq(hl="en-US", tz=0)
        pt.build_payload([keyword], timeframe="today 3-m")
        s = pt.interest_over_time()[keyword]
        s = s[s > 0]
        if s.empty:
            return 0.5
        ratio = s.iloc[-1] / (s.iloc[:-1].mean() or 1)
        return round(float(np.clip((ratio - 0.5) / 1.5 + 0.5, 0, 1)), 3)
    except Exception as exc:
        log.warning("pytrends error: %s -> 0.5", exc)
        return 0.5


def _load_subreddits(path: str) -> List[str]:
    with open(path, encoding="utf-8") as f:
        return [
            line.strip().replace("r/", "")
            for line in f
            if line.strip() and not line.startswith("#")
        ]


def reddit_pipeline(ts_midnight: dt.datetime) -> Dict[str, Any]:
    log.info("Starting Reddit pipeline")
    subs = _load_subreddits(SUBREDDITS_FILE)
    nlp = spacy.load("en_core_web_sm", disable=["ner"])

    log.info("Crawling window=%dh max=%d", HOURS_BACK, MAX_TOTAL)
    raw = RedditCrawler(
        subs,
        hours_back=HOURS_BACK,
        max_total=MAX_TOTAL,
        comment_lim=COMMENT_LIMIT,
        force_time=ts_midnight,
        min_required=MIN_REQUIRED,
    ).fetch()

    clean = RedditProcessor(nlp).process(raw)
    log.info("Messages retained: %d", len(clean))

    if not clean:
        return {"asset": ASSET, "reddit_index": 0.5, "clusters": []}

    scores = [
        score_text(c["text"])
        for c in tqdm(clean, desc="Sentiment", leave=False, dynamic_ncols=True)
    ]

    clusters = Clusterer().cluster([c["text"] for c in clean])

    upvotes = np.array([c["up"] for c in clean])
    ratios = np.array([c.get("ratio", 0.5) for c in clean])
    stamps = np.array([c["ts"] for c in clean])
    now_ts = time.time()

    contrib = []
    for idxs in clusters.values():
        idxs = np.array(idxs)
        w = (1 + np.log1p(upvotes[idxs]).sum()) * ratios[idxs].mean()
        avg = float(np.mean([scores[i] for i in idxs]))
        contrib.append({"avg": avg, "w": w, "idxs": idxs})

    if not contrib:
        reddit_idx = 0.5
    else:
        reddit_idx = float(
            np.average(
                [amplify(c["avg"]) for c in contrib],
                weights=[c["w"] for c in contrib],
            )
        )

    result_clusters = []
    for c in contrib:
        idxs = c["idxs"]
        importance = (
            np.abs(np.array(scores)[idxs] - 0.5)
            * np.log1p(upvotes[idxs])
            * np.exp(-(now_ts - stamps[idxs]) / 3600 / TAU_HOURS)
        )
        top = idxs[np.argsort(importance)[::-1][:3]]
        result_clusters.append(
            {
                "topic": top_keywords([clean[i]["text"] for i in idxs]),
                "avg": round(float(c["avg"]), 3),
                "freq": len(idxs),
                "delta": round(float((c["avg"] - 0.5) * c["w"]), 3),
                "summary": summarize(
                    " ".join(clean[i]["text"] for i in top), c["avg"]
                ),
                "examples": [clean[i]["text"].replace("\n", " ") for i in top],
                "urls": [clean[i]["url"] for i in top],
            }
        )

    return {
        "asset": ASSET,
        "reddit_index": reddit_idx,
        "clusters": sorted(
            result_clusters, key=lambda x: abs(x["delta"]), reverse=True
        )[:TOP_K_DISPLAY],
    }


def mix_scores(reddit_idx: float, gtrend_idx: float, market_idx: float) -> float:
    return round(0.5 * reddit_idx + 0.25 * gtrend_idx + 0.25 * market_idx, 3)


def save_score(
    idx: float,
    price_btc: float | None,
    price_eth: float | None,
    payload: Dict[str, Any],
    ts_midnight: dt.datetime,
) -> None:
    with psycopg2.connect(PG_DSN) as conn, conn.cursor() as cur:
        cur.execute(
            """
            INSERT INTO sentiment_scores (ts, ts_hour, score, price_btc, price_eth)
            VALUES (%s, %s, %s, %s, %s)
            ON CONFLICT (ts_hour) DO UPDATE
            SET score = EXCLUDED.score,
                price_btc = EXCLUDED.price_btc,
                price_eth = EXCLUDED.price_eth;
            """,
            (ts_midnight, ts_midnight, idx, price_btc, price_eth),
        )
        cur.execute(
            """
            INSERT INTO sentiment_details (ts_hour, json_payload)
            VALUES (%s, %s)
            ON CONFLICT (ts_hour) DO UPDATE
            SET json_payload = EXCLUDED.json_payload;
            """,
            (ts_midnight, Json(payload)),
        )
        conn.commit()
    log.info("Saved sentiment score for %s", ts_midnight.date())


def run_day(ts_midnight: dt.datetime) -> None:
    log.info("Running pipeline for %s", ts_midnight.date())
    reddit = reddit_pipeline(ts_midnight)
    gtrend = google_trend_score()
    market = market_composite(ts_midnight)
    global_idx = mix_scores(reddit["reddit_index"], gtrend, market)

    price_btc = fetch_price("bitcoin", ts_midnight)
    price_eth = fetch_price("ethereum", ts_midnight)

    payload = reddit | {
        "google_trend": gtrend,
        "market_index": market,
        "global_index": global_idx,
        "price_btc": price_btc,
        "price_eth": price_eth,
    }
    save_score(global_idx, price_btc, price_eth, payload, ts_midnight)


def missing_days(from_date: dt.datetime) -> List[dt.datetime]:
    from_date = from_date.replace(
        hour=0, minute=0, second=0, microsecond=0, tzinfo=dt.timezone.utc
    )
    today = dt.datetime.now(dt.timezone.utc).replace(
        hour=0, minute=0, second=0, microsecond=0
    )
    with psycopg2.connect(PG_DSN) as conn, conn.cursor() as cur:
        cur.execute(
            """
            SELECT date_trunc('day', ts_hour)
            FROM sentiment_scores
            WHERE ts_hour >= %s
            """,
            (from_date,),
        )
        existing = {r[0].date() for r in cur.fetchall()}

    return [
        dt.datetime.combine(
            from_date.date() + dt.timedelta(days=d),
            dt.time.min,
            tzinfo=dt.timezone.utc,
        )
        for d in range((today.date() - from_date.date()).days + 1)
        if (from_date.date() + dt.timedelta(days=d)) not in existing
    ]


def main() -> None:
    try:
        start = dt.datetime.fromisoformat(START_DATE).replace(tzinfo=dt.timezone.utc)
        days = missing_days(start)
        log.info("Missing days to backfill: %d", len(days))

        for day in tqdm(days, desc="Backfill"):
            run_day(day)

        if not days:
            today = dt.datetime.now(dt.timezone.utc).replace(
                hour=0, minute=0, second=0, microsecond=0
            )
            run_day(today)
        log.info("Pipeline complete")

    except Exception:
        log.exception("Pipeline failed")
        raise


if __name__ == "__main__":
    main()
