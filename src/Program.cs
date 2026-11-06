// Point d'entrée.
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

[assembly: AssemblyTitle("LP Accès Claude")]
[assembly: AssemblyDescription("Liste et retire les dossiers de ce PC partagés avec des chats Claude Desktop (outil non officiel).")]
[assembly: AssemblyCompany("LePousson")]
[assembly: AssemblyProduct("LP Accès Claude")]
[assembly: AssemblyCopyright("LePousson 2026 · non affilié à Anthropic")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: ComVisible(false)]

namespace LpAccesClaude
{
    public static class Program
    {
        public const string Version = "1.0.0";

        [DllImport("user32.dll")]
        static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main(string[] args)
        {
            // --lang=fr ou --lang=en pour forcer la langue (sinon : langue de Windows)
            foreach (var a in args)
            {
                if (a.Equals("--lang=fr", StringComparison.OrdinalIgnoreCase)) Lang.French = true;
                if (a.Equals("--lang=en", StringComparison.OrdinalIgnoreCase)) Lang.French = false;
            }
            try { if (Environment.OSVersion.Version.Major >= 6) SetProcessDPIAware(); } catch (Exception) { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (s, e) => MessageBox.Show(e.Exception.Message, Lang.T("error"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.Run(new MainForm());
        }
    }
}
