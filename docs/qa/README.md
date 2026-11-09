# Automated API QA (Newman + Schemathesis)

Added as a learning change: it tests the five APIs **actually running** (Development, Swagger enabled) with two tools that complement the xUnit tests in `dotnet test`; they do not replace them. xUnit is still what checks specific business rules; these tests look at each API from the outside, over HTTP.

| Tool | Pinned version | What it does | In CI |
|------|----------------|--------------|-------|
| [Newman](https://github.com/postmanlabs/newman) | 6.2.3 (`scripts/qa/package.json` + `package-lock.json`) | Runs the main Postman collection (`docs/postman/ECommerce13.postman_collection.json`): the demo flows across services, with all their assertions. | **Mandatory**: any failed assertion fails the job. |
| [Schemathesis](https://schemathesis.readthedocs.io/) | 4.33.1 (`scripts/qa/requirements.txt`) | Reads each API's OpenAPI (`/swagger/v1/swagger.json`) and generates valid and invalid requests (nulls, numeric extremes, odd strings, extra or missing fields), looking for 500s, undocumented status codes and responses that break the contract. | **Mandatory**: any API finding fails the job (see below). A manual run can uncheck `schemathesis_blocking` to keep findings advisory. |

## Why both xUnit and Newman (Postman)

They are not two copies of the same checks: they test at different altitudes and they fail for different reasons.

- **xUnit** (`tests/*.API.Tests`): in-process tests of services and controllers with fakes for the HTTP clients (`IProductsClient`, `IProductCatalog`). Deterministic and fast; they check the decision logic the wire rarely exercises like state transitions, `int`/`decimal` boundaries, guard order before persistence. Coverage is measured only here.
- **Newman** (the main Postman collection): real HTTP against the five running APIs with real SQLite, chaining data across services. It checks what the wire adds (status codes, `ErrorResponse` bodies, correlation headers) and doubles as the live demonstration that the APIs work: Swagger *describes* the endpoints, the collection *proves* them.

A larger project would not collapse these into one; it would keep both at the same altitudes and add layers around them (contract tests, load tests, UI E2E). The design goal here is that each assertion in the collection fails for a reason no in-process test can produce, and vice versa.

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

### Schemathesis is mandatory

The first run already finds real failures (see below). This PR turns blocking on **on purpose (TDD)**: API findings fail the job so those checks go red before the APIs are fixed. A later, separate PR fixes the findings so later PRs can merge only when every check is green.

- The `Schemathesis` step never fails on its own and does not use `continue-on-error`: `run-schemathesis.sh` records Schemathesis' exit code in `exit-code` and the step logs it. That way findings do not show up as a failed step or as a `Process completed with exit code 1` annotation (the first CI run had one per API).
- The `Schemathesis verdict` step (`qa-verdict.py`) reads the exit code (`exit-code`) and `schemathesis.json`:
  - `0` with at least one tested operation, no failures and no errors: all checks passed.
  - `1` with at least one tested operation, at least one recorded failure and no errors: API failures. With blocking on (the default on pull requests, and on a manual run unless `schemathesis_blocking` is unchecked) it adds an **error** visible on the PR with the number of unique failures, writes the table into the job summary, and **fails the job**. If a manual run unchecks `schemathesis_blocking` (or `SCHEMATHESIS_BLOCKING` is not `true`), that same case stays **advisory**: a warning, and the job does not fail.
- Everything else is an **infrastructure or run error** and fails the job: an `exit-code` that is missing or not an integer, `2` or any other code, a report that is missing, not JSON or not in the expected shape, a report `exit_code` that differs from the real one, zero tested operations or zero generated cases, recorded errors (`errors`, `operations.errored` or `test_cases.errored`, **even when there are also failures**), `1` with no failures in the report, or `0` with failures.
- If any API stopped answering `/health/ready` after fuzzing, the job fails.
- Installing tools, building and starting the stack are normal steps: if they fail, the job fails.

There is no baseline and nothing is excluded: no operations, checks, nulls or limits are filtered. To treat findings as advisory again on a one-off run, uncheck `schemathesis_blocking` when starting the workflow from Actions.

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

Newman 6.2.3 pulls in `handlebars` through `postman-runtime` 7.39.1, which asks for exactly 4.7.8. That version is affected by GHSA-2w6w-674q-4c4q, GHSA-8r5x-fm3f-whwj, GHSA-p8wg-vrv2-v86f, GHSA-3mfm-83xf-c92r, GHSA-9cx6-37pm-9jff, GHSA-xhpv-hc6g-r9c6, GHSA-xjpj-3mr7-gcpf, GHSA-2qvq-rjwj-gvw9, GHSA-7rx3-28cr-v5wh, GHSA-xw65-4hp5-5hc7 and GHSA-442j-39wm-28r2, so Dependency review failed on `scripts/qa/package-lock.json`. Some of these were fixed in 4.7.9 and the rest in 4.7.10, the first release that fixes all of them. `scripts/qa/package.json` therefore has an npm `overrides` entry, `"handlebars": "^4.7.10"`. That range is a floor that stays within major 4: it is not a frozen pin. The exact version and its integrity hash are recorded in `package-lock.json`, and `npm ci` installs exactly that. Newman stays at 6.2.3. The check then passed for handlebars because the package is patched; that override does not relax `dependency-review.yml`. Remove the override once a Newman release ships a patched `handlebars` itself.

Newman 6.2.3 also pulls `@faker-js/faker` 5.5.3 through `postman-collection` 4.4.0. That version is affected by [GHSA-qxc2-j82w-r537](https://github.com/advisories/GHSA-qxc2-j82w-r537) (an RCE in `helpers.fake`, patched only from `@faker-js/faker` 10.5.0 onward). Unlike handlebars, there is no compatible patched release: forcing `"@faker-js/faker": "^10.5.0"` resolves 10.6.0 and Newman then crashes in `postman-collection`'s `lib/superstring/dynamic-variables.js` (`faker.address.city`). Faker 8+ renamed those APIs (`address` became `location`); there is no 5.x patch.

We looked upstream before allowing the advisory. [postman-collection#1390](https://github.com/postmanlabs/postman-collection/issues/1390) (opened 2024-12-05) reports that `@faker-js/faker@5.5.3` is deprecated; a later comment notes that security gates now require a fix for this GHSA and that forcing faker v10 breaks the same `faker.address.city` call. [PR #1393](https://github.com/postmanlabs/postman-collection/pull/1393) (opened 2025-01-11) bumps faker to 9.3.0 and migrates the APIs, but it is still unmerged, does not apply cleanly on the current default branch, has no Postman maintainer review, and 9.3.0 itself stays in the affected range (the advisory covers every release through 10.4.0). Later comments ask to retarget `^10.5.0`. The latest `postman-collection` on npm, 5.3.1 (published 2026-07-21), still depends on `@faker-js/faker` 5.5.3, despite about 2.3 million weekly downloads. 

`postman-collection` never calls `helpers.fake`, so the documented RCE path is not exercised by Newman running this collection.

After allowing that one advisory, Dependency Review failed again, this time on `flatted@3.2.6`. The job log listed only that package, but GitHub's dependency-graph compare API (the same source the action uses) currently reports eleven vulnerable packages introduced by `scripts/qa/package-lock.json`. To stop fixing them one CI failure at a time, this branch now overrides every package in that list that has a compatible patched release in the same major, and allows only the advisories that cannot be patched without breaking Newman:

- Overrides in `scripts/qa/package.json` (each a major-bounded floor; `package-lock.json` records the exact resolved version and its integrity hash): `handlebars` `^4.7.10`, `flatted` `^3.4.2` (fixes [GHSA-25h7-pfq9-p65f](https://github.com/advisories/GHSA-25h7-pfq9-p65f) and [GHSA-rf6f-7fwh-wjgh](https://github.com/advisories/GHSA-rf6f-7fwh-wjgh)), `lodash` `^4.18.0` ([GHSA-r5fr-rjxr-66jc](https://github.com/advisories/GHSA-r5fr-rjxr-66jc) and [GHSA-xxjr-mmjv-4gpg](https://github.com/advisories/GHSA-xxjr-mmjv-4gpg)), `node-forge` `^1.4.1` (the Forge advisories reported for 1.3.1), `underscore` `^1.13.8` ([GHSA-qpx9-hpmf-5gmw](https://github.com/advisories/GHSA-qpx9-hpmf-5gmw)), `jose` `^4.15.5` ([GHSA-hhhv-q57g-882q](https://github.com/advisories/GHSA-hhhv-q57g-882q)), and `qs` `^6.16.1` (the `qs` advisories reported for 6.5.5 and 6.14.3).
- Allowed in `.github/workflows/dependency-review.yml` (`allow-ghsas`): [GHSA-qxc2-j82w-r537](https://github.com/advisories/GHSA-qxc2-j82w-r537) (`@faker-js/faker`, incompatible 10.x API), [GHSA-8cw4-87c7-c6xx](https://github.com/advisories/GHSA-8cw4-87c7-c6xx) (`csv-parse` 4.16.3; the fix is 7.0.2, a new major), and [GHSA-w5hq-g745-h8pq](https://github.com/advisories/GHSA-w5hq-g745-h8pq) (`uuid` 3.4.0 and 8.3.2; the fix is 11.1.1, a new major).

Newman stays at 6.2.3. Remove an override or an allowance once Newman ships that package already patched.

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
- **Business-rule errors the OpenAPI does not describe.** Schemathesis only knows what the Swagger says. If a business rule breaks but the response is a 2xx with values the schema allows (for example, a quantity sum that overflows an `int`), it does not detect it; that needs xUnit boundary tests. Quantity accumulation in Orders and Cart, line amount multiplication, and order-total overflow are now covered: Cart by `QuantityOverflowTests.cs`, Orders by Newman requests pinning the same `int`/`decimal` limits. Unrepresentable values answer 400 ORD-002 / CRT-004 without writes; the exact limits stay accepted.
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

- **ST101 (500) in Orders (fixed):** the initial run returned 500 ORD-007 for `POST /api/orders` with `"items": [null, null]`. Null entries now answer 400 ORD-002 in `CreateAsync`, before the Products call or persistence; the Newman collection covers the regression (`Crear orden con ítem nulo → 400 ORD-002`).
- **ST103, undocumented status code:** the framework's 415 on the POST/PUT endpoints of every API (the body already follows the error contract), and 400 on `GET /api/orders` with an invalid `usuarioId`. Both are decided tolerations, see below.
- **ST201, invalid request accepted:** unknown body properties are ignored (Products PUT, Users login, Notifications send), and `GET /api/orders?usuarioId=` (empty) returns 200 `[]`.
- **ST202, schema-valid request rejected:** the Swagger does not declare constraints the API does enforce (maximum length, formats, a price outside the `decimal` range, `estado: null`, `items: []`), so Schemathesis considers data valid that the API rejects with 400. These are mostly gaps in the contract documentation, not logic failures.

Each failure includes the `curl` command to reproduce it in `schemathesis-cli.txt`. Fixes (code or Swagger) go in separate PRs, each with its own regression test (xUnit or Newman).

## Tolerated findings (found and decided, not ignored)

A finding is silenced only by a written decision. The mechanism is the
`SCHEMA_TOLERATE` variable of the QA verdict step (`scripts/qa/qa-verdict.py`,
one decision per line, `METHOD /path/{template} status` or `* status`): a
Schemathesis run turns green only when **every** failure group maps to junit
cases whose every status code matches a listed decision; anything else keeps
the run red. The decision lives at the gate - the tool's own reports are never
rewritten, so `schemathesis.json` and the junit always show exactly what
Schemathesis saw, and the tolerated groups are listed in the verdict output
and in the job summary. If the tolerated behavior changes, the run goes red
again by itself.

### The framework answers 415 and Swagger does not declare it

- **How it was found:** Schemathesis (ST103, plus ST202 on the same cases) on
  the 2026-11-09 QA pass, on the nine POST/PUT endpoints of the five APIs, by
  sending a `Content-Type` other than `application/json`.
- **What actually happens:** the API answers 415 with a Spanish `ErrorResponse`
  (`"El body debe enviarse como application/json."`) including `correlationId`.
  The body follows section 3 of the TP since the status-code writer fix; only
  the Swagger declaration is absent.
- **Why it is not declared:** no "HTTP Status posibles" table of the TP lists
  415 for any endpoint. Declaring it means nine attributes plus nine examples
  spread over five controllers and five example providers which are not part
  of the TP doc.
- **Tolerance:** `* 415` (covers the stateful-test variants too).
- **Revisit if:** the TP adds 415 to a table, the body stops following the
  error contract, or a framework change moves who emits the 415.

### The APIs silently ignore unknown properties (ST201)

- **How it was found:** Schemathesis (ST201) on the 2026-11-09 QA pass, on
  `POST`/`PUT /api/Products`, `POST /api/Users/register`, `POST /api/Users/login`,
  `POST /api/notifications/send` and `GET /api/orders`, by sending body members
  and query parameters the schemas never mention.
- **What actually happens:** the deserializer (System.Text.Json default) and the
  query binder drop unknown members and the request completes as if they were
  absent: `201`/`200`, contract-shaped bodies. Nothing validates them and
  nothing complains.
- **Why it is not fixed:** rejecting them means
  `[JsonUnmappedMemberHandling(Disallow)]` on every request DTO plus a Spanish
  translation for a `JsonException` that never reaches `ModelState`, a per-DTO
  error-message surface the TP has no text for (no error code covers "unknown
  property"), and a forward-compatibility contract between our own services: a
  field one teammate ships before the peer updates turns working calls into
  400s. Lenient-unknown is the framework default and the mainstream HTTP
  behavior; the TP never mentions the case.
- **Tolerance:** the success statuses of exactly those endpoints
  (`GET /api/orders 200`, `POST /api/Users/register 201`,
  `POST /api/Users/login 200`, `POST /api/Products 201`,
  `PUT /api/Products/{id} 200`, `POST /api/notifications/send 201`).
  Accepted trade-off: the gate grammar matches status codes, so a future
  response-shape finding on those exact endpoints would be silenced too. That
  is the price of a gate that stays useful instead of permanently red.
- **Revisit if:** the TP specifies unknown-property handling, the course
  prefers strict DTOs, or a response-shape finding on one of these six
  endpoints needs to stay visible (then this tolerance narrows).

API response texts (`errorMessage`, `detail`) are part of the API contract and stay in Spanish; this document does not translate them.
