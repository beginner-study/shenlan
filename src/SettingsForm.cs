using System;
using System.Drawing;
using System.Windows.Forms;

namespace DeepBlue
{
    public class SettingsForm : Form
    {
        private Store _store;
        private ComboBox _cboVoice;
        private TrackBar _trkRate;
        private Label _lblRateVal;
        private NumericUpDown _numDays;
        private CheckBox _chkDate;
        private CheckBox _chkToday;
        private CheckBox _chkDue;

        public SettingsForm(Store store)
        {
            _store = store;
            Text = AppInfo.Name + " · 设置";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            Size = new Size(620, 700);
            MinimumSize = new Size(580, 660);
            Font = Ui.F(9F);
            BackColor = Ui.Bg;
            Icon = MainForm.LoadIcon();

            BuildUi();
        }

        private Panel AddCard(int y, int height)
        {
            Panel p = new Panel();
            p.Location = new Point(20, y);
            p.Width = ClientSize.Width - 40;
            p.Height = height;
            p.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            p.BackColor = Ui.Card;
            p.Paint += delegate (object s, PaintEventArgs e) { Ui.PaintCardBorder(s, e, false); };
            Controls.Add(p);
            p.Resize += delegate
            {
                p.Width = ClientSize.Width - 40;
            };
            return p;
        }

        private void BuildUi()
        {
            Panel c1 = AddCard(16, 112);
            Label t1 = Ui.CardTitle("语音与语速");
            t1.Location = new Point(20, 14);
            c1.Controls.Add(t1);

            Label lv = Ui.FieldLabel("语音");
            lv.Location = new Point(20, 52);
            c1.Controls.Add(lv);

            _cboVoice = new ComboBox();
            _cboVoice.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboVoice.Location = new Point(68, 48);
            _cboVoice.Width = 300;
            _cboVoice.Font = Ui.F(9F);
            foreach (string name in BroadcastEngine.ListVoiceNames())
            {
                _cboVoice.Items.Add(name);
            }
            if (_cboVoice.Items.Count == 0) _cboVoice.Items.Add("（未检测到系统语音）");
            int selIdx = -1;
            for (int i = 0; i < _cboVoice.Items.Count; i++)
            {
                if ((string)_cboVoice.Items[i] == _store.Settings.VoiceName) { selIdx = i; break; }
            }
            if (selIdx < 0 && _cboVoice.Items.Count > 0) selIdx = 0;
            _cboVoice.SelectedIndex = selIdx;
            c1.Controls.Add(_cboVoice);

            Button preview = Ui.GhostButton("试听", 72, 30);
            preview.Location = new Point(380, 48);
            preview.Click += delegate { PreviewVoice(); };
            c1.Controls.Add(preview);

            Label lr = Ui.FieldLabel("语速");
            lr.Location = new Point(20, 88);
            c1.Controls.Add(lr);

            _trkRate = new TrackBar();
            _trkRate.Minimum = 5;
            _trkRate.Maximum = 20;
            _trkRate.TickFrequency = 1;
            _trkRate.SmallChange = 1;
            _trkRate.LargeChange = 1;
            int rv = (int)Math.Round(_store.Settings.Rate * 10);
            if (rv < 5) rv = 5;
            if (rv > 20) rv = 20;
            _trkRate.Value = rv;
            _trkRate.Width = 300;
            _trkRate.Height = 40;
            _trkRate.Location = new Point(68, 76);
            _trkRate.Scroll += delegate { UpdateRateLabel(); };
            c1.Controls.Add(_trkRate);

            _lblRateVal = new Label();
            _lblRateVal.Font = Ui.F(9F, FontStyle.Bold);
            _lblRateVal.ForeColor = Ui.Accent;
            _lblRateVal.AutoSize = true;
            _lblRateVal.Location = new Point(380, 84);
            c1.Controls.Add(_lblRateVal);
            UpdateRateLabel();

            Panel c2 = AddCard(140, 76);
            Label t2 = Ui.CardTitle("提醒窗口天数");
            t2.Location = new Point(20, 14);
            c2.Controls.Add(t2);

            _numDays = new NumericUpDown();
            _numDays.Minimum = 1;
            _numDays.Maximum = 30;
            _numDays.Value = _store.Settings.WindowDays;
            _numDays.Font = Ui.F(9.5F);
            _numDays.Width = 64;
            _numDays.Location = new Point(68, 44);
            c2.Controls.Add(_numDays);

            Label ld = new Label();
            ld.Text = "天内截止的 P0 / P1 事项将被播报（1–30）";
            ld.Font = Ui.F(8.5F);
            ld.ForeColor = Ui.Muted;
            ld.AutoSize = true;
            ld.Location = new Point(142, 48);
            c2.Controls.Add(ld);

            Panel c3 = AddCard(228, 128);
            Label t3 = Ui.CardTitle("播报段落开关");
            t3.Location = new Point(20, 14);
            c3.Controls.Add(t3);

            _chkDate = MakeToggle("日期与星期", _store.Settings.SecDate);
            _chkDate.Location = new Point(20, 48);
            c3.Controls.Add(_chkDate);

            CheckBox weather = MakeToggle("天气（后续版本启用）", true);
            weather.Enabled = false;
            weather.Location = new Point(20, 78);
            c3.Controls.Add(weather);

            _chkToday = MakeToggle("今日安排", _store.Settings.SecToday);
            _chkToday.Location = new Point(230, 48);
            c3.Controls.Add(_chkToday);

            _chkDue = MakeToggle("即将截止提醒", _store.Settings.SecDue);
            _chkDue.Location = new Point(230, 78);
            c3.Controls.Add(_chkDue);

            Panel c4 = AddCard(368, 84);
            c4.Enabled = true;
            Label t4 = Ui.CardTitle("天气城市");
            t4.Location = new Point(20, 14);
            c4.Controls.Add(t4);

            Label tag = new Label();
            tag.Text = "后续版本启用";
            tag.Font = Ui.F(8F);
            tag.ForeColor = Ui.Accent;
            tag.BackColor = Ui.AccentSoft;
            tag.AutoSize = false;
            tag.Width = 88;
            tag.Height = 22;
            tag.Location = new Point(110, 12);
            tag.TextAlign = ContentAlignment.MiddleCenter;
            c4.Controls.Add(tag);

            Label lw = new Label();
            lw.Text = "天气模块上线后启用，当前不可操作";
            lw.Font = Ui.F(8.5F);
            lw.ForeColor = Ui.Muted;
            lw.AutoSize = true;
            lw.Location = new Point(20, 50);
            c4.Controls.Add(lw);

            Panel c5 = AddCard(464, 96);
            Label t5 = Ui.CardTitle("关于");
            t5.Location = new Point(20, 14);
            c5.Controls.Add(t5);

            Label ver = new Label();
            ver.Text = "深蓝 DeepBlue V" + AppInfo.Version;
            ver.Font = Ui.F(9F);
            ver.ForeColor = Ui.Ink;
            ver.AutoSize = true;
            ver.Location = new Point(20, 46);
            c5.Controls.Add(ver);

            LinkLabel repo = new LinkLabel();
            repo.Text = AppInfo.RepoUrl;
            repo.Font = Ui.F(9F);
            repo.LinkColor = Ui.Accent;
            repo.AutoSize = true;
            repo.Location = new Point(20, 68);
            repo.Click += delegate
            {
                try { System.Diagnostics.Process.Start(AppInfo.RepoUrl); }
                catch (Exception) { }
            };
            c5.Controls.Add(repo);

            Button save = Ui.PrimaryButton("保存设置", 120, 40);
            save.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            save.Location = new Point(20, ClientSize.Height - 60);
            save.Click += delegate { SaveAll(); };
            Controls.Add(save);

            Button close = Ui.GhostButton("关闭", 80, 40);
            close.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            close.Location = new Point(152, ClientSize.Height - 60);
            close.Click += delegate { Close(); };
            Controls.Add(close);
        }

