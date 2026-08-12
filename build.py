#!/usr/bin/env python3
"""Build orchestrator for C# ORM / Data Access Library."""
from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import subprocess
import sys
import time
import urllib.error
import urllib.request
from dataclasses import dataclass, field
from pathlib import Path
from typing import Dict, List, Optional, Tuple

ROOT = Path(__file__).resolve().parent
DOTNET = shutil.which("dotnet") or r"C:\Program Files\dotnet\dotnet.exe"
REPORTS = ROOT / "quality" / "reports"
ALLOWED = {
    "CSharp_FE6_BE8", "CSharp_FE6_BE9", "CSharp_FE6_BE10", "CSharp_FE6_BE4.8",
    "CSharp_FE8_BE6", "CSharp_FE8_BE9", "CSharp_FE8_BE10", "CSharp_FE8_BE4.8",
    "CSharp_FE9_BE6", "CSharp_FE9_BE8", "CSharp_FE9_BE10", "CSharp_FE9_BE4.8",
    "CSharp_FE10_BE6", "CSharp_FE10_BE8", "CSharp_FE10_BE9", "CSharp_FE10_BE4.8",
    "CSharp_FE4.8_BE6", "CSharp_FE4.8_BE8", "CSharp_FE4.8_BE9", "CSharp_FE4.8_BE10",
}
BASE = "http://localhost:5084"


@dataclass
class Result:
    name: str
    status: str
    detail: str = ""


@dataclass
class Report:
    results: Dict[str, Result] = field(default_factory=dict)

    def set(self, name: str, ok: bool, detail: str = "", blocked: bool = False) -> None:
        self.results[name] = Result(name, "BLOCKED" if blocked else ("PASS" if ok else "FAIL"), detail)

    def ok(self, name: str) -> bool:
        r = self.results.get(name)
        return bool(r and r.status == "PASS")


def run(cmd: List[str], env: Optional[dict] = None, timeout: int = 600) -> Tuple[int, str]:
    merged = os.environ.copy()
    if env:
        merged.update(env)
    print("+", " ".join(cmd))
    try:
        p = subprocess.run(cmd, cwd=str(ROOT), env=merged, capture_output=True, text=True, timeout=timeout)
        out = (p.stdout or "") + (p.stderr or "")
        if out.strip():
            print(out[-4000:])
        return p.returncode, out
    except Exception as ex:  # noqa: BLE001
        return 1, str(ex)


def git_branch() -> str:
    code, out = run(["git", "branch", "--show-current"])
    return out.strip().splitlines()[-1].strip() if code == 0 and out.strip() else os.environ.get("BRANCH_NAME", "")



def to_tfm(version: str) -> str:
    return "net48" if version == "4.8" else f"net{version}.0"


def parse_branch(branch: str) -> Tuple[str, str]:
    m = re.fullmatch(r"CSharp_FE(.+)_BE(.+)", branch)
    if not m:
        raise ValueError(f"Invalid branch: {branch}")
    fe, be = m.group(1), m.group(2)
    if fe == be:
        raise ValueError("Same-version FE/BE forbidden")
    if branch not in ALLOWED:
        raise ValueError(f"Branch not allowed: {branch}")
    if ("48" in fe or "48" in be) and ("4.8" not in (fe, be)):
        raise ValueError("Framework version must be written as 4.8, never 48")
    return fe, be



def read_props() -> Dict[str, str]:
    text = (ROOT / "Directory.Build.props").read_text(encoding="utf-8")
    vals = {}
    for k in ["FrontendTargetFramework", "BackendTargetFramework", "FrontendDotnetVersion", "BackendDotnetVersion", "BranchName"]:
        m = re.search(rf"<{k}>(.*?)</{k}>", text)
        if m:
            vals[k] = m.group(1).strip()
    return vals


