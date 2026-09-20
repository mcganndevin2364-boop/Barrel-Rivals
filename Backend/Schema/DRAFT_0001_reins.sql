-- UNAPPLIED DESIGN DRAFT. No database has been provisioned or migration executed.
-- This file intentionally ends in ROLLBACK. It is not a production migration.
-- The loopback ReinsServerCheck has no database credentials or writer and cannot create these rows.
-- Trusted issuance, authentication, roles, full match verification and transactional settlement
-- must be implemented and concurrency-tested before any real progression can be enabled.
BEGIN;
CREATE SCHEMA reins_draft;

CREATE FUNCTION reins_draft.reject_frozen_change() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
    RAISE EXCEPTION 'Frozen Reins records are append-only; issue a new version instead';
END;
$$;

CREATE TABLE reins_draft.ruleset (
    ruleset_id text PRIMARY KEY CHECK (ruleset_id ~ '^reins/[1-9][0-9]*$'),
    contract_version smallint NOT NULL CHECK (contract_version IN (1, 2)),
    core_source_sha256 bytea NOT NULL CHECK (octet_length(core_source_sha256) = 32),
    rules_sha256 bytea NOT NULL CHECK (octet_length(rules_sha256) = 32),
    fixed_step_ms smallint NOT NULL CHECK (fixed_step_ms = 20),
    maximum_frames integer NOT NULL CHECK (maximum_frames BETWEEN 1 AND 7500),
    created_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (ruleset_id, rules_sha256),
    UNIQUE (ruleset_id, rules_sha256, contract_version)
);

-- Full manifests are server-private in a future competitive service. Reveal permissions are separate.
CREATE TABLE reins_draft.manifest (
    manifest_id uuid PRIMARY KEY,
    match_id uuid NOT NULL,
    round_index smallint NOT NULL CHECK (round_index BETWEEN 0 AND 2),
    ruleset_id text NOT NULL,
    contract_version smallint NOT NULL CHECK (contract_version IN (1, 2)),
    rules_sha256 bytea NOT NULL CHECK (octet_length(rules_sha256) = 32),
    course_id text NOT NULL CHECK (length(course_id) BETWEEN 1 AND 96),
    course_sha256 bytea NOT NULL CHECK (octet_length(course_sha256) = 32),
    footing_plan_sha256 bytea NOT NULL CHECK (octet_length(footing_plan_sha256) = 32),
    loadouts_sha256 bytea NOT NULL CHECK (octet_length(loadouts_sha256) = 32),
    manifest_sha256 bytea NOT NULL UNIQUE CHECK (octet_length(manifest_sha256) = 32),
    canonical_manifest bytea NOT NULL CHECK (octet_length(canonical_manifest) BETWEEN 1 AND 65536),
    issued_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (ruleset_id, rules_sha256, contract_version)
        REFERENCES reins_draft.ruleset (ruleset_id, rules_sha256, contract_version),
    CHECK ((contract_version = 1 AND course_id = 'reins-lab-v1')
        OR (contract_version = 2 AND course_id = 'reins-v2')),
    UNIQUE (match_id, round_index),
    UNIQUE (manifest_id, match_id, round_index),
    UNIQUE (manifest_id, match_id, round_index, contract_version)
);

CREATE TABLE reins_draft.horse_snapshot (
    manifest_id uuid NOT NULL REFERENCES reins_draft.manifest,
    rider_id uuid NOT NULL,
    horse_id uuid NOT NULL,
    loadout_sha256 bytea NOT NULL CHECK (octet_length(loadout_sha256) = 32),
    nerve_permille smallint NOT NULL CHECK (nerve_permille BETWEEN 0 AND 1000),
    fire_permille smallint NOT NULL CHECK (fire_permille BETWEEN 0 AND 1000),
    biddability_permille smallint NOT NULL CHECK (biddability_permille BETWEEN 0 AND 1000),
    heart_permille smallint NOT NULL CHECK (heart_permille BETWEEN 0 AND 1000),
    bond_level smallint NOT NULL CHECK (bond_level BETWEEN 1 AND 20),
    stamina_permille smallint NOT NULL CHECK (stamina_permille BETWEEN 0 AND 1000),
    snapshot_json jsonb NOT NULL CHECK (jsonb_typeof(snapshot_json) = 'object' AND octet_length(snapshot_json::text) <= 16384),
    PRIMARY KEY (manifest_id, rider_id),
    UNIQUE (manifest_id, rider_id, loadout_sha256)
);

