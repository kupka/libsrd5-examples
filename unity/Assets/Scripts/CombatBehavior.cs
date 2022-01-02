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
    public Image Orc1, Orc2, Orc3;
    public Image Ogre1, Ogre2;

    public Sprite ElfBerserker, ElfBerserkerAttack, ElfBersekerDefend;
    public Sprite DwarfBerserker, DwarfBerserkerAttack, DwarfBeserkerDefend;
    public Sprite ElfDruid, ElfDruidAttack, ElfDruidDefend;
    public Sprite DwarfDruid, DwarfDruidAttack, DwarfDruidDefend;
    public Sprite Orc, OrcAttack, OrcDefend;
    public Sprite Ogre, OgreAttack, OgreDefend;

    private Monster orc1 = Monsters.Orc, orc2 = Monsters.Orc, orc3 = Monsters.Orc;
    private Monster ogre1 = Monsters.Ogre, ogre2 = Monsters.Ogre;
    private BattleGroundClassic ground = new BattleGroundClassic();
    private FixedSizedQueue<string> logQueue = new FixedSizedQueue<string>(11);
    private float timer;
    private bool waitForTimer = false;

    private const string DEFAULT_TEXT = "Waiting for enemies...";
    private const string ATTACK_ORCS =
    @"1. Attack first Orc
    2. Attack second Orc
    3. Attack third Orc";

    private const string ATTACK_OGRES =
@"1. Attack first Ogre
    2. Attack second Ogre";

    private const string WON =
@"You won!
0. Return to Main Menu";
    private const string LOST =
