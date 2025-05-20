provider "aws" {
  region = "us-east-1"
}

# Lấy secret từ AWS Secrets Manager
data "aws_secretsmanager_secret_version" "rds_secret" {
  secret_id = "rds/sqlserver/admin" 
}

resource "aws_db_subnet_group" "rds_subnet_group" {
  name       = "rds-subnet-group"
  subnet_ids = [
    "subnet-093a82cbeb36f63e9", # us-east-1e
    "subnet-0185d4fc09c3ea1e4"  # us-east-1b
  ]

  tags = {
    Name = "rds-subnet-group"
  }
}

resource "aws_security_group" "rds_sg" {
  name        = "rds-sg"
  description = "Allow SQL Server access"
  vpc_id      = "vpc-0d48c675588d017ab"

  ingress {
    from_port   = 1433
    to_port     = 1433
    protocol    = "tcp"
    cidr_blocks = ["14.186.37.9/32"]
  }

  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }

  tags = {
    Name = "rds-sg"
  }
}

resource "aws_db_instance" "sql_server_rds" {
  identifier              = "database-ems"
  allocated_storage       = 20
  storage_type            = "gp2"
  engine                  = "sqlserver-ex"
  engine_version          = "14.00.3381.3.v1"
  instance_class          = "db.t3.micro"

  username                = jsondecode(data.aws_secretsmanager_secret_version.rds_secret.secret_string)["username"]
  password                = jsondecode(data.aws_secretsmanager_secret_version.rds_secret.secret_string)["password"]

  db_subnet_group_name    = aws_db_subnet_group.rds_subnet_group.name
  vpc_security_group_ids  = [aws_security_group.rds_sg.id]
  publicly_accessible     = true
  skip_final_snapshot     = true
  deletion_protection     = false
  multi_az                = false

  tags = {
    Name = "database-ems"
  }
}

output "rds_endpoint" {
  value = aws_db_instance.sql_server_rds.endpoint
}
