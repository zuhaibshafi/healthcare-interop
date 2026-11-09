# Inferno conformance testing — set-up, execution and results

Two Inferno test kits run natively on this Windows workstation (no Docker):

| Kit | Suite id | URL | Purpose |
| --- | --- | --- | --- |
| US Core Test Kit (inferno_core 1.5.0) | `us_core_v610` — group *US Core FHIR API* | http://localhost:4567 | Server conformance of HAPI FHIR to US Core 6.1.0 |
| UDS+ Test Kit 1.3.0 (inferno_core 1.0.3) | `uds_plus` — group *Import Manifest Tests* | http://localhost:4568 | Validates the UDS+ submission (manifest + NDJSON) produced by the .NET Data Submitter, acting as HRSA's Data Receiver |

Both kits use the standalone HL7 validator wrapper on http://localhost:8082 (IG packages cached in
`%USERPROFILE%\.fhir\packages`). Runs are driven headlessly through Inferno's JSON API by
`scripts/inferno-run.py`, which writes `results/<name>.json` (raw) and `results/<name>.md`
(per-test table). The same sessions can be opened in the browser (links below) for the full UI view.

## 1. US Core Server v6.1.0 against HAPI FHIR R4

Inputs: `url=http://localhost:8080/fhir`, `patient_ids=demo-patient-1,demo2-patient`, no SMART
authentication (`smart_auth_info={"auth_type":"public"}`), group `us_core_v610_fhir_api` only
(the SMART App Launch and granular-scope groups require an OAuth server and were out of scope).

### Iterations (how the result was reached)

| Run | pass | fail | skip | error | What changed before the run |
| --- | --- | --- | --- | --- | --- |
| 1 | 119 | 9 | 288 | 36 | Baseline: single patient, plain HAPI. Errors = validator still downloading IG packages; fails = US Core search params unknown to HAPI (`CareTeam?role`, `Encounter?discharge-disposition` → HTTP 400), no US Core profiles advertised, no PractitionerRole/Provenance/DAR data |
| 2 | 280 | 4 | 90 | 79 | Second patient with 67 US Core 6.1.0 resources (Provenance, Device, Coverage, Specimen, QuestionnaireResponse …); `hl7.fhir.us.core#6.1.0` installed into HAPI (search parameters + supported profiles). Errors = Inferno treated the quoted inline `FHIR_RESOURCE_VALIDATOR_EXPANSION_PARAMETERS` as a file path on Windows |
| 3 | 324 | 19 | 110 | 0 | Expansion parameters moved to a file → validation live. Remaining fails were data quality: wrong `display` strings (validator is strict), example.org URLs, unknown NDC/goal-category codes |
| 4 | 339 | 4 | 110 | 0 | Display names removed / codes corrected in both sample bundles |
| **5 (final)** | **358** | **2** | **93** | **0** | Detailed-ethnicity coding dropped (VSAC value set cannot be expanded offline), DataAbsentReason example uses code `unknown` as US Core requires |

Final session: http://localhost:4567/test_sessions/ — see `results/inferno-us-core-v610.md` for the
per-test table and `results/inferno-us-core-v610-by-group.md` for the per-profile summary (50 groups).

### Remaining failures (2) — both environmental, not data defects

| Test | Reason | Production remediation |
| --- | --- | --- |
| Capability Statement → *standalone_auth_tls* | Local HAPI is served over plain HTTP | Terminate TLS 1.2+ at a reverse proxy / load balancer |
| Capability Statement → *us_core_instantiate* | HAPI's generated CapabilityStatement does not declare `instantiates: http://hl7.org/fhir/us/core/CapabilityStatement/us-core-server` even though every US Core profile is listed under `supportedProfile` | Register a `CapabilityStatement` customisation interceptor (`IServerConformanceProvider`) that adds the canonical |

### Skips (93)
Skips are tests whose preconditions were not met by the sample population, e.g. 18 Provenance checks
for resource types where Provenance was not linked, Observation profiles with no instances
(pediatric BMI/head circumference/weight-for-height, pregnancy status/intent, treatment intervention
preference, care experience preference), and "SHOULD" search combinations whose parameter values do
not exist in the data. They are not failures; adding more synthetic data would convert them to passes.

