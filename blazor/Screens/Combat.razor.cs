using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using srd5;

namespace Libsrd5Blazor.Screens {

public partial class Combat {
    private string MenuText = "", CombatLog = "";
    // Unit images, positioned as in the Unity scene (centre x, y up, width, height)
    private Slot HeroFront1 = new Slot(-233, 122, 192, 192), HeroFront2 = new Slot(-203, -57, 192, 192),
        HeroBack1 = new Slot(-426, 127, 192, 192), HeroBack2 = new Slot(-401, -47, 192, 192);
    private int heroFront1 = -1, heroFront2 = -1, heroBack1 = -1, heroBack2 = -1;
    private List<Slot> EnemySlots = new List<Slot>();
    private string[] DwarfBarbarianSprites = Game.UnitSprites("dwarf_berserker"), DwarfDruidSprites = Game.UnitSprites("dwarf_druid"),
        ElfBarbarianSprites = Game.UnitSprites("elf_berserker"), ElfDruidSprites = Game.UnitSprites("elf_druid");
    private string[] DwarfWizardSprites = Game.UnitSprites("dwarf_wizard"), ElfWizardSprites = Game.UnitSprites("elf_wizard"),
        HumanBarbarianSprites = Game.UnitSprites("human_berserker"), HumanDruidSprites = Game.UnitSprites("human_druid"),
        HumanWizardSprites = Game.UnitSprites("human_wizard");
    private string[] BoarSprites = Game.UnitSprites("boar"), OrcSprites = Game.UnitSprites("orc"), OgreSprites = Game.UnitSprites("ogre");

    /// <summary>Unity: condition icon centre 20px right / 20px up of the unit centre, 64x64.</summary>
    private static string IconStyle(Slot slot) {
        return string.Format(System.Globalization.CultureInfo.InvariantCulture, "left:{0}px;top:{1}px;width:64px;height:64px;",
            slot.W / 2 + 20 - 32, slot.H / 2 - 20 - 32);
    }

    /// <summary>All unit slots, in drawing order.</summary>
    private IEnumerable<Slot> AllSlots {
        get {
            yield return HeroFront1; yield return HeroFront2; yield return HeroBack1; yield return HeroBack2;
            foreach (Slot slot in EnemySlots) yield return slot;
        }
    }

    private Monster[] enemies;
    private BattleGroundClassic battle;
    private FixedSizedQueue<string> logQueue;
    private float timer = 0f;

    private const string WAITING_FOR_ENEMIES = "Waiting for enemies...";

    private const string SELECT_ACTION =
@"1. Attack
2. Cast Spell
3. Items";

    private const string WON =
@"You won!
0. Return to Main Menu";
    private const string LOST =
@"You lost!
0. Return to Main Menu";

    private const string SELECT_ITEM_ACTION =
@"1. Change/Equip Weapons
2. Use an item
0. Back";

    private Spell selectedSpell;
    private SpellLevel selectedSlot;
    private List<string> selectedTargets = new List<string>();
    private Usable selectedUsable;
    private int selectedCharges;
    enum CombatState {
        ENEMY_TURN, YOUR_TURN_SELECT_ACTION, ATTACK_SELECT_TARGET, ATTACK_EXECUTED,
        ITEM_SELECT_ACTION, ITEM_EQUIP_SELECT_ITEMS, ITEM_USE_SELECT_ITEM, ITEM_USE_SELECT_CHARGES, ITEM_USE_SELECT_TARGETS,
        SPELL_SELECT_SPELL, SPELL_SELECT_SLOT, SPELL_SELECT_TARGETS, SPELL_CAST,
        WON, LOST
    }
    private CombatState state = CombatState.ENEMY_TURN;

    // libsrd5 0.3: Monster.Name is the monster type and can't be changed, so two orcs are both "Orc".
    // The example keeps its own labels ("Orc #0", "Orc #1") and uses them everywhere.
    private Dictionary<Combatant, string> monsterNames = new Dictionary<Combatant, string>();

    private string nameOf(Combatant combatant) {
        string name;
        if (monsterNames.TryGetValue(combatant, out name)) return name;
        return combatant.Name;
    }
    private Dictionary<string, CombatantData> combattants = new Dictionary<string, CombatantData>();

