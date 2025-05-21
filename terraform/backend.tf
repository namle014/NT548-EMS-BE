terraform {
  backend "s3" {
    bucket  = "nt548-25"
    key     = "env/terraform.tfstate"
    region  = "ap-southeast-2"
    encrypt = true
  }
}