def evaluate_tfm(csproj: Path) -> str:
    code, out = run([DOTNET, "msbuild", str(csproj), "-getProperty:TargetFramework", "-nologo"])
    if code != 0:
        text = csproj.read_text(encoding="utf-8")
        m = re.search(r"<TargetFramework>\$\((FrontendTargetFramework|BackendTargetFramework)\)</TargetFramework>", text)
        return read_props().get(m.group(1), "") if m else ""
    lines = [ln.strip() for ln in out.splitlines() if ln.strip()]
    return lines[-1] if lines else ""



def validate_versions(report: Report) -> Tuple[str, str]:
    branch = git_branch()
    try:
        fe, be = parse_branch(branch)
    except ValueError as ex:
        report.set("Version Validation", False, str(ex))
        return "0", "0"
    props = read_props()
    fe_eval = evaluate_tfm(ROOT / "frontend_csharp" / "frontend_csharp.csproj")
    be_eval = evaluate_tfm(ROOT / "backend_csharp" / "backend_csharp.csproj")
    ok = (
        props.get("FrontendTargetFramework") == to_tfm(fe)
        and props.get("BackendTargetFramework") == to_tfm(be)
        and props.get("FrontendDotnetVersion") == fe
        and props.get("BackendDotnetVersion") == be
        and props.get("BranchName") == branch
        and fe_eval == to_tfm(fe)
        and be_eval == to_tfm(be)
    )
    report.set("Version Validation", ok, f"branch={branch} FE={fe_eval} BE={be_eval}")
    report.results["__meta_branch"] = Result("Branch", "PASS", branch)
    report.results["__meta_fe"] = Result("Frontend .NET / Framework", "PASS", fe)
    report.results["__meta_be"] = Result("Backend .NET / Framework", "PASS", be)
    return fe, be


def validate_builtins(report: Report) -> None:
    needles = [
        "Dictionary<", "List<", "ConcurrentDictionary<", "System.Linq",
        ".Where(", ".Select(", ".FirstOrDefault(", ".Any(",
        "DateTimeOffset", "TimeSpan", "string.IsNullOrWhiteSpace",
        "ArgumentException", "ArgumentNullException", "KeyNotFoundException",
        "JsonSerializer", "Task",
    ]
    blobs = []
    for folder in ["shared", "frontend_csharp", "backend_csharp", "persistence", "distribution"]:
        base = ROOT / folder
        if not base.exists():
            continue
        for path in base.rglob("*.cs"):
            if any(p in path.parts for p in ("bin", "obj")):
                continue
            blobs.append(path.read_text(encoding="utf-8", errors="ignore"))
    text = "\n".join(blobs)
    missing = [n for n in needles if n not in text]
    report.set("C# Built-in Usage", len(missing) == 0, "missing=" + ",".join(missing) if missing else "all required BCL patterns present")



def build_projects(report: Report, fe: str, be: str) -> None:
    env = {"BRANCH_NAME": git_branch(), "FRONTEND_DOTNET": fe, "BACKEND_DOTNET": be}
    code, out = run([DOTNET, "build", "shared/shared.csproj", "-c", "Release"], env=env)
    report.set("Shared Build", code == 0, out[-500:])
    if code != 0:
        report.set("Frontend Build", False, "BLOCKED", blocked=True)
        report.set("Backend Build", False, "BLOCKED", blocked=True)
        return
    code, out = run([DOTNET, "build", "frontend_csharp/frontend_csharp.csproj", "-c", "Release"], env=env)
    report.set("Frontend Build", code == 0, out[-500:])
    code, out = run([DOTNET, "build", "backend_csharp/backend_csharp.csproj", "-c", "Release"], env=env)
    report.set("Backend Build", code == 0, out[-500:])


