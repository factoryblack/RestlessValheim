using System.Collections.Generic;
using HarmonyLib;
using Jotunn.Managers;
using RestlessQoL.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.HudTweaks;

public sealed class Notices : FeatureModule
{
    public override string Id => "ui.notices";
    public override bool Enabled => true;

    private const float Gap = 9f;
    private const float Rise = 18f;
    private const float Floor = 220f;
    private const int CapMin = 8;
    private const int CapMax = 16;

    private static GameObject? _root;
    private static GameObject? _stack;
    private static readonly List<Toast> Live = new();
    private static readonly List<Behaviour> ExtraHidden = new();
    private static bool _vanillaHidden;

    protected override void OnLoaded()
    {
        GUIManager.OnCustomGUIAvailable += Rebuild;
    }

    public override void Tick()
    {
        if (ModConfig.NoticesEnabled.Value)
            return;
        if (MessageHud.instance != null)
            Restore(MessageHud.instance);
        if (_root != null)
            _root.SetActive(false);
    }

    public static void Post(string text, int amount = 1, Sprite? icon = null)
    {
        if (!ModConfig.NoticesEnabled.Value || string.IsNullOrWhiteSpace(text))
            return;
        Ensure();
        if (_stack == null)
            return;
        Push(Localize(text).Trim(), amount < 1 ? 1 : amount, icon);
    }

    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MessageHud), "ShowMessage")]
        private static bool ShowMessage(MessageHud __instance, object[] __args)
        {
            if (!ModConfig.NoticesEnabled.Value)
                return true;

            string? text = null;
            var amount = 1;
            Sprite? icon = null;
            var despite = false;
            var log = true;
            var seenBool = 0;
            foreach (var arg in __args)
            {
                switch (arg)
                {
                    case string s:
                        text = s;
                        break;
                    case int n:
                        amount = n;
                        break;
                    case Sprite sp:
                        icon = sp;
                        break;
                    case bool b:
                        if (seenBool == 0)
                            despite = b;
                        else
                            log = b;
                        seenBool++;
                        break;
                }
            }

            if (Hud.IsUserHidden() && !despite)
                return true;
            if (string.IsNullOrWhiteSpace(text))
                return true;

            Ensure();
            if (_stack == null)
                return true;

            Push(Localize(text!).Trim(), amount, icon);
            HideUnlocks(__instance);
            if (log)
            {
                try
                {
                    Traverse.Create(__instance).Method("AddLog", text).GetValue();
                }
                catch
                {
                    // older builds
                }
            }

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.QueueUnlockMsg))]
        private static bool QueueUnlock(MessageHud __instance, Sprite icon, string topic, string description)
        {
            if (!ModConfig.NoticesEnabled.Value)
                return true;
            var title = Localize(topic ?? "").Trim();
            var body = Localize(description ?? "").Trim();
            var text = string.IsNullOrEmpty(body) || body == title
                ? title
                : string.IsNullOrEmpty(title) ? body : title + " — " + body;
            if (string.IsNullOrWhiteSpace(text))
                return false;
            Post(text, 1, icon);
            HideUnlocks(__instance);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.UpdateUnlockMsg))]
        private static bool UpdateUnlock(MessageHud __instance)
        {
            if (!ModConfig.NoticesEnabled.Value)
                return true;
            HideUnlocks(__instance);
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MessageHud), "UpdateMessage")]
        private static bool UpdateMessage(MessageHud __instance)
        {
            if (!ModConfig.NoticesEnabled.Value)
                return true;
            Ensure();
            Drain(__instance);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MessageHud), nameof(MessageHud.Update))]
        private static void Update(MessageHud __instance)
        {
            if (!ModConfig.NoticesEnabled.Value)
            {
                Restore(__instance);
                if (_root != null)
                    _root.SetActive(false);
                return;
            }

            Hide(__instance);
            HideUnlocks(__instance);
            Ensure();
            RestlessUi.HudTuck.Tick();
            if (_root != null)
            {
                var despite = Traverse.Create(__instance).Field("m_showDespiteHiddenHUD").GetValue<bool>();
                if (_stack != null)
                    RestlessUi.HudTuck.Shift(_stack.GetComponent<RectTransform>(), RestlessUi.HudTuck.NoticeAway);
                RestlessUi.HudTuck.Fade(_root);
                _root.SetActive((!Hud.IsUserHidden() || despite) && RestlessUi.HudTuck.Amount < 0.98f);
            }

            Step();
        }
    }

    private static void HideUnlocks(MessageHud hud)
    {
        if (hud.m_unlockMessages != null)
        {
            foreach (var go in hud.m_unlockMessages)
            {
                if (go != null)
                    go.SetActive(false);
            }
        }

        try
        {
            var queue = Traverse.Create(hud).Field("m_unlockMsgQueue");
            if (queue.FieldExists())
                queue.Method("Clear").GetValue();
            var count = Traverse.Create(hud).Field("m_unlockMsgCount");
            if (count.FieldExists())
                count.SetValue(0);
        }
        catch
        {
            // queue layout varies
        }
    }

    private static void Hide(MessageHud hud)
    {
        Mute(hud.m_messageText);
        Mute(hud.m_messageCenterText);
        Mute(hud.m_messageIcon);
        MutePlate(hud.m_messageText);
        MutePlate(hud.m_messageCenterText);
        MuteTmp(hud.m_messageText);
        MuteTmp(hud.m_messageCenterText);
        _vanillaHidden = true;
    }

    private static void Restore(MessageHud hud)
    {
        if (!_vanillaHidden)
            return;
        if (hud.m_messageText != null)
            hud.m_messageText.enabled = true;
        if (hud.m_messageCenterText != null)
            hud.m_messageCenterText.enabled = true;
        if (hud.m_messageIcon != null)
            hud.m_messageIcon.enabled = true;
        foreach (var behaviour in ExtraHidden)
        {
            if (behaviour != null)
                behaviour.enabled = true;
        }

        ExtraHidden.Clear();
        _vanillaHidden = false;
    }

    private static void Mute(Behaviour? behaviour)
    {
        if (behaviour == null || !behaviour.enabled)
            return;
        behaviour.enabled = false;
    }

    private static void MutePlate(Component? source)
    {
        if (source == null)
            return;
        void MuteOn(Transform node)
        {
            foreach (var image in node.GetComponents<Image>())
            {
                if (!image.enabled)
                    continue;
                image.enabled = false;
                ExtraHidden.Add(image);
            }
        }

        MuteOn(source.transform);
        if (source.transform.parent == null)
            return;
        foreach (Transform child in source.transform.parent)
            MuteOn(child);
    }

    private static void MuteTmp(Component? source)
    {
        if (source == null)
            return;
        foreach (var tmp in source.GetComponentsInChildren<TMP_Text>(true))
            Mute(tmp);
    }

    private static string Localize(string text) =>
        Localization.instance != null ? Localization.instance.Localize(text) : text;

    private static Transform? HudRoot() =>
        global::Hud.instance?.m_rootObject != null
            ? global::Hud.instance.m_rootObject.transform
            : null;

    private static bool StackPinnedTopLeft()
    {
        if (_stack == null)
            return false;
        var rt = _stack.GetComponent<RectTransform>();
        return rt != null && rt.anchorMin.x < 0.01f && rt.anchorMin.y > 0.99f;
    }

    private static void Rebuild()
    {
        Live.Clear();
        ExtraHidden.Clear();
        _stack = null;
        _vanillaHidden = false;
        if (_root != null)
            Object.Destroy(_root);
        _root = null;
    }

    private static void Ensure()
    {
        if (_root != null && !StackPinnedTopLeft())
            Rebuild();
        if (_root != null)
            return;
        var parent = HudRoot();
        if (parent == null)
            return;
        _root = RestlessUi.Node(parent, "RestlessNotices");
        RestlessUi.Stretch(_root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        _stack = RestlessUi.Node(_root.transform, "stack");
        RestlessUi.Place(_stack, RestlessUi.HudDock.TopLeft,
            new Vector2(RestlessUi.HudRow.NoticeWidth, 0f), Vector2.zero);
    }

    private static void Drain(MessageHud hud)
    {
        try
        {
            var queue = Traverse.Create(hud).Field("m_msgQeue");
            if (!queue.FieldExists())
                queue = Traverse.Create(hud).Field("m_msgQueue");
            if (!queue.FieldExists())
                return;
            while (queue.Property("Count").GetValue<int>() > 0)
            {
                var msg = queue.Method("Dequeue").GetValue();
                if (msg == null)
                    break;
                var data = Traverse.Create(msg);
                var text = data.Field("m_text").GetValue<string>();
                if (string.IsNullOrWhiteSpace(text))
                    continue;
                Push(Localize(text).Trim(), data.Field("m_amount").GetValue<int>(),
                    data.Field("m_icon").GetValue<Sprite>());
            }
        }
        catch (System.Exception e)
        {
            Plugin.Log.LogWarning("ui.notices drain: " + e.Message);
        }
    }

    private static int Room()
    {
        var h = 0f;
        if (_root != null)
            h = _root.GetComponent<RectTransform>().rect.height;
        if (h < 200f)
            h = Screen.height;
        var row = RestlessUi.HudRow.NoticeHeight + Gap;
        var usable = Mathf.Max(h - Floor, CapMin * row);
        return Mathf.Clamp(Mathf.FloorToInt(usable / row), CapMin, CapMax);
    }

    private static void Push(string text, int amount, Sprite? icon)
    {
        if (_stack == null)
            return;
        amount = amount < 1 ? 1 : amount;
        Toast? oldest = null;
        var sitting = 0;
        foreach (var toast in Live)
        {
            if (toast.Leaving)
                continue;
            sitting++;
            oldest ??= toast;
            if (!ModConfig.NoticesMerge.Value || toast.Text != text)
                continue;
            toast.Add(amount, icon);
            return;
        }

        if (sitting >= Room() && oldest != null)
            oldest.Dismiss();

        var pitch = RestlessUi.HudRow.NoticeHeight + Gap;
        foreach (var toast in Live)
            toast.Shift(pitch);

        var next = Toast.Create(_stack.transform);
        next.Push(text, amount, icon);
        next.ParkIn();
        Live.Add(next);
        next.Root.transform.SetAsLastSibling();
    }

    private static void Step()
    {
        if (_root == null)
            return;
        var slot = 0;
        var pitch = RestlessUi.HudRow.NoticeHeight + Gap;
        for (var i = Live.Count - 1; i >= 0; i--)
        {
            var toast = Live[i];
            if (!toast.Leaving)
            {
                toast.Target = slot * pitch;
                slot++;
            }

            toast.Tick();
            if (!toast.Dead)
                continue;
            Object.Destroy(toast.Root);
            Live.RemoveAt(i);
        }
    }

    private sealed class Toast
    {
        public GameObject Root => _row.Root;
        public string Text = "";
        public float Target;
        public bool Leaving;
        public bool Dead;

        private RestlessUi.HudRow _row = null!;
        private int _amount;
        private float _span;
        private float _life;
        private float _y;
        private float _park;
        private float _alpha;
        private bool _in;

        public static Toast Create(Transform parent)
        {
            return new Toast { _row = RestlessUi.HudRow.Notice(parent) };
        }

        public void Push(string text, int amount, Sprite? icon)
        {
            Text = text ?? "";
            _amount = amount < 0 ? 0 : amount;
            ResetClock();
            Leaving = false;
            Dead = false;
            _in = false;
            _alpha = 0f;
            Paint(icon);
            Root.SetActive(true);
        }

        public void Add(int amount, Sprite? icon)
        {
            _amount += amount;
            ResetClock();
            Leaving = false;
            Dead = false;
            if (!_in)
                ParkIn();
            Paint(icon);
        }

        public void Shift(float dy)
        {
            Target += dy;
            _y += dy;
            _park += dy;
            Place();
        }

        public void ParkIn()
        {
            Target = 0f;
            _y = -Rise;
            _park = 0f;
            _in = true;
            Place();
        }

        public void Dismiss()
        {
            Leaving = true;
            _life = 0f;
        }

        public void Tick()
        {
            if (Dead)
                return;
            if (string.IsNullOrEmpty(Text) && _alpha <= 0f && !_in)
                return;

            if (!Leaving)
            {
                _life -= Time.unscaledDeltaTime;
                if (_life <= 0f)
                    Leaving = true;
            }

            if (!_in)
            {
                _y = Target - Rise;
                _park = Target;
                _in = true;
            }

            if (Leaving)
                _y = _park;
            else
            {
                _y = RestlessUi.Toward(_y, Target);
                _park = _y;
            }

            _alpha = RestlessUi.Toward(_alpha, Leaving ? 0f : 1f);
            var showFill = ModConfig.NoticesFill.Value && !Leaving && _span > 0f;
            _row.SetFill(showFill ? _life / _span : 0f);

            Place();
            Root.transform.localScale = Vector3.one;
            _row.Fade.alpha = _alpha;
            if (Leaving && _alpha <= 0.02f)
                Dead = true;
        }

        private void Place()
        {
            _row.Rect.sizeDelta = new Vector2(RestlessUi.HudRow.NoticeWidth, RestlessUi.HudRow.NoticeHeight);
            _row.Rect.anchoredPosition = new Vector2(0f, -_y);
        }

        private void ResetClock()
        {
            _span = Mathf.Max(0.5f, ModConfig.NoticesHold.Value);
            _life = _span;
        }

        private void Paint(Sprite? icon)
        {
            _row.SetIcon(icon);
            _row.SetCopy(Text, _amount > 1 ? "×" + _amount : null);
        }
    }
}