    protected override void OnKey(KeyCode code) {
        if (timer > 0) return;
        if (state == CombatState.YOUR_TURN_SELECT_ACTION) {
            if (code == KeyCode.Alpha1) {
                state = CombatState.ATTACK_SELECT_TARGET;
                MenuText = getAttackText();
            } else if (code == KeyCode.Alpha2) {
                state = CombatState.SPELL_SELECT_SPELL;
                MenuText = getSelectSpellsText();
                selectedSpell = null;
                selectedSlot = SpellLevel.CANTRIP;
                selectedTargets.Clear();
            } else if (code == KeyCode.Alpha3) {
                state = CombatState.ITEM_SELECT_ACTION;
                MenuText = SELECT_ITEM_ACTION;
                selectedTargets.Clear();
                selectedUsable = null;
                selectedCharges = 1;
            }
        } else if (state == CombatState.ATTACK_SELECT_TARGET) {
            Combatant target = null;
            if (code == KeyCode.Alpha0) {
                returnToMain();
            } else if (code == KeyCode.Alpha1) {
                target = enemies[0];
            } else if (code == KeyCode.Alpha2) {
                target = enemies[1];
            }
            if (target == null || target.HitPoints <= 0) return;
            state = CombatState.ATTACK_EXECUTED;
            if (battle.CurrentCombatant.RangedAttacks.Length > 0)
                battle.RangedAttackAction(target);
            else
                battle.MeleeAttackAction(target);
            timer = 2.0f;
            setAttackDefendSprites(battle.CurrentCombatant, target);
        } else if (state == CombatState.WON || state == CombatState.LOST) {
            if (code == KeyCode.Alpha0) {
                foreach (CharacterSheet hero in Game.Party) {
                    hero.LongRest();
                }
                LoadScene("MainMenu");
            }
        } else if (state == CombatState.SPELL_SELECT_SPELL) {
            if (code == KeyCode.Alpha0) {
                returnToMain();
            } else {
                int index = code - KeyCode.Alpha1;
                if (battle.CurrentCombatant.AvailableSpells.Length == 0) return;
                if (index >= battle.CurrentCombatant.AvailableSpells[0].PreparedSpells.Length) return;
                selectedSpell = battle.CurrentCombatant.AvailableSpells[0].PreparedSpells[index];
                if (selectedSpell.Level == SpellLevel.CANTRIP) {
                    selectedSlot = SpellLevel.CANTRIP;
                    if (selectedSpell.MaximumTargets == 0) {
                        castSpell();
                    } else {
                        state = CombatState.SPELL_SELECT_TARGETS;
                        MenuText = getSelectTargetsText();
                    }
                } else {
                    state = CombatState.SPELL_SELECT_SLOT;
                    MenuText = getSelectSpellSlotText();
                }
            }
        } else if (state == CombatState.SPELL_SELECT_SLOT) {
            if (code == KeyCode.Alpha0) {
                returnToMain();
            } else {
                int index = code - KeyCode.Alpha0;
                if (index < (int)selectedSpell.Level) return;
                int slots = battle.CurrentCombatant.AvailableSpells[0].SlotsCurrent[index];
                if (slots == 0) return;
                selectedSlot = (SpellLevel)index;
                if (selectedSpell.MaximumTargets == 0) {
                    castSpell();
                } else {
                    state = CombatState.SPELL_SELECT_TARGETS;
                    MenuText = getSelectTargetsText();
                }
            }
        } else if (state == CombatState.SPELL_SELECT_TARGETS) {
            if (code == KeyCode.Alpha0) {
                castSpell();
            } else {
                int index = code - KeyCode.Alpha1;
                toggleSelectedTarget(index);
                if (selectedTargets.Count == selectedSpell.MaximumTargets) castSpell();
                MenuText = getSelectTargetsText();
            }
        } else if (state == CombatState.ITEM_SELECT_ACTION) {
            if (code == KeyCode.Alpha0) {
                returnToMain();
            } else if (code == KeyCode.Alpha1) {
                state = CombatState.ITEM_EQUIP_SELECT_ITEMS;
                MenuText = getEquippableItems();
            } else if (code == KeyCode.Alpha2) {
                state = CombatState.ITEM_USE_SELECT_ITEM;
                MenuText = getUsableItems();
            }
        } else if (state == CombatState.ITEM_EQUIP_SELECT_ITEMS) {
            if (code == KeyCode.Alpha0) {
                if (selectedTargets.Count == 0) {
                    returnToMain();
                } else {
                    equipSelectedItems();

                }
            } else {
                int index = code - KeyCode.Alpha1;
                CharacterInventory inventory = ((CharacterSheet)battle.CurrentCombatant).Inventory;
                if (index > inventory.Bag.Length) return;
                selectedTargets.Add(inventory.Bag[index].ToString());
                MenuText = getEquippableItems();
            }
        } else if (state == CombatState.ITEM_USE_SELECT_ITEM) {
            if (code == KeyCode.Alpha0) {
                returnToMain();
            } else {
                int index = code - KeyCode.Alpha1;
                CharacterInventory inventory = ((CharacterSheet)battle.CurrentCombatant).Inventory;
                if (index > inventory.Bag.Length) return;
                int i = 0;
                foreach (Item item in inventory.Bag) {
                    if (item is Consumable && i++ == index) {
                        CharacterSheet hero = (CharacterSheet)battle.CurrentCombatant;
                        hero.Consume((Consumable)item);
                        timer = 1.0f;
                    } else if (item is Usable && i++ == index) {
                        selectedUsable = (Usable)item;
                        if (selectedUsable.Charges == 1 || selectedUsable.MaximumChargePerUse == 1) {
                            selectedCharges = 1;
                            state = CombatState.ITEM_USE_SELECT_TARGETS;
                            MenuText = getSelectTargetsText();
                        } else {
                            state = CombatState.ITEM_USE_SELECT_CHARGES;
                            MenuText = getExpendCharges();
                        }
                    }
                }
            }
        } else if (state == CombatState.ITEM_USE_SELECT_CHARGES) {
            if (code == KeyCode.Alpha0) {
                returnToMain();
            } else {
                int charges = code - KeyCode.Alpha0;
                if (charges > selectedUsable.Charges || charges > selectedUsable.MaximumChargePerUse) return;
                selectedCharges = charges;
                state = CombatState.ITEM_USE_SELECT_TARGETS;
                MenuText = getSelectTargetsText();
            }
        } else if (state == CombatState.ITEM_USE_SELECT_TARGETS) {
            if (code == KeyCode.Alpha0) {
                useSelectedItem();
            } else {
                int index = code - KeyCode.Alpha1;
                toggleSelectedTarget(index);
                if (selectedTargets.Count == selectedUsable.MaximumTargets) useSelectedItem();
                MenuText = getSelectTargetsText();
            }
        }
    }

