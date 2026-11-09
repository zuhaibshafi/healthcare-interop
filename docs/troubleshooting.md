# Integration issues found and how they were resolved

Everything below was hit while bringing the stack up on a locked-down Windows 11 laptop
(no Docker, no WSL, no local administrator). Each entry records the symptom, the root cause, and the
fix that is now baked into the scripts/config in this folder.

## Environment

| # | Symptom | Root cause | Fix |
| --- | --- | --- | --- |
| E1 | `docker` / `wsl` not available; `winget install Docker.DockerDesktop` would require elevation and a reboot | Corporate workstation policy | Portable toolchain under `C:\git\tools` (JDK 21 zip, Maven zip, PostgreSQL EDB binaries zip, Redis Windows zip, RubyInstaller `/CURRENTUSER`); every service is a plain process started by `scripts\start-all.ps1` |
| E2 | System Java is 25; several builds assume 17/21 | eCRNow's Lombok 1.18.22 fails on javac ≥ 21 internals; Spring Boot 3.3 is only certified up to 21 | Downloaded Temurin JDK 21 and point `JAVA_HOME` at it in `env.ps1` / `env.sh` |
| E3 | `pg_ctl start` hangs the shell when launched from Git Bash | postgres inherits the console handles | Start via `Start-Process`/`nohup … &`; data dir initialised with `initdb -U postgres --pwfile` |

## HAPI FHIR JPA starter

| # | Symptom | Root cause | Fix |
| --- | --- | --- | --- |
| H1 | Startup fails: `Feature not supported: "AUTO_SERVER=TRUE && DB_CLOSE_ON_EXIT=FALSE"` | H2 2.x refuses that option combination | JDBC URL `jdbc:h2:file:C:/git/hapi-runtime/h2/hapi;DB_CLOSE_ON_EXIT=FALSE` (persistent file DB without AUTO_SERVER) |
| H2 | Default config is in-memory H2 — data lost on restart | starter default `jdbc:h2:mem:test_mem` | `hapi-runtime\application-local.yaml` overrides datasource, enables bulk export/import, rest-hook subscriptions, `client_id_strategy: ANY`, OpenAPI |
| H3 | Maven `clean` fails while HAPI is running (`Failed to delete target\ROOT.war`) | Windows file lock on the running WAR | Stop HAPI before rebuilding (`stop-all.ps1`) |
| H4 | First start takes ~70 s and the readiness probe returns 000 | Hibernate schema creation + 150 resource definitions | `start-all.ps1` waits on `/fhir/metadata` for up to 240 s |

## eCRNow

