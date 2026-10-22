#define STREAMERBOT

using System;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading;
using System.IO;

public class CPHInline
{
    public bool Execute()
    {
#if STREAMERBOT
        if (!CPH.TryGetArg("SaveSpeakerBotTtsAudioToFileDirectoryPath", out string ttsOutputPath))
        {
            CPH.LogInfo("Please set 'SaveSpeakerBotTtsAudioToFileDirectoryPath' argument to your folder where Speaker.bot saves its TTS output files. Configure that under Settings/General/Save TTS audio to file.");
            return true;
        }
        else
        {
            CPH.LogInfo("Set ttsOutputPath to " + ttsOutputPath);
        }

        if (!CPH.TryGetArg("PathToSpeakerBotVoicealiasesDatFile", out string pathToVoices))
        {
            CPH.LogInfo("Please set 'PathToSpeakerBotVoicealiasesDatFile' argument to your Speaker.bot voicealiases.dat file. Normally found in <Speaker.bot Directory>/data.");
            return true;
        }
        else
        {
            CPH.LogInfo("Set pathToVoices to " + pathToVoices);
        }

        if (!CPH.TryGetArg("PathToSoundEffectsFolder", out string pathToSoundEffectsFolder))
        {
            CPH.LogInfo("Please set 'PathToSoundEffectsFolder' argument to your folder where you keep your sound effects.");
            return true;
        }
        else
        {
            CPH.LogInfo("Set pathToSoundEffectsFolder to " + pathToSoundEffectsFolder);
        }
#else
        string ttsOutputPath = "J:/Programming/GitHub/StreamerBotToSpeakerBot/SavedTTS";
        string pathToVoices = "J:/Programming/GitHub/StreamerBotToSpeakerBot/Speaker.bot-x64-0.1.7/data/voicealiases.dat";
        string pathToSoundEffectsFolder = "J:/Programming/GitHub/StreamerBotToSpeakerBot/SoundEffects";
#endif

#if !STREAMERBOT
#endif
        SpeakerBotVoiceAliases speakerBotVoiceAliases = SpeakerBotVoiceAliases.GetOrCreateInstance(pathToVoices);
        SpeakerBotSynchronizer speakerBotSynchronizer = SpeakerBotSynchronizer.GetOrCreateInstance(ttsOutputPath);
        SoundEffects soundEffects = SoundEffects.GetOrCreateInstance(pathToSoundEffectsFolder);

        // your main code goes here
        string currentSpeaker = "Frost";
#if STREAMERBOT
        if (!CPH.TryGetArg("message", out string inputText))
        {
            CPH.LogInfo("NO-INPUT-TEXT");
            return true;
        }
#else
        string inputText = "Hi chat, (Skootish) this is (doesNotExist) Skootish btw. (Jason) and someone else. (Peng) (boom)";
#endif
        var message = InputConverter.FromString(inputText, speakerBotVoiceAliases, soundEffects);

        if (message.expressions.Count == 0)
        {
#if STREAMERBOT
            return true;
#else
            return 0;
#endif
        }

        for (int i = 0; i < message.expressions.Count; i++)
        {
            if (speakerBotVoiceAliases.DoesVoiceExist(message.expressions[i].Value))
            {
                continue;
            }

            if (soundEffects.TryGetSoundEffectFile(message.expressions[i].Value, out string notNeeded))
            {
                continue;
            }

            message.expressions[i].Type = InputConverter.ExpressionType.Text;
        }

        for (int i = 0; i < message.expressions.Count - 1;)
        {
            bool didRemoveNext = false;
            if (message.expressions[i].Type == InputConverter.ExpressionType.Text)
            {
                if (message.expressions[i + 1].Type == InputConverter.ExpressionType.Text)
                {
                    message.expressions[i].Value = $"{message.expressions[i].Value} {message.expressions[i + 1].Value}";
                    message.expressions.RemoveAt(i + 1);
                }
            }

            if (!didRemoveNext)
            {
                i++;
            }
        }


        foreach (var expr in message.expressions)
        {
            if (expr.Type == InputConverter.ExpressionType.Text)
            {
#if STREAMERBOT
                speakerBotSynchronizer.SendSynced(currentSpeaker, expr.Value, (spk, msg) => { CPH.TtsSpeak(spk, msg, true); });
#else
                speakerBotSynchronizer.SendSynced(currentSpeaker, expr.Value);
#endif
            }
            else
            {
                if (speakerBotVoiceAliases.DoesVoiceExist(expr.Value))
                {
                    currentSpeaker = expr.Value;
                    continue;
                }

                if (soundEffects.TryGetSoundEffectFile(expr.Value, out string? filePath))
                {
                    if (filePath == null)
                    {
                        continue;
                    }
#if STREAMERBOT
                    CPH.PlaySound(filePath, finishBeforeContinuing: true);
#else
                    if (OperatingSystem.IsWindows())
                    {
                        using SoundPlayer player = new(filePath);
                        player.PlaySync();
                    }
#endif
                    continue;
                }

#if STREAMERBOT
                speakerBotSynchronizer.SendSynced(currentSpeaker, expr.Value, (spk, msg) => { CPH.TtsSpeak(spk, msg, true); });
#else
                speakerBotSynchronizer.SendSynced(currentSpeaker, expr.Value);
#endif
            }
        }

        return true;
    }
}

public class InputConverter
{
    public enum ExpressionType
    {
        Text,
        Effect
    }

