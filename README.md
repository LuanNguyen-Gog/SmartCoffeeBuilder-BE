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
4. FE quay về `ReturnUrl`/`CancelUrl`, polling `GET /api/payments/status?orderCode=...` đến khi `isFinal = true`; nếu user huỷ thì gọi `POST /api/payments/cancel?orderCode=...`.
5. Sau khi deploy, admin đăng ký webhook một lần: `POST /api/payments/webhook/confirm` với body `{ "webhookUrl": "https://<api>/api/payments/webhook" }`.

Job Hangfire `expire-subscriptions` chạy mỗi giờ: chuyển gói `active` quá hạn sang `expired`
và huỷ các giao dịch `pending` đã quá hạn link thanh toán.
