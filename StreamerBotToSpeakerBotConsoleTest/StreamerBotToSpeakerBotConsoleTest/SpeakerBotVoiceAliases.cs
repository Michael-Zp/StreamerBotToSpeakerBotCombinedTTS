using Newtonsoft.Json;

namespace StreamerBotToSpeakerBotConsoleTest
{
    public class SpeakerBotVoiceAliases
    {
#pragma warning disable IDE1006 // Naming Styles
        private struct Voice
        {
            public string name { get; set; }

            public float rate { get; set; }

            public float volume { get; set; }

            public float pitch { get; set; }
        }

        private struct VoiceAlias
        {
            public string id { get; set; }

            public string? audioDevice { get; set; }

            public string name { get; set; }

            public Voice[] voices;
        }

        private struct VoiceAliases
        {
            public int version { get; set; }

            public string t { get; set; }

            public VoiceAlias[] aliases;
        }
#pragma warning restore IDE1006 // Naming Styles

        private object m_voiceAliasesLock = new();
        private VoiceAliases m_voiceAliases;
        private Thread m_refreshVoicesThread;

        private static SpeakerBotVoiceAliases? s_instance;

        private SpeakerBotVoiceAliases() { }

        public static SpeakerBotVoiceAliases GetOrCreateInstance(string path)
        {
            if (s_instance == null)
            {
                s_instance = new SpeakerBotVoiceAliases();
                s_instance.Init(path);
            }

            return s_instance;
        }

        public bool DoesVoiceExist(string speaker)
        {
            lock (m_voiceAliasesLock)
            {
                foreach (VoiceAlias voiceAlias in m_voiceAliases.aliases)
                {
                    if (string.Equals(speaker, voiceAlias.name, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void Init(string path)
        {
            ReadCurrentAliases(path);

            m_refreshVoicesThread = new(() =>
            {
                while (true)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(15));
                    try
                    {
                        ReadCurrentAliases(path);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            });
            m_refreshVoicesThread.Start();
        }

        private void ReadCurrentAliases(string path)
        {
            string content = "{}";
            {
                using FileStream fs = new(path, FileMode.Open, FileAccess.Read);
                using StreamReader sr = new(fs);
                content = sr.ReadToEnd();
            }

            lock (m_voiceAliasesLock)
            {
                m_voiceAliases = JsonConvert.DeserializeObject<VoiceAliases>(content);
            }

        }
    }
}