    public class TtsExpression(ExpressionType type, string value)
    {
        public ExpressionType Type { get; set; } = type;

        public string Value { get; set; } = value;
    }

    public class TtsMessage
    {
        public List<TtsExpression> expressions = [];
    }

    public static TtsMessage FromString(string text, SpeakerBotVoiceAliases speakerBotVoiceAliases, SoundEffects soundEffects)
    {
        text = text.Trim();

        List<(int, int, string)> effectPositions = [];

        int lastPos = -1;
        int openParPos = -1;
        for (int i = 0; i < 5000; i++) // Hard cap the limit of tokens parsed to be safe from unexpected runaways
        {
            lastPos++;

            if (lastPos >= text.Length)
            {
                break;
            }

            var foundParAtLocally = text.Substring(lastPos).IndexOfAny(['(', ')']);

            // End of text
            if (foundParAtLocally == -1)
            {
                break;
            }

            var foundParAt = foundParAtLocally + lastPos;

            // Found opening paranthesis
            if (text[foundParAt] == '(')
            {
                openParPos = foundParAt;
                lastPos = foundParAt;
                continue;
            }

            if (openParPos != -1 && text[foundParAt] == ')')
            {
                // Empty pars, just close and continue
                if (openParPos + 1 == foundParAt)
                {
                    openParPos = -1;
                    lastPos = foundParAt;
                    continue;
                }

                string expressionStr = text.Substring(openParPos + 1, foundParAt - openParPos - 1).Trim();

                // Pars filled with white spaces, just close and continue
                if (string.IsNullOrEmpty(expressionStr))
                {
                    openParPos = -1;
                    lastPos = foundParAt;
                    continue;
                }

                effectPositions.Add((openParPos, foundParAt, expressionStr));

                openParPos = -1;
                lastPos = foundParAt;
            }
        }

        TtsMessage msg = new();

        // Special case of no expressions
        if (effectPositions.Count == 0)
        {
            string textStr = text.ToString().Trim();

            if (!string.IsNullOrEmpty(textStr))
            {
                msg.expressions.Add(new TtsExpression(ExpressionType.Text, textStr));
            }

            return msg;
        }

        int textStartPos = 0;
        foreach (var effectPos in effectPositions)
        {
            // Either skip text if the text:
            // starts with an effect "(Frost) Landlord here"
            // has no text between effects "Hello my fellow kids (spiderman)(fart) bye"
            if (textStartPos < effectPos.Item1)
            {
                string textStr = text.Substring(textStartPos, effectPos.Item1 - textStartPos).Trim();

                if (!string.IsNullOrEmpty(textStr))
                {
                    msg.expressions.Add(new TtsExpression(ExpressionType.Text, textStr));
                }
            }

            msg.expressions.Add(new TtsExpression(ExpressionType.Effect, effectPos.Item3));

            textStartPos = effectPos.Item2 + 1;
        }

        if (textStartPos < text.Length)
        {
            string textStr = text.Substring(textStartPos).Trim();

            if (!string.IsNullOrEmpty(textStr))
            {
                msg.expressions.Add(new TtsExpression(ExpressionType.Text, textStr));
            }
        }

        CleanMessage(ref msg, speakerBotVoiceAliases, soundEffects);

        return msg;
    }

    private static void CleanMessage(ref TtsMessage rawMessage, SpeakerBotVoiceAliases speakerBotVoiceAliases, SoundEffects soundEffects)
    {
        for (int i = 0; i < rawMessage.expressions.Count - 1;)
        {
            if (rawMessage.expressions[i].Type == ExpressionType.Text)
            {
                if (rawMessage.expressions[i + 1].Type == ExpressionType.Text)
                {
                    rawMessage.expressions[i].Value = $"{rawMessage.expressions[i].Value} {rawMessage.expressions[i + 1].Value}";
                    rawMessage.expressions.RemoveAt(i + 1);
                    continue;
                }

                if (speakerBotVoiceAliases.DoesVoiceExist(rawMessage.expressions[i + 1].Value))
                {
                    i += 2;
                    continue;
                }

                if (soundEffects.TryGetSoundEffectFile(rawMessage.expressions[i + 1].Value, out string notNeeded))
                {
                    i += 2;
                    continue;
                }

                rawMessage.expressions[i].Value = $"{rawMessage.expressions[i].Value} ({rawMessage.expressions[i + 1].Value})";
                rawMessage.expressions.RemoveAt(i + 1);
                continue;
            }
            else
            {
                if (speakerBotVoiceAliases.DoesVoiceExist(rawMessage.expressions[i].Value))
                {
                    i++;
                    continue;
                }

                if (soundEffects.TryGetSoundEffectFile(rawMessage.expressions[i].Value, out string notNeeded))
                {
                    i++;
                    continue;
                }

                rawMessage.expressions[i].Type = ExpressionType.Text;
                rawMessage.expressions[i].Value = $"({rawMessage.expressions[i].Value})";
            }
        }
    }
}

public class SpeakerBotVoiceAliases
{
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

#if STREAMERBOT
    public void SendSynced(string speaker, string message, Action<string, string> speak)
#else
    public void SendSynced(string speaker, string message)
#endif
    {
#if !STREAMERBOT
        SpeakerBotAPI speakerBotApi = SpeakerBotAPI.GetOrCreateInstance();
#endif

        DateTime beforeCall = DateTime.UtcNow;

#if STREAMERBOT
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