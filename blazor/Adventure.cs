using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using srd5;

// =====================================================================================
//  Adventure mode: a party of three heroes fights a series of increasingly hard battles,
//  earning experience, gold and loot, and buys equipment in between.
//  Engine independent, shared by the Unity, Godot and Blazor examples. The main menu
//  shows MenuText / MainText and forwards number keys to HandleKey.
// =====================================================================================

/// <summary>An enemy type that can appear in adventure fights, with its loot.</summary>
public class EnemyType {
    public string Name;
    public Func<Monster> Create;
    /// <summary>Gold dropped when defeated, as dice ("2d6"), or null for none.</summary>
    public string Gold;
    public LootDrop[] Loot;

    public EnemyType(string name, Func<Monster> create, string gold, params LootDrop[] loot) {
        Name = name;
        Create = create;
        Gold = gold;
        Loot = loot;
    }
}

/// <summary>An item an enemy drops with the given chance (percent).</summary>
public class LootDrop {
    public Func<Item> Create;
    public int Chance;

    public LootDrop(Func<Item> create, int chance) {
        Create = create;
        Chance = chance;
    }
}

/// <summary>One battle of the adventure.</summary>
public class Fight {
    public string Title;
    public EnemyType[] Enemies;

    public Fight(string title, params EnemyType[] enemies) {
        Title = title;
        Enemies = enemies;
    }
}

/// <summary>
/// Everything that defines the adventure. Add or change fights, enemies, loot and spells here.
/// </summary>
public static class AdventureConfig {
    public const int PARTY_SIZE = 3;
    public const int START_GOLD = 200; // gold pieces

    // ----- Enemies and their loot -----
    public static readonly EnemyType Boar = new EnemyType("Boar", () => Monsters.Boar, null); // animals carry no loot
    public static readonly EnemyType Orc = new EnemyType("Orc", () => Monsters.Orc, "2d6",
        new LootDrop(() => Weapons.Javelin, 30), new LootDrop(() => Weapons.Greataxe, 15));
    public static readonly EnemyType Ogre = new EnemyType("Ogre", () => Monsters.Ogre, "4d10",
        new LootDrop(() => Weapons.Greatclub, 25), new LootDrop(() => Potions.PotionOfHealing, 25));

    // ----- The fights, in order of increasing difficulty -----
    public static readonly Fight[] Fights = {
        new Fight("A lonely boar", Boar),
        new Fight("A pack of boars", Boar, Boar, Boar),
        new Fight("An orc with its boars", Orc, Boar, Boar),
        new Fight("Orc hunters", Orc, Orc, Boar, Boar),
        new Fight("An ogre", Ogre),
        new Fight("An ogre and its orc guards", Ogre, Orc, Orc),
        new Fight("Two ogres", Ogre, Ogre),
        new Fight("The ogre warband", Ogre, Ogre, Orc, Orc)
    };

    /// <summary>Experience needed for levels 1, 2, 3, ... (SRD 5.1).</summary>
    public static readonly int[] LEVEL_XP = { 0, 300, 900, 2700, 6500, 14000, 23000, 34000, 48000, 64000 };

    /// <summary>
    /// Spells casters learn and prepare automatically, by spell level. A spell level is learned
    /// as soon as the hero has spell slots for it (e.g. 2nd level spells at character level 3).
    /// Keep at most 9 per class: the combat menu shows 9 spells.
    /// </summary>
    public static Spell[][] SpellsFor(Class clazz) {
        switch (clazz) {
            case Class.DRUID:
                return new Spell[][] {
                    new Spell[] { Spells.Shillelagh, Spells.ProduceFlame },
                    new Spell[] { Spells.CureWounds, Spells.HealingWord, Spells.Thunderwave, Spells.Entangle },
                    new Spell[] { Spells.HoldPerson, Spells.Barkskin, Spells.FlameBlade }
                };
            case Class.WIZARD:
                return new Spell[][] {
                    new Spell[] { Spells.FireBolt, Spells.RayOfFrost },
                    new Spell[] { Spells.MagicMissile, Spells.BurningHands, Spells.Sleep },
                    new Spell[] { Spells.ScorchingRay, Spells.Shatter, Spells.AcidArrow, Spells.HoldPerson }
                };
            default:
                return new Spell[0][];
        }
    }

