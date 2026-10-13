# LeadKhongPhanh stable launcher

`LeadLauncher.csproj` builds the stable Windows x64 entry point named
`LEADKhongPhanh.exe`. It is a self-contained .NET 10 single-file executable and
does not reference Unity, Firebase, or game assemblies. `LeadUpdater.exe` is
intentionally not part of this phase.

## Installation and state layout

The launcher expects its installation root to contain `install.id` and
`Slots/`. A slot is addressed only by a validated `slot-<32 hex>` ID and
contains `slot.json`, the game executable, and its matching `_Data` directory.
The launcher does not infer the active slot from folder names, versions, or
timestamps.

The first launch creates a random 128-bit `install.id` with create-once
semantics. Updater state is placed under
`%LOCALAPPDATA%\LEAD-KHONG-PHANH\<install.id>\Updater\`; it is never written
under the game installation by the state-store code.

```text
Updater/
  active.json
  active.previous.json
  transaction.json
  Signals/<operationId>.healthy.json
  Logs/launcher-YYYYMMDD.log
```

State JSON is strict, size-bounded, and written using a same-directory
temporary file, disk flush, and atomic replace/move. A failed state write
prevents launch or recovery from continuing.

## Recovery and health contract

The transaction state machine is the only allowed route between persisted
phases. Startup validates the install identity, journal, pointers, slot
metadata, executable and `_Data` directory before deciding what may launch.
An uncommitted transaction restores the validated old slot; trial state needs
a matching health receipt and a live process identity. If a trial fails its
health contract, the launcher terminates it only after rechecking PID, process
start time, and canonical executable path.

The trial must produce a valid receipt within 120 seconds. After receipt, the
same process must remain alive and match identity for a 30-second confirmation
window. Receipt timestamps must fall within the trial window. A receipt is not
trusted merely because its file exists.

The receipt contract is:

```json
{
  "schemaVersion": 1,
  "operationId": "0123456789abcdef0123456789abcdef",
  "slotId": "slot-0123456789abcdef0123456789abcdef",
  "version": "1.2.3",
  "pid": 1234,
  "processStartTimeUtc": "2026-10-06T03:00:00Z",
  "executablePath": "canonical running executable path",
  "healthyAtUtc": "2026-10-06T03:00:10Z"
}
```

The game does not yet write this receipt; this phase defines and validates the
contract without changing Unity runtime code.

Phase 4B's standalone staging executable reuses this install, slot, pointer,
and transaction contract and stops at `PREPARED`. See
[the LeadUpdater staging contract](../LeadUpdater/README.md).

## Build and tests

Run the pure .NET tests:

```powershell
dotnet test Tools\LeadLauncher.Tests\LeadLauncher.Tests.csproj -c Release
```

Publish the stable, self-contained Windows x64 executable:

```powershell
dotnet publish Tools\LeadLauncher\LeadLauncher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The output must be installed/renamed into the stable installation root as
`LEADKhongPhanh.exe`. No updater, extraction, slot creation, or payload
replacement is performed by this launcher phase.