    private void returnToMain() {
        state = CombatState.YOUR_TURN_SELECT_ACTION;
        MenuText = SELECT_ACTION;
    }

    private void toggleSelectedTarget(int index) {
        Dictionary<string, CombatantData>.Enumerator enumerator = combattants.GetEnumerator();
        int i = 0;
        while (enumerator.MoveNext()) {
            KeyValuePair<string, CombatantData> current = enumerator.Current;
            if (index == i++) {
                if (selectedTargets.Contains(current.Key))
                    selectedTargets.Remove(current.Key);
                else
                    selectedTargets.Add(current.Key);
            }
        }
    }

    private void combatLogListener(object sender, EventArgs args) {
        if (GlobalEvents.EventTypes.INITIATIVE.Equals(sender)) {
            GlobalEvents.InitiativeRolled initiative = (GlobalEvents.InitiativeRolled)args;
            log(String.Format("{0} rolled for initiative: {1,2}", nameOf(initiative.Roller), initiative.Result));
        } else if (GlobalEvents.EventTypes.ATTACKED.Equals(sender)) {
            GlobalEvents.AttackRolled attacked = (GlobalEvents.AttackRolled)args;
            String hit;
            if (attacked.CriticalHit)
                hit = "Critical hit";
            else if (attacked.Hit)
                hit = "Hit";
            else
                hit = "Missed";
            log(String.Format("{0} attacks {2} (AC{3}) with {4}: {5,2}{1} ({6})",
                        nameOf(attacked.Attacker), attacked.Attack.AttackBonus.ToString("+#;-#;0"),
                        nameOf(attacked.Target), attacked.Target.ArmorClass,
                        attacked.Attack.Name, attacked.Roll, hit));
        } else if (GlobalEvents.EventTypes.HEALED.Equals(sender)) {
            GlobalEvents.HealingReceived healed = (GlobalEvents.HealingReceived)args;
            log(String.Format("{0} was healed for {1} hitpoints", nameOf(healed.Beneficiary), healed.Amount));
        } else if (GlobalEvents.EventTypes.DAMAGED.Equals(sender)) {
            GlobalEvents.DamageReceived damaged = (GlobalEvents.DamageReceived)args;
            log(String.Format("{0} was hit for {1} points of {2} damage", nameOf(damaged.Victim), damaged.Amount, damaged.DamageType.Name()));
        } else if (GlobalEvents.EventTypes.DC.Equals(sender)) {
            GlobalEvents.DCRolled dc = (GlobalEvents.DCRolled)args;
            string success = dc.Success ? "Sucess" : "Fail";
            log(String.Format("{0} rolled {1} {2} DC :{3} ({4})", nameOf(dc.Roller), dc.Ability.Name, dc.DC, dc.Roll, success));
        } else if (GlobalEvents.EventTypes.AFFECT_BY_SPELL.Equals(sender)) {
            GlobalEvents.SpellAffection spell = (GlobalEvents.SpellAffection)args;
            string affected = spell.Affected ? "affected" : "unaffected";
            log(String.Format("{0} is {1} by {2}'s {3} Spell", nameOf(spell.Target), affected, nameOf(spell.Caster), spell.Spell.Name()));
        } else if (GlobalEvents.EventTypes.CONDITION.Equals(sender)) {
            GlobalEvents.ConditionChanged condition = (GlobalEvents.ConditionChanged)args;
            string noLonger = condition.Removed ? "is no longer" : "is";
            log(String.Format("{0} {1} {2}", nameOf(condition.Bearer), noLonger, condition.Condition.Name()));
            if (!combattants.TryGetValue(nameOf(condition.Bearer), out CombatantData bearer)) return;
            // Only the unconscious condition has an icon (rendered by the markup, pulsing via CSS)
            if (condition.Removed)
                bearer.Image.Icons.Remove(condition.Condition);
            else if (condition.Condition == ConditionType.UNCONSCIOUS)
                bearer.Image.Icons.Add(condition.Condition);
        } else if (GlobalEvents.EventTypes.ACTION_FAILED.Equals(sender)) {
            GlobalEvents.ActionFailed failure = (GlobalEvents.ActionFailed)args;
            log(String.Format("{0}'s action failed: {1}", nameOf(failure.Initiator), failure.Reason.ToString()));
        } else if (GlobalEvents.EventTypes.EQUIPMENT.Equals(sender)) {
            GlobalEvents.EquipmentChanged equipment = (GlobalEvents.EquipmentChanged)args;
            if (equipment.Event == GlobalEvents.EquipmentChanged.Events.UNEQUIPPED)
                log(String.Format("{0} unequipped {1}", nameOf(equipment.Hero), equipment.Item.Name));
            else if (equipment.Event == GlobalEvents.EquipmentChanged.Events.EQUIPPED)
                log(String.Format("{0} equipped {1}", nameOf(equipment.Hero), equipment.Item.Name));
            else if (equipment.Event == GlobalEvents.EquipmentChanged.Events.USED)
                log(String.Format("{0} used {1}", nameOf(equipment.Hero), equipment.Item.Name));
            else if (equipment.Event == GlobalEvents.EquipmentChanged.Events.DESTROYED)
                log(String.Format("{0}'s {1} was destroyed", nameOf(equipment.Hero), equipment.Item.Name));
            else if (equipment.Event == GlobalEvents.EquipmentChanged.Events.PUT_IN_BAG)
                log(String.Format("{0} put {1} in the bag", nameOf(equipment.Hero), equipment.Item.Name));
            else if (equipment.Event == GlobalEvents.EquipmentChanged.Events.REMOVED_FROM_BAG)
                log(String.Format("{0} took {1} out of the bag", nameOf(equipment.Hero), equipment.Item.Name));
        } else if (GlobalEvents.EventTypes.CAST_SPELL.Equals(sender)) {
            GlobalEvents.SpellCast cast = (GlobalEvents.SpellCast)args;
            log(String.Format("{0} casts {1}", nameOf(cast.Caster), cast.Spell.Name()));
        } else if (GlobalEvents.EventTypes.EFFECT_ACTIVATED.Equals(sender)) {
            // Raised for activated effects and for activated feats
            if (args is GlobalEvents.FeatActivated) {
                GlobalEvents.FeatActivated feat = (GlobalEvents.FeatActivated)args;
                log(String.Format("{0} uses {1}", nameOf(feat.Source), feat.Feat.Name()));
            } else if (args is GlobalEvents.EffectActivated) {
                GlobalEvents.EffectActivated effect = (GlobalEvents.EffectActivated)args;
                log(String.Format("{0}: {1} activated", nameOf(effect.Source), effect.Effect.Name()));
            }
        } else if (GlobalEvents.EventTypes.DEATH.Equals(sender)) {
            GlobalEvents.Death death = (GlobalEvents.Death)args;
            log(String.Format("{0} dies", nameOf(death.Victim)));
        } else {
            Console.WriteLine("Unexpected Event.");
        }
    }

