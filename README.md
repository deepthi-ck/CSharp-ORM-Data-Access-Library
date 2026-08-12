# C# ORM / Data Access Library

Minimal CleanArchitecture-inspired ORM / data-access platform with UnitOfWork + ApplicationDbContext-style persistence, partitioned in-memory store, replication, TTL, and eviction.

Conceptual inspiration: [jasontaylordev/CleanArchitecture](https://github.com/jasontaylordev/CleanArchitecture) — not a clone.

## Quick start

```bash
python build.py
```

API: `http://localhost:5084`

## Branch model

20 FE/BE pairs including `.NET Framework 4.8` (`net48` TFM; always displayed as `4.8`). See [docs/version-matrix.md](docs/version-matrix.md).
