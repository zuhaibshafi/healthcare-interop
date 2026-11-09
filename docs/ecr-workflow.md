# Electronic Case Reporting (eCR) workflow and how eCRNow implements it

## 1. The public-health problem

Clinicians are legally required to report certain conditions (COVID-19, measles, Lyme disease, STIs,
etc.) to their state/local public health agency (PHA). Manual reporting is slow and incomplete.
**eCR** automates it: the EHR detects a potentially reportable encounter, assembles an
**electronic Initial Case Report (eICR)**, sends it to the shared **APHL AIMS platform**, where the
CDC/CSTE **Reportable Condition Knowledge Management System (RCKMS)** decides whether the case is
reportable in the relevant jurisdiction(s) and returns a **Reportability Response (RR)** to the
EHR and the PHA.

Standards involved:

| Artifact | Standard |
| --- | --- |
| Trigger codes (what to look for) | **eRSD** — electronic Reporting and Surveillance Distribution: a FHIR Bundle of `PlanDefinition` + `ValueSet`s (+ `Library`) published by CSTE/APHL (`http://hl7.org/fhir/us/ecr/PlanDefinition/plandefinition-ersd-instance`) |
| Report | **eICR** — HL7 CDA R2 IG Public Health Case Report (R1.1 / R3.1) or FHIR eICR (HL7 eCR FHIR IG `Bundle` of type `document`/`message`) |
| Response | **Reportability Response** (CDA RR or FHIR RR bundle) |
| EHR data access | FHIR R4 US Core (SMART Backend Services / system launch) |
| Transport | Direct messaging (SMTP/S-MIME), XDR, or FHIR REST `$process-message` |

## 2. The eRSD PlanDefinition state machine (what eCRNow executes)

The eRSD bundle shipped with eCRNow (`documents/app-artifacts/kars/Bundle-bundle-ersd-specification-bundle.json`,
708 resources) contains one `PlanDefinition` whose actions eCRNow turns into a timer-driven
workflow:

```
encounter-start (named event / launchPatient)
   └─▶ start-workflow            initiate-reporting-workflow
         └─▶ check-reportable     execute-reporting-workflow
               ├─ ckr-is-encounter-reportable   check-trigger-codes  (match Condition/Observation/
               │                                  MedicationRequest/Immunization/... codes against the
               │                                  trigger ValueSets, e.g. dxtc "Diagnosis_Problem Triggers")
               └─ ckr-is-encounter-in-progress  evaluate-condition   (re-arm timer while encounter open)
         └─▶ create-and-report-eicr
               ├─ car-is-encounter-in-progress  (loop)
               └─ car-is-encounter-finished     ─▶ report-eicr
                     └─ create-eicr              create-report   (query EHR FHIR for all eICR sections,
                                                                  build CDA R1.1/R3.1 or FHIR eICR)
                     └─ validate-eicr            validate-report (schematron / FHIR validation)
                     └─ submit-eicr              submit-report   (Direct, XDR, or REST $process-message)
```

Our sample data is designed to fire this path: `Condition/demo-condition-covid` carries SNOMED
`840539006` (COVID-19), which is in the eRSD `dxtc` trigger value set
(`http://hl7.org/fhir/us/ecr/ValueSet/dxtc`) and the COVID_19 (Disorders) value set
`2.16.840.1.113762.1.4.1146.1124`. The encounter is `finished`, so the workflow proceeds straight to
create → validate → submit.

## 3. eCRNow application architecture (Release 3.x / 4.x codebase)

| Layer | Implementation |
| --- | --- |
| Runtime | Spring Boot 3 WAR (`target/ecr-now.war`), Java 17+, port 8081 |
| Persistence | PostgreSQL (Hibernate `hbm2ddl.auto=update` creates ~20 tables: `healthcare_setting`, `kar_repos`, `hs_kar_status_v2`, `notification_context`, `eicr`, `ph_messages`, `scheduled_tasks_v2`, …) |
| Scheduling | db-scheduler (`scheduled_tasks_v2`) — every PlanDefinition action becomes a persisted timer |
| Knowledge Artifacts | `KarParserImpl` loads every Bundle in `kar.directory` at startup into `kar_repos` / `KnowledgeArtifactRepository` (local repo URL `http://localhost`, name `local-repo`) |
| EHR access | `EhrFhirR4QueryServiceImpl` (HAPI FHIR client) with paging, query-by-period, retry; auth via `AuthorizationUtils` (System/SofSystem = client-credentials, SofBackend = JWT backend services, UserNamePwd, or no auth) |
| Public APIs | `/api/healthcareSettings`, `/api/kars`, `/api/addKARStatus/`, `/api/launchPatient`, `/api/reLaunchPatient`, `/api/receiveReportabilityResponse`, `/api/eicr`, `/api/phMessage`, `/api/notificationContexts`, Swagger at `/swagger-ui/index.html` |
| Outputs | `bsa.output.directory` (debug JSON/XML of every step when `save.debug.files=true`), `eicr` + `ph_messages` tables |

