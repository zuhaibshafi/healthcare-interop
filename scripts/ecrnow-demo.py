#!/usr/bin/env python
"""
eCRNow <-> HAPI FHIR integration demo driver (no external dependencies, Python 3.10+).

What it does (idempotent, safe to re-run):
  1. Checks HAPI FHIR and eCRNow are reachable.
  2. Registers the local HAPI server as an eCRNow "Healthcare Setting" (the EHR FHIR endpoint).
  3. Lists the Knowledge Artifact Repositories / KARs eCRNow loaded from kar.directory (the eRSD PlanDefinition).
  4. Activates the eRSD KAR for the Healthcare Setting (FHIR eICR output).
  5. Launches patient processing for demo-patient-1 / demo-encounter-1 (the COVID-19 encounter).
  6. Polls eCRNow for the resulting eICR and prints a summary.

Usage:  python ecrnow-demo.py [--hapi http://localhost:8080/fhir] [--ecrnow http://localhost:8081]
                               [--patient demo-patient-1] [--encounter demo-encounter-1]
                               [--output-format FHIR|CDA_R11|CDA_R31] [--auth none|system]
"""
import argparse, json, sys, time, urllib.request, urllib.error

def http(method, url, body=None, headers=None, timeout=60):
    data = None
    hdrs = {"Accept": "application/json"}
    if body is not None:
        data = json.dumps(body).encode()
        hdrs["Content-Type"] = "application/json"
    if headers:
        hdrs.update(headers)
    req = urllib.request.Request(url, data=data, method=method, headers=hdrs)
    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            raw = resp.read().decode("utf-8", "replace")
            try:
                return resp.status, json.loads(raw) if raw.strip() else None
            except json.JSONDecodeError:
                return resp.status, raw
    except urllib.error.HTTPError as e:
        raw = e.read().decode("utf-8", "replace")
        try:
            return e.code, json.loads(raw)
        except json.JSONDecodeError:
            return e.code, raw

