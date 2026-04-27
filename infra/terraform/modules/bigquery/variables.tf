variable "project_id" {
  type = string
}

variable "name_prefix" {
  type = string
}

variable "environment" {
  type = string
}

variable "dataset_id" {
  type    = string
  default = "quantia_market"
}

variable "location" {
  type    = string
  default = "EU"
}

variable "partition_expiration_ms" {
  description = "Per-partition expiration; null for permanent (prod)"
  type        = number
  default     = null
}

variable "deletion_protection" {
  type    = bool
  default = false
}
