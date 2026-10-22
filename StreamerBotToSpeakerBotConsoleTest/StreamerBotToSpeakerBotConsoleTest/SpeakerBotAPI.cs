using System.Net.WebSockets;
using System.Text;
using Newtonsoft.Json;

namespace StreamerBotToSpeakerBotConsoleTest
{
    public class SpeakerBotAPI
    {
        private class BaseCommand(string request)
        {
            public Guid id { get; } = Guid.NewGuid();

            public string request { get; } = request;
        }

        private class SpeakCommand(string voice, string message, bool badWordFilter = true) : BaseCommand("Speak")
        {
            public string voice { get; } = voice;
            public string message { get; } = message;
            public bool badWordFilter { get; } = badWordFilter;
        }

        private class StopCommand() : BaseCommand("Stop");

        private class EnableCommand() : BaseCommand("Enable");

        private class DisableCommand() : BaseCommand("Disable");

        public enum EventsState
        {
            On,
            Off
        }

        private class EventsCommand(EventsState state) : BaseCommand("Events")
        {
            public string state { get; } = state == EventsState.On ? "on" : "off"; // [on|off]
        }

        public enum Modes
        {
            All,
            Command
        }

        private class ModeCommand(Modes mode) : BaseCommand("Mode")
        {
            public string mode { get; } = mode == Modes.All ? "all" : "command"; // [all|command]
        }

        private class QueuePauseCommand() : BaseCommand("Pause");

        private class QueueResumeCommand() : BaseCommand("Resume");

        private class QueueClearCommand() : BaseCommand("Clear");

        private readonly ClientWebSocket m_clientSocket = new();
        private bool m_isConnected = false;

        private SpeakerBotAPI() { }

        private static SpeakerBotAPI? s_instance;

        public static SpeakerBotAPI GetOrCreateInstance()
        {
            if (s_instance == null)
            {
                s_instance = new SpeakerBotAPI();
                s_instance.Init();
            }
            return s_instance;
        }

        public Guid Speak(string voice, string message)
        {
            return SendCommand(new SpeakCommand(voice, message));
        }

        public Guid Stop()
        {
            return SendCommand(new StopCommand());
        }

        public Guid Enable()
        {
            return SendCommand(new EnableCommand());
        }

        public Guid Disable()
        {
            return SendCommand(new DisableCommand());
        }

        public Guid Events(EventsState state)
        {
            return SendCommand(new EventsCommand(state));
        }

        public Guid Mode(Modes mode)
        {
            return SendCommand(new ModeCommand(mode));
        }

        public Guid Pause()
        {
            return SendCommand(new QueuePauseCommand());
        }

        public Guid Resume()
        {
            return SendCommand(new QueueResumeCommand());
        }

        public Guid Clear()
        {
            return SendCommand(new QueueClearCommand());
        }

        private void Init()
        {
            m_isConnected = Connect();
        }

        private bool Connect()
        {
            Uri uri = new("ws://127.0.0.1:7580");

            m_clientSocket.ConnectAsync(uri, default).GetAwaiter().GetResult();

            return true;
        }

        private Guid SendCommand<T>(T command) where T: BaseCommand
        {
            string jsonCommand = JsonConvert.SerializeObject(command);
            Send(jsonCommand);
            return command.id;
        }

        private bool Send(string message)
        {
            if (!m_isConnected)
            {
                throw new WebSocketException("Websocket not connected.");
            }

            var messageBytes = Encoding.UTF8.GetBytes(message);
            m_clientSocket.SendAsync(messageBytes, WebSocketMessageType.Text, true, default).GetAwaiter().GetResult();

            return true;
        }
    }
}
