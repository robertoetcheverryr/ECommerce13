#!/usr/bin/env bash
# Runs Schemathesis against the OpenAPI of ONE API of a stack started with stack.sh.
#
#   scripts/qa/run-schemathesis.sh <Products|Users|Orders|Cart|Notifications> <reports-dir>
#
# DESTRUCTIVE: creates, updates and deletes data and can lock users. Only use it
# against a stack with disposable databases (stack.sh with a new directory).
#
# Optional variables:
#   QA_PORT_BASE        base port (default 5000)
#   ST_MAX_TIME         time budget in seconds for the run (default 90)
#   ST_MAX_EXAMPLES     cases per operation in fuzzing / sequences in stateful (default 30)
#   ST_SEED             seed (default 13)
#   ST                  schemathesis binary (default: st on PATH)
#
# Exits with Schemathesis' code and writes it to <reports-dir>/exit-code:
#   0 = all checks passed, 1 = found failures, 2 = could not run
#   (schema does not load, invalid options, nothing tested). The job decides
#   what to do with each code (see qa-verdict.py).
set -uo pipefail

api="${1:?usage: $0 <api> <reports-dir>}"
reports="${2:?usage: $0 <api> <reports-dir>}"
base="${QA_PORT_BASE:-5000}"
st_bin="${ST:-st}"

case "$api" in
  Products) port=$((base + 1)) ;;
  Users) port=$((base + 2)) ;;
  Orders) port=$((base + 3)) ;;
  Cart) port=$((base + 4)) ;;
  Notifications) port=$((base + 5)) ;;
  *) echo "unknown API: $api" >&2; exit 2 ;;
esac

mkdir -p "$reports"
rm -f "$reports/exit-code"

# No operation or check filters: every OpenAPI operation is tested with all the
# default checks. Nulls and limits are not excluded.
"$st_bin" --no-color run "http://127.0.0.1:$port/swagger/v1/swagger.json" \
  --phases examples,coverage,fuzzing,stateful \
  --seed "${ST_SEED:-13}" \
  --max-examples "${ST_MAX_EXAMPLES:-30}" \
  --max-time "${ST_MAX_TIME:-90}" \
  --workers 1 \
  --request-timeout 10 \
  --wait-for-schema 30 \
  --generation-database none \
  --report junit,json,html \
  --report-dir "$reports" \
  --report-junit-path "$reports/schemathesis-junit.xml" \
  --report-json-path "$reports/schemathesis.json" \
  --report-html-path "$reports/html" \
  > "$reports/schemathesis-cli.txt" 2>&1
code=$?
echo "$code" > "$reports/exit-code"

# The full output stays in the file; the job console only shows the summary.
tail -n 60 "$reports/schemathesis-cli.txt"
echo "schemathesis exit code: $code"
exit "$code"
