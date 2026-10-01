# Render deployment

## What you need to do

1. **Create a free Neon Postgres** — https://neon.com (free tier, no expiry).
   After creating a project, copy the **pooled** connection string. It looks like:
   ```
   Host=ep-xxxx.us-east-2.aws.neon.tech;Port=5432;Database=neondb_xxx;Username=xxx;Password=xxx;SSL Mode=Require
   ```
2. **Push this repo** to `kanmantha/Iform_Demo_Code`.
3. **Create the service** in Render: New → Blueprint → select the repo. `render.yaml` is picked up
   automatically. The blueprint declares the connection string as `sync: false`, so Render will
   prompt you to paste the Neon string as a secret.
4. The `onrender.com` URL appears on the service page when the build finishes.

Sign in with `admin@iform.app` / `Admin@123`.

## Why Docker, not a native runtime

Render has no native .NET runtime. Supported languages are Node/Bun, Python, Ruby, Go, Rust and
Elixir; the docs state .NET must be deployed as a Docker image. Hence `Dockerfile` + `runtime: docker`.

## Why Neon and not Render Postgres

Free Render Postgres **expires 30 days after creation** (then a 14-day grace period, then Render
deletes the database and all data). Your workspace already had its one free slot occupied by
`iform-recovery-db`, expiring 2026-10-16. Neon does not expire, so the demo URL keeps working.

## Changes made for Postgres + Render

- `Program.cs` — binds `http://0.0.0.0:$PORT` (Render injects `PORT=10000` and routes to it).
- `Program.cs` — skips `UseHttpsRedirection` when `PORT` is set, because Render terminates TLS at
  its proxy and forwards plain HTTP; leaving it on causes a redirect loop. Added
  `UseForwardedHeaders` so the app sees the original scheme.
- `Program.cs` — provider is chosen from the connection string: `Host=`/`Username=` prefix selects
  Npgsql, otherwise SQLite. Local dev and the test suite are unaffected.
- `Data/DbSeeder.cs` — runs `MigrateAsync` on Postgres, `EnsureCreatedAsync` on SQLite (local
  SQLite files are throwaway dev artifacts).
- `Data/ApplicationDbContextFactory.cs` — design-time factory so `dotnet ef` generates Postgres
  migrations. Override the target with `IFORM_DESIGN_CONNECTION`.
- `Data/Migrations/*` — the previous 8 migrations were SQLite-specific (`TEXT`/`INTEGER` column
  types) and could not run on Postgres. Replaced with a single `InitialSchema` migration that builds
  all 22 tables. If you later need SQLite migrations back, regenerate them with the factory pointed
  at a SQLite connection string.
- `Dockerfile` / `.dockerignore` — multi-stage .NET 10 build.

## Verified

- `dotnet build` clean; `dotnet test` 53/53 pass.
- Migration applied to a real PostgreSQL 16 instance; 23 tables created.
- App booted against that database, migrations auto-applied, seed produced 5 users / 15 tickets /
  10 incidents / 15 actions / 5 sites / 5 org units / 50 site queries.
- Login as `admin@iform.app` succeeded; `/Dashboard`, `/Tickets`, `/Incidents`, `/Actions`,
  `/SiteQueries`, `/Sites`, `/Organization`, `/Users`, `/Tickets/Board`, `/Products` all returned 200.
- `/SiteQueries/Pdf/1` returned a valid 44 KB `%PDF-` document.
- SQLite path re-checked after the change: still boots and serves.
- **Not verified:** the Docker image itself never built — Docker is not installed on this machine.
  The published `dotnet publish` output was run directly instead, which is what the image runs.

## Regenerating migrations after model changes

```bash
$env:IFORM_DESIGN_CONNECTION="Host=localhost;Port=5432;Username=postgres;Password=postgres;Database=iform_dev"
dotnet ef migrations add <Name> --project IForm.Web --output-dir Data/Migrations
```

## Security warning before sharing the link

Seeded passwords are hardcoded in `DbSeeder.cs` and identical on every deployment. There is no
lockout, rate limiting or CAPTCHA on the login page. On a public URL, `admin@iform.app` /
`Admin@123` is one guess from full admin access.

Acceptable for a short-lived private demo. Before public or long-lived use: move the seed passwords
to environment variables, and add rate limiting / lockout to the login endpoint.
