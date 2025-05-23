output "vpc_id" {
  value = module.vpc.vpc_id
}

output "runner_public_id" {
  value = module.runner.public_ip
}

output "rds_endpoint" {
  value = module.rds.rds_endpoint
}

output "static_site_bucket" {
  value = module.static_host.bucket_name
}

output "access_key_id" {
  value     = module.static_host.aws_access_key_id
  sensitive = true
}

output "secret_access_key" {
  value     = module.static_host.aws_secret_access_key
  sensitive = true
}
