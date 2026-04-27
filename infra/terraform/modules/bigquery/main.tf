resource "google_bigquery_dataset" "main" {
  dataset_id    = var.dataset_id
  friendly_name = "Quantia ${var.environment} dataset"
  description   = "Stores ingested crypto market data (BTC, ETH, ...) at minute resolution"
  location      = var.location

  labels = {
    environment = var.environment
    managed_by  = "terraform"
  }

  default_table_expiration_ms = var.environment == "prod" ? null : 7776000000
}

resource "google_bigquery_table" "crypto_prices" {
  dataset_id = google_bigquery_dataset.main.dataset_id
  table_id   = "crypto_prices"

  description = "OHLCV minute candles for tracked crypto assets"

  time_partitioning {
    type          = "DAY"
    field         = "ts"
    expiration_ms = var.partition_expiration_ms
  }

  clustering = ["symbol"]

  schema = jsonencode([
    { name = "ts",        type = "TIMESTAMP", mode = "REQUIRED", description = "Candle timestamp (UTC)" },
    { name = "symbol",    type = "STRING",    mode = "REQUIRED", description = "Trading pair, e.g. BTCUSDT" },
    { name = "open",      type = "NUMERIC",   mode = "REQUIRED" },
    { name = "high",      type = "NUMERIC",   mode = "REQUIRED" },
    { name = "low",       type = "NUMERIC",   mode = "REQUIRED" },
    { name = "close",     type = "NUMERIC",   mode = "REQUIRED" },
    { name = "volume",    type = "NUMERIC",   mode = "NULLABLE" },
    { name = "ingest_ts", type = "TIMESTAMP", mode = "REQUIRED", description = "Time the row was ingested" }
  ])

  deletion_protection = var.deletion_protection
}

resource "google_service_account" "ingestion" {
  account_id   = "${var.name_prefix}-bq-ingest"
  display_name = "Service account used by Airflow to ingest into BigQuery"
}

resource "google_bigquery_dataset_iam_member" "ingestion_editor" {
  dataset_id = google_bigquery_dataset.main.dataset_id
  role       = "roles/bigquery.dataEditor"
  member     = "serviceAccount:${google_service_account.ingestion.email}"
}

resource "google_project_iam_member" "ingestion_job_user" {
  project = var.project_id
  role    = "roles/bigquery.jobUser"
  member  = "serviceAccount:${google_service_account.ingestion.email}"
}
