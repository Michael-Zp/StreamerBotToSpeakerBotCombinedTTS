using StreamerBotToSpeakerBotConsoleTest;

using ExpressionType = StreamerBotToSpeakerBotConsoleTest.InputConverter.ExpressionType;
using TtsExpression = StreamerBotToSpeakerBotConsoleTest.InputConverter.TtsExpression;
using TtsMessage = StreamerBotToSpeakerBotConsoleTest.InputConverter.TtsMessage;

namespace Tests
{
    [TestClass]
    public sealed class InputConverterTests
    {
        [TestMethod]
        public void TestEmpty()
        {
            (var aliases, var effects) = GetEnvStuff();
            TtsMessage msg = InputConverter.FromString("", aliases, effects);

            Assert.IsEmpty(msg.expressions);
        }

        [TestMethod]
        public void TestOnlyText()
        {
            (var aliases, var effects) = GetEnvStuff();
            string msgStr = "This is a text";
            TtsMessage msg = InputConverter.FromString(msgStr, aliases, effects);

            Assert.IsNotEmpty(msg.expressions);
            CheckTtsExpressions(
                [
                    new TtsExpression(ExpressionType.Text, msgStr)
                ],
                msg.expressions);
        }

        [TestMethod]
        public void TestOnlyEffect()
        {
            (var aliases, var effects) = GetEnvStuff();
            string sfxStr = "(boom)";
            TtsMessage msg = InputConverter.FromString(sfxStr, aliases, effects);

            Assert.IsNotEmpty(msg.expressions);
            CheckTtsExpressions(
                [
                    new TtsExpression(ExpressionType.Effect, "boom")
                ],
                msg.expressions);
        }

        [TestMethod]
        public void TestTextEffectTextEffectText()
        {
            (var aliases, var effects) = GetEnvStuff();
            string msgStr = "Hi Frost, (Skootish) deez nuts (Peng) bye";
            TtsMessage msg = InputConverter.FromString(msgStr, aliases, effects);

            Assert.IsNotEmpty(msg.expressions);
            CheckTtsExpressions(
                [
                    new TtsExpression(ExpressionType.Text, "Hi Frost,"),
                    new TtsExpression(ExpressionType.Effect, "Skootish"),
                    new TtsExpression(ExpressionType.Text, "deez nuts"),
                    new TtsExpression(ExpressionType.Effect, "Peng"),
                    new TtsExpression(ExpressionType.Text, "bye")
                ],
                msg.expressions);
        }

        [TestMethod]
        public void TestClean()
        {
            (var aliases, var effects) = GetEnvStuff();
            string msgStr = "Hi Frost, (DoesNotExist) deez nuts (Peng)(IsALie) bye";
            TtsMessage msg = InputConverter.FromString(msgStr, aliases, effects);

            Assert.IsNotEmpty(msg.expressions);
            CheckTtsExpressions(
                [
                    new TtsExpression(ExpressionType.Text, "Hi Frost, (DoesNotExist) deez nuts"),
                    new TtsExpression(ExpressionType.Effect, "Peng"),
                    new TtsExpression(ExpressionType.Text, "(IsALie) bye")
                ],
                msg.expressions);
        }

        [TestMethod]
        public void TestNested()
        {
            (var aliases, var effects) = GetEnvStuff();
            string msgStr = "Hi Frost, (Skootish(peng)) deez nuts (fa(Frost)rt) bye";
            TtsMessage msg = InputConverter.FromString(msgStr, aliases, effects);

            Assert.IsNotEmpty(msg.expressions);
            CheckTtsExpressions(
                [
                    new TtsExpression(ExpressionType.Text, "Hi Frost, (Skootish"),
                    new TtsExpression(ExpressionType.Effect, "peng"),
                    new TtsExpression(ExpressionType.Text, ") deez nuts (fa"),
                    new TtsExpression(ExpressionType.Effect, "Frost"),
                    new TtsExpression(ExpressionType.Text, "rt) bye")
                ],
                msg.expressions);
        }

