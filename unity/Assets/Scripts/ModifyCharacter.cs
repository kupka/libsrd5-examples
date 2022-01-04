using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using srd5;

public class ModifyCharacter : MonoBehaviour, KeyInputReceiver {
    public Image CharacterImage;
    public Text CharacterName, CharacterText, MenuText;
    public Sprite DwarfBerserker, DwarfDruid, ElfBerserker, ElfDruid;

    enum ModifyCharacterState {
        DEFAULT,
        EQUIP_MELEE_WEAPONS,
        EQUIP_RANGED_WEAPONS,
        EQUIP_ARMOR,
        PREPARE_SPELLS
    }

    enum ModifyCharacterPage {
        ABILITIES_AND_ATTACKS,
        FEATS_AND_PROFICIENCIES,
        SPELLS
    }

    private ModifyCharacterState state = ModifyCharacterState.DEFAULT;
    private ModifyCharacterPage page = ModifyCharacterPage.ABILITIES_AND_ATTACKS;

    private CharacterSheet hero;

    private const string DEFAULT_TEXT =
@"1. Next Page
2. Previous Page
3. Equip Melee Weapons
4. Equip Ranged Weapons
5. Equip Armor 
6. Prepare Spells
0. Return to Main Menu";

    private const string EQUIP_MELEE_WEAPONS =
@"1. Club
2. Quarterstaff (Versatile)
3. Shield (Offhand)
4. Handaxe
5. Greatclub (Both Hands)
0. Return";

    private const string EQUIP_RANGED_WEAPONS =
@"1. Shortbow
2. Longbow
0. Return";

    private const string EQUIP_ARMOR =
@"1. Padded Armor
2. Leather Armor
3. Studded Leader Armor
4. Hide Armor
0. Return";

    private const string PREPARE_SPELLS =
@"Prepared all available spells from libsrd5. More options will be available here when libsrd5 implements more spells.
0. Return";

    // Start is called before the first frame update
    void Start() {
        hero = Game.Characters[Game.SelectedCharacter];
        MenuText.text = DEFAULT_TEXT;
        CharacterName.text = hero.Name;
        if (hero.Race.Race == Race.HILL_DWARF) {
            if (hero.Levels[0].Class.Class == Class.DRUID)
                CharacterImage.sprite = DwarfDruid;
            else
                CharacterImage.sprite = DwarfBerserker;
        } else {
            if (hero.Levels[0].Class.Class == Class.DRUID)
                CharacterImage.sprite = ElfDruid;
            else
                CharacterImage.sprite = ElfBerserker;
        }
        displayAbilitiesAndAttacks();
    }

    // Update is called once per frame
    void Update() {

    }

    private void displayAbilitiesAndAttacks() {
        CharacterText.text = String.Format("{0}\nLevel {1} {2}\n\n", hero.Race.Name, hero.Levels[0].Levels, hero.Levels[0].Class.Name);
        CharacterText.text += "<color=yellow>Abilties</color>\n";
        CharacterText.text += String.Format("STR: {0,2} ({1,2})   DEX: {2,2} ({3,2})\nCON: {4,2} ({5,2})   CHA: {6,2} ({7,2})\nINT: {8,2} ({9,2})   WIS: {10,2} ({11,2})\n\n",
            hero.Strength.Value, hero.Strength.Modifier.ToString("+#;-#;0"), hero.Dexterity.Value, hero.Dexterity.Modifier.ToString("+#;-#;0"),
            hero.Constitution.Value, hero.Constitution.Modifier.ToString("+#;-#;0"), hero.Charisma.Value, hero.Charisma.Modifier.ToString("+#;-#;0"),
            hero.Intelligence.Value, hero.Intelligence.Modifier.ToString("+#;-#;0"), hero.Wisdom.Value, hero.Wisdom.Modifier.ToString("+#;-#;0"));
        CharacterText.text += String.Format("Hitpoints: {0}   Proficiency: {1}   Armor Class: {2}\n\n", hero.HitPointsMax, hero.Proficiency, hero.ArmorClass);
        CharacterText.text += "<color=yellow>Melee Attacks</color>\n";
        if (hero.MeleeAttacks.Length > 0) {
            foreach (Attack attack in hero.MeleeAttacks) {
                CharacterText.text += String.Format("{0}, {1} To Hit, {2} {3} Damage\n", attack.Name, attack.AttackBonus.ToString("+#;-#;0"),
                attack.Damage.Dices.ToString(), attack.Damage.Type.Name());
            }
        }
        CharacterText.text += "\n<color=yellow>Ranged Attacks</color>\n";
        if (hero.RangedAttacks.Length > 0) {
            foreach (Attack attack in hero.RangedAttacks) {
                CharacterText.text += String.Format("{0}, {1} To Hit, {2} {3} Damage\n", attack.Name, attack.AttackBonus.ToString("+#;-#;0"),
                attack.Damage.Dices.ToString(), attack.Damage.Type.Name());
            }
        }
    }