CREATE TABLE reins_draft.run_attempt (
    run_id uuid PRIMARY KEY,
    manifest_id uuid NOT NULL,
    contract_version smallint NOT NULL CHECK (contract_version IN (1, 2)),
    match_id uuid NOT NULL,
    round_index smallint NOT NULL CHECK (round_index BETWEEN 0 AND 2),
    rider_id uuid NOT NULL,
    attempt_id uuid NOT NULL,
    loadout_sha256 bytea NOT NULL CHECK (octet_length(loadout_sha256) = 32),
    assigned_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (manifest_id, match_id, round_index, contract_version)
        REFERENCES reins_draft.manifest (manifest_id, match_id, round_index, contract_version),
    FOREIGN KEY (manifest_id, rider_id, loadout_sha256) REFERENCES reins_draft.horse_snapshot (manifest_id, rider_id, loadout_sha256),
    UNIQUE (match_id, round_index, rider_id, attempt_id),
    UNIQUE (run_id, match_id),
    UNIQUE (run_id, contract_version)
);

-- The backend chooses object_key; it is never a URL to fetch from a client.
-- Keep v1 objects as immutable history. Contract/course/source hashes must match the
-- assigned attempt; a v1 object cannot become a v2 best or trusted match result.
CREATE TABLE reins_draft.replay_object (
    replay_id uuid PRIMARY KEY,
    run_id uuid NOT NULL,
    replay_sha256 bytea NOT NULL CHECK (octet_length(replay_sha256) = 32),
    object_sha256 bytea NOT NULL CHECK (octet_length(object_sha256) = 32),
    object_key text NOT NULL UNIQUE CHECK (length(object_key) BETWEEN 1 AND 512),
    byte_length integer NOT NULL CHECK (byte_length BETWEEN 1 AND 2097152),
    contract_version smallint NOT NULL CHECK (contract_version IN (1, 2)),
    launch_initially_held boolean,
    CHECK ((contract_version = 1 AND launch_initially_held IS NULL)
        OR (contract_version = 2 AND launch_initially_held IS TRUE)),
    frame_count integer NOT NULL CHECK (frame_count BETWEEN 1 AND 7500),
    duration_ms integer NOT NULL CHECK (duration_ms = frame_count * 20),
    stored_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (run_id, contract_version) REFERENCES reins_draft.run_attempt (run_id, contract_version),
    UNIQUE (run_id, replay_sha256),
    UNIQUE (replay_id, run_id)
);

CREATE TABLE reins_draft.run_verification (
    verification_id uuid PRIMARY KEY,
    replay_id uuid NOT NULL,
    run_id uuid NOT NULL,
    verifier_build_sha256 bytea NOT NULL CHECK (octet_length(verifier_build_sha256) = 32),
    result_sha256 bytea NOT NULL CHECK (octet_length(result_sha256) = 32),
    decision text NOT NULL CHECK (decision IN ('complete', 'incomplete', 'timed_out', 'rejected')),
    raw_time_ms integer CHECK (raw_time_ms BETWEEN 0 AND 150000),
    knock_count smallint CHECK (knock_count BETWEEN 0 AND 3),
    final_time_ms integer,
    -- V2 canonical result JSON retains launchOutcome and signed launchReleaseErrorMs;
    -- TimedOut uses JSON null, not an invented zero release error. The trusted verifier
    -- must validate these against replayed frames before insertion.
    result_json jsonb NOT NULL CHECK (jsonb_typeof(result_json) = 'object' AND octet_length(result_json::text) <= 16384),
    verified_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (replay_id, run_id) REFERENCES reins_draft.replay_object (replay_id, run_id),
    CHECK ((decision = 'complete' AND raw_time_ms IS NOT NULL AND knock_count IS NOT NULL
              AND final_time_ms IS NOT NULL AND final_time_ms = raw_time_ms + knock_count * 5000)
        OR (decision <> 'complete' AND raw_time_ms IS NULL AND knock_count IS NULL AND final_time_ms IS NULL)),
    UNIQUE (replay_id, verifier_build_sha256),
    UNIQUE (verification_id, run_id)
);

-- Future match verification must check participant identities, all required paired rounds,
-- retry/disconnect policy, compatible private manifests and the chosen match format.
-- A local replay response is insufficient evidence for inserting this row.
CREATE TABLE reins_draft.verified_match (
    match_id uuid PRIMARY KEY,
    outcome_sha256 bytea NOT NULL CHECK (octet_length(outcome_sha256) = 32),
    verifier_build_sha256 bytea NOT NULL CHECK (octet_length(verifier_build_sha256) = 32),
    verified_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (match_id, outcome_sha256)
);
CREATE TABLE reins_draft.match_run_evidence (
    match_id uuid NOT NULL REFERENCES reins_draft.verified_match,
    verification_id uuid NOT NULL,
    run_id uuid NOT NULL,
    PRIMARY KEY (match_id, verification_id),
    FOREIGN KEY (verification_id, run_id) REFERENCES reins_draft.run_verification (verification_id, run_id),
    FOREIGN KEY (run_id, match_id) REFERENCES reins_draft.run_attempt (run_id, match_id)
);

