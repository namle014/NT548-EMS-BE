variable "instance_type" {
  type = string
}

variable "subnet_id" {
  type = string
}

variable "sg_ids" {
  type = list(string)
}

variable "key_pair" {
  type = string
}

variable "resource_prefix" {
  description = "Prefix for all resources created"
  type        = string
  default     = ""
}
