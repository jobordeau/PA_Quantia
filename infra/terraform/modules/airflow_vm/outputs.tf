output "instance_name" {
  value = google_compute_instance.airflow.name
}

output "instance_self_link" {
  value = google_compute_instance.airflow.self_link
}

output "internal_ip" {
  value = google_compute_instance.airflow.network_interface[0].network_ip
}

output "service_account_email" {
  value = google_service_account.airflow.email
}
