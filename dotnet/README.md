# FhirIntegration.Api — FHIR integration layer (.NET proof-of-concept)

A single ASP.NET Core minimal-API service (`net9.0`, Firely .NET SDK) that sits between
consumers and a HAPI FHIR R4 server and demonstrates four interoperability scenarios in one
codebase:

| Scenario | What it shows |
|---|---|
| **FHIR gateway / facade** | Simplified DTO endpoints and raw-FHIR pass-through over HAPI (search, CRUD, `$everything`, transaction, `$validate`) using `FhirClient`. |
| **OAuth2 token facade** | A client-credentials token endpoint so eCRNow can use its "System" auth type against an open HAPI server. |
| **Public Health Agency (PHA) mock** | Receives eICR message bundles from eCRNow at `$process-message`, persists them, and answers with a Reportability-Response-style acknowledgement. |
| **UDS+ Data Submitter pipeline** | Bulk `$export` → de-identification per the HRSA UDS+ IG → NDJSON + Import Manifest → HL7 validator → `$import` to a mock HRSA Data Receiver. |

```
clients ──► FhirIntegration.Api (http://localhost:5080)
             ├─ /api/fhir/*, /api/patients/*, /api/encounters      FHIR gateway  ──────────────► HAPI FHIR R4   :8080/fhir
             ├─ /oauth/token                                       OAuth2 facade (for eCRNow)
             ├─ /pha/r4/fhir/$process-message                      PHA receiver mock ◄──────────  eCRNow         :8081
             ├─ /api/ecr/*                                         eCRNow client  ──────────────► eCRNow         :8081
             ├─ /api/udsplus/submit, /udsplus/files/*              UDS+ Data Submitter ─────────► HAPI $export, validator :8082
             ├─ /udsplus/$import, /udsplus/$import-status/{id}     UDS+ Data Receiver mock
             └─ /health, /swagger
```

## Why this shape

* **Gateway / facade pattern.** Consumers never talk to HAPI directly. `Services/FhirGatewayService`
  owns the FHIR REST semantics (search parameters, operations, profile tagging, error mapping) and
  exposes either narrow DTOs (`/api/patients` list, `POST /api/patients`, `POST /api/encounters`)
  or raw `application/fhir+json` for FHIR-aware clients. Swapping the FHIR server is configuration.
* **Firely .NET SDK (`Hl7.Fhir.R4` 5.13.4).** Typed R4 POCOs give compile-time safety when
  *building* resources (US Core Patient/Encounter, the UDS+ manifest `Parameters`,
  acknowledgement `MessageHeader`s), `FhirClient` handles REST/operations/paging, and
  `FhirJsonParser`/`FhirJsonSerializer` give lenient parsing of upstream data. The HTTP boundary
  passes FHIR payloads as strings so Swagger does not have to describe the FHIR object graph.
* **JSON-tree de-identification.** `Services/DeIdentifier` works on `System.Text.Json` nodes, not
  POCOs, so one generic rule set (strip narrative/identifiers/display text, drop person and
  organisation references, pseudonymise Patient references) applies to every resource type and
  unknown content round-trips untouched. Profiles come from a small mapping table.
* **Everything on disk.** Each UDS+ run, received eICR and received submission is written under
  `Fhir:OutputDirectory` so reviewers can inspect before/after data without a database.

## Layout