    // ----- The shop -----
    public static List<Func<Item>> ShopWeapons() {
        List<Func<Item>> items = new List<Func<Item>>();
        foreach (PropertyInfo property in typeof(Weapons).GetProperties(BindingFlags.Public | BindingFlags.Static)) {
            if (property.PropertyType != typeof(Weapon) || property.Name == "Claws") continue;
            PropertyInfo p = property;
            items.Add(() => (Item)p.GetValue(null, null));
        }
        return items;
    }

    public static List<Func<Item>> ShopArmor() {
        List<Func<Item>> items = new List<Func<Item>>();
        foreach (PropertyInfo property in typeof(Armors).GetProperties(BindingFlags.Public | BindingFlags.Static)) {
            if (property.PropertyType != typeof(Armor) || property.Name == "MageArmor") continue;
            PropertyInfo p = property;
            items.Add(() => (Item)p.GetValue(null, null));
        }
        items.Add(() => new Shield("Shield", 2, 1000, 6)); // Shields.Shield is a single shared instance
        return items;
    }

    public static List<Func<Item>> ShopPotions() {
        return new List<Func<Item>> { () => Potions.PotionOfHealing, () => Potions.PotionOfGreaterHealing };
    }
}

/// <summary>Enemy positions on the combat screen (Unity UI coordinates: centre, y up, size).</summary>
public static class EnemyLayout {
    public struct Placement {
        public double X, Y, Size;
        public Placement(double x, double y, double size) { X = x; Y = y; Size = size; }
    }

    /// <summary>Places up to 2 large and 4 medium enemies, in the order given.</summary>
    public static Placement[] Place(Monster[] enemies) {
        int large = 0;
        foreach (Monster enemy in enemies) if (enemy.Size >= Size.LARGE) large++;
        double[][] largePositions = large >= 2 ? new double[][] { new double[] { 200, 143 }, new double[] { 273, -26 } }
            : new double[][] { new double[] { 300, 90 } };
        double[][] mediumPositions;
        if (large >= 2)
            mediumPositions = new double[][] { new double[] { 410, 200 }, new double[] { 420, -60 }, new double[] { 110, 40 }, new double[] { 110, 200 } };
        else if (large == 1)
            mediumPositions = new double[][] { new double[] { 150, 200 }, new double[] { 140, -40 }, new double[] { 430, 210 }, new double[] { 440, -50 } };
        else if (enemies.Length == 1)
            mediumPositions = new double[][] { new double[] { 300, 80 } };
        else
            mediumPositions = new double[][] { new double[] { 220, 170 }, new double[] { 230, -30 }, new double[] { 400, 180 }, new double[] { 410, -20 } };
        Placement[] result = new Placement[enemies.Length];
        int l = 0, m = 0;
        for (int i = 0; i < enemies.Length; i++) {
            if (enemies[i].Size >= Size.LARGE) {
                double[] p = largePositions[Math.Min(l++, largePositions.Length - 1)];
                result[i] = new Placement(p[0], p[1], 288);
            } else {
                double[] p = mediumPositions[Math.Min(m++, mediumPositions.Length - 1)];
                result[i] = new Placement(p[0], p[1], 192);
            }
        }
        return result;
    }
}

public enum AdventureAction {
    NONE,
    /// <summary>Open the character creation screen; the new hero joins Adventure.Current.Party.</summary>
    CREATE_CHARACTER,
    /// <summary>Open the combat screen for the current fight.</summary>
    START_FIGHT,
    /// <summary>Leave adventure mode (back to the main menu).</summary>
    LEAVE
}

public class Adventure {
    /// <summary>The running adventure, or null in free mode.</summary>
    public static Adventure Current;

    private enum Menu {
        CREATE_PARTY,
        HUB,
        SHOP,
        SHOP_LIST,
        SHOP_BUY,
        EQUIP_SELECT_HERO,
        EQUIP_SELECT_ITEM,
        FORMATION,
        CONFIRM_LEAVE,
        GAME_OVER,
        VICTORY
    }

    private const int PAGE_SIZE = 8;

    public List<CharacterSheet> Party = new List<CharacterSheet>();
    /// <summary>Party gold in copper pieces (libsrd5 item values are in copper).</summary>
    public int Copper = AdventureConfig.START_GOLD * 100;
    public int FightIndex = 0;
    /// <summary>True while the combat screen runs a fight of this adventure.</summary>
    public bool InFight = false;

