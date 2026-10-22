using System.Diagnostics.CodeAnalysis;
using System.Media;

namespace StreamerBotToSpeakerBotConsoleTest
{
    internal class Program
    {
        static int Main()
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
            string ttsOutputPath = "J:/Programming/GitHub/StreamerBotToSpeakerBotCombinedTts/SavedTTS";
            string pathToVoices = "J:/Programming/GitHub/StreamerBotToSpeakerBotCombinedTts/Speaker.bot-x64-0.1.7/data/voicealiases.dat";
            string pathToSoundEffectsFolder = "J:/Programming/GitHub/StreamerBotToSpeakerBotCombinedTts/SoundEffects";
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

            for(int i = 0; i < message.expressions.Count; i++)
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

            return 0;
        }
    }
}
