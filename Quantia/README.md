# Quantia Web (ASP.NET Core 8)

The user-facing web application: account management, portfolio simulation,
trade tracking, and integration with the ML prediction API and the sentiment
pipeline.

## Build & run

```bash
dotnet restore Quantia.sln
dotnet build Quantia.sln -c Release
dotnet run --project Quantia
```

## Configuration

All settings can be overridden by environment variables (the standard
ASP.NET Core configuration provider chain applies):

| Setting                                         | Env var override                                   | Default                                          |
|-------------------------------------------------|----------------------------------------------------|--------------------------------------------------|
| `ConnectionStrings:DefaultConnection`           | `ConnectionStrings__DefaultConnection`             | `Host=localhost;...;Password=changeme`           |
| `MlApi:BaseUrl`                                 | `MlApi__BaseUrl`                                   | `https://api-test-049u.onrender.com`             |
| `RunningInContainer`                            | `DOTNET_RUNNING_IN_CONTAINER`                      | `false`                                          |

## Endpoints

- `GET /health` — liveness probe (returns `{ status: "ok", utc: ... }`)
- `GET /` — redirects to `/Account/Login`
- `GET /Dashboard` — main dashboard (requires auth)
- `GET /Portfolio` — current portfolio with live PnL
- `GET /Trade` — open / close / edit positions
- `GET /Prediction?symbol=BTCUSDT` — ML signals + equity curve
- `GET /SentimentAnalysis` — latest sentiment report
- `GET /TechnicalAnalysis` — technical indicators view
- `GET /api/sentiment/history?hours=72` — sentiment time series JSON
- `GET /api/sentiment/detail?ts=...` — sentiment cluster detail JSON
- `GET /api/Candlestick/load?symbol=...&start_date=...&end_date=...` — proxies the ML API

## Project layout

```
Quantia/
├── Controllers/   # one per feature
├── Data/          # AppDbContext + repositories
├── Models/        # entities + ViewModels
├── Services/      # ML API clients, equity computation, sentiment service
├── Views/         # Razor (.cshtml)
├── wwwroot/       # static assets
├── Properties/launchSettings.json
├── appsettings.json
├── Program.cs
├── Dockerfile
└── Quantia.csproj
```
