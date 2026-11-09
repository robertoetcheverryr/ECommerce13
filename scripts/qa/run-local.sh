#!/usr/bin/env bash
# Local run equivalent to the api-qa.yml workflow: Newman and Schemathesis, each
# one on a NEW stack with disposable databases in a temporary directory.
#
#   scripts/qa/run-local.sh [newman|Products|Users|Orders|Cart|Notifications ...]
#
# With no arguments it runs everything. Requires a prior build (dotnet build -c Release),
# newman (npm ci in scripts/qa) and schemathesis (pip install -r scripts/qa/requirements.txt).
# Reports go to $QA_OUT (default: ./qa-artifacts, git-ignored).
set -uo pipefail

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
out="${QA_OUT:-$repo/qa-artifacts}"
work="$(mktemp -d "${TMPDIR:-/tmp}/ec13-qa.XXXXXX")"
current=""
# shellcheck disable=SC2329  # invoked from the EXIT trap
cleanup() {
  [[ -n "$current" ]] && bash "$repo/scripts/qa/stack.sh" stop "$current" >/dev/null
  rm -rf "$work"
}
trap cleanup EXIT
trap 'exit 130' INT TERM

targets=("$@")
[[ ${#targets[@]} -eq 0 ]] && targets=(newman Products Users Orders Cart Notifications)
status=0

for target in "${targets[@]}"; do
  current="$work/$target"
  echo "=== $target: new stack in $current"
  if ! bash "$repo/scripts/qa/stack.sh" start "$current"; then
    echo "=== $target: ERROR starting the stack"
    status=1
    current=""
    continue
  fi
  if [[ "$target" == newman ]]; then
    bash "$repo/scripts/qa/run-newman.sh" "$out/newman" > /dev/null
    code=$?
    echo "=== newman: exit $code (details in $out/newman/newman-cli.txt)"
    tail -n 22 "$out/newman/newman-cli.txt"
    [[ $code -eq 0 ]] || status=1
  else
    bash "$repo/scripts/qa/run-schemathesis.sh" "$target" "$out/schemathesis-$target" > /dev/null
    python3 "$repo/scripts/qa/qa-verdict.py" "$target" "$out/schemathesis-$target" || status=1
  fi
  bash "$repo/scripts/qa/stack.sh" stop "$current" >/dev/null
  current=""
done
exit "$status"
