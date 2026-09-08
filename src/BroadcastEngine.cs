using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Speech.Synthesis;

namespace DeepBlue
{
    public class BroadcastEngine
    {
        private SpeechSynthesizer _synth;
        private List<string> _sents;
        private int _idx;
        private bool _active;

        public event Action<int> SentenceStarted;
        public event Action Finished;

        public bool IsPlaying
        {
            get { return _active; }
        }

        public BroadcastEngine()
        {
        }

        public static List<string> ListVoiceNames()
        {
            List<string> names = new List<string>();
            try
            {
                using (SpeechSynthesizer s = new SpeechSynthesizer())
                {
                    foreach (InstalledVoice v in s.GetInstalledVoices())
                    {
                        if (v.Enabled) names.Add(v.VoiceInfo.Name);
                    }
                }
            }
            catch (Exception) { }
            return names;
        }

        public static bool HasChineseVoice()
        {
            try
            {
                using (SpeechSynthesizer s = new SpeechSynthesizer())
                {
                    foreach (InstalledVoice v in s.GetInstalledVoices())
                    {
                        if (!v.Enabled) continue;
                        if (v.VoiceInfo.Culture != null &&
                            v.VoiceInfo.Culture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }
            catch (Exception) { }
            return false;
        }

        public void Play(List<string> sentences, string voiceName, double rate)
        {
            Stop();
            if (sentences == null || sentences.Count == 0) return;

            _synth = new SpeechSynthesizer();

            try
            {
                bool picked = false;
                foreach (InstalledVoice v in _synth.GetInstalledVoices())
                {
                    if (!v.Enabled) continue;
                    if (v.VoiceInfo.Name == voiceName)
                    {
                        _synth.SelectVoice(v.VoiceInfo.Name);
                        picked = true;
                        break;
                    }
                }
                if (!picked)
                {
                    foreach (InstalledVoice v in _synth.GetInstalledVoices())
                    {
                        if (!v.Enabled) continue;
                        if (v.VoiceInfo.Culture != null &&
                            v.VoiceInfo.Culture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
                        {
                            _synth.SelectVoice(v.VoiceInfo.Name);
                            picked = true;
                            break;
                        }
                    }
                }
                if (!picked)
                {
                    ReadOnlyCollection<InstalledVoice> all = _synth.GetInstalledVoices();
                    if (all.Count > 0) _synth.SelectVoice(all[0].VoiceInfo.Name);
                }
            }
            catch (Exception) { }

            double r = rate;
            if (r < 0.5) r = 0.5;
            if (r > 2.0) r = 2.0;
            int synthRate = (int)Math.Round((r - 1.0) * 10.0);
            if (synthRate < -10) synthRate = -10;
            if (synthRate > 10) synthRate = 10;
            _synth.Rate = synthRate;
            _synth.Volume = 100;

            _synth.SpeakCompleted += OnSpeakCompleted;
            _sents = sentences;
            _idx = 0;
            _active = true;
            SpeakCurrent();
        }

        private void SpeakCurrent()
        {
            Action<int> h = SentenceStarted;
            if (h != null) h(_idx);
            _synth.SpeakAsync(_sents[_idx]);
        }

        private void OnSpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            if (!_active) return;
            _idx++;
            if (_idx < _sents.Count)
            {
                SpeakCurrent();
            }
            else
            {
                _active = false;
                Action h = Finished;
                if (h != null) h();
            }
        }

        public void Pause()
        {
            if (_synth != null && _active)
            {
                try { _synth.Pause(); } catch (Exception) { }
            }
        }

        public void Resume()
        {
            if (_synth != null && _active)
            {
                try { _synth.Resume(); } catch (Exception) { }
            }
        }

        public void Stop()
        {
            if (_synth != null)
            {
                _active = false;
                try { _synth.SpeakCompleted -= OnSpeakCompleted; } catch (Exception) { }
                try { _synth.SpeakAsyncCancelAll(); } catch (Exception) { }
                try { _synth.Dispose(); } catch (Exception) { }
                _synth = null;
            }
            _active = false;
        }
    }
}