        [TestMethod]
        public void TestManyEffects()
        {
            (var aliases, var effects) = GetEnvStuff();
            string msgStr = "   (Skootish)(Peng)   (Frost) (boom)  ";
            TtsMessage msg = InputConverter.FromString(msgStr, aliases, effects);

            Assert.IsNotEmpty(msg.expressions);
            CheckTtsExpressions(
                [
                    new TtsExpression(ExpressionType.Effect, "Skootish"),
                    new TtsExpression(ExpressionType.Effect, "Peng"),
                    new TtsExpression(ExpressionType.Effect, "Frost"),
                    new TtsExpression(ExpressionType.Effect, "boom")
                ],
                msg.expressions);
        }

        [TestMethod]
        public void TestOnlyOpen()
        {
            (var aliases, var effects) = GetEnvStuff();
            string msgStr = "   (((((adsfasdf(((asdf((  ";
            TtsMessage msg = InputConverter.FromString(msgStr, aliases, effects);

            Assert.IsNotEmpty(msg.expressions);
            CheckTtsExpressions(
                [
                    new TtsExpression(ExpressionType.Text, "(((((adsfasdf(((asdf((")
                ],
                msg.expressions);
        }

        [TestMethod]
        public void TestOnlyClose()
        {
            (var aliases, var effects) = GetEnvStuff();
            string msgStr = "   )))))adsfasdf)))asdf))  ";
            TtsMessage msg = InputConverter.FromString(msgStr, aliases, effects);

            Assert.IsNotEmpty(msg.expressions);
            CheckTtsExpressions(
                [
                    new TtsExpression(ExpressionType.Text, ")))))adsfasdf)))asdf))")
                ],
                msg.expressions);
        }

        [TestMethod]
        public void TestMultiOpenSingleClose()
        {
            (var aliases, var effects) = GetEnvStuff();
            string msgStr = "Hi, ((((Peng) end.";
            TtsMessage msg = InputConverter.FromString(msgStr, aliases, effects);

            Assert.IsNotEmpty(msg.expressions);
            CheckTtsExpressions(
                [
                    new TtsExpression(ExpressionType.Text, "Hi, ((("),
                    new TtsExpression(ExpressionType.Effect, "Peng"),
                    new TtsExpression(ExpressionType.Text, "end.")
                ],
                msg.expressions);
        }

        [TestMethod]
        public void TestSingleOpenMultiClose()
        {
            (var aliases, var effects) = GetEnvStuff();
            string msgStr = "Hi, (Peng)))) end.";
            TtsMessage msg = InputConverter.FromString(msgStr, aliases, effects);

            Assert.IsNotEmpty(msg.expressions);
            CheckTtsExpressions(
                [
                    new TtsExpression(ExpressionType.Text, "Hi,"),
                    new TtsExpression(ExpressionType.Effect, "Peng"),
                    new TtsExpression(ExpressionType.Text, "))) end.")
                ],
                msg.expressions);
        }

        [TestMethod]
        public void TestRandomDoesNotCrash()
        {
            (var aliases, var effects) = GetEnvStuff();
            string msgStr = "";
            Random rng = new();
            for (int i = 0; i < 1000; i++)
            {
                msgStr += (char)(' ' + rng.NextInt64('}') - ' ');
            }

            // Check if it crashes, so no assert necessary
            TtsMessage msg = InputConverter.FromString(msgStr, aliases, effects);

            Assert.IsNotEmpty(msg.expressions);
        }

        private static (SpeakerBotVoiceAliases, SoundEffects) GetEnvStuff()
        {
            string pathToVoices = "J:/Programming/GitHub/StreamerBotToSpeakerBot/Speaker.bot-x64-0.1.7/data/voicealiases.dat";
            string pathToSoundEffectsFolder = "J:/Programming/GitHub/StreamerBotToSpeakerBot/SoundEffects";
            SpeakerBotVoiceAliases speakerBotVoiceAliases = SpeakerBotVoiceAliases.GetOrCreateInstance(pathToVoices);
            SoundEffects soundEffects = SoundEffects.GetOrCreateInstance(pathToSoundEffectsFolder);

            return (speakerBotVoiceAliases, soundEffects);
        }

        private static void CheckTtsExpressions(List<TtsExpression> expected, List<TtsExpression> actual)
        {
            Assert.HasCount(expected.Count, actual);

            for (int i = 0; i < expected.Count; i++)
            {
                Assert.AreEqual(expected[i].Type, actual[i].Type);
                Assert.AreEqual(expected[i].Value, actual[i].Value);
            }
        }
    }
}
