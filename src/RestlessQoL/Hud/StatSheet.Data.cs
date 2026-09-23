using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RestlessQoL.Core;
using UnityEngine;

namespace RestlessQoL.HudTweaks;

public sealed partial class StatSheet
{
    // Read the slots the game actually uses, including ExtraSlots' equipped items.
    // An equipped ammo stack is not body armour even if its SharedData has m_armor.
    private static readonly FieldInfo[] ArmorSlots = SlotFields("m_chestItem", "m_legItem", "m_helmetItem", "m_shoulderItem");
    private static readonly FieldInfo[] EitrSlots = SlotFields("m_chestItem", "m_legItem", "m_helmetItem", "m_shoulderItem", "m_leftItem", "m_rightItem", "m_utilityItem");
    private static readonly FieldInfo[] GearSlots = SlotFields("m_rightItem", "m_leftItem", "m_chestItem", "m_legItem",
        "m_helmetItem", "m_shoulderItem", "m_utilityItem", "m_trinketItem");

    private static FieldInfo[] SlotFields(params string[] names)
    {
        var fields = new List<FieldInfo>();
        foreach (var name in names)
        {
            var field = AccessTools.Field(typeof(Player), name);
            if (field != null) fields.Add(field);
        }
        return fields.ToArray();
    }

    private static List<ItemDrop.ItemData> Items(Player player, FieldInfo[] fields)
    {
        var items = new List<ItemDrop.ItemData>();
        foreach (var field in fields)
            if (field.GetValue(player) is ItemDrop.ItemData item && item.m_shared != null && !items.Contains(item))
                items.Add(item);
        return items;
    }