    private void displayFeatsAndProficiencies() {
        CharacterText.text = "<color=yellow>Feats</color>\n";
        int i = 0;
        foreach (Feat feat in hero.Feats) {
            String name = feat.Name();
            if (name.Length > 25)
                name = name.Substring(0, 22) + "...";
            else
                name += new string(' ', 25 - name.Length);
            CharacterText.text += String.Format("{0}", name);
            if (i++ % 2 == 1)
                CharacterText.text += "\n";
            else
                CharacterText.text += "  ";
        }
        CharacterText.text += "\n\n<color=yellow>Proficiencies</color>\n";
        i = 0;
        foreach (Proficiency proficiency in hero.Proficiencies) {
            String name = proficiency.Name();
            if (name.Length > 25)
                name = name.Substring(0, 22) + "...";
            else
                name += new string(' ', 25 - name.Length);
            CharacterText.text += String.Format("{0}", name);
            if (i++ % 2 == 1)
                CharacterText.text += "\n";
            else
                CharacterText.text += "  ";
        }
    }

    private void displaySpells() {
        CharacterText.text = "<color=yellow>Spellslots</color>\n\n";
        if (hero.AvailableSpells.Length == 0) {
            CharacterText.text += "None";
            return;
        }
        CharacterText.text += "Can  1st  2nd  3rd  4th  5th  6th  7th  8th  9th\n";
        foreach (AvailableSpells spells in hero.AvailableSpells) {
            CharacterText.text += String.Format("{0,3}  {1,3}  {2,3}  {3,3}  {4,3}  {5,3}  {6,3}  {7,3}  {8,3}  {9,3}\n",
                spells.SlotsMax[0], spells.SlotsMax[1], spells.SlotsMax[2], spells.SlotsMax[3], spells.SlotsMax[4],
                spells.SlotsMax[5], spells.SlotsMax[6], spells.SlotsMax[7], spells.SlotsMax[8], spells.SlotsMax[9]
            );
        }

        CharacterText.text += "\n<color=yellow>Prepared Spells</color>\n\n";
        int i = 0;
        foreach (AvailableSpells spells in hero.AvailableSpells) {
            foreach (Spell spell in spells.PreparedSpells) {
                String name = spell.Name;
                if (name.Length > 25)
                    name = name.Substring(0, 22) + "...";
                else
                    name += new string(' ', 25 - name.Length);
                CharacterText.text += String.Format("{0}", name);
                if (i++ % 2 == 1)
                    CharacterText.text += "\n";
                else
                    CharacterText.text += "  ";
            }
        }
    }

    private void prepareDruidSpells() {
        if (hero.Levels[0].Class.Class != Class.DRUID) return;
        AvailableSpells druidSpells = null;
        foreach (AvailableSpells spells in hero.AvailableSpells) {
            if (spells.CharacterClass.Class == Class.DRUID)
                druidSpells = spells;
        }
        druidSpells.AddKnownSpell(Spells.Shillelagh, Spells.CharmPerson, Spells.CureWounds, Spells.HealingWord);
        druidSpells.AddPreparedSpell(Spells.Shillelagh, Spells.CharmPerson, Spells.CureWounds, Spells.HealingWord);
    }