-- Each match is settled at most once. Corrections require a separately reviewed compensating ledger.
CREATE TABLE reins_draft.settlement (
    settlement_id uuid PRIMARY KEY,
    match_id uuid NOT NULL UNIQUE REFERENCES reins_draft.verified_match,
    operation_id uuid NOT NULL UNIQUE,
    outcome_sha256 bytea NOT NULL CHECK (octet_length(outcome_sha256) = 32),
    settled_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (match_id, outcome_sha256) REFERENCES reins_draft.verified_match (match_id, outcome_sha256)
);
-- No wallet/balance table, debit, cash stake, premium insurance or client grant is introduced.
-- This is a future earned bonus receipt; quantities and eligibility require server-owned rules.
CREATE TABLE reins_draft.streak_bonus_award (
    award_id uuid PRIMARY KEY,
    settlement_id uuid NOT NULL REFERENCES reins_draft.settlement,
    rider_id uuid NOT NULL,
    streak_id uuid NOT NULL,
    award_kind text NOT NULL CHECK (award_kind IN ('earned_base', 'earned_streak_bonus')),
    rule_version integer NOT NULL CHECK (rule_version > 0),
    quantity bigint NOT NULL CHECK (quantity BETWEEN 0 AND 1000000000),
    operation_id uuid NOT NULL UNIQUE,
    awarded_at timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UNIQUE (settlement_id, rider_id, award_kind)
);

-- Bond/stamina changes occur after match verification and settlement, never per client frame.
CREATE TABLE reins_draft.horse_post_match_change (
    settlement_id uuid NOT NULL REFERENCES reins_draft.settlement,
    horse_id uuid NOT NULL,
    owner_id uuid NOT NULL,
    prior_revision bigint NOT NULL CHECK (prior_revision >= 0),
    next_revision bigint NOT NULL CHECK (next_revision = prior_revision + 1),
    bond_after smallint NOT NULL CHECK (bond_after BETWEEN 1 AND 20),
    stamina_after smallint NOT NULL CHECK (stamina_after BETWEEN 0 AND 1000),
    PRIMARY KEY (settlement_id, horse_id),
    UNIQUE (horse_id, next_revision)
);

CREATE TRIGGER freeze_ruleset BEFORE UPDATE OR DELETE ON reins_draft.ruleset FOR EACH ROW EXECUTE FUNCTION reins_draft.reject_frozen_change();
CREATE TRIGGER freeze_manifest BEFORE UPDATE OR DELETE ON reins_draft.manifest FOR EACH ROW EXECUTE FUNCTION reins_draft.reject_frozen_change();
CREATE TRIGGER freeze_snapshot BEFORE UPDATE OR DELETE ON reins_draft.horse_snapshot FOR EACH ROW EXECUTE FUNCTION reins_draft.reject_frozen_change();
CREATE TRIGGER freeze_attempt BEFORE UPDATE OR DELETE ON reins_draft.run_attempt FOR EACH ROW EXECUTE FUNCTION reins_draft.reject_frozen_change();
CREATE TRIGGER freeze_replay BEFORE UPDATE OR DELETE ON reins_draft.replay_object FOR EACH ROW EXECUTE FUNCTION reins_draft.reject_frozen_change();
CREATE TRIGGER freeze_verification BEFORE UPDATE OR DELETE ON reins_draft.run_verification FOR EACH ROW EXECUTE FUNCTION reins_draft.reject_frozen_change();
CREATE TRIGGER freeze_match BEFORE UPDATE OR DELETE ON reins_draft.verified_match FOR EACH ROW EXECUTE FUNCTION reins_draft.reject_frozen_change();
CREATE TRIGGER freeze_match_evidence BEFORE UPDATE OR DELETE ON reins_draft.match_run_evidence FOR EACH ROW EXECUTE FUNCTION reins_draft.reject_frozen_change();
CREATE TRIGGER freeze_settlement BEFORE UPDATE OR DELETE ON reins_draft.settlement FOR EACH ROW EXECUTE FUNCTION reins_draft.reject_frozen_change();
CREATE TRIGGER freeze_bonus BEFORE UPDATE OR DELETE ON reins_draft.streak_bonus_award FOR EACH ROW EXECUTE FUNCTION reins_draft.reject_frozen_change();
CREATE TRIGGER freeze_horse_change BEFORE UPDATE OR DELETE ON reins_draft.horse_post_match_change FOR EACH ROW EXECUTE FUNCTION reins_draft.reject_frozen_change();

-- Future implementation proof: lock match + rider progression, verify expected revisions,
-- insert settlement/bonus/change receipts and update server-owned balances in ONE transaction.
-- Retry the entire transaction on serialization failure; repeated operation IDs return the
-- existing identical receipt and conflicting payloads fail. Test concurrent calls and rollback.
-- No such writer, role grant, wallet routine, persistence proof or production migration exists yet.
ROLLBACK;
