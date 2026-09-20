#!/usr/bin/env python3
"""Bounded HTTP contract smoke checks; uses only Python's standard library and loopback."""
import copy
from datetime import datetime, timezone
import hashlib
import http.client
import json
import os
from pathlib import Path
import socket
import subprocess
import time
import urllib.error
import urllib.request

HERE = Path(__file__).resolve().parent
REPO = HERE.parent.parent
WORK = REPO.parent.parent / "work" / "reins-server-check-v2"
WORK.mkdir(parents=True, exist_ok=True)
base = json.loads((REPO / "Contracts/Reins/preview-request.v2.json").read_text())
fingerprint_source = bytearray()
for name in ("ReinsContracts.cs", "ReinsCourseJudge.cs", "ReinsRun.cs", "ReinsReplay.cs", "ReinsAlley.cs", "../StandardCourse.cs"):
    fingerprint_source.extend(name.encode() + b"\0")
    fingerprint_source.extend((REPO / "Packages/com.barrelrivals.core/Runtime/Reins" / name).read_bytes())
expected_fingerprint = hashlib.sha256(fingerprint_source).hexdigest()
checks = []
suite_passed = False


def send(port, payload=None, raw=None, path="/lab/reins/verify", headers=None):
    data = raw if raw is not None else json.dumps(payload, separators=(",", ":")).encode() if payload is not None else None
    request = urllib.request.Request("http://127.0.0.1:%d%s" % (port, path), data=data,
                                     headers=headers or {"Content-Type": "application/json"})
    try:
        with urllib.request.urlopen(request, timeout=7) as response:
            return response.status, json.load(response)
    except urllib.error.HTTPError as error:
        return error.code, json.load(error)


def check(name, condition):
    checks.append({"name": name, "passed": bool(condition)})
    if not condition:
        raise AssertionError(name)


with socket.socket() as candidate:
    candidate.bind(("127.0.0.1", 0))
    port = candidate.getsockname()[1]
