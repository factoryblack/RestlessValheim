# Workshop and storage multiplayer follow-up — 5 October 2026

## Confirmed source findings and fixes

| Finding | Effect | Change |
| --- | --- | --- |
| Storage browser walks every nearby chest sequentially | Each unrelated remote-owned chest adds an RPC round trip; the local owner completes immediately | Route exact-identity withdrawals through matching inventories; recheck each candidate before requesting |
| Item identity uses locale-dependent durability formatting | Different host/client number formats can make the same gear fail to match | Use invariant numeric formatting and test en-AU against de-DE |
| Withdrawal sends once and waits eight seconds | A lost packet or ownership hand-off produces a long wait | Retry the same durable reservation once a second within the existing eight-second timeout; duplicates retain the same ID |
| Native smelter output can spawn inside QueueProcessed | The one-second board collector misses immediate output; Make stays unfinished and can load replacements | Intercept actual Smelter.Spawn(string,int) delivery for an active, assigned, locally owned board/machine pair |
| Workshop stock includes the controlling peer's bag | Keep coverage changes when another player becomes owner; background work consumes that player's supplies | Shared stock and automatic pulls use board output, accessible chests and registered lots; bags are excluded |
| Chest access uses the controlling peer's identity | Private-chest eligibility changes between host, guest and dedicated ownership | Use the board creator's identity consistently; never skip the chest access check for an actor-zero board |
| Keep consumes incoming coverage before Make, but delivery credits Make first | Counts disagree when Keep was placed before Make for the same output | Use the same Make-first reservation policy in production accounting and delivery credit |
| Duplicate Keep targets are prevented only in UI | Other API callers can create competing standing targets | Reject duplicate Keep targets in the order API |
| A delayed withdrawal can finish after death/out-of-range | Items can be delivered after the table is no longer usable | Recheck availability and return the full batch instead |

The native game assembly confirmed Smelter.Spawn(string,int), and that
QueueProcessed/SpawnProcessed call that method. Unassigned, inactive and
unwanted output retains native drop behaviour. Actual production time, fuel use,
input processing and machine conditions are unchanged. This is not a timer-based
simulation or a faster manufacturing mode.

Keep means shared stock, including the board's unreserved output; automatic
chest delivery is still not implemented. Player bags are neither counted nor
consumed. Chest permissions use the player who placed the board, independent of
which peer currently owns its ZDO. Make output remains reserved for its placing
player. Taking Keep output into a bag now creates a real shared-stock shortage.

## Validation

The reliability suite links the production WorksAccounting and WorksOrder
classes. It checks a Keep target placed before Make, reservations, mixed finished
and incoming output, delivery credit, excess output and active assigned-controller
checks. Native contract tests validate the Spawn signature. Existing transfer
history tests check duplicate IDs, reordering and cancellation conservation.
CI compiles all seven plugins and the external UI consumer.

These checks do not reproduce real network latency, Unity lifecycle ordering,
private-area protections or machine/controller hand-off during the precise spawn
frame. A live two-client pass is still required; no host/client timing improvement
is claimed from a measured session yet.

## Focused two-client test

1. Use identical current Core/Storage/Workshop builds on both clients (and server
   when dedicated). Check the loaded plugin version lines before testing.
2. Put the requested item in a far chest with many unrelated nearby chests. Take
   one stack as host and guest. With debug logging enabled, `Storage take:` reports
   candidate/scanned chest counts, elapsed seconds and result. Confirm unrelated
   chests do not become requests. Repeat with stock split across matching chests.
3. During a request, hand chest ownership to the other peer. Check retry completes
   or cancels within the same timeout and total stock is conserved. Test a full
   bag and walking away/death while waiting; rejected items stay/return in storage.
4. Make five copper using a normal immediate-output smelter. Each finished bar
   must increase Ready on the board; five delivered bars stop further loading.
   Repeat with native stacked output and more than one machine.
5. Place Keep 30 before Make 10 for the same output. Ready Make output is reserved;
   incoming output is counted once. Collect Make output, then take Keep output:
   shared coverage falls and Keep replenishes the actual shortage.
6. Put finished stock and inputs solely in the host's bag, then guest's bag. Neither
   should change Keep coverage or be consumed by autonomous production. Move them
   to eligible chests and check that coverage changes. Reopen from the other peer.
7. Use a private chest allowed to the board creator, and one denied to them.
   Eligibility must remain the same across ownership changes and on a dedicated
   server. A board with creator zero must not bypass private chest permissions.
8. Overlap two active boards. Each machine delivers only to its active assigned
   controller. Cancel the last order: remaining native work may drop normally.
   Check a machine/controller hand-off precisely as a product finishes; if it
   drops instead of being captured, record the owner and timing for follow-up.

## Remaining architecture limits

The board still uses the existing ownership-claim mutation model rather than a
central owner-command RPC interface. Opening it can transfer control away from
another viewer, whose UI then becomes read-only. Production uses the synchronous
chest ownership-claim spend path, so ownership contention can defer loading until
a later tick. A machine owned by a peer that does not also own its assigned board
cannot safely mutate the board ledger; its output retains native delivery until
ownership converges. These are not certified away by the accounting tests.

Independent character/world save rollback and hard-crash transfer atomicity remain
as documented in [reliability.md](reliability.md). A full automatic dependency tree
and designated-chest output routing are separate functionality, not existing
Workshop behaviour.
