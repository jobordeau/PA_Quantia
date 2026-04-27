output "instance_name" {
  value = google_sql_database_instance.main.name
}

output "instance_connection_name" {
  value = google_sql_database_instance.main.connection_name
}

output "private_ip_address" {
  value = google_sql_database_instance.main.private_ip_address
}

output "database_name" {
  value = google_sql_database.quantia.name
}

output "app_user" {
  value = google_sql_user.app.name
}

output "password_secret_id" {
  value = google_secret_manager_secret.db_password.secret_id
}

output "password_secret_name" {
  value = google_secret_manager_secret.db_password.name
}
