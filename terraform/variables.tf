variable "vpc_cidr" {
  description = "CIDR block for VPC"
  type        = string
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

variable "allowed_ports" {
  type = list(string)
}

variable "allowed_public_cidr" {
  type = list(string)
}

variable "instance_type" {
  description = "Instance type for EC2 instances"
  type        = string
}

variable "key_pair" {
  type = string
}

variable "resource_prefix" {
  description = "Prefix for all resources created"
  type        = string
  default     = ""
}
