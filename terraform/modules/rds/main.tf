# Lấy secret từ AWS Secrets Manager
data "aws_secretsmanager_secret_version" "rds_secret" {
  secret_id = "rds/sqlserver/admin"
}

resource "aws_db_subnet_group" "rds_subnet_group" {
  name       = "rds-subnet-group"
  subnet_ids = var.db_subnets

  tags = {
    Name = "rds-subnet-group"
  }
}

resource "aws_security_group" "rds_sg" {
  name        = "rds-sg"
  description = "Allow SQL Server access"
  vpc_id      = var.vpc_id

  lifecycle {
    ignore_changes = [ingress] # Cho phép tự thay đổi source ip
  }

  ingress {
    from_port   = 1433
    to_port     = 1433
    protocol    = "tcp"
    cidr_blocks = var.db_access_cidr
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
  identifier        = "database-ems"
  allocated_storage = 20
  storage_type      = "gp2"
  engine            = "sqlserver-ex"
  engine_version    = "14.00.3381.3.v1"
  instance_class    = "db.t3.micro"

  username = jsondecode(data.aws_secretsmanager_secret_version.rds_secret.secret_string)["username"]
  password = jsondecode(data.aws_secretsmanager_secret_version.rds_secret.secret_string)["password"]

  db_subnet_group_name   = aws_db_subnet_group.rds_subnet_group.name
  vpc_security_group_ids = [aws_security_group.rds_sg.id]
  publicly_accessible    = true
  skip_final_snapshot    = true
  deletion_protection    = false
  multi_az               = false

  tags = {
    Name = "${var.resource_prefix}-database-ems"
  }
}
