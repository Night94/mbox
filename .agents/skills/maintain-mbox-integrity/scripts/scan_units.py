#!/usr/bin/env python3
"""Cheap structural inventory for MBOX unit evaluation."""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any

import yaml


EXCLUDED_DIRS = {".git", "bin", "obj", "models"}
DEFINITION_RE = re.compile(
    r"(?ms)^## Definition\s*\n\s*```yaml\s*\n(.*?)\n```"
)


def relative_path(root: Path, path: Path) -> str:
    return "/" + path.relative_to(root).as_posix()


def read_unit(root: Path, path: Path, invalid: list[dict[str, str]]) -> dict[str, Any] | None:
    text = path.read_text(encoding="utf-8-sig")
    lines = text.splitlines()
    if not lines or lines[0].strip() != "---":
        return None

    try:
        closing = lines[1:].index("---") + 1
    except ValueError:
        return None

    try:
        header = yaml.safe_load("\n".join(lines[1:closing]))
    except yaml.YAMLError as error:
        invalid.append({"path": relative_path(root, path), "problem": f"invalid YAML header: {error}"})
        return None

    if not isinstance(header, dict) or header.get("mbox_unit") != 1:
        return None

    required = {"unit": str, "type": str, "version": int, "uses": dict}
    for field, field_type in required.items():
        if not isinstance(header.get(field), field_type):
            invalid.append({"path": relative_path(root, path), "problem": f"invalid or missing {field}"})
            return None
    if header["version"] < 1:
        invalid.append({"path": relative_path(root, path), "problem": "version is not positive"})
    for dependency, version in header["uses"].items():
        if not isinstance(dependency, str) or not isinstance(version, int) or version < 1:
            invalid.append({"path": relative_path(root, path), "problem": "uses must map unit identifiers to positive versions"})

    return {
        "unit": header["unit"],
        "type": header["type"],
        "version": header["version"],
        "uses": header["uses"],
        "path": relative_path(root, path),
        "file_path": path,
        "payload": "\n".join(lines[closing + 1 :]),
    }


def find_units(root: Path, invalid: list[dict[str, str]]) -> list[dict[str, Any]]:
    units: list[dict[str, Any]] = []
    for path in root.rglob("*.md"):
        if any(part in EXCLUDED_DIRS for part in path.relative_to(root).parts):
            continue
        unit = read_unit(root, path, invalid)
        if unit is not None:
            units.append(unit)
    return units


def catalog_issues(
    root: Path,
    catalog: dict[str, Any],
    units: list[dict[str, Any]],
) -> list[dict[str, Any]]:
    issues: list[dict[str, Any]] = []
    match = DEFINITION_RE.search(catalog["payload"])
    if not match:
        return [{"catalog": catalog["unit"], "problem": "missing YAML Definition block"}]

    try:
        definition = yaml.safe_load(match.group(1))
        scope = definition["scope"]
        entries = definition["catalog"]
        prefix = scope["pathPrefix"]
        unit_types = set(scope["unitTypes"])
        coverage = scope["coverage"]
    except (KeyError, TypeError, yaml.YAMLError) as error:
        return [{"catalog": catalog["unit"], "problem": f"invalid catalog definition: {error}"}]

    if coverage != "complete" or not isinstance(prefix, str) or not unit_types:
        return [{"catalog": catalog["unit"], "problem": "catalog must define non-empty complete scope"}]

    scope_path = (root / prefix.lstrip("/")).resolve()
    actual = {
        unit["unit"]
        for unit in units
        if unit["type"] in unit_types and unit["file_path"].resolve().is_relative_to(scope_path)
    }
    try:
        listed = [entry["unit"] for entry in entries]
    except (KeyError, TypeError):
        return [{"catalog": catalog["unit"], "problem": "catalog entries must identify units"}]
    listed_set = set(listed)

    if len(listed) != len(listed_set):
        issues.append({"catalog": catalog["unit"], "problem": "duplicate catalog entries"})
    missing_entries = sorted(actual - listed_set)
    extra_entries = sorted(listed_set - actual)
    if missing_entries:
        issues.append({"catalog": catalog["unit"], "problem": "missing scope entries", "units": missing_entries})
    if extra_entries:
        issues.append({"catalog": catalog["unit"], "problem": "entries outside scope", "units": extra_entries})

    dependency_units = set(catalog["uses"]) - {"catalog"}
    missing_dependencies = sorted(actual - dependency_units)
    if missing_dependencies:
        issues.append({"catalog": catalog["unit"], "problem": "missing member dependencies", "units": missing_dependencies})
    return issues


def scan(root: Path) -> dict[str, Any]:
    invalid: list[dict[str, str]] = []
    units = find_units(root, invalid)
    grouped: dict[str, list[dict[str, Any]]] = {}
    for unit in units:
        grouped.setdefault(unit["unit"], []).append(unit)

    duplicates = [
        {"unit": identifier, "paths": sorted(item["path"] for item in records)}
        for identifier, records in grouped.items()
        if len(records) > 1
    ]
    indexed = {identifier: records[0] for identifier, records in grouped.items() if len(records) == 1}
    missing_dependencies: list[dict[str, str]] = []
    mismatches: list[dict[str, Any]] = []
    governing_spec_issues: list[dict[str, str]] = []

    for unit in units:
        for dependency, evaluated_version in unit["uses"].items():
            target = indexed.get(dependency)
            if target is None:
                missing_dependencies.append({"unit": unit["unit"], "dependency": dependency})
            elif target["version"] != evaluated_version:
                mismatches.append(
                    {
                        "unit": unit["unit"],
                        "dependency": dependency,
                        "evaluatedVersion": evaluated_version,
                        "currentVersion": target["version"],
                    }
                )

        governing = "spec" if unit["type"] == "spec" and unit["unit"] != "spec" else unit["type"]
        if unit["unit"] == "spec" and unit["type"] == "spec":
            continue
        target = indexed.get(governing)
        if governing not in unit["uses"] or target is None or target["type"] != "spec":
            governing_spec_issues.append({"unit": unit["unit"], "requiredGoverningSpec": governing})

    catalogs = [unit for unit in units if unit["type"] == "catalog"]
    catalog_scope_issues = [
        issue for catalog in catalogs for issue in catalog_issues(root, catalog, units)
    ]

    return {
        "summary": {
            "units": len(units),
            "invalidUnits": len(invalid),
            "duplicateIdentifiers": len(duplicates),
            "missingDependencies": len(missing_dependencies),
            "dependencyVersionMismatches": len(mismatches),
            "governingSpecIssues": len(governing_spec_issues),
            "catalogScopeIssues": len(catalog_scope_issues),
        },
        "invalidUnits": invalid,
        "duplicateIdentifiers": duplicates,
        "missingDependencies": missing_dependencies,
        "dependencyVersionMismatches": mismatches,
        "governingSpecIssues": governing_spec_issues,
        "catalogScopeIssues": catalog_scope_issues,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("repository", nargs="?", default=".", help="repository root")
    parser.add_argument("--fail-on-issues", action="store_true", help="return non-zero when issues are found")
    args = parser.parse_args()

    report = scan(Path(args.repository).resolve())
    print(json.dumps(report, indent=2))
    issue_count = sum(
        report["summary"][key]
        for key in report["summary"]
        if key != "units"
    )
    return 1 if args.fail_on_issues and issue_count else 0


if __name__ == "__main__":
    sys.exit(main())
