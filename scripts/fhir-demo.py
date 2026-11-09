#!/usr/bin/env python
"""
Basic FHIR R4 API demonstration against HAPI (create / read / update / search / history / $everything /
$validate / transaction / delete). Writes every response to results/fhir-demo/ and a markdown summary.

Usage: python fhir-demo.py [--base http://localhost:8080/fhir] [--out ../results/fhir-demo]
"""
import argparse, json, os, time, urllib.request, urllib.error

def call(method, url, body=None, headers=None):
    hdrs = {"Accept": "application/fhir+json"}
    if body is not None:
        hdrs["Content-Type"] = "application/fhir+json"
    if headers:
        hdrs.update(headers)
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(url, data=data, method=method, headers=hdrs)
    t = time.time()
    try:
        with urllib.request.urlopen(req, timeout=120) as r:
            raw = r.read().decode()
            return r.status, dict(r.headers), (json.loads(raw) if raw.strip() else None), int((time.time()-t)*1000)
    except urllib.error.HTTPError as e:
        raw = e.read().decode()
        try:
            return e.code, dict(e.headers), json.loads(raw), int((time.time()-t)*1000)
        except json.JSONDecodeError:
            return e.code, dict(e.headers), raw, int((time.time()-t)*1000)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:8080/fhir")
    ap.add_argument("--out", default=os.path.join(os.path.dirname(__file__), "..", "results", "fhir-demo"))
    ap.add_argument("--bundle", default=os.path.join(os.path.dirname(__file__), "..", "sample-data", "transaction-bundle.json"))
    a = ap.parse_args()
    os.makedirs(a.out, exist_ok=True)
    rows = []

    def step(n, name, method, path, body=None, headers=None, summarize=None):
        code, hdrs, resp, ms = call(method, a.base + path, body, headers)
        fn = f"{n:02d}-{name}.json"
        with open(os.path.join(a.out, fn), "w", encoding="utf-8") as f:
            json.dump(resp if isinstance(resp, (dict, list)) else {"raw": resp}, f, indent=1)
        summary = summarize(resp, hdrs) if (summarize and isinstance(resp, (dict, list))) else ""
        rows.append((n, f"{method} {path}", code, ms, summary, fn))
        print(f"{n:02d} {method:6s} {path:70s} -> {code} ({ms} ms) {summary}")
        return resp, hdrs

    bundle = json.load(open(a.bundle, encoding="utf-8"))

    step(1, "metadata", "GET", "/metadata",
         summarize=lambda r, h: f"{r['software']['name']} {r['software']['version']} FHIR {r['fhirVersion']}, {len(r['rest'][0]['resource'])} resource types")
    step(2, "transaction-load", "POST", "", bundle,
         summarize=lambda r, h: "statuses: " + ", ".join(sorted(set(e['response']['status'] for e in r['entry']))))
    step(3, "read-patient", "GET", "/Patient/demo-patient-1",
         summarize=lambda r, h: f"{r['name'][0]['given'][0]} {r['name'][0]['family']}, version {r['meta']['versionId']}")
    step(4, "vread-patient", "GET", "/Patient/demo-patient-1/_history/1", summarize=lambda r, h: f"versionId {r['meta']['versionId']}")
    step(5, "search-patient-name", "GET", "/Patient?family=Shaw&given=Amy", summarize=lambda r, h: f"total {r.get('total')}")
    step(6, "search-patient-identifier", "GET", "/Patient?identifier=urn:oid:1.2.36.146.595.217.0.1|MRN-000001", summarize=lambda r, h: f"total {r.get('total')}")
    step(7, "search-encounter-by-patient", "GET", "/Encounter?patient=demo-patient-1&_include=Encounter:practitioner",
         summarize=lambda r, h: f"{len(r.get('entry', []))} entries (incl. _include)")
    step(8, "search-condition-code", "GET", "/Condition?patient=demo-patient-1&code=http://snomed.info/sct|840539006",
         summarize=lambda r, h: f"total {r.get('total')} (COVID-19 trigger code)")
    step(9, "search-observation-vitals", "GET", "/Observation?patient=demo-patient-1&category=vital-signs&_sort=-date",
         summarize=lambda r, h: f"total {r.get('total')}")
    step(10, "search-observation-lab-loinc", "GET", "/Observation?patient=demo-patient-1&code=http://loinc.org|94500-6",
         summarize=lambda r, h: f"total {r.get('total')}")
    step(11, "search-revinclude", "GET", "/Patient?_id=demo-patient-1&_revinclude=*",
         summarize=lambda r, h: f"{len(r.get('entry', []))} resources linked to the patient")
    step(12, "everything", "GET", "/Patient/demo-patient-1/$everything",
         summarize=lambda r, h: "types: " + ", ".join(f"{k}={v}" for k, v in sorted(__import__('collections').Counter(e['resource']['resourceType'] for e in r.get('entry', [])).items())))
    # create / update / history / delete on a scratch patient
    scratch = {"resourceType": "Patient", "meta": {"profile": ["http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient"]},
               "identifier": [{"system": "urn:oid:1.2.36.146.595.217.0.1", "value": "MRN-TEMP-001"}],
               "name": [{"family": "Integration", "given": ["Test"]}], "gender": "male", "birthDate": "1990-01-01"}
    created, hdrs = step(13, "create-patient", "POST", "/Patient", scratch, summarize=lambda r, h: f"id {r['id']} version {r['meta']['versionId']}")
    pid = created["id"]
    created["name"][0]["given"] = ["Test", "Updated"]
    step(14, "update-patient", "PUT", f"/Patient/{pid}", created, summarize=lambda r, h: f"version {r['meta']['versionId']}")
    step(15, "patch-patient", "PATCH", f"/Patient/{pid}", [{"op": "replace", "path": "/gender", "value": "other"}],
         headers={"Content-Type": "application/json-patch+json"}, summarize=lambda r, h: f"gender {r.get('gender')} version {r['meta']['versionId']}")
    step(16, "history-patient", "GET", f"/Patient/{pid}/_history", summarize=lambda r, h: f"{r.get('total')} versions")
    step(17, "validate-patient", "POST", "/Patient/$validate?profile=http://hl7.org/fhir/us/core/StructureDefinition/us-core-patient", created,
         summarize=lambda r, h: "issues: " + ", ".join(sorted(set(i['severity'] for i in r.get('issue', [])))))
    bad = {"resourceType": "Patient", "gender": "not-a-code", "birthDate": "yesterday"}
    step(18, "validate-invalid-patient", "POST", "/Patient/$validate", bad,
         summarize=lambda r, h: f"{sum(1 for i in r.get('issue', []) if i['severity'] in ('error','fatal'))} errors reported")
    step(19, "delete-patient", "DELETE", f"/Patient/{pid}", summarize=lambda r, h: "deleted")
    step(20, "read-deleted-patient", "GET", f"/Patient/{pid}", summarize=lambda r, h: "410 Gone expected")
    step(21, "capability-search-params", "GET", "/metadata?_elements=rest.resource.type,rest.resource.searchParam",
         summarize=lambda r, h: f"Patient search params: {len([sp for res in r['rest'][0]['resource'] if res['type']=='Patient' for sp in res.get('searchParam', [])])}")
    step(22, "bulk-export-kickoff", "GET", "/Group/uds-plus-reporting-group/$export?_type=Patient,Encounter,Condition&_outputFormat=application/fhir%2Bndjson",
         headers={"Prefer": "respond-async"}, summarize=lambda r, h: f"202 accepted, poll {h.get('Content-Location', h.get('content-location', ''))}")

    md = ["# FHIR API demonstration against HAPI FHIR R4", "", f"Base URL: `{a.base}`  — generated {time.strftime('%Y-%m-%d %H:%M:%S')}", "",
          "| # | Request | HTTP | ms | Result | File |", "| --- | --- | --- | --- | --- | --- |"]
    for n, req, code, ms, summary, fn in rows:
        md.append(f"| {n} | `{req}` | {code} | {ms} | {summary} | `{fn}` |")
    with open(os.path.join(a.out, "README.md"), "w", encoding="utf-8") as f:
        f.write("\n".join(md) + "\n")
    print(f"\nwrote {os.path.join(a.out, 'README.md')}")

if __name__ == "__main__":
    main()
