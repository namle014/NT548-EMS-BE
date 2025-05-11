resource "aws_route_table" "public" {
  vpc_id = var.vpc_id

  tags = {
    Name = "${var.resource_prefix}-public-rt"
  }
}

resource "aws_route" "public_internet" {
  route_table_id         = aws_route_table.public.id
  destination_cidr_block = "0.0.0.0/0"
  gateway_id             = var.igw_id
}
