from __future__ import annotations

import logging
from datetime import datetime, timedelta, timezone
from typing import List, Dict, Any

import requests
from airflow.decorators import dag, task
from airflow.models import Variable
from airflow.providers.google.cloud.hooks.bigquery import BigQueryHook

log = logging.getLogger(__name__)

SYMBOLS = ["BTCUSDT", "ETHUSDT"]
BINANCE_BASE = "https://api.binance.com/api/v3"
DEFAULT_LOOKBACK_MIN = 5


def _binance_klines(symbol: str, start_ms: int, end_ms: int) -> List[List[Any]]:
    url = f"{BINANCE_BASE}/klines"
    params = {
        "symbol": symbol,
        "interval": "1m",
        "startTime": start_ms,
        "endTime": end_ms,
        "limit": 1000,
    }
    resp = requests.get(url, params=params, timeout=15)
    resp.raise_for_status()
    return resp.json()


def _format_rows(symbol: str, klines: List[List[Any]]) -> List[Dict[str, Any]]:
    now_iso = datetime.now(timezone.utc).isoformat()
    rows: List[Dict[str, Any]] = []
    for k in klines:
        rows.append(
            {
                "ts": datetime.fromtimestamp(k[0] / 1000, tz=timezone.utc).isoformat(),
                "symbol": symbol,
                "open": str(k[1]),
                "high": str(k[2]),
                "low": str(k[3]),
                "close": str(k[4]),
                "volume": str(k[5]),
                "ingest_ts": now_iso,
            }
        )
    return rows


@dag(
    dag_id="quantia_crypto_ingestion",
    description="Ingest BTC/ETH 1-minute candles from Binance into BigQuery",
    schedule="* * * * *",
    start_date=datetime(2025, 1, 1, tzinfo=timezone.utc),
    catchup=False,
    max_active_runs=1,
    default_args={
        "owner": "quantia",
        "retries": 3,
        "retry_delay": timedelta(seconds=30),
    },
    tags=["quantia", "ingestion", "bigquery"],
)
def crypto_ingestion_dag():
    @task
    def fetch_and_load() -> int:
        project = Variable.get("BIGQUERY_PROJECT")
        dataset = Variable.get("BIGQUERY_DATASET")
        table = Variable.get("BIGQUERY_TABLE", default_var="crypto_prices")
        lookback_min = int(Variable.get("INGESTION_LOOKBACK_MIN", default_var=str(DEFAULT_LOOKBACK_MIN)))

        end = datetime.now(timezone.utc)
        start = end - timedelta(minutes=lookback_min)
        start_ms = int(start.timestamp() * 1000)
        end_ms = int(end.timestamp() * 1000)

        all_rows: List[Dict[str, Any]] = []
        for symbol in SYMBOLS:
            klines = _binance_klines(symbol, start_ms, end_ms)
            rows = _format_rows(symbol, klines)
            log.info("Fetched %d candles for %s", len(rows), symbol)
            all_rows.extend(rows)

        if not all_rows:
            log.warning("No rows fetched, skipping insert")
            return 0

        hook = BigQueryHook(use_legacy_sql=False, location=Variable.get("BIGQUERY_LOCATION", default_var="EU"))
        client = hook.get_client(project_id=project)
        full_table = f"{project}.{dataset}.{table}"

        errors = client.insert_rows_json(full_table, all_rows)
        if errors:
            raise RuntimeError(f"BigQuery insert errors: {errors}")

        log.info("Inserted %d rows into %s", len(all_rows), full_table)
        return len(all_rows)

    fetch_and_load()


crypto_ingestion_dag()
