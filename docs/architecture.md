# Architecture

Quantia is a crypto trading insights platform. End users sign up, simulate or
record trades, and consult three analysis modules: technical analysis, market
sentiment, and ML-based price prediction.

This document describes the runtime architecture, the data flows, and the
boundaries between the components in this repository and the external services
they depend on.

## High-level diagram

```
                                ┌──────────────────────────┐
                                │        End user          │
                                │   (browser, GKE LB IP)   │
                                └────────────┬─────────────┘
                                             │ HTTPS
                                             ▼
                            ┌────────────────────────────────────┐
                            │       GKE — Quantia web (.NET 8)   │
                            │  - MVC + cookie auth (BCrypt)      │
                            │  - Calls ML API for predictions    │
                            │  - Reads sentiment from Postgres   │
                            └────┬───────────────────┬───────────┘
                                 │                   │
                                 ▼                   ▼
                  ┌─────────────────────┐   ┌────────────────────────┐
                  │  Cloud SQL Postgres │   │  ML prediction API     │
                  │  - users / trades   │   │  (separate repo &      │
                  │  - sentiment_scores │   │   service: PA_ML)      │
                  │  - sentiment_details│   └────────────────────────┘
                  └──────────┬──────────┘                ▲
                             ▲                            │
                             │ writes                     │
                             │                            │ reads BigQuery
                ┌────────────┴────────────────┐           │
                │  Sentiment job (Python)     │           │
                │  - Reddit / Trends / market │           │
                │  - Cryptobert + HDBSCAN     │           │
                │  - GPT-4o-mini summaries    │           │
                │  Run by Airflow VM, daily   │           │
                └─────────────────────────────┘           │
                                                          │
                                ┌─────────────────────────┴──────┐
                                │  BigQuery — quantia_market     │
                                │  - crypto_prices (1m candles)  │
                                │  - daily-partitioned, clustered│
                                └────────────────────────────────┘
                                              ▲
                                              │ inserts every minute
                                              │
                                ┌─────────────┴──────────────────┐
                                │   Airflow on Compute Engine    │
                                │   - quantia_crypto_ingestion   │
                                │   - quantia_sentiment_pipeline │
                                └────────────────────────────────┘
```

## Components in this repository

| Path                       | What it is                              | Runtime                    |
|----------------------------|-----------------------------------------|----------------------------|
| `Quantia/`                 | ASP.NET Core 8 MVC web app              | Container on GKE (or local)|
| `sentiment_job/`           | Python NLP pipeline                     | Container, run by Airflow  |
| `airflow/dags/`            | Airflow DAGs (ingestion + sentiment)    | Airflow on a GCE VM        |
| `infra/terraform/`         | GCP IaC (VPC, GKE, Cloud SQL, BQ, …)    | Terraform CLI / CI         |
| `deploy/kubernetes/`       | Kustomize manifests for the web app     | kubectl / CI               |
| `.github/workflows/`       | CI (build, lint, test) and CD (deploy)  | GitHub Actions             |
| `scripts/init.sql`         | Postgres schema bootstrap               | docker-compose / migration |
| `docker-compose.yml`       | Local dev stack: Postgres + web + job   | Docker Compose             |

## Components NOT in this repository

- **ML prediction API.** A FastAPI service that serves
  `/prediction/latest`, `/data/{symbol}`, `/pattern/load-data`, `/refresh-model`,
  `/run_ml_pipeline`, `/get_model_metrics`, `/trade/suggest`. It is hosted in a
  separate repository (`PA_ML`, https://github.com/Saytk/PA_ML) and currently
  runs on Render. The web app reads its base URL from configuration
  (`MlApi:BaseUrl`) and uses an injected `IHttpClientFactory("MLApi")`.

## Data flows

### 1. Crypto price ingestion (every minute)
1. Airflow DAG `quantia_crypto_ingestion` triggers.
2. The task fetches the last 5 minutes of 1-minute candles from Binance
   public REST (`/api/v3/klines`) for `BTCUSDT` and `ETHUSDT`.
3. Rows are inserted into `quantia_market.crypto_prices` in BigQuery via
   `BigQueryHook.insert_rows_json`. The table is **partitioned by `ts`
   (DAY)** and **clustered by `symbol`** for efficient downstream queries.

### 2. Sentiment pipeline (daily at 01:00 UTC)
1. Airflow DAG `quantia_sentiment_pipeline` triggers.
2. A `DockerOperator` runs the `quantia/sentiment-job` image with a `PG_CONN`
   pointing to Cloud SQL Postgres.
3. The job:
   - Crawls Reddit (PRAW) for 24h of posts/comments across ~200 crypto subs,
     with a Postgres-backed cache to avoid re-fetching.
   - Filters and language-detects messages with spaCy.
   - Scores sentiment with CryptoBERT.
   - Clusters with HDBSCAN over Sentence-BERT embeddings.
   - Extracts topics with KeyBERT, summarises top messages with GPT-4o-mini.
   - Computes a Google-Trends ratio and a market composite (momentum +
     volatility + volume) from CoinGecko.
   - Mixes the three indices into a global sentiment score.
4. Writes `score + per-cluster JSON payload` into Postgres tables
   `sentiment_scores` and `sentiment_details`.

### 3. User flow (web app)
1. User signs up / logs in (BCrypt-hashed password, cookie auth named
   `QuantiaAuth`).
2. The Dashboard / Portfolio / Trade pages persist the user's positions in
   Postgres (`transactions`, `trades`).
3. The Prediction page calls the ML API and renders signals + executed trades.
4. The Sentiment Analysis page reads the latest payload from
   `sentiment_details` (no live computation in the web tier).

## Environment topology

| Environment | Branch    | GCP project (placeholder)             | DNS host                    |
|-------------|-----------|---------------------------------------|-----------------------------|
| `dev`       | `develop` | `my-quantia-dev-project`              | `dev.quantia.example.com`   |
| `recette`   | `release` | `my-quantia-recette-project`          | `recette.quantia.example.com`|
| `prod`      | `main` / tags | `my-quantia-prod-project`         | `quantia.example.com`       |

Sizing differences are documented in `infra/terraform/README.md` and
`deploy/kubernetes/README.md`.

## Security boundaries

- Cloud SQL has **no public IP** — connectivity is via VPC peering only.
- GKE master is **private**; access is restricted by `master_authorized_cidrs`.
- Pods authenticate to GCP via **Workload Identity** (no JSON keys in
  containers).
- Database password is generated by Terraform (`random_password`) and stored
  in **Secret Manager**; the deploy workflow injects it into a Kubernetes
  Secret at apply time.
- Container images run as **non-root** with `readOnlyRootFilesystem` and
  dropped Linux capabilities.

## CI / CD

- **CI (`.github/workflows/ci.yml`)** runs on every push and PR:
  builds the .NET project, lints Python with Ruff, validates Airflow DAGs,
  formats and validates Terraform per environment, builds Kustomize overlays,
  and smoke-builds the Docker images.
- **Deploy (`.github/workflows/deploy.yml`)** runs on push to environment
  branches (and tags for prod):
  1. Picks the target environment from the branch / tag.
  2. Authenticates to GCP via **Workload Identity Federation** (no static
     keys).
  3. Builds & pushes both Docker images to Artifact Registry with both a
     SHA-based and an environment-based tag.
  4. Pulls GKE credentials, sets the image tag in the kustomize overlay,
     materialises the database connection secret, applies the manifests,
     waits for rollout, and runs an in-cluster `/health` smoke test.
- **Terraform plan (`.github/workflows/terraform-plan.yml`)** runs on PRs that
  touch `infra/terraform/**` and produces a plan per environment.
