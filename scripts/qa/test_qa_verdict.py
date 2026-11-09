"""Tests for qa-verdict.py (standard library only).

    python3 -m unittest discover -s scripts/qa -p "test_*.py" -v
"""
import copy
import importlib.util
import io
import json
import os
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path

_spec = importlib.util.spec_from_file_location("qa_verdict", Path(__file__).with_name("qa-verdict.py"))
qa_verdict = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(qa_verdict)

# Shape taken from a real schemathesis.json (Schemathesis 4.33.1), trimmed.
SUCCESS = {
    "schemathesis_version": "4.33.1", "seed": 13, "running_time": 90.0,
    "stop_reason": "max_time", "complete": True, "exit_code": 0,
    "operations": {"total": 2, "selected": 2, "tested": 2, "errored": 0, "skipped": 0},
    "phases": {"examples": {"status": "success"}, "fuzzing": {"status": "success"}},
    "test_cases": {"generated": 1500, "with_failures": 0, "unique_failures": 0,
                   "without_checks": 0, "errored": 0},
    "failures": [], "errors": [],
}
FAILURE = {"type": "ServerError", "code": "ST101", "title": "Server error", "count": 1,
           "operations": ["POST /api/orders"]}


def report(**changes):
    data = copy.deepcopy(SUCCESS)
    for key, value in changes.items():
        if isinstance(value, dict) and isinstance(data.get(key), dict):
            data[key].update(value)
        else:
            data[key] = value
    return json.dumps(data)


def findings(**changes):
    base = {"exit_code": 1, "failures": [FAILURE],
            "test_cases": {"with_failures": 1, "unique_failures": 1}}
    base.update(changes)
    return report(**base)


class EvaluateTests(unittest.TestCase):
    def check(self, exit_text, report_text, expected, code, blocking=False):
        result, step_code, reason, _ = qa_verdict.evaluate(exit_text, report_text, blocking)
        self.assertEqual((result, step_code), (expected, code), reason)
        return reason

    def test_success_with_tested_operations(self):
        self.check("0\n", report(), qa_verdict.OK, 0)

    def test_findings_are_advisory_by_default(self):
        reason = self.check("1", findings(), qa_verdict.ADVISORY, 0)
        self.assertIn("ADVISORY", reason)

    def test_findings_fail_in_blocking_mode(self):
        self.check("1", findings(), qa_verdict.FAIL, 1, blocking=True)

    def test_zero_tested_with_exit_0_fails(self):
        self.check("0", report(operations={"tested": 0}), qa_verdict.FAIL, 1)

    def test_zero_generated_with_exit_0_fails(self):
        self.check("0", report(test_cases={"generated": 0}), qa_verdict.FAIL, 1)

    def test_zero_tested_with_exit_1_fails(self):
        self.check("1", findings(operations={"tested": 0}), qa_verdict.FAIL, 1)

    def test_missing_report_fails(self):
        self.check("1", None, qa_verdict.FAIL, 1)

    def test_non_json_report_fails(self):
        self.check("1", "<html>no</html>", qa_verdict.FAIL, 1)

    def test_non_object_report_fails(self):
        self.check("0", "[]", qa_verdict.FAIL, 1)

    def test_report_missing_fields_fails(self):
        data = json.loads(report())
        del data["test_cases"]
        self.check("0", json.dumps(data), qa_verdict.FAIL, 1)

    def test_null_operations_fails(self):
        self.check("1", findings(operations=None), qa_verdict.FAIL, 1)

    def test_recorded_errors_with_findings_fail(self):
        errors = [{"title": "Network Error", "count": 1}]
        self.check("1", findings(errors=errors), qa_verdict.FAIL, 1)

    def test_errored_operations_with_findings_fail(self):
        self.check("1", findings(operations={"errored": 1}), qa_verdict.FAIL, 1)

    def test_errored_cases_with_findings_fail(self):
        self.check("1", findings(test_cases={"errored": 2, "unique_failures": 1}), qa_verdict.FAIL, 1)

    def test_errors_with_exit_0_fail(self):
        self.check("0", report(errors=[{"title": "Timeout"}]), qa_verdict.FAIL, 1)

    def test_exit_1_without_findings_fails(self):
        self.check("1", report(exit_code=1), qa_verdict.FAIL, 1)

    def test_exit_0_with_findings_fails(self):
        self.check("0", findings(exit_code=0), qa_verdict.FAIL, 1)

    def test_exit_code_mismatch_fails(self):
        self.check("1", findings(exit_code=0), qa_verdict.FAIL, 1)

    def test_exit_2_schema_error_fails(self):
        schema_error = report(exit_code=2, operations=None, stop_reason="error",
                              errors=[{"title": "Schema Loading Error", "count": 1}])
        self.check("2", schema_error, qa_verdict.FAIL, 1)

    def test_missing_exit_code_fails(self):
        self.check(None, report(), qa_verdict.FAIL, 1)

    def test_invalid_exit_code_fails(self):
        for text in ("", "abc", "1.0", "0 1"):
            with self.subTest(text=text):
                self.check(text, report(), qa_verdict.FAIL, 1)

    def test_other_exit_codes_fail(self):
        for code in ("130", "-1", "3"):
            with self.subTest(code=code):
                self.check(code, report(exit_code=int(code)), qa_verdict.FAIL, 1)

    def test_bool_counts_are_rejected(self):
        self.check("0", report(operations={"tested": True}), qa_verdict.FAIL, 1)


