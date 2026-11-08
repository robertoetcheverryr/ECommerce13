#!/usr/bin/env bash
# Runs the main Postman collection against a stack started with stack.sh.
#
#   scripts/qa/run-newman.sh <reports-dir>
#
# Uses QA_PORT_BASE (default 5000) to override the environment URLs, so it can
# run on other ports without editing the JSON. The manual collection (services
# stopped) is NOT run here. Exits with Newman's code: any failed assertion
# fails the step.
set -euo pipefail

reports="${1:?usage: $0 <reports-dir>}"
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
base="${QA_PORT_BASE:-5000}"
newman="${NEWMAN:-$repo/scripts/qa/node_modules/.bin/newman}"
mkdir -p "$reports"

"$newman" run "$repo/docs/postman/ECommerce13.postman_collection.json" \
  -e "$repo/docs/postman/ECommerce13.local.postman_environment.json" \
  --env-var "productsUrl=http://127.0.0.1:$((base + 1))" \
  --env-var "usersUrl=http://127.0.0.1:$((base + 2))" \
  --env-var "ordersUrl=http://127.0.0.1:$((base + 3))" \
  --env-var "cartUrl=http://127.0.0.1:$((base + 4))" \
  --env-var "notificationsUrl=http://127.0.0.1:$((base + 5))" \
  --timeout-request 15000 \
  --timeout 300000 \
  --color off \
  --disable-unicode \
  -r cli,junit \
  --reporter-junit-export "$reports/newman-junit.xml" \
  2>&1 | tee "$reports/newman-cli.txt"
exit "${PIPESTATUS[0]}"
