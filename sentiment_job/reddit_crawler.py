#!/usr/bin/env python
"""
Récupère posts & commentaires Reddit pour une fenêtre glissante de 24 h
exactement : (ts_midnight – hours_back) ≤ ts < ts_midnight.

• Stocke chaque message brut dans la table **reddit_raw**
  (id, ts, sub, data JSONB).
• Utilise le cache avant d’appeler l’API : seuls les messages manquants
  sont vraiment téléchargés.
"""

from __future__ import annotations
import os, time, datetime as dt, logging
from typing import List, Dict

import praw, prawcore, psycopg2
from psycopg2.extras import Json

log    = logging.getLogger(__name__)
PG_DSN = os.environ["PG_CONN"]                 

class RedditCrawler:
    """Crawl + cache 24 h, avec seuil min_required pour éviter l’API."""

    def __init__(
        self,
        subreddits: List[str],
        hours_back: int  = 24,
        max_total: int   = 10_000,
        comment_lim: int = 50,
        force_time: dt.datetime | None = None,   
        min_required: int = 5_000,               
    ):
        self.subreddits   = subreddits
        self.hours_back   = hours_back
        self.max_total    = max_total
        self.comment_lim  = comment_lim
        self.force_time   = force_time
        self.min_required = min_required

        self.high_ts = int(force_time.timestamp() if force_time else time.time())
        self.low_ts  = self.high_ts - hours_back * 3600

        self.reddit = praw.Reddit(
            client_id       = os.getenv("REDDIT_CLIENT_ID"),
            client_secret   = os.getenv("REDDIT_CLIENT_SECRET"),
            user_agent      = os.getenv("USER_AGENT", "crypto_sentiment_bot"),
            check_for_async = False,
        )
        self.reddit.read_only = True

    def _save_bulk(self, rows: List[Dict]) -> None:
        if not rows:
            return
        with psycopg2.connect(PG_DSN) as conn, conn.cursor() as cur:
            cur.executemany(
                """
                INSERT INTO reddit_raw (id, ts, sub, data)
                VALUES (%s, to_timestamp(%s), %s, %s)
                ON CONFLICT (id) DO NOTHING;
                """,
                [(r["id"], r["ts"], r["sub"], Json(r)) for r in rows],
            )
            conn.commit()
        log.info("Reddit NEW rows fetched : %d", len(rows))

    def fetch(self) -> List[Dict]:
        with psycopg2.connect(PG_DSN) as conn, conn.cursor() as cur:
            cur.execute(
                """
                SELECT data FROM reddit_raw
                WHERE ts >= to_timestamp(%s)
                  AND ts <  to_timestamp(%s)
                """,
                (self.low_ts, self.high_ts),
            )
            rows = [r[0] for r in cur.fetchall()]

        log.info("Reddit cache hit → %d messages", len(rows))

        if len(rows) >= self.min_required:
            log.info("Cache ≥ min_required (%d) → skip appel API", self.min_required)
            return rows[: self.max_total]

        known   = {r["id"] for r in rows}
        new_rows: List[Dict] = []

        for sub_name in self.subreddits:
            try:
                sub = self.reddit.subreddit(sub_name)
                _   = sub.id                      
            except prawcore.exceptions.Redirect:
                log.warning("Subreddit introuvable / privé : %s — ignoré", sub_name)
                continue
            except Exception as exc:
                log.warning("Erreur subreddit %s : %s — ignoré", sub_name, exc)
                continue

            for post in sub.new(limit=None):
                if post.created_utc < self.low_ts:
                    break
                if post.created_utc >= self.high_ts:
                    continue

                # -- POST -------------------------------------------------
                if post.id not in known:
                    new_rows.append(
                        {
                            "id": post.id,
                            "ts": int(post.created_utc),
                            "sub": sub_name,
                            "text": f"{post.title} {post.selftext or ''}",
                            "up": max(post.score, 1),
                            "ratio": post.upvote_ratio or 0.5,
                            "url": f"https://www.reddit.com{post.permalink}",
                        }
                    )
                    known.add(post.id)

                if len(new_rows) + len(rows) >= self.max_total:
                    break

                # -- COMMENTS --------------------------------------------
                try:
                    post.comments.replace_more(limit=0)
                except Exception:
                    continue

                for com in post.comments[: self.comment_lim]:
                    if com.created_utc < self.low_ts:
                        break
                    if com.created_utc >= self.high_ts:
                        continue
                    cid = f"{post.id}_{com.id}"
                    if cid in known:
                        continue
                    new_rows.append(
                        {
                            "id": cid,
                            "ts": int(com.created_utc),
                            "sub": sub_name,
                            "text": com.body,
                            "up": max(com.score, 1),
                            "ratio": 0.5,
                            "url": f"https://www.reddit.com{com.permalink}",
                        }
                    )
                    known.add(cid)
                    if len(new_rows) + len(rows) >= self.max_total:
                        break

                if len(new_rows) + len(rows) >= self.max_total:
                    break

            if len(new_rows) + len(rows) >= self.max_total:
                break


        self._save_bulk(new_rows)
        return rows + new_rows
