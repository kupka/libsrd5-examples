using System.Collections.Generic;
using System.Text;
using srd5;

/// <summary>
/// Race and class descriptions from libsrd5, cut to the size of the text areas.
/// Engine independent, shared by the Unity, Godot and Blazor examples.
/// </summary>
public static class Texts {
    /// <summary>Descriptions are cut after this many lines (the text areas fit about 57 characters per line).</summary>
    public const int DESCRIPTION_LINES = 15;
    public const int LINE_WIDTH = 56;

    /// <summary>Race description followed by the descriptions of the racial traits.</summary>
    public static string RaceDescription(CharacterSheet hero) {
        StringBuilder text = new StringBuilder(hero.Race.Race.Description());
        foreach (Feat feat in hero.Feats) {
            if (isClassFeat(hero, feat)) continue;
            text.Append("\n").Append(feat.Description());
        }
        return Truncate(text.ToString());
    }

    /// <summary>Descriptions of the class features (libsrd5 has no class descriptions yet).</summary>
    public static string ClassDescription(CharacterSheet hero) {
        StringBuilder text = new StringBuilder();
        foreach (Feat feat in hero.Feats) {
            if (!isClassFeat(hero, feat)) continue;
            if (text.Length > 0) text.Append("\n");
            text.Append(feat.Description());
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
}
