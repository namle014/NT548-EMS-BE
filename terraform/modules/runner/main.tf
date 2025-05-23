data "aws_ami" "ubuntu_24_04" {
  most_recent = true
  owners      = ["099720109477"]

  filter {
    name   = "name"
    values = ["ubuntu/images/hvm-ssd-gp3/ubuntu-noble-24.04-amd64-server-*"]
  }

  filter {
    name   = "virtualization-type"
    values = ["hvm"]
  }
}

# resource "aws_iam_role" "ssm_role" {
#   name = "${var.resource_prefix}-ec2-ssm-role"

#   assume_role_policy = jsonencode({
#     Version = "2012-10-17",
#     Statement = [
#       {
#         Effect = "Allow",
#         Principal = {
#           Service = "ec2.amazonaws.com"
#         },
#         Action = "sts:AssumeRole"
#       }
#     ]
#   })
# }

# resource "aws_iam_role_policy_attachment" "ssm_attach" {
#   role       = aws_iam_role.ssm_role.name
#   policy_arn = "arn:aws:iam::aws:policy/AmazonSSMManagedInstanceCore"
# }

# resource "aws_iam_instance_profile" "ssm_instance_profile" {
#   name = "${var.resource_prefix}-ec2-ssm-profile"
#   role = aws_iam_role.ssm_role.name
# }

resource "aws_instance" "runner" {
  ami           = data.aws_ami.ubuntu_24_04.id
  instance_type = var.instance_type

  subnet_id              = var.subnet_id
  vpc_security_group_ids = var.sg_ids
  key_name               = var.key_pair

  # iam_instance_profile = aws_iam_instance_profile.ssm_instance_profile.name

  root_block_device {
    volume_size           = 50
    volume_type           = "gp3"
    delete_on_termination = false # Giữ lại volume nếu instance bị terminate
  }

  tags = {
    Name = "${var.resource_prefix}-runner"
  }
}

resource "aws_eip" "runner_ip" {
  instance = aws_instance.runner.id
  domain   = "vpc"
}
