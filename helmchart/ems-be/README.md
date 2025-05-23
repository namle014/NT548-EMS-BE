# ems-be Helm Chart

Helm chart này dùng để triển khai ứng dụng EMS .NET backend lên EKS hoặc bất kỳ Kubernetes cluster nào.

## Cấu trúc chart
- `Chart.yaml`: Thông tin chart
- `values.yaml`: Giá trị cấu hình mặc định
- `templates/`: Các manifest Kubernetes (Deployment, Service, Ingress...)

## Cách sử dụng

1. Build và push Docker image của bạn lên registry:
   ```sh
   docker build -t your-docker-repo/ems-be:latest ./src
   docker push your-docker-repo/ems-be:latest
   ```
2. Cập nhật `values.yaml`:
   - Sửa `image.repository` thành repo của bạn
   - Sửa `env.ConnectionStrings__DefaultConnection` nếu cần
3. Cài đặt chart lên cluster:
   ```sh
   helm install ems-be ./helmchart/ems-be -n your-namespace
   ```
4. (Tùy chọn) Bật ingress bằng cách sửa `values.yaml`:
   ```yaml
   ingress:
     enabled: true
     host: your-domain.com
   ```

## Tham số quan trọng
- `image.repository`, `image.tag`: Docker image
- `env.ConnectionStrings__DefaultConnection`: Chuỗi kết nối tới RDS
- `service.type`: ClusterIP/LoadBalancer
- `ingress.enabled`, `ingress.host`: Bật/tắt và cấu hình ingress

## Lưu ý bảo mật
- Nên dùng Kubernetes Secret cho password DB ở môi trường production. 