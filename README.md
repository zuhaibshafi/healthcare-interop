# Healthcare Interoperability Technical Assessment — local working stack

End-to-end, Docker-free local setup of **HAPI FHIR R4**, **eCRNow**, **UDS+** (submitter pipeline +
Inferno UDS+ test kit) and **Inferno** (US Core test kit), plus a **.NET integration layer**, with
FHIR API demonstrations, eCR integration, conformance results and documentation.

```
C:\git
├── interop-assessment\            <- this folder: scripts, docs, sample data, .NET PoC, results
│   ├── bootstrap\setup.ps1       one-shot installer for a new machine (tools, clones, patches, builds)
│   ├── bootstrap\patches, templates, kars   local patches, config templates, patched eRSD knowledge artifact
│   ├── scripts\start-all.ps1      start every service (idempotent)      scripts\stop-all.ps1
│   ├── scripts\make-zip.py        rebuild the distributable zip
│   ├── scripts\ecrnow-demo.py     eCRNow <-> HAPI integration driver
│   ├── sample-data\transaction-bundle.json   19 US Core resources (patient Amy Shaw, COVID-19 encounter)
│   ├── dotnet\FhirIntegration.Api  .NET 9 gateway / OAuth facade / PHA mock / UDS+ submitter
│   ├── docs\                      architecture.md, architecture-diagram.svg, ecr-workflow.md,
│   │                              uds-plus-technical-understanding.md, inferno-results.md, troubleshooting.md
│   └── results\                   captured API responses, Inferno results, eICR, UDS+ run output
├── hapi-fhir-jpaserver-starter\   cloned, built -> target\ROOT.war        hapi-runtime\ (config + H2 db)
├── eCRNow\                        cloned, built -> target\ecr-now.war     ecrnow-runtime\ (config, KARs, output)
├── us-core-test-kit\              Inferno US Core kit (Ruby)              inferno-runtime\ (logs)
├── uds-plus-test-kit\             Inferno UDS+ kit (Ruby)
├── uds-plus\                      HRSA UDS+ IG source (reference only)
└── tools\                         portable JDK 21, Maven, PostgreSQL 16, Redis 8, Ruby 3.3, validator jar
```


## 0. Quick start on a new machine (from the zip)

1. Get the package into `C:\git\interop-assessment` (any parent folder works; the scripts derive the root
   from their own location), either by extracting the zip or by cloning:
   ```powershell
   git clone https://github.com/zuhaibshafi/healthcare-interop.git C:\git\interop-assessment
   ```
2. Open PowerShell (no admin needed) and run the bootstrap. It downloads the portable toolchain
   (JDK 21, Maven, PostgreSQL 16, Redis 8, Ruby 3.3 + MSYS2, HL7 validator), clones the four
   repositories at the exact commits used here, applies the local patches, builds HAPI, eCRNow and
   the .NET layer, installs the Inferno gems and initialises the databases. Allow 30–60 minutes and
   ~4 GB of disk; Internet access is required. Only git, .NET SDK 9 and Python 3 must pre-exist
   (the script installs them with winget if missing).
   ```powershell
   powershell -ExecutionPolicy Bypass -File C:\git\interop-assessmentootstrap\setup.ps1
   ```
3. Start the stack, load the sample data and run the demos:
   ```powershell
   powershell -ExecutionPolicy Bypass -File C:\git\interop-assessment\scripts\start-all.ps1
   curl -X POST -H "Content-Type: application/fhir+json" --data-binary "@C:\git\interop-assessment\sample-data	ransaction-bundle.json" http://localhost:8080/fhir
   curl -X POST -H "Content-Type: application/fhir+json" --data-binary "@C:\git\interop-assessment\sample-data	ransaction-bundle-2.json" http://localhost:8080/fhir
   python C:\git\interop-assessment\scriptshir-demo.py
   python C:\git\interop-assessment\scripts\ecrnow-demo.py
   ```
   Inferno runs: see `docs/inferno-results.md` section 3. The first validation of a run downloads
   IG packages into `%USERPROFILE%\.fhir\packages` and can take 10+ minutes.

