// Tests de la logique (exécutés sous Mono, sans interface).
using System;
using System.IO;
using System.Linq;
using System.Text;
using LpAccesClaude;

static class Tests
{
    static int fails = 0;
    static void Check(bool ok, string what) { Console.WriteLine((ok ? "  OK   " : "  ÉCHEC ") + what); if (!ok) fails++; }

    const string Sample = @"{
  ""mcpServers"": { ""x"": { ""command"": ""node"", ""args"": [""a.js""], ""env"": {} } },
  ""preferences"": { ""menuBarEnabled"": false, ""label"": ""Météo <test> & 'ok' \u00e9 \""q\"" \\ \t"", ""zoom"": 1.25, ""big"": 12345678901234567890, ""n"": null, ""neg"": -3e-5 },
  ""remoteFolderConsentMemory"": [ ""C:\\HAOS\\lp-meteo"", ""C:\\HAOS\\_claude"" ],
  ""remoteChatFolderGrants"": {
    ""cbe60f45-1adf-4174-9459-11356fb335f4"": { ""orgUuid"": ""o1"", ""accountUuid"": ""a1"", ""folders"": [ ""C:\\HAOS\\lp-meteo"" ] },
    ""11111111-2222-3333-4444-555555555555"": { ""orgUuid"": ""o1"", ""accountUuid"": ""a1"", ""folders"": [ ""C:\\HAOS\\_claude"", ""C:\\HAOS\\lp-meteo"" ] }
  },
  ""emptyArr"": [], ""one"": [""seul""], ""emoji"": ""\ud83d\ude00 ☀""
}";

    static int Main()
    {
        string dir = Path.Combine(Path.GetTempPath(), "lp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string file = Path.Combine(dir, "claude_desktop_config.json");
        File.WriteAllText(file, Sample, new UTF8Encoding(false));

        Console.WriteLine("JSON :");
        var parsed = Json.Parse(Sample);
        string out1 = Json.Serialize(parsed);
        string out2 = Json.Serialize(Json.Parse(out1));
        Check(out1 == out2, "aller-retour stable");
        Check(out1.Contains("12345678901234567890") && out1.Contains("-3e-5") && out1.Contains("1.25"), "nombres conservés tels quels");
        Check(out1.Contains("Météo <test> & 'ok' é \\\"q\\\" \\\\ \\t"), "texte, accents et échappements conservés");
        Check(out1.Contains("😀 ☀"), "emoji (paire de substitution) conservé");
        Check(out1.IndexOf("mcpServers") < out1.IndexOf("preferences") && out1.IndexOf("preferences") < out1.IndexOf("remoteFolderConsentMemory"), "ordre des clés conservé");
        Check(out1.Contains("\"emptyArr\": []") && out1.Contains("\"env\": {}") && out1.Contains("\"n\": null"), "listes/objets vides et null conservés");
        foreach (var bad in new[] { "{", "{\"a\":}", "[1,]x", "{\"a\" 1}", "\"\\x\"" })
        {
            bool threw = false;
            try { Json.Parse(bad); } catch (FormatException) { threw = true; }
            Check(threw, "rejette un JSON invalide : " + bad);
        }

        Console.WriteLine("Configuration :");
        var store = new ConfigStore(file);
        store.Load();
        var grants = store.Grants();
        Check(grants.Count == 3, "3 accès lus");
        Check(store.Consents().Count == 2, "2 accords mémorisés lus");

        // Retirer le dossier lp-meteo du premier chat : le chat disparaît
        store.Remove(new[] { grants[0] }, null);
        string backup = store.Backup();
        store.Save();
        var re = new ConfigStore(file); re.Load();
        Check(re.Grants().Count == 2 && !re.Grants().Any(g => g.Chat.StartsWith("cbe60f45")), "chat sans dossier retiré");
        Check(re.Consents().Count == 2, "accords inchangés");
        Check(File.Exists(backup) && File.ReadAllText(backup) == Sample, "sauvegarde identique à l'original");
        string saved = File.ReadAllText(file);
        byte[] raw = File.ReadAllBytes(file);
        Check(!(raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF), "écrit sans BOM");
        Check(saved.Contains("\"mcpServers\"") && saved.Contains("Météo") && saved.Contains("12345678901234567890"), "reste de la configuration intact");
        Check(!File.Exists(file + ".tmp-lp"), "pas de fichier temporaire laissé");

        // Retirer un dossier parmi deux : le chat reste avec l'autre
        var g2 = re.Grants().First(g => g.Folder.EndsWith("lp-meteo"));
        re.Remove(new[] { g2 }, new[] { "C:\\HAOS\\lp-meteo" });
        re.Save();
        var re2 = new ConfigStore(file); re2.Load();
        Check(re2.Grants().Count == 1 && re2.Grants()[0].Folder == "C:\\HAOS\\_claude", "un seul dossier retiré, l'autre reste");
        Check(re2.Consents().SequenceEqual(new[] { "C:\\HAOS\\_claude" }), "accord oublié");

        // Tout retirer
        re2.Remove(re2.Grants(), re2.Consents());
        re2.Save();
        var re3 = new ConfigStore(file); re3.Load();
        Check(re3.Grants().Count == 0 && re3.Consents().Count == 0, "tout retiré");
        string s3 = File.ReadAllText(file);
        Check(s3.Contains("\"remoteChatFolderGrants\": {}") && s3.Contains("\"remoteFolderConsentMemory\": []"), "clés gardées, vides");

        // Restauration
        Check(re3.Backups().Count >= 1, "sauvegardes listées");
        re3.Restore(backup);
        var re4 = new ConfigStore(file); re4.Load();
        Check(re4.Grants().Count == 3, "restauration de la sauvegarde");

        // Fichier sans les clés
        File.WriteAllText(file, "{\"a\":1}");
        var re5 = new ConfigStore(file); re5.Load();
        Check(re5.Grants().Count == 0 && re5.Consents().Count == 0, "configuration sans accès : listes vides");
        re5.Remove(new Grant[0], new string[0]); re5.Save();
        Check(File.ReadAllText(file).Contains("\"a\": 1"), "configuration sans accès : inchangée");

        Console.WriteLine("Clés rangées dans « preferences » (format réel de Claude Desktop) :");
        File.WriteAllText(file, @"{
  ""mcpServers"": { ""ha"": { ""command"": ""x"", ""env"": { ""TOKEN"": ""secret"" } } },
  ""preferences"": {
    ""launchPreviewAllowedOrigins"": [ ""https://amazon.fr"" ],
    ""remoteFolderConsentMemory"": [],
    ""remoteChatFolderGrants"": {
      ""cbe60f45-1adf-4174-9459-11356fb335f4"": { ""orgUuid"": ""o"", ""accountUuid"": ""a"", ""folders"": [ ""C:\\HAOS\\_test"" ] }
    },
    ""sidebarMode"": ""chat""
  }
}", new UTF8Encoding(false));
        var p1 = new ConfigStore(file); p1.Load();
        Check(p1.Grants().Count == 1 && p1.Grants()[0].Folder == "C:\\HAOS\\_test", "accès trouvé dans preferences");
        Check(p1.Where(ConfigStore.GrantsKey) == "preferences › remoteChatFolderGrants", "emplacement indiqué : " + p1.Where(ConfigStore.GrantsKey));
        p1.Remove(p1.Grants(), null); p1.Save();
        var p2 = new ConfigStore(file); p2.Load();
        string sp = File.ReadAllText(file);
        Check(p2.Grants().Count == 0 && sp.Contains("\"remoteChatFolderGrants\": {}"), "accès retiré dans preferences");
        Check(sp.Contains("\"TOKEN\": \"secret\"") && sp.Contains("\"sidebarMode\": \"chat\"") && !sp.Contains("\"remoteChatFolderGrants\": {},\n  \"mcpServers\""), "reste du fichier intact, rien ajouté à la racine");
        var root = (JObject)Json.Parse(sp);
        Check(!root.Has(ConfigStore.GrantsKey), "aucune clé créée à la racine");

        Console.WriteLine("Chemins Claude Desktop :");
        Check(ClaudeDesktop.IsDesktopPath(@"C:\Users\x\AppData\Local\AnthropicClaude\app-1.2.3\claude.exe"), "installation classique");
        Check(ClaudeDesktop.IsDesktopPath(@"C:\Program Files\WindowsApps\Claude_1.0.0_x64__abc\app\Claude.exe"), "Microsoft Store");
        Check(ClaudeDesktop.IsDesktopPath(@"C:\Users\x\AppData\Local\Programs\Claude\Claude.exe"), "Programs\\Claude");
        Check(!ClaudeDesktop.IsDesktopPath(@"C:\Users\x\.local\bin\claude.exe"), "Claude Code (.local) ignoré");
        Check(!ClaudeDesktop.IsDesktopPath(@"C:\Users\x\AppData\Local\Microsoft\WinGet\Packages\Anthropic.ClaudeCode\claude.exe"), "Claude Code (WinGet) ignoré");
        Check(!ClaudeDesktop.IsDesktopPath(@"C:\Users\x\AppData\Roaming\npm\node_modules\@anthropic-ai\claude-code\claude.exe"), "Claude Code (npm) ignoré");
        Check(!ClaudeDesktop.IsDesktopPath(null), "chemin inconnu ignoré");

        Console.WriteLine("Textes :");
        Lang.French = true; string fr = Lang.T("btn_apply");
        Lang.French = false; string en = Lang.T("btn_apply");
        Check(fr == "Retirer la sélection" && en == "Remove selection", "français et anglais");

        Directory.Delete(dir, true);
        Console.WriteLine(fails == 0 ? "TOUS LES TESTS PASSENT" : fails + " ÉCHEC(S)");
        return fails == 0 ? 0 : 1;
    }
}
