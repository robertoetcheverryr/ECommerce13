#!/usr/bin/env python3
"""Summarizes a lizard CSV report (cyclomatic complexity) for the job summary.

    python3 scripts/qa/complexity-report.py <lizard.csv> [--threshold N] [--top N]

ADVISORY: functions above the threshold are reported, never failed. The script
only exits non-zero when the report itself is unusable (missing, unreadable,
malformed rows or no functions at all), because that means the tool did not run
correctly, not that the code is too complex. Standard library only.

lizard --csv columns: nloc, ccn, token, param, length, location, file,
function, long_name, start, end.
"""
import argparse
import csv
import os
import sys
from pathlib import Path


class ReportError(Exception):
    """The lizard report cannot be used."""


def parse(text):
    """Returns a list of dicts, one per function, or raises ReportError."""
    functions = []
    for number, row in enumerate(csv.reader(text.splitlines()), start=1):
        if not row:
            continue
        if len(row) != 11:
            raise ReportError(f"row {number}: expected 11 columns, got {len(row)}")
        try:
            nloc, ccn, _tokens, params, length = (int(v) for v in row[:5])
            start = int(row[9])
        except ValueError as exc:
            raise ReportError(f"row {number}: non-numeric metric ({exc})") from None
        functions.append({
            "nloc": nloc, "ccn": ccn, "params": params, "length": length,
            "file": row[6], "function": row[7], "start": start,
        })
    if not functions:
        raise ReportError("the report has no functions (did lizard parse any C# file?)")
    return functions


def summarize(functions, threshold, top):
    """Returns the markdown lines for the job summary."""
    ranked = sorted(functions, key=lambda f: (-f["ccn"], -f["nloc"], f["file"], f["start"]))
    above = [f for f in ranked if f["ccn"] > threshold]
    average = sum(f["ccn"] for f in functions) / len(functions)
    lines = [
        "## Cyclomatic complexity (advisory)",
        "",
        f"- Functions analyzed: {len(functions)}",
        f"- Average CCN: {average:.1f}, max CCN: {ranked[0]['ccn']}",
        f"- Functions above the threshold (CCN > {threshold}): {len(above)}",
        "",
        "Advisory only: this job does not fail on complexity until the quality gates are made blocking.",
        "",
        f"Top {min(top, len(ranked))} functions by CCN:",
        "",
        "| CCN | NLOC | Params | Function | Location |",
        "|---:|---:|---:|---|---|",
    ]
    for f in ranked[:top]:
        flag = " (above threshold)" if f["ccn"] > threshold else ""
        lines.append(
            f"| {f['ccn']}{flag} | {f['nloc']} | {f['params']} | `{f['function']}` | `{f['file']}:{f['start']}` |"
        )
    return lines, len(above)


def main(argv=None):
    parser = argparse.ArgumentParser(description="Summarize a lizard CSV report (advisory).")
    parser.add_argument("report", type=Path)
    parser.add_argument("--threshold", type=int, default=15)
    parser.add_argument("--top", type=int, default=15)
    args = parser.parse_args(argv)
    try:
        text = args.report.read_text(encoding="utf-8")
        functions = parse(text)
    except (OSError, UnicodeDecodeError, ReportError) as exc:
        print(f"::error::Complexity report is unusable: {exc}")
        return 1
    lines, above = summarize(functions, args.threshold, args.top)
    if above:
        print(f"::notice::{above} functions exceed CCN {args.threshold} (advisory, not blocking)")
    text = "\n".join(lines) + "\n"
    print(text)
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as fh:
            fh.write(text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