    private static List<StatRow> Collect(Player player)
    {
        var rows = new List<StatRow>();
        var equipped = Items(player, GearSlots);
        var armorItems = Items(player, ArmorSlots);
        // Public getter includes hidden equipment/set effects as well as HUD buffs.
        var effects = player.GetSEMan().GetStatusEffects();
        var foods = player.GetFoods();

        Pool(rows, "Vitals", "Health", player.GetHealth(), player.GetMaxHealth(), foods, f => f.m_health);
        Pool(rows, "Vitals", "Stamina", player.GetStamina(), player.GetMaxStamina(), foods, f => f.m_stamina);
        if (player.GetMaxEitr() > 0.01f)
            Pool(rows, "Vitals", "Eitr", player.GetEitr(), player.GetMaxEitr(), foods, f => f.m_eitr);
        if (player.GetMaxAdrenaline() > 0.01f)
            Add(rows, "Vitals", "Adrenaline", Pair(player.GetAdrenaline(), player.GetMaxAdrenaline()),
                note: "Current / maximum, as reported by the player.");

        Sets(rows, equipped, effects);

        var armor = 0f;
        var armorParts = new List<Part>();
        foreach (var item in armorItems)
        {
            var amount = item.GetArmor();
            armor += amount;
            if (LoadoutMath.Changed(amount)) armorParts.Add(new Part(Name(item), Signed(amount)));
        }
        foreach (var effect in effects)
        {
            if (effect is not SE_Stats stats) continue;
            var before = armor;
            armor = LoadoutMath.Armor(armor, stats.m_addArmor, stats.m_armorMultiplier);
            if (LoadoutMath.Changed(armor - before))
                armorParts.Add(new Part(Source(effect, equipped), Signed(armor - before) + " (" +
                    Signed(stats.m_addArmor) + ", ×" + N(1f + stats.m_armorMultiplier) + ")"));
        }
        var liveArmor = player.GetBodyArmor();
        Reconcile(armorParts, liveArmor, armor);
        Add(rows, "Defence", "Armour", N(liveArmor), armorParts,
            "Worn armour first; active effects apply in game order. Each effect shows its net contribution, then its flat and multiplier terms.");

        var blocker = player.GetCurrentBlocker();
        if (blocker?.m_shared != null && blocker.m_shared.m_blockable)
        {
            var baseBlock = blocker.GetBaseBlockPower();
            var block = blocker.GetBlockPower(player.GetSkillFactor(Skills.SkillType.Blocking));
            Add(rows, "Defence", "Block armour", N(block), new List<Part>
            {
                new(Name(blocker), Signed(baseBlock)), new("Blocking skill", Signed(block - baseBlock))
            }, "Normal block power before timed parry, incoming-hit conditions and stagger limits.");
        }

        var carry = player.GetMaxCarryWeight();
        var carryParts = new List<Part>();
        var carryEffects = 0f;
        foreach (var effect in effects)
            if (effect is SE_Stats stats && LoadoutMath.Changed(stats.m_addMaxCarryWeight))
            {
                carryEffects += stats.m_addMaxCarryWeight;
                carryParts.Add(new Part(Source(effect, equipped), Signed(stats.m_addMaxCarryWeight)));
            }
        carryParts.Insert(0, new Part("Base / other capacity", N(carry - carryEffects)));
        Add(rows, "Movement & capacity", "Carried weight", Pair(player.GetInventory().GetTotalWeight(), carry), carryParts,
            "Current carried weight / maximum capacity. Sources below explain capacity, not the weight of individual items.");

        var movementParts = new List<Part>();
        var movement = 0f;
        foreach (var item in equipped)
        {
            var value = item.m_shared.m_movementModifier;
            movement += value;
            if (LoadoutMath.Changed(value)) movementParts.Add(new Part(Name(item), Pct(value)));
        }
        var liveMovement = player.GetEquipmentMovementModifier();
        Reconcile(movementParts, liveMovement, movement, true);
        Add(rows, "Movement & capacity", "Equipment movement", Pct(liveMovement), movementParts,
            "Equipment contribution. Running skill, terrain, wind, encumbrance and temporary speed effects are separate conditions.");
        var speedParts = new List<Part>();
        var speed = 0f;
        foreach (var effect in effects)
            if (effect is SE_Stats stats && LoadoutMath.Changed(stats.m_speedModifier))
            {
                speed += stats.m_speedModifier;
                speedParts.Add(new Part(Source(effect, equipped), Pct(stats.m_speedModifier)));
            }
        if (speedParts.Count > 0)
            Add(rows, "Movement & capacity", "Effect speed bonus", Pct(speed), speedParts,
                "Bonuses relative to base movement speed. Directional wind and action-specific movement are not a constant speed bonus.");

        // Attack modifiers are multiplicative in SE_Stats.ModifyAttack, with the
        // general multiplier restricted to m_modifyAttackSkill. Never run a fake
        // attack: third-party overrides may consume charges or trigger procs.
        var weapon = player.GetCurrentWeapon();
        var skill = weapon?.m_shared?.m_skillType ?? Skills.SkillType.Unarmed;
        foreach (var type in DamageTypes)
        {
            var factor = 1f;
            var parts = new List<Part>();
            foreach (var effect in effects)
            {
                if (effect is not SE_Stats stats) continue;
                var general = stats.m_modifyAttackSkill == Skills.SkillType.All || stats.m_modifyAttackSkill == skill
                    ? stats.m_damageModifier : 1f;
                var bonus = Damage(stats.m_percentigeDamageModifiers, type);
                if (!LoadoutMath.Changed(general - 1f) && !LoadoutMath.Changed(bonus)) continue;
                var before = factor;
                factor = LoadoutMath.Damage(factor, general, bonus);
                parts.Add(new Part(Source(effect, equipped), "×" + N(general * (1f + bonus)) +
                    "  (" + Pct(factor - before) + " net)"));
            }
            if (parts.Count > 0)
                Add(rows, "Attack bonuses", TypeName(type), Pct(factor - 1f), parts,
                    "For " + SkillName(skill) + (weapon != null ? " · " + Name(weapon) : "") +
                    ". Active equipment, set and temporary stat bonuses combine by multiplication. " +
                    "This modifies that damage type; it does not add damage a weapon lacks. " +
                    "Weapon damage, skill damage range, attack/combo, world difficulty, target resistance and hit-triggered effects are not part of this percentage.");
        }

        // Include skill-restricted bonuses even when another weapon is held.
        var otherSkills = new HashSet<Skills.SkillType>();
        foreach (var effect in effects)
            if (effect is SE_Stats stats && stats.m_modifyAttackSkill != Skills.SkillType.None &&
                stats.m_modifyAttackSkill != Skills.SkillType.All && stats.m_modifyAttackSkill != skill &&
                LoadoutMath.Changed(stats.m_damageModifier - 1f)) otherSkills.Add(stats.m_modifyAttackSkill);
        foreach (var other in SortedSkills(otherSkills))
        {
            var factor = 1f;
            var parts = new List<Part>();
            foreach (var effect in effects)
                if (effect is SE_Stats stats && stats.m_modifyAttackSkill == other && LoadoutMath.Changed(stats.m_damageModifier - 1f))
                {
                    factor *= stats.m_damageModifier;
                    parts.Add(new Part(Source(effect, equipped), "×" + N(stats.m_damageModifier)));
                }
            Add(rows, "Other weapon bonuses", SkillName(other), Pct(factor - 1f), parts,
                "Applies when using this skill. Multiplies with damage-type bonuses; not active for the weapon currently held.");
        }

        Regeneration(rows, player, equipped, effects, foods);
        SkillBonuses(rows, player, equipped, effects);

        foreach (var type in DamageTypes)
        {
            var mod = player.GetDamageModifier(type);
            var parts = new List<Part>();
            // Same four slots as Player.ApplyArmorDamageMods; no ammunition,
            // weapons, or arbitrary default SharedData values.
            foreach (var item in armorItems)
                foreach (var pair in item.m_shared.m_damageModifiers ?? new List<HitData.DamageModPair>())
                    if (pair.m_type == type && pair.m_modifier != HitData.DamageModifier.Normal)
                        parts.Add(new Part(Name(item), ModName(pair.m_modifier)));
            foreach (var effect in effects)
                if (effect is SE_Stats stats && stats.m_mods != null)
                    foreach (var pair in stats.m_mods)
                        if (pair.m_type == type && pair.m_modifier != HitData.DamageModifier.Normal)
                            parts.Add(new Part(Source(effect, equipped), ModName(pair.m_modifier)));
            if (mod != HitData.DamageModifier.Normal || parts.Count > 0)
                Add(rows, "Resistances", TypeName(type), ModName(mod), parts,
                    "The total is the player's effective resistance. Listed sources are candidates; vanilla resistance priority rules decide the result, so they do not simply add. Other mods can change those rules.");
        }
        foreach (var effect in effects)
        {
            var copy = Clean(effect.GetTooltipString());
            Add(rows, "Active effects", SeName(effect), "Active", new List<Part> { new("Source", Source(effect, equipped)) },
                copy.Length > 0 ? copy : "Active effect; no description supplied.", id: effect.NameHash().ToString());
        }
        return rows;
    }