    private bool[] front = { true, false, false };
    private Menu menu = Menu.CREATE_PARTY;
    private string report = "";
    private List<Func<Item>> shopItems;
    private string shopTitle;
    private int page;
    private Item selectedItem;
    private Func<Item> selectedFactory;
    private int selectedHero;
    private List<Item> equipItems;
    private static System.Random random = new System.Random();

    public string MenuText { get; private set; }
    public string MainText { get; private set; }

    public Adventure() {
        report = "A new adventure begins! Create three heroes. You have " + AdventureConfig.START_GOLD + " gold pieces to equip them.";
        Refresh();
    }

    /// <summary>Called when the main menu shows the adventure (also after other screens return).</summary>
    public void Refresh() {
        foreach (CharacterSheet hero in Party) learnSpells(hero);
        if (menu == Menu.CREATE_PARTY && Party.Count >= AdventureConfig.PARTY_SIZE) {
            menu = Menu.FORMATION;
            report = "Your party is complete. Choose who fights in the front line.";
        }
        show();
    }

    public bool IsFront(int index) {
        return index >= 0 && index < front.Length && front[index];
    }

    public Fight CurrentFight {
        get { return AdventureConfig.Fights[Math.Min(FightIndex, AdventureConfig.Fights.Length - 1)]; }
    }

    /// <summary>The enemies of the current fight (new instances).</summary>
    public Monster[] CreateEnemies() {
        Fight fight = CurrentFight;
        Monster[] enemies = new Monster[fight.Enemies.Length];
        for (int i = 0; i < enemies.Length; i++) enemies[i] = fight.Enemies[i].Create();
        return enemies;
    }

    /// <summary>Called by the combat screen when the fight is over.</summary>
    public void FightFinished(bool won) {
        if (!InFight) return;
        InFight = false;
        if (!won) {
            menu = Menu.GAME_OVER;
            report = "Your party has been defeated by " + CurrentFight.Title.ToLowerInvariant() + ".\n\nGAME OVER";
            show();
            return;
        }
        StringBuilder text = new StringBuilder();
        text.AppendFormat("Victory over {0}!\n\n", CurrentFight.Title.ToLowerInvariant());
        // Experience, shared by the party
        int experience = 0;
        foreach (EnemyType enemy in CurrentFight.Enemies) experience += enemy.Create().Experience;
        int share = experience / Party.Count;
        text.AppendFormat("Each hero gains {0} XP.\n", share);
        // Loot
        int gold = 0;
        List<Item> loot = new List<Item>();
        foreach (EnemyType enemy in CurrentFight.Enemies) {
            if (enemy.Gold != null) gold += roll(enemy.Gold);
            foreach (LootDrop drop in enemy.Loot)
                if (random.Next(100) < drop.Chance) loot.Add(drop.Create());
        }
        Copper += gold * 100;
        if (gold > 0) text.AppendFormat("Loot: {0} gold pieces.\n", gold);
        for (int i = 0; i < loot.Count; i++) {
            CharacterSheet hero = Party[i % Party.Count];
            hero.Inventory.AddToBag(loot[i]);
            text.AppendFormat("Loot: {0} (to {1}'s bag).\n", loot[i].Name, hero.Name);
        }
        if (gold == 0 && loot.Count == 0) text.Append("No loot.\n");
        // Rest: everyone is revived, healed and gets their spell slots back
        foreach (CharacterSheet hero in Party) {
            rest(hero);
            hero.Experience += share;
            while (hero.Levels[0].Levels < AdventureConfig.LEVEL_XP.Length
                    && hero.Experience >= AdventureConfig.LEVEL_XP[hero.Levels[0].Levels]) {
                hero.AddLevel(hero.Levels[0].Class);
                rest(hero);
                text.AppendFormat("{0} reaches level {1}!\n", hero.Name, hero.Levels[0].Levels);
                if (learnSpells(hero)) text.AppendFormat("{0} learns new spells.\n", hero.Name);
            }
        }
        text.Append("The party rests and is fully restored.");
        FightIndex++;
        if (FightIndex >= AdventureConfig.Fights.Length) {
            menu = Menu.VICTORY;
            text.Append("\n\nYou have completed the adventure. Well done!");
        } else {
            menu = Menu.HUB;
        }
        report = text.ToString();
        show();
    }

