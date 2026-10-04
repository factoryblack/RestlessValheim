# Transfer and production acceptance

The transfer protocol is shared by Quick Stack, Restock and the Storage browser. All peers must use the same Core build. Synchronous crafting/building still uses the existing ownership-claim spend path; this change does not replace vanilla crafting with an asynchronous action.

## What the regression suite covers

`tests/Reliability.Tests` runs the production transaction-history, receipt, queue and lease classes. It checks duplicate requests, cancellation arriving before a request, duplicate acknowledgement, cancellation after commit, reservation serialization/ownership hand-off, per-chest scope, history compaction, overlapping controllers and controller destruction, saved receipt retries, character/peer binding and mixed input queues. The native assembly contract is checked separately. These tests do not emulate Unity networking or native machine timers.

## Transfer behaviour

- A chest stores unresolved reservation payloads and terminal outcomes on its ZDO. Duplicate requests repeat the original result, rather than taking another batch.
- A character stores pending requests and commit/cancel decisions in custom data. Decisions retry every two seconds while the chest is loaded, until its owner confirms settlement. Reconnecting cancels requests whose delivery callback no longer exists and resumes recorded decisions.
- A timeout does not refund already delivered items. Cancellation creates a terminal record even when it arrives before the request.
- Character identity is checked against the owning network peer of the loaded Player, rather than comparing a profile ID directly to a peer ID. A refused request does not move stock.
- The sender rechecks a Quick Stack/ground-drop source before accepting the prepared transfer. Moving, consuming, equipping or locking the source during the wait can cancel that transfer.
- Settled history is compacted; retired sequence floors refuse old packets. Unresolved payloads are retained.

Persistence follows Valheim's ordinary character/world saves. This is not an atomic transaction across independently saved character and world files: process crashes, rollback of one save and destruction of a chest with an unresolved transfer still need explicit testing/recovery design. Do not advertise crash-proof transfers. Unresolved reservations are not blindly refunded just because a peer is absent.

## Two-client checks before release

1. With a chest owned by the other client, run Quick Stack, Restock and Storage Take. Repeat with a full bag and with custom item data/quality variants. Total all chest, bag and ground stacks before/after.
2. Delay request replies past eight seconds. Deliver cancellation before a delayed request. Deliver duplicate requests and late replies. Neither source nor destination should gain an extra copy.
3. Lose settlement acknowledgements, then reconnect the character and reload the chest. Its saved decision should settle once. Hand chest ownership to another peer while a reservation is open.
4. Move/drop/consume a Quick Stack source and a Restock destination while waiting. Repeat while switching characters. No callback should mutate another character's inventory.
5. Test a warded chest and requests carrying a different player's profile ID. Test a normal joining client, not only the host. Actor binding depends on a loaded Player ZDO.
6. Exercise chest destruction and abrupt shutdown during each phase. Inspect saved character/world data and record any unresolved reservations; these cases are not certified by the helper suite.

## Production checks

1. Order a smelter batch from a machine with no processed output and insufficient starting fuel. Once all ore is queued, refuelling must continue; queued output must already count toward the request.
2. Queue different ore types and include an existing processed-output buffer. Count input queue entries separately from that buffer. A kiln does not need coal refuelling.
3. Place two preparation tables covering the same rack/oven. Open both windows without ordering: browsing must not claim machines. Place overlapping orders: only the reserved controller may collect/load that machine. A controller should reserve only machines whose conversions are relevant to its outstanding orders.
4. Hand machine/controller ownership to another peer and unload/reload them. The reservation should remain tied to the controller. Finish/cancel the last order or destroy the controller: it should stop blocking other controllers.
5. Repeat the overlap check with Workshop boards, including standing stock orders. Standing orders intentionally retain their machines until cancelled.
6. Order one Cooked Egg, an oven product and a feast with shared intermediate recipes. Confirm normal native cooking times, one completion per output and no unnecessary additional loads.
7. Extinguish required fires and test cauldron usability from the controller's location. Confirm pause/resume without paying twice.

Profile the overlap scene with storage/UI open. Reservations and receipt retries are gated; do not add per-frame station scans or UI rebuilds to fix acceptance failures.


## Workshop/storage follow-up

See [the multiplayer follow-up](workshop-network-audit.md) for remote withdrawal routing, native Workshop delivery, shared-stock accounting and the focused two-client checks.
