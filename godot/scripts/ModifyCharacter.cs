using System;
using System.Collections.Generic;
using Godot;
using srd5;

public partial class ModifyCharacter : Control, IKeyInputReceiver {
    private TextureRect CharacterImage;
    private Label CharacterName, MenuText;
    private RichTextLabel CharacterText; // BBCode enabled, replaces Unity rich text
    private Texture2D DwarfBerserker, DwarfDruid, DwarfWizard, ElfBerserker, ElfDruid, ElfWizard, HumanBerserker, HumanDruid, HumanWizard;

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
        INVENTORY,
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
7. Add a Healing Potion
8. Add a Wand of Magic Missiles (makes fight trivial)
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

    public override void _Ready() {
        CharacterImage = GetNode<TextureRect>("CharacterImage");
        CharacterName = GetNode<Label>("CharacterName");
        CharacterText = GetNode<RichTextLabel>("CharacterText");
        MenuText = GetNode<Label>("MenuText");
        DwarfBerserker = GD.Load<Texture2D>(Game.WESNOTH + "dwarf_berserker.png");
        DwarfDruid = GD.Load<Texture2D>(Game.WESNOTH + "dwarf_druid.png");
        ElfBerserker = GD.Load<Texture2D>(Game.WESNOTH + "elf_berserker.png");
        ElfDruid = GD.Load<Texture2D>(Game.WESNOTH + "elf_druid.png");
        DwarfWizard = GD.Load<Texture2D>(Game.WESNOTH + "dwarf_wizard.png");
        ElfWizard = GD.Load<Texture2D>(Game.WESNOTH + "elf_wizard.png");
        HumanBerserker = GD.Load<Texture2D>(Game.WESNOTH + "human_berserker.png");
        HumanDruid = GD.Load<Texture2D>(Game.WESNOTH + "human_druid.png");
        HumanWizard = GD.Load<Texture2D>(Game.WESNOTH + "human_wizard.png");

        hero = Game.Characters[Game.SelectedCharacter];
        MenuText.Text = DEFAULT_TEXT;
        CharacterName.Text = hero.Name;
        Class heroClass = hero.Levels[0].Class.Class;
        if (hero.Race.Race == Race.HILL_DWARF)
            CharacterImage.Texture = heroClass == Class.WIZARD ? DwarfWizard : heroClass == Class.DRUID ? DwarfDruid : DwarfBerserker;
        else if (hero.Race.Race == Race.HIGH_ELF)
            CharacterImage.Texture = heroClass == Class.WIZARD ? ElfWizard : heroClass == Class.DRUID ? ElfDruid : ElfBerserker;
        else
            CharacterImage.Texture = heroClass == Class.WIZARD ? HumanWizard : heroClass == Class.DRUID ? HumanDruid : HumanBerserker;
        displayAbilitiesAndAttacks();
    }

    private void displayAbilitiesAndAttacks() {
        CharacterText.Text = String.Format("{0}\nLevel {1} {2}\n\n", Texts.RaceName(hero.Race.Race), hero.Levels[0].Levels, Texts.ClassName(hero.Levels[0].Class.Class));
        CharacterText.Text += "[color=yellow]Abilties[/color]\n";
        CharacterText.Text += String.Format("STR: {0,2} ({1,2})   DEX: {2,2} ({3,2})\nCON: {4,2} ({5,2})   CHA: {6,2} ({7,2})\nINT: {8,2} ({9,2})   WIS: {10,2} ({11,2})\n\n",
            hero.Strength.Value, hero.Strength.Modifier.ToString("+#;-#;0"), hero.Dexterity.Value, hero.Dexterity.Modifier.ToString("+#;-#;0"),
            hero.Constitution.Value, hero.Constitution.Modifier.ToString("+#;-#;0"), hero.Charisma.Value, hero.Charisma.Modifier.ToString("+#;-#;0"),
            hero.Intelligence.Value, hero.Intelligence.Modifier.ToString("+#;-#;0"), hero.Wisdom.Value, hero.Wisdom.Modifier.ToString("+#;-#;0"));
        CharacterText.Text += String.Format("Hitpoints: {0}   Proficiency: {1}   Armor Class: {2}\n\n", hero.HitPointsMax, hero.ProficiencyBonus, hero.ArmorClass);
        CharacterText.Text += "[color=yellow]Melee Attacks[/color]\n";
        if (hero.MeleeAttacks.Length > 0) {
            foreach (Attack attack in hero.MeleeAttacks) {
                CharacterText.Text += String.Format("{0}, {1} To Hit, {2} {3} Damage\n", attack.Name, attack.AttackBonus.ToString("+#;-#;0"),
                attack.Damage.Dice.ToString(), attack.Damage.Type.Name());
            }
        }
        CharacterText.Text += "\n[color=yellow]Ranged Attacks[/color]\n";
        if (hero.RangedAttacks.Length > 0) {
            foreach (Attack attack in hero.RangedAttacks) {
                CharacterText.Text += String.Format("{0}, {1} To Hit, {2} {3} Damage\n", attack.Name, attack.AttackBonus.ToString("+#;-#;0"),
                attack.Damage.Dice.ToString(), attack.Damage.Type.Name());
            }
        }
    }

