# Tài liệu tham khảo cũ

Flow trong tài liệu này đã được thay thế. Source hiện tại dùng ASP.NET Core Google OAuth redirect và HttpOnly cookie; xem `README.md` để chạy đúng phiên bản mới.

# Đăng nhập Google với React + .NET C#

Luồng hoạt động: React lấy **ID Token** từ Google → gửi lên backend .NET → backend xác thực token đó với Google → backend phát hành **JWT riêng** của hệ thống để dùng cho các API sau này.

---

## 1. Tạo Google Client ID

1. Vào https://console.cloud.google.com/apis/credentials
2. Tạo project mới (nếu chưa có) → **Create Credentials → OAuth client ID**
3. Application type: **Web application**
4. Authorized JavaScript origins: thêm `http://localhost:5173` (hoặc port React của bạn)
5. Lấy **Client ID** dạng: `xxxxxx.apps.googleusercontent.com`
6. Dán Client ID này vào 2 chỗ:
   - `GOOGLE_CLIENT_ID` trong file `GoogleLoginButton.jsx` (frontend)
   - `Google:ClientId` trong file `appsettings.json` (backend)

---

## 2. Cài đặt Backend (.NET)

```bash
dotnet new webapi -n GoogleAuthDemo
cd GoogleAuthDemo
dotnet add package Google.Apis.Auth
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

Tạo các file bên dưới trong project (đặt `AuthController.cs` trong thư mục `Controllers/`, `GoogleLoginRequest.cs` trong thư mục `Models/`), rồi chạy:

```bash
dotnet run
```

### `Models/GoogleLoginRequest.cs`

```csharp
namespace GoogleAuthDemo.Models
{
    // Frontend sẽ gửi ID Token lấy được từ Google lên đây
    public class GoogleLoginRequest
    {
        public string IdToken { get; set; } = string.Empty;
    }
}
```

### `Controllers/AuthController.cs`

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using GoogleAuthDemo.Models;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace GoogleAuthDemo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            if (string.IsNullOrEmpty(request.IdToken))
                return BadRequest(new { message = "Thiếu IdToken" });

            GoogleJsonWebSignature.Payload payload;
            try
            {
                // Xác thực ID Token với Google (kiểm tra chữ ký, audience, hạn sử dụng...)
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience = new[] { _config["Google:ClientId"] }
                };
                payload = await GoogleJsonWebSignature.ValidateAsync(request.IdToken, settings);
            }
            catch (InvalidJwtException)
            {
                return Unauthorized(new { message = "Google ID Token không hợp lệ" });
            }

            // TODO: Ở đây bạn nên tìm/tạo user trong database dựa vào payload.Email hoặc payload.Subject
            // var user = await _userService.FindOrCreateByGoogleAsync(payload);

            // Phát hành JWT riêng của hệ thống bạn
            var token = GenerateJwtToken(payload.Email, payload.Name, payload.Subject);

            return Ok(new
            {
                token,
                user = new
                {
                    email = payload.Email,
                    name = payload.Name,
                    picture = payload.Picture
                }
            });
        }

        private string GenerateJwtToken(string email, string name, string googleUserId)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, googleUserId),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim("name", name),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
```

### `Program.cs`