## 1. Prerequisites (installed by `bootstrap/setup.ps1`, no admin needed)

| Tool | Location | Notes |
| --- | --- | --- |
| JDK 21 (Temurin) | `C:\git\tools\jdk-21.0.13+9` | system JDK 25 is too new for eCRNow's Lombok |
| Maven 3.9.9 | `C:\git\tools\apache-maven-3.9.9` | |
| PostgreSQL 16.9 | `C:\git\tools\pgsql`, data `C:\git\tools\pgdata` | user `postgres` / `postgres`, DBs `ecrnow`, `inferno` |
| Redis 8.10 (Windows build) | `C:\git\tools\redis8\Redis-8.10.3-Windows-x64-msys2` | Sidekiq needs Redis ≥ 6.2 |
| Ruby 3.3.13 + MSYS2 | `C:\git\tools\Ruby33-x64` | installed per-user via RubyInstaller |
| HL7 validator wrapper 1.0.85 | `C:\git\tools\validator\validator-wrapper.jar` | port 8082 |
| .NET SDK 9/10 | system | |
| Env helpers | `C:\git\tools\env.ps1`, `env.sh`, `ruby-env.sh` | put the portable tools on PATH |

## 2. Start / stop everything

```powershell
powershell -ExecutionPolicy Bypass -File C:\git\interop-assessment\scripts\start-all.ps1
```
First start takes 3–5 minutes (HAPI ≈ 70 s, eCRNow ≈ 30 s, Inferno kits ≈ 2 min each, validator loads
IG packages lazily on first use). Logs go to `C:\git\interop-assessment\logs\`.

```powershell
powershell -ExecutionPolicy Bypass -File C:\git\interop-assessment\scripts\stop-all.ps1
```

| Service | URL |
| --- | --- |
| HAPI FHIR R4 | http://localhost:8080/fhir  (tester UI http://localhost:8080/ , OpenAPI http://localhost:8080/fhir/swagger-ui/index.html) |
| eCRNow | http://localhost:8081/swagger-ui/index.html |
| HL7 validator wrapper | http://localhost:8082/ |
| Inferno US Core | http://localhost:4567/ |
| Inferno UDS+ | http://localhost:4568/ |
| .NET integration layer | http://localhost:5080/swagger |

## 3. Demonstrations

### 3.1 FHIR operations on HAPI
```bash
# load the US Core sample data (transaction bundle, 19 resources)
curl -X POST -H "Content-Type: application/fhir+json" --data-binary @sample-data/transaction-bundle.json http://localhost:8080/fhir
curl -X POST -H "Content-Type: application/fhir+json" --data-binary @sample-data/transaction-bundle-2.json http://localhost:8080/fhir
python scripts/fhir-demo.py        # 22 scripted CRUD/search/$everything/$validate/bulk-export calls -> results/fhir-demo/
curl http://localhost:8080/fhir/Patient/demo-patient-1
curl "http://localhost:8080/fhir/Encounter?patient=demo-patient-1"
curl "http://localhost:8080/fhir/Condition?patient=demo-patient-1&code=http://snomed.info/sct|840539006"
curl "http://localhost:8080/fhir/Patient/demo-patient-1/\$everything"
```
Captured responses: `results/01-transaction-response.json` and the `.NET` smoke tests under
`results/dotnet-smoke/`.

### 3.2 eCRNow ↔ HAPI (electronic case reporting)
```bash
python scripts/ecrnow-demo.py            # register HAPI, activate eRSD, launchPatient, wait for eICR
```
The script output and the eICR artefacts are in `results/` and `C:\git\ecrnow-runtime\bsa-output\`.
Background: `docs/ecr-workflow.md`.

### 3.3 .NET integration layer
```powershell
dotnet run --project C:\git\interop-assessment\dotnet\FhirIntegration.Api
```
See `dotnet/README.md` for the endpoint catalogue (patients, encounters, `$everything`, transaction,
OAuth token facade, PHA receiver, UDS+ submit).

### 3.4 UDS+ submission pipeline
```bash
curl -X POST "http://localhost:5080/api/udsplus/submit?groupId=uds-plus-reporting-group&validate=true"
```
Produces de-identified NDJSON + Import Manifest under `results/dotnet-output/udsplus/<run>/`, validates
with the UDS+ IG package and submits to the mock `$import` receiver. The manifest URL can be pasted
into the Inferno UDS+ kit (http://localhost:4568) as `import_manifest`. Background:
`docs/uds-plus-technical-understanding.md`.

### 3.5 Inferno conformance tests
US Core Server v6.1.0, "US Core FHIR API" group, url `http://localhost:8080/fhir`,
patient_ids `demo-patient-1,demo2-patient` (no SMART auth). UDS+ kit with the generated manifest. Results and
how to re-run: `docs/inferno-results.md`.

