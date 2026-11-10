using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;
using srd5;

/// <summary>
/// The "Library" of the main menu: browse the items and spells implemented in libsrd5.
/// Engine independent (shared by the Unity, Godot and Blazor examples): the screen shows
/// MenuText in its menu area and MainText in its main text area and forwards number keys
/// to HandleKey.
/// </summary>
public class Library {
    private enum Menu {
        ROOT,
        ITEMS,
        SPELLS,
        LIST
    }

    private class Entry {
        public string Name;
        public Func<string> Details;

        public Entry(string name, Func<string> details) {
            Name = name;
            Details = details;
        }
    }

    private const int PAGE_SIZE = 8; // keys 1-8, 9 = next page, 0 = back

    private const string ROOT_TEXT =
@"Library

1. Items
2. Spells
0. Back";

    private const string ITEMS_TEXT =
@"Library - Items

1. Weapons
2. Armor
3. Consumables
0. Back";

    private const string SPELLS_TEXT =
@"Library - Spells

1. Druid
2. Wizard
0. Back";

    private const string INTRO_TEXT =
@"Browse the items and spells implemented in libsrd5.

Select a category on the left, then an entry to see its details here.";

    private Menu menu = Menu.ROOT;
    private Menu listParent = Menu.ROOT;
    private string listTitle = "";
    private List<Entry> entries = new List<Entry>();
    private int page = 0;

    public string MenuText { get; private set; }
    public string MainText { get; private set; }

    public Library() {
        MenuText = ROOT_TEXT;
        MainText = INTRO_TEXT;
    }

    /// <summary>Handles a number key. Returns false when the player leaves the library.</summary>
    public bool HandleKey(int digit) {
        switch (menu) {
            case Menu.ROOT:
                if (digit == 0) return false;
                if (digit == 1) showMenu(Menu.ITEMS, ITEMS_TEXT);
                else if (digit == 2) showMenu(Menu.SPELLS, SPELLS_TEXT);
                break;
            case Menu.ITEMS:
                if (digit == 0) showMenu(Menu.ROOT, ROOT_TEXT);
                else if (digit == 1) showList("Weapons", Menu.ITEMS, weaponEntries());
                else if (digit == 2) showList("Armor", Menu.ITEMS, armorEntries());
                else if (digit == 3) showList("Consumables", Menu.ITEMS, consumableEntries());
                break;
            case Menu.SPELLS:
                if (digit == 0) showMenu(Menu.ROOT, ROOT_TEXT);
                else if (digit == 1) showList("Druid Spells", Menu.SPELLS, spellEntries(DRUID_SPELLS));
                else if (digit == 2) showList("Wizard Spells", Menu.SPELLS, spellEntries(WIZARD_SPELLS));
                break;
            case Menu.LIST:
                if (digit == 0) {
                    showMenu(listParent, listParent == Menu.ITEMS ? ITEMS_TEXT : SPELLS_TEXT);
                } else if (digit == 9) {
                    page = (page + 1) % pageCount();
                    MenuText = listText();
                } else {
                    int index = page * PAGE_SIZE + digit - 1;
                    if (index < entries.Count) MainText = entries[index].Details();
                }
                break;
        }
        return true;
    }

    private void showMenu(Menu target, string text) {
        menu = target;
        MenuText = text;
        MainText = INTRO_TEXT;
    }

    private void showList(string title, Menu parent, List<Entry> list) {
        menu = Menu.LIST;
        listParent = parent;
        listTitle = title;
        entries = list;
        page = 0;
        MenuText = listText();
        MainText = INTRO_TEXT;
    }

    private int pageCount() {
        return Math.Max(1, (entries.Count + PAGE_SIZE - 1) / PAGE_SIZE);
    }

    private string listText() {
        StringBuilder text = new StringBuilder();
        text.AppendFormat("{0} (page {1}/{2})\n\n", listTitle, page + 1, pageCount());
        for (int i = 0; i < PAGE_SIZE; i++) {
            int index = page * PAGE_SIZE + i;
            if (index >= entries.Count) break;
            text.AppendFormat("{0}. {1}\n", i + 1, entries[index].Name);
        }
        if (pageCount() > 1) text.Append("9. Next page\n");
        text.Append("0. Back");
        return text.ToString();
    }

    // ---------- Items ----------

