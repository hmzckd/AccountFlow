# AccountFlow

AccountFlow is a small full-stack authentication project built with ASP.NET Core, MongoDB, React, and TypeScript. I built it to cover the parts of account management that are easy to get wrong: email verification, refresh-token rotation, password recovery, session revocation, and role-based access.

[![CI](https://github.com/hmzckd/AccountFlow/actions/workflows/ci.yml/badge.svg)](https://github.com/hmzckd/AccountFlow/actions/workflows/ci.yml)

## What it includes

- Registration with a single-use email verification link
- Resending expired verification links with a per-account cooldown
- JWT access tokens and rotating refresh tokens
- Password reset that invalidates existing access and refresh tokens
- Enumeration-safe recovery and verification responses
- User and admin routes in the React client
- Admin-only registration statistics and email endpoint
- Rate limiting, startup configuration validation, and consistent API errors
- MongoDB-backed integration tests for the main authentication flows

## Stack

| Area | Technology |
| --- | --- |
| API | ASP.NET Core controllers on .NET 10 |
| Database | MongoDB 7 / MongoDB.Driver |
| Authentication | JWT bearer tokens and ASP.NET Core PasswordHasher |
| Validation | FluentValidation |
| API reference | OpenAPI and Scalar |
| Frontend | React 19, TypeScript, Vite, Tailwind CSS |
| Tests | xUnit and WebApplicationFactory |
| CI | GitHub Actions with MongoDB service container |

## Repository layout

```text
.
├── AccountFlow/
│   ├── Backend/              API controllers, services, models, and validators
│   ├── frontend/             React client
│   ├── Program.cs            application setup and middleware pipeline
│   └── appsettings.example.json
├── AccountFlow.Tests/        unit and HTTP/MongoDB integration tests
├── .github/workflows/ci.yml
└── AccountFlow.sln
```

## Run locally

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 20.19 or newer
- MongoDB running on `localhost:27017`
- Optional: an SMTP account for real verification and reset emails

### 1. Configure the API

The committed configuration contains no usable secret. Set local values with the .NET secret manager:

```powershell
dotnet user-secrets set --project AccountFlow "AppSettings:Token" "replace-with-a-random-secret-of-at-least-32-bytes"
dotnet user-secrets set --project AccountFlow "ConnectionStrings:DbConnection" "mongodb://localhost:27017/accountflow"
```

For real email delivery, also set the sender address and app password:

```powershell
dotnet user-secrets set --project AccountFlow "GmailOptions:Email" "you@example.com"
dotnet user-secrets set --project AccountFlow "GmailOptions:Password" "your-app-password"
```

If you only want to try the project locally, disable delivery. Verification and reset links will be written to the API console instead:

```powershell
dotnet user-secrets set --project AccountFlow "Auth:SendVerificationEmails" "false"
```

You can also copy `AccountFlow/appsettings.example.json` to `AccountFlow/appsettings.Development.json` and replace its placeholders. The development file is ignored by Git.

### 2. Start the API

```powershell
dotnet run --project AccountFlow --launch-profile http
```

The API listens on `http://localhost:5205`. In Development, Scalar is available at `http://localhost:5205/scalar/v1`.

### 3. Start the frontend

```powershell
cd AccountFlow/frontend
Copy-Item .env.example .env.local
npm ci
npm run dev
```

Open `http://localhost:5173`. The frontend reads the API URL from:

```dotenv
VITE_API_BASE_URL=http://localhost:5205/api
```

### 4. Prepare an admin account

There is no default admin password in the repository. Create and verify a normal account first, then update its role in MongoDB:

```javascript
// Run in mongosh after connecting to mongodb://localhost:27017/accountflow
db.users.updateOne(
  { user_email: "admin@example.com" },
  { $set: { Role: "Admin" } }
)
```

Log out and sign in again so the new JWT contains the `Admin` role. The account will then be sent to `/admin` after login.

## Checks

Backend tests require MongoDB:

```powershell
dotnet test AccountFlow.sln --configuration Release
```

Frontend checks:

```powershell
cd AccountFlow/frontend
npm ci
npm run lint
npm run build
```

## API overview

| Method | Route | Access |
| --- | --- | --- |
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/verify-email` | Public |
| POST | `/api/auth/verify-email/resend` | Public |
| POST | `/api/auth/login` | Public |
| POST | `/api/auth/refresh` | Public |
| POST | `/api/auth/password/reset-request` | Public |
| POST | `/api/auth/password/reset` | Public |
| GET | `/api/admin/stats/daily` | Admin |
| GET | `/api/admin/stats/daily-registrations` | Admin |
| GET | `/api/admin/stats/daily-unverified` | Admin |
| POST | `/api/email/sendEmail` | Admin |

Example requests are available in [`AccountFlow/AccountFlow.http`](AccountFlow/AccountFlow.http).

## Security decisions

- Verification, password-reset, and refresh tokens are stored as SHA-256 hashes.
- Refresh tokens rotate atomically and cannot be reused after a successful exchange.
- Password reset increments a session version checked on every authenticated request.
- Registration email failure rolls back the new user instead of leaving a locked account.
- Verification resends use an atomic cooldown and do not reveal whether an account exists.
- JWT and token lifetime settings are validated before the application starts.
- The MongoDB email index prevents duplicate accounts during concurrent registration.

## License

This project is available under the [MIT License](LICENSE).
