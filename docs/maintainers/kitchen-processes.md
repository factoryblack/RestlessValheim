# Physical kitchen processing

Kitchen orders are executed by the owner of the preparation table at the existing one-second tick cadence. There are no new per-frame station scans. Rendering a recipe does not acquire a machine.

## Recipe routing

Native machine conversions take precedence over a direct finished-item entry in `cook.yaml`.

| Process | Preparation | Physical processing |
| --- | --- | --- |
| Rack food | Raw meat or fish | Real rack slot and native cook timer |
| Baked food | Cook ingredients make the native raw dish/dough at its required station | One raw item enters the oven per finished item; native fuel, queue and bake timer |
| Mead | Native base recipe at its required kettle/cauldron | One base enters a fermenter; native duration, cover and produced-item count |
| Cauldron/preparation dish | Configured ingredients and crafting duration | One saved job per physical crafting station; native fire, shelter and upgrade checks |

Crafting stations have no native unattended queue. Their preparation jobs are a Cook extension; racks, ovens and fermenters retain their own processing state. Existing cooked ingredients can satisfy an intermediate step. New orders still distinguish newly finished output from food already carried by the player.

Raw preparation rows are derived without changing the configured finished-food rows. Catalogue rebuilding clears derived entries when the world database/scene changes. Missing native raw recipes can fall back to the configured preparation requirement; native machine output is never manufactured directly by that fallback.

## Authority and collection

`ProductionLease` assigns a native machine to an active controller. Preparation jobs persist the station ZDO identity; another cauldron cannot finish a job paused at its original station. A preparation table uses its own preparation capacity, and claims only the types of nearby crafting stations its orders require.

Insertion rechecks live capacity and verifies native acceptance before debiting the pantry. Rack insertion calls the native owner handler; oven insertion uses its native input queue; fermenter insertion uses the native content hash and start-time handler.

Rack completion reads the native slot state. Oven completion intercepts native `Smelter.Spawn` under an exact active lease, before the normal caller clears its processed buffer. Non-food workshop machines retain their ordinary/Workshop path.

Fermenter tapping uses native `RPC_Tap` and its delayed callback. A saved receipt covers the interval after the barrel clears its source and before output is delivered. Native collection and reload recovery consume the same receipt once. Recovery waits for the saved tapping deadline and gives a live native callback priority. A committed receipt can finish even if a player has manually refilled the barrel. An interrupted uncommitted intent cannot award output while its original content remains.

Old preparation jobs without a station identity bind once on resume. Cancelling preparation returns paid inputs. Cancelling an order does not refund ingredients already loaded into a native machine. Destroying a bound preparation station returns its in-progress ingredients to the kitchen; destroying the table spills its pantry and paid preparation inputs. A completed batch in the tapping handoff remains accounted for.

## Verification

CI builds all seven plugins and checks the native insertion/timer/collection signatures against the installed Valheim assembly. Linked production tests cover routing, batch rounding, independent preparation capacity, save migration, rejected/full/non-owner loads, cover/fuel conditions, tap persistence and duplicate collection.

These tests substitute game boundaries; they are not a running Valheim multiplayer test. Before release, verify these cases with the built Core and Cook DLLs on all peers:

| Test | Expected result |
| --- | --- |
| Bread with flour and oven fuel, then three bread | Raw dough appears in the pantry and enters the oven; baking takes native time; preparation rounds to whole dough batches |
| A pie requiring cooked protein | Meat occupies real rack hooks, the raw pie is assembled, then the oven bakes it |
| Seven meads with enough base ingredients and two fermenters | Two native batches are prepared/fermented, with the configured per-barrel yield; spare output remains in pantry |
| Full rack/oven or occupied fermenter | Further loads do not consume ingredients or exceed capacity |
| Remove fire, fuel, roof or cover | The relevant process pauses/blocks and resumes under the native station rules |
| Two cauldrons, then destroy one during preparation | Independent jobs run; destroyed-station ingredients return once; surviving work continues |
| Cancel during preparation, baking, fermentation and tapping | Preparation refunds once; loaded machines continue; a completed tapping receipt is collected once |
| Save/reload during preparation and during tapping | Bound preparation progress survives; the delayed batch is recovered once |
| Two nearby kitchens and a remote peer taking table ownership | Active leases prevent cross-collection; contents, preparation bindings and output counts survive handoff |
| Dedicated server, player leaves range, then returns | Fuel can come from eligible nearby storage without a local player; output remains collectable from order/pantry |

Personal inventory supply and automatic delivery still depend on the relevant local player being on the executing peer. Shared nearby stock and persisted machine state are the durable source of unattended work. Broader controller command/RPC ownership redesign is outside this change.
