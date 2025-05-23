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