    protected override void Start() {
        battle = new BattleGroundClassic();
        MenuText = WAITING_FOR_ENEMIES;
        logQueue = new FixedSizedQueue<string>(11);
        GlobalEvents.Handlers += combatLogListener;
        int i = 0;
        int front = 0;
        int back = 0;
        foreach (CharacterSheet hero in Game.Party) {
            if (Game.IsFrontLine(i)) {
                battle.AddCombatant(hero, ClassicLocation.Row.FRONT_LEFT);
                if (front++ == 0) {
                    heroFront1 = i;
                    assignSprites(hero, HeroFront1);
                } else {
                    heroFront2 = i;
                    assignSprites(hero, HeroFront2);
                }
            } else {
                battle.AddCombatant(hero, ClassicLocation.Row.BACK_LEFT);
                if (back++ == 0) {
                    heroBack1 = i;
                    assignSprites(hero, HeroBack1);
                } else {
                    heroBack2 = i;
                    assignSprites(hero, HeroBack2);
                }
            }
            i++;
        }

        enemies = Game.CreateEnemies();
        EnemyLayout.Placement[] placements = EnemyLayout.Place(enemies);
        // add back to front (higher on screen first) so nearer enemies are drawn on top
        List<int> drawOrder = new List<int>();
        for (i = 0; i < enemies.Length; i++) drawOrder.Add(i);
        drawOrder.Sort((a, b) => placements[b].Y.CompareTo(placements[a].Y));
        foreach (int k in drawOrder) {
            string[] sprites = enemySprites(enemies[k]);
            Slot image = new Slot(placements[k].X, placements[k].Y, placements[k].Size, placements[k].Size);
            EnemySlots.Add(image);
            image.Visible = true;
            image.Sprite = sprites[0];
            battle.AddCombatant(enemies[k], ClassicLocation.Row.FRONT_RIGHT);
            monsterNames[enemies[k]] = nameOf(enemies[k]) + " #" + k;
            combattants.Add(nameOf(enemies[k]), new CombatantData(enemies[k], image, sprites));
        }
        battle.Initialize();
        timer = 2.0f;
        _ = updateLoop();
    }

