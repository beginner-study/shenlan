using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DeepBlue
{
    public class MainForm : Form
    {
        private enum BState { Loading, Ready, Playing, Paused, Finished }

        private Store _store;
        private BroadcastEngine _engine = new BroadcastEngine();
        private DateTime _dataDay;
        private BState _state = BState.Loading;
        private bool _voiceWarned;
        private bool _pendingStart;
        private WeatherData _weather;

        private Panel _cardDate;
        private Label _lblDate;
        private Label _lblDateSub;
        private RichTextBox _rtb;
        private List<int> _sentStart = new List<int>();
        private List<int> _sentLen = new List<int>();
        private int _curSent = -1;

        private Button _btnStart;
        private Panel _pnlControls;
        private Button _btnPause;
        private Button _btnStop;
        private Button _btnSched;
        private Button _btnSettings;
        private Label _lblStatus;
        private Label _lblReadyAt;

        private ScheduleForm _schedForm;
        private System.Windows.Forms.Timer _loadTimer;

        public MainForm()
        {
            Text = AppInfo.Name;
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(960, 660);
            MinimumSize = new Size(860, 560);
            Font = Ui.F(9F);
            BackColor = Ui.Bg;
            Icon = LoadIcon();

            BuildUi();

            _engine.SentenceStarted += delegate (int i)
            {
                if (IsHandleCreated) BeginInvoke((Action)(delegate { OnSentenceStarted(i); }));
            };
            _engine.Finished += delegate
            {
                if (IsHandleCreated) BeginInvoke((Action)(delegate { OnEngineFinished(); }));
            };

            _loadTimer = new System.Windows.Forms.Timer();
            _loadTimer.Interval = 450;
            _loadTimer.Tick += delegate { _loadTimer.Stop(); OnDataLoaded(); };
            _loadTimer.Start();
        }

        internal static Icon LoadIcon()
        {
            try
            {
                string path = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (System.IO.File.Exists(path)) return new Icon(path);
            }
            catch (Exception) { }
            return null;
        }

        private void BuildUi()
        {
            Panel pnlTop = new Panel();
            pnlTop.Dock = DockStyle.Top;
            pnlTop.Height = 140;
            pnlTop.BackColor = Ui.Bg;
            Controls.Add(pnlTop);

            _btnSched = Ui.GhostButton("日程管理", 92, 34);
            _btnSched.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnSched.Click += delegate { OpenSchedule(); };
            pnlTop.Controls.Add(_btnSched);

            _btnSettings = Ui.GhostButton("设置", 72, 34);
            _btnSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            _btnSettings.Click += delegate { OpenSettings(); };
            pnlTop.Controls.Add(_btnSettings);

            _cardDate = new Panel();
            _cardDate.BackColor = Ui.Card;
            _cardDate.Paint += delegate (object s, PaintEventArgs e)
            { Ui.PaintCardBorder(s, e, _state == BState.Loading); };
            pnlTop.Controls.Add(_cardDate);

            _lblDate = new Label();
            _lblDate.Text = "——";
            _lblDate.Font = Ui.F(19F, FontStyle.Bold);
            _lblDate.ForeColor = Ui.Ink;
            _lblDate.AutoSize = false;
            _lblDate.Height = 40;
            _lblDate.Location = new Point(20, 12);
            _lblDate.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            _lblDate.Click += delegate { _cardDate.Focus(); };
            _cardDate.Controls.Add(_lblDate);

            _lblDateSub = new Label();
            _lblDateSub.Text = "正在读取系统日期与本地日程…";
            _lblDateSub.Font = Ui.F(9.5F);
            _lblDateSub.ForeColor = Ui.Muted;
            _lblDateSub.AutoSize = false;
            _lblDateSub.Height = 24;
            _lblDateSub.Location = new Point(20, 54);
            _lblDateSub.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            _cardDate.Controls.Add(_lblDateSub);

            Panel pnlMid = new Panel();
            pnlMid.Dock = DockStyle.Fill;
            pnlMid.BackColor = Ui.Bg;
            pnlMid.Padding = new Padding(24, 4, 24, 12);
            Controls.Add(pnlMid);
            pnlMid.BringToFront();

            Panel cardScript = new Panel();
            cardScript.Dock = DockStyle.Fill;
            cardScript.BackColor = Ui.Card;
            cardScript.Padding = new Padding(20, 16, 20, 16);
            cardScript.Paint += delegate (object s, PaintEventArgs e) { Ui.PaintCardBorder(s, e, false); };
            pnlMid.Controls.Add(cardScript);

            _rtb = new RichTextBox();
            _rtb.Dock = DockStyle.Fill;
            _rtb.BorderStyle = BorderStyle.None;
            _rtb.BackColor = Ui.Card;
            _rtb.Font = Ui.F(11.5F);
            _rtb.ForeColor = Ui.Ink;
            _rtb.ReadOnly = true;
            _rtb.TabStop = false;
            _rtb.HideSelection = false;
            _rtb.DetectUrls = false;
            _rtb.WordWrap = true;
            _rtb.Text = "";
            cardScript.Controls.Add(_rtb);

            Panel pnlBottom = new Panel();
            pnlBottom.Dock = DockStyle.Bottom;
            pnlBottom.Height = 118;
            pnlBottom.BackColor = Ui.Bg;
            Controls.Add(pnlBottom);

            _btnStart = Ui.PrimaryButton("开始新的一天", 300, 52);
            _btnStart.Click += delegate { OnStartClick(); };
            pnlBottom.Controls.Add(_btnStart);
            _btnStart.Enabled = false;
            _btnStart.BackColor = Color.FromArgb(156, 163, 175);

            _pnlControls = new Panel();
            _pnlControls.Width = 320;
            _pnlControls.Height = 52;
            _pnlControls.BackColor = Ui.Bg;
            _pnlControls.Visible = false;
            pnlBottom.Controls.Add(_pnlControls);

            _btnPause = Ui.GhostButton("暂停", 150, 52);
            _btnPause.Location = new Point(0, 0);
            _btnPause.Click += delegate { OnPauseClick(); };
            _pnlControls.Controls.Add(_btnPause);

            _btnStop = Ui.DangerGhostButton("停止", 150, 52);
            _btnStop.Location = new Point(170, 0);
            _btnStop.Click += delegate { OnStopClick(); };
            _pnlControls.Controls.Add(_btnStop);

            _lblStatus = new Label();
            _lblStatus.Text = AppInfo.NameEn + " V" + AppInfo.Version;
            _lblStatus.Font = Ui.F(8.5F);
            _lblStatus.ForeColor = Ui.Muted;
            _lblStatus.AutoSize = true;
            _lblStatus.Location = new Point(24, 94);
            _lblStatus.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            pnlBottom.Controls.Add(_lblStatus);

            _lblReadyAt = new Label();
            _lblReadyAt.Text = "";
            _lblReadyAt.Font = Ui.F(8.5F);
            _lblReadyAt.ForeColor = Ui.Muted;
            _lblReadyAt.AutoSize = true;
            _lblReadyAt.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;
            pnlBottom.Controls.Add(_lblReadyAt);

            Resize += delegate { LayoutControls(); };
            LayoutControls();
        }

        private void LayoutControls()
        {
            int w = ClientSize.Width;
            _btnSched.Location = new Point(w - _btnSched.Width - 24, 20);
            _btnSettings.Location = new Point(w - _btnSettings.Width - 24 - _btnSched.Width - 8, 20);
            _cardDate.Location = new Point(24, 18);
            _cardDate.Width = w - 48;
            _cardDate.Height = 90;
            _lblDate.Width = _cardDate.Width - 40;
            _lblDateSub.Width = _cardDate.Width - 40;
            _lblReadyAt.Location = new Point(
                w - _lblReadyAt.Width - 24,
                _lblReadyAt.Parent.Height - 24);
        }

        private void OnDataLoaded()
        {
            _store = Store.Load();
            _dataDay = DateTime.Today;
            _state = BState.Ready;
            _btnStart.Enabled = true;
            _btnStart.BackColor = Ui.Accent;
            RefreshDateCard();
            _rtb.Text = "";
            _sentStart.Clear();
            _sentLen.Clear();
            _curSent = -1;
            _lblReadyAt.Text = "数据就绪 · " + DateTime.Now.ToString("HH:mm");
            StartWeather();
            if (_pendingStart)
            {
                _pendingStart = false;
                BeginBroadcast();
            }
        }

        private void StartWeather()
        {
            _weather = null;
            if (_store == null) return;
            AppSettings s = _store.Settings;
            if (!s.WeatherOn || string.IsNullOrEmpty(s.WeatherCity))
            {
                UpdateWeatherUi(null, false);
                return;
            }
            WeatherData cached = WeatherEngine.LoadCache();
            if (cached != null && WeatherEngine.IsFresh(cached) && WeatherEngine.Matches(cached, s))
            {
                _weather = cached;
                UpdateWeatherUi(cached, false);
                return;
            }
            UpdateWeatherUi(null, true);
            string city = s.WeatherCity;
            double lat = s.WeatherLat;
            double lon = s.WeatherLon;
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                WeatherData d = WeatherEngine.Fetch(city, lat, lon);
                if (d != null) WeatherEngine.SaveCache(d);
                try
                {
                    BeginInvoke((Action)(delegate
                    {
                        if (IsDisposed || _store == null) return;
                        if (_store.Settings.WeatherCity != city) return;
                        if (d != null) _weather = d;
                        UpdateWeatherUi(d, false);
                    }));
                }
                catch (Exception) { }
            });
        }

        private void UpdateWeatherUi(WeatherData d, bool fetching)
        {
            if (d != null)
            {
                _lblDateSub.Text = "数据就绪 · " + WeatherEngine.CardLine(d);
            }
            else if (fetching)
            {
                _lblDateSub.Text = "数据就绪 · 天气获取中…";
            }
            else if (_store != null && _store.Settings.WeatherOn &&
                     !string.IsNullOrEmpty(_store.Settings.WeatherCity))
            {
                _lblDateSub.Text = "数据就绪 · 天气暂不可用（播报将跳过天气段）";
            }
            else
            {
                _lblDateSub.Text = "数据就绪 · 点击按钮开始播报";
            }
        }

        private void RefreshDateCard()
        {
            DateTime today = DateTime.Today;
            _lblDate.Text = ScriptEngine.CnDate(today) + " " + ScriptEngine.WeekName(today);
            _cardDate.Invalidate();
        }

        private void OnStartClick()
        {
            if (_state == BState.Playing || _state == BState.Paused) return;
            if (_state == BState.Loading)
            {
                _pendingStart = true;
                return;
            }
            BeginBroadcast();
        }

        private void BeginBroadcast()
        {
            if (_store == null) return;
            AppSettings s = _store.Settings;
            bool weatherUsable = s.WeatherOn && !string.IsNullOrEmpty(s.WeatherCity);
            if (!s.SecDate && !s.SecToday && !s.SecDue && !weatherUsable)
            {
                MessageBox.Show("所有播报段落均已关闭，请在设置中开启。",
                    AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (_dataDay != DateTime.Today)
            {
                OnDataLoaded();
                _lblReadyAt.Text = "检测到日期已变化，已自动重新取数 · " + DateTime.Now.ToString("HH:mm");
            }

            if (!_voiceWarned && !BroadcastEngine.HasChineseVoice())
            {
                _voiceWarned = true;
                MessageBox.Show(
                    "当前系统未检测到中文语音包，播报将使用系统默认语音，效果可能不理想。\n\n" +
                    "安装指引：设置 → 时间和语言 → 语言 → 中文（简体）→ 语言选项 → 添加语音（TTS）。",
                    AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            List<string> sents = ScriptEngine.Compose(_store.Items, s, _weather);
            RenderScript(sents);
            _engine.Play(sents, s.VoiceName, s.Rate);
            SetState(BState.Playing);
        }

        private void RenderScript(List<string> sents)
        {
            _rtb.Text = "";
            _sentStart.Clear();
            _sentLen.Clear();
            _curSent = -1;
            foreach (string sent in sents)
            {
                int start = _rtb.TextLength;
                _rtb.AppendText(sent);
                _rtb.AppendText("\n\n");
                _sentStart.Add(start);
                _sentLen.Add(sent.Length);
            }
        }

        private void OnSentenceStarted(int i)
        {
            if ((_state != BState.Playing && _state != BState.Paused) || i < 0 || i >= _sentStart.Count) return;
            if (_curSent >= 0 && _curSent < _sentStart.Count)
            {
                _rtb.Select(_sentStart[_curSent], _sentLen[_curSent]);
                _rtb.SelectionBackColor = Ui.Card;
                _rtb.SelectionColor = Ui.Ink;
            }
            _curSent = i;
            _rtb.Select(_sentStart[i], _sentLen[i]);
            _rtb.SelectionBackColor = Ui.AccentSoft;
            _rtb.SelectionColor = Ui.Ink;
            _rtb.Select(_sentStart[i], 1);
            _rtb.ScrollToCaret();
        }

        private void OnEngineFinished()
        {
            if (_state == BState.Playing || _state == BState.Paused) SetState(BState.Finished);
        }

        private void OnPauseClick()
        {
            if (_state == BState.Playing)
            {
                _engine.Pause();
                SetState(BState.Paused);
            }
            else if (_state == BState.Paused)
            {
                _engine.Resume();
                SetState(BState.Playing);
            }
        }

        private void OnStopClick()
        {
            _engine.Stop();
            SetState(BState.Ready);
            _btnStart.Text = "重新播报";
            _lblStatus.Text = "已停止";
        }

        private void SetState(BState st)
        {
            _state = st;
            bool showMain = (st == BState.Loading || st == BState.Ready || st == BState.Finished);
            _btnStart.Visible = showMain;
            _pnlControls.Visible = !showMain;
            if (st == BState.Finished)
            {
                _btnStart.Text = "重新播报";
                _lblStatus.Text = "播报完成 · " + DateTime.Now.ToString("HH:mm");
            }
            else if (st == BState.Playing)
            {
                _btnPause.Text = "暂停";
                _lblStatus.Text = "播报中…";
            }
            else if (st == BState.Paused)
            {
                _btnPause.Text = "继续";
                _lblStatus.Text = "已暂停";
            }
            else if (st == BState.Ready)
            {
                _btnStart.Text = _rtb.TextLength > 0 ? "重新播报" : "开始新的一天";
                _lblStatus.Text = AppInfo.NameEn + " V" + AppInfo.Version;
            }
        }

        private void OpenSchedule()
        {
            if (_store == null) return;
            if (_schedForm == null || _schedForm.IsDisposed)
            {
                _schedForm = new ScheduleForm(_store);
            }
            _schedForm.Show(this);
            _schedForm.Activate();
        }

        private void OpenSettings()
        {
            if (_store == null) return;
            using (SettingsForm f = new SettingsForm(_store))
            {
                f.ShowDialog(this);
            }
            StartWeather();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _engine.Stop();
            if (_schedForm != null && !_schedForm.IsDisposed)
            {
                try { _schedForm.Close(); } catch (Exception) { }
            }
            base.OnFormClosing(e);
        }
    }
}
