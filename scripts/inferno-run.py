#!/usr/bin/env python
"""
Headless Inferno runner using the Inferno JSON API (no browser needed).

Creates a test session, starts a run for a suite or test group with the given inputs, waits for
completion and writes two artefacts:
  <out>.json  – raw results from GET /api/test_sessions/{id}/results
  <out>.md    – human readable summary (pass/fail/skip/omit counts + per-test table)

Examples
  # US Core v6.1.0 "US Core FHIR API" group against local HAPI (no auth)
  python inferno-run.py --inferno http://localhost:4567 --suite us_core_v610 \
      --group us_core_v610-us_core_v610_fhir_api \
      --input url=http://localhost:8080/fhir --input patient_ids=demo-patient-1 \
      --input 'smart_auth_info={"auth_type":"public"}' \
      --out ../results/inferno-us-core-v610

  # UDS+ kit, manifest URL produced by the .NET submitter
  python inferno-run.py --inferno http://localhost:4568 --suite uds_plus \
      --group uds_plus-uds_plus_test_group \
      --input import_manifest=http://localhost:5080/udsplus/files/<run>/manifest.json \
      --out ../results/inferno-uds-plus
"""
import argparse, json, sys, time, urllib.request, urllib.error
from collections import Counter

def api(base, method, path, body=None):
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(base + path, data=data, method=method,
                                 headers={"Content-Type": "application/json", "Accept": "application/json"})
    try:
        with urllib.request.urlopen(req, timeout=120) as r:
            raw = r.read().decode()
            return r.status, (json.loads(raw) if raw.strip() else None)
    except urllib.error.HTTPError as e:
        raw = e.read().decode()
        try:
            return e.code, json.loads(raw)
        except json.JSONDecodeError:
            return e.code, raw

def index_tests(suite):
    """Map test id -> (group title, test title) and group id -> title."""
    tests, groups = {}, {}
    def walk(g, trail):
        groups[g["id"]] = " / ".join(trail + [g.get("title") or g["id"]])
        for t in g.get("tests", []):
            tests[t["id"]] = (groups[g["id"]], t.get("title") or t["id"])
        for s in g.get("test_groups", []):
            walk(s, trail + [g.get("title") or g["id"]])
    for g in suite.get("test_groups", []):
        walk(g, [])
    return tests, groups

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--inferno", default="http://localhost:4567")
    ap.add_argument("--suite", required=True)
    ap.add_argument("--group", help="test group id to run (default: whole suite)")
    ap.add_argument("--suite-option", action="append", default=[], help="id=value")
    ap.add_argument("--input", action="append", default=[], help="name=value (value may be JSON)")
    ap.add_argument("--out", required=True, help="output path prefix (without extension)")
    ap.add_argument("--timeout", type=int, default=3600)
    a = ap.parse_args()

    base = a.inferno.rstrip("/")
    code, suite = api(base, "GET", f"/api/test_suites/{a.suite}")
    if code != 200:
        sys.exit(f"suite {a.suite} not found at {base}: {code} {suite}")
    tests_idx, groups_idx = index_tests(suite)

    opts = [{"id": k, "value": v} for k, v in (s.split("=", 1) for s in a.suite_option)]
    code, session = api(base, "POST", f"/api/test_sessions?test_suite_id={a.suite}", {"suite_options": opts} if opts else {})
    if code not in (200, 201):
        sys.exit(f"could not create session: {code} {session}")
    sid = session["id"]
    print(f"session {sid}  ->  {base}/test_sessions/{sid}")

    inputs = []
    for item in a.input:
        name, value = item.split("=", 1)
        inputs.append({"name": name, "value": value})
    run_body = {"test_session_id": sid, "inputs": inputs}
    if a.group:
        run_body["test_group_id"] = a.group
    else:
        run_body["test_suite_id"] = a.suite
    code, run = api(base, "POST", "/api/test_runs", run_body)
    if code not in (200, 201):
        sys.exit(f"could not start run: {code} {json.dumps(run)[:800]}")
    rid = run["id"]
    print(f"test run {rid} started ({a.group or a.suite}); waiting ...")

    t0 = time.time()
    while time.time() - t0 < a.timeout:
        code, run = api(base, "GET", f"/api/test_runs/{rid}?include_results=false")
        status = run.get("status") if isinstance(run, dict) else None
        if status in ("done", "cancelling", "cancelled", "error"):
            break
        time.sleep(5)
    print(f"run status: {status} after {int(time.time()-t0)}s")

    code, results = api(base, "GET", f"/api/test_sessions/{sid}/results")
    with open(a.out + ".json", "w", encoding="utf-8") as f:
        json.dump({"inferno": base, "suite": a.suite, "group": a.group, "session": sid, "run": rid,
                   "inputs": inputs, "results": results}, f, indent=1)

    test_results = [r for r in results if r.get("test_id")]
    counts = Counter(r["result"] for r in test_results)
    lines = [f"# Inferno results — {suite.get('title')} ({a.suite})", "",
             f"- Inferno: {base}  (session [{sid}]({base}/test_sessions/{sid}))",
             f"- Scope: `{a.group or a.suite}`",
             f"- Inputs: " + ", ".join(f"`{i['name']}`=`{i['value'][:80]}`" for i in inputs),
             f"- Run status: {status}, duration {int(time.time()-t0)} s", "",
             "| Result | Count |", "| --- | --- |"]
    for k in ("pass", "fail", "skip", "omit", "error", "wait", "cancel"):
        if counts.get(k):
            lines.append(f"| {k} | {counts[k]} |")
    lines += ["", "## Per-test results", "", "| Group | Test | Result | Message |", "| --- | --- | --- | --- |"]
    for r in sorted(test_results, key=lambda x: (tests_idx.get(x["test_id"], ("~", ""))[0], x["test_id"])):
        grp, title = tests_idx.get(r["test_id"], ("?", r["test_id"]))
        msg = (r.get("result_message") or "").replace("\n", " ").replace("|", "\\|")[:180]
        lines.append(f"| {grp} | {title} | **{r['result']}** | {msg} |")
    with open(a.out + ".md", "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    print(dict(counts))
    print(f"wrote {a.out}.json and {a.out}.md")

if __name__ == "__main__":
    main()
