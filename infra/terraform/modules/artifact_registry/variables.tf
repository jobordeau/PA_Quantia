variable "name_prefix" {
  type = string
}

variable "environment" {
  type = string
}

variable "region" {
  type = string
}

variable "keep_versions" {
  type    = number
  default = 10
}
