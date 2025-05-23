output "bucket_name" {
  value = aws_s3_bucket.nextjs_site.bucket
}

output "cloudfront_domain_name" {
  value = aws_cloudfront_distribution.cdn.domain_name
}

output "aws_access_key_id" {
  value       = aws_iam_access_key.github_actions.id
  description = "AWS Access Key ID"
  sensitive   = true
}

output "aws_secret_access_key" {
  value       = aws_iam_access_key.github_actions.secret
  description = "AWS Secret Access Key"
  sensitive   = true
}
