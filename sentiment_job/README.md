# Sentiment job

Daily NLP pipeline that produces a market sentiment score for crypto assets
by combining three signals:

1. **Reddit sentiment** — crawl, filter, language-detect, score with
   CryptoBERT, cluster with HDBSCAN, summarise with GPT-4o-mini.
2. **Google Trends** — recent search interest ratio for the asset name.
3. **Market composite** — momentum + volatility + volume signal computed
   from CoinGecko price history.

These three are mixed (50% Reddit, 25% Trends, 25% market) into a single
score in `[0, 1]` and stored in Postgres alongside the per-cluster JSON
payload (topic, average score, examples, summary, URLs).

## Run locally

```bash
python -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
python -m spacy download en_core_web_sm

cp .env.example .env        # fill in Reddit / OpenAI keys + PG_CONN
python main.py
```

## Run via Docker

```bash
docker build -t quantia-sentiment-job .
docker run --rm \
  --env-file .env \
  --network host \
  quantia-sentiment-job
```

## Run via Airflow

The DAG `quantia_sentiment_pipeline` (in `airflow/dags/`) runs this job daily
at 01:00 UTC using a `DockerOperator`. It pulls every required env var from
Airflow Variables (`PG_CONN`, `REDDIT_CLIENT_ID`, etc.).

## Database schema

The job writes to two tables (created by `scripts/init.sql`):

- `sentiment_scores(ts, ts_hour, score, price_btc, price_eth)` — one row per
  pipeline run (daily), unique on `ts_hour`.
- `sentiment_details(ts_hour, json_payload)` — per-day cluster payload as
  JSONB, primary key on `ts_hour`.

Reddit raw documents are also cached in `reddit_raw(id, ts, sub, data)` to
avoid re-fetching during the day.

## Modules

| File                  | Purpose                                                       |
|-----------------------|---------------------------------------------------------------|
| `main.py`             | Orchestrator + backfill loop                                  |
| `reddit_crawler.py`   | PRAW-based crawler with cache                                 |
| `reddit_processor.py` | spaCy-based filtering (language, length, has-verb, no-question)|
| `processor.py`        | spaCy language-detection plumbing                             |
| `sentiment_scorer.py` | CryptoBERT scoring                                            |
| `clusterer.py`        | Sentence-BERT + HDBSCAN clustering                            |
| `keywords.py`         | KeyBERT topic extraction                                      |
| `summarizer.py`       | GPT-4o-mini summarisation                                     |
| `tech_indicators.py`  | CoinGecko-based market composite                              |
| `price_fetcher.py`    | CoinGecko price lookup with retry                             |
| `aggregator.py`       | DB archival utility (older than `ARCHIVE_DAYS`)               |
| `utils.py`            | Sigmoid amplification helper                                  |
| `logging_config.py`   | Console + rotating-file logger                                |
