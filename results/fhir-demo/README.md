# FHIR API demonstration against HAPI FHIR R4

Base URL: `http://localhost:8080/fhir`  — generated 2026-11-08 23:47:28

| # | Request | HTTP | ms | Result | File |
| --- | --- | --- | --- | --- | --- |
| 1 | `GET /metadata` | 200 | 541 | HAPI FHIR Server 8.12.0 FHIR 4.0.1, 146 resource types | `01-metadata.json` |
| 2 | `POST ` | 200 | 398 | statuses: 200 OK | `02-transaction-load.json` |
| 3 | `GET /Patient/demo-patient-1` | 200 | 13 | Amy Shaw, version 1 | `03-read-patient.json` |
| 4 | `GET /Patient/demo-patient-1/_history/1` | 200 | 14 | versionId 1 | `04-vread-patient.json` |
| 5 | `GET /Patient?family=Shaw&given=Amy` | 200 | 24 | total 1 | `05-search-patient-name.json` |
| 6 | `GET /Patient?identifier=urn:oid:1.2.36.146.595.217.0.1|MRN-000001` | 200 | 43 | total 1 | `06-search-patient-identifier.json` |
| 7 | `GET /Encounter?patient=demo-patient-1&_include=Encounter:practitioner` | 200 | 49 | 2 entries (incl. _include) | `07-search-encounter-by-patient.json` |
| 8 | `GET /Condition?patient=demo-patient-1&code=http://snomed.info/sct|840539006` | 200 | 24 | total 1 (COVID-19 trigger code) | `08-search-condition-code.json` |
| 9 | `GET /Observation?patient=demo-patient-1&category=vital-signs&_sort=-date` | 200 | 37 | total 2 | `09-search-observation-vitals.json` |
| 10 | `GET /Observation?patient=demo-patient-1&code=http://loinc.org|94500-6` | 200 | 38 | total 1 | `10-search-observation-lab-loinc.json` |
| 11 | `GET /Patient?_id=demo-patient-1&_revinclude=*` | 200 | 41 | 16 resources linked to the patient | `11-search-revinclude.json` |
| 12 | `GET /Patient/demo-patient-1/$everything` | 200 | 59 | types: AllergyIntolerance=1, CareTeam=1, Condition=2, DiagnosticReport=1, Encounter=1, Goal=1, Immunization=1, Location=1, MedicationRequest=1, Observation=4, Organization=1, Patient=1, Practitioner=1, Procedure=1 | `12-everything.json` |
| 13 | `POST /Patient` | 201 | 42 | id 23070867-3801-4cbb-b1cc-016790c4c065 version 1 | `13-create-patient.json` |
| 14 | `PUT /Patient/23070867-3801-4cbb-b1cc-016790c4c065` | 200 | 37 | version 2 | `14-update-patient.json` |
| 15 | `PATCH /Patient/23070867-3801-4cbb-b1cc-016790c4c065` | 200 | 141 | gender other version 3 | `15-patch-patient.json` |
| 16 | `GET /Patient/23070867-3801-4cbb-b1cc-016790c4c065/_history` | 200 | 48 | 3 versions | `16-history-patient.json` |
| 17 | `POST /Patient/$validate?profile=http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient` | 200 | 87 | issues: error, warning | `17-validate-patient.json` |
| 18 | `POST /Patient/$validate` | 400 | 33 | 1 errors reported | `18-validate-invalid-patient.json` |
| 19 | `DELETE /Patient/23070867-3801-4cbb-b1cc-016790c4c065` | 200 | 34 | deleted | `19-delete-patient.json` |
| 20 | `GET /Patient/23070867-3801-4cbb-b1cc-016790c4c065` | 410 | 19 | 410 Gone expected | `20-read-deleted-patient.json` |
| 21 | `GET /metadata?_elements=rest.resource.type,rest.resource.searchParam` | 200 | 348 | Patient search params: 33 | `21-capability-search-params.json` |
| 22 | `GET /Group/uds-plus-reporting-group/$export?_type=Patient,Encounter,Condition&_outputFormat=application/fhir%2Bndjson` | 202 | 40 |  | `22-bulk-export-kickoff.json` |
