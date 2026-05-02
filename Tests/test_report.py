#!/usr/bin/env python3
"""Cross-platform test runner that reports per-project and total test counts
plus per-project and combined SQLite.Framework code coverage.

Mirrors the .github/workflows/coverage.yml pipeline:
  * dotnet build -c Release
  * dotnet run -- --report-xunit-junit ...    (test counts)
  * coverlet <dll> --include "[SQLite.Framework]*" --format cobertura

Works on Linux, macOS, and Windows. Only requires Python 3.8+ and the
.NET SDK on PATH. coverlet.console is auto-installed if missing.
"""

from __future__ import annotations

import argparse
import concurrent.futures
import os
import platform
import re
import shutil
import subprocess
import sys
import time
import xml.etree.ElementTree as ET
from collections import defaultdict
from dataclasses import dataclass, field
from pathlib import Path
from typing import Iterable

REPO_ROOT = Path(__file__).resolve().parent.parent
TESTS_DIR = Path(__file__).resolve().parent
DEFAULT_TFM = "net10.0"
DEFAULT_CONFIG = "Release"
COVERAGE_INCLUDE = "[SQLite.Framework]*"
RESULTS_DIR = TESTS_DIR / "TestResults"


@dataclass
class TestStats:
    total: int = 0
    passed: int = 0
    failed: int = 0
    skipped: int = 0
    errors: int = 0
    duration: float = 0.0


@dataclass
class CoverageStats:
    lines_valid: int = 0
    lines_covered: int = 0
    branches_valid: int = 0
    branches_covered: int = 0

    @property
    def line_rate(self) -> float:
        return self.lines_covered / self.lines_valid if self.lines_valid else 0.0

    @property
    def branch_rate(self) -> float:
        return (
            self.branches_covered / self.branches_valid if self.branches_valid else 0.0
        )


@dataclass
class ProjectResult:
    name: str
    csproj: Path
    tests: TestStats = field(default_factory=TestStats)
    coverage: CoverageStats | None = None
    junit_path: Path | None = None
    cobertura_path: Path | None = None
    build_failed: bool = False
    test_run_failed: bool = False
    coverage_failed: bool = False
    notes: list[str] = field(default_factory=list)


def discover_projects(filter_names: list[str] | None) -> list[Path]:
    projects = sorted(TESTS_DIR.glob("SQLite.Framework.Tests*/"))
    csprojs: list[Path] = []
    for p in projects:
        matches = list(p.glob("*.csproj"))
        if not matches:
            continue
        csproj = matches[0]
        if filter_names and csproj.stem not in filter_names:
            continue
        csprojs.append(csproj)
    return csprojs


def run(
    cmd: list[str],
    cwd: Path | None = None,
    env: dict[str, str] | None = None,
    capture: bool = True,
) -> subprocess.CompletedProcess:
    return subprocess.run(
        cmd,
        cwd=str(cwd) if cwd else None,
        env={**os.environ, **(env or {})},
        capture_output=capture,
        text=True,
        check=False,
    )


def ensure_coverlet() -> str | None:
    coverlet = shutil.which("coverlet")
    if coverlet:
        return coverlet
    print(
        "coverlet not found on PATH. Installing coverlet.console as a global tool...",
        flush=True,
    )
    res = run(["dotnet", "tool", "install", "--global", "coverlet.console"])
    if res.returncode != 0 and "already installed" not in (res.stderr or ""):
        print(res.stdout)
        print(res.stderr, file=sys.stderr)
        return None
    candidates = []
    home = Path.home()
    if platform.system() == "Windows":
        candidates.append(home / ".dotnet" / "tools" / "coverlet.exe")
    else:
        candidates.append(home / ".dotnet" / "tools" / "coverlet")
    for c in candidates:
        if c.exists():
            os.environ["PATH"] = os.pathsep.join(
                [str(c.parent), os.environ.get("PATH", "")]
            )
            return str(c)
    return shutil.which("coverlet")


def build_project(csproj: Path, configuration: str) -> bool:
    print(f"  building {csproj.stem}...", flush=True)
    res = run(
        [
            "dotnet",
            "build",
            str(csproj),
            "-c",
            configuration,
            "--nologo",
            "-v",
            "minimal",
        ]
    )
    if res.returncode != 0:
        print(res.stdout)
        print(res.stderr, file=sys.stderr)
        return False
    return True


