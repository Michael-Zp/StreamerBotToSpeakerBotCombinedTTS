namespace StreamerBotToSpeakerBotConsoleTest
{
    public class SpeakerBotSynchronizer
    {
        private string m_ttsOutputPath;

        private SpeakerBotSynchronizer(string ttsOutputPath)
        { 
            m_ttsOutputPath = ttsOutputPath;
        }

        private static SpeakerBotSynchronizer? s_instance;


        public static SpeakerBotSynchronizer GetOrCreateInstance(string ttsOutputPath)
        {
            s_instance ??= new SpeakerBotSynchronizer(ttsOutputPath);
            return s_instance;
        }

#if STREAMBOT
        public void SendSynced(Action<string, string> speak, string speaker, string message)
#else
        public void SendSynced(string speaker, string message)
#endif
        {
#if !STREAMBOT
            SpeakerBotAPI speakerBotApi = SpeakerBotAPI.GetOrCreateInstance();
#endif

            DateTime beforeCall = DateTime.UtcNow;

#if STREAMBOT
            speak(speaker, message);
#else
            speakerBotApi.Speak(speaker, message);
#endif

            DirectoryInfo di = new(m_ttsOutputPath);

            string? filePath = null;
            for (int i = 0; i < 3; i++)
            {
                foreach (var file in di.EnumerateFiles())
                {
                    if (file.CreationTimeUtc > beforeCall)
                    {
                        filePath = file.FullName;
                        break;
                    }
                }

                if (filePath != null)
                {
                    break;
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(100));
            }

            TimeSpan ttsDuration;
            if (filePath == null)
            {
                string testString = "This is a very long message and I should not see anything in the logs until it is done";
                double testLength = 6.33; // Not good but better than nothing I guess
                double ratio = message.Length / testString.Length;
                ttsDuration = TimeSpan.FromSeconds(testLength * ratio) + TimeSpan.FromSeconds(2);
            }
            else
            {
                byte[] allBytes = File.ReadAllBytes(filePath);
                int byterate = BitConverter.ToInt32(new[] { allBytes[28], allBytes[29], allBytes[30], allBytes[31] }, 0);
                double duration = (double)(allBytes.Length - 8) / (double)byterate;
                ttsDuration = TimeSpan.FromSeconds(duration) + TimeSpan.FromMilliseconds(200);
            }

            TimeSpan timeToSleep = ttsDuration - (DateTime.UtcNow - beforeCall);

            if (timeToSleep > TimeSpan.Zero)
            {
                Thread.Sleep(timeToSleep);
            }
        }
    }
}
