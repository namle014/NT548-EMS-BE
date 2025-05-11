#!/bin/bash

SG_ID="sg-011018fff9656bb13"
PORTS=(22 9000) # SSH và Sonarqube
MY_IP=$(curl -s https://checkip.amazonaws.com | tr -d '\n')

echo "Current IP: $MY_IP"

# Loại bỏ rule cũ (có thể xóa nếu có vấn đề)
for PORT in "${PORTS[@]}"; do
  aws ec2 revoke-security-group-ingress \
    --group-id "$SG_ID" \
    --protocol tcp \
    --port "$PORT" \
    --cidr 0.0.0.0/0 2>/dev/null
done

for PORT in "${PORTS[@]}"; do
  if aws ec2 authorize-security-group-ingress \
    --group-id "$SG_ID" \
    --protocol tcp \
    --port "$PORT" \
    --cidr "${MY_IP}/32"; then
    echo "Đã cập nhật rule ingress, cho phép ${MY_IP}/32 truy cập ở port $PORT"
  else
    echo "⚠️ Rule cho port $PORT có thể đã tồn tại hoặc thêm thất bại"
  fi
done