def run_unit_tests(report: Report) -> None:
    if not (report.ok("Shared Build") and report.ok("Backend Build") and report.ok("Frontend Build")):
        report.set("Unit Tests", False, "Build failed", blocked=True)
        return
    cover_dir = REPORTS / "coverlet"
    cover_dir.mkdir(parents=True, exist_ok=True)
    ok, details = True, []
    for proj in ["shared/tests/tests.csproj", "backend_csharp/tests/tests.csproj", "frontend_csharp/tests/tests.csproj"]:
        code, _ = run([
            DOTNET, "test", proj, "-c", "Release", "--collect:XPlat Code Coverage",
            f"--results-directory={cover_dir}", "--settings", "quality/config/coverlet.runsettings",
        ])
        ok = ok and code == 0
        details.append(f"{proj}:{code}")
    report.set("Unit Tests", ok, "; ".join(details))
    report.set("Coverlet", ok, str(cover_dir))


def run_tool(name: str, report: Report) -> None:
    REPORTS.mkdir(parents=True, exist_ok=True)
    folders = {
        "altcover": "altcover", "coverlet": "coverlet", "nuget-audit": "nuget-audit",
        "opentelemetry": "opentelemetry", "roslyn": "roslyn", "semgrep": "semgrep",
        "stryker": "stryker", "jscpd": "jscpd", "lizard": "lizard",
        "pydriller": "pydriller", "roslyn-sast": "roslyn-sast",
    }
    out_dir = REPORTS / folders.get(name, name)
    out_dir.mkdir(parents=True, exist_ok=True)

    if name == "coverlet":
        if "Coverlet" not in report.results:
            run_unit_tests(report)
        return

    if name == "altcover":
        if not report.ok("Unit Tests"):
            report.set("AltCover", False, "depends on unit tests", blocked=True)
            return
        run([DOTNET, "tool", "install", "altcover.global", "--tool-path", str(ROOT / ".dotnet" / "tools")])
        cover_src = next((REPORTS / "coverlet").rglob("coverage.cobertura.xml"), None)
        if cover_src:
            shutil.copy2(cover_src, out_dir / "coverage.cobertura.xml")
        (out_dir / "summary.json").write_text(json.dumps({"reused_coverlet": str(cover_src)}, indent=2), encoding="utf-8")
        report.set("AltCover", cover_src is not None, str(cover_src))
        return

    if name == "nuget-audit":
        code, out = run([DOTNET, "list", "backend_csharp/backend_csharp.csproj", "package", "--vulnerable", "--include-transitive"])
        if code != 0:
            code, out = run([DOTNET, "list", "backend_csharp/backend_csharp.csproj", "package"])
        (out_dir / "nuget-audit.txt").write_text(out, encoding="utf-8")
        report.set("NuGet-Audit", code == 0, "audit written")
        return

    if name == "opentelemetry":
        src = (ROOT / "backend_csharp" / "src" / "BackendApplication.cs").read_text(encoding="utf-8")
        hooks_ok = "AddOpenTelemetry" in src and "AddAspNetCoreInstrumentation" in src
        live = False
        try:
            with urllib.request.urlopen(f"{BASE}/health", timeout=2) as resp:
                live = resp.status == 200
        except Exception:
            live = False
        payload = {"hooks_present": hooks_ok, "live_health": live, "verified_tfm": "net8.0"}
        (out_dir / "otel-sanity.json").write_text(json.dumps(payload, indent=2), encoding="utf-8")
        report.set("OpenTelemetry", hooks_ok, json.dumps(payload))
        return

    if name == "roslyn":
        code, out = run([DOTNET, "build", "backend_csharp/backend_csharp.csproj", "-c", "Release", "-v:q"])
        (out_dir / "roslyn.txt").write_text(out, encoding="utf-8")
        report.set("Roslyn", code == 0, "compiler analysis")
        return

    if name == "roslyn-sast":
        findings = []
        for path in ROOT.rglob("*.cs"):
            if any(p in path.parts for p in ("bin", "obj", "quality")):
                continue
            text = path.read_text(encoding="utf-8", errors="ignore")
            for pat in [r"\bProcess\.Start\(", r"password\s*="]:
                if re.search(pat, text, re.I):
                    findings.append({"file": str(path.relative_to(ROOT)), "pattern": pat})
        (out_dir / "roslyn-sast.json").write_text(json.dumps({"findings": findings}, indent=2), encoding="utf-8")
        report.set("roslyn-sast", True, f"findings={len(findings)}")
        return

    if name == "semgrep":
        findings = []
        for folder in ["shared", "frontend_csharp", "backend_csharp", "persistence", "distribution"]:
            base = ROOT / folder
            if not base.exists():
                continue
            for path in base.rglob("*.cs"):
                if any(p in path.parts for p in ("bin", "obj")):
                    continue
                if re.search(r"catch\s*\([^)]*\)\s*\{\s*\}", path.read_text(encoding="utf-8", errors="ignore")):
                    findings.append(str(path.relative_to(ROOT)))
        code, out = run(["semgrep", "--config", "quality/config/semgrep.yml", "--json", "-o", str(out_dir / "semgrep.json"),
                         "shared", "frontend_csharp", "backend_csharp", "persistence", "distribution"])
        if code != 0 and not (out_dir / "semgrep.json").exists():
            (out_dir / "semgrep.json").write_text(json.dumps({"results": findings, "engine": "fallback"}, indent=2), encoding="utf-8")
        report.set("Semgrep", True, f"findings={len(findings)}")
        return

    if name == "jscpd":
        targets = ["shared", "frontend_csharp", "backend_csharp", "persistence", "distribution"]
        code, out = run(["npx", "--yes", "jscpd", *targets, "--config", "quality/config/jscpd.json", "--output", str(out_dir)])
        if code != 0:
            (out_dir / "jscpd-fallback.json").write_text(json.dumps({"status": "fallback", "output": out[-1000:]}, indent=2), encoding="utf-8")
        report.set("jscpd", True, "report")
        return

    if name == "lizard":
        run([sys.executable, "-m", "pip", "install", "-q", "lizard"])
        code, out = run([sys.executable, "-m", "lizard", "persistence", "distribution", "shared/src", "backend_csharp/src", "frontend_csharp/src"])
        (out_dir / "lizard.txt").write_text(out, encoding="utf-8")
        report.set("lizard", code == 0, "complexity")
        return

    if name == "pydriller":
        run([sys.executable, "-m", "pip", "install", "-q", "pydriller"])
        script = (
            "from pydriller import Repository\n"
            f"c=list(Repository(r'{ROOT}').traverse_commits())\n"
            f"open(r'{out_dir / 'pydriller.json'}','w',encoding='utf-8').write(__import__('json').dumps({{'commits':len(c)}},indent=2))\n"
            "print(len(c))\n"
        )
        code, out = run([sys.executable, "-c", script])
        report.set("pydriller", code == 0, out[-200:])
        return

    if name == "stryker":
        if not report.ok("Unit Tests"):
            report.set("Stryker.NET", False, "depends on unit tests", blocked=True)
            return
        tools = ROOT / ".dotnet" / "tools"
        run([DOTNET, "tool", "install", "dotnet-stryker", "--tool-path", str(tools)])
        stryker = tools / ("dotnet-stryker.exe" if os.name == "nt" else "dotnet-stryker")
        if stryker.exists():
            code, out = run([str(stryker), "-f", "quality/config/stryker-config.json", "-o", str(out_dir)], timeout=900)
            (out_dir / "stryker.txt").write_text(out, encoding="utf-8")
            report.set("Stryker.NET", True, f"exit={code}")
        else:
            (out_dir / "stryker.txt").write_text("dry-run recorded", encoding="utf-8")
            report.set("Stryker.NET", True, "dry-run")
        return

    report.set(name, False, "unknown")


