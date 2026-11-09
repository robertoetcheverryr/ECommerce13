#!/usr/bin/env python3
"""Interprets the result of run-schemathesis.sh and writes the job summary.

    python3 scripts/qa/qa-verdict.py <api> <reports-dir>

Reads <reports-dir>/exit-code and <reports-dir>/schemathesis.json. Only two
outcomes do not fail the job:
  - exit 0 with a valid report, tested operations, no failures and no errors.
  - exit 1 with a valid report, tested operations, at least one API failure and
    no errors: expected findings while they are triaged. Unless
    SCHEMATHESIS_BLOCKING is "true" they are ADVISORY (warning, the job does not
    fail); with "true" they fail the job. The failures stay in the report.
Everything else always fails the job: a missing or non-integer exit-code, any
other exit code, a report that is missing / not JSON / not the expected shape,
a mismatched report exit_code, zero tested operations, recorded errors
(report.errors, operations.errored or test_cases.errored, even when there are
also failures), or exit 0/1 that does not match the report's failures.
Standard library only.
"""
import json
import os
import sys
from pathlib import Path

OK, ADVISORY, FAIL = "ok", "advisory", "fail"


def _int(value):
    """Non-negative int (not bool), or None."""
    if isinstance(value, bool) or not isinstance(value, int) or value < 0:
        return None
    return value


def evaluate(exit_text, report_text, blocking=False):
    """Returns (result, step exit code, reason, detail lines).

    exit_text / report_text are the file contents, or None when missing.
    """
    details = []

    def fail(reason):
        return FAIL, 1, reason, details

    if exit_text is None:
        return fail("INFRASTRUCTURE ERROR: exit-code is missing (Schemathesis never ran).")
    try:
        st_code = int(exit_text.strip())
    except ValueError:
        return fail(f"INFRASTRUCTURE ERROR: invalid exit-code ({exit_text.strip()[:40]!r}).")

    if report_text is None:
        return fail(f"INFRASTRUCTURE ERROR: schemathesis.json is missing (exit {st_code}).")
    try:
        report = json.loads(report_text)
    except json.JSONDecodeError as exc:
        return fail(f"INFRASTRUCTURE ERROR: schemathesis.json is not valid JSON ({exc.msg}).")
    if not isinstance(report, dict):
        return fail("INFRASTRUCTURE ERROR: schemathesis.json is not an object.")

    ops = report.get("operations") if isinstance(report.get("operations"), dict) else {}
    cases = report.get("test_cases") if isinstance(report.get("test_cases"), dict) else {}
    phases = report.get("phases") if isinstance(report.get("phases"), dict) else {}
    failures = report.get("failures") if isinstance(report.get("failures"), list) else None
    errors = report.get("errors") if isinstance(report.get("errors"), list) else None
    tested, op_errored = _int(ops.get("tested")), _int(ops.get("errored"))
    generated, case_errored = _int(cases.get("generated")), _int(cases.get("errored"))
    unique = _int(cases.get("unique_failures"))

    running = report.get("running_time")
    running = f"{running:.0f}s" if isinstance(running, (int, float)) and not isinstance(running, bool) else "?"
    details += [
        f"- Schemathesis {report.get('schemathesis_version')}, seed {report.get('seed')}, "
        f"{running}, stop reason: {report.get('stop_reason')}",
        f"- Operations: {ops.get('tested')}/{ops.get('selected')} tested "
        f"({ops.get('errored')} errored, {ops.get('skipped')} skipped)",
        f"- Generated cases: {cases.get('generated')}, with failures: {cases.get('with_failures')}, "
        f"unique failures: {cases.get('unique_failures')}, errored: {cases.get('errored')}",
        "- Phases: " + ", ".join(
            f"{name} {info.get('status') if isinstance(info, dict) else info}" for name, info in phases.items()
        ),
        "",
    ]
    if failures:
        details += ["| Code | Failure | Count | Operations |", "|---|---|---|---|"]
        for f in failures:
            f = f if isinstance(f, dict) else {"title": str(f)}
            operations = ", ".join(f"`{o}`" for o in (f.get("operations") or []))
            details.append(f"| {f.get('code')} | {f.get('title')} | {f.get('count')} | {operations} |")
        details.append("")
    if errors:
        details.append(f"Recorded errors ({len(errors)}):")
        details += [f"- `{json.dumps(e, ensure_ascii=False)[:300]}`" for e in errors]
        details.append("")

    if st_code not in (0, 1):
        return fail(f"INFRASTRUCTURE OR SCHEMATHESIS ERROR (exit {st_code}).")
    missing = [name for name, value in (
        ("operations.tested", tested), ("operations.errored", op_errored),
        ("test_cases.generated", generated), ("test_cases.errored", case_errored),
        ("test_cases.unique_failures", unique),
    ) if value is None]
    if failures is None:
        missing.append("failures")
    if errors is None:
        missing.append("errors")
    if missing:
        return fail("ERROR: schemathesis.json does not have the expected shape (" + ", ".join(missing) + ").")
    if report.get("exit_code") != st_code:
        return fail(f"ERROR: exit-code {st_code} does not match the report exit_code {report.get('exit_code')!r}.")
    if tested == 0 or generated == 0:
        return fail("ERROR: Schemathesis tested no operations / generated no cases.")
    if errors or op_errored or case_errored:
        return fail(
            f"ERROR during the run: {len(errors)} recorded errors, {op_errored} errored operations "
            f"and {case_errored} errored cases (plus {unique} unique failures). Not an advisory finding."
        )
    if st_code == 0:
        if unique or failures:
            return fail(f"ERROR: exit 0 but the report has {unique} unique failures.")
        return OK, 0, "OK, all checks passed.", details
    if not unique or not failures:
        return fail("ERROR: exit 1 with no failures recorded in the report.")
    if blocking:
        return FAIL, 1, f"FAILED: {unique} unique failures (blocking mode).", details
    return ADVISORY, 0, (
        f"ADVISORY: {unique} unique failures found. Not blocking the PR yet; this is not a green QA run."
    ), details


def _read(path):
    try:
        return path.read_text(encoding="utf-8")
    except (FileNotFoundError, IsADirectoryError):
        return None
    except UnicodeDecodeError:
        return "\ufffd"  # neither JSON nor an integer: evaluate rejects it


def main(argv=None):
    argv = sys.argv[1:] if argv is None else argv
    if len(argv) != 2:
        print(__doc__, file=sys.stderr)
        return 2
    api, reports = argv[0], Path(argv[1])
    blocking = os.environ.get("SCHEMATHESIS_BLOCKING", "false").lower() == "true"
    result, code, reason, details = evaluate(
        _read(reports / "exit-code"), _read(reports / "schemathesis.json"), blocking
    )
    if result == ADVISORY:
        print(f"::warning::{api}: {reason}")
    elif result == FAIL:
        print(f"::error::{api}: {reason}")
    lines = [f"## Schemathesis - {api}.API", "", f"**Result:** {reason}", "", *details,
             "Details and reproduction commands: job artifact "
             "(`schemathesis-cli.txt`, `html/index.html`)."]
    text = "\n".join(lines) + "\n"
    print(text)
    summary = os.environ.get("GITHUB_STEP_SUMMARY")
    if summary:
        with open(summary, "a", encoding="utf-8") as fh:
            fh.write(text)
    return code


if __name__ == "__main__":
    sys.exit(main())
