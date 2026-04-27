variable "project_id" {
  type = string
}

variable "name_prefix" {
  type = string
}

variable "environment" {
  type = string
}

variable "region" {
  type = string
}

variable "zone" {
  type = string
}

variable "network_id" {
  type = string
}

variable "subnet_id" {
  type = string
}

variable "machine_type" {
  type    = string
  default = "e2-standard-2"
}

variable "data_disk_size_gb" {
  type    = number
  default = 50
}

variable "airflow_image" {
  type    = string
  default = "apache/airflow:2.9.2"
}

variable "bigquery_dataset" {
  type = string
}

variable "bigquery_table" {
  type = string
}

variable "dags_git_repo" {
  description = "Optional git repo URL where DAGs live; leave empty to skip clone"
  type        = string
  default     = ""
}

variable "dags_git_branch" {
  type    = string
  default = "main"
}