    private static void Regeneration(List<StatRow> rows, Player player, List<ItemDrop.ItemData> gear,
        List<StatusEffect> effects, List<Player.Food> foods)
    {
        var foodRegen = 0f;
        var foodParts = new List<Part>();
        foreach (var food in foods)
        {
            var value = food.m_item.m_shared.m_foodRegen;
            foodRegen += value;
            if (LoadoutMath.Changed(value)) foodParts.Add(new Part(Name(food.m_item), Signed(value)));
        }
        var health = 1f;
        var stamina = 1f;
        var eitr = 1f;
        player.GetSEMan().ModifyHealthRegen(ref health);
        player.GetSEMan().ModifyStaminaRegen(ref stamina);
        player.GetSEMan().ModifyEitrRegen(ref eitr);
        RegenRow(rows, "Health recovery", health, gear, effects, s => s.m_healthRegenMultiplier);
        RegenRow(rows, "Stamina recovery", stamina, gear, effects, s => s.m_staminaRegenMultiplier);
        var equipmentEitr = player.GetEquipmentEitrRegenModifier();
        var eitrParts = RegenParts(effects, gear, s => s.m_eitrRegenMultiplier, out var predicted);
        var accountedEitr = 0f;
        foreach (var item in Items(player, EitrSlots))
        {
            var contribution = item.m_shared.m_eitrRegenModifier;
            accountedEitr += contribution;
            if (LoadoutMath.Changed(contribution)) eitrParts.Add(new Part(Name(item), Pct(contribution)));
        }
        Reconcile(eitrParts, eitr + equipmentEitr, predicted + accountedEitr, true);
        if (player.GetMaxEitr() > 0f || eitrParts.Count > 0)
            Add(rows, "Recovery", "Eitr recovery", Pct(eitr + equipmentEitr - 1f), eitrParts,
                "Relative to normal recovery. Equipment is added after active effects, as in the player's recovery calculation.");
        if (foodParts.Count > 0)
        {
            foodParts.Add(new Part("Active health recovery", "×" + N(health)));
            Add(rows, "Recovery", "Food healing / tick", N(foodRegen * health), foodParts,
                "Food healing multiplied by active recovery effects. This is healing per food tick, not per second. Direct potion healing is shown under Active effects.");
        }
    }

    private static List<Part> RegenParts(List<StatusEffect> effects, List<ItemDrop.ItemData> gear,
        Func<SE_Stats, float> take, out float predicted)
    {
        predicted = 1f;
        var parts = new List<Part>();
        foreach (var effect in effects)
            if (effect is SE_Stats stats)
            {
                var multiplier = take(stats);
                var before = predicted;
                predicted = LoadoutMath.Regen(predicted, multiplier);
                if (LoadoutMath.Changed(predicted - before))
                    parts.Add(new Part(Source(effect, gear), Pct(predicted - before) +
                        (multiplier > 1f ? " (additive)" : " (×" + N(multiplier) + ")")));
            }
        return parts;
    }

