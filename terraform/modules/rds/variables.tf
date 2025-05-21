variable "vpc_id" {
  type = string
}

variable "db_subnets" {
  type = list(string)
}

variable "db_access_cidr" {
  type = list(string)
}

variable "resource_prefix" {
  description = "Prefix for all resources created"
  type        = string
  default     = ""
}
