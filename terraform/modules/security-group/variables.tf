variable "vpc_id" {
  type = string
}

variable "allowed_public_cidr" {
  type = list(string)
}

variable "allowed_ports" {
  type = list(string)
}

variable "resource_prefix" {
  description = "Prefix for all resources created"
  type        = string
  default     = ""
}
