#!/usr/bin/env python3
"""Writes a per-API code coverage table for the Tests job summary.

    python scripts/qa/coverage-summary.py <Cobertura.xml> [--details SummaryGithub.md] [--tests-dir tests]

Input: the merged Cobertura report that ReportGenerator writes from the Coverlet
results of every test project. One row per API: line and branch coverage of its
assembly, or a note when the API has no test project (Orders today). No
percentage is invented for an API without tests.

ADVISORY: low coverage never fails. The script only exits non-zero when the
report is unusable (missing, not XML, no packages) or when an API that HAS a
test project is missing from it, because that means the collector did not run
correctly. Standard library only.
"""
import argparse
import math
import os
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

APIS = ("Products", "Users", "Orders", "Cart", "Notifications")


class ReportError(Exception):
    """The coverage report cannot be used."""


def _rate(element, attribute):
    value = element.get(attribute)
    if value is None:
        return None
    try:
        rate = float(value)
    except ValueError:
        raise ReportError(f"{attribute}={value!r} is not a number") from None
    if not 0.0 <= rate <= 1.0:
        raise ReportError(f"{attribute}={value!r} is outside 0..1")
    return rate


def parse(text):
    """Returns {package name: {"line": rate, "branch": rate or None}}."""
    try:
        root = ET.fromstring(text)
    except ET.ParseError as exc:
        raise ReportError(f"not valid XML ({exc})") from None
    if root.tag != "coverage":
        raise ReportError(f"root element is <{root.tag}>, expected <coverage>")
    packages = {}
    for package in root.iter("package"):
        name = package.get("name")
        line = _rate(package, "line-rate")
        if not name or line is None:
            raise ReportError("a <package> has no name or no line-rate")
        packages[name] = {"line": line, "branch": _rate(package, "branch-rate")}
    if not packages:
        raise ReportError("the report has no packages (did any test project collect coverage?)")
    return packages


def rows(packages, tested):
    """Returns one (api, line, branch, note) tuple per API, or raises ReportError."""
    result = []
    for api in APIS:
        name = f"{api}.API"
        if name in packages:
            result.append((api, packages[name]["line"], packages[name]["branch"], ""))
        elif api in tested:
            raise ReportError(f"{name} has a test project but is not in the coverage report")
        else:
            result.append((api, None, None, "no test project, coverage not measured"))
    return result


def _pct(rate):
    # Truncated (not rounded) to one decimal, like ReportGenerator, so both reports agree.
    return "n/a" if rate is None else f"{math.floor(round(rate * 1000, 6)) / 10:.1f}%"


def summarize(table, details=None):
    lines = [
        "## Code coverage per API (advisory)",
        "",
        "Coverlet (`XPlat Code Coverage`) during `dotnet test`, merged with ReportGenerator.",
        "Advisory only: low coverage never fails this job.",
        "",
        "| API | Line coverage | Branch coverage | Note |",
        "|---|---:|---:|---|",
    ]
    for api, line, branch, note in table:
        lines.append(f"| {api}.API | {_pct(line)} | {_pct(branch)} | {note} |")
    lines.append("")
    lines.append("The HTML report and the Cobertura files are in the `coverage-report` artifact (7 days).")
    if details:
        lines += ["", "<details><summary>ReportGenerator summary</summary>", "", details.strip(), "", "</details>"]
    return lines


def tested_apis(tests_dir):
    return {api for api in APIS if (Path(tests_dir) / f"{api}.API.Tests").is_dir()}


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument("report", help="merged Cobertura.xml written by ReportGenerator")
    parser.add_argument("--details", help="optional ReportGenerator MarkdownSummaryGithub file to append")
    parser.add_argument("--tests-dir", default="tests", help="folder with the <API>.API.Tests projects")
    args = parser.parse_args(argv)
    try:
        try:
            text = Path(args.report).read_text(encoding="utf-8")
        except OSError as exc:
            raise ReportError(f"cannot read {args.report} ({exc})") from None
        table = rows(parse(text), tested_apis(args.tests_dir))
        details = None
        if args.details:
            try:
                details = Path(args.details).read_text(encoding="utf-8")
            except OSError as exc:
                raise ReportError(f"cannot read {args.details} ({exc})") from None
    except ReportError as exc:
        print(f"::error::Coverage report is unusable: {exc}")
        return 1
    text = "\n".join(summarize(table, details)) + "\n"
    print(text)
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as fh:
            fh.write(text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
