# Quantia

Plateforme d'analyse et d'insights pour le trading de cryptomonnaies. Projet de fin d'études — Master Big Data & AI, ESGI.

Quantia est une **application multi-services** combinant une application web .NET, un pipeline NLP en Python pour le sentiment de marché, une couche de données Airflow ingérant les prix des cryptos dans BigQuery, et un déploiement géré par Terraform sur GKE (Google Kubernetes Engine).

> **Note sur l'API de prédiction :** Le modèle de ML qui alimente la page *Prediction* réside dans un dépôt séparé — [PA_ML](https://github.com/Saytk/PA_ML) — et est consommé par l'application web via HTTP. Son URL de base est entièrement configurable (`MlApi:BaseUrl`). Tous les autres composants se trouvent dans ce dépôt.

---

## Table des matières

- [Aperçu de l'architecture](#aperçu-de-larchitecture)
- [Structure du dépôt](#structure-du-dépôt)
- [Démarrage rapide (Docker Compose)](#démarrage-rapide-docker-compose)
- [Exécution de l'app web (Standalone)](#exécution-de-lapp-web-standalone)
- [Exécution du sentiment job (Standalone)](#exécution-du-sentiment-job-standalone)
- [Déploiement sur GCP](#déploiement-sur-gcp)
- [CI / CD](#ci--cd)
- [Stack technique](#stack-technique)
- [Licence](#licence)

---

## Aperçu de l'architecture

```
            Utilisateur ─► GKE Ingress ─► Quantia.Web (.NET 8) ─► Cloud SQL Postgres
                                              │
                                              └─► PA_ML API (dépôt séparé)
                                                       ▲
                                                       │ lecture
                                                       │
        Airflow VM ─► Binance API ─► BigQuery (crypto_prices, bougies 1m)
              └────► sentiment_job (Docker) ─► Postgres (sentiment_*)
```

Architecture détaillée, flux de données et limites de sécurité :
[`docs/architecture.md`](./docs/architecture.md).

---

## Structure du dépôt

```
.
├── Quantia/                  # Application web ASP.NET Core 8 MVC
│   ├── Controllers/
│   ├── Models/
│   ├── Services/
│   ├── Data/
│   ├── Views/
│   ├── wwwroot/
│   └── Dockerfile
├── sentiment_job/            # Pipeline NLP Python (Reddit + Trends + Marché)
│   ├── main.py
│   ├── tech_indicators.py
│   ├── requirements.txt
│   └── Dockerfile
├── airflow/
│   ├── dags/
│   │   ├── crypto_ingestion_dag.py    # 1-minute BTC/ETH → BigQuery
│   │   └── sentiment_pipeline_dag.py  # Job de sentiment quotidien
│   ├── tests/
│   └── requirements.txt
├── infra/terraform/          # GCP IaC (modulaire : dev/recette/prod)
│   ├── modules/{vpc,gke,cloud_sql,bigquery,airflow_vm,artifact_registry}
│   └── environments/{dev,recette,prod}
├── deploy/kubernetes/        # Manifestes Kustomize pour l'app web
│   ├── base/
│   └── overlays/{dev,recette,prod}
├── .github/workflows/        # CI + déploiement + Terraform plan
├── scripts/init.sql          # Schéma Postgres (utilisé par docker-compose)
├── docker-compose.yml        # Dev local : Postgres + web + sentiment job
├── docs/architecture.md
├── Quantia.sln
├── .env.example
└── README.md
```

---

## Démarrage rapide (Docker Compose)

La méthode la plus rapide pour lancer la plateforme localement. Lance Postgres (avec le schéma pré-chargé) et l'application web .NET. Le job de sentiment Python est également configuré mais masqué derrière le profil Compose `jobs` (il est lourd — télécharge torch et un modèle spaCy — il ne s'exécute donc qu'à la demande).

### Prérequis

- Docker 24+ et le plugin Docker Compose v2
- ~2 Go de RAM libre pour la stack web ; +4 Go si vous lancez aussi le job de sentiment

### 1. Configurer l'environnement

```bash
cp .env.example .env
$EDITOR .env
```

Minimum requis pour démarrer la stack web :

```env
POSTGRES_PASSWORD=changeme
WEB_PORT=8080
ML_API_BASE_URL=https://api-test-049u.onrender.com
```

Pour lancer aussi le job de sentiment, renseignez :

```env
REDDIT_CLIENT_ID=...
REDDIT_CLIENT_SECRET=...
USER_AGENT=quantia-sentiment-bot/0.1.0 (par u/votre_utilisateur)
OPENAI_API_KEY=sk-...
```

### 2. Démarrer la stack web

```bash
docker compose up --build
```

Cela lance :
- `quantia-postgres` sur `localhost:5432` — schéma appliqué automatiquement via `scripts/init.sql`.
- `quantia-web` sur `http://localhost:8080` — l'application ASP.NET Core.

Vérifiez la santé du service :

```bash
curl -fsS http://localhost:8080/health
# {"status":"ok","utc":"2025-..."}
```

Ouvrez `http://localhost:8080`, créez un compte et explorez le tableau de bord.

### 3. Lancer le job de sentiment (optionnel)

```bash
docker compose --profile jobs run --rm sentiment-job
```

C'est une exécution ponctuelle : il scanne Reddit, score le sentiment, calcule l'indice composite de marché et écrit le résultat dans Postgres. Les appels suivants comblent les jours manquants.

---

## Exécution de l'app web (Standalone)

Si vous avez le SDK .NET 8 installé et une instance Postgres accessible sur `localhost:5432` :

```bash
# 1. Initialiser la base de données
psql -U postgres -d postgres -c "CREATE DATABASE quantia;"
psql -U postgres -d quantia -f scripts/init.sql

# 2. Configurer la chaîne de connexion
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=quantia;Username=postgres;Password=VOTRE_MOT_DE_PASSE'
export MlApi__BaseUrl='https://api-test-049u.onrender.com'

# 3. Lancer
dotnet run --project Quantia
```

L'application sera disponible sur `http://localhost:5194` (profil HTTP) ou `https://localhost:7248` (profil HTTPS).

---

## Exécution du sentiment job (Standalone)

Pour exécuter le pipeline Python en dehors de Docker :

```bash
cd sentiment_job
python -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
python -m spacy download en_core_web_sm

cp .env.example .env
$EDITOR .env                    # définissez PG_CONN, REDDIT_*, OPENAI_API_KEY

python main.py
```

Le job est idempotent au niveau journalier : chaque exécution calcule les jours manquants entre `START_DATE` et aujourd'hui.

---

## Déploiement sur GCP

Le projet provisionne et déploie sur trois environnements GCP : `dev`, `recette` (staging), et `prod`.

1. **Provisionnement de l'infrastructure** avec Terraform — voir [`infra/terraform/README.md`](./infra/terraform/README.md).
2. **Configuration de GitHub Actions** via Workload Identity Federation (pas de clés statiques), puis configuration des variables et secrets par environnement :
   - `GCP_PROJECT_ID`, `GCP_REGION`, `GKE_CLUSTER_NAME`, `DB_CONNECTION_STRING`, etc.
3. **Push sur la branche correspondante** pour déclencher le déploiement :
   - `develop` → `dev`
   - `release` → `recette`
   - `main` ou tag `v*.*.*` → `prod`

---

## CI / CD

| Workflow                    | Déclencheur                               | Action                                                               |
|-----------------------------|-------------------------------------------|----------------------------------------------------------------------|
| `ci.yml`                    | push / PR vers `main`, `develop`, `release` | Build .NET, lint Python, tests DAGs Airflow, Terraform fmt/validate, build images Docker |
| `deploy.yml`                | push sur branche d'env / tag              | Build & push images vers Artifact Registry, déploiement GKE via Kustomize, test de santé |
| `terraform-plan.yml`        | PR modifiant `infra/terraform/**`         | `terraform plan` par environnement, attaché en commentaire de la PR  |

---

## Stack technique

- **Web App :** ASP.NET Core 8 MVC, EF Core 9, authentification par cookie, BCrypt, Bootstrap 5.
- **Sentiment Pipeline :** Python 3.11, PRAW, spaCy, CryptoBERT (Transformers), Sentence-BERT, HDBSCAN, KeyBERT, OpenAI GPT-4o-mini, pytrends, CoinGecko, psycopg2.
- **Orchestration :** Apache Airflow 2.9.
- **Stockage :** PostgreSQL 16 (données opérationnelles) + BigQuery (séries temporelles / analytique).
- **Cloud (GCP) :** VPC, GKE privé, Cloud SQL, BigQuery, Artifact Registry, Secret Manager.
- **IaC & Déploiement :** Terraform 1.9, Kubernetes, Docker, GitHub Actions.

---