| # | Symptom | Root cause | Fix |
| --- | --- | --- | --- |
| C1 | `mvn package`: `NoSuchFieldError: Class com.sun.tools.javac.tree.JCTree$JCImport does not have member field 'qualid'` | Lombok 1.18.22 incompatible with JDK 21 | `pom.xml` Lombok → 1.18.36 (one line) |
| C2 | Build runs the full unit-test suite (WireMock etc.) despite `-DskipTests` | pom binds surefire `skipTests` to `${skip.unit.tests}` | `-Dmaven.test.skip=true` (also skips test compilation) |
| C3 | Spotless `check` fails the build | formatting gate in `verify` | `-Dspotless.check.skip=true` for local builds |
| C4 | `POST /api/healthcareSettings` → HTTP 500 NPE | Controller dereferences the *null* existing setting (`hsd`) when `tokenUrl` is missing while trying to read `CapabilityStatement.rest.security` | Always send `tokenUrl` (we point it at the .NET `/oauth/token` facade) |
| C5 | eCRNow requests an OAuth token before every EHR query; HAPI has no auth server | `authType` drives `AuthorizationUtils.getToken()` | `authType: "None"` → falls to the `UNKNOWN` branch which returns an empty token (no `Authorization` header), or `authType: "System"` against the .NET client-credentials facade |
| C6 | Trigger code **matches** (`Found Match for CodeSystem http://snomed.info/sct and Code 840539006 in ValueSet dxtc`) but the workflow stops with `Null Value returned from FHIR Path Expression Evaluator : So condition not met` | The eRSD bundle shipped in `documents/app-artifacts/kars/Bundle-bundle-ersd-specification-bundle.json` has a typo in the applicability expression of action `ckr-is-encounter-reportable`: `$procedureOrders.exists()` instead of `%procedureOrders.exists()`; FHIRPath returns null for the unknown `$` variable | Patched the runtime copy in `C:\git\ecrnow-runtime\kars\ersd\` (`%procedureOrders`). Upstream bug worth reporting |
| C7 | `POST /api/loadKars` + `reLaunchPatient` after fixing the KAR executed **zero** trigger actions | In-memory KAR cache is rebuilt by `loadKars`, but the healthcare-setting ↔ KAR action map is not refreshed for existing notification contexts | Restart eCRNow after changing knowledge artifacts (KARs load at startup), then `launchPatient` |
| C8 | `GET /api/eicr` → 400 `Required request header 'X-Request-ID'` | eCRNow correlates every call with `X-Request-ID` | Demo script sends the header on every request |
| C9 | Release-2 property `ersd.file.location` must still exist | Legacy loader is always initialised | Points to a non-existent dummy file as the README instructs |
| C11 | `create-report` produces the eICR but `submit-report` logs `No resources to submit` | In the eRSD bundle the `validate-eicr` / `submit-eicr` input DataRequirements have no `us-ph-relateddata-extension`, so eCRNow cannot link `eicr-report` → `validated-eicr-report` between actions (`KarProcessingData.getDataForId` only looks up outputs through that mapping) | Added `http://hl7.org/fhir/us/ecr/StructureDefinition/us-ph-relateddata-extension` (valueString = the input id) to both inputs in the runtime KAR copy |
| C12 | Submission fails with `HTTP 404 Not Found` from the PHA endpoint | eCRNow builds a HAPI client on `report-submission.endpoint` and calls `$process-message` on it; the sample value already ended in `/$process-message`, producing `/$process-message/$process-message` | `report-submission.endpoint=http://localhost:5080/pha/r4/fhir` (base URL). The PHA record is still looked up by the full `.../$process-message` URL |
| C13 | `PHA is NULL` / `Continuing without auth token` warnings | No `PublicHealthAuthority` registered for the submission URL | `POST /api/publicHealthAuthority` with `fhirServerBaseURL` = `http://localhost:5080/pha/r4/fhir/$process-message`, `authType: None` (demo script does this) |
| C14 | Second `launchPatient` for the same encounter: `duplicate key value violates unique constraint` on `notification_context_v2`, job silently not scheduled | eCRNow keeps one notification context per server/patient/encounter/trigger; `reLaunchPatient` does not re-run the trigger actions after a KAR reload | Delete the context (`DELETE /api/notificationContext` with JSON body `{id, fhirServerBaseUrl, patientId, notificationResourceId}`) or the row in `notification_context_v2`, then `launchPatient` again |
| C10 | eRSD timers: nothing happens for 3 minutes after launch | PlanDefinition `relatedAction.offsetDuration` schedules `check-reportable` via db-scheduler | Expected behaviour; `GET /api/scheduledTasks` shows pending jobs, `ignore.timers=true` can be set for demos |

## Inferno (native Windows, no Docker)