@"You lost!
0. Return to Main Menu";

    enum CombatState {
        ENEMY_TURN,
        YOUR_TURN,
        WON,
        LOST
    }

    private CombatState state = CombatState.ENEMY_TURN;

    private Dictionary<string, CombattantData> combattants = new Dictionary<string, CombattantData>();

    public void KeyPressHandler(KeyCode code) {
        if (state == CombatState.YOUR_TURN) {
            Combattant target = null;
            if (Game.Scene == Game.CombatScene.ORCS) {
                if (Input.GetKeyUp(KeyCode.Alpha1)) {
                    target = orc1;
                } else if (Input.GetKeyUp(KeyCode.Alpha2)) {
                    target = orc2;
                } else if (Input.GetKeyUp(KeyCode.Alpha3)) {
                    target = orc3;
                }
            } else if (Game.Scene == Game.CombatScene.OGRES) {
                if (Input.GetKeyUp(KeyCode.Alpha1)) {
                    target = orc1;
                } else if (Input.GetKeyUp(KeyCode.Alpha2)) {
                    target = orc2;
                } else if (Input.GetKeyUp(KeyCode.Alpha3)) {
                    target = orc3;
                }
            }
            if (target == null || target.HitPoints <= 0) return;
            ground.MeleeAttackAction(target);
            timer = 2.0f;
            waitForTimer = true;
            setAttackDefendSprites(ground.CurrentCombattant, target);
        } else if (state == CombatState.WON || state == CombatState.LOST) {
            if (Input.GetKeyUp(KeyCode.Alpha0)) {
                SceneManager.LoadScene("MainMenu");
            }
        }
    }

    // Start is called before the first frame update
    void Start() {
        MenuText.text = DEFAULT_TEXT;
        GlobalEvents.Handlers += delegate (object sender, EventArgs args) {
            if (GlobalEvents.EventTypes.INITIATIVE.Equals(sender)) {
                GlobalEvents.InitiativeRolled initiative = (GlobalEvents.InitiativeRolled)args;
                log(String.Format("{0} rolled for initiative: {1,2}", initiative.Roller.Name, initiative.Result));
            } else if (GlobalEvents.EventTypes.ATTACKED.Equals(sender)) {
                GlobalEvents.AttackRolled attacked = (GlobalEvents.AttackRolled)args;
                String crit = attacked.CriticalHit ? " CRITICAL HIT!" : "";
                log(String.Format("{0} attacked {1}: {2,2}{3}", attacked.Attacker.Name, attacked.Target.Name, attacked.Roll, crit));
            } else if (GlobalEvents.EventTypes.HEALED.Equals(sender)) {
                GlobalEvents.HealingReceived healed = (GlobalEvents.HealingReceived)args;
                log(String.Format("{0} was healed for {1} hitpoints", healed.Beneficiary.Name, healed.Amount));
            } else if (GlobalEvents.EventTypes.DAMAGED.Equals(sender)) {
                GlobalEvents.DamageReceived damaged = (GlobalEvents.DamageReceived)args;
                log(String.Format("{0} was hit for {1} points of {2} damage", damaged.Victim.Name, damaged.Amount, damaged.DamageType.Name()));
            } else {
                Debug.Log("Unexpected Event.");
            }
        };

        orc1.Name = "Orc1";
        orc2.Name = "Orc2";
        orc3.Name = "Orc3";
        ogre1.Name = "Ogre1";
        ogre2.Name = "Ogre2";

        int i = 0;
        int front = 0;
        int back = 0;
        foreach (CharacterSheet hero in Game.Characters) {
            if (i == Game.FrontLineChar1 || i == Game.FrontLineChar2) {
                ground.AddCombattant(hero, ClassicLocation.Row.FRONT);
                if (front++ == 0) {
                    heroFront1 = i;
                    assignSprites(hero, HeroFront1);
                } else {
                    heroFront2 = i;
                    assignSprites(hero, HeroFront2);
                }
            } else {
                ground.AddCombattant(hero, ClassicLocation.Row.BACK);
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

        if (Game.Scene == Game.CombatScene.ORCS) {
            Orc1.gameObject.SetActive(true);
            Orc2.gameObject.SetActive(true);
            Orc3.gameObject.SetActive(true);
            ground.AddCombattant(orc1, ClassicLocation.Row.FRONT);
            ground.AddCombattant(orc2, ClassicLocation.Row.FRONT);
            ground.AddCombattant(orc3, ClassicLocation.Row.FRONT);
            combattants.Add(orc1.Name, new CombattantData(orc1, Orc1, Orc, OrcAttack, OrcDefend));
            combattants.Add(orc2.Name, new CombattantData(orc2, Orc2, Orc, OrcAttack, OrcDefend));
            combattants.Add(orc3.Name, new CombattantData(orc3, Orc3, Orc, OrcAttack, OrcDefend));
        } else if (Game.Scene == Game.CombatScene.OGRES) {
            Ogre1.gameObject.SetActive(true);
            Ogre2.gameObject.SetActive(true);
            ground.AddCombattant(ogre1, ClassicLocation.Row.FRONT);
            ground.AddCombattant(ogre2, ClassicLocation.Row.FRONT);
            combattants.Add(ogre1.Name, new CombattantData(ogre1, Ogre1, Ogre, OgreAttack, OgreDefend));
            combattants.Add(ogre2.Name, new CombattantData(ogre2, Ogre2, Ogre, OgreAttack, OgreDefend));
        }

        ground.Initialize();
        timer = 2.0f;
        waitForTimer = true;
    }

    private void assignSprites(CharacterSheet hero, Image heroImage) {
        heroImage.gameObject.SetActive(true);
        if (hero.Race.Race == Race.HILL_DWARF) {
            if (hero.Levels[0].Class.Class == Class.BARBARIAN) {
                heroImage.sprite = DwarfBerserker;
                combattants.Add(hero.Name, new CombattantData(hero, heroImage, DwarfBerserker, DwarfBerserkerAttack, DwarfBeserkerDefend));
            } else if (hero.Levels[0].Class.Class == Class.DRUID) {
                heroImage.sprite = DwarfDruid;
                combattants.Add(hero.Name, new CombattantData(hero, heroImage, DwarfDruid, DwarfDruidAttack, DwarfDruidDefend));
            }
        } else if (hero.Race.Race == Race.HIGH_ELF) {
            if (hero.Levels[0].Class.Class == Class.BARBARIAN) {
                heroImage.sprite = ElfBerserker;
                combattants.Add(hero.Name, new CombattantData(hero, heroImage, ElfBerserker, ElfBerserkerAttack, ElfBersekerDefend));
            } else if (hero.Levels[0].Class.Class == Class.DRUID) {
                heroImage.sprite = ElfDruid;
                combattants.Add(hero.Name, new CombattantData(hero, heroImage, ElfDruid, ElfDruidAttack, ElfDruidDefend));
            }
        }
    }

    // Update is called once per frame
    void Update() {
        if (waitForTimer) {
            timer -= Time.deltaTime;
            if (timer > 0) return;
            waitForTimer = false;
            nextPhase();
        }
    }

    private void nextPhase() {
        clearAttackDefendSprites();
        for (TurnPhase phase = ground.NextPhase(); phase != TurnPhase.ACTION; phase = ground.NextPhase()) ;
        if (ground.CurrentCombattant is CharacterSheet) {
            if (ground.CurrentCombattant.HitPoints <= 0) {
                nextPhase();
            } else {
                log(String.Format("It is {0}'s turn", ground.CurrentCombattant.Name));
                state = CombatState.YOUR_TURN;
                if (Game.Scene == Game.CombatScene.ORCS)
                    MenuText.text = ATTACK_ORCS;
                else if (Game.Scene == Game.CombatScene.OGRES)
                    MenuText.text = ATTACK_OGRES;
            }
            return;
        };
        // select target
        CharacterSheet target = null;
        bool useRanged = false;
        if (heroFront1 > -1 && Game.Characters[heroFront1].HitPoints > 0) {
            target = Game.Characters[heroFront1];
        } else if (heroFront2 > -1 && Game.Characters[heroFront2].HitPoints > 0) {
            target = Game.Characters[heroFront2];
        } else if (heroBack1 > -1 && Game.Characters[heroBack1].HitPoints > 0) {
            target = Game.Characters[heroBack1];
            useRanged = true;
        } else if (heroBack2 > -1 && Game.Characters[heroBack2].HitPoints > 0) {
            target = Game.Characters[heroBack2];
            useRanged = true;
        }
        if (target == null) {
            state = CombatState.LOST;
            MenuText.text = LOST;
            return;
        }
        if (useRanged) {
            // TODO: Implement Ranged Combat
        } else {
            ground.MeleeAttackAction(target);
            timer = 2.0f;
            waitForTimer = true;
            setAttackDefendSprites(ground.CurrentCombattant, target);
        }
    }

    private void clearAttackDefendSprites() {
        foreach (CombattantData data in combattants.Values) {
            if (data.Combattant.HitPoints <= 0 && data.Image.gameObject.activeSelf) {
                log(String.Format("{0} has died, R.I.P.", data.Combattant.Name));
                data.Image.gameObject.SetActive(false);
                checkIfWonOrLost();
            }
            data.Image.sprite = data.Sprites[0];
        }
    }

    private void checkIfWonOrLost() {
        bool heroAlive = false;
        bool monsterAlive = false;
        foreach (CombattantData data in combattants.Values) {
            if (data.Combattant is Monster && data.Combattant.HitPoints > 0)
                monsterAlive = true;
            if (data.Combattant is CharacterSheet && data.Combattant.HitPoints > 0)
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

    private void setAttackDefendSprites(Combattant attacker, Combattant target) {
        combattants[attacker.Name].Image.sprite = combattants[attacker.Name].Sprites[1];
        combattants[target.Name].Image.sprite = combattants[target.Name].Sprites[2];
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
}
