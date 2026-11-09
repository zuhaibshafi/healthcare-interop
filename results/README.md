# Results index

| Deliverable | Evidence |
| --- | --- |
| HAPI FHIR R4 running + basic FHIR operations | `fhir-demo/README.md` (22 scripted calls: metadata, transaction, read, vread, search, `_include`/`_revinclude`, `$everything`, create/update/PATCH/history/delete, `$validate`, Bulk `$export` kick-off) with one JSON response per call; `01-transaction-response.json` |
| eCRNow ↔ HAPI integration | `ecr/` — `TriggerQueryBundle_*.json` (resources eCRNow pulled from HAPI and matched against the eRSD trigger value set), `create-report_*.json/.xml` (eICR document), `pha-received-eicr.json` (the `eicr-case-report-message` bundle as received by the .NET public-health endpoint), `ecrnow-run-log.txt`, `ecrnow-demo-final-run.txt` |
| Inferno US Core v6.1.0 | `inferno-us-core-v610.md` (final per-test table), `inferno-us-core-v610-by-group.md`, `inferno-us-core-v610.json` (raw API output); earlier iteration `inferno-us-core-v610-run2.md` |
| Inferno UDS+ 1.3.0 | `inferno-uds-plus.md` / `.json` (final), `inferno-uds-plus-run1.*` (first iteration) |
| .NET integration layer smoke tests | `dotnet-smoke/*.json` — health, metadata, patient search/CRUD, `$everything`, encounter create, transaction, `$validate`, OAuth token, PHA `$process-message`, eCRNow proxy, UDS+ submit (v1, no-validate, v2) |
| UDS+ submissions (de-identified NDJSON, import manifests, validation reports) | `dotnet-output/udsplus/<run-id>/` and the mock receiver's copies in `dotnet-output/udsplus-received/` |
| eICRs received by the mock PHA | `dotnet-output/eicr/` |

See `../docs/inferno-results.md` for the interpretation of the Inferno numbers and
`../docs/troubleshooting.md` for every issue found on the way.
