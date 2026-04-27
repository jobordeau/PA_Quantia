terraform {
  required_version = ">= 1.5.0"

  required_providers {
    google = {
      source  = "hashicorp/google"
      version = "~> 5.30"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }

  backend "gcs" {
    bucket = "quantia-tfstate-dev"
    prefix = "terraform/state"
  }
}

provider "google" {
  project = var.project_id
  region  = var.region
}

locals {
  environment = "dev"
  name_prefix = "quantia-${local.environment}"
}

module "vpc" {
  source      = "../../modules/vpc"
  name_prefix = local.name_prefix
  region      = var.region
}

module "artifact_registry" {
  source      = "../../modules/artifact_registry"
  name_prefix = local.name_prefix
  environment = local.environment
  region      = var.region
}

module "bigquery" {
  source              = "../../modules/bigquery"
  project_id          = var.project_id
  name_prefix         = local.name_prefix
  environment         = local.environment
  dataset_id          = "quantia_market_dev"
  location            = var.bigquery_location
  deletion_protection = false
}

module "cloud_sql" {
  source              = "../../modules/cloud_sql"
  name_prefix         = local.name_prefix
  region              = var.region
  network_id          = module.vpc.network_id
  tier                = "db-f1-micro"
  availability_type   = "ZONAL"
  disk_size_gb        = 10
  deletion_protection = false
}

module "gke" {
  source                  = "../../modules/gke"
  project_id              = var.project_id
  name_prefix             = local.name_prefix
  environment             = local.environment
  region                  = var.region
  network_id              = module.vpc.network_id
  subnet_id               = module.vpc.subnet_id
  pods_range_name         = module.vpc.pods_range_name
  services_range_name     = module.vpc.services_range_name
  machine_type            = "e2-standard-2"
  node_count_per_zone     = 1
  min_node_count          = 1
  max_node_count          = 2
  deletion_protection     = false
  master_authorized_cidrs = var.master_authorized_cidrs
}

module "airflow_vm" {
  source           = "../../modules/airflow_vm"
  project_id       = var.project_id
  name_prefix      = local.name_prefix
  environment      = local.environment
  region           = var.region
  zone             = var.zone
  network_id       = module.vpc.network_id
  subnet_id        = module.vpc.subnet_id
  machine_type     = "e2-standard-2"
  bigquery_dataset = module.bigquery.dataset_id
  bigquery_table   = module.bigquery.table_id
  dags_git_repo    = var.dags_git_repo
}