        private CheckBox MakeToggle(string text, bool value)
        {
            CheckBox c = new CheckBox();
            c.Text = text;
            c.Checked = value;
            c.Font = Ui.F(9F);
            c.ForeColor = Ui.Ink;
            c.AutoSize = true;
            return c;
        }

        private void UpdateRateLabel()
        {
            _lblRateVal.Text = (_trkRate.Value / 10.0).ToString("0.0") + "x";
        }

        private void PreviewVoice()
        {
            try
            {
                using (System.Speech.Synthesis.SpeechSynthesizer s =
                    new System.Speech.Synthesis.SpeechSynthesizer())
                {
                    if (_cboVoice.SelectedIndex >= 0 && _cboVoice.Items.Count > 0)
                    {
                        try
                        {
                            string name = (string)_cboVoice.Items[_cboVoice.SelectedIndex];
                            if (name.Length > 0 && name != "（未检测到系统语音）") s.SelectVoice(name);
                        }
                        catch (Exception) { }
                    }
                    double rate = _trkRate.Value / 10.0;
                    int sr = (int)Math.Round((rate - 1.0) * 10.0);
                    if (sr < -10) sr = -10;
                    if (sr > 10) sr = 10;
                    s.Rate = sr;
                    s.SpeakAsync(AppInfo.SampleVoiceText);
                }
            }
            catch (Exception)
            {
                MessageBox.Show("语音试听失败，请检查系统语音。", AppInfo.Name,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SaveAll()
        {
            _store.Settings.VoiceName =
                _cboVoice.SelectedIndex >= 0 && _cboVoice.Items.Count > 0
                    ? (string)_cboVoice.Items[_cboVoice.SelectedIndex]
                    : "";
            if (_store.Settings.VoiceName == "（未检测到系统语音）") _store.Settings.VoiceName = "";
            _store.Settings.Rate = _trkRate.Value / 10.0;
            _store.Settings.WindowDays = (int)_numDays.Value;
            _store.Settings.SecDate = _chkDate.Checked;
            _store.Settings.SecToday = _chkToday.Checked;
            _store.Settings.SecDue = _chkDue.Checked;
            _store.Save();
            MessageBox.Show("设置已保存。", AppInfo.Name,
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
