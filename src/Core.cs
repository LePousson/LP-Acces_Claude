// Logique sans interface : fichier de configuration de Claude Desktop et processus de l'application.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace LpAccesClaude
{
    /// <summary>Un dossier accordé à un chat.</summary>
    public sealed class Grant
    {
        public readonly string Chat;
        public readonly string Folder;
        public Grant(string chat, string folder) { Chat = chat; Folder = folder; }
        public string ChatUrl { get { return "https://claude.ai/chat/" + Chat; } }
        public bool SameAs(Grant g) { return g != null && g.Chat == Chat && string.Equals(g.Folder, Folder, StringComparison.Ordinal); }
    }

    /// <summary>Lecture et modification de claude_desktop_config.json.</summary>
    public sealed class ConfigStore
    {
        public const string GrantsKey = "remoteChatFolderGrants";
        public const string ConsentKey = "remoteFolderConsentMemory";
        public const string BackupMarker = ".bak-";

        public readonly string Path;
        public JObject Root { get; private set; }

        public ConfigStore(string path) { Path = path; }

        /// <summary>Emplacements possibles : installation classique, puis Microsoft Store (dossier virtualisé).</summary>
        public static List<string> CandidatePaths()
        {
            var list = new List<string>();
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (!string.IsNullOrEmpty(appData)) list.Add(System.IO.Path.Combine(appData, "Claude", "claude_desktop_config.json"));
            string pkgs = string.IsNullOrEmpty(local) ? null : System.IO.Path.Combine(local, "Packages");
            if (pkgs != null && Directory.Exists(pkgs))
            {
                try
                {
                    foreach (var d in Directory.GetDirectories(pkgs, "Claude*"))
                        list.Add(System.IO.Path.Combine(d, "LocalCache", "Roaming", "Claude", "claude_desktop_config.json"));
                }
                catch (Exception) { }
            }
            return list;
        }

        /// <summary>Le fichier existant le plus récemment modifié, ou null.</summary>
        public static string FindConfig()
        {
            return CandidatePaths().Where(File.Exists).OrderByDescending(p => File.GetLastWriteTimeUtc(p)).FirstOrDefault();
        }

        public void Load()
        {
            string text = File.ReadAllText(Path, Encoding.UTF8);
            var root = Json.Parse(text) as JObject;
            if (root == null) throw new FormatException("Le fichier ne contient pas un objet JSON.");
            Root = root;
        }

        /// <summary>
        /// Objet qui contient la clé demandée : à la racine ou plus bas (Claude Desktop range ses réglages
        /// dans « preferences »). Recherche en largeur : le plus haut niveau gagne.
        /// </summary>
        public JObject FindOwner(string key)
        {
            if (Root == null) return null;
            var queue = new Queue<JObject>();
            queue.Enqueue(Root);
            while (queue.Count > 0)
            {
                var o = queue.Dequeue();
                if (o.Has(key)) return o;
                foreach (var kv in o.Items)
                {
                    var child = kv.Value as JObject;
                    if (child != null) queue.Enqueue(child);
                }
            }
            return null;
        }

        /// <summary>Chemin lisible de la clé dans le fichier (ex. « preferences › remoteChatFolderGrants »).</summary>
        public string Where(string key)
        {
            var path = new List<string>();
            return FindPath(Root, key, path) ? string.Join(" › ", path) : null;
        }

        private static bool FindPath(JObject o, string key, List<string> path)
        {
            if (o == null) return false;
            if (o.Has(key)) { path.Add(key); return true; }
            foreach (var kv in o.Items)
            {
                var child = kv.Value as JObject;
                if (child == null) continue;
                path.Add(kv.Key);
                if (FindPath(child, key, path)) return true;
                path.RemoveAt(path.Count - 1);
            }
            return false;
        }

        private JObject GrantsObject() { var owner = FindOwner(GrantsKey); return owner == null ? null : owner.Get(GrantsKey) as JObject; }

        private JArray ConsentArray() { var owner = FindOwner(ConsentKey); return owner == null ? null : owner.Get(ConsentKey) as JArray; }

        public List<Grant> Grants()
        {
            var list = new List<Grant>();
            var grants = GrantsObject();
            if (grants == null) return list;
            foreach (var chat in grants.Items)
            {
                var entry = chat.Value as JObject;
                var folders = entry == null ? null : entry.Get("folders") as JArray;
                if (folders == null) continue;
                foreach (var f in folders.Items)
                {
                    var s = f as string;
                    if (!string.IsNullOrEmpty(s)) list.Add(new Grant(chat.Key, s));
                }
            }
            return list;
        }

        public List<string> Consents()
        {
            var list = new List<string>();
            var mem = ConsentArray();
            if (mem == null) return list;
            foreach (var f in mem.Items)
            {
                var s = f as string;
                if (!string.IsNullOrEmpty(s)) list.Add(s);
            }
            return list;
        }

        /// <summary>Retire des accès (chat, dossier) et oublie des accords mémorisés. Un chat sans dossier est retiré.</summary>
        public void Remove(IEnumerable<Grant> remove, IEnumerable<string> forget)
        {
            var grants = GrantsObject();
            if (grants != null && remove != null)
            {
                foreach (var g in remove)
                {
                    var entry = grants.Get(g.Chat) as JObject;
                    if (entry == null) continue;
                    var folders = entry.Get("folders") as JArray;
                    if (folders != null) folders.Items.RemoveAll(f => (f as string) == g.Folder);
                    if (folders == null || folders.Items.Count == 0) grants.Remove(g.Chat);
                }
            }
            var mem = ConsentArray();
            if (mem != null && forget != null)
            {
                var set = new HashSet<string>(forget, StringComparer.Ordinal);
                mem.Items.RemoveAll(f => f is string && set.Contains((string)f));
            }
        }

        public string Backup()
        {
            string dest = Path + BackupMarker + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            if (File.Exists(dest)) dest += "-" + Guid.NewGuid().ToString("N").Substring(0, 4);
            File.Copy(Path, dest);
            return dest;
        }

        /// <summary>Écrit en UTF-8 sans BOM (comme l'application), via un fichier temporaire.</summary>
        public void Save()
        {
            string json = Json.Serialize(Root);
            string tmp = Path + ".tmp-lp";
            File.WriteAllText(tmp, json, new UTF8Encoding(false));
            Json.Parse(File.ReadAllText(tmp, Encoding.UTF8)); // contrôle avant de remplacer
            File.Copy(tmp, Path, true);
            File.Delete(tmp);
        }

        public List<string> Backups()
        {
            string dir = System.IO.Path.GetDirectoryName(Path);
            string name = System.IO.Path.GetFileName(Path) + BackupMarker + "*";
            if (dir == null || !Directory.Exists(dir)) return new List<string>();
            return Directory.GetFiles(dir, name).OrderByDescending(f => f, StringComparer.Ordinal).ToList();
        }

        public void Restore(string backup)
        {
            Json.Parse(File.ReadAllText(backup, Encoding.UTF8)); // la sauvegarde doit être un JSON valide
            File.Copy(backup, Path, true);
        }
    }

    /// <summary>Repérage, fermeture et lancement de l'application Claude Desktop (pas de Claude Code en ligne de commande).</summary>
    public static class ClaudeDesktop
    {
        private static readonly Regex CliPath = new Regex(@"\\(\.local|npm|node_modules|\.claude|\.bun|WinGet)\\", RegexOptions.IgnoreCase);
        private static readonly Regex DesktopPath = new Regex(@"\\AnthropicClaude\\|\\Programs\\Claude\\|\\WindowsApps\\[^\\]*Claude", RegexOptions.IgnoreCase);

        public static bool IsDesktopPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            if (CliPath.IsMatch(path)) return false;
            return DesktopPath.IsMatch(path);
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint access, bool inherit, int pid);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool QueryFullProcessImageName(IntPtr h, int flags, StringBuilder name, ref int size);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr h);

        /// <summary>Chemin de l'exécutable, ou null si illisible.</summary>
        public static string PathOf(Process p)
        {
            try
            {
                IntPtr h = OpenProcess(0x1000 /* QUERY_LIMITED_INFORMATION */, false, p.Id);
                if (h == IntPtr.Zero) return null;
                try
                {
                    var sb = new StringBuilder(1024);
                    int size = sb.Capacity;
                    return QueryFullProcessImageName(h, 0, sb, ref size) ? sb.ToString() : null;
                }
                finally { CloseHandle(h); }
            }
            catch (Exception) { return null; }
        }

        public sealed class Found
        {
            public readonly List<Process> Desktop = new List<Process>();
            public readonly List<Process> Unknown = new List<Process>(); // chemin illisible
            public readonly List<string> Paths = new List<string>();
        }

        public static Found Scan()
        {
            var f = new Found();
            Process[] all;
            try { all = Process.GetProcessesByName("claude"); } catch (Exception) { all = new Process[0]; }
            foreach (var p in all)
            {
                string path = PathOf(p);
                if (path == null) { f.Unknown.Add(p); continue; }
                if (IsDesktopPath(path))
                {
                    f.Desktop.Add(p);
                    if (!f.Paths.Contains(path, StringComparer.OrdinalIgnoreCase)) f.Paths.Add(path);
                }
            }
            return f;
        }

        public static bool IsRunning() { return Scan().Desktop.Count > 0; }

        /// <summary>Fermeture propre, puis forcée si l'application reste dans la barre des tâches.</summary>
        public static bool Close(Action<string> log)
        {
            var found = Scan();
            if (found.Desktop.Count == 0) return true;
            foreach (var p in found.Desktop) { try { p.CloseMainWindow(); } catch (Exception) { } }
            if (WaitGone(5000)) return true;
            if (log != null) log(Lang.T("log_force"));
            foreach (var p in Scan().Desktop) { try { p.Kill(); } catch (Exception) { } }
            return WaitGone(5000);
        }

        private static bool WaitGone(int ms)
        {
            int waited = 0;
            while (waited < ms)
            {
                if (!IsRunning()) return true;
                Thread.Sleep(250);
                waited += 250;
            }
            return !IsRunning();
        }

        /// <summary>Comment relancer l'application : à déterminer AVANT de la fermer.</summary>
        public sealed class Launcher
        {
            public string Exe;     // exécutable à lancer
            public string AppId;   // Microsoft Store : identifiant d'application (shell:AppsFolder)
            public string Shortcut;
            public bool CanLaunch { get { return Exe != null || AppId != null || Shortcut != null; } }
        }

        public static Launcher FindLauncher()
        {
            var l = new Launcher();
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var found = Scan();
            string running = found.Paths.OrderBy(p => p.Length).FirstOrDefault();
            bool store = running != null && running.IndexOf("\\WindowsApps\\", StringComparison.OrdinalIgnoreCase) >= 0;
            if (!store)
            {
                // Installation classique : le lanceur racine choisit la bonne version après une mise à jour
                string stub = System.IO.Path.Combine(local, "AnthropicClaude", "claude.exe");
                if (File.Exists(stub)) { l.Exe = stub; return l; }
                if (running != null) { l.Exe = running; return l; }
                string prog = System.IO.Path.Combine(local, "Programs", "Claude", "Claude.exe");
                if (File.Exists(prog)) { l.Exe = prog; return l; }
            }
            l.AppId = StoreAppId();
            if (l.AppId != null) return l;
            l.Shortcut = StartMenuShortcut();
            return l;
        }

        private static string StoreAppId()
        {
            try
            {
                var psi = new ProcessStartInfo("powershell.exe",
                    "-NoProfile -NonInteractive -Command \"(Get-StartApps | Where-Object { $_.Name -eq 'Claude' } | Select-Object -First 1).AppID\"");
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                using (var p = Process.Start(psi))
                {
                    string id = p.StandardOutput.ReadToEnd().Trim();
                    p.WaitForExit(15000);
                    return string.IsNullOrEmpty(id) ? null : id;
                }
            }
            catch (Exception) { return null; }
        }

        private static string StartMenuShortcut()
        {
            foreach (var root in new[] {
                Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu) })
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                try
                {
                    var hit = Directory.GetFiles(root, "Claude.lnk", SearchOption.AllDirectories).FirstOrDefault();
                    if (hit != null) return hit;
                }
                catch (Exception) { }
            }
            return null;
        }

        public static void Launch(Launcher l)
        {
            if (l.Exe != null) { Process.Start(new ProcessStartInfo(l.Exe) { UseShellExecute = true }); return; }
            if (l.AppId != null) { Process.Start("explorer.exe", "shell:AppsFolder\\" + l.AppId); return; }
            if (l.Shortcut != null) { Process.Start(new ProcessStartInfo(l.Shortcut) { UseShellExecute = true }); return; }
            throw new Win32Exception(2, Lang.T("err_no_launch"));
        }
    }
}
