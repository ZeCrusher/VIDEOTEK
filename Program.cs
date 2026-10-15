using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FFmpegConverterApp
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }

    public class VideoItem
    {
        public string FilePath { get; set; } = "";
        public string FileName => Path.GetFileName(FilePath);
        public string Duration { get; set; } = "En attente...";
        public string Size { get; set; } = "";
        public string Status { get; set; } = "En attente";
    }

    public class MainForm : Form
    {
        private bool isDarkMode = true;

        // Palette dynamique pour le thème Good / Keentheme
        private Color BgDarkSidebar;
        private Color BgDarkMain;
        private Color BgCard;
        private Color BgInput;
        private Color BorderColor;
        private Color TextPrimary;
        private Color TextMuted;

        // Couleurs d'accentuation
        private static readonly Color AccentPurple = Color.FromArgb(99, 91, 255);
        private static readonly Color AccentGreen = Color.FromArgb(80, 205, 137);
        private static readonly Color AccentRed = Color.FromArgb(241, 65, 108);

        // Sidebar
        private Panel panelSidebar = null!;
        private PictureBox picLogo = null!;
        private Label lblAppTitle = null!;
        private Label lblProjectsHeader = null!;
        private RoundedButton btnNavConvert = null!;
        private RoundedButton btnNavBench = null!;
        private RoundedButton btnThemeToggle = null!;
        private ListBox lstSidebarFiles = null!;

        // Zone principale
        private Panel panelMain = null!;
        private TextBox txtSourceBar = null!;
        private Button btnBrowseSource = null!;

        // GroupBoxes
        private RoundedGroupBox grpPreset = null!, grpProg = null!;

        // Contrôles des options
        private Label lblOut = null!, lblFmt = null!, lblCodec = null!, lblRes = null!, lblSpeed = null!, lblExist = null!;
        private ComboBox cboCodec = null!, cboResolution = null!, cboSpeedPreset = null!;
        private TextBox txtOutFolder = null!;
        private Button btnBrowseOutFolder = null!;

        private RadioButton rbMKV = null!, rbMP4 = null!, rbAVI = null!, rbWebM = null!;
        private RadioButton rbExistOverwrite = null!, rbExistSkip = null!, rbExistRename = null!, rbExistAsk = null!;
        private CheckBox chkFastStart = null!, chkAAC = null!, chkMute = null!;

        // Glisser-déposer & Grille
        private RoundedPanel dragDropZone = null!;
        private Label lblDrag = null!;
        private DataGridView gridFiles = null!;
        private List<VideoItem> videoList = new List<VideoItem>();
        private Button btnRemove = null!, btnClear = null!;

        // Progression & Actions
        private RoundedButton btnStart = null!, btnStop = null!, btnBench = null!;
        private RoundedProgressBar progressBar = null!;
        private Label lblStatus = null!;
        private Label lblSpeedInfo = null!;
        private RichTextBox txtLog = null!;

        private bool isConverting = false;
        private bool stopRequested = false;

        public MainForm()
        {
            InitUI();
            LoadEmbeddedLogo();
            ApplyTheme();
        }

        private void InitUI()
        {
            this.Text = "ODYSSÉE - SERGE TSK - Videotek Commander 2026";
            this.Size = new Size(1250, 830);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            this.MinimumSize = new Size(1100, 750);

            // --- PANNEAU LATÉRAL (SIDEBAR 240px) ---
            panelSidebar = new Panel { Dock = DockStyle.Left, Width = 240 };

            picLogo = new PictureBox
            {
                Location = new Point(15, 15),
                Size = new Size(40, 40),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent
            };

            lblAppTitle = new Label
            {
                Text = "VIDEOTEK\n2026.TSK- Odyssée",
                Location = new Point(62, 16),
                Size = new Size(165, 38),
                Font = new Font("Segoe UI", 8.5F, FontStyle.Bold)
            };

            btnNavConvert = CreateRoundedButton("Convertir", 15, 75, 210, 38, AccentPurple, Color.White);
            btnNavBench = CreateRoundedButton("Benchmark", 15, 120, 210, 38, Color.Gray, Color.White);
            btnNavBench.Click += (s, e) => RunBenchmark();

            btnThemeToggle = CreateRoundedButton("Mode Clair", 15, 165, 210, 32, Color.Gray, Color.White);
            btnThemeToggle.Click += (s, e) => { isDarkMode = !isDarkMode; ApplyTheme(); };

            lblProjectsHeader = new Label
            {
                Text = "LISTE DES FICHIERS",
                Location = new Point(15, 220),
                AutoSize = true,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            lstSidebarFiles = new ListBox
            {
                Location = new Point(15, 245),
                Size = new Size(210, 500),
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 8.5F)
            };

            panelSidebar.Controls.AddRange(new Control[] {
                picLogo, lblAppTitle, btnNavConvert, btnNavBench, btnThemeToggle, lblProjectsHeader, lstSidebarFiles
            });

            // --- ZONE PRINCIPALE ---
            panelMain = new Panel { Dock = DockStyle.Fill, Padding = new Padding(20) };

            txtSourceBar = new TextBox
            {
                Location = new Point(20, 18),
                Size = new Size(820, 28),
                BorderStyle = BorderStyle.FixedSingle,
                Text = "Glissez vos fichiers ou entrez le chemin d'accès..."
            };

            btnBrowseSource = CreateRoundedButton("Choisir un fichier", 850, 17, 110, 29, AccentPurple, Color.White);
            btnBrowseSource.Click += (s, e) => OpenFilesDialog();

            // Groupe des Presets & Options
            grpPreset = CreateGroupBox("Presets & Directives d'encodage", 20, 60, 940, 230);

            lblOut = new Label { Text = "Dossier de sortie :", Location = new Point(15, 25), AutoSize = true };
            txtOutFolder = new TextBox { Location = new Point(130, 22), Size = new Size(650, 25), BorderStyle = BorderStyle.FixedSingle };
            txtOutFolder.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "Converted");

            btnBrowseOutFolder = CreateRoundedButton("Parcourir", 790, 21, 130, 27, Color.Gray, Color.White);
            btnBrowseOutFolder.Click += (s, e) => {
                using var dlg = new FolderBrowserDialog();
                if (dlg.ShowDialog() == DialogResult.OK) txtOutFolder.Text = dlg.SelectedPath;
            };

            lblFmt = new Label { Text = "Format Container :", Location = new Point(15, 62), AutoSize = true };
            rbMP4 = CreateRadioButton("MP4", 140, 60, true);
            rbMKV = CreateRadioButton("MKV", 210, 60, false);
            rbAVI = CreateRadioButton("AVI", 280, 60, false);
            rbWebM = CreateRadioButton("WebM", 340, 60, false);

            lblCodec = new Label { Text = "Codec Vidéo :", Location = new Point(15, 98), AutoSize = true };
            cboCodec = new ComboBox { Location = new Point(140, 95), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            cboCodec.Items.AddRange(new object[] { "H.264 (libx264)", "H.265 / HEVC (libx265)", "VP9 (libvpx-vp9)", "Copie Directe (Stream Copy)" });
            cboCodec.SelectedIndex = 0;

            lblRes = new Label { Text = "Résolution :", Location = new Point(360, 98), AutoSize = true };
            cboResolution = new ComboBox { Location = new Point(440, 95), Size = new Size(200, 25), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            cboResolution.Items.AddRange(new object[] {
                "Auto / Originale", "160p (284x160)", "240p (426x240)", "360p (640x360)",
                "480p (854x480)", "720p (1280x720 HD)", "1080p (1920x1080 Full HD)", "1440p (2K)", "2160p (4K UHD)"
            });
            cboResolution.SelectedIndex = 0;

            lblSpeed = new Label { Text = "Vitesse Encoder (Preset) :", Location = new Point(650, 98), AutoSize = true };
            cboSpeedPreset = new ComboBox { Location = new Point(810, 95), Size = new Size(110, 25), DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat };
            cboSpeedPreset.Items.AddRange(new object[] { "ultrafast", "superfast", "veryfast", "faster", "fast", "medium", "slow", "slower", "veryslow" });
            cboSpeedPreset.SelectedIndex = 5;

            chkFastStart = CreateCheckBox("Enable Fast Start (moov atom at beginning)", 15, 135);
            chkFastStart.Checked = true;
            chkAAC = CreateCheckBox("Audio AAC (192 kbps)", 360, 135);
            chkAAC.Checked = true;
            chkMute = CreateCheckBox("Mute (Supprimer son)", 550, 135);

            lblExist = new Label { Text = "Fichier existant :", Location = new Point(15, 175), AutoSize = true };
            rbExistOverwrite = CreateRadioButton("Écraser", 140, 173, true);
            rbExistRename = CreateRadioButton("Renommer", 220, 173, false);
            rbExistSkip = CreateRadioButton("Ignorer", 320, 173, false);
            rbExistAsk = CreateRadioButton("Demander", 400, 173, false);

            grpPreset.Controls.AddRange(new Control[] {
                lblOut, txtOutFolder, btnBrowseOutFolder, lblFmt, rbMP4, rbMKV, rbAVI, rbWebM,
                lblCodec, cboCodec, lblRes, cboResolution, lblSpeed, cboSpeedPreset,
                chkFastStart, chkAAC, chkMute, lblExist, rbExistOverwrite, rbExistRename, rbExistSkip, rbExistAsk
            });

            // Zone Glisser-Déposer
            dragDropZone = new RoundedPanel { Location = new Point(20, 300), Size = new Size(940, 55), CornerRadius = 10, AllowDrop = true };

            lblDrag = new Label
            {
                Text = "Glissez et déposez vos fichiers vidéo ici pour charger la file d'attente...",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = AccentPurple,
                Font = new Font("Segoe UI", 9.5F, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            dragDropZone.Controls.Add(lblDrag);

            lblDrag.Click += (s, e) => OpenFilesDialog();
            dragDropZone.Click += (s, e) => OpenFilesDialog();
            dragDropZone.DragEnter += DragDropZone_DragEnter;
            dragDropZone.DragDrop += DragDropZone_DragDrop;
            lblDrag.DragEnter += DragDropZone_DragEnter;
            lblDrag.DragDrop += DragDropZone_DragDrop;

            // --- TABLEAU (DATA GRID VIEW - STYLE KEENTHEME GOOD) ---
            gridFiles = new DataGridView
            {
                Location = new Point(20, 365),
                Size = new Size(940, 180),
                BorderStyle = BorderStyle.None,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AllowUserToAddRows = false,
                AutoGenerateColumns = false,
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 36,
                RowTemplate = { Height = 32 }
            };

            gridFiles.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            gridFiles.DefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            gridFiles.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "FileName", 	HeaderText = "NOM DU FICHIER", Width = 380 });
            gridFiles.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Duration", 	HeaderText = "DURÉE", Width = 120 });
            gridFiles.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Size", 		HeaderText = "TAILLE", Width = 120 });
            gridFiles.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", 		HeaderText = "STATUT", Width = 300 });

            // Boutons d'action
            btnStart = CreateRoundedButton("Démarrer la conversion", 20, 555, 300, 42, AccentPurple, Color.White);
            btnStart.Font = new Font("Segoe UI", 10.5F, FontStyle.Bold);
            btnStart.Click += BtnStart_Click;

            btnBench = CreateRoundedButton("BENCH (Tester la puissance)", 330, 555, 250, 42, AccentGreen, Color.Black);
            btnBench.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnBench.Click += (s, e) => RunBenchmark();

            btnStop = CreateRoundedButton("Arrêter", 590, 555, 180, 42, Color.Gray, Color.White);
            btnStop.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnStop.Enabled = false;
            btnStop.Click += (s, e) => { stopRequested = true; Log("[INFO] Annulation demandée par l'utilisateur..."); };

            btnRemove = CreateRoundedButton("Supprimer", 780, 555, 85, 42, AccentRed, Color.White);
            btnRemove.Click += (s, e) => RemoveSelectedFiles();

            btnClear = CreateRoundedButton("Vider", 875, 555, 85, 42, AccentRed, Color.White);
            btnClear.Click += (s, e) => ClearFiles();

            // Section Progression
            grpProg = CreateGroupBox("Encoding progress", 20, 608, 940, 85);
            progressBar = new RoundedProgressBar { Location = new Point(15, 25), Size = new Size(910, 18), Value = 0, ProgressColor = AccentPurple, CornerRadius = 6 };
            lblSpeedInfo = new Label { Text = "Encoding at 0.0kbits/s (0.0x) - 0s remaining", Location = new Point(15, 50), AutoSize = true, ForeColor = AccentPurple, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            lblStatus = new Label { Text = "Prêt", Location = new Point(650, 50), AutoSize = true };
            grpProg.Controls.AddRange(new Control[] { progressBar, lblSpeedInfo, lblStatus });

            // Journal
            txtLog = new RichTextBox
            {
                Location = new Point(20, 700),
                Size = new Size(940, 75),
                Font = new Font("Consolas", 8.5F),
                ReadOnly = true,
                BorderStyle = BorderStyle.None
            };

            panelMain.Controls.AddRange(new Control[] {
                txtSourceBar, btnBrowseSource, grpPreset, dragDropZone, gridFiles,
                btnStart, btnBench, btnStop, btnRemove, btnClear, grpProg, txtLog
            });

            this.Controls.Add(panelMain);
            this.Controls.Add(panelSidebar);

            RefreshGrid();
        }

        #region Embedded Logo Loader
        private void LoadEmbeddedLogo()
        {
            try
            {
                var asm = Assembly.GetExecutingAssembly();
                string? resName = asm.GetManifestResourceNames().FirstOrDefault(n => n.EndsWith("logo.png", StringComparison.OrdinalIgnoreCase));
                if (resName != null)
                {
                    using var stream = asm.GetManifestResourceStream(resName);
                    if (stream != null)
                    {
                        picLogo.Image = Image.FromStream(stream);
                        return;
                    }
                }
            }
            catch { }

            string logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logo.png");
            if (File.Exists(logoPath)) picLogo.Image = Image.FromFile(logoPath);
        }
        #endregion

        #region BENCH - Benchmark Machine Performance
        private async void RunBenchmark()
        {
            if (isConverting) return;

            isConverting = true;
            btnStart.Enabled = false;
            btnBench.Enabled = false;
            btnStop.Enabled = true;
            stopRequested = false;

			// Log("... Tu prends la pilule bleue, l'histoire s'arrête là, tu te réveilles dans ton lit et tu crois ce que tu veux croire. ");
			// Log(".... Choisis la pilule rouge et tu restes au pays des merveilles et on descend avec le lapin blanc au fond du gouffre.");
			// Log(" Sinon...> Les options : -y -f lavfi -i ");
			// log(" testsrc2=duration=60:size=7680x4320:rate=60 ");
			// log(" -an -c:v libx265 -preset veryslow -crf 18 -threads 0 -f null - ");		
            Log("==============================================================");
            Log("* LANCEMENT DU BENCHMARK H.264 (Odyssée Performance Test)    *");
            Log("==============================================================");
			

            lblStatus.Text = "Benchmark en cours...";
            progressBar.Value = 0;

            // string args = "-y -f lavfi -i testsrc=duration=10:size=1920x1080:rate=240 -c:v libx264 -preset medium -f null -";
			string args = "-y -f lavfi -i testsrc2=duration=60:size=7680x4320:rate=60 -an -c:v libx265 -preset veryslow -crf 18 -threads 0 -f null -";

            Stopwatch sw = Stopwatch.StartNew();
            int maxFps = 0;
            double maxSpeed = 0.0;

            await Task.Run(() =>
            {
                Process p = new Process();
                p.StartInfo.FileName = "ffmpeg";
                p.StartInfo.Arguments = args;
                p.StartInfo.UseShellExecute = false;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.CreateNoWindow = true;

                try { p.Start(); }
                catch (Exception ex)
                {
                    this.Invoke(() => Log($"[ERREUR BENCHMARK] FFmpeg introuvable : {ex.Message}"));
                    return;
                }

                StreamReader reader = p.StandardError;
                string? line;

                while ((line = reader.ReadLine()) != null)
                {
                    if (stopRequested)
                    {
                        try { p.Kill(); } catch { }
                        break;
                    }

                    if (line.Contains("fps=") && line.Contains("speed="))
                    {
                        Match mFps = Regex.Match(line, @"fps=\s*(\d+)");
                        Match mSpeed = Regex.Match(line, @"speed=\s*([\d\.]+)x");

                        if (mFps.Success && int.TryParse(mFps.Groups[1].Value, out int fps)) maxFps = Math.Max(maxFps, fps);
                        if (mSpeed.Success && double.TryParse(mSpeed.Groups[1].Value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double spd)) maxSpeed = Math.Max(maxSpeed, spd);

                        this.Invoke(() =>
                        {
                            progressBar.Value = Math.Min(100, (int)((sw.Elapsed.TotalSeconds / 10.0) * 100));
                            lblSpeedInfo.Text = $"Benchmark 1080p240: FPS = {maxFps} | Vitesse = {maxSpeed:F1}x";
                        });
                    }
                }
                p.WaitForExit();
            });

            sw.Stop();
            isConverting = false;
            btnStart.Enabled = true;
            btnBench.Enabled = true;
            btnStop.Enabled = false;

            if (!stopRequested)
            {
                progressBar.Value = 100;
                int score = (int)(maxFps * 10 + maxSpeed * 100);
                string rating = score > 1500 ? "EXCELLENT (Station de travail très puissante)" :
                                score > 800 ? "TRÈS BON (Enco rapide)" : "MOYEN (Processeur limité)";

                Log($"[RESULTAT BENCHMARK] Score Odyssée : {score} pts | FPS Max: {maxFps} | Vitesse Max: {maxSpeed:F1}x");
                Log($"[EVALUATION MACHINE] {rating}");

                MessageBox.Show($"Résultats du Benchmark :\n\nScore Odyssée : {score} Pts\nVitesse d'encodage 1080p240 : {maxSpeed:F1}x ({maxFps} FPS)\nÉvaluation : {rating}", "Résultat BENCH Odyssée", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        #endregion

        #region Application Intégrale des Thèmes (Sombre & Clair Keentheme)
        private void ApplyTheme()
        {
            if (isDarkMode)
            {
                // Thème Sombre Keentheme Good
                BgDarkSidebar = Color.FromArgb(18, 19, 26);
                BgDarkMain = Color.FromArgb(28, 29, 39);
                BgCard = Color.FromArgb(36, 37, 50);
                BgInput = Color.FromArgb(24, 25, 34);
                BorderColor = Color.FromArgb(50, 52, 70);
                TextPrimary = Color.FromArgb(240, 240, 245);
                TextMuted = Color.FromArgb(140, 144, 165);
                btnThemeToggle.Text = "Mode Clair";
            }
            else
            {
                // Thème Clair Keentheme Good
                BgDarkSidebar = Color.FromArgb(240, 243, 248);
                BgDarkMain = Color.FromArgb(248, 249, 252);
                BgCard = Color.White;
                BgInput = Color.FromArgb(243, 246, 249);
                BorderColor = Color.FromArgb(220, 224, 235);
                TextPrimary = Color.FromArgb(24, 28, 50);
                TextMuted = Color.FromArgb(120, 125, 145);
                btnThemeToggle.Text = "Mode Sombre";
            }

            // Fenêtres et Panneaux
            this.BackColor = BgDarkMain;
            panelSidebar.BackColor = BgDarkSidebar;
            panelMain.BackColor = BgDarkMain;

            // Panneau Latéral
            lblAppTitle.ForeColor = TextPrimary;
            lblProjectsHeader.ForeColor = TextMuted;
            lstSidebarFiles.BackColor = BgInput;
            lstSidebarFiles.ForeColor = TextPrimary;

            btnNavBench.BackColor = BgCard;
            btnNavBench.ForeColor = TextPrimary;
            btnThemeToggle.BackColor = BgInput;
            btnThemeToggle.ForeColor = TextMuted;

            // Entrées & Boutons du Panneau Principal
            txtSourceBar.BackColor = BgInput;
            txtSourceBar.ForeColor = TextPrimary;

            btnBrowseOutFolder.BackColor = BgCard;
            btnBrowseOutFolder.ForeColor = TextPrimary;

            if (!isConverting)
            {
                btnStop.BackColor = BgCard;
                btnStop.ForeColor = TextMuted;
            }

            // Conteneurs GroupBox (Presets & Progression)
            grpPreset.BackColor = BgCard;
            grpPreset.BorderColor = BorderColor;
            grpPreset.ForeColor = TextPrimary;

            grpProg.BackColor = BgCard;
            grpProg.BorderColor = BorderColor;
            grpProg.ForeColor = TextPrimary;

            // Libellés dans les GroupBoxes
            lblOut.ForeColor = TextMuted;
            lblFmt.ForeColor = TextMuted;
            lblCodec.ForeColor = TextMuted;
            lblRes.ForeColor = TextMuted;
            lblSpeed.ForeColor = TextMuted;
            lblExist.ForeColor = TextMuted;
            lblStatus.ForeColor = TextMuted;

            // Contrôles de saisie
            txtOutFolder.BackColor = BgInput;
            txtOutFolder.ForeColor = TextPrimary;

            cboCodec.BackColor = BgInput;
            cboCodec.ForeColor = TextPrimary;

            cboResolution.BackColor = BgInput;
            cboResolution.ForeColor = TextPrimary;

            cboSpeedPreset.BackColor = BgInput;
            cboSpeedPreset.ForeColor = TextPrimary;

            chkFastStart.ForeColor = TextPrimary;
            chkAAC.ForeColor = TextPrimary;
            chkMute.ForeColor = TextPrimary;

            rbMP4.ForeColor = TextPrimary;
            rbMKV.ForeColor = TextPrimary;
            rbAVI.ForeColor = TextPrimary;
            rbWebM.ForeColor = TextPrimary;

            rbExistOverwrite.ForeColor = TextPrimary;
            rbExistRename.ForeColor = TextPrimary;
            rbExistSkip.ForeColor = TextPrimary;
            rbExistAsk.ForeColor = TextPrimary;

            // Zone Glisser-Déposer
            dragDropZone.BackColor = BgInput;
            dragDropZone.BorderColor = AccentPurple;

            // --- STYLISATION DU TABLEAU (STYLE GOOD KEENTHEME) ---
            gridFiles.BackgroundColor = BgCard;
            gridFiles.GridColor = BorderColor;

            gridFiles.DefaultCellStyle.BackColor = BgCard;
            gridFiles.DefaultCellStyle.ForeColor = TextPrimary;
            gridFiles.DefaultCellStyle.SelectionBackColor = isDarkMode ? Color.FromArgb(50, 52, 75) : Color.FromArgb(235, 238, 250);
            gridFiles.DefaultCellStyle.SelectionForeColor = isDarkMode ? Color.White : AccentPurple;

            gridFiles.ColumnHeadersDefaultCellStyle.BackColor = isDarkMode ? Color.FromArgb(30, 31, 42) : Color.FromArgb(243, 246, 249);
            gridFiles.ColumnHeadersDefaultCellStyle.ForeColor = AccentPurple;
            gridFiles.ColumnHeadersDefaultCellStyle.SelectionBackColor = gridFiles.ColumnHeadersDefaultCellStyle.BackColor;

            // Barre de progression
            progressBar.BackColor = BgInput;
            progressBar.ProgressColor = AccentPurple;

            // RichTextBox de Log
            txtLog.BackColor = BgInput;
            txtLog.ForeColor = AccentGreen;

            this.Refresh();
        }
        #endregion

        #region Drag & Drop and File Queue
        private void DragDropZone_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void DragDropZone_DragDrop(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetData(DataFormats.FileDrop) is string[] files) AddFiles(files);
        }

        private void OpenFilesDialog()
        {
            using var dlg = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Fichiers Vidéo|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.webm;*.m4v;*.ts|Tous les fichiers|*.*"
            };
            if (dlg.ShowDialog() == DialogResult.OK) AddFiles(dlg.FileNames);
        }

        private void AddFiles(string[] files)
        {
            foreach (var f in files)
            {
                if (File.Exists(f) && !videoList.Any(v => v.FilePath.Equals(f, StringComparison.OrdinalIgnoreCase)))
                {
                    FileInfo fi = new FileInfo(f);
                    double sizeMb = fi.Length / (1024.0 * 1024.0);
                    videoList.Add(new VideoItem { FilePath = f, Size = $"{sizeMb:F1} Mo" });
                }
            }
            RefreshGrid();
        }

        private void RemoveSelectedFiles()
        {
            foreach (DataGridViewRow row in gridFiles.SelectedRows)
            {
                if (row.DataBoundItem is VideoItem item) videoList.Remove(item);
            }
            RefreshGrid();
        }

        private void ClearFiles()
        {
            videoList.Clear();
            RefreshGrid();
        }

        private void RefreshGrid()
        {
            gridFiles.DataSource = null;
            gridFiles.DataSource = videoList;

            lstSidebarFiles.Items.Clear();
            foreach (var item in videoList) lstSidebarFiles.Items.Add($" {item.FileName}");
        }
        #endregion

        #region Conversion Logic
        private async void BtnStart_Click(object? sender, EventArgs e)
        {
            if (isConverting) return;
            if (videoList.Count == 0)
            {
                MessageBox.Show("Veuillez ajouter au moins un fichier vidéo.", "Aucun fichier", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string outFolder = txtOutFolder.Text.Trim();
            if (string.IsNullOrWhiteSpace(outFolder))
            {
                MessageBox.Show("Veuillez choisir un dossier de sortie.", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!Directory.Exists(outFolder)) Directory.CreateDirectory(outFolder);

            string ext = ".mp4";
            if (rbMKV.Checked) ext = ".mkv";
            else if (rbAVI.Checked) ext = ".avi";
            else if (rbWebM.Checked) ext = ".webm";

            isConverting = true;
            stopRequested = false;
            btnStart.Enabled = false;
            btnBench.Enabled = false;
            btnStop.Enabled = true;

            int total = videoList.Count;
            int currentCount = 0;

            foreach (var item in videoList)
            {
                if (stopRequested) break;

                currentCount++;
                item.Status = "En cours...";
                RefreshGrid();

                string destPath = Path.Combine(outFolder, Path.GetFileNameWithoutExtension(item.FilePath) + ext);

                if (File.Exists(destPath))
                {
                    if (rbExistSkip.Checked)
                    {
                        item.Status = "Ignoré";
                        RefreshGrid();
                        continue;
                    }
                    else if (rbExistRename.Checked)
                    {
                        destPath = Path.Combine(outFolder, $"{Path.GetFileNameWithoutExtension(item.FilePath)}_{DateTime.Now:HHmmss}{ext}");
                    }
                    else if (rbExistAsk.Checked)
                    {
                        var res = MessageBox.Show($"Le fichier {Path.GetFileName(destPath)} existe déjà. Écraser ?", "Fichier existant", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                        if (res == DialogResult.No) { item.Status = "Ignoré"; RefreshGrid(); continue; }
                        else if (res == DialogResult.Cancel) break;
                    }
                }

                string args = $"-y -i \"{item.FilePath}\" ";

                if (cboCodec.SelectedIndex == 0) args += "-c:v libx264 ";
                else if (cboCodec.SelectedIndex == 1) args += "-c:v libx265 ";
                else if (cboCodec.SelectedIndex == 2) args += "-c:v libvpx-vp9 ";
                else if (cboCodec.SelectedIndex == 3) args += "-c:v copy ";

                if (cboCodec.SelectedIndex != 3) args += $"-preset {cboSpeedPreset.SelectedItem} ";

                string scaleParam = GetScaleParameter(cboResolution.SelectedIndex);
                if (!string.IsNullOrEmpty(scaleParam)) args += $"-vf {scaleParam} ";

                if (chkMute.Checked) args += "-an ";
                else if (chkAAC.Checked) args += "-c:a aac -b:a 192k ";

                if (chkFastStart.Checked && ext == ".mp4") args += "-movflags +faststart ";

                args += $"\"{destPath}\"";

                Log($"[DEBUT] Conversion : {item.FileName} ({currentCount}/{total})");
                bool success = await Task.Run(() => RunFFmpeg(args, item, currentCount, total));

                item.Status = success ? "Terminé" : (stopRequested ? "Annulé" : "Erreur");
                RefreshGrid();
            }

            isConverting = false;
            btnStart.Enabled = true;
            btnBench.Enabled = true;
            btnStop.Enabled = false;

            lblStatus.Text = stopRequested ? "Conversion interrompue." : "Toutes les conversions sont terminées !";
            progressBar.Value = 100;
        }

        private string GetScaleParameter(int index)
        {
            return index switch
            {
                1 => "scale=-2:160",   // 160p
                2 => "scale=-2:240",   // 240p
                3 => "scale=-2:360",   // 360p
                4 => "scale=-2:480",   // 480p
                5 => "scale=-2:720",   // 720p
                6 => "scale=-2:1080",  // 1080p
                7 => "scale=-2:1440",  // 1440p
                8 => "scale=-2:2160",  // 2160p
                _ => ""                // Originale
            };
        }

        private bool RunFFmpeg(string arguments, VideoItem item, int index, int total)
        {
            Process process = new Process();
            process.StartInfo.FileName = "ffmpeg";
            process.StartInfo.Arguments = arguments;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            try { process.Start(); }
            catch (Exception ex)
            {
                Log($"[ERREUR] FFmpeg non trouvé : {ex.Message}");
                return false;
            }

            StreamReader reader = process.StandardError;
            string? line;
            TimeSpan totalDuration = TimeSpan.Zero;

            while ((line = reader.ReadLine()) != null)
            {
                if (stopRequested)
                {
                    try { process.Kill(); } catch { }
                    return false;
                }

                if (totalDuration == TimeSpan.Zero && line.Contains("Duration:"))
                {
                    Match m = Regex.Match(line, @"Duration:\s(\d\d):(\d\d):(\d\d\.\d\d)");
                    if (m.Success)
                    {
                        totalDuration = new TimeSpan(0, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), (int)double.Parse(m.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture));
                        item.Duration = $"{totalDuration.Minutes:D2}:{totalDuration.Seconds:D2}";
                        this.Invoke(RefreshGrid);
                    }
                }

                if (line.Contains("time=") && totalDuration.TotalSeconds > 0)
                {
                    Match m = Regex.Match(line, @"time=(\d\d):(\d\d):(\d\d\.\d\d)");
                    Match mBitrate = Regex.Match(line, @"bitrate=\s*([\d\.]+)kbits/s");
                    Match mSpeed = Regex.Match(line, @"speed=\s*([\d\.]+)x");

                    if (m.Success)
                    {
                        TimeSpan current = new TimeSpan(0, int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value), (int)double.Parse(m.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture));
                        double pct = (current.TotalSeconds / totalDuration.TotalSeconds) * 100;
                        int val = Math.Clamp((int)pct, 0, 100);

                        string bitRateStr = mBitrate.Success ? mBitrate.Groups[1].Value : "1500";
                        string speedStr = mSpeed.Success ? mSpeed.Groups[1].Value : "1.0";
                        double remainingSec = totalDuration.TotalSeconds - current.TotalSeconds;

                        this.Invoke(() =>
                        {
                            progressBar.Value = val;
                            lblSpeedInfo.Text = $"Encoding at {bitRateStr}kbits/s ({speedStr}x) - {(int)remainingSec}s remaining";
                            lblStatus.Text = $"Fichier {index}/{total} ({val}%)";
                        });
                    }
                }
            }

            process.WaitForExit();
            return process.ExitCode == 0;
        }
        #endregion

        #region UI Creation Helpers
        private RoundedGroupBox CreateGroupBox(string title, int x, int y, int w, int h)
        {
            return new RoundedGroupBox { Text = title, Location = new Point(x, y), Size = new Size(w, h) };
        }

        private RoundedButton CreateRoundedButton(string text, int x, int y, int w, int h, Color bg, Color fg)
        {
            return new RoundedButton { Text = text, Location = new Point(x, y), Size = new Size(w, h), BackColor = bg, ForeColor = fg, CornerRadius = 8 };
        }

        private CheckBox CreateCheckBox(string text, int x, int y)
        {
            return new CheckBox { Text = text, Location = new Point(x, y), AutoSize = true };
        }

        private RadioButton CreateRadioButton(string text, int x, int y, bool isChecked)
        {
            return new RadioButton { Text = text, Location = new Point(x, y), AutoSize = true, Checked = isChecked };
        }

        private void Log(string msg)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => Log(msg)));
                return;
            }
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}" + Environment.NewLine);
            txtLog.SelectionStart = txtLog.Text.Length;
            txtLog.ScrollToCaret();
        }
        #endregion
    }

    #region Custom UI Controls
    public class RoundedPanel : Panel
    {
        public int CornerRadius { get; set; } = 12;
        public Color BorderColor { get; set; } = Color.FromArgb(50, 52, 70);

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using GraphicsPath path = GetRoundedPath(rect, CornerRadius);
            using SolidBrush bgBrush = new SolidBrush(BackColor);
            using Pen borderPen = new Pen(BorderColor, 1.5f);

            e.Graphics.FillPath(bgBrush, path);
            e.Graphics.DrawPath(borderPen, path);
        }

        public static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int diameter = radius * 2;
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class RoundedGroupBox : GroupBox
    {
        public int CornerRadius { get; set; } = 12;
        public Color BorderColor { get; set; } = Color.FromArgb(50, 52, 70);

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 10, Width - 1, Height - 11);
            using GraphicsPath path = RoundedPanel.GetRoundedPath(rect, CornerRadius);

            using SolidBrush bgBrush = new SolidBrush(BackColor);
            using Pen borderPen = new Pen(BorderColor, 1.5f);
            using SolidBrush textBrush = new SolidBrush(ForeColor);

            e.Graphics.FillPath(bgBrush, path);
            e.Graphics.DrawPath(borderPen, path);
            e.Graphics.DrawString(Text, Font, textBrush, new PointF(15, 0));
        }
    }

    public class RoundedButton : Button
    {
        public int CornerRadius { get; set; } = 8;

        public RoundedButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rect = new Rectangle(0, 0, Width, Height);
            using GraphicsPath path = RoundedPanel.GetRoundedPath(rect, CornerRadius);

            this.Region = new Region(path);

            using SolidBrush bgBrush = new SolidBrush(BackColor);
            using SolidBrush textBrush = new SolidBrush(ForeColor);

            e.Graphics.FillPath(bgBrush, path);

            SizeF textSize = e.Graphics.MeasureString(Text, Font);
            PointF textPos = new PointF((Width - textSize.Width) / 2, (Height - textSize.Height) / 2);
            e.Graphics.DrawString(Text, Font, textBrush, textPos);
        }
    }

    public class RoundedProgressBar : Control
    {
        private int _value = 0;
        public int Value
        {
            get => _value;
            set { _value = Math.Clamp(value, 0, 100); Invalidate(); }
        }

        public Color ProgressColor { get; set; } = Color.FromArgb(99, 91, 255);
        public int CornerRadius { get; set; } = 8;

        public RoundedProgressBar()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using GraphicsPath bgPath = RoundedPanel.GetRoundedPath(rect, CornerRadius);
            using SolidBrush bgBrush = new SolidBrush(BackColor);
            e.Graphics.FillPath(bgBrush, bgPath);

            if (_value > 0)
            {
                int fillWidth = (int)((Width - 1) * (_value / 100.0));
                if (fillWidth > CornerRadius)
                {
                    Rectangle fillRect = new Rectangle(0, 0, fillWidth, Height - 1);
                    using GraphicsPath fillPath = RoundedPanel.GetRoundedPath(fillRect, CornerRadius);
                    using SolidBrush fillBrush = new SolidBrush(ProgressColor);
                    e.Graphics.FillPath(fillBrush, fillPath);
                }
            }
        }
    }
    #endregion
}