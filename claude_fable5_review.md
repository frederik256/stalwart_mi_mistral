# Codebase Review: Does it meet initspec.md / SPEC.md?

**Reviewer**: Claude Fable 5
**Date**: 2026-07-08
**Scope**: Full review of `src/`, `tests/`, and configs against the goals in `initspec.md` and `SPEC.md`.

## Verdict: No — the codebase will not meet the goals in its current state

It builds cleanly on .NET 8 and all 268 tests pass, which gives a misleading impression of
health. Underneath, the tool cannot perform any migration at all, and several of its
integrations target APIs that don't exist. Findings below, worst problems first.

---

## Blocker 1: Every CLI command is a stub

All six command handlers (`setup`, `migrate`, `export`, `import`, `validate`, `vandelay`)
just print "will be implemented in a future update" and return 0 — e.g.
`src/StalwartMigration/CLI/Commands/MigrateCommandHandler.cs:29`,
`src/StalwartMigration/CLI/Commands/ExportCommandHandler.cs:29`.

The spec says "migrate via the CLI application" — nothing is reachable from the CLI. The
`MigrationOrchestrator`, exporter, and importer classes exist but are never instantiated by
any command, and there is no config-file loading anywhere, so `--source-config` /
`--target-config` options are decorative.

## Blocker 2: The Stalwart client targets a fictional API

`src/StalwartMigration/Infrastructure/Stalwart/StalwartClient.cs` calls
`/api/v1/auth/login`, `/api/v1/domains`, `/api/v1/accounts`, `/api/v1/aliases`.

Checked against the actual OpenAPI spec that SPEC.md references
(stalwartlabs/stalwart `api/v1/openapi.yml`): the real management API exposes `/api/auth`,
`/api/account`, `/api/schema`, and token/live endpoints — and account/domain management in
Stalwart is done through `/api/principal` with Basic auth.

**Every request this client makes would 404 against a real Stalwart server**, so even if
the CLI were wired up, account/domain/alias creation (the tool's stated core value over
Vandelay) cannot work.

## Blocker 3: Email migration — the #1 functional goal — has no working path

- **Fallback path, extraction**: `HMailServerClient.GetMessagesAsync` reads only metadata
  (subject, from, to, date, size) — never the body or the raw message file. It also reads
  `account.Messages`, but in the hMailServer COM API messages live under
  `Account.IMAPFolders → Folder.Messages` (raw content via `Message.Filename`), so this
  would fail at runtime, and folder structure (Inbox/Sent/…) isn't modeled anywhere.
- **Fallback path, EML**: `HMailServerExporter.BuildEmlContent` fabricates a pseudo-EML
  from those metadata fields (it even writes a `Bcc:` header). MimeKit is referenced but
  never used. Attachments are written as the literal text `[Attachment Placeholder]`
  (`src/StalwartMigration/Core/Exporters/HMailServerExporter.cs:262`).
- **Fallback path, import**: `StalwartImporter` imports only domains/accounts/aliases —
  it never reads the EML files, and `IStalwartClient.ImportMessageAsync` throws
  `NotImplementedException` (`StalwartClient.cs:490`).
- **Vandelay path**: The real Vandelay CLI uses
  `vandelay import imap --url imaps://… --auth-basic user archive.sqlite` and
  `vandelay export --url … --account-name …`. The runner builds flags that don't exist
  (`--imap-host`, `--jmap-url`, `--account`, `--log-level`, `--skip-ssl-validation`), and
  the orchestrator invokes it as `RunAsync("import", [domainName])` — a bare domain name,
  no URL, no credentials, no archive path — then counts `MessagesProcessed += 1` per
  domain regardless of what actually moved
  (`src/StalwartMigration/Core/MigrationOrchestrator.cs:245`).

## Blocker 4: Resume/incremental migration isn't implemented

