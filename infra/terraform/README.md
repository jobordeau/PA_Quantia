# Terraform — Quantia infrastructure

Infrastructure as Code for the Quantia platform on Google Cloud Platform.

## Layout

```
terraform/
├── modules/
│   ├── vpc/                  # VPC + subnet + NAT + IAP firewall
│   ├── gke/                  # Private GKE cluster + node pool + Workload Identity
│   ├── cloud_sql/            # Private Postgres (Cloud SQL) + Secret Manager
│   ├── bigquery/             # Dataset + partitioned table for crypto candles
│   ├── airflow_vm/           # Compute Engine VM running Airflow on COS
│   └── artifact_registry/    # Docker image registry
└── environments/
    ├── dev/
    ├── recette/              # Staging (UAT)
    └── prod/
```

Each environment is a separate Terraform root that calls the same modules with
different sizing / availability / protection settings.

## Prerequisites

1. A GCP project per environment (or a single project with naming separation).
2. APIs enabled in each project:
   ```
   gcloud services enable \
     compute.googleapis.com \
     container.googleapis.com \
     sqladmin.googleapis.com \
     servicenetworking.googleapis.com \
     bigquery.googleapis.com \
     secretmanager.googleapis.com \
     artifactregistry.googleapis.com \
     iam.googleapis.com \
     iap.googleapis.com
   ```
3. A GCS bucket for Terraform state per environment, e.g.:
   ```
   gsutil mb -p PROJECT_ID -l EU gs://quantia-tfstate-dev
   gsutil versioning set on gs://quantia-tfstate-dev
   ```
4. Authentication: `gcloud auth application-default login`.

## Usage

```bash
cd environments/dev
cp terraform.tfvars.example terraform.tfvars
$EDITOR terraform.tfvars   # set your project_id, region, etc.

terraform init
terraform plan -out plan.tfplan
terraform apply plan.tfplan
```

To target a different environment, replace `dev` with `recette` or `prod`.

## What gets provisioned

- **VPC** with a `/20` private subnet, secondary ranges for GKE pods/services,
  Cloud NAT for egress, and an IAP firewall rule for SSH.
- **Cloud SQL Postgres 16** with private IP only, automated backups,
  point-in-time recovery enabled in `recette` and `prod`. The generated
  password is stored in Secret Manager.
- **BigQuery** dataset + a daily-partitioned, symbol-clustered `crypto_prices`
  table for ingestion at minute resolution.
- **Private GKE** cluster with Workload Identity, Regional control plane,
  auto-upgrade/auto-repair node pool, autoscaling.
- **Airflow on a Compute Engine VM** (Container-Optimized OS) running the
  Airflow standalone image as a systemd-managed Docker container, with the
  data directory mounted on a separate persistent disk.
- **Artifact Registry** Docker repository for the application images.

## Environments

| Resource          | dev               | recette                | prod                       |
|-------------------|-------------------|------------------------|----------------------------|
| Cloud SQL tier    | `db-f1-micro`     | `db-custom-2-7680`     | `db-custom-4-15360`        |
| Cloud SQL HA      | ZONAL             | ZONAL                  | REGIONAL                   |
| GKE machine       | `e2-standard-2`   | `e2-standard-2`        | `e2-standard-4`            |
| GKE min/max nodes | 1 / 2             | 2 / 4                  | 3 / 10                     |
| Deletion protect. | off               | on                     | on                         |
| BQ partition exp. | 90 days           | 90 days                | unlimited                  |
| Airflow branch    | `main`            | `release`              | `main`                     |

## Destroying

```bash
cd environments/dev
terraform destroy
```

Note that `recette` and `prod` set `deletion_protection = true` on Cloud SQL,
GKE, and BigQuery. You must explicitly toggle that off and re-apply before
`destroy` will succeed.