```
dotnet/
├── FhirIntegration.sln
├── FhirIntegration.Api.http              sample requests (REST Client / Rider / VS)
├── README.md
└── FhirIntegration.Api/
    ├── Program.cs                        DI wiring, HttpClients, Swagger, error handler
    ├── appsettings.json                  Fhir / Auth / UdsPlus sections
    ├── Models/
    │   ├── Options.cs                    FhirOptions, AuthOptions, UdsPlusOptions, HealthCenterInfo
    │   └── Dtos.cs                       request/response contracts
    ├── Services/
    │   ├── FhirGatewayService.cs         FhirClient-based gateway over HAPI
    │   ├── FhirSerialization.cs          Firely parser/serializer wrapper
    │   ├── BulkDataExportClient.cs       $export kick-off / poll / NDJSON download
    │   ├── DeIdentifier.cs               UDS+ de-identification rules + profile table (Trust Service Provider role)
    │   ├── UdsPlusManifestBuilder.cs     UDS+ Import Manifest (Parameters)
    │   ├── ValidatorClient.cs            HL7 validator wrapper client (session reuse)
    │   ├── UdsPlusSubmitterService.cs    pipeline orchestration (Data Submitter)
    │   ├── UdsPlusReceiverService.cs     mock HRSA $import (Data Receiver)
    │   ├── PhaReceiverService.cs         eICR persistence + acknowledgement
    │   ├── EcrNowClient.cs               eCRNow REST client
    │   ├── TokenService.cs               client-credentials JWT issuer
    │   └── OutputPaths.cs                on-disk layout, path-traversal guard
    └── Endpoints/
        ├── FhirGatewayEndpoints.cs  AuthEndpoints.cs  PhaEndpoints.cs
        ├── EcrEndpoints.cs          UdsPlusEndpoints.cs  HealthEndpoints.cs
        └── EndpointHelpers.cs            raw-body reading, OperationOutcome error mapping
```

## Running

Prerequisites: .NET SDK 9.x, HAPI FHIR at `http://localhost:8080/fhir`, the validator wrapper at
`http://localhost:8082` (and optionally eCRNow at `http://localhost:8081`). Adjust `appsettings.json`
(`Fhir` section) if they live elsewhere.

```bash
cd dotnet
dotnet build
dotnet run --project FhirIntegration.Api
# Swagger UI: http://localhost:5080/swagger
```

Configuration (`appsettings.json`):

```json
"Fhir": {
  "BaseUrl": "http://localhost:8080/fhir",
  "ValidatorUrl": "http://localhost:8082",
  "EcrNowUrl": "http://localhost:8081",
  "OutputDirectory": "C:/git/interop-assessment/results/dotnet-output",
  "PublicBaseUrl": "http://localhost:5080"
}
```

## Endpoints

