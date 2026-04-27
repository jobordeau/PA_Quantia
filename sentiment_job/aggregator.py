from __future__ import annotations

import datetime as dt
import logging

LOGGER = logging.getLogger(__name__)
ARCHIVE_DAYS = 30


def archive_old(conn) -> None:
    limit = dt.datetime.now(dt.timezone.utc) - dt.timedelta(days=ARCHIVE_DAYS)
    with conn.cursor() as cur:
        cur.execute(
            "DELETE FROM sentiment_scores WHERE ts_hour < %s",
            (limit,),
        )
        cur.execute(
            "DELETE FROM sentiment_details WHERE ts_hour < %s",
            (limit,),
        )
    conn.commit()
    LOGGER.info("Archived data older than %s", limit.date())