    private void displayFeatsAndProficiencies() {
        CharacterText.Text = "[color=yellow]Feats[/color]\n";
        int i = 0;
        foreach (Feat feat in hero.Feats) {
            String name = feat.Name();
            if (name.Length > 25)
                name = name.Substring(0, 22) + "...";
            else
                name += new string(' ', 25 - name.Length);
            CharacterText.Text += String.Format("{0}", name);
            if (i++ % 2 == 1)
                CharacterText.Text += "\n";
            else
                CharacterText.Text += "  ";
        }
        CharacterText.Text += "\n\n[color=yellow]Proficiencies[/color]\n";
        i = 0;
        foreach (Proficiency proficiency in hero.Proficiencies) {
            String name = proficiency.Name();
            if (name.Length > 25)
                name = name.Substring(0, 22) + "...";
            else
                name += new string(' ', 25 - name.Length);
            CharacterText.Text += String.Format("{0}", name);
            if (i++ % 2 == 1)
                CharacterText.Text += "\n";
            else
                CharacterText.Text += "  ";
        }
    }

    private void displaySpells() {
        CharacterText.Text = "[color=yellow]Spellslots[/color]\n\n";
        if (hero.AvailableSpells.Length == 0) {
            CharacterText.Text += "None";
            return;
        }
        CharacterText.Text += "Can  1st  2nd  3rd  4th  5th  6th  7th  8th  9th\n";
        foreach (AvailableSpells spells in hero.AvailableSpells) {
            CharacterText.Text += String.Format("{0,3}  {1,3}  {2,3}  {3,3}  {4,3}  {5,3}  {6,3}  {7,3}  {8,3}  {9,3}\n",
                spells.SlotsMax[0], spells.SlotsMax[1], spells.SlotsMax[2], spells.SlotsMax[3], spells.SlotsMax[4],
                spells.SlotsMax[5], spells.SlotsMax[6], spells.SlotsMax[7], spells.SlotsMax[8], spells.SlotsMax[9]
            );
        }

        CharacterText.Text += "\n[color=yellow]Prepared Spells[/color]\n\n";
        int i = 0;
        foreach (AvailableSpells spells in hero.AvailableSpells) {
            foreach (Spell spell in spells.PreparedSpells) {
                String name = spell.Name;
                Game.StringPad(ref name, 25);
                CharacterText.Text += String.Format("{0}", name);
                if (i++ % 2 == 1)
                    CharacterText.Text += "\n";
                else
                    CharacterText.Text += "  ";
            }
        }
    }

    private string emptyIfNull(Item item) {
        if (item == null)
            return "Empty";
        else
            return item.Name;
    }

    private void displayInventory() {
        CharacterText.Text = "[color=yellow]Inventory[/color]\n";
        CharacterText.Text += String.Format("Mainhand: {0}\n", emptyIfNull(hero.Inventory.MainHand));
        CharacterText.Text += String.Format("Offhand:  {0}\n", emptyIfNull(hero.Inventory.OffHand));
        CharacterText.Text += String.Format("Armor:    {0}\n", emptyIfNull(hero.Inventory.Armor));
        CharacterText.Text += "\n[color=yellow]Items in backpack[/color]\n";
        foreach (Item item in hero.Inventory.Bag) {
            CharacterText.Text += String.Format("{0}\n", item.Name);
        }
    }

    // Druids and wizards learn and prepare a fixed selection of spells implemented in libsrd5
    private void prepareSpells() {
        Class heroClass = hero.Levels[0].Class.Class;
        Spell[] selection;
        if (heroClass == Class.DRUID)
            selection = new Spell[] { Spells.Shillelagh, Spells.CharmPerson, Spells.CureWounds, Spells.HealingWord };
        else if (heroClass == Class.WIZARD)
            selection = new Spell[] { Spells.FireBolt, Spells.RayOfFrost, Spells.MagicMissile, Spells.BurningHands, Spells.Sleep };
        else
            return;
        AvailableSpells classSpells = null;
        foreach (AvailableSpells spells in hero.AvailableSpells) {
            if (spells.CharacterClass.Class == heroClass)
                classSpells = spells;
        }
        if (classSpells == null) return;
        classSpells.AddKnownSpell(selection);
        classSpells.AddPreparedSpell(selection);
    }

