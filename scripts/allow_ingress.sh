#!/bin/bash

SG_ID="sg-08820e2b38514b7ad"
PORTS=(1433) # SSH
MY_IP=$(curl -s https://checkip.amazonaws.com | tr -d '\n')

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
