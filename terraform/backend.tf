terraform {
  backend "s3" {
    bucket  = "nt548-tfbe"
    key     = "env/terraform.tfstate"
    region  = "us-east-1"
    encrypt = true
  }
}
