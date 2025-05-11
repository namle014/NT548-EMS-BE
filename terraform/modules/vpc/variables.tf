variable "vpc_cidr" {
  description = "CIDR block for VPC"
  type        = string
}

variable "resource_prefix" {
  description = "Prefix for all resources created"
  type        = string
  default     = ""
}