environment = os.environ.copy()
environment["BARREL_REINS_PORT"] = str(port)
# An external wildcard URL must not override the hard-coded loopback listener.
environment["ASPNETCORE_URLS"] = "http://0.0.0.0:5199"
process = None
with (WORK / "server.log").open("w") as output:
    try:
        process = subprocess.Popen(["bash", str(HERE / "run.sh"), "serve"], cwd=REPO, env=environment,
                                   stdout=output, stderr=subprocess.STDOUT)
        deadline = time.monotonic() + 15
        while True:
            if process.poll() is not None:
                raise RuntimeError("Local server exited; inspect work/reins-server-check-v2/server.log")
            try:
                status, health = send(port, path="/lab/reins/health")
                break
            except (urllib.error.URLError, ConnectionError):
                if time.monotonic() > deadline:
                    raise
                time.sleep(.1)
        check("health_is_offline_and_bounded", status == 200 and health["authority"] is False and health["maximumFrames"] == 7500 and health["contractVersion"] == 2 and health["courseId"] == "reins-v2")
        check("health_fingerprint_matches_canonical_rule_sources", health["ruleFingerprint"] == expected_fingerprint)
        status, first = send(port, base)
        check("incomplete_replay_cannot_be_accepted", status == 200 and first["accepted"] is False and first["status"] == "Approach" and first["result"] is None)
        check("exact_tick_count_recomputed", first["state"]["tick"] == 3 and first["frameCount"] == 3)
        check("response_fingerprint_matches_health_and_sources", first["ruleFingerprint"] == health["ruleFingerprint"] == expected_fingerprint)
        status, second = send(port, raw=json.dumps(base, indent=4, sort_keys=True).encode())
        check("canonical_hash_ignores_json_whitespace_and_key_order", status == 200 and first == second)
        check("canonical_digests_match_independent_python_serialization",
              first["manifestSha256"] == hashlib.sha256(json.dumps(base["manifest"], separators=(",", ":")).encode()).hexdigest()
              and first["replaySha256"] == hashlib.sha256(json.dumps(base, separators=(",", ":")).encode()).hexdigest())
        check("approach_has_fixed_pose_and_unresolved_launch", first["state"]["raceTimeMs"] == 0
              and abs(first["state"]["z"] + 5.91) < 1e-10 and first["state"]["x"] == 0
              and first["state"]["launchOutcome"] == "Pending" and first["state"]["launchReleaseErrorMs"] is None)
        def launch_frames(count, release_tick=None, repress=False):
            value = copy.deepcopy(base)
            value["frames"] = [dict(base["frames"][0], tick=i + 1,
                                    launchHeld=(release_tick is None or i + 1 < release_tick or (repress and i + 1 > release_tick)))
                               for i in range(count)]
            return value
        for tick, outcome, error in [(1,"Weak",-3980),(187,"Weak",-260),(188,"Good",-240),
                                      (193,"Good",-140),(194,"Perfect",-120),(200,"Perfect",0),
                                      (206,"Perfect",120),(207,"Good",140),(212,"Good",240),
                                      (213,"Weak",260),(220,"Weak",400)]:
            status, launch = send(port, launch_frames(max(200,tick),tick))
            check("launch_boundary_tick_%d" % tick, status == 200 and launch["state"]["launchOutcome"] == outcome
                  and launch["state"]["launchReleaseErrorMs"] == error)
        status, go = send(port, launch_frames(200,200))
        check("go_is_exact_start_pose_clock_and_speed", status == 200 and go["status"] == "Racing"
              and go["state"]["raceTimeMs"] == 0 and go["state"]["z"] == 0 and go["state"]["x"] == 0
              and go["state"]["speedMetresPerSecond"] == 1.5)
        status, timeout = send(port, launch_frames(220))
        check("held_launch_times_out_with_null_error", status == 200 and timeout["state"]["launchOutcome"] == "TimedOut"
              and timeout["state"]["launchReleaseErrorMs"] is None and timeout["status"] == "Racing")
        status, late = send(port, launch_frames(221,221))
        check("post_timeout_release_does_not_regrade", status == 200 and late["state"]["launchOutcome"] == "TimedOut"
              and late["state"]["launchReleaseErrorMs"] is None)
        status, repressed = send(port, launch_frames(220,1,True))
        check("release_and_repress_cannot_rearm", status == 200 and repressed["state"]["launchOutcome"] == "Weak"
              and repressed["state"]["launchReleaseErrorMs"] == -3980)
        status, released = send(port, launch_frames(3,1))
        check("launch_hold_changes_replay_hash_only", status == 200 and released["manifestSha256"] == first["manifestSha256"]
              and released["replaySha256"] != first["replaySha256"])
        (WORK / "preview-response.json").write_text(json.dumps(first, indent=2) + "\n")

        def reject(name, mutate, code=None):
            value = copy.deepcopy(base)
            mutate(value)
            status, response = send(port, value)
            check(name, status == 400 and not response["accepted"] and response["ruleFingerprint"] == expected_fingerprint
                  and (code is None or response["error"]["code"] == code))

        status, legacy = send(port, json.loads((REPO / "Contracts/Reins/preview-request.v1.json").read_text()))
        check("v1_envelope_explicitly_rejected", status == 400 and legacy["error"]["code"] == "contract_version")
        reject("mixed_v1_rules", lambda v: v["manifest"].update(rulesVersion=1), "rules_version")
        reject("mixed_v1_course", lambda v: v["manifest"].update(courseId="reins-lab-v1"), "course_version")
        reject("missing_course", lambda v: v["manifest"].pop("courseId"), "missing_field")
        reject("wrong_fingerprint", lambda v: v.update(ruleFingerprint="f" * 64), "rule_fingerprint")
        reject("malformed_fingerprint", lambda v: v.update(ruleFingerprint=12), "rule_fingerprint")
        reject("missing_fingerprint", lambda v: v.pop("ruleFingerprint"), "missing_field")
        reject("unarmed_launch_rejected", lambda v: v.update(launchInitiallyHeld=False), "launch_initial_state")
        reject("numeric_initial_state_rejected", lambda v: v.update(launchInitiallyHeld=1), "boolean_required")
        reject("missing_initial_state", lambda v: v.pop("launchInitiallyHeld"), "missing_field")
        reject("mixed_gate_tap_rejected", lambda v: v["frames"][0].update(gateTap=False), "unknown_or_duplicate_field")
        reject("unknown_ruleset", lambda v: v["manifest"].update(rulesVersion=999), "rules_version")
        reject("string_ruleset", lambda v: v["manifest"].update(rulesVersion="2"), "rules_version")
        reject("unknown_contract", lambda v: v.update(contractVersion=99))
        reject("negative_seed", lambda v: v["manifest"].update(seed=-1))
        reject("seed_overflow", lambda v: v["manifest"].update(seed=4294967296))
        reject("invalid_surface", lambda v: v["manifest"].update(surface="Unknown"), "enum_value")
        reject("numeric_enum_name", lambda v: v["manifest"].update(surface="0"), "enum_value")
        reject("invalid_round", lambda v: v["manifest"].update(roundIndex=3))
        reject("invalid_horse_stat", lambda v: v["manifest"]["horse"].update(nervePermille=1001))
        reject("reins_out_of_range", lambda v: v["frames"][0].update(leftPermille=1001))
        reject("fractional_rein", lambda v: v["frames"][0].update(leftPermille=.5))
        reject("decimal_integer_token", lambda v: v["frames"][0].update(leftPermille=0.0))
        reject("numeric_launch_state", lambda v: v["frames"][0].update(launchHeld=1), "boolean_required")
        reject("numeric_boolean", lambda v: v["frames"][0].update(wrap=1))
        reject("invalid_drive", lambda v: v["frames"][0].update(drive="Left,Right"), "enum_value")
        reject("tick_gap", lambda v: v["frames"][1].update(tick=3), "frame_order")
        reject("tick_repeat", lambda v: v["frames"][1].update(tick=1), "frame_order")
        reject("missing_field", lambda v: v["frames"][0].pop("launchHeld"), "missing_field")
        reject("uploaded_final_time_rejected", lambda v: v.update(finalTimeMs=1), "unknown_or_duplicate_field")
        reject("uploaded_position_rejected", lambda v: v["frames"][0].update(x=0), "unknown_or_duplicate_field")
        reject("empty_frames", lambda v: v.update(frames=[]), "frame_limit")
        reject("too_many_frames", lambda v: v.update(frames=[base["frames"][0]] * 7501), "frame_limit")
        raw = json.dumps(base).replace('"contractVersion": 2', '"contractVersion": 2, "contractVersion": 2')
        status, response = send(port, raw=raw.encode())
        check("duplicate_property", status == 400 and response["error"]["code"] == "unknown_or_duplicate_field")
        raw = json.dumps(base).replace('"leftPermille": 0', '"leftPermille": NaN', 1)
        status, response = send(port, raw=raw.encode())
        check("nan_rejected", status == 400 and response["error"]["code"] == "invalid_json")
        raw = json.dumps(base).replace('"leftPermille": 0', '"leftPermille": 1e309', 1)
        status, response = send(port, raw=raw.encode())
        check("overflow_number_rejected", status == 400)
        raw = json.dumps(base).replace('"leftPermille": 0', '"leftPermille": 0e0', 1)
        status, response = send(port, raw=raw.encode())
        check("exponent_integer_token_rejected", status == 400 and response["error"]["code"] == "integer_range")
        with socket.create_connection(("127.0.0.1", port), timeout=7) as connection:
            connection.sendall(("POST /lab/reins/verify HTTP/1.1\r\nHost: 127.0.0.1:%d\r\n"
                                "Content-Type: application/json\r\nContent-Length: 2097153\r\nConnection: close\r\n\r\n" % port).encode())
            response = http.client.HTTPResponse(connection)
            response.begin()
            document = json.loads(response.read())
            check("oversized_declared_body_rejected_before_upload", response.status == 413 and document["error"]["code"] == "body_limit")
        status, response = send(port, base, headers={"Content-Type": "application/json", "Content-Encoding": "gzip"})
        check("compressed_body_rejected", status == 415)
        status, response = send(port, base, headers={"Content-Type": "text/plain"})
        check("wrong_content_type", status == 415)
        status, response = send(port, base, headers={"Content-Type": "application/json", "Origin": "https://example.invalid"})
        check("browser_origin_rejected", status == 403)
        status, response = send(port, base, headers={"Content-Type": "application/json", "Host": "example.invalid"})
        check("foreign_host_rejected", status == 403)
        value = copy.deepcopy(base)
        value["frames"] = [dict(base["frames"][0], tick=i + 1) for i in range(7500)]
        status, timed_out = send(port, value)
        check("maximum_length_incomplete_run_times_out_without_result", status == 200 and timed_out["status"] == "TimedOut"
              and timed_out["state"]["tick"] == 7500 and timed_out["accepted"] is False and timed_out["result"] is None)

        # Occupy both request-body slots without sending body bytes, then verify the cap and deadline.
        held = []
        try:
            for _ in range(2):
                connection = socket.create_connection(("127.0.0.1", port), timeout=7)
                held.append(connection)
                connection.sendall(("POST /lab/reins/verify HTTP/1.1\r\nHost: 127.0.0.1:%d\r\n"
                                    "Content-Type: application/json\r\nContent-Length: 1\r\nConnection: close\r\n\r\n" % port).encode())
            cap_deadline = time.monotonic() + 1
            while True:
                status, response = send(port, base)
                if status == 429 or time.monotonic() >= cap_deadline:
                    break
                time.sleep(.02)
            check("two_verification_slots_are_enforced", status == 429 and response["error"]["code"] == "busy")
            for index, connection in enumerate(held):
                response = http.client.HTTPResponse(connection)
                response.begin()
                document = json.loads(response.read())
                check("slow_body_deadline_%d" % (index + 1), response.status == 408 and document["error"]["code"] == "deadline")
        finally:
            for connection in held:
                connection.close()
        status, response = send(port, base)
        check("verification_slots_recovered_after_deadline", status == 200 and response["status"] == "Approach")

        complete_path = REPO / "Contracts/Reins/complete-request.v2.json"
        if complete_path.exists():
            value = json.loads(complete_path.read_text())
            status, complete = send(port, value)
            check("complete_replay_recomputes_result", status == 200 and complete["accepted"] is True and complete["authority"] is False
                  and complete["result"]["finalTimeMs"] == complete["result"]["raceTimeMs"] + 5000 * complete["result"]["knockCount"])
            check("complete_launch_result_matches_state", complete["result"]["launchOutcome"] == complete["state"]["launchOutcome"] == "Perfect"
                  and complete["result"]["launchReleaseErrorMs"] == complete["state"]["launchReleaseErrorMs"] == 0)
            expected = json.loads((REPO / "Contracts/Reins/complete-response.v2.json").read_text())
            check("complete_fixture_matches_reviewed_expected_response", complete == expected)
            check("complete_digest_matches_python_serialization", complete["replaySha256"]
                  == hashlib.sha256(json.dumps(value, separators=(",", ":")).encode()).hexdigest())
            status, repeated = send(port, value)
            check("complete_replay_is_repeatable", status == 200 and complete == repeated)
            value["frames"].append(dict(value["frames"][-1], tick=len(value["frames"]) + 1))
            status, response = send(port, value)
            check("post_terminal_frame_rejected", status == 400 and response["error"]["code"] == "post_terminal_input")
            (WORK / "complete-response.json").write_text(json.dumps(complete, indent=2) + "\n")
            historical = list((REPO / "Contracts/Reins").glob("*.v1.*")) + [HERE / "verification-evidence.json"]
            check("all_v1_contract_and_verifier_evidence_bytes_preserved", all(
                path.read_bytes() == subprocess.check_output(["git", "show", "HEAD:" + str(path.relative_to(REPO))], cwd=REPO)
                for path in historical))
            suite_passed = True
        else:
            raise AssertionError("Complete fixture is required before this smoke suite can pass")
    finally:
        if process is not None and process.poll() is None:
            process.terminate()
            try:
                process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait(timeout=5)
        (WORK / "smoke-results.json").write_text(json.dumps({"scope": "offline-consistency", "suitePassed": suite_passed, "checks": checks}, indent=2) + "\n")
if suite_passed:
    evidence = {"scope":"offline-consistency", "contractVersion":2, "rulesVersion":2,
                "ruleFingerprint":expected_fingerprint, "verifiedAtUtc":datetime.now(timezone.utc).isoformat(),
                "suitePassed":True, "checks":checks,
                "limits":["Local consistency only; no authentication, ranked authority, storage or rewards.",
                          "Unity/IL2CPP parity and physical device acceptance require separate evidence."]}
    (HERE / "verification-evidence.v2.json").write_text(json.dumps(evidence, indent=2) + "\n")
print("Reins loopback HTTP smoke: %d/%d passed. Evidence: %s" % (sum(c["passed"] for c in checks), len(checks), WORK))