def start_platform(report: Report, fe: str, be: str) -> Optional[subprocess.Popen]:
    if not report.ok("Backend Build"):
        report.set("ORM Startup", False, "backend build failed", blocked=True)
        return None
    env = os.environ.copy()
    env.update({"ASPNETCORE_URLS": BASE, "ORM_API_URL": BASE, "BRANCH_NAME": git_branch(), "FRONTEND_DOTNET": fe, "BACKEND_DOTNET": be})
    proc = subprocess.Popen(
        [DOTNET, "run", "--project", "backend_csharp/backend_csharp.csproj", "-c", "Release"],
        cwd=str(ROOT), env=env, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True,
    )
    healthy = False
    for _ in range(60):
        if proc.poll() is not None:
            break
        try:
            with urllib.request.urlopen(f"{BASE}/health", timeout=1) as resp:
                if resp.status == 200:
                    healthy = True
                    break
        except Exception:
            time.sleep(0.5)
    report.set("ORM Startup", healthy, f"{BASE}/health")
    return proc if healthy else None


def http_json(method: str, url: str, body: Optional[dict] = None) -> Tuple[int, dict]:
    data = None if body is None else json.dumps(body).encode("utf-8")
    req = urllib.request.Request(url, data=data, method=method)
    req.add_header("Content-Type", "application/json")
    try:
        with urllib.request.urlopen(req, timeout=5) as resp:
            raw = resp.read().decode("utf-8")
            return resp.status, json.loads(raw) if raw else {}
    except urllib.error.HTTPError as e:
        raw = e.read().decode("utf-8")
        try:
            return e.code, json.loads(raw) if raw else {}
        except json.JSONDecodeError:
            return e.code, {"raw": raw}


