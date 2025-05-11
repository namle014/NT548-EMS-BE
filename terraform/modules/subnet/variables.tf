variable "vpc_id" {
  type = string
}

variable "public_subnet_cidrs" {
  description = "List of public subnet CIDR blocks"
  type        = list(string)
}

variable "private_subnet_cidrs" {
  description = "List of private subnet CIDR blocks"
  type        = list(string)
}

variable "azs" {
  description = "List of availability zones"
  type        = list(string)
}

variable "public_rt" {
  type = string
}

variable "resource_prefix" {
  description = "Prefix for all resources created"
  type        = string
  default     = ""
}