def run_tests(csproj: Path, configuration: str) -> tuple[Path | None, bool, str]:
    RESULTS_DIR.mkdir(parents=True, exist_ok=True)
    junit_name = f"{csproj.stem}.junit.xml"
    junit_path = RESULTS_DIR / junit_name
    if junit_path.exists():
        junit_path.unlink()
    cmd = [
        "dotnet",
        "run",
        "--project",
        str(csproj),
        "-c",
        configuration,
        "--no-build",
        "--",
        "--report-xunit-junit",
        "--report-xunit-junit-filename",
        junit_name,
        "--results-directory",
        str(RESULTS_DIR),
    ]
    res = run(cmd)
    if not junit_path.exists():
        return None, False, (res.stderr or res.stdout or "")
    return junit_path, res.returncode == 0, ""


def parse_junit(path: Path) -> TestStats:
    stats = TestStats()
    try:
        tree = ET.parse(path)
    except ET.ParseError:
        return stats
    root = tree.getroot()
    suites: Iterable[ET.Element]
    if root.tag == "testsuites":
        suites = root.findall("testsuite")
        if not suites:
            suites = [root]
    elif root.tag == "testsuite":
        suites = [root]
    else:
        suites = root.findall(".//testsuite")
    for s in suites:
        try:
            stats.total += int(s.get("tests", "0") or 0)
            stats.failed += int(s.get("failures", "0") or 0)
            stats.errors += int(s.get("errors", "0") or 0)
            stats.skipped += int(s.get("skipped", "0") or 0)
            stats.duration += float(s.get("time", "0") or 0)
        except ValueError:
            continue
    stats.passed = max(stats.total - stats.failed - stats.errors - stats.skipped, 0)
    return stats


def run_coverage(
    csproj: Path, configuration: str, tfm: str, coverlet: str
) -> tuple[Path | None, str]:
    project_dir = csproj.parent
    dll = project_dir / "bin" / configuration / tfm / f"{csproj.stem}.dll"
    if not dll.exists():
        return None, f"missing build output: {dll}"
    out = project_dir / "coverage.cobertura.xml"
    if out.exists():
        out.unlink()
    cmd = [
        coverlet,
        str(dll),
        "--target",
        "dotnet",
        "--targetargs",
        f"exec {dll}",
        "--include",
        COVERAGE_INCLUDE,
        "--format",
        "cobertura",
        "--output",
        str(out),
    ]
    res = run(cmd)
    if not out.exists():
        return None, (res.stderr or res.stdout or "coverlet did not produce output")
    return out, ""


CONDITION_RE = re.compile(r"\((\d+)\s*/\s*(\d+)\)")


def parse_cobertura(
    path: Path,
) -> tuple[CoverageStats, dict[tuple[str, int], tuple[int, int, int]]]:
    """Returns (top-level totals, per-line map of (file,line) -> (hits, br_cov, br_total))."""
    stats = CoverageStats()
    line_map: dict[tuple[str, int], tuple[int, int, int]] = {}
    try:
        tree = ET.parse(path)
    except ET.ParseError:
        return stats, line_map
    root = tree.getroot()
    try:
        stats.lines_valid = int(root.get("lines-valid", "0") or 0)
        stats.lines_covered = int(root.get("lines-covered", "0") or 0)
        stats.branches_valid = int(root.get("branches-valid", "0") or 0)
        stats.branches_covered = int(root.get("branches-covered", "0") or 0)
    except ValueError:
        pass
    for cls in root.findall(".//class"):
        filename = cls.get("filename", "") or ""
        for line in cls.findall(".//line"):
            try:
                num = int(line.get("number", "0") or 0)
            except ValueError:
                continue
            try:
                hits = int(line.get("hits", "0") or 0)
            except ValueError:
                hits = 0
            br_cov = br_total = 0
            if (line.get("branch", "False") or "False").lower() == "true":
                cc = line.get("condition-coverage", "") or ""
                m = CONDITION_RE.search(cc)
                if m:
                    br_cov = int(m.group(1))
                    br_total = int(m.group(2))
            key = (filename, num)
            prev = line_map.get(key)
            if prev is None:
                line_map[key] = (hits, br_cov, br_total)
            else:
                line_map[key] = (
                    max(prev[0], hits),
                    max(prev[1], br_cov),
                    max(prev[2], br_total),
                )
    return stats, line_map


def merge_coverage(
    per_project_lines: list[dict[tuple[str, int], tuple[int, int, int]]],
) -> CoverageStats:
    merged: dict[tuple[str, int], tuple[int, int, int]] = {}
    for lm in per_project_lines:
        for key, val in lm.items():
            prev = merged.get(key)
            if prev is None:
                merged[key] = val
            else:
                merged[key] = (
                    max(prev[0], val[0]),
                    max(prev[1], val[1]),
                    max(prev[2], val[2]),
                )
    out = CoverageStats()
    for hits, br_cov, br_total in merged.values():
        out.lines_valid += 1
        if hits > 0:
            out.lines_covered += 1
        out.branches_valid += br_total
        out.branches_covered += br_cov
    return out


