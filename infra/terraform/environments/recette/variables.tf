variable "project_id" {
  description = "GCP project ID for the dev environment"
  type        = string
}

variable "region" {
  type    = string
  default = "europe-west1"
}

variable "zone" {
  type    = string
  default = "europe-west1-b"
}

variable "bigquery_location" {
  type    = string
  default = "EU"
}

variable "master_authorized_cidrs" {
  type = list(object({
    cidr = string
    name = string
  }))
  default = []
}

variable "dags_git_repo" {
  type    = string
  default = ""
}