## 4. Documentation index

- `docs/architecture.md` + `docs/architecture-diagram.svg` — solution architecture
- `docs/ecr-workflow.md` — eCR workflow, eCRNow internals, integration issues solved
- `docs/uds-plus-technical-understanding.md` — UDS+ IG actors, profiles, submission flow
- `docs/inferno-results.md` — Inferno execution details and results
- `docs/troubleshooting.md` — every integration issue hit during setup and its fix
- `dotnet/README.md` — .NET proof of concept

## 5. Outcome summary

| Expected activity / deliverable | Status | Where |
| --- | --- | --- |
| Review architecture, source and dependencies | done | `docs/architecture.md`, `docs/ecr-workflow.md` |
| Configure and start HAPI FHIR R4 | running on :8080 (H2 file DB, US Core 6.1.0 package installed, Bulk Data enabled) | `hapi-runtime/application-local.yaml` |
| Configure and start eCRNow | running on :8081 (PostgreSQL, eRSD KAR loaded) | `ecrnow-runtime/application-local.properties` |
| eCRNow ↔ HAPI connectivity | HealthcareSetting registered, eRSD activated, `launchPatient` → trigger match → eICR → `$process-message` to the PHA mock, Reportability-Response-style ack received | `results/ecr/`, `scripts/ecrnow-demo.py` |
| Create / retrieve Patient, Encounter, clinical resources | 2 synthetic patients, 86 resources, CRUD + search + `$everything` + `$validate` demonstrated | `sample-data/`, `results/fhir-demo/` |
| Understand the eCR workflow | documented, incl. the eRSD PlanDefinition state machine and 14 integration issues solved | `docs/ecr-workflow.md`, `docs/troubleshooting.md` |
| UDS+ requirements & IGs | documented; Data Source / Submitter / Trust Service Provider / Receiver actors implemented or mocked | `docs/uds-plus-technical-understanding.md`, `.NET /api/udsplus/submit` |
| Inferno conformance tests | US Core v6.1.0 FHIR API: **358 pass / 2 fail (TLS, `instantiates`) / 93 skip / 0 error**; UDS+ kit: **12 pass / 1 fail (offline VSAC value-set expansion) / 10 omit** on the final .NET submission | `docs/inferno-results.md`, `results/inferno-*` |
| Integration issues & troubleshooting | 30+ issues logged with root cause and fix | `docs/troubleshooting.md` |
| .NET integration layer | ASP.NET Core 9 + Firely SDK: FHIR gateway, OAuth facade, PHA receiver, UDS+ submitter/receiver | `dotnet/`, `results/dotnet-smoke/` |
| Solution architecture diagram | Mermaid + SVG | `docs/architecture.md`, `docs/architecture-diagram.svg` |
| Setup instructions & technical documentation | this README + `docs/` | |
