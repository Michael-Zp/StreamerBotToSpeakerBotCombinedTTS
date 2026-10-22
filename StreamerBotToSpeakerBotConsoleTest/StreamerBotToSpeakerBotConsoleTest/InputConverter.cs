namespace StreamerBotToSpeakerBotConsoleTest
{
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
}
