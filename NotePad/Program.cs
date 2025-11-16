using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace NotePad
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new NotepadForm());
        }
    }

    public class NotepadForm : Form
    {
        private MenuStrip menu;
        private ToolStripMenuItem fileMenu, editMenu, formatMenu, viewMenu, helpMenu;
        private ToolStripMenuItem newItem, openItem, saveItem, saveAsItem, exitItem;
        private ToolStripMenuItem undoItem, cutItem, copyItem, pasteItem, selectAllItem;
        private ToolStripMenuItem wordWrapItem, fontItem;
        private ToolStripMenuItem statusBarItem;
        private ToolStripMenuItem aboutItem;

        private TextBox txt;
        private StatusStrip status;
        private ToolStripStatusLabel statusLabel;
        private OpenFileDialog ofd;
        private SaveFileDialog sfd;
        private FontDialog fontDlg;

        private string currentPath = null;
        private bool isDirty = false;
        private bool statusVisible = true;

        public NotepadForm()
        {
            InitializeComponents();
            UpdateTitle();
        }

        private void InitializeComponents()
        {
            this.Text = "Not Defteri";
            this.Size = new Size(900, 600);

            menu = new MenuStrip();

            fileMenu = new ToolStripMenuItem("Dosya");
            editMenu = new ToolStripMenuItem("Düzen");
            formatMenu = new ToolStripMenuItem("Biçim");
            viewMenu = new ToolStripMenuItem("Görünüm");
            helpMenu = new ToolStripMenuItem("Yardım");

            newItem = new ToolStripMenuItem("Yeni", null, NewFile) { ShortcutKeys = Keys.Control | Keys.N };
            openItem = new ToolStripMenuItem("Aç...", null, OpenFile) { ShortcutKeys = Keys.Control | Keys.O };
            saveItem = new ToolStripMenuItem("Kaydet", null, SaveFile) { ShortcutKeys = Keys.Control | Keys.S };
            saveAsItem = new ToolStripMenuItem("Farklı Kaydet...", null, SaveAsFile);
            exitItem = new ToolStripMenuItem("Çıkış", null, ExitApp);

            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { newItem, openItem, saveItem, saveAsItem, new ToolStripSeparator(), exitItem });

            // Edit kısmını hocaya yollamadan gözden geçirmeliyim !!!
            undoItem = new ToolStripMenuItem("Geri Al", null, (s, e) => { if (txt.CanUndo) txt.Undo(); }) { ShortcutKeys = Keys.Control | Keys.Z };
            cutItem = new ToolStripMenuItem("Kes", null, (s, e) => txt.Cut()) { ShortcutKeys = Keys.Control | Keys.X };
            copyItem = new ToolStripMenuItem("Kopyala", null, (s, e) => txt.Copy()) { ShortcutKeys = Keys.Control | Keys.C };
            pasteItem = new ToolStripMenuItem("Yapıştır", null, (s, e) => txt.Paste()) { ShortcutKeys = Keys.Control | Keys.V };
            selectAllItem = new ToolStripMenuItem("Tümünü Seç", null, (s, e) => txt.SelectAll()) { ShortcutKeys = Keys.Control | Keys.A };

            editMenu.DropDownItems.AddRange(new ToolStripItem[] { undoItem, new ToolStripSeparator(), cutItem, copyItem, pasteItem, new ToolStripSeparator(), selectAllItem });

            wordWrapItem = new ToolStripMenuItem("Satır Kaydır", null, ToggleWordWrap) { CheckOnClick = true, Checked = true };
            fontItem = new ToolStripMenuItem("Yazı Tipi...", null, ShowFontDialog);

            formatMenu.DropDownItems.AddRange(new ToolStripItem[] { wordWrapItem, fontItem });

            statusBarItem = new ToolStripMenuItem("Durum Çubuğu", null, ToggleStatusBar) { CheckOnClick = true, Checked = true };
            viewMenu.DropDownItems.Add(statusBarItem);

            aboutItem = new ToolStripMenuItem("Hakkında", null, (s, e) => MessageBox.Show("Basit Not Defteri\nYapım: ChatGPT örneği", "Hakkında", MessageBoxButtons.OK, MessageBoxIcon.Information));
            helpMenu.DropDownItems.Add(aboutItem);

            menu.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, formatMenu, viewMenu, helpMenu });
            this.MainMenuStrip = menu;
            this.Controls.Add(menu);

            txt = new TextBox()
            {
                Multiline = true,
                Dock = DockStyle.Fill,
                AcceptsTab = true,
                AcceptsReturn = true,
                ScrollBars = ScrollBars.Both,
                Font = new Font("Consolas", 11),
                WordWrap = true
            };
            txt.TextChanged += (s, e) => { isDirty = true; UpdateStatus(); UpdateTitle(); };
            txt.SelectionChanged += (s, e) => UpdateStatus();
            txt.KeyDown += Txt_KeyDown;
            this.Controls.Add(txt);

            status = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Satır 1, Kolon 1");
            status.Items.Add(statusLabel);
            this.Controls.Add(status);

            ofd = new OpenFileDialog() { Filter = "Metin Dosyaları (*.txt)|*.txt|Tüm Dosyalar|*.*" };
            sfd = new SaveFileDialog() { Filter = "Metin Dosyaları (*.txt)|*.txt|Tüm Dosyalar|*.*" };
            fontDlg = new FontDialog();

            this.FormClosing += NotepadForm_FormClosing;
        }

        private void Txt_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.F)
            {
                ShowFindDialog();
                e.SuppressKeyPress = true;
            }
        }

        private void ToggleWordWrap(object sender, EventArgs e)
        {
            txt.WordWrap = wordWrapItem.Checked;
            txt.ScrollBars = txt.WordWrap ? ScrollBars.Vertical : ScrollBars.Both;
        }

        private void ShowFontDialog(object sender, EventArgs e)
        {
            fontDlg.Font = txt.Font;
            if (fontDlg.ShowDialog() == DialogResult.OK)
            {
                txt.Font = fontDlg.Font;
            }
        }

        private void ToggleStatusBar(object sender, EventArgs e)
        {
            statusVisible = statusBarItem.Checked;
            status.Visible = statusVisible;
        }

        private void NewFile(object sender, EventArgs e)
        {
            if (!ConfirmSaveIfNeeded()) return;
            txt.Clear();
            currentPath = null;
            isDirty = false;
            UpdateTitle();
        }

        private void OpenFile(object sender, EventArgs e)
        {
            if (!ConfirmSaveIfNeeded()) return;
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    txt.Text = File.ReadAllText(ofd.FileName, Encoding.UTF8);
                    currentPath = ofd.FileName;
                    isDirty = false;
                    UpdateTitle();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Dosya açılamadı: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void SaveFile(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(currentPath))
            {
                SaveAsFile(sender, e);
                return;
            }

            try
            {
                File.WriteAllText(currentPath, txt.Text, Encoding.UTF8);
                isDirty = false;
                UpdateTitle();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kaydedilemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveAsFile(object sender, EventArgs e)
        {
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    File.WriteAllText(sfd.FileName, txt.Text, Encoding.UTF8);
                    currentPath = sfd.FileName;
                    isDirty = false;
                    UpdateTitle();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Kaydedilemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void ExitApp(object sender, EventArgs e)
        {
            this.Close();
        }

        private void NotepadForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!ConfirmSaveIfNeeded())
            {
                e.Cancel = true;
            }
        }

        private bool ConfirmSaveIfNeeded()
        {
            if (!isDirty) return true;

            var dr = MessageBox.Show("Değişiklikler kaydedilmedi. Kaydetmek istiyor musunuz?", "Kaydet", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
            if (dr == DialogResult.Yes)
            {
                if (string.IsNullOrEmpty(currentPath))
                {
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        try
                        {
                            File.WriteAllText(sfd.FileName, txt.Text, Encoding.UTF8);
                            currentPath = sfd.FileName;
                            isDirty = false;
                            UpdateTitle();
                            return true;
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Kaydedilemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                    }
                    else return false;
                }
                else
                {
                    try
                    {
                        File.WriteAllText(currentPath, txt.Text, Encoding.UTF8);
                        isDirty = false;
                        UpdateTitle();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Kaydedilemedi: " + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }
                }
            }
            else if (dr == DialogResult.No)
            {
                return true;
            }
            else 
            {
                return false;
            }
        }

        private void UpdateTitle()
        {
            string name = currentPath == null ? "Adsız" : Path.GetFileName(currentPath);
            this.Text = $"{name}{(isDirty ? "*" : "")} - Not Defteri";
        }

        private void UpdateStatus()
        {
            if (!statusVisible) return;

            int index = txt.SelectionStart;
            int line = txt.GetLineFromCharIndex(index);
            int col = index - txt.GetFirstCharIndexFromLine(line);
            statusLabel.Text = $"Satır {line + 1}, Kolon {col + 1}";
        }

        private void ShowFindDialog()
        {
            using (Form f = new Form())
            {
                f.Text = "Bul";
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.StartPosition = FormStartPosition.CenterParent;
                f.ClientSize = new Size(360, 90);
                f.MaximizeBox = false;
                f.MinimizeBox = false;
                f.ShowIcon = false;

                Label lbl = new Label() { Text = "Ara:", Location = new Point(8, 12), AutoSize = true };
                TextBox tb = new TextBox() { Location = new Point(50, 10), Width = 290 };
                CheckBox matchCase = new CheckBox() { Text = "Büyük/küçük harf duyarlı", Location = new Point(8, 40), AutoSize = true };
                Button findNext = new Button() { Text = "Bul", Location = new Point(250, 40), Width = 90, DialogResult = DialogResult.None };

                findNext.Click += (s, e) =>
                {
                    string search = tb.Text;
                    if (string.IsNullOrEmpty(search)) return;

                    StringComparison cmp = matchCase.Checked ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
                    int start = txt.SelectionStart + txt.SelectionLength;
                    int pos = txt.Text.IndexOf(search, start, cmp);
                    if (pos < 0)
                    {
                        pos = txt.Text.IndexOf(search, 0, cmp);
                    }
                    if (pos >= 0)
                    {
                        txt.Select(pos, search.Length);
                        txt.ScrollToCaret();
                        txt.Focus();
                    }
                    else
                    {
                        MessageBox.Show("Bulunamadı.", "Sonuç", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                };

                f.Controls.AddRange(new Control[] { lbl, tb, matchCase, findNext });
                f.ShowDialog(this);
            }
        }
    }
}