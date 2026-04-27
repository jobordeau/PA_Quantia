variable "name_prefix" {
  type = string
}

variable "region" {
  type = string
}

variable "network_id" {
  type = string
}

variable "tier" {
  description = "Cloud SQL machine tier (e.g. db-f1-micro, db-custom-2-7680)"
  type        = string
  default     = "db-f1-micro"
}

variable "availability_type" {
  description = "ZONAL or REGIONAL"
  type        = string
  default     = "ZONAL"
}

variable "disk_size_gb" {
  type    = number
  default = 10
}

variable "database_name" {
  type    = string
  default = "quantia"
}

variable "app_user" {
  type    = string
  default = "quantia"
}

variable "deletion_protection" {
  type    = bool
  default = false
}

variable "point_in_time_recovery" {
  type    = bool
  default = false
}

variable "backup_retention_days" {
  type    = number
  default = 7
}