    /// <summary>Handles a number key; returns what the screen has to do.</summary>
    public AdventureAction HandleKey(int digit) {
        switch (menu) {
            case Menu.CREATE_PARTY:
                if (digit == 1) return AdventureAction.CREATE_CHARACTER;
                if (digit == 0) { menu = Menu.CONFIRM_LEAVE; report = ""; }
                break;
            case Menu.HUB:
                report = "";
                if (digit == 1) { menu = Menu.SHOP; }
                else if (digit == 2) { menu = Menu.EQUIP_SELECT_HERO; }
                else if (digit == 3) { menu = Menu.FORMATION; }
                else if (digit == 4) { InFight = true; return AdventureAction.START_FIGHT; }
                else if (digit == 0) { menu = Menu.CONFIRM_LEAVE; }
                break;
            case Menu.SHOP:
                if (digit == 0) menu = Menu.HUB;
                else if (digit == 1) openShopList("Weapons", AdventureConfig.ShopWeapons());
                else if (digit == 2) openShopList("Armor", AdventureConfig.ShopArmor());
                else if (digit == 3) openShopList("Potions", AdventureConfig.ShopPotions());
                break;
            case Menu.SHOP_LIST:
                if (digit == 0) { menu = Menu.SHOP; report = ""; }
                else if (digit == 9) page = (page + 1) % pageCount(shopItems.Count);
                else {
                    int index = page * PAGE_SIZE + digit - 1;
                    if (index < shopItems.Count) {
                        selectedFactory = shopItems[index];
                        selectedItem = selectedFactory();
                        menu = Menu.SHOP_BUY;
                        report = Library.ItemDetails(selectedItem);
                    }
                }
                break;
            case Menu.SHOP_BUY:
                if (digit == 0) { menu = Menu.SHOP_LIST; report = ""; }
                else if (digit <= Party.Count) {
                    if (Copper < selectedItem.Value) {
                        report = "You cannot afford " + selectedItem.Name + ".";
                    } else {
                        Copper -= selectedItem.Value;
                        Party[digit - 1].Inventory.AddToBag(selectedFactory());
                        report = Party[digit - 1].Name + " bought " + selectedItem.Name + ". Equip it in the Equip menu.";
                    }
                    menu = Menu.SHOP_LIST;
                }
                break;
            case Menu.EQUIP_SELECT_HERO:
                if (digit == 0) menu = Menu.HUB;
                else if (digit <= Party.Count) {
                    selectedHero = digit - 1;
                    page = 0;
                    menu = Menu.EQUIP_SELECT_ITEM;
                }
                break;
            case Menu.EQUIP_SELECT_ITEM:
                if (digit == 0) { menu = Menu.EQUIP_SELECT_HERO; report = ""; }
                else if (digit == 9) page = (page + 1) % pageCount(equipItems.Count);
                else {
                    int index = page * PAGE_SIZE + digit - 1;
                    if (index < equipItems.Count) {
                        CharacterSheet hero = Party[selectedHero];
                        hero.Equip(equipItems[index]);
                        report = hero.Name + " equipped " + equipItems[index].Name + ".";
                        page = 0;
                    }
                }
                break;
            case Menu.FORMATION:
                if (digit >= 1 && digit <= Party.Count) {
                    front[digit - 1] = !front[digit - 1];
                    report = "";
                } else if (digit == 0) {
                    int count = 0;
                    foreach (bool f in front) if (f) count++;
                    if (count == 1 || count == 2) { menu = Menu.HUB; report = ""; }
                    else report = "Put one or two heroes in the front line.";
                }
                break;
            case Menu.CONFIRM_LEAVE:
                if (digit == 1) { Current = null; return AdventureAction.LEAVE; }
                if (digit == 0) menu = Party.Count >= AdventureConfig.PARTY_SIZE ? Menu.HUB : Menu.CREATE_PARTY;
                break;
            case Menu.GAME_OVER:
            case Menu.VICTORY:
                if (digit == 0) { Current = null; return AdventureAction.LEAVE; }
                break;
        }
        show();
        return AdventureAction.NONE;
    }

    // ---------- Texts ----------