    private static List<Entry> weaponEntries() {
        List<Entry> list = new List<Entry>();
        foreach (Weapon weapon in StaticMembers<Weapon>(typeof(Weapons))) {
            if (weapon.Name == "Claws") continue; // natural weapon of monsters
            Weapon w = weapon;
            list.Add(new Entry(w.Name, () => weaponDetails(w)));
        }
        return list;
    }

    private static List<Entry> armorEntries() {
        List<Entry> list = new List<Entry>();
        foreach (Armor armor in StaticMembers<Armor>(typeof(Armors))) {
            if (armor.Name == "Mage armor") continue; // created by the spell
            Armor a = armor;
            list.Add(new Entry(a.Name, () => armorDetails(a)));
        }
        foreach (Shield shield in StaticMembers<Shield>(typeof(Shields))) {
            Shield s = shield;
            list.Add(new Entry(s.Name, () => shieldDetails(s)));
        }
        return list;
    }

    private static List<Entry> consumableEntries() {
        List<Entry> list = new List<Entry>();
        foreach (Consumable consumable in StaticMembers<Consumable>(typeof(Potions))) {
            Consumable c = consumable;
            list.Add(new Entry(c.Name, () => consumableDetails(c)));
        }
        foreach (Usable usable in StaticMembers<Usable>(typeof(Wands))) {
            Usable u = usable;
            list.Add(new Entry(u.Name, () => usableDetails(u)));
        }
        return list;
    }

    private static string weaponDetails(Weapon weapon) {
        StringBuilder text = header(weapon);
        text.AppendFormat("Damage:      {0} {1}\n", weapon.Damage.Dice, weapon.Damage.Type.Name());
        if (weapon.RangeNormal > 0)
            text.AppendFormat("Range:       {0} / {1} ft\n", weapon.RangeNormal, weapon.RangeLong);
        text.AppendFormat("Reach:       {0} ft\n", weapon.Reach);
        text.AppendFormat("Properties:  {0}\n", join(weapon.Properties));
        footer(text, weapon);
        return text.ToString();
    }

    private static string armorDetails(Armor armor) {
        StringBuilder text = header(armor);
        text.AppendFormat("Armor Class: {0}\n", armor.AC);
        string dex = armor.MaxDexBonus >= 10 ? "full" : armor.MaxDexBonus == 0 ? "none" : "max +" + armor.MaxDexBonus;
        text.AppendFormat("Dex bonus:   {0}\n", dex);
        if (armor.Strength > 0) text.AppendFormat("Strength:    {0} required\n", armor.Strength);
        text.AppendFormat("Properties:  {0}\n", join(armor.Properties));
        footer(text, armor);
        return text.ToString();
    }

    private static string shieldDetails(Shield shield) {
        StringBuilder text = header(shield);
        text.AppendFormat("Armor Class: +{0}\n", shield.AC);
        footer(text, shield);
        return text.ToString();
    }

    private static string consumableDetails(Consumable consumable) {
        StringBuilder text = header(consumable);
        text.AppendFormat("Charges:     {0}\n", consumable.Charges);
        footer(text, consumable);
        return text.ToString();
    }

    private static string usableDetails(Usable usable) {
        StringBuilder text = header(usable);
        text.AppendFormat("Charges:     {0} (max. {1} per use)\n", usable.Charges, usable.MaximumChargePerUse);
        text.AppendFormat("Targets:     up to {0}\n", usable.MaximumTargets);
        footer(text, usable);
        return text.ToString();
    }

    private static StringBuilder header(Item item) {
        StringBuilder text = new StringBuilder();
        text.AppendFormat("{0}\n{1}, {2}\n\n", item.Name, pretty(item.Type), pretty(item.Rarity));
        return text;
    }

    private static void footer(StringBuilder text, Item item) {
        if (item.Proficiencies != null && item.Proficiencies.Length > 0) {
            List<string> names = new List<string>();
            foreach (Proficiency proficiency in item.Proficiencies) names.Add(proficiency.Name());
            text.AppendFormat("Proficiency: {0}\n", string.Join(", ", names.ToArray()));
        }
        text.AppendFormat("Weight:      {0} lb\n", item.Weight.ToString(CultureInfo.InvariantCulture));
        if (item.Value > 0) text.AppendFormat("Value:       {0}\n", coins(item.Value));
    }

    /// <summary>libsrd5 item values are in copper pieces.</summary>
    private static string coins(int copper) {
        if (copper % 100 == 0) return (copper / 100) + " gp";
        if (copper % 10 == 0) return (copper / 10) + " sp";
        return copper + " cp";
    }