def fmt_pct(numer: int, denom: int) -> str:
    if denom == 0:
        return "  n/a"
    return f"{(numer / denom) * 100:5.2f}%"


def fmt_table(rows: list[list[str]], headers: list[str]) -> str:
    widths = [len(h) for h in headers]
    for row in rows:
        for i, cell in enumerate(row):
            if len(cell) > widths[i]:
                widths[i] = len(cell)
    line = "  ".join(h.ljust(widths[i]) for i, h in enumerate(headers))
    sep = "  ".join("-" * widths[i] for i in range(len(headers)))
    out = [line, sep]
    for row in rows:
        out.append("  ".join(row[i].ljust(widths[i]) for i in range(len(headers))))
    return "\n".join(out)


def build_phase(
    csprojs: list[Path], configuration: str, skip_build: bool
) -> dict[str, ProjectResult]:
    """Builds every project sequentially so that the shared SQLite.Framework
    output isn't written by two MSBuild invocations at once."""
    results: dict[str, ProjectResult] = {
        c.stem: ProjectResult(name=c.stem, csproj=c) for c in csprojs
    }
    if skip_build:
        return results
    for csproj in csprojs:
        if not build_project(csproj, configuration):
            results[csproj.stem].build_failed = True
    return results


def run_project(
    csproj: Path,
    configuration: str,
    tfm: str,
    coverlet: str | None,
    skip_tests: bool,
    skip_coverage: bool,
    result: ProjectResult,
) -> ProjectResult:
    """Runs tests and collects coverage for a single project. Safe to invoke
    in parallel across projects: each project has its own dll, junit
    filename, and coverage output path."""
    if result.build_failed:
        return result
    if not skip_tests:
        print(f"  running tests for {csproj.stem}...", flush=True)
        t0 = time.monotonic()
        junit, ok, err = run_tests(csproj, configuration)
        elapsed = time.monotonic() - t0
        result.junit_path = junit
        if junit is None:
            result.test_run_failed = True
            result.notes.append(f"test run failed: {err.strip()[:200]}")
        else:
            result.tests = parse_junit(junit)
            if result.tests.duration <= 0:
                result.tests.duration = elapsed
            if not ok and (result.tests.failed + result.tests.errors) == 0:
                result.notes.append(
                    "test runner exited non-zero with no recorded failures"
                )
    if not skip_coverage:
        if coverlet is None:
            result.coverage_failed = True
            result.notes.append("coverlet not available")
        else:
            print(f"  collecting coverage for {csproj.stem}...", flush=True)
            cob, err = run_coverage(csproj, configuration, tfm, coverlet)
            result.cobertura_path = cob
            if cob is None:
                result.coverage_failed = True
                result.notes.append(f"coverage failed: {err.strip()[:200]}")
            else:
                cov, _ = parse_cobertura(cob)
                result.coverage = cov
    return result


