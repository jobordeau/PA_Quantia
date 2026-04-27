# Quantia

Crypto trading insights platform. End-of-year project — Master's in Big Data
& AI, ESGI.

Quantia is a **multi-service application** combining a .NET web app, a Python
NLP pipeline for market sentiment, an Airflow data layer ingesting crypto
prices into BigQuery, and a Terraform-managed deployment on GKE.

> **Note on the prediction API.** The ML model that powers the *Prediction*
> page lives in a separate repository — [PA_ML](https://github.com/Saytk/PA_ML)
> — and is consumed by the web app over HTTP. Its base URL is fully
> configurable (`MlApi:BaseUrl`). All other components are in this repo.

---

## Table of contents

- [Architecture overview](#architecture-overview)
- [Repository layout](#repository-layout)
- [Quick start (Docker Compose)](#quick-start-docker-compose)
- [Run the web app standalone](#run-the-web-app-standalone)
- [Run the sentiment job standalone](#run-the-sentiment-job-standalone)
- [Deploy to GCP](#deploy-to-gcp)
- [CI / CD](#ci--cd)
- [Tech stack](#tech-stack)
- [License](#license)

---

## Architecture overview

```
            User ─► GKE Ingress ─► Quantia.Web (.NET 8) ─► Cloud SQL Postgres
                                              │
                                              └─► PA_ML API (separate repo)
                                                       ▲
                                                       │ reads
                                                       │
        Airflow VM ─► Binance API ─► BigQuery (crypto_prices, 1m candles)
              └────► sentiment_job (Docker) ─► Postgres (sentiment_*)
```

Detailed architecture, data flows, and security boundaries:
[`docs/architecture.md`](./docs/architecture.md).

---

## Repository layout

```
.
├── Quantia/                  # ASP.NET Core 8 MVC web app
│   ├── Controllers/
│   ├── Models/
│   ├── Services/
│   ├── Data/
│   ├── Views/
│   ├── wwwroot/
│   └── Dockerfile
├── sentiment_job/            # Python NLP pipeline (Reddit + Trends + market)
│   ├── main.py
│   ├── tech_indicators.py
│   ├── requirements.txt
│   └── Dockerfile
├── airflow/
│   ├── dags/
│   │   ├── crypto_ingestion_dag.py    # 1-minute BTC/ETH → BigQuery
│   │   └── sentiment_pipeline_dag.py  # daily sentiment job
│   ├── tests/
│   └── requirements.txt
├── infra/terraform/          # GCP IaC (modular, dev/recette/prod)
│   ├── modules/{vpc,gke,cloud_sql,bigquery,airflow_vm,artifact_registry}
│   └── environments/{dev,recette,prod}
├── deploy/kubernetes/        # Kustomize manifests for the web app
│   ├── base/
│   └── overlays/{dev,recette,prod}
├── .github/workflows/        # CI + deploy + Terraform plan
├── scripts/init.sql          # Postgres schema (used by docker-compose)
├── docker-compose.yml        # Local dev: Postgres + web + sentiment job
├── docs/architecture.md
├── Quantia.sln
├── .env.example
└── README.md
```

---

## Quick start (Docker Compose)

The fastest way to run the platform locally. Spins up Postgres (with the schema
pre-loaded) and the .NET web app. The Python sentiment job is wired in too but
hidden behind the `jobs` Compose profile (it's heavy — pulls torch and a spaCy
model — so it only runs on demand).

### Prerequisites

- Docker 24+ and the Docker Compose v2 plugin
- ~2 GB free RAM for the web stack; +4 GB if you also run the sentiment job

### 1. Configure environment

```bash
cp .env.example .env
$EDITOR .env
```

Minimum required to boot the web stack:

```env
POSTGRES_PASSWORD=changeme
WEB_PORT=8080
ML_API_BASE_URL=https://api-test-049u.onrender.com
```

To also run the sentiment job, fill in:

```env
REDDIT_CLIENT_ID=...
REDDIT_CLIENT_SECRET=...
USER_AGENT=quantia-sentiment-bot/0.1.0 (by u/your_user)
OPENAI_API_KEY=sk-...
```

### 2. Start the web stack

```bash
docker compose up --build
```

This brings up:
- `quantia-postgres` on `localhost:5432` — schema applied automatically from
  `scripts/init.sql`.
- `quantia-web` on `http://localhost:8080` — the ASP.NET Core app.

Watch the healthcheck:

```bash
curl -fsS http://localhost:8080/health
# {"status":"ok","utc":"2025-..."}
```

Open `http://localhost:8080`, register an account, and explore the dashboard.

### 3. Run the sentiment job (optional)

```bash
docker compose --profile jobs run --rm sentiment-job
```

This is a one-shot run: it crawls Reddit, scores sentiment, computes the
market composite, and writes the result to Postgres. Subsequent calls
back-fill any missing days.

### 4. Stop / clean up

```bash
docker compose down                 # stop containers, keep DB volume
docker compose down -v              # also drop the Postgres volume
```

---

## Run the web app standalone

If you have the .NET 8 SDK installed and a Postgres instance reachable on
`localhost:5432`:

```bash
# 1. Bootstrap the database
psql -U postgres -d postgres -c "CREATE DATABASE quantia;"
psql -U postgres -d quantia -f scripts/init.sql

# 2. Configure the connection string
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=quantia;Username=postgres;Password=YOUR_PASSWORD'
export MlApi__BaseUrl='https://api-test-049u.onrender.com'

# 3. Run
dotnet run --project Quantia
```

The app will be available at `http://localhost:5194` (HTTP profile) or
`https://localhost:7248` (HTTPS profile).

---

## Run the sentiment job standalone

If you'd rather run the Python pipeline outside Docker:

```bash
cd sentiment_job
python -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
python -m spacy download en_core_web_sm

cp .env.example .env
$EDITOR .env                    # set PG_CONN, REDDIT_*, OPENAI_API_KEY

python main.py
```

The job is idempotent at the day-level: each run computes any missing days
between `START_DATE` and today, then computes today's score.

---

## Deploy to GCP

The project provisions and deploys to three GCP environments: `dev`,
`recette` (staging), and `prod`. The full procedure is:

1. **Provision the infrastructure** with Terraform — see
   [`infra/terraform/README.md`](./infra/terraform/README.md). Each
   environment is a separate Terraform root and uses a GCS backend.
2. **Configure GitHub Actions** with Workload Identity Federation (no static
   keys), then set the per-environment GitHub variables and secrets that
   `.github/workflows/deploy.yml` expects:

   | Kind     | Name                                | Used for                                      |
   |----------|-------------------------------------|-----------------------------------------------|
   | Variable | `GCP_PROJECT_ID`                    | Project for the env                           |
   | Variable | `GCP_REGION`                        | e.g. `europe-west1`                           |
   | Variable | `GCP_AR_REPO`                       | e.g. `quantia-prod-images`                    |
   | Variable | `GKE_CLUSTER_NAME`                  | e.g. `quantia-prod-gke`                       |
   | Secret   | `GCP_WORKLOAD_IDENTITY_PROVIDER`    | WIF provider resource                         |
   | Secret   | `GCP_DEPLOYER_SA`                   | Deployer GCP service account email            |
   | Secret   | `GCP_TERRAFORM_SA`                  | Terraform GCP service account email           |
   | Secret   | `DB_CONNECTION_STRING`              | Cloud SQL connection string (env-specific)    |
3. **Push to the right branch** to trigger a deploy:
   - `develop` → `dev`
   - `release` → `recette`
   - `main` or a `v*.*.*` tag → `prod`

The deploy workflow builds & pushes both images to Artifact Registry, applies
the matching Kustomize overlay to GKE, waits for the rollout, and runs an
in-cluster `/health` smoke test.

---

## CI / CD

| Workflow                    | Trigger                                   | What it does                                                         |
|-----------------------------|-------------------------------------------|----------------------------------------------------------------------|
| `ci.yml`                    | push / PR to `main`, `develop`, `release` | .NET build, Python lint+compile, Airflow DAG tests, Terraform fmt+validate (3 envs), Kustomize build (3 overlays), Docker image smoke build |
| `deploy.yml`                | push to env branch / tag, manual dispatch | Build & push images to Artifact Registry, deploy to GKE via Kustomize, wait for rollout, smoke test |
| `terraform-plan.yml`        | PR touching `infra/terraform/**`          | `terraform plan` per env, attached to the PR                         |

All cloud authentication uses **Workload Identity Federation** — no JSON keys
in repo secrets.

---

## Tech stack

- **Web app:** ASP.NET Core 8 MVC, EF Core 9 (`Npgsql.EntityFrameworkCore.PostgreSQL`),
  cookie authentication, BCrypt password hashing, Bootstrap 5 + jQuery.
- **Sentiment pipeline:** Python 3.11, PRAW, spaCy + spaCy-langdetect,
  CryptoBERT (HuggingFace `transformers`), Sentence-BERT, HDBSCAN, KeyBERT,
  OpenAI GPT-4o-mini, pytrends, CoinGecko REST, psycopg2.
- **Orchestration:** Apache Airflow 2.9 with the Google and Docker providers.
- **Datastore:** PostgreSQL 16 (operational data) + BigQuery (analytical /
  time-series).
- **Cloud:** GCP — VPC, private GKE, Cloud SQL, BigQuery, Compute Engine VM
  (Container-Optimized OS), Artifact Registry, Secret Manager, Cloud NAT, IAP.
- **IaC & deploy:** Terraform 1.9, Kustomize, Docker, GitHub Actions.

---

## License

[MIT](./LICENSE)