    private static void RegenRow(List<StatRow> rows, string title, float actual, List<ItemDrop.ItemData> gear,
        List<StatusEffect> effects, Func<SE_Stats, float> take)
    {
        var parts = RegenParts(effects, gear, take, out var predicted);
        Reconcile(parts, actual, predicted, true);
        Add(rows, "Recovery", title, Pct(actual - 1f), parts,
            "Relative to normal recovery. Positive recovery bonuses add; reductions multiply in active-effect order. Activity and regeneration delays can still pause recovery.");
    }

    private static void SkillBonuses(List<StatRow> rows, Player player, List<ItemDrop.ItemData> gear, List<StatusEffect> effects)
    {
        var skills = new HashSet<Skills.SkillType>();
        foreach (var effect in effects)
            if (effect is SE_Stats stats && stats.m_skillLevel != Skills.SkillType.None)
            {
                skills.Add(stats.m_skillLevel);
                if (stats.m_skillLevel2 != Skills.SkillType.None) skills.Add(stats.m_skillLevel2);
            }
        if (skills.Remove(Skills.SkillType.All))
            foreach (var known in player.GetSkills().GetSkillList()) skills.Add(known.m_info.m_skill);
        foreach (var skill in SortedSkills(skills))
        {
            var parts = new List<Part>();
            var bonus = 0f;
            foreach (var effect in effects)
            {
                if (effect is not SE_Stats stats || stats.m_skillLevel == Skills.SkillType.None) continue;
                var amount = 0f;
                if (stats.m_skillLevel == skill || stats.m_skillLevel == Skills.SkillType.All) amount += stats.m_skillLevelModifier;
                if (stats.m_skillLevel2 == skill || stats.m_skillLevel2 == Skills.SkillType.All) amount += stats.m_skillLevelModifier2;
                bonus += amount;
                if (LoadoutMath.Changed(amount)) parts.Add(new Part(Source(effect, gear), Signed(amount)));
            }
            var value = player.GetSkillFactor(skill) * 100f;
            var trained = player.GetSkills().GetSkillList().Find(s => s.m_info.m_skill == skill)?.m_level ?? 0f;
            parts.Insert(0, new Part("Trained level", N(trained)));
            Reconcile(parts, value, trained + bonus);
            Add(rows, "Modified skills", SkillName(skill), N(value), parts,
                "Effective skill level reported by the game. Bonuses affect the relevant skill's calculations, not a universal damage percentage.");
        }
    }

    private static List<Skills.SkillType> SortedSkills(HashSet<Skills.SkillType> skills)
    {
        var list = new List<Skills.SkillType>(skills);
        list.Sort((a, b) => ((int)a).CompareTo((int)b));
        return list;
    }

    private static void Sets(List<StatRow> rows, List<ItemDrop.ItemData> gear, List<StatusEffect> effects)
    {
        var seen = new HashSet<string>();
        foreach (var item in gear)
        {
            var shared = item.m_shared;
            var effect = shared.m_setStatusEffect;
            if (effect == null || string.IsNullOrEmpty(shared.m_setName) || !seen.Add(shared.m_setName)) continue;
            var count = 0;
            var parts = new List<Part>();
            foreach (var piece in gear)
                if (piece.m_shared.m_setName == shared.m_setName)
                {
                    count++;
                    parts.Add(new Part(Name(piece), "Equipped"));
                }
            var active = effects.Exists(e => e != null && e.NameHash() == effect.NameHash());
            Add(rows, "Equipment sets", SeName(effect), count + "/" + shared.m_setSize, parts,
                (active ? "Active. Counted once in totals, not once per set piece." :
                    "Inactive. The bonus printed on the item is not contributing to live totals.") +
                "\n\n" + Clean(effect.GetTooltipString()), id: shared.m_setName);
            var row = rows[rows.Count - 1];
            row.SetRequired = shared.m_setSize;
            row.SetEquipped = count;
            row.SetActive = active;

        }
    }

    private static void Pool(List<StatRow> rows, string group, string label, float now, float max,
        List<Player.Food> foods, Func<Player.Food, float> take)
    {
        var parts = new List<Part>();
        var sum = 0f;
        foreach (var food in foods)
        {
            var amount = take(food);
            sum += amount;
            if (LoadoutMath.Changed(amount)) parts.Add(new Part(Name(food.m_item), Signed(amount)));
        }
        parts.Insert(0, new Part("Base / other maximum", N(max - sum)));
        Add(rows, group, label, Pair(now, max), parts,
            "Current / maximum. Food contributions use their current values, including decay. Base / other includes any remaining player or mod contribution.");
    }

