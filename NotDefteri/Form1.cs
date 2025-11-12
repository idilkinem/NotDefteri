using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace NotDefteri
{
    public partial class Form1 : Form
    {
        // UI
        private MenuStrip menu;
        private StatusStrip status;
        private ToolStripStatusLabel statusCaret;
        private TextBox editor;
        private OpenFileDialog ofd;
        private SaveFileDialog sfd;
        private FontDialog fdlg;

        // State
        private string currentPath = null;
        private bool isDirty = false;

        public Form1()
        {
            InitializeComponent(); // Designer kalsın
            BuildUI();             // Arayüzü çalışma anında kur
            WireEvents();          // Olay bağla
            UpdateCaret();
        }

        // ---------------- UI Build ----------------
        private void BuildUI()
        {
            Text = "Adsız - Not Defteri";
            Width = 1000;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;

            // Menü
            menu = new MenuStrip();
            BuildMenu();

            // Status bar
            status = new StatusStrip();
            statusCaret = new ToolStripStatusLabel("Satır 1, Sütun 1");
            status.Items.Add(statusCaret);

            // Editör
            editor = new TextBox
            {
                Multiline = true,
                AcceptsReturn = true,
                AcceptsTab = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Dock = DockStyle.Fill,
                Font = new Font("Consolas", 11f),
                HideSelection = false
            };

            // Dialoglar
            ofd = new OpenFileDialog
            {
                Filter = "Metin Dosyaları (*.txt)|*.txt|Tüm Dosyalar (*.*)|*.*",
                Title = "Dosya Aç"
            };
            sfd = new SaveFileDialog
            {
                Filter = "Metin Dosyaları (*.txt)|*.txt|Tüm Dosyalar (*.*)|*.*",
                Title = "Farklı Kaydet"
            };
            fdlg = new FontDialog();

            // Yerleşim
            Controls.Add(editor);
            Controls.Add(status);
            Controls.Add(menu);
            MainMenuStrip = menu;

            // Sürükle-bırak
            AllowDrop = true;
        }

        private void BuildMenu()
        {
            // Dosya
            var mFile = new ToolStripMenuItem("&Dosya");
            var miNew = new ToolStripMenuItem("&Yeni", null, (s, e) => NewFile()) { ShortcutKeys = Keys.Control | Keys.N };
            var miOpen = new ToolStripMenuItem("&Aç...", null, (s, e) => OpenFile()) { ShortcutKeys = Keys.Control | Keys.O };
            var miSave = new ToolStripMenuItem("&Kaydet", null, (s, e) => SaveFile()) { ShortcutKeys = Keys.Control | Keys.S };
            var miSaveAs = new ToolStripMenuItem("Farklı &Kaydet...", null, (s, e) => SaveFileAs());
            var miExit = new ToolStripMenuItem("Çı&kış", null, (s, e) => Close());
            mFile.DropDownItems.AddRange(new ToolStripItem[] {
                miNew, miOpen, new ToolStripSeparator(), miSave, miSaveAs, new ToolStripSeparator(), miExit
            });

            // Düzen
            var mEdit = new ToolStripMenuItem("&Düzen");
            var miUndo = new ToolStripMenuItem("Geri &Al", null, (s, e) => { if (editor.CanUndo) editor.Undo(); }) { ShortcutKeys = Keys.Control | Keys.Z };
            var miRedo = new ToolStripMenuItem("&İleri Al", null, (s, e) => TextBoxWin32.Redo(editor)) { ShortcutKeys = Keys.Control | Keys.Y };
            var miCut = new ToolStripMenuItem("&Kes", null, (s, e) => editor.Cut()) { ShortcutKeys = Keys.Control | Keys.X };
            var miCopy = new ToolStripMenuItem("K&opyala", null, (s, e) => editor.Copy()) { ShortcutKeys = Keys.Control | Keys.C };
            var miPaste = new ToolStripMenuItem("&Yapıştır", null, (s, e) => editor.Paste()) { ShortcutKeys = Keys.Control | Keys.V };
            var miSelectAll = new ToolStripMenuItem("&Tümünü Seç", null, (s, e) => editor.SelectAll()) { ShortcutKeys = Keys.Control | Keys.A };
            var miFind = new ToolStripMenuItem("&Bul...", null, (s, e) => FindText()) { ShortcutKeys = Keys.Control | Keys.F };
            var miReplace = new ToolStripMenuItem("&Değiştir...", null, (s, e) => ReplaceText()) { ShortcutKeys = Keys.Control | Keys.H };
            mEdit.DropDownItems.AddRange(new ToolStripItem[] {
                miUndo, miRedo, new ToolStripSeparator(),
                miCut, miCopy, miPaste, new ToolStripSeparator(),
                miSelectAll, new ToolStripSeparator(),
                miFind, miReplace
            });

            // Biçim
            var mFormat = new ToolStripMenuItem("&Biçim");
            var miWrap = new ToolStripMenuItem("&Kelime Kaydırma", null, (s, e) => ToggleWrap()) { Checked = false };
            var miFont = new ToolStripMenuItem("&Yazı Tipi...", null, (s, e) => ChangeFont());
            mFormat.DropDownItems.AddRange(new ToolStripItem[] { miWrap, miFont });

            // Yardım
            var mHelp = new ToolStripMenuItem("&Yardım");
            var miAbout = new ToolStripMenuItem("Hakkında", null,
                (s, e) => MessageBox.Show("Basit Not Defteri\nC# WinForms", "Hakkında",
                                        MessageBoxButtons.OK, MessageBoxIcon.Information));
            mHelp.DropDownItems.Add(miAbout);

            menu.Items.AddRange(new ToolStripItem[] { mFile, mEdit, mFormat, mHelp });
        }

        private void WireEvents()
        {
            editor.TextChanged += (s, e) => { isDirty = true; UpdateTitle(); UpdateCaret(); };
            editor.KeyUp += (s, e) => UpdateCaret();
            editor.MouseUp += (s, e) => UpdateCaret();

            FormClosing += Form1_FormClosing;

            DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
            };
            DragDrop += (s, e) =>
            {
                var files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length > 0) TryOpen(files[0]);
            };
        }

        // ---------------- File Ops ----------------
        private void NewFile()
        {
            if (!ConfirmDiscard()) return;
            editor.Clear();
            currentPath = null;
            isDirty = false;
            UpdateTitle();
        }

        private void OpenFile()
        {
            if (!ConfirmDiscard()) return;
            if (ofd.ShowDialog(this) == DialogResult.OK)
                TryOpen(ofd.FileName);
        }

        private void TryOpen(string path)
        {
            try
            {
                string text;
                using (var sr = new StreamReader(path, new UTF8Encoding(true), true))
                    text = sr.ReadToEnd();

                editor.Text = text;
                currentPath = path;
                isDirty = false;
                UpdateTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Açılırken hata: {ex.Message}", "Hata",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveFile()
        {
            if (currentPath == null) { SaveFileAs(); return; }
            TrySave(currentPath);
        }

        private void SaveFileAs()
        {
            if (currentPath != null) sfd.FileName = Path.GetFileName(currentPath);
            if (sfd.ShowDialog(this) == DialogResult.OK)
                TrySave(sfd.FileName);
        }

        private void TrySave(string path)
        {
            try
            {
                using (var sw = new StreamWriter(path, false, new UTF8Encoding(true)))
                    sw.Write(editor.Text);

                currentPath = path;
                isDirty = false;
                UpdateTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kaydedilirken hata: {ex.Message}", "Hata",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool ConfirmDiscard()
        {
            if (!isDirty) return true;
            var res = MessageBox.Show("Değişiklikler kaydedilsin mi?", "Onay",
                                      MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (res == DialogResult.Cancel) return false;
            if (res == DialogResult.Yes) { SaveFile(); return !isDirty; }
            return true; // Hayır
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ConfirmDiscard()) e.Cancel = true;
        }

        // ---------------- Edit / Format ----------------
        private void FindText()
        {
            string input = PromptInput("Bul:", "Bul");
            if (string.IsNullOrEmpty(input)) return;

            int start = editor.SelectionStart + editor.SelectionLength;
            int idx = editor.Text.IndexOf(input, start, StringComparison.CurrentCultureIgnoreCase);
            if (idx < 0 && start > 0)
                idx = editor.Text.IndexOf(input, 0, StringComparison.CurrentCultureIgnoreCase);

            if (idx >= 0)
            {
                editor.Select(idx, input.Length);
                editor.ScrollToCaret();
                editor.Focus();
            }
            else
            {
                MessageBox.Show("Metin bulunamadı.", "Bul",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ReplaceText()
        {
            string find = PromptInput("Bul:", "Değiştir");
            if (string.IsNullOrEmpty(find)) return;
            string repl = PromptInput("Bununla değiştir:", "Değiştir");

            int count = 0;
            int idx = editor.Text.IndexOf(find, 0, StringComparison.CurrentCultureIgnoreCase);
            while (idx >= 0)
            {
                editor.Select(idx, find.Length);
                editor.SelectedText = repl;
                count++;
                idx = editor.Text.IndexOf(find, idx + repl.Length, StringComparison.CurrentCultureIgnoreCase);
            }
            if (count == 0)
                MessageBox.Show("Eşleşme yok.", "Değiştir",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ToggleWrap()
        {
            editor.WordWrap = !editor.WordWrap;
            editor.ScrollBars = editor.WordWrap ? ScrollBars.Vertical : ScrollBars.Both;

            // Menü tikini güncelle
            var formatMenu = (ToolStripMenuItem)menu.Items[2];
            var wrapItem = (ToolStripMenuItem)formatMenu.DropDownItems[0];
            wrapItem.Checked = editor.WordWrap;
        }

        private void ChangeFont()
        {
            fdlg.Font = editor.Font;
            if (fdlg.ShowDialog(this) == DialogResult.OK)
                editor.Font = fdlg.Font;
        }

        // ---------------- UI helpers ----------------
        private void UpdateTitle()
        {
            string name = currentPath == null ? "Adsız" : Path.GetFileName(currentPath);
            Text = (isDirty ? "*" : "") + name + " - Not Defteri";
        }

        private void UpdateCaret()
        {
            int index = editor.SelectionStart;
            int line = editor.GetLineFromCharIndex(index);
            int firstIndex = editor.GetFirstCharIndexOfCurrentLine();
            int column = index - firstIndex;
            statusCaret.Text = $"Satır {line + 1}, Sütun {column + 1}";
        }

        private static string PromptInput(string text, string caption)
            => PromptBox.Show(text, caption);
    }

    // ------- Mini yardımcı sınıflar --------
    internal static class TextBoxWin32
    {
        private const int EM_REDO = 0x0454;

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public static void Redo(TextBox tb)
        {
            if (tb == null || tb.IsDisposed) return;
            SendMessage(tb.Handle, EM_REDO, IntPtr.Zero, IntPtr.Zero);
        }
    }

    internal static class PromptBox
    {
        public static string Show(string text, string caption)
        {
            var form = new Form()
            {
                Width = 420,
                Height = 160,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false
            };
            var lbl = new Label() { Left = 12, Top = 12, Width = 380, Text = text };
            var tb = new TextBox() { Left = 12, Top = 40, Width = 380 };
            var ok = new Button() { Text = "Tamam", Left = 220, Width = 80, Top = 80, DialogResult = DialogResult.OK };
            var cancel = new Button() { Text = "İptal", Left = 312, Width = 80, Top = 80, DialogResult = DialogResult.Cancel };
            ok.Anchor = cancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            form.Controls.AddRange(new Control[] { lbl, tb, ok, cancel });
            form.AcceptButton = ok;
            form.CancelButton = cancel;

            return form.ShowDialog() == DialogResult.OK ? tb.Text : null;
        }
    }
}
