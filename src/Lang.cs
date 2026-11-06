// Textes de l'interface en français et en anglais (selon la langue de Windows).
using System.Collections.Generic;
using System.Globalization;

namespace LpAccesClaude
{
    public static class Lang
    {
        public static bool French = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr";

        private static readonly Dictionary<string, string[]> S = new Dictionary<string, string[]>
        {
            // clé                { français, anglais }
            { "title",            new[] { "Accès des chats Claude à tes dossiers", "Claude chats' access to your folders" } },
            { "subtitle",         new[] { "Dossiers de ce PC partagés avec des chats de l'application Claude Desktop", "Folders on this PC shared with chats in the Claude Desktop app" } },
            { "warning",          new[] { "Outil non officiel : il modifie un fichier interne de Claude Desktop (claude_desktop_config.json). Une sauvegarde est faite avant chaque modification. Une mise à jour de Claude peut le rendre inopérant.",
                                          "Unofficial tool: it edits an internal Claude Desktop file (claude_desktop_config.json). A backup is made before every change. A Claude update may stop it from working." } },
            { "grants",           new[] { "Dossiers accordés à des chats", "Folders granted to chats" } },
            { "consents",         new[] { "Accords mémorisés (Claude ne redemande pas ton accord pour ces dossiers)", "Remembered consents (Claude won't ask again for these folders)" } },
            { "col_folder",       new[] { "Dossier", "Folder" } },
            { "col_chat",         new[] { "Chat", "Chat" } },
            { "col_open",         new[] { "Ouvrir", "Open" } },
            { "open_chat",        new[] { "Ouvrir le chat", "Open chat" } },
            { "none_grants",      new[] { "Aucun dossier n'est accordé à un chat.", "No folder is granted to a chat." } },
            { "none_consents",    new[] { "Aucun accord mémorisé.", "No remembered consent." } },
            { "btn_refresh",      new[] { "Actualiser", "Refresh" } },
            { "btn_all",          new[] { "Tout cocher", "Select all" } },
            { "btn_none",         new[] { "Tout décocher", "Clear selection" } },
            { "btn_apply",        new[] { "Retirer la sélection", "Remove selection" } },
            { "btn_restore",      new[] { "Restaurer une sauvegarde…", "Restore a backup…" } },
            { "btn_folder",       new[] { "Ouvrir le dossier", "Open folder" } },
            { "status_running",   new[] { "Claude Desktop est ouvert", "Claude Desktop is running" } },
            { "status_closed",    new[] { "Claude Desktop est fermé", "Claude Desktop is closed" } },
            { "config",           new[] { "Configuration : {0}", "Configuration: {0}" } },
            { "no_config",        new[] { "Fichier claude_desktop_config.json introuvable. Claude Desktop est-il installé sur ce PC ?", "claude_desktop_config.json not found. Is Claude Desktop installed on this PC?" } },
            { "bad_json",         new[] { "Le fichier de configuration n'est pas un JSON valide :\n{0}", "The configuration file is not valid JSON:\n{0}" } },
            { "nothing",          new[] { "Coche au moins un dossier ou un accord à retirer.", "Tick at least one folder or consent to remove." } },
            { "recap",            new[] { "Voici ce qui va être retiré :\n\n{0}\nContinuer ?", "This will be removed:\n\n{0}\nContinue?" } },
            { "recap_grant",      new[] { "• {0}  (chat {1})", "• {0}  (chat {1})" } },
            { "recap_consent",    new[] { "• Accord mémorisé : {0}", "• Remembered consent: {0}" } },
            { "ask_forget",       new[] { "Ces dossiers ne seront plus accordés à aucun chat, mais Claude a mémorisé ton accord :\n\n{0}\n\nOublier aussi cet accord, pour que Claude te redemande la prochaine fois ?",
                                          "These folders will no longer be granted to any chat, but Claude remembers your consent:\n\n{0}\n\nAlso forget it, so Claude asks you again next time?" } },
            { "ask_close",        new[] { "Claude Desktop doit être fermé : il réécrit ce fichier tant qu'il tourne.\nLes chats en cours s'arrêtent (leur historique est conservé).\n\nFermer Claude Desktop maintenant ?",
                                          "Claude Desktop must be closed: it rewrites this file while running.\nRunning chats will stop (their history is kept).\n\nClose Claude Desktop now?" } },
            { "ask_unknown",      new[] { "Un processus « claude » tourne encore, mais son emplacement n'est pas lisible.\nVérifie que Claude Desktop est bien quitté (pas d'icône près de l'horloge).\n\nClaude Desktop est-il bien fermé ?",
                                          "A \"claude\" process is still running but its location can't be read.\nMake sure Claude Desktop is fully quit (no icon near the clock).\n\nIs Claude Desktop closed?" } },
            { "ask_relaunch",     new[] { "C'est fait. Relancer Claude Desktop ?", "Done. Relaunch Claude Desktop?" } },
            { "ask_restore",      new[] { "Remettre cette sauvegarde à la place de la configuration actuelle ?\n\n{0}\n\n(La configuration actuelle est elle-même sauvegardée avant.)",
                                          "Replace the current configuration with this backup?\n\n{0}\n\n(The current configuration is backed up first.)" } },
            { "close_failed",     new[] { "Impossible de fermer Claude Desktop. Quitte-le à la main (clic droit sur l'icône près de l'horloge → Quitter), puis réessaie.",
                                          "Couldn't close Claude Desktop. Quit it manually (right-click its icon near the clock → Quit), then try again." } },
            { "err_no_launch",    new[] { "Je n'ai pas trouvé comment lancer Claude Desktop : ouvre-le depuis le menu Démarrer.", "Couldn't find how to start Claude Desktop: open it from the Start menu." } },
            { "verify_failed",    new[] { "La vérification montre que certains accès sont toujours présents.", "Verification shows some access is still present." } },
            { "failed_restored",  new[] { "Échec : {0}\n\nLa sauvegarde a été remise en place, rien n'a changé.", "Failed: {0}\n\nThe backup was put back, nothing changed." } },
            { "log_start",        new[] { "Fichier : {0}", "File: {0}" } },
            { "log_loaded",       new[] { "{0} accès et {1} accord(s) mémorisé(s) trouvés.", "Found {0} grant(s) and {1} remembered consent(s)." } },
            { "log_closing",      new[] { "Fermeture de Claude Desktop…", "Closing Claude Desktop…" } },
            { "log_force",        new[] { "Claude Desktop reste ouvert (icône près de l'horloge) : arrêt forcé.", "Claude Desktop stays open (tray icon): forcing it to quit." } },
            { "log_closed",       new[] { "Claude Desktop est fermé.", "Claude Desktop is closed." } },
            { "log_backup",       new[] { "Sauvegarde : {0}", "Backup: {0}" } },
            { "log_saved",        new[] { "Configuration enregistrée et vérifiée.", "Configuration saved and verified." } },
            { "log_removed",      new[] { "Retiré : {0}", "Removed: {0}" } },
            { "log_launch",       new[] { "Claude Desktop est relancé.", "Claude Desktop restarted." } },
            { "log_restored",     new[] { "Sauvegarde remise en place : {0}", "Backup restored: {0}" } },
            { "log_cancel",       new[] { "Annulé, rien n'a été modifié.", "Cancelled, nothing was changed." } },
            { "no_backup",        new[] { "Aucune sauvegarde n'a encore été faite.", "No backup has been made yet." } },
            { "backup_filter",    new[] { "Sauvegardes de configuration", "Configuration backups" } },
            { "error",            new[] { "Erreur", "Error" } },
            { "about",            new[] { "v{0} · LePousson · non affilié à Anthropic", "v{0} · LePousson · not affiliated with Anthropic" } },
        };

        public static string T(string key)
        {
            string[] v;
            if (!S.TryGetValue(key, out v)) return key;
            return French ? v[0] : v[1];
        }

        public static string T(string key, params object[] args) { return string.Format(T(key), args); }
    }
}
