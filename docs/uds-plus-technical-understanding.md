# UDS+ (Uniform Data System Plus) — Technical Understanding

## 1. What UDS+ is

HRSA's Uniform Data System (UDS) is the annual report every Health Center Program awardee and
look-alike (Federally Qualified Health Centers, FQHCs) must file. Historically it is a set of
aggregate tables (patients by ZIP code, demographics in Tables 3A/3B/4, staffing and utilization in
Table 5, diagnoses and services in Tables 6A/6B, clinical quality measures in Table 7, etc.).

**UDS+ modernizes that report**: instead of aggregate counts, health centers submit **de-identified,
patient-level FHIR R4 data** using HL7 FHIR Bulk Data. HRSA then computes the tables itself. The
specification is the **HRSA UDS+ FHIR Implementation Guide**:

| Item | Value |
| --- | --- |
| Canonical | `http://fhir.org/guides/hrsa/uds-plus/ImplementationGuide/fhir.hrsa.uds-plus` |
| Current version used here | **2.0.0 (STU2)** — package `fhir.hrsa.uds-plus#2.0.0` |
| Published at | https://fhir.org/guides/hrsa/uds-plus/STU2/ |
| Source repo | https://github.com/drajer-health/uds-plus (IG source only, no runnable server) |
| Base FHIR | R4 (4.0.1), builds on US Core |
| Conformance testing | Inferno UDS+ Test Kit (https://github.com/inferno-framework/uds-plus-test-kit), running locally on http://localhost:4568 |

An important finding during this assessment: **there is no HRSA-published "UDS+ server" to install**.
The IG defines actors and operations; the runnable pieces are (a) a FHIR data source that supports
Bulk Data export, (b) a submitter/de-identification pipeline that each health center or vendor
builds, and (c) HRSA's receiver (for which Inferno's test kit is the public stand-in). In this
project the submitter pipeline is implemented in the .NET integration layer
(`dotnet/FhirIntegration.Api`, `POST /api/udsplus/submit`), HAPI FHIR is the data source, and the
Inferno UDS+ kit plus a small mock `$import` receiver play HRSA.

## 2. Actors and the end-to-end flow

```
 Health Center EHR            Data Submitter (health center / vendor)           HRSA
 ┌──────────────────┐   1 $export   ┌──────────────────────────────┐  5 $import  ┌──────────────────┐
 │ Data Source      │ ───────────▶  │ download NDJSON              │ ─────────▶  │ Data Receiver    │
 │ (HAPI FHIR R4,   │ ◀───────────  │ 2 de-identify ($deidentify / │ ◀─────────  │ validates files, │
 │  Group/$export)  │  NDJSON files │   Trust Service Provider)    │  status /   │ returns Operation│
 └──────────────────┘               │ 3 validate vs UDS+ profiles  │  Operation  │ Outcomes, builds │
                                    │ 4 host files + build manifest│  Outcome    │ UDS tables       │
                                    └──────────────────────────────┘             └──────────────────┘
```

1. **Data Source** (EHR) maintains a `Group` of patients who qualify for the reporting year and
   supports `GET [base]/Group/[id]/$export` with `_outputFormat=application/ndjson`, `_type`, and
   `_since` (Bulk Data Access IG, async request pattern: `Prefer: respond-async`, poll the
   `Content-Location` until HTTP 200, then download each `output[].url`).
   *Locally*: HAPI starter with `bulk_export_enabled: true`; `Group/uds-plus-reporting-group`.
2. **Trust Service Provider / de-identification** removes direct identifiers before anything leaves
   the health center. The IG defines a `$deidentify` operation (Parameters in, Parameters out) but
   leaves the algorithm to implementers. Minimum expectations drawn from the profiles:
   - `Patient`: no name, telecom, address, photo, contact, identifiers other than a pseudonymous
     linkage id; `birthDate` replaced by an **age** extension; race, ethnicity, sex, veteran status,
     agricultural-worker status, housing status, income captured as extensions / observations.
   - Clinical resources keep codes and dates but reference only the pseudonymous patient id; no
     practitioner names.
   - The submitter must retain the mapping from real id to pseudonym (re-identification is only
     possible inside the health center).
3. **Validate** each resource against the UDS+ profiles before submission (HL7 validator with
   `-ig fhir.hrsa.uds-plus#2.0.0`, or the Inferno kit).
4. **Host the NDJSON files** at URLs HRSA can fetch and build the **Import Manifest** — a
   `Parameters` resource conforming to `uds-plus-import-manifest`
   (`inputFormat`, `inputSource`, one `input` part per file with `type` + `url`, plus the
   reporting-period and submitter metadata parameters).
