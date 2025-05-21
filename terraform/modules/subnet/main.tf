resource "aws_subnet" "public" {
  vpc_id                  = var.vpc_id
  count                   = length(var.public_subnet_cidrs)
  cidr_block              = var.public_subnet_cidrs[count.index]
  availability_zone       = var.azs[0]
  map_public_ip_on_launch = true # EC2 trong subnet này tự động có public IP

  tags = {
    Name = "${var.resource_prefix}-public-subnet-${count.index + 1}"
  }
}

resource "aws_subnet" "private" {
  vpc_id            = var.vpc_id
  count             = length(var.private_subnet_cidrs)
  cidr_block        = var.private_subnet_cidrs[count.index]
  # availability_zone = var.azs[0]
  availability_zone = var.azs[count.index]

  tags = {
    Name = "${var.resource_prefix}-private-subnet-${count.index + 1}"
  }
}

resource "aws_route_table_association" "public" {
  count          = length(aws_subnet.public)
  route_table_id = var.public_rt
  subnet_id      = aws_subnet.public[count.index].id
}
