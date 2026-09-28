# Cookbook UI
The preparation table opens an owned RestlessCook window through Kitchen.Opened.
Core supplies the existing material, type, controls and scrolling helpers via the same friend-assembly mechanism used by other first-party modules. No recipe or automation rules are moved into Core.

## Included
- Searchable feast/all-food catalogue using Kitchen.Rows.
- A focused recipe branch with connected cards, quantities, state colours and depth-aware back navigation. Repeated ingredients in separate branches stay separate.
- Selected-step ingredients, station requirement and other-use names.
- Orders list, live plan, explicit placement, collect and two-click cancellation.
- Kitchen station capacity, fuel and block reasons.
- Local F8 Cookbook toggle. Disabling the UI does not cancel automation.
- Canvas scaling, clipped independent readers, pooled cards, native scrollbar artwork.
- Poll only while open, once per second; repaint plans only when their display signature changes. No UI station discovery in the per-frame path.
- Closing, walking away, death, settings/inventory opening and plugin teardown release input blocking.

## Backend integration limits
This screen displays Kitchen state as supplied; it does not independently promise multiplayer transactional safety or recalculate availability. Kitchen.Plan/Live/Stations currently perform their own discovery scans, so overall scan cost remains a backend concern.
Kitchen exposes Take for a known prefab but no pantry enumeration: cancelled-order leftovers have no complete pantry browsing/collection UI yet.
There is no queue priority API or per-player permission/result contract. Cancel is void; the UI reports a request and refreshes actual orders.
KitchenRun.Tick increments order.Ready for root production and Collect reduces it without reducing Count/removing the order. Finite completion semantics need backend verification before calling this production-ready.
The backend's planning and station scanning may claim ownership, and it has no dedicated server-authorised order command interface. Multiplayer contention remains a backend validation item.
Search/filter and order quantity apply to recipe planning, not active orders. Controller navigation into offscreen list entries remains a follow-up.
This is a functional first UI pass, not pixel-identical to the illustrative mockup. It uses real game icons and actual cookbook relationships.

## Validation
Build through the repository CI (Unity/game references are not installed locally). In-game checks required:
- Open/close/reopen, Escape without pause-menu spillover, F8 toggle and walk-away cleanup.
- Long names, repeated ingredients, deep branches and large UI scale.
- Place an order; follow live preparation; collect with full/partial inventory; cancel with confirmation.
- Check an empty queue, missing ingredients, missing station upgrades, fuel starvation and full racks.
- Two players sharing a table and stations; save/reload and owner handoff.
Install matching RestlessCore and RestlessCook builds.
