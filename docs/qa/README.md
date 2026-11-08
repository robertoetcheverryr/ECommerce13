# Automated API QA (Newman + Schemathesis)

Added as a learning change: it tests the five APIs **actually running** (Development, Swagger enabled) with two tools that complement the xUnit tests in `dotnet test`; they do not replace them. xUnit is still what checks specific business rules; these tests look at each API from the outside, over HTTP.

| Tool | Pinned version | What it does | In CI |
|------|----------------|--------------|-------|
| [Newman](https://github.com/postmanlabs/newman) | 6.2.3 (`scripts/qa/package.json` + `package-lock.json`) | Runs the main Postman collection (`docs/postman/ECommerce13.postman_collection.json`): the demo flows across services, with all their assertions. | **Mandatory**: any failed assertion fails the job. |
| [Schemathesis](https://schemathesis.readthedocs.io/) | 4.33.1 (`scripts/qa/requirements.txt`) | Reads each API's OpenAPI (`/swagger/v1/swagger.json`) and generates valid and invalid requests (nulls, numeric extremes, odd strings, extra or missing fields), looking for 500s, undocumented status codes and responses that break the contract. | **Advisory** for now (see below). |

## Architecture

```
.github/workflows/api-qa.yml      API QA workflow (Newman + Schemathesis)
.github/workflows/code-quality.yml  advisory cyclomatic complexity (lizard), SBOM, workflow/script lint
.github/workflows/tests.yml       xUnit tests plus per-API code coverage (Coverlet + ReportGenerator)
.github/workflows/scorecard.yml   OpenSSF Scorecard (published results, Security tab)
.github/workflows/dependency-review.yml  dependency review on pull requests
.github/dependabot.yml            weekly dependency update PRs
scripts/qa/stack.sh               starts/stops the 5 APIs with disposable SQLite databases
scripts/qa/run-newman.sh          runs the main Postman collection
scripts/qa/run-schemathesis.sh    runs Schemathesis against one API
scripts/qa/qa-verdict.py          interprets the Schemathesis result and writes the job summary
scripts/qa/test_qa_verdict.py     unit tests (unittest, standard library only) for qa-verdict.py
scripts/qa/run-local.sh           the same as the workflow, on a local machine
scripts/qa/complexity-report.py   summarizes the lizard CSV report for the Code Quality job
scripts/qa/test_complexity_report.py  unit tests for complexity-report.py
scripts/qa/coverage-summary.py    per-API coverage table for the Tests job summary
scripts/qa/test_coverage_summary.py   unit tests for coverage-summary.py
.yamllint.yml                     yamllint rules for the workflow lint job
```

`stack.sh start <dir>` runs the `.dll` files from the Release build (`dotnet build -c Release`), in the order Products, Users, Orders, Cart, Notifications, with:

- `ASPNETCORE_ENVIRONMENT=Development` (Swagger enabled and the same Development configuration as a demo with `dotnet run`);
- each API listening only on `127.0.0.1`, on `QA_PORT_BASE + 1..5` (5001..5005 by default, the `launchSettings.json` ports);
- `ConnectionStrings__DefaultConnection` pointing to a new database inside `<dir>/<API>/`, and each API's working directory in that same folder (the Serilog logs and `console.log` end up there). It does not touch the databases or the `logs/` folders under `src/`;
- the service-to-service URLs (`Orders:BaseUrl`, `Products:BaseUrl`, `Services:ProductsApi:BaseUrl`) built from the same ports, so Products ↔ Orders and Cart → Products are connected.

Before starting, it checks that no port is already in use. After starting each API it waits for `/health/ready` (60 attempts, one per second); if the API exits or does not answer, it prints the last lines of its console, stops whatever it had started and fails. `stack.sh stop <dir>` only stops the PIDs it recorded in `<dir>/*.pid`, and only if they are still that API's processes.

## The `api-qa.yml` workflow

Runs on every pull request to `master`/`main` and manually from **Actions → API QA → Run workflow**. It has no schedule. It only uses `ubuntu-latest` (standard runner: on a public repo it does not consume paid minutes). A new push to the same PR cancels the previous run (`concurrency` with `cancel-in-progress`).

| Job | Timeout | What it does |
|-----|---------|--------------|
| `newman` | 15 min | Build, fresh stack, main collection. Fails if any assertion fails. |
| `schemathesis` (matrix Products, Users, Orders, Cart, Notifications) | 20 min each | `qa-verdict.py` tests, build, fresh stack **per API**, checks the OpenAPI responds, runs Schemathesis, checks the five APIs are still alive and writes the summary. |

Each matrix job runs on a fresh runner, so **Newman and each Schemathesis API get their own stack and their own databases**: the junk products, locked users and orders left by fuzzing one API do not contaminate Newman or the others. All five APIs are always started, even when only one is tested, because Products calls Orders on delete and Cart/Orders call Products.

### Why Schemathesis is advisory

The first run already finds real failures (see below). Until they are triaged and fixed in other PRs, the job **does not block the PR, but it does not go silently green either**:

- The `Schemathesis` step never fails on its own and does not use `continue-on-error`: `run-schemathesis.sh` records Schemathesis' exit code in `exit-code` and the step logs it. That way findings do not show up as a failed step or as a `Process completed with exit code 1` annotation (the first CI run had one per API).
- The `Schemathesis verdict` step (`qa-verdict.py`) reads the exit code (`exit-code`) and `schemathesis.json`. Only two cases do not fail the job:
  - `0` with at least one tested operation, no failures and no errors: all checks passed.
  - `1` with at least one tested operation, at least one recorded failure and no errors: API failures. It adds a **warning** visible on the PR with the number of unique failures and writes the table into the job summary. The job does not fail.
- Everything else is an **infrastructure or run error** and fails the job: an `exit-code` that is missing or not an integer, `2` or any other code, a report that is missing, not JSON or not in the expected shape, a report `exit_code` that differs from the real one, zero tested operations or zero generated cases, recorded errors (`errors`, `operations.errored` or `test_cases.errored`, **even when there are also failures**), `1` with no failures in the report, or `0` with failures.
- If any API stopped answering `/health/ready` after fuzzing, the job fails.
- Installing tools, building and starting the stack are normal steps: if they fail, the job fails.

There is no baseline and nothing is excluded: no operations, checks, nulls or limits are filtered. To make it mandatory, change the `SCHEMATHESIS_BLOCKING` default to `'true'` in the workflow (or pick `schemathesis_blocking` when running it manually): then any failure fails the job.

Plan: this PR merges with Schemathesis still advisory, because the current findings would otherwise block it. After it merges, `SCHEMATHESIS_BLOCKING` can be switched to `'true'`; a later, separate PR fixes the findings so that the unit tests and the whole QA suite (fuzzing, complexity, coverage, dependencies) pass with blocking gates.

### Budget and reports

- Schemathesis: phases `examples`, `coverage`, `fuzzing` and `stateful`, `--seed 13`, `--max-examples 30`, `--max-time 90` per API (a manual run can ask for 10 to 600 seconds), one worker, 10 s timeout per request. With `--max-time` it keeps fuzzing until the time is spent, so the number of cases depends on runner speed: the seed makes runs similar, not identical.
- Estimated duration of each job: a few minutes of install and build, plus the Schemathesis budget. The Newman collection itself takes about 3 seconds.
- Artifacts (**7 days** retention): `newman-reports` (`newman-cli.txt`, `newman-junit.xml`) and `schemathesis-<API>` (`schemathesis-cli.txt` with the `curl` commands to reproduce each failure, `schemathesis.json`, `schemathesis-junit.xml`, `html/index.html`). Only the last 500 console lines of each API are uploaded. Databases, Serilog logs (tens of MB after fuzzing) and build output are **never** uploaded.

## Dependency and code hygiene

| Piece | File | What it does | Blocking? |
|-------|------|--------------|-----------|
| Dependabot version updates | `.github/dependabot.yml` | Weekly update PRs (at most 5 open per ecosystem) for `nuget` (`/`, the solution and its projects), `github-actions` (`/`, the workflows), `npm` (`/scripts/qa`, Newman) and `pip` (`/scripts/qa`, Schemathesis and lizard). | No, it opens PRs. |
| Dependency review | `.github/workflows/dependency-review.yml` | On every pull request, `actions/dependency-review-action` (pinned to `v5.0.0`) checks the dependencies the PR adds or changes against the GitHub Advisory Database. | Yes, for vulnerable dependencies **introduced by the PR**. |
| Action majors in `Tests` | `.github/workflows/tests.yml` | `actions/checkout@v7` and `actions/setup-dotnet@v6`, which run on Node 24 and remove the Node 20 deprecation warning. Runner, triggers and test commands are unchanged. | Same as before. |
| Cyclomatic complexity | `.github/workflows/code-quality.yml` (`complexity`) | Runs [lizard](https://github.com/terryyin/lizard) 1.25.1 (`scripts/qa/requirements-quality.txt`) on the C# of the five API projects (`bin/` and `obj/` excluded, tests not included) and writes the job summary with `scripts/qa/complexity-report.py`: functions analyzed, average and max CCN, how many exceed CCN 15 and the 15 most complex functions. The CSV report is uploaded as `complexity-report` (7 days). | No, **advisory**. Only an unusable report (tool error, no functions parsed) fails the job. |

Known vulnerability already on `master`: `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 (`NU1903`, [GHSA-2m69-gcr7-jv3q](https://github.com/advisories/GHSA-2m69-gcr7-jv3q)), a transitive dependency of Products, Users, Cart and their tests. Dependency review does not flag it here because this PR does not change those packages; Dependabot alerts and security updates are where it surfaces. It is not upgraded in this PR.

Newman 6.2.3 pulls in `handlebars` through `postman-runtime` 7.39.1, which asks for exactly 4.7.8. That version is affected by GHSA-2w6w-674q-4c4q, GHSA-8r5x-fm3f-whwj, GHSA-p8wg-vrv2-v86f, GHSA-3mfm-83xf-c92r, GHSA-9cx6-37pm-9jff, GHSA-xhpv-hc6g-r9c6, GHSA-xjpj-3mr7-gcpf, GHSA-2qvq-rjwj-gvw9, GHSA-7rx3-28cr-v5wh, GHSA-xw65-4hp5-5hc7 and GHSA-442j-39wm-28r2, so Dependency review failed on `scripts/qa/package-lock.json`. Some of these were fixed in 4.7.9 and the rest in 4.7.10, the first release that fixes all of them. `scripts/qa/package.json` therefore has an npm `overrides` entry, `"handlebars": "^4.7.10"`. That range is a floor that stays within major 4: it is not a frozen pin. The exact version and its integrity hash are recorded in `package-lock.json`, and `npm ci` installs exactly that. Newman stays at 6.2.3. The check passes because the package is patched; `dependency-review.yml` is not relaxed. Remove the override once a Newman release ships a patched `handlebars` itself.

lizard measures functions and methods. Top-level statements in `Program.cs` are not counted as a function.

Public-repo secret scanning (and push protection) is provided by GitHub itself; no workflow is needed for it.

## Code coverage per API

The `Tests` workflow (`.github/workflows/tests.yml`, `windows-latest`, so every step runs in `pwsh`) keeps its restore and Release build, and the test step now also passes `--collect "XPlat Code Coverage"`. `coverlet.collector` 6.0.4 was **already** referenced by the four test projects (Products, Users, Cart, Notifications); no package reference or C# changed.

1. `dotnet test` writes one `coverage.cobertura.xml` per test project. A failing test still fails this step, as before.
2. ReportGenerator (`dotnet-reportgenerator-globaltool` 5.5.12, installed with `--tool-path`) merges them into an HTML report, a GitHub markdown summary and a merged Cobertura file.
3. `scripts/qa/coverage-summary.py` writes a per-API table (line and branch coverage of each API assembly) into the job summary, followed by the ReportGenerator summary. **Orders.API has no test project**, so it is listed as "no test project, coverage not measured"; no percentage is made up for it. Percentages are truncated to one decimal, like ReportGenerator, so both tables agree.
4. The `coverage-report` artifact (7 days) holds the HTML report (`index.html`) and the raw Cobertura files.

Coverage is **advisory**: there is no threshold and low coverage never fails the job. Only tool errors fail it (ReportGenerator finds no coverage files, an unreadable report, or an API that has a test project but is missing from the report). No step uses `continue-on-error`.

Local run on this branch (`dotnet test --configuration Release --collect "XPlat Code Coverage"` with the .NET 10 SDK on Linux, then ReportGenerator 5.5.12): 218 tests passed (Products 75, Users 55, Cart 82, Notifications 6).

| API | Line coverage | Branch coverage |
|-----|--------------:|----------------:|
| Products.API | 95.9% | 71.2% |
| Users.API | 93.6% | 70.7% |
| Orders.API | not measured (no test project) | n/a |
| Cart.API | 66.8% | 32.5% |
| Notifications.API | 35.3% | 3.1% |

CI publishes the current numbers on every run; the root `README.md` copies this local snapshot.

## SBOM

The `sbom` job of `Code Quality` installs [CycloneDX for .NET](https://github.com/CycloneDX/cyclonedx-dotnet) 6.3.1 as a .NET tool and runs it on `ECommerce13.sln` with `--exclude-test-projects`. It writes a CycloneDX 1.7 JSON SBOM of the NuGet packages (direct and transitive) the five APIs resolve, without changing any project file. The job summary shows the component count, and the SBOM is uploaded as the `sbom-cyclonedx` artifact (`ecommerce13.cdx.json`, 7 days). It fails only if the tool fails or the SBOM has no components; it does not evaluate vulnerabilities (Dependabot and dependency review cover that). The local run produced 37 components.

The "Tecnologías usadas" list in the root `README.md` was checked against the `.csproj` files and this SBOM.

## Workflow and script lint

The `lint` job of `Code Quality` is **strict**: these files are ours and are expected to be clean, so any finding fails the job.

| Linter | Version | How it is installed | What it checks |
|--------|---------|---------------------|----------------|
| [actionlint](https://github.com/rhysd/actionlint) | 1.7.12 | Official release binary, sha256 verified | Every workflow in `.github/workflows` (syntax, expressions, action inputs). It also runs shellcheck on each `run:` block. |
| [shellcheck](https://github.com/koalaman/shellcheck) | 0.11.0 | Official release binary, sha256 verified | `scripts/qa/*.sh` |
| [yamllint](https://github.com/adrienverge/yamllint) | 1.38.0 | `scripts/qa/requirements-quality.txt` (pip, Dependabot keeps it updated) | `.github` and `.yamllint.yml`, with `--strict`. Rules in `.yamllint.yml`: default set, `on:` keys allowed, `[ a, b ]` flow lists allowed, no `---` required, lines up to 160 characters. |

The job only needs the network to download those three tools.

## OpenSSF Scorecard

`.github/workflows/scorecard.yml` follows the official [`ossf/scorecard-action`](https://github.com/ossf/scorecard-action) template for public repos (v2.5.0):

- Triggers: weekly (Mondays 07:30 UTC), push to `master`, and `branch_protection_rule`. There is no `pull_request` trigger: with `publish_results: true` the OpenSSF API only accepts results from the default branch.
- `publish_results: true`: results are published to the OpenSSF API (api.scorecard.dev, needed for a Scorecard badge), uploaded to the Security tab (code scanning) and kept as the `scorecard-sarif` artifact (7 days).
- Permissions: `read-all` at workflow level; only the job gets `security-events: write` and `id-token: write`. The OpenSSF API rejects published results from workflows with top-level `env`/`defaults`, workflow-level write permissions or unapproved actions, so this workflow stays minimal.
- Its actions are pinned by commit SHA with a version comment (Scorecard's Pinned-Dependencies check rewards that; Dependabot updates SHA pins too).
- A low score **does not fail** the job: the action has no score threshold. It only fails if the analysis, the publication or the upload fails. It never runs on pull requests, so it is not a PR check.

Some checks depend on repository settings rather than code, for example Branch-Protection and Code-Review (branch protection rules and required reviews on `master`). They may look weak until those settings are enabled. No score is quoted here; see the Security tab or the Scorecard API after the first run on `master`.

## Running locally

From the repo root (Linux, macOS or WSL; the scripts are bash):

```bash
dotnet build ECommerce13.sln -c Release
npm ci --prefix scripts/qa
python3 -m venv /tmp/ec13-qa-venv && /tmp/ec13-qa-venv/bin/pip install -r scripts/qa/requirements.txt

# Everything (Newman and the five APIs with Schemathesis), each on a fresh stack:
ST=/tmp/ec13-qa-venv/bin/st bash scripts/qa/run-local.sh

# Only some targets:
ST=/tmp/ec13-qa-venv/bin/st bash scripts/qa/run-local.sh newman Orders
```

The `qa-verdict.py`, `complexity-report.py` and `coverage-summary.py` tests do not need the APIs:

```bash
python3 -m unittest discover -s scripts/qa -p "test_*.py" -v
```

Complexity report, as in the Code Quality job:

```bash
/tmp/ec13-qa-venv/bin/pip install -r scripts/qa/requirements-quality.txt
/tmp/ec13-qa-venv/bin/lizard -l csharp --csv -C 15 -i -1 -x "*/bin/*" -x "*/obj/*" \
  src/Products.API src/Users.API src/Orders.API src/Cart.API src/Notifications.API > /tmp/lizard.csv
python3 scripts/qa/complexity-report.py /tmp/lizard.csv --threshold 15
```

Coverage per API, as in the Tests job:

```bash
dotnet test ECommerce13.sln -c Release --collect "XPlat Code Coverage" --results-directory /tmp/ec13-cov
dotnet tool install dotnet-reportgenerator-globaltool --version 5.5.12 --tool-path /tmp/ec13-tools
/tmp/ec13-tools/reportgenerator "-reports:/tmp/ec13-cov/**/coverage.cobertura.xml" "-targetdir:/tmp/ec13-cov/report" "-reporttypes:Html;MarkdownSummaryGithub;Cobertura"
python3 scripts/qa/coverage-summary.py /tmp/ec13-cov/report/Cobertura.xml
```

Reports go to `qa-artifacts/` and Schemathesis leaves caches in `.schemathesis/` and `.hypothesis/` (all three are git-ignored); databases and logs go to a temporary directory that is deleted at the end. If ports 5001..5005 are busy (for example, with the APIs running for the demo), use another range: `QA_PORT_BASE=6100` uses 6101..6105. `ST_MAX_TIME=30` shortens the Schemathesis budget.

> **Schemathesis is destructive.** It creates, updates and deletes products, registers users and locks them with failed logins, and creates orders and notifications. Only run it against a stack with disposable databases, like the one `stack.sh` builds. Never against the demo APIs or databases you want to keep.

## Out of scope

- **Manual Postman collection** (`ECommerce13.manual.postman_collection.json`: CRT-005, ORD-007 and PRD-005). It needs Products or Orders to be stopped mid-run; it is not automated in this PR. It is still run by hand as described in `docs/postman/README.md`. That is why CI does **not** validate the whole Postman collection, only the main one.
- **Documented deviations**, unchanged: ORD-003 and NTF-001 are still not implemented because Users.API has no endpoint to look up a user by id. Neither tool tests or fakes them.
- **Business-rule errors the OpenAPI does not describe.** Schemathesis only knows what the Swagger says. If a business rule breaks but the response is a 2xx with values the schema allows (for example, a quantity sum that overflows an `int`), it does not detect it; that needs xUnit boundary tests.
- **Data chained across APIs.** When fuzzing Orders or Cart there are no real products in Products, so almost everything ends in 404 (ORD-004 / CRT-002) and Schemathesis warns `missing_test_data`. Fuzzing rarely reaches the stock logic. Users, Cart and Notifications have no links between operations that Schemathesis can infer, so their `stateful` phase is skipped.
- The APIs have no authentication; none is invented.

## Failures observed in the first run

First CI run of this workflow (`API QA` run 42515226742 on PR #19): Newman passed (121 requests, 512 assertions, 0 failures), and Schemathesis reported 33 unique findings across the five APIs (Products 9, Users 7, Orders 9, Cart 4, Notifications 4) with no infrastructure errors. The only real server error is the Orders 500 below.

Local smoke run with `run-local.sh` on `master` at `17336cc`, with a budget **reduced to 15 s per API** (`ST_MAX_TIME=15`, seed 13; CI uses 90 s and may find more). Each target on its own fresh stack and databases. **Newman: 121 requests, 512 assertions, 0 failures.** Schemathesis (this is not a green QA run; no run recorded infrastructure errors):

| API | Generated cases | Unique failures | Codes |
|-----|-----------------|-----------------|-------|
| Products | 2,578 | 6 | ST202 ×3, ST201 ×1, ST103 ×2 |
| Users | 3,722 | 7 | ST202 ×4, ST201 ×1, ST103 ×2 |
| Orders | 2,937 | 9 | **ST101 ×1**, ST202 ×4, ST201 ×1, ST103 ×3 |
| Cart | 3,457 | 4 | ST202 ×2, ST103 ×2 |
| Notifications | 2,807 | 4 | ST202 ×2, ST201 ×1, ST103 ×1 |

- **ST101 (500) in Orders:** `POST /api/orders` with `"items": [null, null]` returns 500 ORD-007 instead of 400 ORD-002.
- **ST103, undocumented status code:** 415 with no body on the POST/PUT endpoints of every API, and 400 on `GET /api/orders` with an invalid `usuarioId` (the Swagger does not declare that 400).
- **ST201, invalid request accepted:** unknown body properties are ignored (Products PUT, Users login, Notifications send), and `GET /api/orders?usuarioId=` (empty) returns 200 `[]`.
- **ST202, schema-valid request rejected:** the Swagger does not declare constraints the API does enforce (maximum length, formats, a price outside the `decimal` range, `estado: null`, `items: []`), so Schemathesis considers data valid that the API rejects with 400. These are mostly gaps in the contract documentation, not logic failures.

Each failure includes the `curl` command to reproduce it in `schemathesis-cli.txt`. Fixes (code or Swagger) go in separate PRs, each with its own xUnit regression test.

API response texts (`errorMessage`, `detail`) are part of the API contract and stay in Spanish; this document does not translate them.
