# Airflow — Quantia data pipelines

Apache Airflow DAGs orchestrating the data layer of Quantia. In production
they run on a Compute Engine VM provisioned by `infra/terraform/modules/airflow_vm`.

## DAGs

### `quantia_crypto_ingestion`
- **Schedule:** every minute (`* * * * *`)
- **Source:** Binance public REST API (`/api/v3/klines`)
- **Sink:** BigQuery — `${BIGQUERY_PROJECT}.${BIGQUERY_DATASET}.crypto_prices`
- **Symbols:** `BTCUSDT`, `ETHUSDT`
- **Behaviour:** fetches the last `INGESTION_LOOKBACK_MIN` minutes of 1-minute
  candles for each symbol and inserts them via `BigQueryHook.insert_rows_json`.
  Re-runs are safe at the row level because each insert is keyed by `ts + symbol`
  in downstream queries.

### `quantia_sentiment_pipeline`
- **Schedule:** daily at 01:00 UTC (`0 1 * * *`)
- **Operator:** `DockerOperator` running the `quantia/sentiment-job` image
- **Output:** rows in PostgreSQL (`sentiment_scores`, `sentiment_details`).
  Reads from Reddit, Google Trends and CoinGecko, scores with CryptoBERT,
  clusters with HDBSCAN, summarises with GPT-4o-mini.

## Required Airflow variables

Set these via the Airflow UI (Admin → Variables) or `airflow variables set`:

| Variable                  | Used by                       | Example                                      |
|---------------------------|-------------------------------|----------------------------------------------|
| `BIGQUERY_PROJECT`        | crypto_ingestion              | `my-quantia-prod-project`                    |
| `BIGQUERY_DATASET`        | crypto_ingestion              | `quantia_market`                             |
| `BIGQUERY_TABLE`          | crypto_ingestion (optional)   | `crypto_prices`                              |
| `BIGQUERY_LOCATION`       | crypto_ingestion (optional)   | `EU`                                         |
| `INGESTION_LOOKBACK_MIN`  | crypto_ingestion (optional)   | `5`                                          |
| `PG_CONN`                 | sentiment_pipeline            | `postgresql://user:pwd@10.0.0.5:5432/quantia`|
| `REDDIT_CLIENT_ID`        | sentiment_pipeline            | (your Reddit script app id)                  |
| `REDDIT_CLIENT_SECRET`    | sentiment_pipeline            | (your Reddit script app secret)              |
| `OPENAI_API_KEY`          | sentiment_pipeline            | (your OpenAI key)                            |
| `SENTIMENT_JOB_IMAGE`     | sentiment_pipeline            | `europe-west1-docker.pkg.dev/PROJECT/quantia-prod-images/sentiment-job:v1`|

## Local testing

```bash
cd airflow
python -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
pytest tests/
```

The tests verify DAG import integrity and basic structure without requiring a
running Airflow scheduler.