    public void KeyPressHandler(Key code) {
        if (state == ModifyCharacterState.DEFAULT) {
            if (code == Key.Key0) {
                hero.LongRest();
                Game.LoadScene(this, "MainMenu");
            } else if (code == Key.Key1 || code == Key.Key2) {
                if (code == Key.Key1)
                    if (++page > ModifyCharacterPage.SPELLS) page = ModifyCharacterPage.ABILITIES_AND_ATTACKS;
                if (code == Key.Key2)
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
                    case ModifyCharacterPage.INVENTORY:
                        displayInventory();
                        break;
                }
            } else if (code == Key.Key3) {
                state = ModifyCharacterState.EQUIP_MELEE_WEAPONS;
                MenuText.Text = EQUIP_MELEE_WEAPONS;
                page = ModifyCharacterPage.ABILITIES_AND_ATTACKS;
                displayAbilitiesAndAttacks();
            } else if (code == Key.Key4) {
                state = ModifyCharacterState.EQUIP_RANGED_WEAPONS;
                MenuText.Text = EQUIP_RANGED_WEAPONS;
                page = ModifyCharacterPage.ABILITIES_AND_ATTACKS;
                displayAbilitiesAndAttacks();
            } else if (code == Key.Key5) {
                state = ModifyCharacterState.EQUIP_ARMOR;
                MenuText.Text = EQUIP_ARMOR;
                page = ModifyCharacterPage.ABILITIES_AND_ATTACKS;
                displayAbilitiesAndAttacks();
            } else if (code == Key.Key6) {
                state = ModifyCharacterState.PREPARE_SPELLS;
                MenuText.Text = PREPARE_SPELLS;
                page = ModifyCharacterPage.SPELLS;
                prepareSpells();
                displaySpells();
            } else if (code == Key.Key7) {
                // Don't add if one is already in inventory
                foreach (Item item in hero.Inventory.Bag) {
                    if (item.Is(Potions.PotionOfHealing))
                        return;
                }
                hero.Inventory.AddToBag(Potions.PotionOfHealing);
                page = ModifyCharacterPage.INVENTORY;
                displayInventory();
            } else if (code == Key.Key8) {
                // Replace if one is already in inventory (charges could be used up)
                foreach (Item item in hero.Inventory.Bag) {
                    if (item.Is(Wands.WandOfMagicMissiles)) {
                        hero.Inventory.RemoveFromBag(item);
                        break;
                    }
                }
                hero.Inventory.AddToBag(Wands.WandOfMagicMissiles);
                page = ModifyCharacterPage.INVENTORY;
                displayInventory();
            }
        } else if (state == ModifyCharacterState.EQUIP_MELEE_WEAPONS) {
            if (code == Key.Key0) {
                state = ModifyCharacterState.DEFAULT;
                MenuText.Text = DEFAULT_TEXT;
            } else if (code == Key.Key1) {
                hero.Unequip(hero.Inventory.MainHand);
                hero.Equip(Weapons.Club);
            } else if (code == Key.Key2) {
                hero.Unequip(hero.Inventory.MainHand);
                hero.Equip(Weapons.Quarterstaff);
            } else if (code == Key.Key3) {
                hero.Equip(Shields.Shield);
            } else if (code == Key.Key4) {
                hero.Unequip(hero.Inventory.MainHand);
                hero.Equip(Weapons.Handaxe);
            } else if (code == Key.Key5) {
                hero.Equip(Weapons.Greatclub);
            }
            displayAbilitiesAndAttacks();
            clearInventoryClutter();
        } else if (state == ModifyCharacterState.EQUIP_RANGED_WEAPONS) {
            if (code == Key.Key0) {
                state = ModifyCharacterState.DEFAULT;
                MenuText.Text = DEFAULT_TEXT;
            } else if (code == Key.Key1) {
                hero.Equip(Weapons.Shortbow);
            } else if (code == Key.Key2) {
                hero.Equip(Weapons.Longbow);
            }
            displayAbilitiesAndAttacks();
            clearInventoryClutter();
        } else if (state == ModifyCharacterState.EQUIP_ARMOR) {
            if (code == Key.Key0) {
                state = ModifyCharacterState.DEFAULT;
                MenuText.Text = DEFAULT_TEXT;
            } else if (code == Key.Key1) {
                hero.Equip(Armors.PaddedArmor);
            } else if (code == Key.Key2) {
                hero.Equip(Armors.LeatherArmor);
            } else if (code == Key.Key3) {
                hero.Equip(Armors.StuddedLeatherArmor);
            } else if (code == Key.Key4) {
                hero.Equip(Armors.HideArmor);
            }
            displayAbilitiesAndAttacks();
            clearInventoryClutter();
        } else if (state == ModifyCharacterState.PREPARE_SPELLS) {
            if (code == Key.Key0) {
                state = ModifyCharacterState.DEFAULT;
                MenuText.Text = DEFAULT_TEXT;
            }
        }
    }

    private void clearInventoryClutter() {
        // keep a maximum of one melee weapon, one ranged weapon and one shield in bag
        List<Item> toRemove = new List<Item>();
        bool melee = false, shield = false, ranged = false;
        foreach (Item item in hero.Inventory.Bag) {
            if (item is Weapon) {
                Weapon weapon = (Weapon)item;
                if (weapon.HasProperty(WeaponProperty.AMMUNITION)) {
                    if (ranged)
                        toRemove.Add(weapon);
                    else
                        ranged = true;
                } else {
                    if (melee)
                        toRemove.Add(weapon);
                    else
                        melee = true;
                }
            } else if (item is Shield) {
                if (shield)
                    toRemove.Add(item);
                else
                    shield = true;
            } else if (item is Armor) {
                toRemove.Add(item);
            }
        }
        foreach (Item item in toRemove) {
            hero.Inventory.RemoveFromBag(item);
        }
    }
}