class MainTests(unittest.TestCase):
    def run_main(self, files, env=None):
        with tempfile.TemporaryDirectory() as tmp:
            for name, content in files.items():
                Path(tmp, name).write_text(content, encoding="utf-8")
            summary = Path(tmp, "summary.md")
            old = dict(os.environ)
            os.environ.update({"GITHUB_STEP_SUMMARY": str(summary), **(env or {})})
            try:
                out = io.StringIO()
                with redirect_stdout(out):
                    code = qa_verdict.main(["Orders", tmp])
            finally:
                os.environ.clear()
                os.environ.update(old)
            return code, out.getvalue(), summary.read_text(encoding="utf-8")

    def test_advisory_writes_warning_and_summary(self):
        code, out, summary = self.run_main({"exit-code": "1\n", "schemathesis.json": findings()},
                                           {"SCHEMATHESIS_BLOCKING": "false"})
        self.assertEqual(code, 0)
        self.assertIn("::warning::Orders", out)
        self.assertIn("| ST101 |", summary)

    def test_blocking_env_fails(self):
        code, out, _ = self.run_main({"exit-code": "1", "schemathesis.json": findings()},
                                     {"SCHEMATHESIS_BLOCKING": "true"})
        self.assertEqual(code, 1)
        self.assertIn("::error::Orders", out)

    def test_empty_directory_fails(self):
        code, out, _ = self.run_main({})
        self.assertEqual(code, 1)
        self.assertIn("::error::", out)


def junit_with(cases):
    """Build a `st --report junit` XML: cases is a list of (name, codes, body)."""
    items = "".join(
        f'<testcase classname="schemathesis" name="{name}" time="1.0">'
        f'<failure type="failure" message="{codes}">{body}</failure></testcase>'
        for name, codes, body in cases
    )
    return f'<testsuites><testsuite name="schemathesis">{items}</testsuite></testsuites>'