def step(msg):
    print(f"\n=== {msg}")

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--hapi", default="http://localhost:8080/fhir")
    ap.add_argument("--ecrnow", default="http://localhost:8081")
    ap.add_argument("--dotnet", default="http://localhost:5080", help=".NET integration layer (OAuth facade + PHA receiver)")
    ap.add_argument("--patient", default="demo-patient-1")
    ap.add_argument("--encounter", default="demo-encounter-1")
    ap.add_argument("--output-format", default="FHIR", choices=["FHIR", "CDA_R11", "CDA_R31", "BOTH"])
    ap.add_argument("--auth", default="none", choices=["none", "system"],
                    help="none = eCRNow queries HAPI without a token; system = SMART Backend-style client_credentials via the .NET /oauth/token facade")
    ap.add_argument("--wait", type=int, default=240, help="seconds to wait for the eICR to be produced")
    a = ap.parse_args()

    step("1. Connectivity")
    code, meta = http("GET", f"{a.hapi}/metadata")
    if code != 200:
        sys.exit(f"HAPI not reachable at {a.hapi} (HTTP {code})")
    print(f"HAPI FHIR OK: {meta['software']['name']} {meta['software']['version']} fhirVersion={meta['fhirVersion']}")
    code, hs_list = http("GET", f"{a.ecrnow}/api/healthcareSettings/")
    if code != 200:
        sys.exit(f"eCRNow not reachable at {a.ecrnow}/api/healthcareSettings/ (HTTP {code}): {hs_list}")
    print(f"eCRNow OK: {len(hs_list)} healthcare setting(s) registered")

    step("2. Register HAPI as an eCRNow Healthcare Setting")
    existing = next((h for h in hs_list if h.get("fhirServerBaseURL") == a.hapi), None)
    if existing:
        hs = existing
        print(f"Already registered as hsId={hs['id']}")
    else:
        payload = {
            "clientId": "ecrnow-local",
            "clientSecret": "ecrnow-secret",
            "fhirServerBaseURL": a.hapi,
            "fhirVersion": "R4",
            # tokenUrl must be non-null: eCRNow tries to discover it from CapabilityStatement.rest.security
            # when missing and NPEs on an open server (see troubleshooting notes).
            "tokenUrl": f"{a.dotnet}/oauth/token",
            "scopes": "system/*.read",
            "authType": "System" if a.auth == "system" else "None",
            "requireAud": False,
            "isDirect": False, "isXdr": False, "isRestAPI": True, "fhirAPI": False,
            "restApiUrl": f"{a.dotnet}/pha/r4/fhir/$process-message",
            "assigningAuthorityId": "2.16.840.1.113883.4.6",
            "orgName": "Greenway Community Health Center",
            "orgIdSystem": "http://hl7.org/fhir/sid/us-npi", "orgId": "1234567893",
            "encounterStartThreshold": "1", "encounterEndThreshold": "1",
            "ehrSupportsSubscriptions": False,
            "offhoursEnabled": False,
            "debugEnabled": True,
            "createDocRefForResponse": False,
            "handOffResponseToRestApi": "false",
            "directTlsVersion": "TLSv1.2",
        }
        code, hs = http("POST", f"{a.ecrnow}/api/healthcareSettings", payload)
        if code != 200:
            sys.exit(f"Failed to create healthcare setting: HTTP {code} {hs}")
        print(f"Created hsId={hs['id']} for {hs['fhirServerBaseURL']} (authType={hs['authType']})")
    hs_id = hs["id"]

    step("3. Knowledge Artifact Repositories loaded by eCRNow (kar.directory)")
    code, repos = http("GET", f"{a.ecrnow}/api/kars/")
    if code != 200 or not repos:
        print("No KAR repositories reported, asking eCRNow to (re)load from kar.directory ...")
        http("POST", f"{a.ecrnow}/api/loadKars")
        code, repos = http("GET", f"{a.ecrnow}/api/kars/")
    kars = []
    for r in repos or []:
        for k in r.get("karsInfo", []):
            kars.append(k)
            print(f"repo={r.get('repoName')} url={r.get('fhirServerURL')} -> karId={k.get('karId')} version={k.get('karVersion')} name={k.get('karName')}")
    ersd = next((k for k in kars if "ersd" in (k.get("karId") or "").lower()), kars[0] if kars else None)
    if not ersd:
        sys.exit("No knowledge artifact found. Check kar.directory in application-local.properties and the eCRNow log.")

    step("4. Activate the eRSD KAR for this Healthcare Setting")
    status = [{
        "hsId": hs_id,
        "karId": ersd["karId"],
        "karVersion": ersd["karVersion"],
        "versionUniqueKarId": f"{ersd['karId']}|{ersd['karVersion']}",
        "isActive": True,
        "subscriptionsEnabled": False,
        "covidOnly": False,
        "outputFormat": a.output_format,
    }]
    code, body = http("POST", f"{a.ecrnow}/api/addKARStatus/", status)
    print(f"addKARStatus -> HTTP {code}: {json.dumps(body)[:300]}")
    code, body = http("GET", f"{a.ecrnow}/api/karStatusByHsId?hsId={hs_id}")
    print(f"active KARs for hsId={hs_id}: {[ (s.get('karId'), s.get('karVersion'), s.get('isActive'), s.get('outputFormat')) for s in (body or [])]}")

    step("4b. Register the Public Health Agency receiver (the .NET mock) so submit-report can look it up")
    pha_url = f"{a.dotnet}/pha/r4/fhir/$process-message"
    code, phas = http("GET", f"{a.ecrnow}/api/publicHealthAuthority/")
    if code == 200 and any(p.get("fhirServerBaseURL") == pha_url for p in (phas or [])):
        print(f"PHA already registered: {pha_url}")
    else:
        code, body = http("POST", f"{a.ecrnow}/api/publicHealthAuthority", {
            "fhirServerBaseURL": pha_url, "fhirVersion": "R4", "clientId": "ecrnow-local", "clientSecret": "ecrnow-secret",
            "tokenUrl": f"{a.dotnet}/oauth/token", "scopes": "system/*.write", "authType": "None", "requireAud": False})
        print(f"register PHA -> HTTP {code}: {json.dumps(body)[:200]}")
    print("NOTE: report-submission.endpoint in application-local.properties must be the PHA *base* URL "
          "(http://localhost:5080/pha/r4/fhir); eCRNow's FHIR client appends /$process-message itself.")

    step("5. Launch patient processing (EHR -> eCRNow 'launchPatient')")
    code, ctxs = http("GET", f"{a.ecrnow}/api/notificationContexts?fhirServerBaseUrl={a.hapi}&patientId={a.patient}")
    stale = [c for c in (ctxs or []) if c.get("notificationResourceId") == a.encounter]
    if stale:
        print(f"{len(stale)} existing notification context(s) for this encounter -> deleting so the launch is not rejected "
              "(eCRNow enforces a unique context per server/patient/encounter/trigger)")
        for c in stale:
            # DELETE /api/notificationContext takes a JSON body (NotificationContextData)
            http("DELETE", f"{a.ecrnow}/api/notificationContext",
                 {"id": c.get("id"), "fhirServerBaseUrl": a.hapi, "patientId": a.patient, "notificationResourceId": a.encounter})
    launch = {"fhirServerURL": a.hapi, "patientId": a.patient, "encounterId": a.encounter, "validationMode": False}
    code, body = http("POST", f"{a.ecrnow}/api/launchPatient", launch, headers={"X-Request-ID": f"demo-{int(time.time())}"})
    print(f"launchPatient -> HTTP {code}: {str(body)[:400]}")

    step("6. Wait for the eRSD workflow (encounter-start -> 3 min timer -> check-trigger-codes -> create-report -> validate -> submit)")
    code, before = http("GET", f"{a.dotnet}/pha/received")
    before_n = len(before) if isinstance(before, list) else 0
    deadline = time.time() + a.wait
    received = None
    while time.time() < deadline:
        code, now = http("GET", f"{a.dotnet}/pha/received")
        if isinstance(now, list) and len(now) > before_n:
            received = now[0]
            break
        time.sleep(10)
        print("  ... workflow running (see C:/git/ecrnow-runtime/bsa-output for TriggerQueryBundle / create-report files)")
    code, ctxs = http("GET", f"{a.ecrnow}/api/notificationContexts?fhirServerBaseUrl={a.hapi}&patientId={a.patient}")
    print("eCRNow notification context(s):", [(c.get("triggerEvent"), c.get("notificationProcessingStatus")) for c in (ctxs or [])])
    if received:
        print(f"eICR received by the PHA mock: {received.get('fileName')} ({received.get('sizeBytes')} bytes) -> {received.get('url')}")
        code, bundle = http("GET", received["url"])
        if isinstance(bundle, dict):
            inner = [e["resource"] for e in bundle.get("entry", []) if e["resource"]["resourceType"] == "Bundle"]
            hdr = [e["resource"] for e in bundle.get("entry", []) if e["resource"]["resourceType"] == "MessageHeader"]
            print("  message type:", bundle.get("type"), "| event:", hdr[0].get("eventCoding", {}).get("code") if hdr else None)
            if inner:
                from collections import Counter
                print("  eICR document contents:", dict(Counter(e["resource"]["resourceType"] for e in inner[0].get("entry", []))))
    else:
        print("No eICR arrived within the wait window. Check logs/ecrNow.log and bsa-output; the timer may still be pending.")

    step("Done")
    print(f"Swagger UI for eCRNow:  {a.ecrnow}/swagger-ui/index.html")
    print(f"Debug output (eICR bundles, trigger evaluation): C:/git/ecrnow-runtime/bsa-output/")

if __name__ == "__main__":
    main()
