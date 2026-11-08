#!/usr/bin/env bash
# Starts or stops the five APIs (Development) with disposable SQLite databases.
#
#   scripts/qa/stack.sh start <runtime-dir>   # needs a prior build: dotnet build -c Release
#   scripts/qa/stack.sh stop  <runtime-dir>
#
# <runtime-dir> must be a NEW or disposable directory: it holds the databases
# (*.db), the Serilog logs and the console output of each service.
# Never point it at an API folder or at a database you want to keep.
#
# Optional variables:
#   QA_PORT_BASE   base port (default 5000 -> 5001..5005, like launchSettings)
#   QA_CONFIG      build configuration (default Release)
#   QA_READY_TRIES /health/ready attempts per service, 1 per second (default 60)
#   DOTNET         dotnet binary (default: dotnet on PATH)
set -euo pipefail

cmd="${1:-}"
runtime="${2:-}"
if [[ -z "$cmd" || -z "$runtime" ]]; then
  echo "usage: $0 start|stop <runtime-dir>" >&2
  exit 2
fi

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
base="${QA_PORT_BASE:-5000}"
config="${QA_CONFIG:-Release}"
tries="${QA_READY_TRIES:-60}"
dotnet_bin="${DOTNET:-dotnet}"

# Startup order: Products before Cart and Orders (both call it).
services=(Products Users Orders Cart Notifications)

port_of() {
  case "$1" in
    Products) echo $((base + 1)) ;;
    Users) echo $((base + 2)) ;;
    Orders) echo $((base + 3)) ;;
    Cart) echo $((base + 4)) ;;
    Notifications) echo $((base + 5)) ;;
  esac
}

stop_stack() {
  local pidfile pid name
  shopt -s nullglob
  for pidfile in "$runtime"/*.pid; do
    name="$(basename "$pidfile" .pid)"
    pid="$(cat "$pidfile")"
    # Only kill the PID this script recorded, and only if it is still that API's process.
    if kill -0 "$pid" 2>/dev/null && tr '\0' ' ' < "/proc/$pid/cmdline" 2>/dev/null | grep -q "$name.API"; then
      kill "$pid" 2>/dev/null || true
      for _ in $(seq 1 20); do
        kill -0 "$pid" 2>/dev/null || break
        sleep 0.5
      done
      kill -9 "$pid" 2>/dev/null || true
      echo "stopped $name.API (pid $pid)"
    fi
    rm -f "$pidfile"
  done
}

wait_ready() {
  local name="$1" port="$2" pid="$3" body
  for i in $(seq 1 "$tries"); do
    if ! kill -0 "$pid" 2>/dev/null; then
      echo "::error::$name.API exited before becoming ready (pid $pid). Last lines:" >&2
      tail -n 40 "$runtime/$name/console.log" >&2 || true
      return 1
    fi
    if body="$(curl -fsS --max-time 2 "http://127.0.0.1:$port/health/ready" 2>/dev/null)"; then
      echo "$name.API ready on :$port after ${i}s -> $body"
      return 0
    fi
    sleep 1
  done
  echo "::error::$name.API did not answer /health/ready within ${tries}s (port $port). Last lines:" >&2
  curl -sS --max-time 2 "http://127.0.0.1:$port/health/ready" >&2 || true
  echo >&2
  tail -n 40 "$runtime/$name/console.log" >&2 || true
  return 1
}

start_stack() {
  mkdir -p "$runtime"
  runtime="$(cd "$runtime" && pwd)"
  if compgen -G "$runtime/*.pid" >/dev/null; then
    echo "::error::$runtime already has recorded PIDs; use a new directory or run stop" >&2
    exit 1
  fi
  local name port out dll pid db products_url orders_url
  # Before starting anything: no port may already be answering.
  for name in "${services[@]}"; do
    port="$(port_of "$name")"
    if curl -sS --max-time 1 -o /dev/null "http://127.0.0.1:$port/" 2>/dev/null; then
      echo "::error::port $port is already in use; choose another QA_PORT_BASE" >&2
      exit 1
    fi
  done
  for name in "${services[@]}"; do
    port="$(port_of "$name")"
    out="$repo/src/$name.API/bin/$config/net10.0"
    dll="$out/$name.API.dll"
    if [[ ! -f "$dll" ]]; then
      echo "::error::missing $dll; run first: dotnet build -c $config" >&2
      stop_stack
      exit 1
    fi
    # Own working directory: Serilog writes logs/ relative to it, and the database lives there.
    mkdir -p "$runtime/$name"
    db="$runtime/$name/$(echo "$name" | tr '[:upper:]' '[:lower:]').db"
    products_url="http://127.0.0.1:$(port_of Products)"
    orders_url="http://127.0.0.1:$(port_of Orders)"
    (
      cd "$runtime/$name"
      export ASPNETCORE_ENVIRONMENT=Development
      export ASPNETCORE_URLS="http://127.0.0.1:$port"
      export ConnectionStrings__DefaultConnection="Data Source=$db"
      export Orders__BaseUrl="$orders_url"
      export Products__BaseUrl="$products_url"
      export Services__ProductsApi__BaseUrl="$products_url"
      exec "$dotnet_bin" "$dll" --contentRoot "$out" > console.log 2>&1
    ) &
    pid=$!
    echo "$pid" > "$runtime/$name.pid"
    echo "started $name.API (pid $pid, port $port, data in $runtime/$name)"
    if ! wait_ready "$name" "$port" "$pid"; then
      stop_stack
      exit 1
    fi
  done
}

case "$cmd" in
  start) start_stack ;;
  stop)
    [[ -d "$runtime" ]] || exit 0
    runtime="$(cd "$runtime" && pwd)"
    stop_stack
    ;;
  *) echo "unknown command: $cmd" >&2; exit 2 ;;
esac