    private void show() {
        StringBuilder m = new StringBuilder();
        switch (menu) {
            case Menu.CREATE_PARTY:
                m.AppendFormat("Adventure - create your party ({0}/{1})\n\n1. Create a character\n0. Leave adventure", Party.Count, AdventureConfig.PARTY_SIZE);
                break;
            case Menu.HUB:
                m.AppendFormat("Adventure - fight {0}/{1}\nNext: {2}\n\n1. Shop\n2. Equip\n3. Formation\n4. Start the fight\n0. Leave adventure",
                    FightIndex + 1, AdventureConfig.Fights.Length, CurrentFight.Title);
                break;
            case Menu.SHOP:
                m.AppendFormat("Shop ({0})\n\n1. Weapons\n2. Armor\n3. Potions\n0. Back", gold());
                break;
            case Menu.SHOP_LIST: {
                    m.AppendFormat("{0} ({1}, page {2}/{3})\n\n", shopTitle, gold(), page + 1, pageCount(shopItems.Count));
                    for (int i = 0; i < PAGE_SIZE; i++) {
                        int index = page * PAGE_SIZE + i;
                        if (index >= shopItems.Count) break;
                        Item item = shopItems[index]();
                        m.AppendFormat("{0}. {1} ({2})\n", i + 1, item.Name, coins(item.Value));
                    }
                    if (pageCount(shopItems.Count) > 1) m.Append("9. Next page\n");
                    m.Append("0. Back");
                    break;
                }
            case Menu.SHOP_BUY:
                m.AppendFormat("Buy {0} for {1}?\n({2} left)\n\n", selectedItem.Name, coins(selectedItem.Value), gold());
                for (int i = 0; i < Party.Count; i++) m.AppendFormat("{0}. {1}\n", i + 1, Party[i].Name);
                m.Append("0. Back");
                break;
            case Menu.EQUIP_SELECT_HERO:
                m.Append("Equip which hero?\n\n");
                for (int i = 0; i < Party.Count; i++) m.AppendFormat("{0}. {1}\n", i + 1, Party[i].Name);
                m.Append("0. Back");
                break;
            case Menu.EQUIP_SELECT_ITEM: {
                    CharacterSheet hero = Party[selectedHero];
                    equipItems = new List<Item>();
                    foreach (Item item in hero.Inventory.Bag)
                        if (item is Weapon || item is Armor || item is Shield) equipItems.Add(item);
                    m.AppendFormat("Equip {0} (page {1}/{2})\n\n", hero.Name, page + 1, pageCount(equipItems.Count));
                    for (int i = 0; i < PAGE_SIZE; i++) {
                        int index = page * PAGE_SIZE + i;
                        if (index >= equipItems.Count) break;
                        m.AppendFormat("{0}. {1}\n", i + 1, equipItems[index].Name);
                    }
                    if (equipItems.Count == 0) m.Append("Nothing to equip in the bag.\n");
                    if (pageCount(equipItems.Count) > 1) m.Append("9. Next page\n");
                    m.Append("0. Back");
                    break;
                }
            case Menu.FORMATION:
                m.Append("Formation: 1 or 2 heroes in front\n\n");
                for (int i = 0; i < Party.Count; i++) m.AppendFormat("{0}. {1} ({2})\n", i + 1, Party[i].Name, front[i] ? "front" : "back");
                m.Append("0. Done");
                break;
            case Menu.CONFIRM_LEAVE:
                m.Append("Leave the adventure?\nYour party will be lost.\n\n1. Yes, leave\n0. No, stay");
                break;
            case Menu.GAME_OVER:
            case Menu.VICTORY:
                m.Append("0. Back to the main menu");
                break;
        }
        MenuText = m.ToString();

        if (menu == Menu.SHOP_BUY || menu == Menu.GAME_OVER || menu == Menu.VICTORY) {
            MainText = report;
        } else if (menu == Menu.EQUIP_SELECT_ITEM) {
            MainText = (report.Length > 0 ? report + "\n\n" : "") + heroSheet(Party[selectedHero]);
        } else {
            MainText = (report.Length > 0 ? report + "\n\n" : "") + partySummary();
        }
    }