    public override void Dispose() {
        base.Dispose();
        GlobalEvents.Handlers -= combatLogListener;
    }

    private string[] heroSprites(CharacterSheet hero) {
        Class heroClass = hero.Levels[0].Class.Class;
        if (hero.Race.Race == Race.HILL_DWARF)
            return heroClass == Class.WIZARD ? DwarfWizardSprites : heroClass == Class.DRUID ? DwarfDruidSprites : DwarfBarbarianSprites;
        if (hero.Race.Race == Race.HIGH_ELF)
            return heroClass == Class.WIZARD ? ElfWizardSprites : heroClass == Class.DRUID ? ElfDruidSprites : ElfBarbarianSprites;
        return heroClass == Class.WIZARD ? HumanWizardSprites : heroClass == Class.DRUID ? HumanDruidSprites : HumanBarbarianSprites;
    }

    private string[] enemySprites(Monster enemy) {
        switch (enemy.ID) {
            case Monsters.ID.BOAR: return BoarSprites;
            case Monsters.ID.OGRE: return OgreSprites;
            default: return OrcSprites;
        }
    }

    private void combatFinished(bool won) {
        if (Adventure.Current != null) Adventure.Current.FightFinished(won);
    }

    private void assignSprites(CharacterSheet hero, Slot heroImage) {
        heroImage.Visible = true;
        string[] sprites = heroSprites(hero);
        heroImage.Sprite = sprites[0];
        combattants.Add(nameOf(hero), new CombatantData(hero, heroImage, sprites));
    }

