# Changelog

## 0.1.1

- Hammer-remove or smash returns stored extras and the pile's build cost (the wood/stone spent to place it)

## 0.1.0

- Vanilla resource piles (wood stack, stone pile, and the rest) open as uncapped single-item stores
- Existing placed piles keep working — the vanilla prefab is dressed, not replaced
- Take stack grabs one bag-shaped stack (one partial, or one new max stack)
- Tray Stack and Core ` dump matching bag stacks into the pile
- Hammer-remove / smash spills stored extras; vacuum and craft-from-chests ignore piles
- Pile tray unlocks the cursor so Take stack / Stack can be clicked
- Vacuum pulls matching ground drops into a pile even when stored is 0 (the piece kind is enough)
- Pile count is stored as a long and shown on the tray text, not as an item stack (that was capping at 1000)