| Method | Path | Purpose |
|---|---|---|
| GET | `/health` | Per-dependency reachability (HAPI, validator, eCRNow) |
| GET | `/api/fhir/metadata` | CapabilityStatement summary (fhirVersion, software, resource type count) |
| POST | `/api/fhir/transaction` | Forward a transaction/batch Bundle; returns per-entry statuses |
| POST | `/api/fhir/validate?profile=` | HAPI `$validate`; returns OperationOutcome |
| GET | `/api/patients?family=&given=&identifier=` | Patient search → simplified DTO list |
| GET | `/api/patients/{id}` | Raw FHIR Patient |
| POST | `/api/patients` | Create US Core Patient from `{family, given, gender, birthDate, mrn}` |
| PUT | `/api/patients/{id}` | Update with raw FHIR Patient JSON |
| DELETE | `/api/patients/{id}` | Delete (HAPI returns 409 if other resources still reference the patient) |
| GET | `/api/patients/{id}/encounters` · `/conditions` · `/observations?category=` | Patient-scoped searches (FHIR searchset) |
| GET | `/api/patients/{id}/everything` | `$everything` with `{resourceType: count}` summary + Bundle |
| POST | `/api/encounters` | Create US Core Encounter `{patientId, start, end, classCode, typeText, status}` |
| POST | `/oauth/token` | OAuth2 client-credentials token (Basic or form client auth) |
| GET | `/.well-known/smart-configuration` | Discovery document for the token facade |
| GET | `/pha/r4/fhir/metadata` | Minimal R4 CapabilityStatement advertising `$process-message` (eCRNow's FHIR client probes this) |
| POST | `/pha/r4/fhir/$process-message` (alias `/pha/r4/fhir/Bundle`) | Receive eICR message Bundle; persists to `eicr/`; returns ack Bundle |
| GET | `/pha/received` · `/pha/received/{file}` | List / fetch received eICRs |
| POST | `/api/ecr/launch` | `{patientId, encounterId}` → eCRNow `/api/launchPatient` |
| GET | `/api/ecr/healthcare-settings` | Proxy of eCRNow `/api/healthcareSettings/` |
| POST | `/api/udsplus/submit?groupId=&validate=&submit=` | Run the UDS+ Data Submitter pipeline |
| GET | `/api/udsplus/runs` | Previous runs and artefact URLs |
| GET | `/udsplus/files/{runId}/{file}` | Read-only NDJSON / manifest download (`application/fhir+ndjson`) |
| POST | `/udsplus/$import` | Mock HRSA Data Receiver; 202 + `Content-Location` |
| GET | `/udsplus/$import-status/{id}` · `/udsplus/received` | Receiver status / all submissions |

## Example calls

```bash
curl http://localhost:5080/health
curl http://localhost:5080/api/fhir/metadata
curl "http://localhost:5080/api/patients?family=Shaw"
curl http://localhost:5080/api/patients/demo-patient-1
curl http://localhost:5080/api/patients/demo-patient-1/everything

# create / delete a patient
curl -X POST http://localhost:5080/api/patients -H "Content-Type: application/json" \
  -d '{"family":"Test","given":"Integration","gender":"male","birthDate":"1990-05-05","mrn":"MRN-1"}'
curl -X DELETE http://localhost:5080/api/patients/<id>

# encounter
curl -X POST http://localhost:5080/api/encounters -H "Content-Type: application/json" \
  -d '{"patientId":"demo-patient-1","start":"2026-11-08T09:00:00-05:00","end":"2026-11-08T09:30:00-05:00","classCode":"AMB","typeText":"Office Visit","status":"finished"}'

# OAuth2 token (HTTP Basic or form fields)
curl -u ecrnow-local:ecrnow-secret -d "grant_type=client_credentials&scope=system/*.read" http://localhost:5080/oauth/token
curl -d "grant_type=client_credentials&client_id=ecrnow-local&client_secret=ecrnow-secret" http://localhost:5080/oauth/token

# PHA receiver
curl -X POST "http://localhost:5080/pha/r4/fhir/\$process-message" -H "Content-Type: application/fhir+json" --data-binary @eicr.json
curl http://localhost:5080/pha/received

# eCRNow
curl -X POST http://localhost:5080/api/ecr/launch -H "Content-Type: application/json" \
  -d '{"patientId":"demo-patient-1","encounterId":"demo-encounter-1"}'

# UDS+ (first validated run can take several minutes while the validator downloads the IG)
curl --max-time 900 -X POST "http://localhost:5080/api/udsplus/submit?groupId=uds-plus-reporting-group&validate=true"
```

## eCRNow integration

eCRNow's *healthcare setting* has three fields this service satisfies:

| eCRNow setting | Value | Notes |
|---|---|---|
| FHIR server URL | `http://localhost:8080/fhir` | HAPI (open). eCRNow reads Patient/Encounter/… from here. |
| Auth type / token URL | `System` · `http://localhost:5080/oauth/token` | Client-credentials grant; client `ecrnow-local` / `ecrnow-secret` (from `Auth:Clients`). eCRNow gets a signed JWT and sends it as a Bearer token; HAPI ignores it because it is open, so eCRNow's standard SMART Backend Services flow works unmodified. |
| PHA endpoint (`restAPIURL`, `isRestAPI=true`) | `http://localhost:5080/pha/r4/fhir` | eCRNow builds a HAPI FHIR client on this base URL: it reads `/metadata` (served as a minimal CapabilityStatement) and invokes `$process-message` with the eICR message Bundle. The mock stores the bundle under `OutputDirectory/eicr/` and returns a `message` Bundle whose `MessageHeader.response.code = ok`. |

Trigger a report with `POST /api/ecr/launch {"patientId":"demo-patient-1","encounterId":"demo-encounter-1"}`;
this layer adds `fhirServerURL` from configuration plus the `X-Request-ID` header eCRNow requires, and
relays eCRNow's response (including its 422 when the same patient/encounter was already launched).

## UDS+ pipeline and IG actors

The HRSA UDS+ IG (`fhir.hrsa.uds-plus` 2.0.0) defines four actors. Mapping to this code:

| IG actor | Implemented by | Notes |
|---|---|---|
| **Data Source** | HAPI FHIR | Holds identifiable US Core data; `Group/uds-plus-reporting-group` is the reporting population. `BulkDataExportClient` drives `Group/{id}/$export` (`Prefer: respond-async`, poll `Content-Location`, download NDJSON). |
| **Data Submitter** | `UdsPlusSubmitterService` | Orchestrates export → de-identify → NDJSON files → Import Manifest → validation → `$import`. Writes every artefact to `OutputDirectory/udsplus/{runId}/` (`source/*.ndjson` identifiable input, `*.ndjson` de-identified output, `manifest.json`, `validation-report.json`, `run-summary.json`). |
| **Trust Service Provider** (de-identification) | `DeIdentifier` | **Patient:** removes name, telecom, identifier, photo, contact, GP, managing organisation, link, marital status, birthDate; keeps gender, communication, birthsex/sex extensions; adds `uds-plus-age-extension` (Quantity in years, `>= 90` above 89); address collapses to `postalCode` = 3-digit ZIP prefix + `00`; id becomes SHA-256 of the source id; race/ethnicity are normalised to the UDS+ extension profiles (US Core URLs, `ombCategory` kept, mandatory `detailed` derived from the OMB category when the source has none — e.g. White→European, Hispanic→Mexican, Not Hispanic→null flavour `UNK` because the detailed-ethnicity value set has no non-Hispanic codes — and mandatory `text`; derivations are listed in the run `notes`). **All types:** every date/dateTime/instant anywhere in the resource (Periods and extensions included) truncated to the year as the UDS+ invariants require — except on Observation, whose UDS+ profiles have no truncation rule and whose vital signs must stay day-precise (FHIR `vs-1`); ages are computed before truncation. Narrative/identifier/notes removed, `Coding.display` and `CodeableConcept.text` removed, reference `display` stripped, Practitioner/Organization/RelatedPerson references and participant/performer/recorder blocks removed, `Observation.issued` removed, Condition.category capped at one (uds-plus-diagnosis is 1..1), NDC codings dropped from Immunization.vaccineCode (CVX kept), Patient references rewritten to the pseudonym, Encounter gets age-at-visit. `meta.profile` is set from the type→profile table (`de-identified-uds-plus-patient`, `uds-plus-encounter`, `uds-plus-diagnosis`, `uds-plus-procedure`, `uds-plus-observation` / `uds-plus-lab-observation`, `uds-plus-immunization`, `uds-plus-medicationrequest`, `uds-plus-allergyintolerance`). |
| **Data Receiver** | `UdsPlusReceiverService` (`/udsplus/$import`) | Mock HRSA receiver: parses the manifest, actually downloads every `udsData.url`, counts resources per type, persists under `OutputDirectory/udsplus-received/{id}/`, returns `202` + `Content-Location: /udsplus/$import-status/{id}` → `{status, counts, outcomes}`. |

The Import Manifest follows the IG's `Parameters-example`: `inputFormat=fhir+ndjson`, `submissionId`,
`submissionTime`, `hcInformation{uei, grantNumber, hcName, hcAddress, hcContactPerson, hcContactPhone,
hcPrenatalCareByReferralOnly}` (from `UdsPlus:HealthCenter`), `importProtocol=https`, and one
`udsData{type, url, expirationTime}` per NDJSON file. With `validate=true` the manifest and every
de-identified resource are sent in one call to the validator wrapper with IG `fhir.hrsa.uds-plus#2.0.0`;
the session id is cached so later runs reuse the warm engine.

## Known limitations (PoC scope)

* No authentication on the gateway itself; the token facade issues tokens but nothing verifies them (HAPI is open).
* Validation findings are reported, not silently fixed: apart from the documented race/ethnicity
  `detailed` derivation, the pipeline does not invent data, so profile constraints the source data
  cannot satisfy show up in `validation-report.json`. If the validator is unreachable the run still
  completes and `validation.error` says why.
* The offline HL7 validator has no terminology server, so it cannot expand the VSAC-based US Core
  `detailed-race` / `detailed-ethnicity` value sets; the mandatory `detailed` race/ethnicity codings
  therefore show as value-set errors in `validation-report.json` even though the IG requires them.
  Likewise, terminology errors inherited from the source data (for example a US Core 7 category code
  validated against US Core 6.1) are reported, not rewritten.
* Only the eight resource types in `UdsPlus:ExportResourceTypes` are exported and mapped; other
  resources in the reporting group (Coverage, Specimen, Device, ServiceRequest, Provenance, …) are ignored.
* `hcInformation` values are demo placeholders. `bearer` tokens on `udsData` are omitted because the file endpoint is public on localhost.
* The mock receiver keeps submission state in memory (plus `status.json`); it resets on restart.