### Configuration objects
- **HealthcareSetting** — one per EHR FHIR endpoint: `fhirServerBaseURL`, `fhirVersion`, `authType`,
  `clientId/clientSecret/tokenUrl/scopes`, transport flags (`isDirect`, `isXdr`, `isRestAPI`,
  `restApiUrl`), organisation identifiers, encounter thresholds, off-hours windows.
- **KnowledgeArtifactStatus** — activates a KAR (`karId|karVersion`) for a HealthcareSetting and
  chooses the output format (`FHIR`, `CDA_R11`, `CDA_R31`, `BOTH`).
- **PublicHealthAuthority** — credentials for the PHA/AIMS FHIR endpoint used by `submit-report`.

## 4. Local integration pattern used in this assessment

```
 HAPI FHIR R4 (8080) ◀──── FHIR R4 queries (Patient, Encounter, Condition, Observation, …) ──── eCRNow (8081)
        ▲                                                                                        │
        │  loads US Core sample data                                                             │ eICR (FHIR Bundle)
 .NET integration layer (5080) ────────── POST /api/launchPatient ───────────────────────────────▶│
        ▲  /oauth/token  (client_credentials facade so eCRNow's "System" auth type works)        │
        └───────────────── POST /pha/r4/fhir/$process-message  (mock PHA / AIMS receiver) ◀──────┘
```

Run `python scripts/ecrnow-demo.py` to: register HAPI as a HealthcareSetting → activate the eRSD KAR
→ `launchPatient(demo-patient-1, demo-encounter-1)` → observe the eICR and the mock Reportability
Response. Debug artefacts land in `C:\git\ecrnow-runtime\bsa-output\`.

## 5. Integration issues found and how they were resolved

| # | Issue | Root cause | Resolution |
| --- | --- | --- | --- |
| 1 | `mvn package` fails: `NoSuchFieldError: JCTree$JCImport ... qualid` | Lombok 1.18.22 pinned in `pom.xml` is incompatible with JDK 21 | Bumped Lombok to 1.18.36 (single-line pom change); alternatively build with JDK 17 |
| 2 | Spotless `check` goal fails the build on a clean clone | Formatting check runs in `verify` phase | `-Dspotless.check.skip=true` for local builds |
| 3 | Unit tests execute even with `-DskipTests` | pom wires surefire to `${skip.unit.tests}` | `-Dmaven.test.skip=true` (or `-Dskip.unit.tests=true -Dskip.integration.tests=true`) |
| 4 | `POST /api/healthcareSettings` returns 500 when `tokenUrl` is omitted | Controller dereferences a null `hsd` while trying to discover the token endpoint from `CapabilityStatement.rest.security` | Always send `tokenUrl` (pointing at the .NET `/oauth/token` facade) |
| 5 | eCRNow always requests an OAuth token for the EHR | Open HAPI has no token endpoint | Either `authType: "None"` (eCRNow then calls HAPI without a bearer token) or `authType: "System"` against the .NET client-credentials facade |
| 6 | Release-2 property `ersd.file.location` still mandatory | Legacy loader | Point it to a non-existent file (documented in README) |
| 7 | eRSD typo `$procedureOrders` (FHIRPath), missing related-data links between create/validate/submit, submission endpoint must be the PHA base URL, PHA registration, one notification context per encounter | See `troubleshooting.md` items C6, C11–C14 | Patched runtime KAR copy, fixed `report-submission.endpoint`, registered PHA, demo script clears stale contexts |
| 8 | No Docker / WSL / admin on the workstation | Corporate laptop | Everything runs from portable binaries under `C:\git\tools` (JDK 21, Maven, PostgreSQL 16, Redis 8, Ruby 3.3) |
