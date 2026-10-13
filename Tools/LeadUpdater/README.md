# LeadUpdater package staging

`LeadUpdater.csproj` publishes a standalone, self-contained Windows x64
`LeadUpdater.exe`. It references the Phase 4A launcher assembly only to reuse
the install identity, active pointer, slot validator, transaction schema/state
machine, path checks, and atomic JSON writer. It does not launch a process,
change either active pointer, commit a slot, or perform a trial boot.

## Input contract

Arguments are parsed as switch/value pairs supplied by the trusted caller
through `ProcessStartInfo.ArgumentList`. Required switches:

```text
--install-root <trusted installation root>
--package <ZIP under Update/Temp or Update/Cache>
--operation-id <32 hexadecimal characters>
--expected-version <major.minor.patch>
--expected-sha256 <64 hexadecimal characters>
--expected-file-size <positive byte count>
--launcher-pid <positive PID>
--launcher-start-time-utc <round-trip UTC timestamp>
```

The optional `--game-pid` and `--game-start-time-utc` switches must be supplied
together. When present, the updater derives the expected image path from the
validated active slot and waits up to 120 seconds for that exact PID, start
time, and executable identity to exit. It never terminates a process.

The package's build contract is a root-level `LEADKhongPhanh.exe` and
`LEADKhongPhanh_Data/`. The updater does not select an executable by scanning
the archive. `slot.json` is updater-owned and is rejected inside the ZIP.

## Staging and limits

The package is size-checked and streamed through SHA-256 verification while a
read-only file handle prevents writes during subsequent ZIP preflight and
extraction. The complete archive is preflighted before the first extraction
write. Paths are normalized to Windows separators and rejected for traversal,
rooting, reserved names, ADS, collisions, links/reparse metadata, and
case-insensitive duplicates.

Default archive limits follow the existing Phase 2 configuration where
available: 4 GiB compressed package, 50,000 entries, 8 GiB expanded total, and
200:1 maximum compression ratio. The updater adds a 4 GiB per-entry expanded
limit and a 64 MiB central-directory ceiling, checked before opening entries.
`UpdaterLimits` validates these values and allows tests or a future trusted
updater configuration to supply stricter values.

After package verification and preflight, the updater creates a random
`slot-<32 hex>` destination distinct from both active and previous slot IDs.
An exclusive state-root lock serializes concurrent staging operations. It
streams files with create-new semantics, writes its own `slot.json`, runs
the shared slot validator plus the package contract checks, then transitions
the existing journal from `PREPARING` to `PREPARED`. Failures after
`PREPARING` transition through `ROLLBACK_REQUIRED` and `ROLLED_BACK`; cleanup
must complete before the journal is cleared. If recovery cannot be safely
completed, the journal and staging files are preserved and the updater fails.

The updater stores transaction state under the install's existing
`%LOCALAPPDATA%\LEAD-KHONG-PHANH\<install-id>\Updater\` directory. It only
reads `active.json`; it never writes `active.json` or
`active.previous.json`. Package SHA-256 verifies integrity against the trusted
caller-provided expectation; it is not a package signature or authentication
mechanism.

The phase stops at `PREPARED`. Commit, trial launch, health monitoring, and
runtime rollback are reserved for Phase 4C.

## Build, tests, and publish

```powershell
dotnet test Tools\LeadUpdater.Tests\LeadUpdater.Tests.csproj --configuration Release
dotnet publish Tools\LeadUpdater\LeadUpdater.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true --output <output-directory>
```
