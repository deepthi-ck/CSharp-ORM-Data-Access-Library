# C# ORM / Data Access Library

Minimal CleanArchitecture-inspired ORM / data-access platform with UnitOfWork + ApplicationDbContext-style persistence, partitioned in-memory store, replication, TTL, and eviction.

Conceptual inspiration: [jasontaylordev/CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture) — not a clone.

## Quick start

```bash
# Full quality gate (optional)
python build.py

# Run API + browser UI (C# only — no Python required to view UI)
dotnet run --project backend_csharp/backend_csharp.csproj -c Release
```

Open the dashboard in a browser: **http://localhost:5084/**  
(also `/ui`, health at `/health`)

Optional frontend UI host (proxies API with C# `HttpClient` + `HttpListener`):

```bash
# terminal 1 — API
dotnet run --project backend_csharp/backend_csharp.csproj -c Release

# terminal 2 — UI on :5085
dotnet run --project frontend_csharp/frontend_csharp.csproj -c Release
```

Then open **http://localhost:5085/**

API JSON: `http://localhost:5084`  
Dashboard HTML is generated with C# BCL only (`StringBuilder`, `WebUtility`, `JsonSerializer`, `HttpListener` / ASP.NET).

## Branch model

20 FE/BE pairs including `.NET Framework 4.8` (`net48` TFM; always displayed as `4.8`). See [docs/version-matrix.md](docs/version-matrix.md).