| # | Symptom | Root cause | Fix |
| --- | --- | --- | --- |
| I1 | `bundle exec inferno migrate` → `cannot load such file -- ffi` | `rest-client` requires the `ffi` gem only on Windows; the Linux-generated `Gemfile.lock` omits it | Added `gem 'ffi', platforms: [:windows]` to both kits' Gemfiles + `bundle install` |
| I2 | UDS+ kit `bundle install` fails resolving `io-console 0.7.2` (needs native build) | pinned transitive dependency of `debug`/`irb` | `bundle lock --update io-console reline irb debug` to newer precompiled versions |
| I3 | Puma/Sidekiq: `redis-client requires Redis 6+ with HELLO command available` | tporadowski Redis 5.0.14 (the usual Windows build) is too old for Sidekiq 7 | Switched to `redis-windows/redis-windows` 8.10.3 (msys2 build) |
| I4 | Default `FHIR_RESOURCE_VALIDATOR_URL=http://localhost/hl7validatorapi` expects the nginx+validator docker services | docker-compose only setup | Run `validator-wrapper.jar` standalone (port 8082) and export `FHIR_RESOURCE_VALIDATOR_URL=http://localhost:8082` |
| I5 | Two Inferno kits share one Redis → workers steal each other's jobs | same Sidekiq queue names | US Core kit uses `redis://127.0.0.1:6379/0`, UDS+ kit `/1`, different `INFERNO_PORT` |
| I6 | First validation takes 10+ minutes | validator downloads `hl7.fhir.us.core#6.1.0`, `hl7.terminology`, `us.nlm.vsac`, UDS+ packages into `%USERPROFILE%\.fhir\packages` | One-time cost; `start-all.ps1` keeps the validator running so the cache is warm |
| I7 | Exported `FHIR_RESOURCE_VALIDATOR_URL` / `REDIS_URL` ignored; worker still dials `localhost:80` | The kits load `.env.development`, which overrides process env | Edited `.env.development` in both kits (validator `http://localhost:8082`, Redis db 0 / 1, `INFERNO_HOST`, `INFERNO_PORT`) |
| I8 | Validator overwhelmed at start-up (eight parallel US Core/SMART sessions, each loading hundreds of MB of IG packages); first Inferno run reports `Unable to connect to validator` | Inferno pre-creates a validator session per suite at boot | `INITIALIZE_VALIDATOR_SESSIONS=false` in `.env.development` so sessions are created lazily for the suite actually run; validator started with `-Xmx8g` |
| I9 | US Core search tests return HTTP 400 (`CareTeam?role`, `Encounter?discharge-disposition`) and capability tests fail (`US Core Patient profile not supported`, missing `instantiates`) | Plain HAPI does not know the US Core search parameters/profiles | Installed `hl7.fhir.us.core#6.1.0` into HAPI via `hapi.fhir.implementationguides` (STORE_AND_INSTALL) in `application-local.yaml`; HAPI then advertises `supportedProfile` and accepts the US Core search parameters (verified `CareTeam?role=` and `Condition?asserted-date=` return 200). Remaining known gap: HAPI's generated CapabilityStatement does not list `instantiates: us-core-server`, so that single Inferno test still fails (requires a custom CapabilityStatement interceptor) |
| I10 | Second US Core run: 79 tests `error` with `Unable to connect to validator at http://localhost:8082` although the validator answers; detail message `Invalid argument @ rb_sysopen - '{"resourceType":"Parameters"...` | `.env.development` sets `FHIR_RESOURCE_VALIDATOR_EXPANSION_PARAMETERS` to single-quoted inline JSON; on Windows the quotes survive dotenv parsing, so Inferno treats the value as a *file path* and `File.read` fails | Moved the JSON to `config/validator_expansion_parameters.json` and pointed the variable at that path |
| I11 | "No resources using the DataAbsentReason CodeSystem have been found" although an Observation carries `dataAbsentReason` | Inferno only counts the code `unknown` from `http://terminology.hl7.org/CodeSystem/data-absent-reason` (US Core missing-data guidance) | Sample Observation now uses `unknown` instead of `not-performed` |
| I12 | Patient validation error on the US Core / UDS+ `detailed` race & ethnicity coding | The offline validator cannot expand the VSAC detailed race/ethnicity value sets (`Cannot process resource ... exclude[0].filter`); no terminology server configured | Dropped the optional `detailed` sub-extension in the US Core sample; for UDS+ (where it is required) documented as a validator limitation — configure `tx.fhir.org` on the validator wrapper to clear it |
| I13 | Validator "session" expires after 60 min idle; next Inferno run shows `Net::ReadTimeout` / one `error` while it re-instantiates | validator-wrapper `sessionCacheDuration=60` | Re-run the group; or raise the cache duration when starting the wrapper |

## UDS+

| # | Symptom | Root cause | Fix |
| --- | --- | --- | --- |
| U1 | No installable "UDS+ application" exists | HRSA publishes an IG and a test kit; the repo `drajer-health/uds-plus` is the IG source | Implemented the Data Submitter / de-identification actors in the .NET layer and used Inferno's UDS+ kit as the receiver |
| U2 | Bulk `$export` is disabled by default in the HAPI starter | `bulk_export_enabled: false` | enabled in `application-local.yaml`; `Group/uds-plus-reporting-group` added to the sample data |
| U3 | UDS+ kit: `uds-plus-diag-8`, `uds-plus-enc-3/4`, `uds-plus-medreq-1` (dates must be truncated to year), `Observation.issued` max 0, `Condition.category` max 1, race/ethnicity `detailed` slice required | Our first de-identifier kept full dates and US Core extensions | Implemented the IG constraints in `DeIdentifier.cs`; year truncation applies only to Condition, Encounter and MedicationRequest — truncating Observation dates breaks the base vital-signs rule vs-1 (day precision) |
