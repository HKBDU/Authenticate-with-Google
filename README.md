# AuthGG

Ứng dụng đăng nhập Google với React + ASP.NET Core. Flow hiện tại dùng OAuth 2.0 redirect ở backend:

1. React chuyển trình duyệt đến `GET /api/auth/google/login`.
2. ASP.NET Core chuyển người dùng sang Google.
3. Google callback về `/api/auth/google/callback`.
4. Backend xác thực code, lưu user in-memory và tạo HttpOnly cookie.
5. Backend redirect về `http://localhost:5173/auth/callback`.
6. React gọi `/api/auth/me` để lấy user hiện tại.

## Cấu hình Google Cloud

Đã cấu hình cho local development:

- Authorized JavaScript origins: `http://localhost:5173`
- Authorized redirect URI: `http://localhost:8000/api/auth/google/callback/`

Client Secret chỉ được đặt ở backend, không đưa vào React hoặc commit vào source.

## Chạy backend

PowerShell:

```powershell
$env:Google__ClientSecret = "YOUR_GOOGLE_CLIENT_SECRET"
dotnet restore .\AuthGG.Api\AuthGG.Api.csproj
dotnet run --project .\AuthGG.Api
```

Backend chạy tại `http://localhost:8000`.

## Chạy frontend

```powershell
cd .\AuthGG.Web
npm.cmd install
npm.cmd run dev
```

Frontend chạy tại `http://localhost:5173`.

## Ghi chú

- User hiện được lưu trong `UserStore` bằng memory; restart backend sẽ xóa dữ liệu.
- Session dùng cookie HttpOnly nên Client Secret và thông tin phiên không nằm trong localStorage.
- Khi production, thay `UserStore` bằng database, dùng HTTPS và cấu hình cookie `Secure`.