5. **Data Submitter notifies HRSA** with `POST [hrsa-base]/$import` carrying the manifest. HRSA
   downloads the files asynchronously, validates them, and exposes a status endpoint that returns
   `OperationOutcome` errors/warnings. A failed submission is fixed and **re-submitted in full**
   (a resubmission replaces the earlier one).

## 3. Key profiles (all FHIR R4, canonical prefix `http://fhir.org/guides/hrsa/uds-plus/StructureDefinition/`)

| Profile id | Base | Purpose / UDS table |
| --- | --- | --- |
| `de-identified-uds-plus-patient` | Patient | Demographics: age extension, sex, race, ethnicity, ZIP (via patients-by-ZIP reporting), veteran, housing, agricultural-worker status |
| `uds-plus-encounter` | Encounter | Visits (Table 5 utilization), class, type, period, service provider |
| `uds-plus-diagnosis` | Condition | Table 6A selected diagnoses (ICD-10-CM / SNOMED) |
| `uds-plus-procedure` | Procedure | Table 6A services, CPT/HCPCS |
| `uds-plus-observation`, `uds-plus-lab-observation`, `uds-plus-clinical-result-observation` | Observation | Screenings, vitals, labs that feed Table 6B / 7 measures (HbA1c, BP, BMI, depression screening, SDOH) |
| `de-identified-uds-plus-income-observation` | Observation | Income as % of poverty guideline (Table 4) |
| `uds-plus-immunization`, `uds-plus-medicationrequest`, `uds-plus-medicationstatement`, `uds-plus-allergyintolerance`, `uds-plus-servicerequest`, `uds-plus-adverseevent` | — | Clinical quality measure numerators/denominators |
| `uds-plus-coverage` | Coverage | Insurance (Table 4, Medicaid/Medicare/private/uninsured) |
| `uds-plus-location`, `uds-plus-relatedperson` | — | Service sites, relationships |
| `uds-plus-import-manifest` | Parameters | Submission manifest for `$import` |
| `uds-plus-deidentify-data-urls-parameter`, `uds-plus-patient-reporting-parameters` | Parameters | `$deidentify` input/output and per-patient reporting flags (e.g. capitated vs FFS member months) |

Extensions worth knowing: `uds-plus-age-extension`, `uds-plus-housing-status-extension`,
`uds-plus-veteran-status-extension`, `uds-plus-agriculture-worker-status-extension`,
`uds-plus-sex-extension`, plus the US Core race/ethnicity extensions reused as-is.

Capability statements: *Data Source*, *Data Submitter (client)*, *Data Receiver (server)*,
*Trust Service Provider*.

## 4. How this maps to what was built locally

| IG actor | Local component | Evidence |
| --- | --- | --- |
| Data Source | HAPI FHIR R4 (`http://localhost:8080/fhir`), `Group/uds-plus-reporting-group`, `$export` enabled | `results/` export output, HAPI CapabilityStatement lists `export`/`export-poll-status` |
| Data Submitter + Trust Service Provider | .NET `UdsPlusSubmitterService` + `DeIdentifier` (`POST /api/udsplus/submit`) | NDJSON + `manifest.json` + `validation-report.json` under `results/dotnet-output/udsplus/<run>/` |
| Data Receiver | (a) .NET mock `POST /udsplus/$import` with status polling; (b) Inferno UDS+ Test Kit (`http://localhost:4568`) acting as HRSA's receiver and validating every file | Inferno test-session results |
| Validation | HL7 validator wrapper (`http://localhost:8082`) with `fhir.hrsa.uds-plus#2.0.0` | `validation-report.json` |

## 5. Architectural implications for a vendor (e.g. an ambulatory EHR)

- **Bulk Data export is the entry point.** The EHR must expose a stable `Group` per reporting year
  (ideally dynamic / query-based) and performant `$export` with `_type`/`_since` filtering. For a
  cloud deployment such as AWS HealthLake, `$export` is available natively; the submitter pipeline
  only needs IAM credentials and the Group id.
- **De-identification must happen inside the covered entity's trust boundary.** Keep the
  pseudonym crosswalk (hash + salt or token vault) in the EHR tenant; never ship it with the data.
- **Validation before submission saves cycles.** HRSA rejects whole submissions; running the HL7
  validator with the UDS+ package (or the Inferno kit) in CI catches profile errors early.
- **Terminology matters.** Measures depend on value sets (CPT, ICD-10-CM, LOINC, SNOMED CT,
  RxNorm, CVX). The data must carry the codes US Core already requires, so good US Core conformance
  (tested here with Inferno) is the foundation of UDS+ readiness.
- **Operational model.** Submission is annual but the pipeline should be runnable on demand for
  preview/validation runs; status polling and OperationOutcome triage should be surfaced to health
  center staff.