    private string partySummary() {
        StringBuilder text = new StringBuilder();
        text.AppendFormat("Gold: {0}\n", gold());
        for (int i = 0; i < Party.Count; i++) {
            CharacterSheet hero = Party[i];
            int level = hero.Levels[0].Levels;
            string next = level < AdventureConfig.LEVEL_XP.Length ? "/" + AdventureConfig.LEVEL_XP[level] : "";
            text.AppendFormat("\n{0}. {1} ({2})\n   {3} {4} level {5}\n   HP {6}/{7}  AC {8}  XP {9}{10}\n",
                i + 1, hero.Name, front[i] ? "front" : "back", hero.Race.Name, hero.Levels[0].Class.Name, level,
                hero.HitPoints, hero.HitPointsMax, hero.ArmorClass, hero.Experience, next);
        }
        return text.ToString();
    }

    private static string heroSheet(CharacterSheet hero) {
        StringBuilder text = new StringBuilder();
        text.AppendFormat("{0}\nMain hand: {1}\nOff hand:  {2}\nArmor:     {3}\n\nBag:\n", hero.Name,
            nameOf(hero.Inventory.MainHand), nameOf(hero.Inventory.OffHand), nameOf(hero.Inventory.Armor));
        foreach (Item item in hero.Inventory.Bag) text.AppendFormat("- {0}\n", item.Name);
        return text.ToString();
    }

    // ---------- Helpers ----------

    private void openShopList(string title, List<Func<Item>> items) {
        shopTitle = title;
        shopItems = new List<Func<Item>>();
        foreach (Func<Item> item in items) if (item().Value > 0) shopItems.Add(item);
        page = 0;
        report = "";
        menu = Menu.SHOP_LIST;
    }

    /// <summary>Revives, heals and refreshes a hero (long rest).</summary>
    private static void rest(CharacterSheet hero) {
        if (hero.Dead) {
            // libsrd5 0.3 has no public way to revive (Dead has an internal setter)
            PropertyInfo dead = typeof(Combatant).GetProperty("Dead", BindingFlags.Public | BindingFlags.Instance);
            dead.GetSetMethod(true).Invoke(hero, new object[] { false });
        }
        if (hero.HitPoints <= 0) hero.HealDamage(1);
        hero.RemoveCondition(ConditionType.UNCONSCIOUS);
        hero.LongRest();
    }

    /// <summary>Learns and prepares the configured spells for every spell level the hero has slots for.</summary>
    private static bool learnSpells(CharacterSheet hero) {
        if (hero.Levels.Length == 0) return false;
        Class clazz = hero.Levels[0].Class.Class;
        Spell[][] bySpellLevel = AdventureConfig.SpellsFor(clazz);
        bool learned = false;
        foreach (AvailableSpells spells in hero.AvailableSpells) {
            if (spells.CharacterClass.Class != clazz) continue;
            for (int level = 0; level < bySpellLevel.Length; level++) {
                if (level > 0 && spells.SlotsMax[level] == 0) continue;
                foreach (Spell spell in bySpellLevel[level]) {
                    if (knows(spells, spell)) continue;
                    spells.AddKnownSpell(spell);
                    spells.AddPreparedSpell(spell);
                    learned = true;
                }
            }
        }
        return learned;
    }

    private static bool knows(AvailableSpells spells, Spell spell) {
        foreach (Spell known in spells.KnownSpells) if (known.ID == spell.ID) return true;
        return false;
    }

    /// <summary>Rolls dice like "2d6" or "1d4+2".</summary>
    private static int roll(string dice) {
        int d = dice.IndexOf('d');
        int plus = dice.IndexOf('+');
        int amount = int.Parse(dice.Substring(0, d));
        int die = int.Parse(plus < 0 ? dice.Substring(d + 1) : dice.Substring(d + 1, plus - d - 1));
        int total = plus < 0 ? 0 : int.Parse(dice.Substring(plus + 1));
        for (int i = 0; i < amount; i++) total += random.Next(1, die + 1);
        return total;
    }

    private string gold() {
        return coins(Copper);
    }

    private static string coins(int copper) {
        if (copper % 100 == 0) return (copper / 100) + " gp";
        if (copper % 10 == 0) return (copper / 10) + " sp";
        return copper + " cp";
    }

    private static int pageCount(int count) {
        return Math.Max(1, (count + PAGE_SIZE - 1) / PAGE_SIZE);
    }

    private static string nameOf(Item item) {
        return item == null ? "-" : item.Name;
    }
}
