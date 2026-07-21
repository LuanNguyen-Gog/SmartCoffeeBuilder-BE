# SmartCoffeeBuilder-BE
Backend API for Smart Coffee Builder Capstone Project

## Thanh toán phí nền tảng (payOS)

Nguồn thu của platform gồm 2 luồng, đều qua [payOS](https://payos.vn):

1. **Subscription** — chủ quán (`owner`) và nhà cung cấp (`provider`) mua gói theo tháng/năm.
   Chủ quán gói **free KHÔNG dùng được AI design** (`POST /api/ai-recommendations` trả 409);
   có subscription `active` còn hạn thì được mở khoá.
2. **Đẩy bài (post boost)** — chủ quán trả phí theo ngày để bài đăng tuyển provider
   được ghim nổi bật lên đầu danh sách `GET /api/posts` (`isBoosted`/`boostedUntil` trong response).

### Cấu hình (appsettings / biến môi trường)

```json
{
  "PayOs": {
    "ClientId": "<payOS client id>",
    "ApiKey": "<payOS api key>",
    "ChecksumKey": "<payOS checksum key>",
    "ReturnUrl": "https://<frontend>/payment/success",
    "CancelUrl": "https://<frontend>/payment/cancel",
    "ExpirationSeconds": 900,
    "PostBoostPricePerDay": 20000
  }
}
```

`PostBoostPricePerDay` (VND/ngày, mặc định 20.000) là đơn giá đẩy bài — tổng tiền = đơn giá × số ngày (1–90).

### Luồng thanh toán

1. FE gọi `GET /api/payments/plans` để hiển thị các gói.
2. User chọn gói → FE gọi `POST /api/payments/subscriptions` (JWT) → nhận `checkoutUrl`/`qrCode` và redirect sang payOS.
   Với đẩy bài: `POST /api/payments/post-boosts` body `{ "postId": ..., "days": ... }` (chỉ chủ quán sở hữu bài đăng đang `open`).
3. payOS gọi `POST /api/payments/webhook` (đã verify chữ ký) → hệ thống kích hoạt subscription (đang có gói active thì cộng nối tiếp từ ngày hết hạn) hoặc cộng ngày boost vào `boostedUntil` của bài đăng.
4. FE quay về `ReturnUrl`/`CancelUrl`, polling `GET /api/payments/status?orderCode=...` (JWT) đến khi `isFinal = true`; nếu user huỷ thì gọi `POST /api/payments/cancel?orderCode=...` (JWT).
5. Sau khi deploy, admin đăng ký webhook một lần: `POST /api/payments/webhook/confirm` với body `{ "webhookUrl": "https://<api>/api/payments/webhook" }`.

Job Hangfire `expire-subscriptions` chạy mỗi giờ: chuyển gói `active` quá hạn sang `expired`
và huỷ các giao dịch `pending` đã quá hạn link thanh toán.

### Idempotency & bảo mật

- **Tái sử dụng link còn hạn**: `POST /subscriptions` và `POST /post-boosts` kiểm tra trước xem
  account đã có giao dịch `pending` chưa hết hạn (`PayOs:ExpirationSeconds`) cho đúng gói/đúng bài
  đăng đó chưa — nếu có thì trả lại `checkoutUrl`/`qrCode` cũ thay vì tạo giao dịch/đơn payOS mới.
  Chống việc bấm "Thanh toán" nhiều lần (double-click, mất mạng bấm lại...) sinh ra nhiều mã cho
  cùng một nhu cầu thanh toán.
- **Huỷ giao dịch huỷ cả link payOS**: `POST /cancel` gọi `payOS.cancelPaymentLink` thật, không chỉ
  đổi trạng thái nội bộ — tránh trường hợp user bấm huỷ nhưng link cũ vẫn quét trả tiền được, khiến
  tiền bị trừ mà hệ thống không kích hoạt gì (vì webhook bị bỏ qua do giao dịch đã "cancelled").
- **Chống webhook xử lý trùng**: payOS có thể gửi lại webhook (retry) cho cùng một giao dịch. Việc
  chuyển trạng thái `pending → paid/failed/cancelled` luôn đi qua một câu `UPDATE ... WHERE status =
  'pending'` atomic ở tầng DB — chỉ đúng một lệnh gọi "thắng" quyền xử lý (kích hoạt subscription /
  cộng ngày boost), các lệnh gọi trùng sau đó tự nhận biết "đã xử lý trước đó" mà không cộng dồn.
- **`status`/`cancel` yêu cầu JWT + kiểm tra chủ sở hữu**: `orderCode` chỉ là timestamp mili-giây nên
  dễ đoán — nếu để anonymous, người khác có thể dò xem hoặc huỷ giao dịch của tài khoản khác.
