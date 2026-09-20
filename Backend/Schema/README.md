# Reins persistence draft — not applied

`DRAFT_0001_reins.sql` is a schema design for later backend work. **No PostgreSQL server was installed or connected, and this SQL has not been executed or validated against a database.** It is wrapped in `BEGIN`/`ROLLBACK`, creates only a dedicated draft schema, and supplies no production writer or role grants. It must not be placed in an automatic migration pipeline.

The design records immutable ruleset/core digests, private match/round manifests, course and footing digests, frozen horse/loadout snapshots, uniquely identified run attempts, bounded replay objects, and append-only verification outcomes. Composite foreign keys tie a run to its exact match/round/participant snapshot and tie match evidence to the corresponding run. Mutation guards require new versioned records instead of editing frozen history.

The v2 compatibility draft preserves immutable v1 history and adds contract-version foreign keys through ruleset → manifest → attempt → replay. Manifests explicitly separate `reins-lab-v1` and `reins-v2`; replay objects require `launch_initially_held = true` only for v2 and null for historical v1. V2 result JSON records `launchOutcome` and a signed nullable `launchReleaseErrorMs` (timeout has null, never fabricated zero). These columns do not migrate or regrade old inputs. A future trusted verifier must compare the exact course, rules/source fingerprint and canonical launch/input envelope before accepting playback; digests are compatibility data, not authorization. The current local endpoint rejects v1 verification entirely.

Future settlement is unique per verified match and operation ID. Earned bonus awards are unique per settlement, participant and award kind. Post-match horse changes are unique per settlement/horse and per horse revision. There is no client wallet table, premium insurance, purchasable stake or progression endpoint. Bond and stamina values are future post-match snapshots, not client-controlled live writes.

These constraints alone are insufficient to authorize a result or grant a reward. A future trusted service must verify identities, manifest issuance, required paired rounds, match format, retry/disconnect policy and all run decisions before it can produce a verified match. The local Reins replay endpoint cannot do that and has no database access.

Before promotion to a migration, select the PostgreSQL version; validate DDL in an isolated instance; add restrictive writer roles and retention policy; implement settlement with appropriate row locks or serializable transactions; test duplicate operation IDs, conflicting payloads, concurrent match completion, expected horse revisions, rollback and whole-transaction retry. Balance/ledger reconciliation and backup/restore tests remain separate requirements. An existing receipt must be returned for an identical retry; a conflicting retry must fail.

The ten-mechanic attachment is design input. Its proposed cash-out multipliers and paid streak insurance are not enabled by this draft. Final earned progression semantics require the current design decision before implementing transaction writers.

References: [PostgreSQL constraints](https://www.postgresql.org/docs/current/ddl-constraints.html) and [transaction isolation](https://www.postgresql.org/docs/current/transaction-iso.html).