    private static void Reconcile(List<Part> parts, float actual, float predicted, bool percentage = false)
    {
        var difference = LoadoutMath.Residual(actual, predicted);
        if (LoadoutMath.Changed(difference))
            parts.Add(new Part("Other / game adjustment", percentage ? Pct(difference) : Signed(difference)));
    }

    private static void Add(List<StatRow> rows, string group, string label, string value,
        List<Part>? parts = null, string note = "", string id = "") => rows.Add(new StatRow
        { Group = group, Label = label, Value = value, Parts = parts ?? new List<Part>(), Note = note, Id = id });

    private static string Source(StatusEffect effect, List<ItemDrop.ItemData> gear)
    {
        var names = new List<string>();
        var hash = effect.NameHash();
        foreach (var item in gear)
        {
            var shared = item.m_shared;
            if (shared.m_setStatusEffect != null && shared.m_setStatusEffect.NameHash() == hash)
                return SeName(effect) + " (set)";
            if (shared.m_equipStatusEffect != null && shared.m_equipStatusEffect.NameHash() == hash && !names.Contains(Name(item)))
                names.Add(Name(item));
        }
        return names.Count > 0 ? string.Join(" / ", names) : SeName(effect);
    }

    private static string Clean(string copy) => RestlessUi.Bare(Localization.instance != null ? Localization.instance.Localize(copy) : copy);
    private static string Name(ItemDrop.ItemData item) => Clean(item.m_shared.m_name);
    private static string SeName(StatusEffect effect) => Clean(effect.m_name);
    private static string SkillName(Skills.SkillType skill)
    {
        var token = "$skill_" + skill.ToString().ToLowerInvariant();
        var name = Clean(token);
        return name == token || name.Contains("skill_") ? skill.ToString() : name;
    }
    private static string Pair(float now, float max) => N(now) + " / " + N(max);
    private static string N(float value) => (Mathf.Abs(value) < 0.005f ? 0f : value).ToString("0.##");
    private static string Signed(float value) => (value > 0.0001f ? "+" : "") + N(value);
    private static string Pct(float value) => Signed(value * 100f) + "%";
    private static string ModName(HitData.DamageModifier mod) => mod switch
    {
        HitData.DamageModifier.SlightlyResistant => "Slightly resistant",
        HitData.DamageModifier.Resistant => "Resistant",
        HitData.DamageModifier.VeryResistant => "Very resistant",
        HitData.DamageModifier.SlightlyWeak => "Slightly weak",
        HitData.DamageModifier.Weak => "Weak",
        HitData.DamageModifier.VeryWeak => "Very weak",
        HitData.DamageModifier.Immune => "Immune",
        HitData.DamageModifier.Ignore => "Ignore",
        _ => mod.ToString()
    };
    private static string TypeName(HitData.DamageType type) => type.ToString();
    private static float Damage(HitData.DamageTypes damage, HitData.DamageType type) => type switch
    {
        HitData.DamageType.Slash => damage.m_slash,
        HitData.DamageType.Pierce => damage.m_pierce,
        HitData.DamageType.Blunt => damage.m_blunt,
        HitData.DamageType.Chop => damage.m_chop,
        HitData.DamageType.Pickaxe => damage.m_pickaxe,
        HitData.DamageType.Fire => damage.m_fire,
        HitData.DamageType.Frost => damage.m_frost,
        HitData.DamageType.Lightning => damage.m_lightning,
        HitData.DamageType.Poison => damage.m_poison,
        HitData.DamageType.Spirit => damage.m_spirit,
        _ => 0f
    };
    private static readonly HitData.DamageType[] DamageTypes =
    {
        HitData.DamageType.Slash, HitData.DamageType.Pierce, HitData.DamageType.Blunt,
        HitData.DamageType.Chop, HitData.DamageType.Pickaxe, HitData.DamageType.Fire,
        HitData.DamageType.Frost, HitData.DamageType.Lightning, HitData.DamageType.Poison, HitData.DamageType.Spirit
    };
    private sealed class StatRow
    {
        public string Group = "";
        public string Label = "";
        public string Value = "";
        public string Note = "";
        public string Id = "";
        public int SetRequired, SetEquipped;
        public bool SetActive;
        public string Key => Group + "/" + (Id.Length > 0 ? Id : Label);
        public List<Part> Parts = new();
    }
    private readonly struct Part
    {
        public readonly string Name;
        public readonly string Value;
        public Part(string name, string value) { Name = name; Value = value; }
    }
}
