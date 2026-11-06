# LP Accès Claude

Petite application Windows qui liste les dossiers de ton PC partagés avec des chats de l'application **Claude Desktop**, et permet de retirer ces accès sans supprimer le chat ni toucher au dossier.

> **Outil non officiel**, non affilié à Anthropic. Il modifie un fichier interne de Claude Desktop (`claude_desktop_config.json`). Une sauvegarde est faite avant chaque modification. Une mise à jour de Claude peut le rendre inopérant.

## Utilisation

1. Lance `LP-AccesClaude.exe`. Rien à installer : il utilise .NET Framework 4.8, présent dans Windows 10 et 11.
   Au premier lancement d'un fichier téléchargé, Windows peut afficher « Windows a protégé votre ordinateur » : *Informations complémentaires* → *Exécuter quand même* (l'application n'est pas signée).
2. Coche les dossiers à retirer (clic n'importe où sur la ligne), et/ou les **accords mémorisés** (sans eux, Claude redemande ton accord si le dossier est de nouveau sollicité).
3. *Retirer la sélection* : récapitulatif → fermeture de Claude Desktop après confirmation → sauvegarde → modification → vérification → proposition de relancer Claude Desktop.

Autres boutons : *Ouvrir le chat* (lien vers la conversation), *Restaurer une sauvegarde…*, *Ouvrir le dossier* de la configuration.
Langue : celle de Windows (français ou anglais). Pour forcer : `LP-AccesClaude.exe --lang=en` ou `--lang=fr`.

## Où sont les données

- Configuration : `%APPDATA%\Claude\claude_desktop_config.json` (ou le dossier virtualisé d'une installation Microsoft Store).
  - `remoteChatFolderGrants` : dossiers accordés, chat par chat ;
  - `remoteFolderConsentMemory` : dossiers déjà acceptés.
- Sauvegardes : à côté, `claude_desktop_config.json.bak-AAAAMMJJ-HHMMSS`.

L'application ne vise que Claude Desktop : Claude Code en ligne de commande (`claude.exe` dans `.local`, npm ou WinGet) n'est jamais fermé.

## Télécharger

Dernière version : onglet **Releases** de ce dépôt (fichier `LP-AccesClaude-vX.Y.Z.zip`).

## Compiler soi-même

Code en C# 5 / WinForms (.NET Framework 4.8), sans dépendance. Sous Windows, sans rien installer :
- `build.cmd` → `dist\LP-AccesClaude.exe` (compilateur C# intégré à Windows) ;
- `test.cmd` → tests de la logique (lecture/écriture JSON, retrait des accès, sauvegardes).

Chaque version publiée est compilée par GitHub Actions à partir de ce dépôt (`.github/workflows/build.yml`).

## Code signing policy

Free code signing provided by [SignPath.io](https://about.signpath.io/), certificate by [SignPath Foundation](https://signpath.org/).
*Signature de code gratuite fournie par SignPath.io, certificat de la SignPath Foundation.*

**Team roles / Rôles**
- Committers and reviewers / Auteurs et relecteurs : [LePousson](https://github.com/LePousson) (owner, [members](https://github.com/LePousson/LP-Acces_Claude/graphs/contributors))
- Approvers / Approbateurs : [LePousson](https://github.com/LePousson)

Only binaries built by this repository's GitHub Actions workflow from its public source code are signed. Every release is approved manually before signing.
*Seuls les binaires compilés par le workflow GitHub Actions de ce dépôt, à partir de son code public, sont signés. Chaque version est approuvée à la main avant signature.*

**Privacy policy / Confidentialité** : this program will not transfer any information to other networked systems unless specifically requested by the user (clicking a chat link opens it in the browser).
*Ce programme ne transmet aucune information à d'autres systèmes en réseau, sauf demande explicite de l'utilisateur (cliquer sur un lien de chat l'ouvre dans le navigateur).*

---

# LP Claude Access (English)

A small Windows app that lists the folders on your PC shared with chats in the **Claude Desktop** app, and lets you remove that access without deleting the chat or touching the folder.

> **Unofficial tool**, not affiliated with Anthropic. It edits an internal Claude Desktop file (`claude_desktop_config.json`). A backup is made before every change. A Claude update may stop it from working.

1. Run `LP-AccesClaude.exe` (nothing to install, uses the .NET Framework 4.8 built into Windows 10/11). SmartScreen may warn on first run: *More info* → *Run anyway* (the app is unsigned).
2. Tick the folders and/or remembered consents to remove.
3. *Remove selection*: summary → closes Claude Desktop after confirmation → backup → edit → verification → offer to restart Claude Desktop.

Language follows Windows (French or English); force it with `--lang=en` / `--lang=fr`. Build it yourself with `build.cmd` / `test.cmd` (C# compiler built into Windows). Releases are built by GitHub Actions from this repository and signed by SignPath.io with a certificate from the SignPath Foundation (once the project is accepted). The app sends no data and makes no network calls, apart from opening a chat link in your browser when you click it.