    // ---------- Spells ----------

    // SRD 5.1 class spell lists, cantrips to 2nd level. Only spells libsrd5 implements are shown.
    private static readonly Spells.ID[] DRUID_SPELLS = {
        Spells.ID.DRUIDCRAFT, Spells.ID.GUIDANCE, Spells.ID.MENDING, Spells.ID.POISON_SPRAY, Spells.ID.PRODUCE_FLAME,
        Spells.ID.RESISTANCE, Spells.ID.SHILLELAGH,
        Spells.ID.CHARM_PERSON, Spells.ID.CREATE_OR_DESTROY_WATER, Spells.ID.CURE_WOUNDS, Spells.ID.DETECT_MAGIC,
        Spells.ID.DETECT_POISON_AND_DISEASE, Spells.ID.ENTANGLE, Spells.ID.FAERIE_FIRE, Spells.ID.FOG_CLOUD,
        Spells.ID.GOODBERRY, Spells.ID.HEALING_WORD, Spells.ID.JUMP, Spells.ID.LONGSTRIDER, Spells.ID.PURIFY_FOOD_AND_DRINK,
        Spells.ID.SPEAK_WITH_ANIMALS, Spells.ID.THUNDERWAVE,
        Spells.ID.ANIMAL_MESSENGER, Spells.ID.BARKSKIN, Spells.ID.DARKVISION, Spells.ID.ENHANCE_ABILITY, Spells.ID.FIND_TRAPS,
        Spells.ID.FLAME_BLADE, Spells.ID.FLAMING_SPHERE, Spells.ID.GUST_OF_WIND, Spells.ID.HEAT_METAL, Spells.ID.HOLD_PERSON,
        Spells.ID.LESSER_RESTORATION, Spells.ID.LOCATE_ANIMALS_OR_PLANTS, Spells.ID.LOCATE_OBJECT, Spells.ID.MOONBEAM,
        Spells.ID.PASS_WITHOUT_TRACE, Spells.ID.PROTECTION_FROM_POISON, Spells.ID.SPIKE_GROWTH
    };

    private static readonly Spells.ID[] WIZARD_SPELLS = {
        Spells.ID.ACID_SPLASH, Spells.ID.CHILL_TOUCH, Spells.ID.DANCING_LIGHTS, Spells.ID.FIRE_BOLT, Spells.ID.LIGHT,
        Spells.ID.MAGE_HAND, Spells.ID.MENDING, Spells.ID.MESSAGE, Spells.ID.MINOR_ILLUSION, Spells.ID.POISON_SPRAY,
        Spells.ID.PRESTIDIGITATION, Spells.ID.RAY_OF_FROST, Spells.ID.SHOCKING_GRASP,
        Spells.ID.ALARM, Spells.ID.BURNING_HANDS, Spells.ID.CHARM_PERSON, Spells.ID.COLOR_SPRAY, Spells.ID.COMPREHEND_LANGUAGES,
        Spells.ID.DETECT_MAGIC, Spells.ID.DISGUISE_SELF, Spells.ID.EXPEDITIOUS_RETREAT, Spells.ID.FALSE_LIFE,
        Spells.ID.FEATHER_FALL, Spells.ID.FIND_FAMILIAR, Spells.ID.FLOATING_DISK, Spells.ID.FOG_CLOUD, Spells.ID.GREASE,
        Spells.ID.HIDEOUS_LAUGHTER, Spells.ID.IDENTIFY, Spells.ID.ILLUSORY_SCRIPT, Spells.ID.JUMP, Spells.ID.LONGSTRIDER,
        Spells.ID.MAGE_ARMOR, Spells.ID.MAGIC_MISSILE, Spells.ID.PROTECTION_FROM_EVIL_AND_GOOD, Spells.ID.SHIELD,
        Spells.ID.SILENT_IMAGE, Spells.ID.SLEEP, Spells.ID.THUNDERWAVE, Spells.ID.UNSEEN_SERVANT,
        Spells.ID.ACID_ARROW, Spells.ID.ALTER_SELF, Spells.ID.ARCANE_LOCK, Spells.ID.ARCANISTS_MAGIC_AURA,
        Spells.ID.BLINDNESS_DEAFNESS, Spells.ID.BLUR, Spells.ID.CONTINUAL_FLAME, Spells.ID.DARKNESS, Spells.ID.DARKVISION,
        Spells.ID.DETECT_THOUGHTS, Spells.ID.ENLARGE_REDUCE, Spells.ID.FLAMING_SPHERE, Spells.ID.GENTLE_REPOSE,
        Spells.ID.GUST_OF_WIND, Spells.ID.HOLD_PERSON, Spells.ID.INVISIBILITY, Spells.ID.KNOCK, Spells.ID.LEVITATE,
        Spells.ID.LOCATE_OBJECT, Spells.ID.MAGIC_MOUTH, Spells.ID.MAGIC_WEAPON, Spells.ID.MIRROR_IMAGE, Spells.ID.MISTY_STEP,
        Spells.ID.RAY_OF_ENFEEBLEMENT, Spells.ID.ROPE_TRICK, Spells.ID.SCORCHING_RAY, Spells.ID.SEE_INVISIBILITY,
        Spells.ID.SHATTER, Spells.ID.SPIDER_CLIMB, Spells.ID.SUGGESTION, Spells.ID.WEB
    };

