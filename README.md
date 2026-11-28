---
title: Nazeef
emoji: 🧼
colorFrom: blue
colorTo: indigo
sdk: docker
app_port: 7860
pinned: false
license: mit
---

# Nazeef Backend

ASP.NET Core Backend for Nazeef Application deployed on Hugging Face Spaces.

## Local setup (SQL Server)

1. Copy `NdeefApp/appsettings.example.json` to `NdeefApp/appsettings.json` (git-ignored) and fill in the values.
2. Set `ConnectionStrings:DefaultConnection` to your local SQL Server (the example uses Windows auth on `localhost`).
3. Set `Jwt:Key` (32+ chars). Keep secrets out of git; prefer `dotnet user-secrets` or environment variables (`Kashier__ApiKey`, ...).
4. `dotnet run --project NdeefApp` — migrations are applied automatically on startup. Roles and the services catalog are always seeded; demo users/data and the SuperAdmin are created only when `Seed:Enabled=true`.
5. Payments: set `Kashier:PublicApiBaseUrl` to a public HTTPS URL (e.g. a tunnel) and `Kashier:PaymentApiKey` / `Kashier:ApiKey` so callbacks and webhooks can be signature-verified. `Kashier:SkipSignatureValidation` is for local debugging only.
