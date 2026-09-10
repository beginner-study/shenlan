using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace DeepBlue
{
    public class ScheduleItem
    {
        public int Id;
        public string Title = "";
        public string Date;
        public string RecurType = "none";
        public int Weekday;
        public int Monthday = 1;
        public string Time;
        public string Due;
        public string Priority = "P2";
        public string Note = "";
        public bool Done;

        [ScriptIgnore]
        public DateTime ConfirmUntil = DateTime.MinValue;

        public bool IsRecurring
        {
            get { return RecurType != null && RecurType != "none"; }
        }
    }

    public class AppSettings
    {
        public string VoiceName = "";
        public double Rate = 1.0;
        public int WindowDays = 7;
        public bool SecDate = true;
        public bool SecToday = true;
        public bool SecDue = true;
        public bool WeatherOn = false;
        public string WeatherCity = "";
        public double WeatherLat = 0;
        public double WeatherLon = 0;
        public string WeatherSource = "open-meteo";
        public string QwHost = "";
        public string QwKey = "";
        public string QwLocation = "";
    }

    public static class AppInfo
    {
        public const string Name = "深蓝";
        public const string NameEn = "DeepBlue";
        public const string Version = "1.3.0";
        public const string RepoUrl = "https://github.com/beginner-study/shenlan";
        public const string SampleVoiceText = "你好，我是深蓝，这是语音试听。";
    }

    public class Store
    {
        public List<ScheduleItem> Items = new List<ScheduleItem>();
        public AppSettings Settings = new AppSettings();
        public int NextId = 1;

        private class DataFile
        {
            public List<ScheduleItem> items;
            public int nextId;
        }

        public static string OverrideDir = null;

        public static string DataDir
        {
            get
            {
                if (OverrideDir != null) return OverrideDir;
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "DeepBlue");
            }
        }

        private static string DataFilePath
        {
            get { return Path.Combine(DataDir, "data.json"); }
        }

        private static string SettingsFilePath
        {
            get { return Path.Combine(DataDir, "settings.json"); }
        }

        public static Store Load()
        {
            Store store = new Store();
            try
            {
                if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);
            }
            catch (Exception) { }

            JavaScriptSerializer ser = new JavaScriptSerializer();

            try
            {
                if (File.Exists(DataFilePath))
                {
                    DataFile df = ser.Deserialize<DataFile>(File.ReadAllText(DataFilePath));
                    if (df != null)
                    {
                        if (df.items != null)
                        {
                            foreach (ScheduleItem it in df.items) store.Items.Add(Normalize(it));
                        }
                        if (df.nextId > 0) store.NextId = df.nextId;
                    }
                }
            }
            catch (Exception)
            {
                try
                {
                    if (File.Exists(DataFilePath))
                        File.Move(DataFilePath, DataFilePath + ".corrupt");
                }
                catch (Exception) { }
            }

            foreach (ScheduleItem it in store.Items)
            {
                if (it.Id >= store.NextId) store.NextId = it.Id + 1;
            }

            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    AppSettings s = ser.Deserialize<AppSettings>(File.ReadAllText(SettingsFilePath));
                    if (s != null) store.Settings = NormalizeSettings(s);
                }
            }
            catch (Exception) { }

            return store;
        }

        private static ScheduleItem Normalize(ScheduleItem it)
        {
            if (it == null) return null;
            if (it.Title == null) it.Title = "";
            if (it.RecurType == null || it.RecurType.Length == 0) it.RecurType = "none";
            if (it.Priority == null || it.Priority.Length == 0) it.Priority = "P2";
            if (it.Note == null) it.Note = "";
            if (it.Date == null) it.Date = "";
            if (it.Time == null) it.Time = "";
            if (it.Due == null) it.Due = "";
            return it;
        }

        private static AppSettings NormalizeSettings(AppSettings s)
        {
            if (s.VoiceName == null) s.VoiceName = "";
            if (s.WeatherCity == null) s.WeatherCity = "";
            if (s.QwHost == null) s.QwHost = "";
            if (s.QwKey == null) s.QwKey = "";
            if (s.QwLocation == null) s.QwLocation = "";
            if (s.WeatherSource != "qweather") s.WeatherSource = "open-meteo";
            s.QwHost = s.QwHost.Trim().TrimEnd('/').Replace("https://", "").Replace("http://", "");
            if (s.Rate < 0.5) s.Rate = 0.5;
            if (s.Rate > 2.0) s.Rate = 2.0;
            if (s.WindowDays < 1) s.WindowDays = 1;
            if (s.WindowDays > 30) s.WindowDays = 30;
            return s;
        }

        public static string LastSaveError = null;

        public void Save()
        {
            LastSaveError = null;
            try
            {
                if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);
                JavaScriptSerializer ser = new JavaScriptSerializer();
                DataFile df = new DataFile();
                df.items = Items;
                df.nextId = NextId;
                File.WriteAllText(DataFilePath, ser.Serialize(df));
                File.WriteAllText(SettingsFilePath, ser.Serialize(Settings));
            }
            catch (Exception ex)
            {
                LastSaveError = ex.GetType().Name + ": " + ex.Message;
            }
        }

        public ScheduleItem Find(int id)
        {
            foreach (ScheduleItem it in Items)
            {
                if (it.Id == id) return it;
            }
            return null;
        }

        public void Delete(int id)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i].Id == id) { Items.RemoveAt(i); return; }
            }
        }
    }
}