### What the passing tests demonstrate
- CapabilityStatement declares JSON support, US Core profiles and the required resource interactions.
- Read / search / `_include` / `_revinclude` / chained and combined search parameters for Patient,
  AllergyIntolerance, CarePlan, CareTeam, Condition (both profiles), Coverage, Device, DiagnosticReport
  (lab + note), DocumentReference, Encounter, Goal, Immunization, Location, Medication,
  MedicationRequest, Observation (lab, vitals, smoking status, screening/assessment, clinical result,
  SDOH, occupation, sexual orientation…), Organization, Practitioner, PractitionerRole, Procedure,
  Provenance, QuestionnaireResponse, RelatedPerson, ServiceRequest, Specimen.
- Every returned resource validates against its US Core 6.1.0 profile with the HL7 validator
  (terminology bindings included) — 0 profile-validation failures in the final run.
- Must-support element coverage and reference-resolution checks pass for the two patients.
- Missing-data representation (DataAbsentReason extension and code system) is demonstrated.

## 2. UDS+ Test Kit against the .NET Data Submitter output

Input: `import_manifest=<URL of manifest.json served by the .NET layer>` (e.g.
`http://localhost:5080/udsplus/files/<run-id>/manifest.json`). The kit downloads the manifest,
validates it against `uds-plus-import-manifest`, reads every referenced NDJSON file and validates
each resource against the matching UDS+ 2.0.0 profile.

| Run | pass | fail | omit | error | Notes |
| --- | --- | --- | --- | --- | --- |
| 1 (single patient, first de-identifier) | 8 | 5 | 10 | 0 | Manifest + file reading valid; profile failures told us exactly what the IG expects: `uds-plus-diag-8`, `uds-plus-enc-3/4`, `uds-plus-medreq-1` (dates truncated to year), `Observation.issued` forbidden, race/ethnicity extensions need the `detailed` slice |
| 2 (both patients, year truncation everywhere) | 8 | 4 | 10 | 1 | Over-truncation: vital-sign `effectiveDateTime` must stay day-precise (vs-1); `Condition.category` max 1; stale NDC coding; detailed race code not expandable offline. The single `error` was a validator session that expired mid-run (timeout) |
| 3 (truncation limited to Condition/Encounter/MedicationRequest) | 8 | 5 | 10 | 0 | Showed the opposite edge: the IG's year-truncation invariants exist on *every* profile except Observation (uds-plus-all-1, uds-plus-diag-1 incl. dates inside extensions, uds-plus-imm-3, uds-plus-proc-2/3) |
| **4 (final: truncate all dates except Observation)** | **12** | **1** | **10** | **0** | Final de-identifier (`results/inferno-uds-plus.md`). The single failure is the offline detailed race/ethnicity value-set limitation described below; all other UDS+ profiles (diagnosis, encounter, procedure, immunization, allergy, lab, observation, medication request) and the manifest validate cleanly |

"Omit" tests are UDS+ data types the submission did not contain (Coverage, RelatedPerson,
Location, AdverseEvent, Income, ServiceRequest, MedicationStatement, clinical-result, data-URL and
patient-reporting-parameter files) — the kit skips validation for absent file types.

Known limitation: the offline validator cannot expand the VSAC "Detailed race / Detailed ethnicity"
value sets (`Cannot process resource ... at "exclude[0].filter"`), so the Patient `detailed`
coding is reported as an error even though the code is valid; with a terminology server
(tx.fhir.org) configured on the validator wrapper this check passes.

## 3. How to re-run

```bash
# US Core (≈ 1–2 min once the validator session is warm; first validation can take 10+ min)
python scripts/inferno-run.py --inferno http://localhost:4567 --suite us_core_v610 \
  --group us_core_v610-us_core_v610_fhir_api \
  --input url=http://localhost:8080/fhir --input patient_ids=demo-patient-1,demo2-patient \
  --input 'smart_auth_info={"auth_type":"public"}' --out results/inferno-us-core-v610

# UDS+ (use the manifestUrl returned by POST http://localhost:5080/api/udsplus/submit?groupId=uds-plus-reporting-group&validate=true)
python scripts/inferno-run.py --inferno http://localhost:4568 --suite uds_plus \
  --group uds_plus-uds_plus_test_group --input import_manifest=<manifestUrl> --out results/inferno-uds-plus
```
Or use the web UIs (http://localhost:4567, http://localhost:4568): create a session for the suite,
enter the same inputs, run the group.
