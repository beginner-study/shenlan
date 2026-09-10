using System;
using System.Collections.Generic;
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
        private CheckBox _chkWeather;
        private TextBox _txtCity;
        private Button _btnSearch;
        private ComboBox _cboHits;
        private Label _lblCityNow;
        private ComboBox _cboSource;
        private TextBox _txtQwHost;
        private TextBox _txtQwKey;
        private Label _lblSourceTag;
        private string _pickName = "";
        private double _pickLat;
        private double _pickLon;
        private string _pickLoc = "";

        public SettingsForm(Store store)
        {
            _store = store;
            _pickName = _store.Settings.WeatherCity;
            _pickLat = _store.Settings.WeatherLat;
            _pickLon = _store.Settings.WeatherLon;
            _pickLoc = _store.Settings.QwLocation;
            Text = AppInfo.Name + " · 设置";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            Size = new Size(620, 892);
            MinimumSize = new Size(580, 852);
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
            if (selIdx < 0 && _cboVoice.Items.Count > 0)
            {
                List<string> names = new List<string>();
                foreach (object o in _cboVoice.Items) names.Add((string)o);
                string pref = VoicePicker.PickDefault(names);
                for (int i = 0; i < _cboVoice.Items.Count; i++)
                {
                    if ((string)_cboVoice.Items[i] == pref) { selIdx = i; break; }
                }
                if (selIdx < 0) selIdx = 0;
            }
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

            CheckBox weather = MakeToggle("天气", _store.Settings.WeatherOn);
            weather.Location = new Point(20, 78);
            c3.Controls.Add(weather);
            _chkWeather = weather;

            _chkToday = MakeToggle("今日安排", _store.Settings.SecToday);
            _chkToday.Location = new Point(230, 48);
            c3.Controls.Add(_chkToday);

            _chkDue = MakeToggle("即将截止提醒", _store.Settings.SecDue);
            _chkDue.Location = new Point(230, 78);
            c3.Controls.Add(_chkDue);

            Panel c4 = AddCard(368, 244);
            Label t4 = Ui.CardTitle("天气");
            t4.Location = new Point(20, 14);
            c4.Controls.Add(t4);

            _lblSourceTag = new Label();
            _lblSourceTag.Font = Ui.F(8F);
            _lblSourceTag.ForeColor = Ui.Accent;
            _lblSourceTag.BackColor = Ui.AccentSoft;
            _lblSourceTag.AutoSize = false;
            _lblSourceTag.Width = 140;
            _lblSourceTag.Height = 22;
            _lblSourceTag.Location = new Point(60, 12);
            _lblSourceTag.TextAlign = ContentAlignment.MiddleCenter;
            c4.Controls.Add(_lblSourceTag);

            Label lsrc = Ui.FieldLabel("数据源");
            lsrc.Location = new Point(20, 52);
            c4.Controls.Add(lsrc);

            _cboSource = new ComboBox();
            _cboSource.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboSource.Font = Ui.F(9F);
            _cboSource.Width = 220;
            _cboSource.Location = new Point(68, 48);
            _cboSource.Items.Add("Open-Meteo（免费，无需 Key）");
            _cboSource.Items.Add("和风天气（国内数据，需 Key）");
            _cboSource.SelectedIndex =
                _store.Settings.WeatherSource == "qweather" ? 1 : 0;
            _cboSource.SelectedIndexChanged += delegate { OnSourceChanged(); };
            c4.Controls.Add(_cboSource);

            Label lhost = Ui.FieldLabel("API Host");
            lhost.Location = new Point(20, 88);
            c4.Controls.Add(lhost);

            _txtQwHost = new TextBox();
            _txtQwHost.Font = Ui.F(9F);
            _txtQwHost.Width = 302;
            _txtQwHost.Location = new Point(68, 84);
            _txtQwHost.Text = _store.Settings.QwHost;
            c4.Controls.Add(_txtQwHost);

            Label lkey = Ui.FieldLabel("API Key");
            lkey.Location = new Point(20, 122);
            c4.Controls.Add(lkey);

            _txtQwKey = new TextBox();
            _txtQwKey.Font = Ui.F(9F);
            _txtQwKey.Width = 302;
            _txtQwKey.Location = new Point(68, 118);
            _txtQwKey.Text = _store.Settings.QwKey;
            c4.Controls.Add(_txtQwKey);

            Label lkw = Ui.FieldLabel("城市");
            lkw.Location = new Point(20, 156);
            c4.Controls.Add(lkw);

            _txtCity = new TextBox();
            _txtCity.Font = Ui.F(9F);
            _txtCity.Width = 220;
            _txtCity.Location = new Point(68, 152);
            _txtCity.KeyDown += delegate (object s, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; OnSearchCity(); }
            };
            c4.Controls.Add(_txtCity);

            _btnSearch = Ui.GhostButton("搜索", 72, 30);
            _btnSearch.Location = new Point(298, 151);
            _btnSearch.Click += delegate { OnSearchCity(); };
            c4.Controls.Add(_btnSearch);

            _cboHits = new ComboBox();
            _cboHits.DropDownStyle = ComboBoxStyle.DropDownList;
            _cboHits.Font = Ui.F(9F);
            _cboHits.Width = 302;
            _cboHits.Location = new Point(68, 186);
            _cboHits.Items.Add("（输入城市名后点击搜索）");
            _cboHits.SelectedIndex = 0;
            _cboHits.SelectedIndexChanged += delegate
            {
                CityHit h = _cboHits.SelectedItem as CityHit;
                if (h != null) ApplyPick(h);
            };
            c4.Controls.Add(_cboHits);

            _lblCityNow = new Label();
            _lblCityNow.Font = Ui.F(8.5F);
            _lblCityNow.ForeColor = Ui.Muted;
            _lblCityNow.AutoSize = true;
            _lblCityNow.Location = new Point(20, 220);
            c4.Controls.Add(_lblCityNow);
            UpdateCityNow();
            OnSourceChanged();

            Panel c5 = AddCard(624, 96);
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

        private bool IsQwMode()
        {
            return _cboSource != null && _cboSource.SelectedIndex == 1;
        }

        private void OnSourceChanged()
        {
            bool qw = IsQwMode();
            _txtQwHost.Enabled = qw;
            _txtQwKey.Enabled = qw;
            _txtQwHost.BackColor = qw ? SystemColors.Window : Ui.Bg;
            _txtQwKey.BackColor = qw ? SystemColors.Window : Ui.Bg;
            _lblSourceTag.Text = qw ? "和风天气 · 需联网" : "Open-Meteo · 免费免Key";
            _lblSourceTag.Width = qw ? 130 : 140;
            UpdateCityNow();
        }

        private string EffectiveQwLocation()
        {
            if (_pickLoc.Length > 0) return _pickLoc;
            if (_pickLat != 0 || _pickLon != 0)
            {
                return WeatherEngine.BuildQwCoords(_pickLat, _pickLon);
            }
            return "";
        }

        private void UpdateCityNow()
        {
            if (_pickName.Length > 0)
            {
                if (IsQwMode())
                {
                    string loc = EffectiveQwLocation();
                    if (loc.Length == 0)
                    {
                        _lblCityNow.Text = "当前：" + _pickName + "（请重新搜索一次城市以同步和风配置）";
                    }
                    else if (loc.IndexOf(',') >= 0)
                    {
                        _lblCityNow.Text = "当前：" + _pickName + "（和风坐标 " + loc + "）";
                    }
                    else
                    {
                        _lblCityNow.Text = "当前：" + _pickName + "（和风 LocationID " + loc + "）";
                    }
                }
                else
                {
                    System.Globalization.CultureInfo inv =
                        System.Globalization.CultureInfo.InvariantCulture;
                    _lblCityNow.Text = "当前：" + _pickName + "（" +
                        _pickLat.ToString("0.####", inv) + ", " +
                        _pickLon.ToString("0.####", inv) + "）";
                }
            }
            else
            {
                _lblCityNow.Text = "当前：未设置";
            }
        }

        private void ApplyPick(CityHit h)
        {
            _pickName = h.Name;
            _pickLat = h.Lat;
            _pickLon = h.Lon;
            _pickLoc = h.LocationId;
            UpdateCityNow();
        }

        private void OnSearchCity()
        {
            string kw = _txtCity.Text.Trim();
            if (kw.Length == 0)
            {
                MessageBox.Show("请输入城市名，如：北京、上海。", AppInfo.Name,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            AppSettings snap = new AppSettings();
            snap.WeatherSource = IsQwMode() ? "qweather" : "open-meteo";
            snap.QwHost = _txtQwHost.Text.Trim();
            snap.QwKey = _txtQwKey.Text.Trim();
            if (IsQwMode() && (snap.QwHost.Length == 0 || snap.QwKey.Length == 0))
            {
                MessageBox.Show("使用和风天气前，请先填写 API Host 与 API Key（在和风天气开发者控制台 → 设置中查看）。",
                    AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            _btnSearch.Enabled = false;
            _btnSearch.Text = "搜索中…";
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                List<CityHit> hits = WeatherEngine.SearchCity(kw, snap);
                try
                {
                    BeginInvoke((Action)(delegate
                    {
                        if (IsDisposed) return;
                        _btnSearch.Enabled = true;
                        _btnSearch.Text = "搜索";
                        _cboHits.Items.Clear();
                        if (hits.Count == 0)
                        {
                            _cboHits.Items.Add("未找到城市，请换个关键词");
                            if (WeatherEngine.LastError != null && WeatherEngine.LastError.Length > 0)
                            {
                                _cboHits.Items.Add("错误：" + WeatherEngine.LastError);
                            }
                            _cboHits.SelectedIndex = 0;
                            return;
                        }
                        foreach (CityHit h in hits) _cboHits.Items.Add(h);
                        _cboHits.SelectedIndex = 0;
                        ApplyPick(hits[0]);
                    }));
                }
                catch (Exception) { }
            });
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
            _store.Settings.WeatherOn = _chkWeather.Checked;
            _store.Settings.WeatherSource = IsQwMode() ? "qweather" : "open-meteo";
            _store.Settings.QwHost = _txtQwHost.Text.Trim();
            _store.Settings.QwKey = _txtQwKey.Text.Trim();
            if (_pickName.Length > 0)
            {
                _store.Settings.WeatherCity = _pickName;
                _store.Settings.WeatherLat = _pickLat;
                _store.Settings.WeatherLon = _pickLon;
                _store.Settings.QwLocation = IsQwMode() ? EffectiveQwLocation() : "";
            }
            _store.Save();
            if (_store.Settings.WeatherOn && _store.Settings.WeatherCity.Length == 0)
            {
                MessageBox.Show(
                    "设置已保存。天气播报已开启，但尚未设置城市，天气段将暂不播报。\n" +
                    "请在「天气」卡片中搜索并选择城市。",
                    AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else if (_store.Settings.WeatherOn && _store.Settings.WeatherSource == "qweather" &&
                     (_store.Settings.QwHost.Length == 0 || _store.Settings.QwKey.Length == 0 ||
                      _store.Settings.QwLocation.Length == 0))
            {
                MessageBox.Show(
                    "设置已保存。和风天气配置不完整（缺少 API Host、API Key 或城市 LocationID），天气段将暂不播报。",
                    AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("设置已保存。", AppInfo.Name,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