BODY_415 = "ST103 Undocumented status code\n Received: 415\n [415] Unsupported Media Type:\n ok"
BODY_400 = "ST202 Schema-valid request rejected\n Received: 400\n [400] Bad Request:\n ok"
BODY_406 = "ST103 Undocumented status code\n Received: 406\n [406] Not Acceptable:\n ok"
class ToleranceTests(unittest.TestCase):
    """SCHEMA_TOLERATE decided exceptions applied at the gate (never rewriting reports)."""

    def setUp(self):
        self.decisions = qa_verdict.parse_tolerations("* 415\nGET /api/orders 400")
        self.report_103 = report(
            exit_code=1, test_cases={"generated": 10, "unique_failures": 1},
            failures=[{"type": "UndefinedStatusCode", "code": "ST103",
                       "title": "Undocumented status code", "count": 1}],
        )

    def test_parse_tolerations(self):
        self.assertEqual(self.decisions, [("*", "415"), ("GET /api/orders", "400")])

    def test_fully_decided_run_is_green(self):
        junit = junit_with([("POST /api/orders", "ST103", BODY_415)])
        result, code, reason, _ = qa_verdict.evaluate(
            "1", self.report_103, junit_text=junit, tolerations=self.decisions)
        self.assertEqual((result, code), (qa_verdict.OK, 0))
        self.assertIn("decided exceptions", reason)

    def test_compound_undecided_status_keeps_red(self):
        junit = junit_with([("POST /api/orders", "ST103", BODY_415),
                            ("PUT /api/orders/{id}", "ST103", BODY_406)])
        result, code, _, _ = qa_verdict.evaluate(
            "1", self.report_103, junit_text=junit, tolerations=self.decisions)
        self.assertEqual((result, code), (qa_verdict.ADVISORY, 0))

    def test_operation_scoped_decision(self):
        report_400 = json.dumps({**json.loads(self.report_103),
                                 "failures": [{"code": "ST103", "count": 1}]})
        ok_junit = junit_with([("GET /api/orders", "ST103", BODY_400)])
        bad_junit = junit_with([("POST /api/orders", "ST103", BODY_400)])
        self.assertEqual(qa_verdict.evaluate("1", report_400, junit_text=ok_junit,
                                             tolerations=self.decisions)[0], qa_verdict.OK)
        self.assertEqual(qa_verdict.evaluate("1", report_400, junit_text=bad_junit,
                                             tolerations=self.decisions)[0], qa_verdict.ADVISORY)

    def test_stateful_cases_use_the_reproduction_curl(self):
        body = (BODY_415 + "\nReproduce with:\n    curl -X POST "
                "http://127.0.0.1:5003/api/orders?x=1")
        junit = junit_with([("Stateful tests", "ST103, ST202", body)])
        codes, statuses = qa_verdict.tolerated_codes(junit, self.decisions)
        self.assertEqual(codes, {"ST103", "ST202"})
        self.assertEqual(statuses["ST103"], ["415"])

    def test_no_junit_no_tolerated_code(self):
        codes, _ = qa_verdict.tolerated_codes(None, self.decisions)
        self.assertEqual(codes, set())
        result, _, _, _ = qa_verdict.evaluate(
            "1", self.report_103, junit_text=None, tolerations=self.decisions)
        self.assertEqual(result, qa_verdict.ADVISORY)

    def test_group_missing_from_junit_stays_open(self):
        junit = junit_with([("POST /api/orders", "ST103", BODY_415)])
        two_groups = report(
            exit_code=1, test_cases={"generated": 10, "unique_failures": 2},
            failures=[{"code": "ST103", "count": 1}, {"code": "ST201", "count": 1}],
        )
        result, _, _, _ = qa_verdict.evaluate(
            "1", two_groups, junit_text=junit, tolerations=self.decisions)
        self.assertEqual(result, qa_verdict.ADVISORY)

    def test_blocking_mode_honours_the_decision(self):
        junit = junit_with([("POST /api/orders", "ST103", BODY_415)])
        result, code, _, _ = qa_verdict.evaluate(
            "1", self.report_103, blocking=True, junit_text=junit,
            tolerations=self.decisions)
        self.assertEqual((result, code), (qa_verdict.OK, 0))


if __name__ == "__main__":
    unittest.main()
