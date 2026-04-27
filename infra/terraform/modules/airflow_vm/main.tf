resource "google_service_account" "airflow" {
  account_id   = "${var.name_prefix}-airflow"
  display_name = "Service account for the Airflow VM"
}

resource "google_project_iam_member" "airflow_bq_user" {
  project = var.project_id
  role    = "roles/bigquery.jobUser"
  member  = "serviceAccount:${google_service_account.airflow.email}"
}

resource "google_project_iam_member" "airflow_bq_data_editor" {
  project = var.project_id
  role    = "roles/bigquery.dataEditor"
  member  = "serviceAccount:${google_service_account.airflow.email}"
}

resource "google_project_iam_member" "airflow_secret_accessor" {
  project = var.project_id
  role    = "roles/secretmanager.secretAccessor"
  member  = "serviceAccount:${google_service_account.airflow.email}"
}

resource "google_project_iam_member" "airflow_log_writer" {
  project = var.project_id
  role    = "roles/logging.logWriter"
  member  = "serviceAccount:${google_service_account.airflow.email}"
}

resource "google_compute_disk" "airflow_data" {
  name = "${var.name_prefix}-airflow-data"
  type = "pd-balanced"
  zone = var.zone
  size = var.data_disk_size_gb
}

resource "google_compute_instance" "airflow" {
  name         = "${var.name_prefix}-airflow"
  machine_type = var.machine_type
  zone         = var.zone

  tags = ["allow-iap-ssh", "airflow"]

  boot_disk {
    initialize_params {
      image = "projects/cos-cloud/global/images/family/cos-stable"
      size  = 30
      type  = "pd-balanced"
    }
  }

  attached_disk {
    source      = google_compute_disk.airflow_data.id
    device_name = "airflow-data"
    mode        = "READ_WRITE"
  }

  network_interface {
    network    = var.network_id
    subnetwork = var.subnet_id
  }

  service_account {
    email  = google_service_account.airflow.email
    scopes = ["cloud-platform"]
  }

  metadata = {
    enable-oslogin           = "TRUE"
    google-logging-enabled   = "true"
    google-monitoring-enabled = "true"
  }

  metadata_startup_script = templatefile("${path.module}/startup.sh.tftpl", {
    airflow_image          = var.airflow_image
    bigquery_project       = var.project_id
    bigquery_dataset       = var.bigquery_dataset
    bigquery_table         = var.bigquery_table
    git_repo_url           = var.dags_git_repo
    git_branch             = var.dags_git_branch
    environment            = var.environment
  })

  allow_stopping_for_update = true
}
