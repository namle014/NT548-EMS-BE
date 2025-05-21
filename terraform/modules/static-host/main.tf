resource "aws_s3_bucket" "nextjs_site" {
  bucket = "my-nextjs-static-site"

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
