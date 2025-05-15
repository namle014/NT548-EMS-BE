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

resource "aws_security_group_rule" "inbounds" {
  count       = length(var.allowed_ports)
  type        = "ingress"
  protocol    = "tcp"
  cidr_blocks = var.allowed_public_cidr
  from_port   = var.allowed_ports[count.index]
  to_port     = var.allowed_ports[count.index]

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
