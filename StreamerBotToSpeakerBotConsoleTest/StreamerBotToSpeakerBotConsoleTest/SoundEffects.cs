using Newtonsoft.Json;
using System.Media;

namespace StreamerBotToSpeakerBotConsoleTest
{
    public class SoundEffects
    {
        private struct SoundEffect
        {
            public string name;
            public string pathToFile;
        }

        private object m_soundEffectLock = new();
        private List<SoundEffect> m_soundEffects = new();
        private Thread m_refreshSoundEffectsThread;

        private static SoundEffects? s_instance;

        private SoundEffects() { }

        public static SoundEffects GetOrCreateInstance(string folderPath)
        {
            if (s_instance == null)
            {
                s_instance = new SoundEffects();
                s_instance.Init(folderPath);
            }

            return s_instance;
        }

        public bool TryGetSoundEffectFile(string effectName, out string? filePath)
        {
            lock (m_soundEffectLock)
            {
                foreach (var soundEffect in m_soundEffects)
                {
                    if (string.Equals(soundEffect.name, effectName, StringComparison.OrdinalIgnoreCase))
                    {
                        FileInfo fi = new(soundEffect.pathToFile);
                        if (!fi.Exists)
                        {
                            filePath = null;
                            return false;
                        }
                        filePath = fi.FullName;
                        return true;
                    }
                }
            }

            filePath = null;
            return false;
        }

        private void Init(string folderPath)
        {
            ReadAndUpdateCurrentSoundEffects(folderPath);

            m_refreshSoundEffectsThread = new(() =>
            {
                while (true)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(15));
                    try
                    {
                        ReadAndUpdateCurrentSoundEffects(folderPath);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            });
            m_refreshSoundEffectsThread.Start();
        }

        private void ReadAndUpdateCurrentSoundEffects(string folderPath)
        {
            DirectoryInfo directoryInfo = new(folderPath);

            if (!directoryInfo.Exists)
            {
                return;
            }

            List<SoundEffect> tempSoundEffects = new();
            foreach (var file in directoryInfo.EnumerateFiles())
            {
                tempSoundEffects.Add(new SoundEffect()
                {
                    name = Path.GetFileNameWithoutExtension(file.Name),
                    pathToFile = file.FullName,
                });
            }

            lock (m_soundEffectLock)
            {
                m_soundEffects.Clear();
                m_soundEffects.AddRange(tempSoundEffects);
            }
        }
    }
}