def main() -> int:
    parser = argparse.ArgumentParser(
        description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter
    )
    parser.add_argument(
        "-c",
        "--configuration",
        default=DEFAULT_CONFIG,
        help="Build configuration (default: Release)",
    )
    parser.add_argument(
        "-f",
        "--framework",
        default=DEFAULT_TFM,
        help="Target framework (default: net10.0)",
    )
    parser.add_argument(
        "-p",
        "--project",
        action="append",
        default=None,
        help="Run only the named project (repeatable). Stem of csproj file.",
    )
    parser.add_argument("--no-tests", action="store_true", help="Skip test execution")
    parser.add_argument(
        "--no-coverage", action="store_true", help="Skip coverage collection"
    )
    parser.add_argument(
        "--skip-build", action="store_true", help="Assume build outputs are current"
    )
    parser.add_argument(
        "--parallel",
        type=int,
        default=0,
        help="Max projects to test in parallel (default: min(num_projects, cpu_count))",
    )
    args = parser.parse_args()

    csprojs = discover_projects(args.project)
    if not csprojs:
        print("No test projects discovered.", file=sys.stderr)
        return 1

    coverlet = None if args.no_coverage else ensure_coverlet()
    if not args.no_coverage and coverlet is None:
        print(
            "Continuing without coverage collection (coverlet unavailable).",
            file=sys.stderr,
        )

    cpu_count = os.cpu_count() or 1
    parallel = args.parallel if args.parallel > 0 else min(len(csprojs), cpu_count)
    parallel = max(1, parallel)

    print(f"Discovered {len(csprojs)} test project(s) (parallel={parallel}):")
    for c in csprojs:
        print(f"  - {c.stem}")
    print()

    print("Build phase (sequential)..." if not args.skip_build else "Skipping build phase.", flush=True)
    project_results = build_phase(csprojs, args.configuration, args.skip_build)

    print(f"\nTest + coverage phase ({parallel}-way parallel)...", flush=True)
    phase_start = time.monotonic()
    results: list[ProjectResult] = []
    if parallel == 1:
        for csproj in csprojs:
            print(f"==> {csproj.stem}")
            results.append(
                run_project(
                    csproj,
                    args.configuration,
                    args.framework,
                    coverlet,
                    args.no_tests,
                    args.no_coverage,
                    project_results[csproj.stem],
                )
            )
    else:
        with concurrent.futures.ThreadPoolExecutor(max_workers=parallel) as ex:
            futures = {
                ex.submit(
                    run_project,
                    c,
                    args.configuration,
                    args.framework,
                    coverlet,
                    args.no_tests,
                    args.no_coverage,
                    project_results[c.stem],
                ): c
                for c in csprojs
            }
            for fut in concurrent.futures.as_completed(futures):
                results.append(fut.result())
        results.sort(key=lambda r: r.name)
    phase_wall = time.monotonic() - phase_start

    print()
    print("=" * 78)
    print(f"Test Results (test+coverage wall time: {phase_wall:.2f}s)")
    print("=" * 78)
    test_rows: list[list[str]] = []
    totals = TestStats()
    for r in results:
        if r.build_failed:
            test_rows.append([r.name, "BUILD FAIL", "-", "-", "-", "-"])
            continue
        if r.test_run_failed:
            test_rows.append([r.name, "RUN FAIL", "-", "-", "-", "-"])
            continue
        t = r.tests
        test_rows.append(
            [
                r.name,
                str(t.total),
                str(t.passed),
                str(t.failed + t.errors),
                str(t.skipped),
                f"{t.duration:.2f}s",
            ]
        )
        totals.total += t.total
        totals.passed += t.passed
        totals.failed += t.failed
        totals.errors += t.errors
        totals.skipped += t.skipped
        totals.duration += t.duration
    test_rows.append(
        [
            "TOTAL",
            str(totals.total),
            str(totals.passed),
            str(totals.failed + totals.errors),
            str(totals.skipped),
            f"{totals.duration:.2f}s",
        ]
    )
    print(
        fmt_table(
            test_rows, ["Project", "Total", "Passed", "Failed", "Skipped", "Time"]
        )
    )
    print()

    if not args.no_coverage:
        print("=" * 78)
        print("Coverage (assembly: SQLite.Framework)")
        print("=" * 78)
        per_line_maps: list[dict[tuple[str, int], tuple[int, int, int]]] = []
        cov_rows: list[list[str]] = []
        for r in results:
            if r.coverage is None:
                cov_rows.append([r.name, "-", "-", "-", "-"])
                continue
            cv = r.coverage
            cov_rows.append(
                [
                    r.name,
                    fmt_pct(cv.lines_covered, cv.lines_valid),
                    f"{cv.lines_covered}/{cv.lines_valid}",
                    fmt_pct(cv.branches_covered, cv.branches_valid),
                    f"{cv.branches_covered}/{cv.branches_valid}",
                ]
            )
            if r.cobertura_path is not None:
                _, lm = parse_cobertura(r.cobertura_path)
                per_line_maps.append(lm)
        if per_line_maps:
            combined = merge_coverage(per_line_maps)
            cov_rows.append(
                [
                    "COMBINED",
                    fmt_pct(combined.lines_covered, combined.lines_valid),
                    f"{combined.lines_covered}/{combined.lines_valid}",
                    fmt_pct(combined.branches_covered, combined.branches_valid),
                    f"{combined.branches_covered}/{combined.branches_valid}",
                ]
            )
        print(
            fmt_table(
                cov_rows, ["Project", "Lines %", "Lines", "Branches %", "Branches"]
            )
        )
        print()

    notes = [(r.name, n) for r in results for n in r.notes]
    if notes:
        print("Notes:")
        for name, n in notes:
            print(f"  [{name}] {n}")
        print()

    failed = totals.failed + totals.errors
    has_build_failures = any(r.build_failed or r.test_run_failed for r in results)
    return 1 if failed > 0 or has_build_failures else 0


if __name__ == "__main__":
    sys.exit(main())