    // Unity's Update(): a ~20 fps loop for the timer between combat phases
    private async Task updateLoop() {
        Stopwatch clock = Stopwatch.StartNew();
        double last = 0;
        while (!disposed) {
            await Task.Delay(50);
            double now = clock.Elapsed.TotalSeconds;
            float deltaTime = (float)(now - last);
            last = now;
            if (disposed) break;
            if (timer > 0) {
                timer -= deltaTime;
                if (timer > 0) continue;
                nextPhase();
                StateHasChanged();
            }
        }
    }

    private void nextPhase() {
        clearAttackDefendSprites();
        if (state == CombatState.WON || state == CombatState.LOST) return;
        for (TurnPhase phase = battle.NextPhase(); phase != TurnPhase.ACTION; phase = battle.NextPhase()) ;
        if (battle.CurrentCombatant is CharacterSheet) {
            if (battle.CurrentCombatant.HitPoints <= 0) {
                nextPhase();
            } else {
                log(String.Format("It is {0}'s turn", nameOf(battle.CurrentCombatant)));
                state = CombatState.YOUR_TURN_SELECT_ACTION;
                MenuText = SELECT_ACTION;
            }
            return;
        } else if (battle.CurrentCombatant.HitPoints <= 0) {
            nextPhase();
            return;
        }
        MenuText = WAITING_FOR_ENEMIES;
        // select target
        CharacterSheet target = null;
        foreach (int index in new int[] { heroFront1, heroFront2, heroBack1, heroBack2 }) {
            if (index == -1) continue;
            CharacterSheet hero = Game.Party[index];
            if (hero.HitPoints > 0) {
                target = hero;
                break;
            }
        }
        if (target == null) {
            state = CombatState.LOST;
            MenuText = LOST;
            combatFinished(false);
            return;
        }
        if (battle.Distance(target) > 5) {
            battle.RangedAttackAction(target);
            timer = 2.0f;
            setAttackDefendSprites(battle.CurrentCombatant, target);
        } else {
            battle.MeleeAttackAction(target);
            timer = 2.0f;
            setAttackDefendSprites(battle.CurrentCombatant, target);
        }
    }

    private void clearAttackDefendSprites() {
        foreach (CombatantData data in combattants.Values) {
            checkIfWonOrLost();
            data.Image.Sprite = data.Sprites[0];
        }
    }

    private void checkIfWonOrLost() {
        bool heroAlive = false;
        bool monsterAlive = false;
        foreach (CombatantData data in combattants.Values) {
            if (data.Combatant is Monster && data.Combatant.HitPoints > 0)
                monsterAlive = true;
            if (data.Combatant is CharacterSheet && data.Combatant.HitPoints > 0)
                heroAlive = true;
        }
        if (!heroAlive) {
            state = CombatState.LOST;
            MenuText = LOST;
            combatFinished(false);
        } else if (!monsterAlive) {
            state = CombatState.WON;
            MenuText = WON;
            combatFinished(true);
        }
    }

    private void setAttackDefendSprites(Combatant attacker, Combatant target) {
        combattants[nameOf(attacker)].Image.Sprite = combattants[nameOf(attacker)].Sprites[1];
        combattants[nameOf(target)].Image.Sprite = combattants[nameOf(target)].Sprites[2];
    }

    private void log(string log) {
        logQueue.Enqueue(log);
        string[] lines = logQueue.ToArray();
        CombatLog = "";
        foreach (string line in lines) {
            if (CombatLog.Length > 0) CombatLog += "\n";
            CombatLog += line;
        }
    }

    private string getAttackText() {
        String text = "Select target\n";
        for (int i = 0; i < enemies.Length; i++) {
            if (enemies[i].HitPoints <= 0) continue;
            text += String.Format("{0}. {1}\n", i + 1, nameOf(enemies[i]));
        }
        text += "0. Back";
        return text;
    }

    private string getSelectSpellsText() {
        String text = "Select spell\n";
        if (battle.CurrentCombatant.AvailableSpells.Length > 0) {
            for (int i = 0; i < Math.Min(9, battle.CurrentCombatant.AvailableSpells[0].PreparedSpells.Length); i++) {
                Spell spell = battle.CurrentCombatant.AvailableSpells[0].PreparedSpells[i];
                text += String.Format("{0}. {1}\n", i + 1, spell.Name);
            }
        }
        text += "0. Back";
        return text;
    }

