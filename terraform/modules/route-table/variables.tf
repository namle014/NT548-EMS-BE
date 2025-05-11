variable "vpc_id" {
  type = string
}

variable "igw_id" {
  type = string
}

variable "resource_prefix" {
  description = "Prefix for all resources created"
  type        = string
  default     = ""
}
