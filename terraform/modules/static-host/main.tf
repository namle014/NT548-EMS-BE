resource "random_id" "suffix" {
  byte_length = 4
}

resource "aws_s3_bucket" "nextjs_site" {
  bucket = "fe-deploy-${random_id.suffix.hex}"

  force_destroy = true
}

resource "aws_s3_bucket_website_configuration" "this" {
  bucket = aws_s3_bucket.nextjs_site.id

  index_document {
    suffix = "index.html"
  }

  error_document {
    key = "index.html"
  }
}

resource "aws_s3_bucket_public_access_block" "no_block_public_policy" {
  bucket = aws_s3_bucket.nextjs_site.id

  block_public_acls       = false
  block_public_policy     = false
  ignore_public_acls      = false
  restrict_public_buckets = false
}

data "aws_iam_policy_document" "policy_public_access" {
  statement {
    sid    = "PublicReadGetObject"
    effect = "Allow"
    principals {
      type        = "*"
      identifiers = ["*"]
    }
    actions = ["s3:GetObject"]
    resources = [
      aws_s3_bucket.nextjs_site.arn,
      "${aws_s3_bucket.nextjs_site.arn}/*"
    ]
  }
}

resource "aws_s3_bucket_policy" "public_access" {
  bucket = aws_s3_bucket.nextjs_site.id
  policy = data.aws_iam_policy_document.policy_public_access.json

  depends_on = [aws_s3_bucket_public_access_block.no_block_public_policy]
}

resource "aws_cloudfront_distribution" "cdn" {
  origin {
    domain_name = aws_s3_bucket_website_configuration.this.website_endpoint
    origin_id   = "S3-nextjs"

    custom_origin_config {
      http_port              = 80
      https_port             = 443
      origin_protocol_policy = "http-only"
      origin_ssl_protocols   = ["TLSv1.2"]
    }
  }

  enabled             = true
  is_ipv6_enabled     = true
  default_root_object = "index.html"

  default_cache_behavior {
    allowed_methods  = ["GET", "HEAD"]
    cached_methods   = ["GET", "HEAD"]
    target_origin_id = "S3-nextjs"

    viewer_protocol_policy = "redirect-to-https"

    forwarded_values {
      query_string = false

      cookies {
        forward = "none"
      }
    }
  }

  viewer_certificate {
    cloudfront_default_certificate = true
  }

  restrictions {
    geo_restriction {
      restriction_type = "none"
    }
  }

  tags = {
    Name = "${var.resource_prefix}-StaticSite"
  }
}

resource "aws_iam_user" "github_actions" {
  name = "github-actions-deploy"
}

resource "aws_iam_access_key" "github_actions" {
  user = aws_iam_user.github_actions.name
}

resource "aws_iam_user_policy" "github_actions_policy" {
  name = "github-actions-deploy-policy"
  user = aws_iam_user.github_actions.name

  policy = jsonencode({
    Version = "2012-10-17",
    Statement = [
      {
        Effect = "Allow",
        Action = [
          "s3:PutObject",
          "s3:PutObjectAcl",
          "s3:GetObject",
          "s3:DeleteObject",
          "s3:ListBucket"
        ],
        Resource = [
          aws_s3_bucket.nextjs_site.arn,
          "${aws_s3_bucket.nextjs_site.arn}/*"
        ]
      },
      {
        Effect = "Allow",
        Action = "cloudfront:CreateInvalidation",
        Resource = "*"
      }
    ]
  })
}
