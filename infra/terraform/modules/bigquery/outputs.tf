output "dataset_id" {
  value = google_bigquery_dataset.main.dataset_id
}

output "table_id" {
  value = google_bigquery_table.crypto_prices.table_id
}

output "fully_qualified_table" {
  value = "${var.project_id}.${google_bigquery_dataset.main.dataset_id}.${google_bigquery_table.crypto_prices.table_id}"
}

output "ingestion_service_account_email" {
  value = google_service_account.ingestion.email
}
