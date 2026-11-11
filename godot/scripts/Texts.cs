using System.Collections.Generic;
using System.Text;
using srd5;

/// <summary>
/// Display texts for races and classes. Uses the libsrd5 texts and fills in the ones libsrd5
/// does not have yet (libsrd5 0.3.4 has no Human or Wizard names, no Human description and
/// no class descriptions). Engine independent, shared by the Unity, Godot and Blazor examples.
/// </summary>
public static class Texts {
    /// <summary>Descriptions are cut after this many lines (the text areas fit about 57 characters per line).</summary>
    public const int DESCRIPTION_LINES = 15;
    public const int LINE_WIDTH = 56;

    public static string RaceName(Race race) {
        string name = race.Name();
        if (!isMissing(name)) return name;
        switch (race) {
            case Race.HUMAN: return "Human";
            default: return pretty(race.ToString());
        }
    }

    public static string ClassName(Class clazz) {
        string name = clazz.Name();
        if (!isMissing(name)) return name;
        return pretty(clazz.ToString());
    }

    /// <summary>Race description followed by the descriptions of the racial traits.</summary>
    public static string RaceDescription(CharacterSheet hero) {
        StringBuilder text = new StringBuilder(raceText(hero.Race.Race));
        foreach (Feat feat in hero.Feats) {
            if (isClassFeat(hero, feat)) continue;
            text.Append("\n").Append(feat.Description());
        }
        return Truncate(text.ToString());
    }

    /// <summary>Class description followed by the descriptions of the class features.</summary>
    public static string ClassDescription(CharacterSheet hero) {
        if (hero.Levels.Length == 0) return "";
        CharacterClass characterClass = hero.Levels[0].Class;
        StringBuilder text = new StringBuilder(classText(characterClass.Class));
        foreach (Feat feat in hero.Feats) {
            if (!isClassFeat(hero, feat)) continue;
            text.Append("\n").Append(feat.Description());
        }
        return Truncate(text.ToString());
    }

    /// <summary>Word-wraps the text like the text areas and cuts it after DESCRIPTION_LINES lines.</summary>
    public static string Truncate(string text) {
        if (text == null) return "";
        List<string> lines = new List<string>();
        foreach (string paragraph in text.Replace("\r", "").Split('\n')) {
            string line = "";
            foreach (string word in paragraph.Split(' ')) {
                if (line.Length > 0 && line.Length + 1 + word.Length > LINE_WIDTH) {
                    lines.Add(line);
                    line = word;
                } else {
                    line = line.Length == 0 ? word : line + " " + word;
                }
            }
            lines.Add(line);
        }
        if (lines.Count <= DESCRIPTION_LINES) return text;
        lines = lines.GetRange(0, DESCRIPTION_LINES);
        string last = lines[DESCRIPTION_LINES - 1];
        if (last.Length > LINE_WIDTH - 3) last = last.Substring(0, LINE_WIDTH - 3);
        lines[DESCRIPTION_LINES - 1] = last.TrimEnd() + "...";
        return string.Join("\n", lines.ToArray());
    }

    private static string raceText(Race race) {
        string description = race.Description();
        if (!isMissing(description)) return description;
        switch (race) {
            case Race.HUMAN:
                return "Humans are the youngest and most widespread of the common races: short-lived, adaptable and ambitious.\n" +
                    "As a human, each of your ability scores increases by 1. You are Medium sized, have a walking speed of 30 feet and speak Common and one extra language.";
            default:
                return RaceName(race);
        }
    }

    private static string classText(Class clazz) {
        switch (clazz) {
            case Class.BARBARIAN:
                return "A fierce warrior who draws on primal fury and fights with a rage in battle.\n" +
                    "Hit die: d12. Saving throws: Strength, Constitution. Armor: light, medium, shields. Weapons: simple and martial.";
            case Class.DRUID:
                return "A keeper of the old ways who calls on the powers of nature: healing, entangling plants and fire.\n" +
                    "Hit die: d8. Saving throws: Intelligence, Wisdom. Spellcasting ability: Wisdom. Armor: light, medium, shields (no metal). Weapons: club, dagger, dart, javelin, mace, quarterstaff, scimitar, sickle, sling, spear.";
            case Class.WIZARD:
                return "A scholar of the arcane who studies spells from a spellbook and bends reality with them.\n" +
                    "Hit die: d6. Saving throws: Intelligence, Wisdom. Spellcasting ability: Intelligence, prepares spells every day. Armor: none. Weapons: dagger, dart, sling, quarterstaff, light crossbow.";
            default:
                return ClassName(clazz);
        }
    }

    /// <summary>True if the feat comes from the hero's (first) class rather than the race.</summary>
    private static bool isClassFeat(CharacterSheet hero, Feat feat) {
        if (hero.Levels.Length == 0) return false;
        CharacterLevel level = hero.Levels[0];
        Feat[][] classFeats = level.Class.Feats;
        if (classFeats == null) return false;
        for (int i = 0; i <= level.Levels && i < classFeats.Length; i++) {
            if (classFeats[i] == null) continue;
            foreach (Feat classFeat in classFeats[i])
                if (classFeat == feat) return true;
        }
        return false;
    }

    private static bool isMissing(string text) {
        return text == null || text.EndsWith("(Name missing)") || text.EndsWith("(Description missing)");
    }

    /// <summary>HALF_ORC -> Half orc</summary>
    private static string pretty(string value) {
        string text = value.Replace('_', ' ').ToLowerInvariant();
        return text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text.Substring(1);
    }
}
