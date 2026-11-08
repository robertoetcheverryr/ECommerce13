"""Tests for complexity-report.py (standard library only).

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
    "complexity_report", Path(__file__).with_name("complexity-report.py"))
complexity_report = importlib.util.module_from_spec(_spec)
_spec.loader.exec_module(complexity_report)

# Rows in the lizard 1.25.1 --csv format (no header).
SIMPLE = ('4,1,14,1,4,"A::A@14-17@src/X.API/A.cs","src/X.API/A.cs","A::A","A::A( int x)",14,17\n')
COMPLEX = ('89,22,401,2,116,"S::Create@117-232@src/X.API/S.cs","src/X.API/S.cs","S::Create",'
           '"S::Create( Request r ,\n CancellationToken ct)",117,232\n')


class ParseTests(unittest.TestCase):
    def test_parses_rows_including_multiline_names(self):
        functions = complexity_report.parse(SIMPLE + COMPLEX)
        self.assertEqual([f["ccn"] for f in functions], [1, 22])
        self.assertEqual(functions[1]["start"], 117)

    def test_empty_report_is_an_error(self):
        with self.assertRaises(complexity_report.ReportError):
            complexity_report.parse("")

    def test_wrong_column_count_is_an_error(self):
        with self.assertRaises(complexity_report.ReportError):
            complexity_report.parse("1,2,3\n")

    def test_non_numeric_metric_is_an_error(self):
        with self.assertRaises(complexity_report.ReportError):
            complexity_report.parse(SIMPLE.replace("4,1,", "4,x,", 1))


class MainTests(unittest.TestCase):
    def run_main(self, content):
        with tempfile.TemporaryDirectory() as tmp:
            report = Path(tmp, "lizard.csv")
            if content is not None:
                report.write_text(content, encoding="utf-8")
            summary = Path(tmp, "summary.md")
            old = dict(os.environ)
            os.environ["GITHUB_STEP_SUMMARY"] = str(summary)
            try:
                out = io.StringIO()
                with redirect_stdout(out):
                    code = complexity_report.main([str(report), "--threshold", "15"])
            finally:
                os.environ.clear()
                os.environ.update(old)
            return code, out.getvalue(), summary.read_text(encoding="utf-8") if summary.exists() else ""

    def test_functions_above_threshold_are_advisory(self):
        code, out, summary = self.run_main(SIMPLE + COMPLEX)
        self.assertEqual(code, 0)
        self.assertIn("::notice::1 functions exceed CCN 15", out)
        self.assertIn("| 22 (above threshold) |", summary)
        self.assertIn("Functions above the threshold (CCN > 15): 1", summary)

    def test_clean_report_has_no_notice(self):
        code, out, summary = self.run_main(SIMPLE)
        self.assertEqual(code, 0)
        self.assertNotIn("::notice::", out)
        self.assertIn("Functions analyzed: 1", summary)

    def test_missing_report_fails(self):
        code, out, _ = self.run_main(None)
        self.assertEqual(code, 1)
        self.assertIn("::error::", out)

    def test_empty_report_fails(self):
        code, out, _ = self.run_main("")
        self.assertEqual(code, 1)
        self.assertIn("::error::", out)


if __name__ == "__main__":
    unittest.main()