```csharp
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Cho phép React (chạy ở port khác) gọi API này
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173") // đổi theo port React của bạn (Vite mặc định 5173, CRA là 3000)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Cấu hình xác thực bằng JWT do chính backend phát hành
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

### `appsettings.json`

```json
{
  "Google": {
    "ClientId": "DÁN_GOOGLE_CLIENT_ID_CỦA_BẠN_VÀO_ĐÂY"
  },
  "Jwt": {
    "Key": "day-la-chuoi-bi-mat-it-nhat-32-ky-tu-doi-cai-nay-nhe",
    "Issuer": "GoogleAuthDemo",
    "Audience": "GoogleAuthDemoUsers"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

---

## 3. Cài đặt Frontend (React)

```bash
npm create vite@latest my-app -- --template react
cd my-app
npm install @react-oauth/google
```

Tạo file `src/GoogleLoginButton.jsx`:

```jsx
import { GoogleLogin, GoogleOAuthProvider } from '@react-oauth/google';
import { useState } from 'react';

// Đặt Client ID của bạn vào đây (hoặc đọc từ biến môi trường VITE_GOOGLE_CLIENT_ID)
const GOOGLE_CLIENT_ID = 'DÁN_GOOGLE_CLIENT_ID_CỦA_BẠN_VÀO_ĐÂY';
const API_BASE_URL = 'https://localhost:5001'; // đổi theo port backend .NET của bạn

function LoginInner() {
  const [user, setUser] = useState(null);
  const [error, setError] = useState('');

  const handleSuccess = async (credentialResponse) => {
    setError('');
    try {
      // credentialResponse.credential chính là Google ID Token
      const res = await fetch(`${API_BASE_URL}/api/auth/google-login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ idToken: credentialResponse.credential }),
      });

      if (!res.ok) {
        throw new Error('Đăng nhập thất bại');
      }

      const data = await res.json();

      // Lưu JWT do backend phát hành để dùng cho các API sau này
      localStorage.setItem('accessToken', data.token);
      setUser(data.user);
    } catch (err) {
      setError(err.message);
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('accessToken');
    setUser(null);
  };

  if (user) {
    return (
      <div>
        <img src={user.picture} alt={user.name} style={{ width: 40, borderRadius: '50%' }} />
        <p>Xin chào, {user.name} ({user.email})</p>
        <button onClick={handleLogout}>Đăng xuất</button>
      </div>
    );
  }

  return (
    <div>
      <GoogleLogin
        onSuccess={handleSuccess}
        onError={() => setError('Đăng nhập Google thất bại')}
      />
      {error && <p style={{ color: 'red' }}>{error}</p>}
    </div>
  );
}

// Bọc ngoài bằng GoogleOAuthProvider để dùng được component GoogleLogin
export default function GoogleLoginButton() {
  return (
    <GoogleOAuthProvider clientId={GOOGLE_CLIENT_ID}>
      <LoginInner />
    </GoogleOAuthProvider>
  );
}
```

Dùng trong `App.jsx`:

```jsx
import GoogleLoginButton from './GoogleLoginButton';

function App() {
  return (
    <div>
      <h1>Demo đăng nhập Google</h1>
      <GoogleLoginButton />
    </div>
  );
}

export default App;
```

Chạy:

```bash
npm run dev
```

---

## 4. Luồng hoạt động chi tiết

1. User bấm nút Google Login trên React.
2. Google trả về **ID Token** (JWT do Google ký) cho frontend.
3. Frontend gửi ID Token này lên `/api/auth/google-login` của backend.
4. Backend dùng thư viện `Google.Apis.Auth` để xác thực token (kiểm tra chữ ký, audience, hạn dùng) với server Google.
5. Nếu hợp lệ, backend lấy email/tên/ảnh từ token, tìm hoặc tạo user trong database, rồi phát hành **JWT riêng** của hệ thống.
6. Frontend lưu JWT này (ví dụ `localStorage`) và gửi kèm header `Authorization: Bearer <token>` cho các API cần đăng nhập sau đó.

---

## 5. Những điểm cần bạn tự bổ sung

- Phần `// TODO` trong `AuthController.cs`: lưu user vào database (EF Core, Dapper...).
- Đổi `Jwt:Key` trong `appsettings.json` thành chuỗi bí mật riêng, **không commit lên Git**, nên đưa vào biến môi trường hoặc User Secrets khi deploy thật.
- Cấu hình HTTPS đúng port giữa frontend/backend cho khớp với CORS.
- Nếu deploy production, thêm origin thật vào Authorized JavaScript origins trên Google Console và trong CORS policy.