Checkpoints are *written* (per domain, not the spec's every-30-seconds), but nothing ever
*reads* them: `CheckpointService.LoadCheckpointAsync` has no production callers, and the
`--resume` / `--last-checkpoint` options dead-end in the stub migrate handler. A failed
migration cannot be resumed, and re-running would re-process everything — with no
duplicate-by-Message-ID handling, which the spec's conflict-resolution section requires.

## Blocker 5: The ZIP archives are nearly empty

The spec's architecture is "extract to zip files, import from them."
`ArchiveManager.CreateDomainArchiveAsync`
(`src/StalwartMigration/Infrastructure/FileSystem/ArchiveManager.cs:370`) puts a single
`metadata.json` containing `{ Domain = "name" }` into the zip — the exported account JSON,
alias JSON, EML files, and attachments on disk are never added, and the importer reads
loose files from a directory rather than the archives anyway.

---

## Other significant issues

- **hMailServer auth is wrong and never called**: `_server.Authenticate("Administrator", pw)`
  cast to `bool` (`HMailServerClient.cs:193`) — the real COM call is
  `Application.Authenticate(user, pw)` returning an Account object (or null). The
  orchestrator's comment says "we assume it's already connected"
  (`MigrationOrchestrator.cs:86`).
- **Passwords are extracted and forwarded** (`HMailServerClient.cs:526`, copied into
  `AccountRequest.Password`), directly contradicting the spec's "migrate accounts without
  passwords" and its security boundaries.
- **TLS validation is disabled unconditionally** in `StalwartClient.cs:59`
  (`ServerCertificateCustomValidationCallback = (_, _, _, _) => true`) — violates the
  spec's security section.
- **`SetupAsync` result bug**: after the domain loop, `result` is replaced with a fresh
  object (`MigrationOrchestrator.cs:176`), discarding all per-domain results, then
  iterates the now-empty `DomainResults` list.
- **Tests are green but hollow**: 268 passed / 20 skipped, but they cover constructors,
  validators, models, and the stub handlers; COM-dependent tests are skipped on Linux; no
  test exercises an end-to-end export → zip → import flow with real assertions on content.
- **No README.md** (required by the spec's project structure), while `docs/` extensively
  describes behavior that doesn't exist — a trap for users.

## What's genuinely in good shape

- The skeleton matches the spec's project structure almost exactly.
- Solid plumbing where it exists: HTTP retry with exponential backoff + jitter, a process
  runner with timeout/cancellation/kill handling, checkpoint file service,
  path/domain/email sanitizers, a sensitive-data log filter, a clean exception hierarchy.
- Domain/account/alias *extraction* via COM is plausibly shaped (modulo the auth and
  Messages issues above).
- The project's own `tasks/todo.md` is focused on small data-loss polish items — it does
  not reflect that the five blockers above exist.

## Goal-by-goal scorecard (initspec.md)

| Goal | Status |
|---|---|
| Migrate the email | ❌ No working path (fake EML, placeholder attachments, no message import, broken Vandelay invocation) |
| Migrate user accounts (without passwords) | ❌ Wrong API endpoints; also *does* copy passwords, contradicting spec |
| Domain-by-domain and all domains | ❌ Filtering logic exists but is unreachable from the CLI |
| Incremental / resumable transfer | ❌ Checkpoints written but never read; `--resume` dead-ends in a stub |
| Migrate via CLI (not direct DB) | ❌ All CLI handlers are stubs |
| Import into Stalwart in Docker via API | ❌ Client targets non-existent `/api/v1/*` endpoints |
| Extract to zip files | ❌ Zips contain only a one-field metadata.json |

## Recommended remediation path

1. Wire the CLI handlers to `MigrationOrchestrator` with real config-file loading.
2. Rewrite `StalwartClient` against the real `/api/principal` management API (Basic auth;
   domains, individuals, and aliases are all principal operations).
3. Fix the Vandelay invocation to match its actual CLI: per-account, with
   `--url` / `--auth-basic` / `--account-name` and an archive path; parse its real output.
4. Either fix COM message extraction to read raw EML via `IMAPFolders` /
   `Message.Filename` (preserving folder structure), or drop the fallback path entirely
   and commit to Vandelay for messages.
5. Implement resume: read checkpoints on start, skip completed domains/accounts,
   deduplicate by Message-ID.
6. Stop extracting/forwarding passwords; make TLS validation opt-out via config; fix the
   `SetupAsync` result-overwrite bug.
7. Add at least one end-to-end test (export → archive → import against a mock or real
   Dockerized Stalwart) that asserts on message content, not just object construction.
