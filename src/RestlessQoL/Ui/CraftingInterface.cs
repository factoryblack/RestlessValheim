using HarmonyLib;
using Jotunn.Managers;
using RestlessQoL.Core;
using UnityEngine;
using UnityEngine.UI;

namespace RestlessQoL.Ui;

public sealed class CraftingInterface : FeatureModule
{
    public override string Id=>"ui.crafting_window";
    public override bool Enabled=>true;
    public override bool TickInMenus=>true;
    private static CanvasGroup? _craftGroup;
    private static bool _addedGroup;
    private static float _alpha;
    private static bool _interactable,_raycasts;
    private static GameObject? _entry;
    protected override void OnLoaded()=>GUIManager.OnCustomGUIAvailable+=Reset;
    public override void Tick()
    {
        if(!ModConfig.CraftingWindowEnabled.Value){CraftingWindow.Close(false);Restore();return;}
        var gui=InventoryGui.instance;
        if(gui==null||!InventoryGui.IsVisible())return;
        EnsureEntry(gui);
        if(_entry!=null)_entry.SetActive(gui.m_crafting.gameObject.activeInHierarchy);
    }
    private static void EnsureEntry(InventoryGui gui)
    {
        if(_entry!=null)return;
        if(gui.m_crafting==null)return;
        _craftGroup=gui.m_crafting.GetComponent<CanvasGroup>();_addedGroup=_craftGroup==null;
        if(_craftGroup==null)_craftGroup=gui.m_crafting.gameObject.AddComponent<CanvasGroup>();
        _alpha=_craftGroup.alpha;_interactable=_craftGroup.interactable;_raycasts=_craftGroup.blocksRaycasts;
        _craftGroup.alpha=0;_craftGroup.interactable=false;_craftGroup.blocksRaycasts=false;
        // A sibling remains usable while the native crafting subtree is visually suppressed.
        _entry=RestlessUi.Tray(gui.m_crafting.parent,"RestlessCraftingEntry");RestlessUi.PaperSurface(_entry);
        var rect=(RectTransform)_entry.transform;
        rect.anchorMin=gui.m_crafting.anchorMin;rect.anchorMax=gui.m_crafting.anchorMax;
        rect.pivot=gui.m_crafting.pivot;rect.anchoredPosition=gui.m_crafting.anchoredPosition;
        rect.sizeDelta=new Vector2(360,160);
        Api.UiKitWindow.Label(rect,"Crafting",26,new Rect(24,16,300,36));
        Api.UiKitWindow.Label(rect,"Recipes, equipment upgrades and station extensions",17,new Rect(24,54,310,44),true);
        Api.UiKitApi.Button(rect,"Open Crafting",new Rect(24,108,310,38),()=>CraftingWindow.Open(gui));
    }
    private static void Restore()
    {
        if(_craftGroup!=null)
        {
            if(_addedGroup)Object.Destroy(_craftGroup);
            else {_craftGroup.alpha=_alpha;_craftGroup.interactable=_interactable;_craftGroup.blocksRaycasts=_raycasts;}
        }
        _craftGroup=null;_addedGroup=false;
        if(_entry!=null){_entry.SetActive(false);Object.Destroy(_entry);}_entry=null;
    }
    private static void Reset(){CraftingWindow.Reset();Restore();}
    [HarmonyPatch]
    private static class Patches
    {
        [HarmonyPostfix,HarmonyPatch(typeof(InventoryGui),nameof(InventoryGui.Show))]
        private static void Show(InventoryGui __instance,Container container)
        {
            if(!ModConfig.CraftingWindowEnabled.Value)return;
            EnsureEntry(__instance);
            if(container==null&&Player.m_localPlayer?.GetCurrentCraftingStation()!=null)CraftingWindow.Open(__instance);
        }
        [HarmonyPrefix,HarmonyPatch(typeof(InventoryGui),nameof(InventoryGui.Hide))]
        private static void Hide()=>CraftingWindow.Close(false);
        [HarmonyPrefix,HarmonyPatch(typeof(InventoryGui),nameof(InventoryGui.UpdateGamepad))]
        private static bool Gamepad()=>!CraftingWindow.IsOpen;
        [HarmonyPrefix,HarmonyPatch(typeof(InventoryGui),nameof(InventoryGui.UpdateRecipeGamepadInput))]
        private static bool RecipeGamepad()=>!CraftingWindow.IsOpen;
        [HarmonyPrefix,HarmonyPatch(typeof(InventoryGui),nameof(InventoryGui.DoCrafting))]
        private static void BeforeCraft(InventoryGui __instance,out CraftingWindow.CraftResult? __state)=>__state=CraftingWindow.BeforeCraft(__instance);
        [HarmonyPostfix,HarmonyPatch(typeof(InventoryGui),nameof(InventoryGui.DoCrafting))]
        private static void AfterCraft(InventoryGui __instance,CraftingWindow.CraftResult? __state)=>CraftingWindow.AfterCraft(__instance,__state);
        [HarmonyPrefix,HarmonyPatch(typeof(Menu),"Update")]
        private static bool MenuInput()=>!CraftingWindow.BlocksMenu;
        [HarmonyPrefix,HarmonyPatch(typeof(SettingsUi),nameof(SettingsUi.Open))]
        private static void Settings()=>CraftingWindow.Close(true);
    }
}
