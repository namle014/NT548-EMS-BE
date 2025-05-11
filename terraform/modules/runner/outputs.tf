output "public_ip" {
  value = aws_eip.runner_ip.public_ip
}