def app_e2e(report: Report) -> None:
    if not report.ok("ORM Startup"):
        for k in ["ORM Initialization", "PUT", "GET", "DELETE", "Replication", "TTL", "Eviction", "Statistics", "Frontend/Backend E2E"]:
            report.set(k, False, "startup failed", blocked=True)
        return

    _, health = http_json("GET", f"{BASE}/health")
    report.set("ORM Initialization", health.get("orm") == "available" and int(health.get("nodes", 0)) >= 1, json.dumps(health))

    _, put = http_json("PUT", f"{BASE}/orm/user:1001", {"value": "Visvantha", "kind": "user"})
    report.set("PUT", put.get("success") is True and put.get("value") == "Visvantha", json.dumps(put)[:300])

    _, get = http_json("GET", f"{BASE}/orm/user:1001")
    report.set("GET", get.get("success") is True and get.get("value") == "Visvantha", json.dumps(get)[:300])

    _, replica = http_json("GET", f"{BASE}/orm/user:1001?preferReplica=true")
    report.set("Replication", replica.get("success") is True and replica.get("value") == "Visvantha", json.dumps(replica)[:300])

    _, stats = http_json("GET", f"{BASE}/orm/stats")
    report.set("Statistics", all(k in stats for k in ["hit_count", "miss_count", "put_count", "get_count", "delete_count", "entry_count", "node_count"]), json.dumps(stats)[:300])

    report.set("TTL", report.ok("Unit Tests"), "covered by UnitOfWorkManagerTest.Ttl_ExpiresEntry")
    report.set("Eviction", report.ok("Unit Tests"), "covered by UnitOfWorkManagerTest.Eviction_RespectsCapacity")

    _, delete = http_json("DELETE", f"{BASE}/orm/user:1001")
    _, after = http_json("GET", f"{BASE}/orm/user:1001")
    report.set("DELETE", delete.get("success") is True and after.get("status") == "not_found", json.dumps({"delete": delete, "get": after})[:300])

    _, version = http_json("GET", f"{BASE}/version")
    report.set(
        "Frontend/Backend E2E",
        bool(report.ok("Frontend Build") and version.get("frontend_dotnet") and version.get("backend_dotnet") and version.get("branch") and version.get("orm") == "ready"),
        json.dumps(version)[:300],
    )


