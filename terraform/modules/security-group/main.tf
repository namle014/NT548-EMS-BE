resource "aws_security_group" "public" {
  name   = "public"
  vpc_id = var.vpc_id

  lifecycle {
    ignore_changes = [ingress] # Cho phép tự thay đổi source ip
  }

  tags = {
    Name = "${var.resource_prefix}-public-sg"
  }
}

# Truy cap Sonarqube
resource "aws_security_group_rule" "inbound_allow_9000" {
  type        = "ingress"
  from_port   = 9000
  to_port     = 9000
  protocol    = "tcp"
  cidr_blocks = var.allowed_public_cidr

  security_group_id = aws_security_group.public.id
}

resource "aws_security_group_rule" "inbound_allow_22" {
  type        = "ingress"
  from_port   = 22
  to_port     = 22
  protocol    = "tcp"
  cidr_blocks = var.allowed_public_cidr

  security_group_id = aws_security_group.public.id
}

resource "aws_security_group_rule" "outbound_allow_all" {
  type        = "egress"
  from_port   = 0
  to_port     = 65535
  protocol    = "-1"
  cidr_blocks = ["0.0.0.0/0"]

  security_group_id = aws_security_group.public.id
}
