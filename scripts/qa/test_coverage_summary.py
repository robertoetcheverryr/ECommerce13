"""Tests for coverage-summary.py (standard library only).

    python3 -m unittest discover -s scripts/qa -p "test_*.py" -v
"""
import importlib.util
import io
import os
import tempfile
import unittest
from contextlib import redirect_stdout
from pathlib import Path

_spec = importlib.util.spec_from_file_location(
    "coverage_summary", Path(__file__).with_name("coverage-summary.py"))
coverage_summary = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(coverage_summary)


def report(*packages):
    body = "".join(
        f'<package name="{name}" line-rate="{line}" branch-rate="{branch}" complexity="1" />'
        for name, line, branch in packages)
    return f'<?xml version="1.0"?><coverage line-rate="0.7"><packages>{body}</packages></coverage>'


FOUR = report(("Products.API", "0.9598", "0.7123"), ("Users.API", "0.9369", "0.7071"),
              ("Cart.API", "0.6682", "0.3252"), ("Notifications.API", "0.3530", "0.0317"))
TESTED = {"Products", "Users", "Cart", "Notifications"}


class ParseTests(unittest.TestCase):
    def test_reads_packages(self):
        packages = coverage_summary.parse(FOUR)
        self.assertAlmostEqual(packages["Cart.API"]["line"], 0.6682)
        self.assertEqual(len(packages), 4)

    def test_invalid_xml_is_an_error(self):
        with self.assertRaises(coverage_summary.ReportError):
            coverage_summary.parse("<coverage>")

    def test_no_packages_is_an_error(self):
        with self.assertRaises(coverage_summary.ReportError):
            coverage_summary.parse(report())

    def test_rate_outside_range_is_an_error(self):
        with self.assertRaises(coverage_summary.ReportError):
            coverage_summary.parse(report(("Cart.API", "1.5", "0")))

    def test_wrong_root_is_an_error(self):
        with self.assertRaises(coverage_summary.ReportError):
            coverage_summary.parse("<report />")


class RowsTests(unittest.TestCase):
    def test_api_without_test_project_gets_a_note_not_a_number(self):
        table = coverage_summary.rows(coverage_summary.parse(FOUR), TESTED)
        orders = [row for row in table if row[0] == "Orders"][0]
        self.assertEqual(orders[1:3], (None, None))
        self.assertIn("no test project", orders[3])

    def test_tested_api_missing_from_report_is_an_error(self):
        with self.assertRaises(coverage_summary.ReportError):
            coverage_summary.rows(coverage_summary.parse(FOUR), TESTED | {"Orders"})


class MainTests(unittest.TestCase):
    def run_main(self, xml, details=None):
        with tempfile.TemporaryDirectory() as tmp:
            tmp = Path(tmp)
            for api in TESTED:
                (tmp / "tests" / f"{api}.API.Tests").mkdir(parents=True)
            (tmp / "Cobertura.xml").write_text(xml, encoding="utf-8")
            argv = [str(tmp / "Cobertura.xml"), "--tests-dir", str(tmp / "tests")]
            if details is not None:
                (tmp / "SummaryGithub.md").write_text(details, encoding="utf-8")
                argv += ["--details", str(tmp / "SummaryGithub.md")]
            summary = tmp / "summary.md"
            old = os.environ.get("GITHUB_STEP_SUMMARY")
            os.environ["GITHUB_STEP_SUMMARY"] = str(summary)
            try:
                out = io.StringIO()
                with redirect_stdout(out):
                    code = coverage_summary.main(argv)
            finally:
                if old is None:
                    del os.environ["GITHUB_STEP_SUMMARY"]
                else:
                    os.environ["GITHUB_STEP_SUMMARY"] = old
            text = summary.read_text(encoding="utf-8") if summary.exists() else ""
            return code, out.getvalue(), text

    def test_low_coverage_does_not_fail(self):
        code, _, summary = self.run_main(FOUR, details="# Summary\nline")
        self.assertEqual(code, 0)
        self.assertIn("| Notifications.API | 35.3% | 3.1% |  |", summary)
        self.assertIn("| Orders.API | n/a | n/a | no test project, coverage not measured |", summary)
        self.assertIn("ReportGenerator summary", summary)

    def test_unusable_report_fails(self):
        code, out, summary = self.run_main("not xml")
        self.assertEqual(code, 1)
        self.assertIn("::error::", out)
        self.assertEqual(summary, "")

    def test_missing_report_fails(self):
        out = io.StringIO()
        with redirect_stdout(out):
            code = coverage_summary.main(["/nonexistent/Cobertura.xml"])
        self.assertEqual(code, 1)


if __name__ == "__main__":
    unittest.main()
