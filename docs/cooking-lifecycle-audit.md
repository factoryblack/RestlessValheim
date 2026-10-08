# Cooking lifecycle audit — 8 October 2026

Baseline: main at `55e138dc0b7a2648c4c2ef41d22a0d90536c2ae7`.

## Fixed: placed feasts revert to preview physics

The existing placement/load hooks restored cloned colliders and called WearNTear.SetupColliders. However, Feast.UpdateVisual called KeepPlate and EnsureHit, which called DressFeast and disabled those same colliders again. Interacting also called EnsureHit. The restored support state was therefore temporary.

Native assembly inspection confirms that WearNTear.SetupColliders rebuilds m_colliders and m_bounds, and UpdateSupport consumes those cached bounds. The observed disappearance a few seconds after placement is consistent with unsupported-piece wear. This is a verified code defect and a strong explanation of the report, not a captured in-game reproduction.

The custom prefab now records the original collider enabled states before configuring preview physics. Unity clones/remaps those serialized references. A valid networked feast uses the original collider states and disables the preview plate hitbox; the ghost uses the plate hitbox. Subsequent visual refreshes retain placed physics. Placement and load still rebuild native support bounds. Missing custom meshes retain vanilla physics. No support immunity, destruction suppression, per-frame callback or timer change was introduced.

The interaction path no longer configures colliders, and visual refresh no longer performs the same setup twice.

## Fixed: collection snapshot before authority

KitchenRun.Collect read the ledger before ClaimOwnership, then used that snapshot to grant items and write the pantry/order state. It now checks ownership first and reads afterward. This removes a stale-read window; it does not constitute a complete multiplayer transaction protocol.

## Adjacent paths reviewed

- Native rack/oven insertion checks owner, capacity and native acceptance before debiting pantry.
- Preparation jobs bind to a station, pause while unavailable and retain paid inputs for cancellation.
- Fermenter tapping persists a handoff receipt and suppresses duplicate native output during recovery.
- Destruction hooks refund bound preparation inputs and spill the preparation table pantry.
- Feast material binding resolves the custom material, rather than an unrelated vanilla food item.

No additional change to these paths was justified in this pass. A source review cannot certify unload/crash recovery, peer handoff or physical station behaviour in a running world.

## Follow-up risks

- Kitchen order actions still use peer ownership claims rather than host-side transactional RPCs. The collection fix does not prove competing peer actions are serialized.
- Tick planning uses Stock(origin, 0), which includes the processing peer's local inventory, while actual ingredient pulls use the order player's identity. A remote order may be presented as ready while waiting for ingredients that peer cannot consume. Actual loading still requires a successful pull; this is a planning/availability concern requiring multiplayer follow-up.
- Physical output and recovery need explicit testing when a controller or machine unloads or changes owner.

## Validation

Production FeastColliderState.Apply is linked into KitchenMachines.Tests with a small collider boundary substitute: preview, placement, repeated refresh, reload, missing plate and preservation of original defaults. These tests do not execute Unity serialization or physics.

Native assembly contracts cover WearNTear.SetupColliders/OnPlaced and Feast.Start/UpdateVisual in addition to existing cooking adapter contracts. The normal CI release build and regression suites run on this PR.

## In-game acceptance checklist

1. Place a custom feast on a supported floor; wait at least 30 seconds and interact repeatedly.
2. Consume portions, save/reload and walk out of range/back. Verify the plate remains usable.
3. Remove its support, damage it and dismantle it: ordinary vanilla rules should remain effective.
4. Repeat with a remote player and with packaged assets missing in a dev build.
5. Collect an order on a second peer; check that pantry and ready counts agree afterward.
