using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using srd5;

public class CombatBehavior : MonoBehaviour, KeyInputReceiver {
    public Text MenuText, CombatLog;
    public Image HeroFront1, HeroFront2, HeroBack1, HeroBack2;
    private int heroFront1 = -1, heroFront2 = -1, heroBack1 = -1, heroBack2 = -1;
    public Image[] LargeEnemies, MediumEnemies;
    public Sprite[] DwarfBarbarianSprites, DwarfDruidSprites, ElfBarbarianSprites, ElfDruidSprites;
    public Sprite[] DwarfWizardSprites, ElfWizardSprites, HumanBarbarianSprites, HumanDruidSprites, HumanWizardSprites;
    public Sprite[] BoarSprites, OrcSprites, OgreSprites;
    public Image[] Conditions;

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

    private ConditionIconBehavior iconBehavior;
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

    public void KeyPressHandler(KeyCode code) {
        if (timer > 0) return;
        if (state == CombatState.YOUR_TURN_SELECT_ACTION) {
            if (code == KeyCode.Alpha1) {
                state = CombatState.ATTACK_SELECT_TARGET;
                MenuText.text = getAttackText();
            } else if (code == KeyCode.Alpha2) {
                state = CombatState.SPELL_SELECT_SPELL;
                MenuText.text = getSelectSpellsText();
                selectedSpell = null;
                selectedSlot = SpellLevel.CANTRIP;
                selectedTargets.Clear();
            } else if (code == KeyCode.Alpha3) {
                state = CombatState.ITEM_SELECT_ACTION;
                MenuText.text = SELECT_ITEM_ACTION;
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
                foreach (CharacterSheet hero in Game.Characters) {
                    hero.LongRest();
                }
                SceneManager.LoadScene("MainMenu");
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
                        MenuText.text = getSelectTargetsText();
                    }
                } else {
                    state = CombatState.SPELL_SELECT_SLOT;
                    MenuText.text = getSelectSpellSlotText();
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
                    MenuText.text = getSelectTargetsText();
                }
            }
        } else if (state == CombatState.SPELL_SELECT_TARGETS) {
            if (code == KeyCode.Alpha0) {
                castSpell();
            } else {
                int index = code - KeyCode.Alpha1;
                toggleSelectedTarget(index);
                if (selectedTargets.Count == selectedSpell.MaximumTargets) castSpell();
                MenuText.text = getSelectTargetsText();
            }
        } else if (state == CombatState.ITEM_SELECT_ACTION) {
            if (code == KeyCode.Alpha0) {
                returnToMain();
            } else if (code == KeyCode.Alpha1) {
                state = CombatState.ITEM_EQUIP_SELECT_ITEMS;
                MenuText.text = getEquippableItems();
            } else if (code == KeyCode.Alpha2) {
                state = CombatState.ITEM_USE_SELECT_ITEM;
                MenuText.text = getUsableItems();
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
                MenuText.text = getEquippableItems();
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
                            MenuText.text = getSelectTargetsText();
                        } else {
                            state = CombatState.ITEM_USE_SELECT_CHARGES;
                            MenuText.text = getExpendCharges();
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
                MenuText.text = getSelectTargetsText();
            }
        } else if (state == CombatState.ITEM_USE_SELECT_TARGETS) {
            if (code == KeyCode.Alpha0) {
                useSelectedItem();
            } else {
                int index = code - KeyCode.Alpha1;
                toggleSelectedTarget(index);
                if (selectedTargets.Count == selectedUsable.MaximumTargets) useSelectedItem();
                MenuText.text = getSelectTargetsText();
            }
        }
    }

    private void returnToMain() {
        state = CombatState.YOUR_TURN_SELECT_ACTION;
        MenuText.text = SELECT_ACTION;
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
            if (condition.Removed) {
                combattants[nameOf(condition.Bearer)].ConditionIcons[condition.Condition].gameObject.SetActive(false);
            } else {
                switch (condition.Condition) {
                    case ConditionType.UNCONSCIOUS:
                        Image icon = Instantiate<Image>(Conditions[0]);
                        icon.gameObject.transform.SetParent(combattants[nameOf(condition.Bearer)].Image.gameObject.transform, false);
                        combattants[nameOf(condition.Bearer)].ConditionIcons[condition.Condition] = icon;
                        icon.gameObject.transform.position = combattants[nameOf(condition.Bearer)].Image.gameObject.transform.position + new Vector3(20, 20);
                        icon.gameObject.SetActive(true);
                        iconBehavior.Icons.Add(icon);
                        break;
                }
            }
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
            Debug.Log("Unexpected Event.");
        }
    }

    // Start is called before the first frame update
    void Start() {
        enemies = new Monster[2];
        battle = new BattleGroundClassic();
        MenuText.text = WAITING_FOR_ENEMIES;
        logQueue = new FixedSizedQueue<string>(11);
        iconBehavior = gameObject.GetComponent<ConditionIconBehavior>();
        GlobalEvents.Handlers += combatLogListener;
        int i = 0;
        int front = 0;
        int back = 0;
        foreach (CharacterSheet hero in Game.Characters) {
            if (i == Game.FrontLineChar1 || i == Game.FrontLineChar2) {
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

        Sprite[] enemySprites = null;
        Image[] enemyImages = null;
        if (Game.Scene == Game.CombatScene.BOARS) {
            enemies[0] = Monsters.Boar;
            enemies[1] = Monsters.Boar;
            enemySprites = BoarSprites;
            enemyImages = MediumEnemies;
        } else if (Game.Scene == Game.CombatScene.OGRES) {
            enemies[0] = Monsters.Ogre;
            enemies[1] = Monsters.Ogre;
            enemySprites = OgreSprites;
            enemyImages = LargeEnemies;
        } else if (Game.Scene == Game.CombatScene.ORCS) {
            enemies[0] = Monsters.Orc;
            enemies[1] = Monsters.Orc;
            enemySprites = OrcSprites;
            enemyImages = MediumEnemies;
        }

        for (i = 0; i < enemies.Length; i++) {
            enemyImages[i].gameObject.SetActive(true);
            enemyImages[i].sprite = enemySprites[0];
            battle.AddCombatant(enemies[i], ClassicLocation.Row.FRONT_RIGHT);
            monsterNames[enemies[i]] = nameOf(enemies[i]) + " #" + i;
            combattants.Add(nameOf(enemies[i]), new CombatantData(enemies[i], enemyImages[i], enemySprites));
        }
        battle.Initialize();
        timer = 2.0f;
    }

    void OnDestroy() {
        GlobalEvents.Handlers -= combatLogListener;
    }

    private Sprite[] heroSprites(CharacterSheet hero) {
        Class heroClass = hero.Levels[0].Class.Class;
        if (hero.Race.Race == Race.HILL_DWARF)
            return heroClass == Class.WIZARD ? DwarfWizardSprites : heroClass == Class.DRUID ? DwarfDruidSprites : DwarfBarbarianSprites;
        if (hero.Race.Race == Race.HIGH_ELF)
            return heroClass == Class.WIZARD ? ElfWizardSprites : heroClass == Class.DRUID ? ElfDruidSprites : ElfBarbarianSprites;
        return heroClass == Class.WIZARD ? HumanWizardSprites : heroClass == Class.DRUID ? HumanDruidSprites : HumanBarbarianSprites;
    }

    private void assignSprites(CharacterSheet hero, Image heroImage) {
        heroImage.gameObject.SetActive(true);
        Sprite[] sprites = heroSprites(hero);
        heroImage.sprite = sprites[0];
        combattants.Add(nameOf(hero), new CombatantData(hero, heroImage, sprites));
    }

    // Update is called once per frame
    void Update() {
        if (timer > 0) {
            timer -= Time.deltaTime;
            if (timer > 0) return;
            nextPhase();
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
                MenuText.text = SELECT_ACTION;
            }
            return;
        } else if (battle.CurrentCombatant.HitPoints <= 0) {
            nextPhase();
            return;
        }
        MenuText.text = WAITING_FOR_ENEMIES;
        // select target
        CharacterSheet target = null;
        foreach (int index in new int[] { heroFront1, heroFront2, heroBack1, heroBack2 }) {
            if (index == -1) continue;
            CharacterSheet hero = Game.Characters[index];
            if (hero.HitPoints > 0) {
                target = hero;
                break;
            }
        }
        if (target == null) {
            state = CombatState.LOST;
            MenuText.text = LOST;
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
            data.Image.sprite = data.Sprites[0];
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
            MenuText.text = LOST;
        } else if (!monsterAlive) {
            state = CombatState.WON;
            MenuText.text = WON;
        }
    }

    private void setAttackDefendSprites(Combatant attacker, Combatant target) {
        combattants[nameOf(attacker)].Image.sprite = combattants[nameOf(attacker)].Sprites[1];
        combattants[nameOf(target)].Image.sprite = combattants[nameOf(target)].Sprites[2];
    }

    private void log(string log) {
        logQueue.Enqueue(log);
        string[] lines = logQueue.ToArray();
        CombatLog.text = "";
        foreach (string line in lines) {
            if (CombatLog.text.Length > 0) CombatLog.text += "\n";
            CombatLog.text += line;
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
