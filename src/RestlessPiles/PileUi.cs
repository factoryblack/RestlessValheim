using Jotunn.Managers;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessPiles;

internal static class PileUi
{
    private const float Width = 360f;
    private const float Height = 228f;
    private const float Reach = 4f;

    private static GameObject? _root;
    private static GameObject? _tray;
    private static GameObject? _slot;
    private static Image? _icon;
    private static Text? _title;
    private static Text? _meta;
    private static PileBox? _box;
    private static float _opened;
    private static bool _blocked;

    public static bool IsOpen => _root != null && _root.activeSelf && _box != null;

    public static void Open(PileBox box)
    {
        if (box == null || !box.isActiveAndEnabled)
            return;
        if (IsOpen && _box == box && Time.unscaledTime - _opened > 0.2f)
        {
            Close();
            return;
        }

        _box = box;
        _opened = Time.unscaledTime;
        Ensure();
        if (_root == null)
            return;
        _root.SetActive(true);
        Block(true);
        FreeCursor();
        Refresh();
    }

    public static void Close()
    {
        _box = null;
        Block(false);
        if (_root != null)
            _root.SetActive(false);
    }

    public static void TearDown()
    {
        Block(false);
        if (_root != null)
            Object.Destroy(_root);
        _root = null;
        _tray = null;
        _slot = null;
        _icon = null;
        _title = null;
        _meta = null;
        _box = null;
    }

    public static void Tick()
    {
        if (!IsOpen)
            return;
        if (!PileConfig.On || Player.m_localPlayer == null || RestlessUi.MapOpen()
            || RestlessUi.InventoryOpen() || Menu.IsVisible()
            || _box == null || !_box.isActiveAndEnabled)
        {
            Close();
            return;
        }

        var away = (Player.m_localPlayer.transform.position - _box.transform.position).sqrMagnitude;
        if (away > Reach * Reach)
        {
            Close();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        FreeCursor();
        Refresh();
    }

    private static void Block(bool on)
    {
        if (on == _blocked)
            return;
        GUIManager.BlockInput(on);
        _blocked = on;
        if (on)
            FreeCursor();
    }

    private static void FreeCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private static void Ensure()
    {
        var parent = GUIManager.CustomGUIFront != null
            ? GUIManager.CustomGUIFront.transform
            : Hud.instance?.m_rootObject?.transform;
        if (parent == null)
            return;
        if (_root != null)
            return;

        _root = RestlessUi.Node(parent, "RestlessPile");
        RestlessUi.Place(_root, RestlessUi.HudDock.Center, new Vector2(Width, Height), new Vector2(0f, 36f));
        _tray = RestlessUi.Tray(_root.transform, "tray");
        RestlessUi.Stretch(_tray, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RestlessUi.PaperSurface(_tray);
        RestlessUi.PaperCorner(_tray);
        RestlessUi.PaperControl(_tray);

        _title = RestlessUi.Label(_tray.transform, "", RestlessUi.TitleSize, RestlessUi.Text,
            TextAnchor.MiddleLeft);
        RestlessUi.Pin(_title.gameObject, new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(RestlessUi.PlateInset, -18f), new Vector2(Width - 56f, 36f));
        RestlessUi.TitleTear(_tray, _title.rectTransform);

        _slot = RestlessUi.Graphic(_tray.transform, "slot", Color.clear, false);
        RestlessUi.Pin(_slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 16f), new Vector2(72f, 72f));
        var iconGo = RestlessUi.Graphic(_slot.transform, "icon", Color.white, false);
        RestlessUi.Stretch(iconGo, Vector2.zero, Vector2.one, new Vector2(10f, 10f), new Vector2(-10f, -10f));
        _icon = iconGo.GetComponent<Image>();
        _icon.preserveAspect = true;

        _meta = RestlessUi.Label(_tray.transform, "", RestlessUi.BodySize, RestlessUi.Muted,
            TextAnchor.MiddleCenter);
        RestlessUi.Pin(_meta.gameObject, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -36f), new Vector2(Width - 48f, 24f));

        Button("take", "Take stack", new Vector2(-78f, 22f), () =>
        {
            var player = Player.m_localPlayer;
            if (player != null && _box != null)
                PileBag.TakeStack(player, _box);
        });
        Button("stack", "Stack", new Vector2(78f, 22f), () =>
        {
            var player = Player.m_localPlayer;
            if (player != null && _box != null)
                PileBag.DumpInto(player, _box);
        });
    }

    private static void Button(string name, string title, Vector2 pos, UnityEngine.Events.UnityAction click)
    {
        var plate = RestlessUi.Chip(_tray!.transform, name);
        RestlessUi.PaperControl(plate);
        RestlessUi.Pin(plate, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), pos,
            new Vector2(148f, RestlessUi.KeyHeight));
        var label = RestlessUi.Label(plate.transform, title, RestlessUi.MetaSize, RestlessUi.Text,
            TextAnchor.MiddleCenter);
        RestlessUi.Stretch(label.gameObject, Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
        var button = plate.AddComponent<Button>();
        button.targetGraphic = plate.GetComponent<Image>();
        RestlessUi.PaperSelectable(button);
        button.onClick.AddListener(click);
    }

    private static void Refresh()
    {
        if (_box == null || _tray == null)
            return;
        if (!_box.isActiveAndEnabled)
        {
            Close();
            return;
        }

        if (_title != null)
            _title.text = _box.GetHoverName();
        if (_title != null)
            RestlessUi.TitleTear(_tray, _title.rectTransform);
        if (_meta != null)
            _meta.text = _box.Stored.ToString();
        if (_icon != null)
        {
            _icon.sprite = RestlessUi.IconOf(_box.Item);
            _icon.enabled = _icon.sprite != null;
        }

        if (_slot != null && _box.Item != null)
        {
            var show = _box.Item.Clone();
            show.m_stack = 1;
            RestlessUi.DressSlot(_slot, _icon, false, show);
        }
    }
}
