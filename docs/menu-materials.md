# ESC material pass

The pause column and logout/quit confirmations now share the paper material used
by inventory and settings. No new artwork is required for this pass.

- A panel fits active menu button bounds with 24px local padding. Native button
  rectangles, layout, actions and controller navigation remain authoritative.
- Owned paper controls use centred 20px Averia labels, with a 16px minimum for
  compact/localized labels. Native interactability controls disabled presentation.
- Confirmation surfaces encompass the live question/title and buttons with
  horizontal 28px and vertical 24px padding. Each text source is mirrored once;
  unnamed questions are no longer also guessed to be a heading.
- Hover/controller focus uses the existing focus-corners asset. Owned geometry
  ignores layout groups; the fullscreen darken remains native.
- Undress restores captured TMP alpha/visibility, native graphic enabled/raycast
  state, button targetGraphic, transitions and colours. Only feedback components
  created by this dresser are destroyed. No global ChipButton layout reset.
- Settings, player lists and cloud warning subtrees remain outside this dresser.

## Verification boundary

C# syntax parsing is not a Unity compilation or runtime visual check. Before merge,
build with Valheim references and check:

1. ESC open/close repeatedly; enabled and disabled Save; optional invite/skip and
   Restless buttons. Panel bounds must follow the actual visible column.
2. Mouse hover/click and controller traversal/submit/cancel on every action.
3. Logout and quit: correct live/localized question, Yes/No, cancel, reopen. Test
   long translations and UI scaling for clipped copy or panel overflow.
4. Disable/re-enable the Restless menu style while open; native text and focus
   return, with no duplicate surfaces or invisible labels.
5. Open settings, player list and cloud warnings; their native content stays intact.
6. Reconnect and change scene; owned surfaces and focus components do not persist.

No gameplay, save, logout or exit callback was replaced.