    private static Dictionary<Spells.ID, Spell> implementedSpells;

    private static List<Entry> spellEntries(Spells.ID[] classList) {
        if (implementedSpells == null) {
            implementedSpells = new Dictionary<Spells.ID, Spell>();
            foreach (Spell spell in StaticMembers<Spell>(typeof(Spells)))
                implementedSpells[spell.ID] = spell;
        }
        List<Spell> spells = new List<Spell>();
        foreach (Spells.ID id in classList) {
            Spell spell;
            if (implementedSpells.TryGetValue(id, out spell) && spell.Level <= SpellLevel.SECOND)
                spells.Add(spell);
        }
        // cantrips first, then by level and name
        spells.Sort((a, b) => a.Level != b.Level ? a.Level.CompareTo(b.Level) : string.CompareOrdinal(a.Name, b.Name));
        List<Entry> list = new List<Entry>();
        foreach (Spell spell in spells) {
            Spell s = spell;
            string level = s.Level == SpellLevel.CANTRIP ? "C" : ((int)s.Level).ToString();
            list.Add(new Entry(String.Format("[{0}] {1}", level, s.Name), () => spellDetails(s)));
        }
        return list;
    }

    private static string spellDetails(Spell spell) {
        StringBuilder text = new StringBuilder();
        string level = spell.Level == SpellLevel.CANTRIP ? "Cantrip" : "Level " + (int)spell.Level;
        text.AppendFormat("{0}\n{1} {2}\n\n", spell.Name, level, pretty(spell.School));
        text.AppendFormat("Casting time: {0}\n", pretty(spell.CastingTime));
        text.AppendFormat("Range:        {0}\n", spell.Range > 0 ? spell.Range + " ft" : "Self / touch");
        text.AppendFormat("Components:   {0}\n", join(spell.Components));
        text.AppendFormat("Duration:     {0}\n", pretty(spell.Duration));
        if (spell.AreaOfEffect > 0) text.AppendFormat("Area:         {0} ft\n", spell.AreaOfEffect);
        if (spell.MaximumTargets > 0) text.AppendFormat("Targets:      up to {0}\n", spell.MaximumTargets);
        text.AppendFormat("\n{0}\n", spell.ID.Description());
        return text.ToString();
    }

    // ---------- Helpers ----------

    /// <summary>All public static properties and fields of the given type (e.g. Weapons.Club).</summary>
    private static List<T> StaticMembers<T>(Type container) where T : class {
        List<T> result = new List<T>();
        foreach (PropertyInfo property in container.GetProperties(BindingFlags.Public | BindingFlags.Static)) {
            if (!typeof(T).IsAssignableFrom(property.PropertyType) || property.GetIndexParameters().Length > 0) continue;
            T value = property.GetValue(null, null) as T;
            if (value != null) result.Add(value);
        }
        foreach (FieldInfo field in container.GetFields(BindingFlags.Public | BindingFlags.Static)) {
            if (!typeof(T).IsAssignableFrom(field.FieldType)) continue;
            T value = field.GetValue(null) as T;
            if (value != null) result.Add(value);
        }
        return result;
    }

    /// <summary>ONE_ACTION -> One action</summary>
    private static string pretty(object value) {
        string text = value.ToString().Replace('_', ' ').ToLowerInvariant();
        return text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }

    private static string join<T>(T[] values) {
        if (values == null || values.Length == 0) return "-";
        List<string> names = new List<string>();
        foreach (T value in values) names.Add(pretty(value));
        return string.Join(", ", names.ToArray());
    }
}