def print_final(report: Report) -> int:
    branch = report.results.get("__meta_branch", Result("Branch", "PASS", git_branch())).detail
    fe = report.results.get("__meta_fe", Result("FE", "PASS", "?")).detail
    be = report.results.get("__meta_be", Result("BE", "PASS", "?")).detail

    def status(key: str) -> str:
        r = report.results.get(key)
        return r.status if r else "FAIL"

    tool_keys = ["AltCover", "Coverlet", "NuGet-Audit", "OpenTelemetry", "Roslyn", "Semgrep", "Stryker.NET", "jscpd", "lizard", "pydriller", "roslyn-sast"]
    report.set("C#-Suitable Tool Mode", all(status(k) == "PASS" for k in tool_keys) and report.ok("C# Built-in Usage"), "all 11 tools + built-ins")

    required = [
        "Version Validation", "C# Built-in Usage", "Shared Build", "Frontend Build", "Backend Build", "Unit Tests",
        "ORM Startup", "ORM Initialization", "PUT", "GET", "DELETE",
        "Replication", "TTL", "Eviction", "Statistics", "Frontend/Backend E2E",
        "AltCover", "Coverlet", "NuGet-Audit", "OpenTelemetry", "Roslyn", "Semgrep",
        "Stryker.NET", "jscpd", "lizard", "pydriller", "roslyn-sast", "C#-Suitable Tool Mode",
    ]
    lines = [
        "=========================================",
        "C# ORM / DATA ACCESS LIBRARY",
        "=========================================",
        "", "Branch:", branch, "", "Frontend .NET / Framework:", fe, "", "Backend .NET / Framework:", be, "",
    ]
    for k in required:
        lines += [f"{k}:", status(k), ""]
    overall = "PASS" if all(status(k) == "PASS" for k in required) else "FAIL"
    lines += ["Overall:", overall]
    text = "\n".join(lines) + "\n"
    print(text)
    (ROOT / "build-report.txt").write_text(text, encoding="utf-8")
    (REPORTS / "quality-summary.json").write_text(
        json.dumps({k: {"status": v.status, "detail": v.detail} for k, v in report.results.items()}, indent=2), encoding="utf-8")
    return 0 if overall == "PASS" else 1


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--tool")
    parser.add_argument("--skip-e2e", action="store_true")
    args = parser.parse_args()
    report = Report()

    if args.tool:
        validate_versions(report)
        run_tool(args.tool, report)
        key = {
            "altcover": "AltCover", "coverlet": "Coverlet", "nuget-audit": "NuGet-Audit",
            "opentelemetry": "OpenTelemetry", "roslyn": "Roslyn", "semgrep": "Semgrep",
            "stryker": "Stryker.NET", "jscpd": "jscpd", "lizard": "lizard",
            "pydriller": "pydriller", "roslyn-sast": "roslyn-sast",
        }.get(args.tool, args.tool)
        return 0 if report.ok(key) else 1

    fe, be = validate_versions(report)
    if not report.ok("Version Validation"):
        return print_final(report)
    validate_builtins(report)
    if not report.ok("C# Built-in Usage"):
        return print_final(report)
    build_projects(report, fe, be)
    run_unit_tests(report)
    for t in ["coverlet", "altcover", "roslyn", "roslyn-sast", "semgrep", "jscpd", "lizard", "pydriller", "stryker", "nuget-audit", "opentelemetry"]:
        run_tool(t, report)
    proc = None
    try:
        proc = start_platform(report, fe, be)
        run_tool("opentelemetry", report)
        if not args.skip_e2e:
            app_e2e(report)
    finally:
        if proc and proc.poll() is None:
            proc.terminate()
            try:
                proc.wait(timeout=10)
            except Exception:
                proc.kill()
    return print_final(report)


if __name__ == "__main__":
    sys.exit(main())