    public void KeyPressHandler(KeyCode code) {
        if (state == ModifyCharacterState.DEFAULT) {
            if (code == KeyCode.Alpha0) {
                SceneManager.LoadScene("MainMenu");
            } else if (code == KeyCode.Alpha1 || code == KeyCode.Alpha2) {
                if (code == KeyCode.Alpha1)
                    if (++page > ModifyCharacterPage.SPELLS) page = ModifyCharacterPage.ABILITIES_AND_ATTACKS;
                if (code == KeyCode.Alpha2)
                    if (--page < ModifyCharacterPage.ABILITIES_AND_ATTACKS) page = ModifyCharacterPage.SPELLS;
                switch (page) {
                    case ModifyCharacterPage.ABILITIES_AND_ATTACKS:
                        displayAbilitiesAndAttacks();
                        break;
                    case ModifyCharacterPage.FEATS_AND_PROFICIENCIES:
                        displayFeatsAndProficiencies();
                        break;
                    case ModifyCharacterPage.SPELLS:
                        displaySpells();
                        break;
                }
            } else if (code == KeyCode.Alpha3) {
                state = ModifyCharacterState.EQUIP_MELEE_WEAPONS;
                MenuText.text = EQUIP_MELEE_WEAPONS;
                page = ModifyCharacterPage.ABILITIES_AND_ATTACKS;
                displayAbilitiesAndAttacks();
            } else if (code == KeyCode.Alpha4) {
                state = ModifyCharacterState.EQUIP_RANGED_WEAPONS;
                MenuText.text = EQUIP_RANGED_WEAPONS;
                page = ModifyCharacterPage.ABILITIES_AND_ATTACKS;
                displayAbilitiesAndAttacks();
            } else if (code == KeyCode.Alpha5) {
                state = ModifyCharacterState.EQUIP_ARMOR;
                MenuText.text = EQUIP_ARMOR;
                page = ModifyCharacterPage.ABILITIES_AND_ATTACKS;
                displayAbilitiesAndAttacks();
            } else if (code == KeyCode.Alpha6) {
                state = ModifyCharacterState.PREPARE_SPELLS;
                MenuText.text = PREPARE_SPELLS;
                page = ModifyCharacterPage.SPELLS;
                prepareDruidSpells();
                displaySpells();
            }
        } else if (state == ModifyCharacterState.EQUIP_MELEE_WEAPONS) {
            if (code == KeyCode.Alpha0) {
                state = ModifyCharacterState.DEFAULT;
                MenuText.text = DEFAULT_TEXT;
            } else if (code == KeyCode.Alpha1) {
                if (hero.Inventory.MainHand != null)
                    hero.Unequip(hero.Inventory.MainHand);
                hero.Equip(new Thing<Weapon>(Weapons.Club));
            } else if (code == KeyCode.Alpha2) {
                if (hero.Inventory.MainHand != null)
                    hero.Unequip(hero.Inventory.MainHand);
                hero.Equip(new Thing<Weapon>(Weapons.Quarterstaff));
            } else if (code == KeyCode.Alpha3) {
                hero.Equip(new Thing<Shield>(Shields.Shield));
            } else if (code == KeyCode.Alpha4) {
                if (hero.Inventory.MainHand != null)
                    hero.Unequip(hero.Inventory.MainHand);
                hero.Equip(new Thing<Weapon>(Weapons.Handaxe));
            } else if (code == KeyCode.Alpha5) {
                hero.Equip(new Thing<Weapon>(Weapons.Greatclub));
            }
            displayAbilitiesAndAttacks();
        } else if (state == ModifyCharacterState.EQUIP_RANGED_WEAPONS) {
            if (code == KeyCode.Alpha0) {
                state = ModifyCharacterState.DEFAULT;
                MenuText.text = DEFAULT_TEXT;
            } else if (code == KeyCode.Alpha1) {
                hero.Equip(new Thing<Weapon>(Weapons.Shortbow));
            } else if (code == KeyCode.Alpha2) {
                hero.Equip(new Thing<Weapon>(Weapons.Longbow));
            }
            displayAbilitiesAndAttacks();
        } else if (state == ModifyCharacterState.EQUIP_ARMOR) {
            if (code == KeyCode.Alpha0) {
                state = ModifyCharacterState.DEFAULT;
                MenuText.text = DEFAULT_TEXT;
            } else if (code == KeyCode.Alpha1) {
                hero.Equip(new Thing<Armor>(Armors.PaddedArmor));
            } else if (code == KeyCode.Alpha2) {
                hero.Equip(new Thing<Armor>(Armors.LeatherArmor));
            } else if (code == KeyCode.Alpha3) {
                hero.Equip(new Thing<Armor>(Armors.StuddedLeatherArmor));
            } else if (code == KeyCode.Alpha4) {
                hero.Equip(new Thing<Armor>(Armors.HideArmor));
            }
            displayAbilitiesAndAttacks();
        } else if (state == ModifyCharacterState.PREPARE_SPELLS) {
            if (code == KeyCode.Alpha0) {
                state = ModifyCharacterState.DEFAULT;
                MenuText.text = DEFAULT_TEXT;
            }
        }
    }
}
