using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace DeepBlue
{
    public class ScheduleForm : Form
    {
        private Store _store;
        private string _tab = "all";
        private int _editingId = -1;

        private Panel _editorCard;
        private TextBox _fTitle;
        private ComboBox _fRecur;
        private Label _fRecurLabel;
        private ComboBox _fWeekday;
        private NumericUpDown _fMonthday;
        private Label _fDateLabel;
        private DateTimePicker _fDate;
        private TextBox _fTime;
        private DateTimePicker _fDue;
        private ComboBox _fPriority;
        private TextBox _fNote;
        private Label _fErr;
        private Panel _list;
        private List<Button> _tabButtons = new List<Button>();
        private System.Windows.Forms.Timer _confirmTimer;

        public ScheduleForm(Store store)
        {
            _store = store;
            Text = AppInfo.Name + " · 日程管理";
            StartPosition = FormStartPosition.CenterParent;
            Size = new Size(1000, 680);
            MinimumSize = new Size(960, 620);
            Font = Ui.F(9F);
            BackColor = Ui.Bg;
            Icon = MainForm.LoadIcon();

            Panel list = new Panel();
            list.Dock = DockStyle.Fill;
            list.AutoScroll = true;
            list.BackColor = Ui.Bg;
            list.Padding = new Padding(24, 4, 24, 16);
            Controls.Add(list);
            _list = list;

            _editorCard = new Panel();
            _editorCard.Dock = DockStyle.Top;
            _editorCard.Height = 178;
            _editorCard.BackColor = Ui.Card;
            _editorCard.Visible = false;
            _editorCard.Paint += delegate (object s, PaintEventArgs e) { Ui.PaintCardBorder(s, e, false); };
            Controls.Add(_editorCard);

            Panel toolbar = new Panel();
            toolbar.Dock = DockStyle.Top;
            toolbar.Height = 60;
            toolbar.BackColor = Ui.Bg;
            Controls.Add(toolbar);

            BuildToolbar(toolbar);
            BuildEditor();

            _confirmTimer = new System.Windows.Forms.Timer();
            _confirmTimer.Interval = 500;
            _confirmTimer.Tick += delegate
            {
                bool pending = false;
                foreach (ScheduleItem it in _store.Items)
                {
                    if (it.ConfirmUntil > DateTime.Now) pending = true;
                }
                if (pending) return;
                bool had = false;
                foreach (ScheduleItem it in _store.Items)
                {
                    if (it.ConfirmUntil != DateTime.MinValue) { it.ConfirmUntil = DateTime.MinValue; had = true; }
                }
                if (had) Render();
                _confirmTimer.Stop();
            };

            Resize += delegate { Render(); };
            Render();
        }

        private void BuildToolbar(Panel toolbar)
        {
            string[] names = { "全部", "今天", "即将截止" };
            for (int i = 0; i < names.Length; i++)
            {
                Button b = Ui.GhostButton(names[i], 88, 34);
                b.Location = new Point(24 + i * 94, 13);
                int idx = i;
                b.Click += delegate { SwitchTab(idx); };
                toolbar.Controls.Add(b);
                _tabButtons.Add(b);
            }

            Button btnNew = Ui.PrimaryButton("新建事项", 104, 34);
            btnNew.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnNew.Click += delegate { ShowEditor(null); };
            toolbar.Controls.Add(btnNew);
            toolbar.Resize += delegate
            {
                btnNew.Location = new Point(toolbar.Width - btnNew.Width - 24, 13);
            };
            btnNew.Location = new Point(toolbar.Width - btnNew.Width - 24, 13);
        }

        private void BuildEditor()
        {
            Label l1 = Ui.FieldLabel("标题 *");
            l1.Location = new Point(16, 18);
            _editorCard.Controls.Add(l1);

            _fTitle = new TextBox();
            _fTitle.Font = Ui.F(9.5F);
            _fTitle.MaxLength = 50;
            _fTitle.Location = new Point(72, 14);
            _fTitle.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            _editorCard.Controls.Add(_fTitle);

            Label l2 = Ui.FieldLabel("重复规则");
            l2.Location = new Point(16, 56);
            _editorCard.Controls.Add(l2);

            _fRecur = new ComboBox();
            _fRecur.DropDownStyle = ComboBoxStyle.DropDownList;
            _fRecur.Items.AddRange(new object[] { "不重复", "每天", "每周", "每月" });
            _fRecur.Location = new Point(72, 52);
            _fRecur.Width = 100;
            _fRecur.SelectedIndexChanged += delegate { SyncRecurFields(); };
            _editorCard.Controls.Add(_fRecur);

            _fRecurLabel = Ui.FieldLabel("周几");
            _fRecurLabel.Location = new Point(186, 56);
            _editorCard.Controls.Add(_fRecurLabel);

            _fWeekday = new ComboBox();
            _fWeekday.DropDownStyle = ComboBoxStyle.DropDownList;
            _fWeekday.Items.AddRange(new object[] { "周一", "周二", "周三", "周四", "周五", "周六", "周日" });
            _fWeekday.Location = new Point(232, 52);
            _fWeekday.Width = 72;
            _editorCard.Controls.Add(_fWeekday);

            _fMonthday = new NumericUpDown();
            _fMonthday.Minimum = 1;
            _fMonthday.Maximum = 31;
            _fMonthday.Location = new Point(232, 52);
            _fMonthday.Width = 72;
            _editorCard.Controls.Add(_fMonthday);

            _fDateLabel = Ui.FieldLabel("日期 *");
            _fDateLabel.Location = new Point(330, 56);
            _editorCard.Controls.Add(_fDateLabel);

            _fDate = new DateTimePicker();
            _fDate.Format = DateTimePickerFormat.Custom;
            _fDate.CustomFormat = "yyyy-MM-dd";
            _fDate.Location = new Point(380, 52);
            _fDate.Width = 130;
            _editorCard.Controls.Add(_fDate);

            Label l3 = Ui.FieldLabel("时间");
            l3.Location = new Point(524, 56);
            _editorCard.Controls.Add(l3);

            _fTime = new TextBox();
            _fTime.Font = Ui.F(9.5F);
            _fTime.Location = new Point(566, 52);
            _fTime.Width = 90;
            _editorCard.Controls.Add(_fTime);

            Label l4 = Ui.FieldLabel("截止日期");
            l4.Location = new Point(16, 96);
            _editorCard.Controls.Add(l4);

            _fDue = new DateTimePicker();
            _fDue.Format = DateTimePickerFormat.Custom;
            _fDue.CustomFormat = "yyyy-MM-dd";
            _fDue.ShowCheckBox = true;
            _fDue.Checked = false;
            _fDue.Location = new Point(72, 92);
            _fDue.Width = 130;
            _editorCard.Controls.Add(_fDue);

            Label l5 = Ui.FieldLabel("优先级");
            l5.Location = new Point(216, 96);
            _editorCard.Controls.Add(l5);

            _fPriority = new ComboBox();
            _fPriority.DropDownStyle = ComboBoxStyle.DropDownList;
            _fPriority.Items.AddRange(new object[] { "P0 最高", "P1 高", "P2 中（默认）", "P3 低" });
            _fPriority.SelectedIndex = 2;
            _fPriority.Location = new Point(270, 92);
            _fPriority.Width = 118;
            _editorCard.Controls.Add(_fPriority);

            Label l6 = Ui.FieldLabel("备注");
            l6.Location = new Point(398, 96);
            _editorCard.Controls.Add(l6);

            _fNote = new TextBox();
            _fNote.Font = Ui.F(9.5F);
            _fNote.MaxLength = 200;
            _fNote.Location = new Point(444, 92);
            _fNote.Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right;
            _editorCard.Controls.Add(_fNote);

            Button save = Ui.PrimaryButton("保存", 96, 34);
            save.Location = new Point(72, 132);
            save.Click += delegate { SaveEditor(); };
            _editorCard.Controls.Add(save);

            Button cancel = Ui.GhostButton("取消", 72, 34);
            cancel.Location = new Point(180, 132);
            cancel.Click += delegate { HideEditor(); };
            _editorCard.Controls.Add(cancel);

            _fErr = new Label();
            _fErr.Font = Ui.F(8.5F);
            _fErr.ForeColor = Ui.Danger;
            _fErr.AutoSize = true;
            _fErr.Location = new Point(262, 140);
            _editorCard.Controls.Add(_fErr);

            _editorCard.Resize += delegate
            {
                _fTitle.Width = _editorCard.Width - 72 - 20;
                _fNote.Width = _editorCard.Width - 444 - 20;
            };
            _fRecur.SelectedIndex = 0;
        }

        private void SyncRecurFields()
        {
            int sel = _fRecur.SelectedIndex;
            bool showDate = (sel == 0);
            bool weekly = (sel == 2);
            bool monthly = (sel == 3);
            _fDateLabel.Visible = showDate;
            _fDate.Visible = showDate;
            _fRecurLabel.Visible = weekly || monthly;
            _fRecurLabel.Text = weekly ? "周几" : "每月几号";
            _fWeekday.Visible = weekly;
            _fMonthday.Visible = monthly;
        }

        private void SwitchTab(int idx)
        {
            _tab = idx == 0 ? "all" : idx == 1 ? "today" : "due";
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                Button b = _tabButtons[i];
                bool on = (i == idx);
                b.BackColor = on ? Ui.AccentSoft : Ui.Card;
                b.ForeColor = on ? Ui.Accent : Ui.Ink;
                b.Font = on ? Ui.F(9F, FontStyle.Bold) : Ui.F(9F);
            }
            Render();
        }

        private void ShowEditor(ScheduleItem it)
        {
            _editingId = it != null ? it.Id : -1;
            _fErr.Text = "";
            _fTitle.Text = it != null ? it.Title : "";
            int recurIdx = 0;
            if (it != null && it.IsRecurring)
            {
                if (it.RecurType == "daily") recurIdx = 1;
                else if (it.RecurType == "weekly") recurIdx = 2;
                else if (it.RecurType == "monthly") recurIdx = 3;
            }
            _fRecur.SelectedIndex = recurIdx;

            if (it != null && it.RecurType == "weekly")
            {
                int[] map = { 1, 2, 3, 4, 5, 6, 0 };
                for (int i = 0; i < map.Length; i++) if (map[i] == it.Weekday) { _fWeekday.SelectedIndex = i; break; }
                if (_fWeekday.SelectedIndex < 0) _fWeekday.SelectedIndex = 0;
            }
            else _fWeekday.SelectedIndex = 0;

            if (it != null && it.RecurType == "monthly" && it.Monthday >= 1 && it.Monthday <= 31)
                _fMonthday.Value = it.Monthday;
            else _fMonthday.Value = 1;

            DateTime? d = it != null ? ScriptEngine.ParseDate(it.Date) : null;
            _fDate.Value = d != null ? d.Value : DateTime.Today;
            _fDate.MinDate = new DateTime(2000, 1, 1);

            _fTime.Text = it != null ? (it.Time ?? "") : "";

            DateTime? due = it != null ? ScriptEngine.ParseDate(it.Due) : null;
            _fDue.Checked = due != null;
            if (due != null) _fDue.Value = due.Value;

            int pIdx = 2;
            if (it != null)
            {
                if (it.Priority == "P0") pIdx = 0;
                else if (it.Priority == "P1") pIdx = 1;
                else if (it.Priority == "P3") pIdx = 3;
            }
            _fPriority.SelectedIndex = pIdx;

            _fNote.Text = it != null ? (it.Note ?? "") : "";

            SyncRecurFields();
            _editorCard.Visible = true;
            _fTitle.Focus();
        }

        private void HideEditor()
        {
            _editorCard.Visible = false;
            _editingId = -1;
        }

        private void SaveEditor()
        {
            string title = _fTitle.Text.Trim();
            if (title.Length == 0) { _fErr.Text = "请填写标题"; return; }

            int recurIdx = _fRecur.SelectedIndex;
            string time = _fTime.Text.Trim();
            if (time.Length > 0 && !ScriptEngine.IsValidTime(time))
            {
                _fErr.Text = "时间格式应为 HH:MM，如 09:30"; return;
            }

            string dateStr = null;
            if (recurIdx == 0)
            {
                if (_fDate.Value.Date < DateTime.Today && _editingId < 0)
                {
                    _fErr.Text = "日期不能早于今天"; return;
                }
                dateStr = ScriptEngine.ToDateStr(_fDate.Value.Date);
            }

            string dueStr = null;
            if (_fDue.Checked)
            {
                dueStr = ScriptEngine.ToDateStr(_fDue.Value.Date);
                if (recurIdx == 0 && _fDue.Value.Date < _fDate.Value.Date)
                {
                    _fErr.Text = "截止日期不能早于发生日期"; return;
                }
            }

            string recur = "none";
            int weekday = 1, monthday = 1;
            if (recurIdx == 1) recur = "daily";
            else if (recurIdx == 2)
            {
                recur = "weekly";
                int[] map = { 1, 2, 3, 4, 5, 6, 0 };
                weekday = map[_fWeekday.SelectedIndex];
            }
            else if (recurIdx == 3)
            {
                recur = "monthly";
                monthday = (int)_fMonthday.Value;
            }

            string priority = "P2";
            if (_fPriority.SelectedIndex == 0) priority = "P0";
            else if (_fPriority.SelectedIndex == 1) priority = "P1";
            else if (_fPriority.SelectedIndex == 3) priority = "P3";

            ScheduleItem it = _editingId >= 0 ? _store.Find(_editingId) : null;
            if (it == null)
            {
                it = new ScheduleItem();
                it.Id = _store.NextId;
                _store.NextId++;
                _store.Items.Add(it);
            }
            it.Title = title;
            it.Date = dateStr;
            it.RecurType = recur;
            it.Weekday = weekday;
            it.Monthday = monthday;
            it.Time = time.Length > 0 ? time : null;
            it.Due = dueStr;
            it.Priority = priority;
            it.Note = _fNote.Text.Trim();

            _store.Save();
            HideEditor();
            Render();
        }

        private List<ScheduleItem> ItemsForTab()
        {
            List<ScheduleItem> all = new List<ScheduleItem>(_store.Items);
            if (_tab == "today")
            {
                List<ScheduleItem> r = new List<ScheduleItem>();
                foreach (ScheduleItem it in all)
                {
                    if (ScriptEngine.IsToday(it, DateTime.Today)) r.Add(it);
                }
                return r;
            }
            if (_tab == "due")
            {
                return ScriptEngine.DeadlinesIn(_store.Items, _store.Settings.WindowDays, DateTime.Today);
            }
            return all;
        }

        private void Render()
        {
            _list.SuspendLayout();
            _list.Controls.Clear();

            List<ScheduleItem> items = ItemsForTab();
            int inner = _list.ClientSize.Width - _list.Padding.Horizontal - 24;

            if (items.Count == 0)
            {
                Label empty = new Label();
                empty.Text = _tab == "due"
                    ? "提醒窗口内暂无 P0 / P1 的截止事项"
                    : "暂无事项，点击右上角“新建事项”录入";
                empty.Font = Ui.F(9.5F);
                empty.ForeColor = Ui.Muted;
                empty.AutoSize = false;
                empty.Width = Math.Max(300, inner);
                empty.Height = 120;
                empty.TextAlign = ContentAlignment.MiddleCenter;
                _list.Controls.Add(empty);
                _list.ResumeLayout();
                return;
            }

            List<KeyValuePair<DateTime, ScheduleItem>> dated =
                new List<KeyValuePair<DateTime, ScheduleItem>>();
            List<ScheduleItem> undated = new List<ScheduleItem>();

            foreach (ScheduleItem it in items)
            {
                DateTime? occ = ScriptEngine.NextOccurrence(it, DateTime.Today);
                if (occ != null) dated.Add(new KeyValuePair<DateTime, ScheduleItem>(occ.Value, it));
                else undated.Add(it);
            }

            dated.Sort(delegate (KeyValuePair<DateTime, ScheduleItem> a, KeyValuePair<DateTime, ScheduleItem> b)
            {
                int c = a.Key.CompareTo(b.Key);
                if (c != 0) return c;
                return string.CompareOrdinal(a.Value.Time ?? "99:99", b.Value.Time ?? "99:99");
            });
            undated.Sort(delegate (ScheduleItem a, ScheduleItem b)
            {
                DateTime? da = ScriptEngine.ParseDate(a.Due);
                DateTime? db = ScriptEngine.ParseDate(b.Due);
                long ka = da != null ? da.Value.Ticks : DateTime.MaxValue.Ticks;
                long kb = db != null ? db.Value.Ticks : DateTime.MaxValue.Ticks;
                return ka.CompareTo(kb);
            });

            int y = 8;
            DateTime today = DateTime.Today;
            string curLabel = null;

            foreach (KeyValuePair<DateTime, ScheduleItem> kv in dated)
            {
                DateTime d = kv.Key;
                string label;
                if (d.Date == today) label = "今天";
                else if (d.Date == today.AddDays(1)) label = "明天";
                else label = d.Month + "月" + d.Day + "日 " + ScriptEngine.WeekShortName(d);
                if (label != curLabel)
                {
                    curLabel = label;
                    y = AddGroupHeader(label, d.Date == today, y, inner);
                }
                y = AddItemRow(kv.Value, y, inner);
            }

            if (undated.Count > 0)
            {
                y = AddGroupHeader("无固定日期 · 按截止日排列", false, y, inner);
                foreach (ScheduleItem it in undated)
                {
                    y = AddItemRow(it, y, inner);
                }
            }

            _list.ResumeLayout();
        }

        private int AddGroupHeader(string text, bool today, int y, int width)
        {
            Panel p = new Panel();
            p.Location = new Point(0, y);
            p.Width = width;
            p.Height = 36;
            p.BackColor = Ui.Bg;

            Label l = new Label();
            l.Text = today ? "● " + text : text;
            l.AutoSize = false;
            l.Width = width;
            l.Height = 30;
            l.Location = new Point(4, 4);
            l.Font = Ui.F(9.5F, FontStyle.Bold);
            l.ForeColor = today ? Ui.Accent : Ui.Muted;
            p.Controls.Add(l);
            _list.Controls.Add(p);
            return y + p.Height;
        }

        private int AddItemRow(ScheduleItem it, int y, int width)
        {
            Panel p = new Panel();
            p.Location = new Point(0, y);
            p.Width = width;
            p.Height = 58;
            p.BackColor = Ui.Card;
            p.Paint += delegate (object s, PaintEventArgs e) { Ui.PaintItemBorder(s, e); };

            Label badge = new Label();
            badge.Text = it.Priority;
            badge.Width = 44;
            badge.Height = 24;
            badge.Location = new Point(12, 17);
            badge.BackColor = Ui.PriorityColor(it.Priority);
            badge.ForeColor = Color.White;
            badge.Font = Ui.F(8.5F, FontStyle.Bold);
            badge.TextAlign = ContentAlignment.MiddleCenter;
            p.Controls.Add(badge);

            Label title = new Label();
            title.Text = it.Title;
            title.AutoSize = false;
            title.Width = Math.Max(160, width - 64 - 250);
            title.Height = 26;
            title.Location = new Point(66, 9);
            title.Font = Ui.F(9.5F, it.Done ? FontStyle.Strikeout : FontStyle.Bold);
            title.ForeColor = it.Done ? Ui.Muted : Ui.Ink;
            title.AutoEllipsis = true;
            p.Controls.Add(title);

            Label meta = new Label();
            List<string> parts = new List<string>();
            if (!string.IsNullOrEmpty(it.Time)) parts.Add(it.Time);
            if (it.IsRecurring)
            {
                if (it.RecurType == "daily") parts.Add("每天");
                else if (it.RecurType == "weekly")
                    parts.Add("每周" + new string[] { "日", "一", "二", "三", "四", "五", "六" }[it.Weekday]);
                else if (it.RecurType == "monthly") parts.Add("每月" + it.Monthday + "日");
            }
            DateTime? due = ScriptEngine.ParseDate(it.Due);
            if (due != null)
            {
                int left = (int)(due.Value.Date - DateTime.Today).TotalDays;
                string leftStr = left <= 0 ? "今天截止" : "剩 " + left + " 天";
                parts.Add(due.Value.Month + "月" + due.Value.Day + "日截止 · " + leftStr);
            }
            if (!string.IsNullOrEmpty(it.Note)) parts.Add("备注：" + it.Note);
            meta.Text = string.Join("  ·  ", parts.ToArray());
            meta.AutoSize = false;
            meta.Width = Math.Max(160, width - 64 - 250);
            meta.Height = 18;
            meta.Location = new Point(66, 34);
            meta.Font = Ui.F(8.25F);
            meta.ForeColor = Ui.Muted;
            meta.AutoEllipsis = true;
            p.Controls.Add(meta);

            Button bDel = Ui.DangerGhostButton("删除", 64, 30);
            Button bEdit = Ui.GhostButton("编辑", 56, 30);
            Button bDone = Ui.GhostButton(it.Done ? "恢复" : "完成", 56, 30);

            bDel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            bEdit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            bDone.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            bool confirming = it.ConfirmUntil > DateTime.Now;
            if (confirming)
            {
                bDel.Text = "确认删除";
                bDel.Width = 88;
                bDel.BackColor = Ui.Danger;
                bDel.ForeColor = Color.White;
                bDel.FlatAppearance.MouseOverBackColor = Ui.Danger;
            }
            bDel.Location = new Point(width - bDel.Width - 12, 14);
            bEdit.Location = new Point(width - bDel.Width - 12 - 62, 14);
            bDone.Location = new Point(width - bDel.Width - 12 - 62 - 62, 14);

            int id = it.Id;
            bDel.Click += delegate { OnDelete(id); };
            bEdit.Click += delegate { OnEdit(id); };
            bDone.Click += delegate { OnDone(id); };

            p.Controls.Add(bDone);
            p.Controls.Add(bEdit);
            p.Controls.Add(bDel);

            _list.Controls.Add(p);
            return y + p.Height + 8;
        }

        private void OnDelete(int id)
        {
            ScheduleItem it = _store.Find(id);
            if (it == null) return;
            if (it.ConfirmUntil > DateTime.Now)
            {
                _store.Delete(id);
                _store.Save();
                Render();
                _confirmTimer.Stop();
            }
            else
            {
                foreach (ScheduleItem x in _store.Items) x.ConfirmUntil = DateTime.MinValue;
                it.ConfirmUntil = DateTime.Now.AddSeconds(3);
                _confirmTimer.Start();
                Render();
            }
        }

        private void OnEdit(int id)
        {
            ScheduleItem it = _store.Find(id);
            if (it == null) return;
            ShowEditor(it);
        }

        private void OnDone(int id)
        {
            ScheduleItem it = _store.Find(id);
            if (it == null) return;
            it.Done = !it.Done;
            it.ConfirmUntil = DateTime.MinValue;
            _store.Save();
            Render();
        }
    }
}
