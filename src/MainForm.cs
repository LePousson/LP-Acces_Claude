// Fenêtre principale : liste des accès, choix, et application des retraits.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace LpAccesClaude
{
    public sealed class MainForm : Form
    {
        // ---------------------------------------------------------------- Palette (logique sombre LP)
        static readonly Color CBack = Color.FromArgb(0x14, 0x14, 0x16);
        static readonly Color CCard = Color.FromArgb(0x1c, 0x1c, 0x1e);
        static readonly Color CRowB = Color.FromArgb(0x23, 0x23, 0x25);
        static readonly Color CPill = Color.FromArgb(0x2c, 0x2c, 0x2e);
        static readonly Color CBorder = Color.FromArgb(0x3a, 0x3a, 0x3c);
        static readonly Color CText = Color.FromArgb(0xf2, 0xf2, 0xf7);
        static readonly Color CMuted = Color.FromArgb(0x8e, 0x8e, 0x93);
        static readonly Color CHlBg = Color.FromArgb(0x1e, 0x2a, 0x36);
        static readonly Color CBlue = Color.FromArgb(0x4f, 0xa3, 0xe0);
        static readonly Color CGreen = Color.FromArgb(0x22, 0xc5, 0x5e);
        static readonly Color COrange = Color.FromArgb(0xf5, 0x9e, 0x0b);
        static readonly Color CRed = Color.FromArgb(0xef, 0x44, 0x44);

        readonly float scale;
        int Px(int n) { return (int)Math.Round(n * scale); }

        ConfigStore store;
        List<Grant> grants = new List<Grant>();
        List<string> consents = new List<string>();

        DataGridView gridGrants, gridConsents;
        Label emptyGrants, emptyConsents, statusLabel, pathLabel;
        Panel statusDot;
        RichTextBox logBox;
        Button btnRefresh, btnAll, btnNone, btnApply, btnRestore, btnFolder;
        System.Windows.Forms.Timer statusTimer;
        bool busy;

        public MainForm()
        {
            using (var g = CreateGraphics()) scale = g.DpiX / 96f;
            Text = Lang.T("title");
            Font = new Font("Segoe UI", 9.75f);
            BackColor = CBack;
            ForeColor = CText;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(Px(1000), Px(720));
            MinimumSize = new Size(Px(760), Px(560));
            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (Exception) { }
            BuildUi();
            statusTimer = new System.Windows.Forms.Timer { Interval = 3000 };
            statusTimer.Tick += (s, e) => UpdateStatus();
            statusTimer.Start();
            Shown += (s, e) => LoadConfig(true);
        }

        // ---------------------------------------------------------------- Barre de titre sombre (Windows 10 20H1+ / 11)
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try { int on = 1; if (DwmSetWindowAttribute(Handle, 20, ref on, 4) != 0) DwmSetWindowAttribute(Handle, 19, ref on, 4); } catch (Exception) { }
        }

        // ---------------------------------------------------------------- Construction de l'interface
        void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 9, BackColor = CBack,
                Padding = new Padding(Px(18), Px(14), Px(18), Px(12))
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // 0 en-tête
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // 1 avertissement
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // 2 titre accès
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 62));    // 3 grille accès
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // 4 titre accords
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 38));    // 5 grille accords
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // 6 boutons
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, Px(96)));// 7 journal
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));       // 8 pied
            Controls.Add(root);

            // En-tête : titre + état de Claude Desktop
            var head = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true, Margin = new Padding(0, 0, 0, Px(8)) };
            head.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            head.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var titles = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, AutoSize = true, WrapContents = false, Margin = Padding.Empty };
            titles.Controls.Add(new Label { Text = Lang.T("title"), AutoSize = true, Font = new Font("Segoe UI Semibold", 16f), ForeColor = CText, Margin = Padding.Empty });
            titles.Controls.Add(new Label { Text = Lang.T("subtitle"), AutoSize = true, ForeColor = CMuted, Margin = new Padding(Px(2), Px(2), 0, 0) });
            head.Controls.Add(titles, 0, 0);
            var pill = new FlowLayoutPanel { AutoSize = true, BackColor = CPill, Padding = new Padding(Px(10), Px(6), Px(12), Px(6)), Anchor = AnchorStyles.Right, WrapContents = false };
            statusDot = new Panel { Size = new Size(Px(10), Px(10)), Margin = new Padding(0, Px(5), Px(8), 0), BackColor = CMuted };
            MakeRound(statusDot);
            statusLabel = new Label { AutoSize = true, ForeColor = CText, Margin = Padding.Empty, Font = new Font("Segoe UI Semibold", 9.75f) };
            pill.Controls.Add(statusDot);
            pill.Controls.Add(statusLabel);
            MakeRound(pill, Px(14));
            head.Controls.Add(pill, 1, 0);
            root.Controls.Add(head, 0, 0);

            // Avertissement « outil non officiel » (hauteur calculée d'après le texte)
            var warn = new Panel { Dock = DockStyle.Top, BackColor = Color.FromArgb(0x2a, 0x22, 0x12), Margin = new Padding(0, 0, 0, Px(10)), Height = Px(48) };
            var warnBar = new Panel { BackColor = COrange, Dock = DockStyle.Left, Width = Px(5) };
            var warnText = new Label { Text = Lang.T("warning"), ForeColor = Color.FromArgb(0xf3, 0xd9, 0xa8), AutoSize = false, Dock = DockStyle.Fill, Padding = new Padding(Px(12), Px(8), Px(10), Px(8)), TextAlign = ContentAlignment.MiddleLeft };
            warn.Controls.Add(warnText);
            warn.Controls.Add(warnBar);
            EventHandler fitWarn = (s, e) =>
            {
                int w = Math.Max(Px(200), warn.Width - warnBar.Width - warnText.Padding.Horizontal);
                var size = TextRenderer.MeasureText(warnText.Text, warnText.Font, new Size(w, int.MaxValue), TextFormatFlags.WordBreak);
                int h = size.Height + warnText.Padding.Vertical + Px(2);
                if (warn.Height != h) warn.Height = h;
            };
            warn.SizeChanged += fitWarn;
            warn.HandleCreated += fitWarn;
            root.Controls.Add(warn, 0, 1);

            // Accès
            root.Controls.Add(SectionTitle(Lang.T("grants")), 0, 2);
            gridGrants = MakeGrid();
            gridGrants.Columns.Add(new DataGridViewCheckBoxColumn { Name = "sel", HeaderText = "", Width = Px(36), Resizable = DataGridViewTriState.False });
            gridGrants.Columns.Add(new DataGridViewTextBoxColumn { Name = "folder", HeaderText = Lang.T("col_folder"), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 55, ReadOnly = true });
            gridGrants.Columns.Add(new DataGridViewTextBoxColumn { Name = "chat", HeaderText = Lang.T("col_chat"), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, FillWeight = 45, ReadOnly = true });
            var openCol = new DataGridViewLinkColumn { Name = "open", HeaderText = Lang.T("col_open"), Width = Px(120), ReadOnly = true };
            gridGrants.Columns.Add(openCol);
            openCol.LinkColor = CBlue;              // couleurs réglées après l'ajout à la grille
            openCol.ActiveLinkColor = CText;
            openCol.VisitedLinkColor = CBlue;
            openCol.TrackVisitedState = false;
            openCol.LinkBehavior = LinkBehavior.HoverUnderline;
            gridGrants.CellContentClick += GridGrantsClick;
            emptyGrants = EmptyLabel(Lang.T("none_grants"));
            root.Controls.Add(Wrap(gridGrants, emptyGrants), 0, 3);

            // Accords mémorisés
            root.Controls.Add(SectionTitle(Lang.T("consents")), 0, 4);
            gridConsents = MakeGrid();
            gridConsents.Columns.Add(new DataGridViewCheckBoxColumn { Name = "sel", HeaderText = "", Width = Px(36), Resizable = DataGridViewTriState.False });
            gridConsents.Columns.Add(new DataGridViewTextBoxColumn { Name = "folder", HeaderText = Lang.T("col_folder"), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill, ReadOnly = true });
            gridConsents.CellContentClick += (s, e) => ToggleCheck(gridConsents, e);
            emptyConsents = EmptyLabel(Lang.T("none_consents"));
            root.Controls.Add(Wrap(gridConsents, emptyConsents), 0, 5);

            // Boutons
            var bar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 7, AutoSize = true, Margin = new Padding(0, Px(10), 0, Px(8)) };
            for (int i = 0; i < 7; i++) bar.ColumnStyles.Add(new ColumnStyle(i == 3 ? SizeType.Percent : SizeType.AutoSize, i == 3 ? 100 : 0));
            btnRefresh = MakeButton(Lang.T("btn_refresh"), false);
            btnAll = MakeButton(Lang.T("btn_all"), false);
            btnNone = MakeButton(Lang.T("btn_none"), false);
            btnRestore = MakeButton(Lang.T("btn_restore"), false);
            btnFolder = MakeButton(Lang.T("btn_folder"), false);
            btnApply = MakeButton(Lang.T("btn_apply"), true);
            btnRefresh.Click += (s, e) => LoadConfig(true);
            btnAll.Click += (s, e) => SetAll(true);
            btnNone.Click += (s, e) => SetAll(false);
            btnApply.Click += (s, e) => ApplyRemoval();
            btnRestore.Click += (s, e) => RestoreBackup();
            btnFolder.Click += (s, e) => OpenFolder();
            bar.Controls.Add(btnRefresh, 0, 0);
            bar.Controls.Add(btnAll, 1, 0);
            bar.Controls.Add(btnNone, 2, 0);
            bar.Controls.Add(btnRestore, 4, 0);
            bar.Controls.Add(btnFolder, 5, 0);
            bar.Controls.Add(btnApply, 6, 0);
            root.Controls.Add(bar, 0, 6);

            // Journal
            logBox = new RichTextBox
            {
                Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = CCard, ForeColor = CMuted,
                Font = new Font("Consolas", 9f), DetectUrls = false, Margin = Padding.Empty, ScrollBars = RichTextBoxScrollBars.Vertical
            };
            var logWrap = new Panel { Dock = DockStyle.Fill, BackColor = CCard, Padding = new Padding(Px(10), Px(6), Px(6), Px(6)), Margin = Padding.Empty };
            logWrap.Controls.Add(logBox);
            root.Controls.Add(logWrap, 0, 7);

            // Pied
            var foot = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true, Margin = new Padding(0, Px(8), 0, 0) };
            foot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foot.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            pathLabel = new Label { AutoSize = true, ForeColor = CMuted, Margin = Padding.Empty, AutoEllipsis = true };
            foot.Controls.Add(pathLabel, 0, 0);
            foot.Controls.Add(new Label { Text = Lang.T("about", Program.Version), AutoSize = true, ForeColor = CMuted, Margin = Padding.Empty }, 1, 0);
            root.Controls.Add(foot, 0, 8);
        }

        Label SectionTitle(string text)
        {
            return new Label { Text = text.ToUpperInvariant(), AutoSize = true, ForeColor = CMuted, Font = new Font("Segoe UI Semibold", 8.5f), Margin = new Padding(0, Px(6), 0, Px(6)) };
        }

        Label EmptyLabel(string text)
        {
            return new Label { Text = text, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = CMuted, BackColor = CCard, Visible = false };
        }

        Panel Wrap(DataGridView grid, Label empty)
        {
            var p = new Panel { Dock = DockStyle.Fill, BackColor = CCard, Margin = Padding.Empty, Padding = new Padding(1) };
            p.Controls.Add(grid);
            p.Controls.Add(empty);
            return p;
        }

        DataGridView MakeGrid()
        {
            var g = new DataGridView
            {
                Dock = DockStyle.Fill, BackgroundColor = CCard, BorderStyle = BorderStyle.None, GridColor = CPill,
                RowHeadersVisible = false, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AllowUserToResizeRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, EnableHeadersVisualStyles = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing, ColumnHeadersHeight = Px(32),
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
                ShowCellToolTips = true, StandardTab = true
            };
            g.RowTemplate.Height = Px(30);
            g.DefaultCellStyle.BackColor = CCard;
            g.DefaultCellStyle.ForeColor = CText;
            g.DefaultCellStyle.SelectionBackColor = CHlBg;
            g.DefaultCellStyle.SelectionForeColor = CText;
            g.DefaultCellStyle.Padding = new Padding(Px(6), 0, Px(6), 0);
            g.AlternatingRowsDefaultCellStyle.BackColor = CRowB;
            g.ColumnHeadersDefaultCellStyle.BackColor = CPill;
            g.ColumnHeadersDefaultCellStyle.ForeColor = CMuted;
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = CPill;
            g.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 8.5f);
            g.ColumnHeadersDefaultCellStyle.Padding = new Padding(Px(6), 0, Px(6), 0);
            // Clic n'importe où sur la ligne = cocher / décocher
            g.CellClick += (s, e) =>
            {
                if (e.RowIndex < 0 || busy) return;
                string col = g.Columns[e.ColumnIndex].Name;
                if (col == "open" || col == "sel") return;
                var cell = g.Rows[e.RowIndex].Cells["sel"];
                cell.Value = !(cell.Value is bool && (bool)cell.Value);
            };
            g.CurrentCellDirtyStateChanged += (s, e) => { if (g.IsCurrentCellDirty) g.CommitEdit(DataGridViewDataErrorContexts.Commit); };
            return g;
        }

        Button MakeButton(string text, bool accent)
        {
            var b = new Button
            {
                Text = text, AutoSize = true, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, UseVisualStyleBackColor = false,
                Padding = new Padding(Px(10), Px(4), Px(10), Px(4)), Margin = new Padding(0, 0, Px(8), 0),
                BackColor = accent ? CHlBg : CPill, ForeColor = accent ? CBlue : CText,
                Font = accent ? new Font("Segoe UI Semibold", 9.75f) : Font
            };
            b.FlatAppearance.BorderColor = accent ? CBlue : CBorder;
            b.FlatAppearance.MouseOverBackColor = accent ? Color.FromArgb(0x24, 0x38, 0x4c) : Color.FromArgb(0x38, 0x38, 0x3a);
            b.FlatAppearance.MouseDownBackColor = accent ? Color.FromArgb(0x2c, 0x46, 0x60) : CBorder;
            return b;
        }

        static void MakeRound(Control c, int radius = 0)
        {
            EventHandler apply = (s, e) =>
            {
                if (c.Width <= 0 || c.Height <= 0) return;
                int r = radius > 0 ? Math.Min(radius, Math.Min(c.Width, c.Height)) : Math.Min(c.Width, c.Height);
                var path = new System.Drawing.Drawing2D.GraphicsPath();
                path.AddArc(0, 0, r, r, 180, 90);
                path.AddArc(c.Width - r, 0, r, r, 270, 90);
                path.AddArc(c.Width - r, c.Height - r, r, r, 0, 90);
                path.AddArc(0, c.Height - r, r, r, 90, 90);
                path.CloseFigure();
                c.Region = new Region(path);
            };
            c.SizeChanged += apply;
            c.HandleCreated += apply;
        }

        // ---------------------------------------------------------------- Journal et état
        void Log(string text, Color? color = null)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => Log(text, color))); return; }
            logBox.SelectionStart = logBox.TextLength;
            logBox.SelectionColor = color ?? CMuted;
            logBox.AppendText((logBox.TextLength > 0 ? "\n" : "") + DateTime.Now.ToString("HH:mm:ss") + "  " + text);
            logBox.ScrollToCaret();
        }

        void UpdateStatus()
        {
            bool running = false;
            try { running = ClaudeDesktop.IsRunning(); } catch (Exception) { }
            statusDot.BackColor = running ? CGreen : CMuted;
            statusLabel.Text = Lang.T(running ? "status_running" : "status_closed");
        }

        void SetBusy(bool b)
        {
            if (InvokeRequired) { Invoke(new Action(() => SetBusy(b))); return; }
            busy = b;
            foreach (var c in new Control[] { btnRefresh, btnAll, btnNone, btnApply, btnRestore, btnFolder, gridGrants, gridConsents }) c.Enabled = !b;
            UseWaitCursor = b;
        }

        // ---------------------------------------------------------------- Chargement
        bool LoadConfig(bool log)
        {
            UpdateStatus();
            string path = store != null && File.Exists(store.Path) ? store.Path : ConfigStore.FindConfig();
            if (path == null)
            {
                store = null;
                grants.Clear(); consents.Clear(); FillGrids();
                pathLabel.Text = "";
                Log(Lang.T("no_config"), CRed);
                return false;
            }
            store = new ConfigStore(path);
            pathLabel.Text = Lang.T("config", path);
            try { store.Load(); }
            catch (Exception ex)
            {
                grants.Clear(); consents.Clear(); FillGrids();
                Log(Lang.T("bad_json", ex.Message), CRed);
                return false;
            }
            grants = store.Grants();
            consents = store.Consents();
            FillGrids();
            if (log)
            {
                Log(Lang.T("log_loaded", grants.Count, consents.Count));
                string where = store.Where(ConfigStore.GrantsKey);
                if (where != null) Log(Lang.T("log_where", where));
            }
            return true;
        }

        void FillGrids()
        {
            gridGrants.Rows.Clear();
            foreach (var g in grants.OrderBy(x => x.Chat, StringComparer.Ordinal).ThenBy(x => x.Folder, StringComparer.OrdinalIgnoreCase))
            {
                int i = gridGrants.Rows.Add(false, g.Folder, g.Chat, Lang.T("open_chat"));
                gridGrants.Rows[i].Tag = g;
                gridGrants.Rows[i].Cells["chat"].ToolTipText = g.ChatUrl;
                gridGrants.Rows[i].Cells["chat"].Style.ForeColor = CMuted;
                gridGrants.Rows[i].Cells["open"].ToolTipText = g.ChatUrl;
            }
            gridConsents.Rows.Clear();
            foreach (var c in consents)
            {
                int i = gridConsents.Rows.Add(false, c);
                gridConsents.Rows[i].Tag = c;
            }
            gridGrants.Visible = grants.Count > 0; emptyGrants.Visible = grants.Count == 0;
            gridConsents.Visible = consents.Count > 0; emptyConsents.Visible = consents.Count == 0;
            gridGrants.ClearSelection(); gridConsents.ClearSelection();
        }

        void GridGrantsClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (gridGrants.Columns[e.ColumnIndex].Name == "open")
            {
                var g = gridGrants.Rows[e.RowIndex].Tag as Grant;
                if (g != null) try { Process.Start(new ProcessStartInfo(g.ChatUrl) { UseShellExecute = true }); } catch (Exception) { }
                return;
            }
            ToggleCheck(gridGrants, e);
        }

        static void ToggleCheck(DataGridView grid, DataGridViewCellEventArgs e)
        {
            // La case à cocher est validée tout de suite par CurrentCellDirtyStateChanged.
        }

        void SetAll(bool on)
        {
            foreach (DataGridViewRow r in gridGrants.Rows) r.Cells["sel"].Value = on;
            foreach (DataGridViewRow r in gridConsents.Rows) r.Cells["sel"].Value = on;
            gridGrants.EndEdit(); gridConsents.EndEdit();
        }

        static bool Checked(DataGridViewRow r) { var v = r.Cells["sel"].Value; return v is bool && (bool)v; }

        // ---------------------------------------------------------------- Boîtes de dialogue
        bool Ask(string text, MessageBoxIcon icon = MessageBoxIcon.Question)
        {
            if (InvokeRequired) return (bool)Invoke(new Func<bool>(() => Ask(text, icon)));
            return MessageBox.Show(this, text, Lang.T("title"), MessageBoxButtons.YesNo, icon, MessageBoxDefaultButton.Button2) == DialogResult.Yes;
        }

        void Tell(string text, MessageBoxIcon icon)
        {
            if (InvokeRequired) { Invoke(new Action(() => Tell(text, icon))); return; }
            MessageBox.Show(this, text, Lang.T("title"), MessageBoxButtons.OK, icon);
        }

        // ---------------------------------------------------------------- Retrait des accès
        void ApplyRemoval()
        {
            if (busy || store == null) return;
            gridGrants.EndEdit(); gridConsents.EndEdit();
            var remove = gridGrants.Rows.Cast<DataGridViewRow>().Where(Checked).Select(r => (Grant)r.Tag).ToList();
            var forget = gridConsents.Rows.Cast<DataGridViewRow>().Where(Checked).Select(r => (string)r.Tag).ToList();
            if (remove.Count == 0 && forget.Count == 0) { Tell(Lang.T("nothing"), MessageBoxIcon.Information); return; }

            // Dossiers qui ne seront plus accordés nulle part mais restent mémorisés : proposer de les oublier aussi
            var remaining = grants.Where(g => !remove.Any(r => r.SameAs(g))).Select(g => g.Folder).ToList();
            var orphans = remove.Select(r => r.Folder).Distinct()
                .Where(f => consents.Contains(f) && !remaining.Contains(f) && !forget.Contains(f)).ToList();
            if (orphans.Count > 0 && Ask(Lang.T("ask_forget", string.Join("\n", orphans.Select(f => "• " + f)))))
                forget.AddRange(orphans);

            string recap = string.Join("\n", remove.Select(r => Lang.T("recap_grant", r.Folder, r.Chat)))
                + (remove.Count > 0 ? "\n" : "")
                + string.Join("\n", forget.Select(f => Lang.T("recap_consent", f))) + "\n";
            if (!Ask(Lang.T("recap", recap))) { Log(Lang.T("log_cancel")); return; }

            bool running = ClaudeDesktop.IsRunning();
            if (running && !Ask(Lang.T("ask_close"), MessageBoxIcon.Warning)) { Log(Lang.T("log_cancel")); return; }

            SetBusy(true);
            new Thread(() => RunRemoval(remove, forget, running)) { IsBackground = true }.Start();
        }

        void RunRemoval(List<Grant> remove, List<string> forget, bool wasRunning)
        {
            ClaudeDesktop.Launcher launcher = null;
            try
            {
                if (!CloseClaude(out launcher)) return;

                // Relecture : l'application a pu écrire le fichier en se fermant
                var fresh = new ConfigStore(store.Path);
                fresh.Load();
                string backup = fresh.Backup();
                Log(Lang.T("log_backup", backup));
                try
                {
                    fresh.Remove(remove, forget);
                    fresh.Save();
                    var check = new ConfigStore(store.Path);
                    check.Load();
                    var left = check.Grants();
                    var mem = check.Consents();
                    if (remove.Any(r => left.Any(g => g.SameAs(r))) || forget.Any(f => mem.Contains(f)))
                        throw new InvalidOperationException(Lang.T("verify_failed"));
                    foreach (var r in remove) Log(Lang.T("log_removed", r.Folder + "  ←  " + r.Chat), CGreen);
                    foreach (var f in forget) Log(Lang.T("log_removed", f), CGreen);
                    Log(Lang.T("log_saved"), CGreen);
                }
                catch (Exception ex)
                {
                    try { File.Copy(backup, store.Path, true); } catch (Exception) { }
                    Log(ex.Message, CRed);
                    Tell(Lang.T("failed_restored", ex.Message), MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                Log(ex.Message, CRed);
                Tell(ex.Message, MessageBoxIcon.Error);
            }
            finally
            {
                Invoke(new Action(() => { LoadConfig(false); SetBusy(false); }));
                if (launcher != null && wasRunning) OfferRelaunch(launcher);
            }
        }

        // Ferme Claude Desktop (si besoin) en mémorisant comment le relancer.
        bool CloseClaude(out ClaudeDesktop.Launcher launcher)
        {
            launcher = null;
            if (ClaudeDesktop.IsRunning())
            {
                launcher = ClaudeDesktop.FindLauncher();
                Log(Lang.T("log_closing"));
                foreach (var p in ClaudeDesktop.Scan().Paths) Log("  " + p);
                if (!ClaudeDesktop.Close(s => Log(s, COrange)))
                {
                    Log(Lang.T("close_failed"), CRed);
                    Tell(Lang.T("close_failed"), MessageBoxIcon.Error);
                    launcher = null;
                    return false;
                }
                Log(Lang.T("log_closed"));
                Thread.Sleep(800);
            }
            if (ClaudeDesktop.Scan().Unknown.Count > 0 && !Ask(Lang.T("ask_unknown"), MessageBoxIcon.Warning))
            {
                Log(Lang.T("log_cancel"));
                return false;
            }
            if (launcher == null) launcher = ClaudeDesktop.FindLauncher();
            return true;
        }

        void OfferRelaunch(ClaudeDesktop.Launcher launcher)
        {
            if (!Ask(Lang.T("ask_relaunch"))) return;
            try { ClaudeDesktop.Launch(launcher); Log(Lang.T("log_launch"), CGreen); }
            catch (Exception ex) { Log(ex.Message, CRed); Tell(Lang.T("err_no_launch"), MessageBoxIcon.Warning); }
            Invoke(new Action(UpdateStatus));
        }

        // ---------------------------------------------------------------- Restauration
        void RestoreBackup()
        {
            if (busy || store == null) return;
            if (store.Backups().Count == 0) { Tell(Lang.T("no_backup"), MessageBoxIcon.Information); return; }
            string chosen;
            using (var dlg = new OpenFileDialog
            {
                InitialDirectory = Path.GetDirectoryName(store.Path),
                Filter = Lang.T("backup_filter") + "|" + Path.GetFileName(store.Path) + ConfigStore.BackupMarker + "*",
                CheckFileExists = true, Multiselect = false
            })
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                chosen = dlg.FileName;
            }
            if (!Ask(Lang.T("ask_restore", chosen), MessageBoxIcon.Warning)) return;
            bool running = ClaudeDesktop.IsRunning();
            if (running && !Ask(Lang.T("ask_close"), MessageBoxIcon.Warning)) { Log(Lang.T("log_cancel")); return; }

            SetBusy(true);
            new Thread(() =>
            {
                ClaudeDesktop.Launcher launcher = null;
                try
                {
                    if (!CloseClaude(out launcher)) return;
                    var cur = new ConfigStore(store.Path);
                    Log(Lang.T("log_backup", cur.Backup()));
                    cur.Restore(chosen);
                    Log(Lang.T("log_restored", chosen), CGreen);
                }
                catch (Exception ex) { Log(ex.Message, CRed); Tell(ex.Message, MessageBoxIcon.Error); }
                finally
                {
                    Invoke(new Action(() => { LoadConfig(true); SetBusy(false); }));
                    if (launcher != null && running) OfferRelaunch(launcher);
                }
            }) { IsBackground = true }.Start();
        }

        void OpenFolder()
        {
            if (store == null) return;
            try { Process.Start("explorer.exe", "/select,\"" + store.Path + "\""); } catch (Exception) { }
        }
    }
}
