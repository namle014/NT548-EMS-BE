terraform {
  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 5.0"
    }
  }
}

module "vpc" {
  source = "./modules/vpc"

  vpc_cidr = var.vpc_cidr

  resource_prefix = var.resource_prefix
}

module "gw" {
  source = "./modules/gateway"

  vpc_id = module.vpc.vpc_id

  resource_prefix = var.resource_prefix
}

module "rt" {
  source = "./modules/route-table"

  vpc_id = module.vpc.vpc_id
  igw_id = module.gw.igw_id

  resource_prefix = var.resource_prefix
}

module "subnet" {
  source = "./modules/subnet"

  vpc_id               = module.vpc.vpc_id
  private_subnet_cidrs = var.private_subnet_cidrs
  public_subnet_cidrs  = var.public_subnet_cidrs
  azs                  = var.azs
  public_rt            = module.rt.public_rt_id

  resource_prefix = var.resource_prefix
}

module "sg" {
  source = "./modules/security-group"

  vpc_id              = module.vpc.vpc_id
  allowed_ports       = var.allowed_ports
  allowed_public_cidr = var.allowed_public_cidr

  resource_prefix = var.resource_prefix
}

module "runner" {
  source     = "./modules/runner"
  depends_on = [module.gw]

  instance_type = var.instance_type
  key_pair      = var.key_pair
  sg_ids        = [module.sg.public_sg_id]
  subnet_id     = module.subnet.public_subnet_ids[0]

  resource_prefix = var.resource_prefix
}

module "rds" {
  source = "./modules/rds"

  vpc_id         = module.vpc.vpc_id
  db_subnets     = slice(module.subnet.private_subnet_ids, 0, 2)
  db_access_cidr = [var.vpc_cidr]
}

module "static_host" {
  source = "./modules/static-host"
}