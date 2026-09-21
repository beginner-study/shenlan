using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DeepBlue
{
    // 方案一 · 书籍形态主窗体
    // 封面态（合上的书）→ 3D 翻页 → 展开态（对页：今日 + 晨间播报）
    public class MainForm : Form
    {
        private enum BState { Loading, Ready, Opening, Playing, Paused, Finished }

        private Store _store;
        private BroadcastEngine _engine = new BroadcastEngine();
        private DateTime _dataDay;
        private BState _state = BState.Loading;
        private bool _voiceWarned;
        private WeatherData _weather;

        private readonly CoverPanel _cover;
        private readonly BookPanel _book;

        private List<int> _sentStart = new List<int>();
        private List<int> _sentLen = new List<int>();
        private int _curSent = -1;

        private ScheduleForm _schedForm;
        private GlassMenu _menu;
        private System.Windows.Forms.Timer _loadTimer;

        // 窗口物理像素常量（1.5 倍放大）：封面 678x903、展开 1440x1026
        private const int CoverW = 678, CoverH = 903;
        private const int BookW = 1440, BookH = 1026;

        public MainForm()
        {
            Text = AppInfo.Name;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = true;
            Size = new Size(CoverW, CoverH);
            Font = Ui.F(9F * Ui.Scale);
            BackColor = Ui.ForestShell;
            Icon = LoadIcon();

            _cover = new CoverPanel();
            _cover.Bounds = new Rectangle(0, 0, CoverW, CoverH);
            _cover.OpenClick += delegate { OnOpenClick(); };
            _cover.DotsClick += delegate { ShowMenu(_cover, new Point(Ui.X(396), Ui.X(58))); };
            _cover.CloseClick += delegate { Close(); };
            Controls.Add(_cover);

            _book = new BookPanel();
            _book.Visible = false;
            _book.DotsClick += delegate { ShowMenu(_book, new Point(Ui.X(890), Ui.X(47))); };
            _book.CloseClick += delegate { Close(); };
            _book.BtnPause.Click += delegate { OnPauseClick(); };
            _book.BtnStop.Click += delegate { OnStopClick(); };
            _book.MouseDown += delegate (object s, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left)
                    DragMove();
            };
            Controls.Add(_book);

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

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(
            IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
        private const int WM_NCLBUTTONDOWN = 0xA1;
        private const int HTCAPTION = 2;

        private void DragMove()
        {
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, (IntPtr)HTCAPTION, IntPtr.Zero);
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

        // 与 ScriptEngine.Greeting 时段一致
        private static string HeadTitleFor(int hour)
        {
            if (hour >= 5 && hour < 11) return "晨间播报";
            if (hour >= 11 && hour < 13) return "午间播报";
            if (hour >= 13 && hour < 18) return "下午播报";
            return "晚间播报";
        }

        // ============ 数据 ============

        private void OnDataLoaded()
        {
            _store = Store.Load();
            _dataDay = DateTime.Today;
            // 右页标题与问候语同一套时段，避免「晨间播报 / 下午好」自相矛盾
            _book.SetHeadTitle(HeadTitleFor(DateTime.Now.Hour));
            _state = BState.Ready;
            _cover.OpenEnabled = true;
            _book.PageLeft.SetWeatherFlag(_store.Settings.WeatherOn &&
                _store.Settings.WeatherCity.Length > 0);
            RefreshLeftPage("数据就绪 · " + DateTime.Now.ToString("HH:mm"));
            StartWeather();
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
            AppSettings wsnap = _store.Settings;
            System.Threading.ThreadPool.QueueUserWorkItem(delegate
            {
                WeatherData d = WeatherEngine.Fetch(wsnap);
                if (d != null) WeatherEngine.SaveCache(d);
                try
                {
                    BeginInvoke((Action)(delegate
                    {
                        if (IsDisposed || _store == null) return;
                        if (_store.Settings.WeatherCity != wsnap.WeatherCity ||
                            _store.Settings.WeatherSource != wsnap.WeatherSource) return;
                        if (d != null) _weather = d;
                        UpdateWeatherUi(d, false);
                    }));
                }
                catch (Exception) { }
            });
        }

        private void UpdateWeatherUi(WeatherData d, bool fetching)
        {
            // 底注只报状态，不重复天气正文（天气卡片已在上方展示）
            string note;
            if (fetching) note = "数据就绪 · 天气获取中…";
            else if (d != null) note = "数据就绪 · 天气已更新";
            else if (_store != null && _store.Settings.WeatherOn &&
                     !string.IsNullOrEmpty(_store.Settings.WeatherCity))
                note = "数据就绪 · 天气暂不可用";
            else
                note = "数据就绪";
            RefreshLeftPage(note);
        }

        private void RefreshLeftPage(string note)
        {
            _book.PageLeft.SetData(_store, _weather, note);
        }

        // ============ 开启新的一天 ============

        private void OnOpenClick()
        {
            if (_state != BState.Ready) return;
            if (_store == null) return;

            AppSettings s = _store.Settings;
            bool qwReady = s.WeatherSource != "qweather" ||
                (s.QwHost.Length > 0 && s.QwKey.Length > 0 && s.QwLocation.Length > 0);
            bool weatherUsable = s.WeatherOn && !string.IsNullOrEmpty(s.WeatherCity) && qwReady;
            if (!s.SecDate && !s.SecToday && !s.SecDue && !weatherUsable)
            {
                MessageBox.Show("所有播报段落均已关闭，请在设置中开启。",
                    AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (!_voiceWarned && !BroadcastEngine.HasChineseVoice())
            {
                _voiceWarned = true;
                MessageBox.Show(
                    "当前系统未检测到中文语音包，播报将使用系统默认语音，效果可能不理想。\n\n" +
                    "安装指引：设置 → 时间和语言 → 语言 → 中文（简体）→ 语言选项 → 添加语音（TTS）。",
                    AppInfo.Name, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            _state = BState.Opening;

            // 展开窗口在当前屏幕工作区尽量正中（用户约定：不再保持书脊定位）
            Rectangle wa = Screen.FromControl(this).WorkingArea;
            int newX = wa.Left + (wa.Width - BookW) / 2;
            int newY = wa.Top + (wa.Height - BookH) / 2;
            if (newX < wa.Left) newX = wa.Left;
            if (newY < wa.Top) newY = wa.Top;
            if (newX + BookW > wa.Right) newX = wa.Right - BookW;
            if (newY + BookH > wa.Bottom) newY = wa.Bottom - BookH;
            SetBounds(newX, newY, BookW, BookH);

            _cover.Visible = false;
            _book.Visible = true;
            _book.BringToFront();
            _state = BState.Ready;
            BeginBroadcast();
        }

        // ============ 播报 ============

        private void BeginBroadcast()
        {
            if (_state == BState.Opening) return;
            if (_store == null) return;

            if (_dataDay != DateTime.Today)
            {
                OnDataLoaded();
            }

            List<string> sents = ScriptEngine.Compose(_store.Items, _store.Settings, _weather);
            RenderScript(sents);
            _engine.Play(sents, _store.Settings.VoiceName, _store.Settings.Rate);
            SetState(BState.Playing);
        }

        private void RenderScript(List<string> sents)
        {
            _book.Rtb.Text = "";
            _sentStart.Clear();
            _sentLen.Clear();
            _curSent = -1;
            foreach (string sent in sents)
            {
                int start = _book.Rtb.TextLength;
                _book.Rtb.AppendText(sent);
                _book.Rtb.AppendText("\n\n");
                _sentStart.Add(start);
                _sentLen.Add(sent.Length);
            }
            _book.SetProgress(-1, sents.Count);
        }

        private void OnSentenceStarted(int i)
        {
            if ((_state != BState.Playing && _state != BState.Paused) ||
                i < 0 || i >= _sentStart.Count) return;
            if (_curSent >= 0 && _curSent < _sentStart.Count)
            {
                _book.Rtb.Select(_sentStart[_curSent], _sentLen[_curSent]);
                _book.Rtb.SelectionBackColor = Ui.Paper;
                _book.Rtb.SelectionColor = Ui.InkGreenBody;
            }
            _curSent = i;
            _book.Rtb.Select(_sentStart[i], _sentLen[i]);
            _book.Rtb.SelectionBackColor = Ui.GoldSoft;
            _book.Rtb.SelectionColor = Ui.GoldInk;
            // 选区收零（仅留插入点）：配合 HideSelection，杜绝系统蓝底原生选区
            _book.Rtb.Select(_sentStart[i], 0);
            _book.Rtb.ScrollToCaret();
            _book.SetProgress(i, _sentLen.Count);
            _book.LblStatus.Text = "正在播报 · " + (i + 1) + " / " + _sentLen.Count;
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
            SetState(BState.Finished);
            _book.LblStatus.Text = "已停止";
        }

        private void SetState(BState st)
        {
            _state = st;
            bool playing = st == BState.Playing || st == BState.Paused;
            _book.BtnPause.Visible = playing;
            _book.BtnStop.Visible = playing;
            _book.SetVisibleForPlaying(playing);
            if (st == BState.Finished)
            {
                _book.LblStatus.Text = "播报完成 · " + DateTime.Now.ToString("HH:mm");
            }
            else if (st == BState.Playing)
            {
                _book.BtnPause.Text = "暂 停";
            }
            else if (st == BState.Paused)
            {
                _book.BtnPause.Text = "继 续";
            }
        }

        // ============ 菜单 ============

        private void ShowMenu(Control host, Point btnBottomRightLocal)
        {
            if (_store == null) return;
            if (_menu != null && !_menu.IsDisposed)
            {
                try { _menu.Close(); } catch (Exception) { }
            }
            // 不能用 using 包裹：Show() 返回后菜单立即被 Dispose，永远显示不出来
            _menu = new GlassMenu(new string[] { "日程管理", "设置" }, Ui.InkGreen);
            _menu.ItemChosen += delegate (int idx)
            {
                if (idx == 0) OpenSchedule();
                else OpenSettings();
            };
            Point p = host.PointToScreen(btnBottomRightLocal);
            Screen sc = Screen.FromPoint(p);
            Point loc = new Point(p.X - _menu.Width, p.Y + 10);
            if (loc.X < sc.WorkingArea.Left) loc.X = sc.WorkingArea.Left;
            if (loc.Y + _menu.Height > sc.WorkingArea.Bottom)
                loc.Y = p.Y - _menu.Height - 50;
            _menu.Location = loc;
            _menu.Show(this);
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
            _book.PageLeft.SetWeatherFlag(_store.Settings.WeatherOn &&
                _store.Settings.WeatherCity.Length > 0);
            RefreshLeftPage("数据就绪 · " + DateTime.Now.ToString("HH:mm"));
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

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                if (_state == BState.Playing || _state == BState.Paused)
                {
                    OnStopClick();
                    return true;
                }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
