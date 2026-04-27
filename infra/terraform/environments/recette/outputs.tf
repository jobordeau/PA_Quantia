output "gke_cluster_name" {
  value = module.gke.cluster_name
}

output "gke_endpoint" {
  value     = module.gke.cluster_endpoint
  sensitive = true
}

output "cloud_sql_connection_name" {
  value = module.cloud_sql.instance_connection_name
}

output "cloud_sql_private_ip" {
  value     = module.cloud_sql.private_ip_address
  sensitive = true
}

output "bigquery_table" {
  value = module.bigquery.fully_qualified_table
}

output "artifact_registry_url" {
  value = module.artifact_registry.repository_url
}

output "airflow_vm_internal_ip" {
  value     = module.airflow_vm.internal_ip
  sensitive = true
}