    private string getSelectSpellSlotText() {
        String text = String.Format("Select level to cast {0}\n", selectedSpell.Name);
        for (int i = (int)(selectedSpell.Level); i < 10; i++) {
            int slots = battle.CurrentCombatant.AvailableSpells[0].SlotsCurrent[i];
            if (slots == 0) continue;
            text += String.Format("{0}. Level ({1} slots left)\n", i, slots);
        }
        text += "0. Back";
        return text;
    }

    private string getSelectTargetsText() {
        string text = "Select target(s)\n";
        Dictionary<string, CombatantData>.Enumerator enumerator = combattants.GetEnumerator();
        int i = 1;
        while (enumerator.MoveNext()) {
            KeyValuePair<string, CombatantData> current = enumerator.Current;
            string name = current.Key;
            Game.StringPad(ref name, 13);
            string selected = selectedTargets.Contains(current.Key) ? "*" : " ";
            text += String.Format("{2}{0}. {1}", i, name, selected);
            if (i++ % 2 == 0)
                text += "\n";
            else
                text += " ";
        }
        if (i % 2 == 0)
            text += "\n";
        text += "0. Confirm";
        return text;
    }

    private void castSpell() {
        state = CombatState.SPELL_CAST;
        List<Combatant> targets = new List<Combatant>();
        foreach (string name in selectedTargets) {
            targets.Add(combattants[name].Combatant);
        }
        if (!battle.SpellCastAction(selectedSpell, selectedSlot, battle.CurrentCombatant.AvailableSpells[0], targets.ToArray()))
            log(String.Format("{0}'s spell failed (probably targets out of range)", nameOf(battle.CurrentCombatant)));
        timer = 2.0f;
        foreach (Combatant target in targets) {
            setAttackDefendSprites(battle.CurrentCombatant, target);
        }
    }

    private string getEquippableItems() {
        string text = "Select item(s) to equip from inventory\n";
        CharacterInventory inventory = ((CharacterSheet)battle.CurrentCombatant).Inventory;
        int i = 1;
        foreach (Item item in inventory.Bag) {
            string selected = selectedTargets.Contains(item.ToString()) ? "*" : " ";
            if (item.Type == ItemType.WEAPON || item.Type == ItemType.SHIELD)
                text += String.Format("{2}{0}. {1}\n", i++, item.Name, selected);
        }
        text += "0. Back";
        return text;
    }

    private string getUsableItems() {
        string text = "Select item to use\n";
        CharacterInventory inventory = ((CharacterSheet)battle.CurrentCombatant).Inventory;
        int i = 1;
        foreach (Item item in inventory.Bag) {
            string selected = selectedTargets.Contains(item.ToString()) ? "*" : " ";
            if (item is Consumable) {
                Consumable consumable = (Consumable)item;
                text += String.Format("{2}{0}. {1} ({3})\n", i++, item.Name, selected, consumable.Charges);
            } else if (item is Usable) {
                Usable usable = (Usable)item;
                if (usable.Charges == 0) continue;
                text += String.Format("{2}{0}. {1} ({3})\n", i++, item.Name, selected, usable.Charges);
            }
        }
        text += "0. Back";
        return text;
    }

    private string getExpendCharges() {
        string text = "Expend charges\n";
        int availableForUse = Math.Min(selectedUsable.MaximumChargePerUse, selectedUsable.Charges);
        text += String.Format("1-{0} Charges ({1} left)\n", availableForUse, selectedUsable.Charges);
        text += "0. Back";
        return text;
    }

    private void equipSelectedItems() {
        CharacterSheet hero = (CharacterSheet)battle.CurrentCombatant;
        foreach (string name in selectedTargets) {
            foreach (Item item in hero.Inventory.Bag) {
                if (name == item.ToString()) {
                    hero.Equip(item);
                }
            }
        }
        timer = 0.5f;
    }

    private void useSelectedItem() {
        CharacterSheet hero = (CharacterSheet)battle.CurrentCombatant;
        Combatant[] targets = new Combatant[selectedTargets.Count];
        for (int i = 0; i < selectedTargets.Count; i++) {
            targets[i] = combattants[selectedTargets[i]].Combatant;
        }
        hero.Use(selectedUsable, selectedCharges, battle, targets);
        timer = 2.0f;
        foreach (Combatant target in targets) {
            setAttackDefendSprites(battle.CurrentCombatant, target);
        }
    }
}
}
