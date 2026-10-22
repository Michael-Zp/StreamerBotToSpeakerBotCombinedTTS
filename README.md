# StreamerBotToSpeakerBotCombinedTTS

## CAUTION

The action triggers are not setup correctly here, because I don't know what people want.
It currently triggers FOR EVERY MESSAGE send in the linked chat.
Change that when activating it. You can just close Speaker.bot before configuring it as it should be disabled then.

## Setup

- Install `Streamer.bot` and `Speaker.bot` as stated on their website
- Open `Speaker.bot`
  - Go to `Settings/General` and set 'Save TTS audio to file' with some path you'd like
  - Go to `Settings/Websocket Server` and start the server. Also probably set Auto Start.
- Open `Streamer.bot`
  - Import `StreamerBotActions/TTSParser.dat`
  - Go to `Actions&Queues/Actions`
  - Look at Action TTSParser
    - Set all the path parameters in the Sub-Actions panel
      - `SaveSpeakerBotTtsAudioToFileDirectoryPath`: Directry where you pointed `Speaker.bot` with the `'Save TTS audio to file'` to.
      - `PathToSpeakerBotVoicealiasesDatFile`: File in the `Speaker.bot` data directory called `voicealiases.dat`.
      - `PathToSoundEffectsFolder`: Directory where your local sound effects live.

## What does it support

### Multiple Speaker.bot voices

It looks into your `voicealiases.dat` which saves all the voices in the `Speaker.bot/Settings/Voice Aliases` and can use those names in the TTS.

### Sound Effects

Currently does not look into folders just the root in the `PathToSoundEffectsFolder`, so keep everything in one folder.

The file name is the name used in the TTS.


## Example

"Hi Chat, (Skootish) this is Skootish btw. (Slience) (Frost) Play the song (FrostTheme)"

Will result in:
- "Hi Chat,"
  - Said by default voice
- "this is Skootish btw. (Slience)"
  - Said by Skootish
- "Play the song"
  - Said by Frost
- Frost theme song plays


## Problems

### Sync issues

Speaker.bot does not provide any sync mechanism. I'm currently using the generated WAV files from the `'Save TTS audio to file'` option to calculate how long a TTS snippet will take, but if Streamer.bot is laggy that might get out of sync.

I could probably sync it better by scraping the Speaker.bot log file, as this logs the start time of the TTS, but it still might not be perfect.

### Voice delay

Speaker.bot takes a bit of time before talking.

I don't think there is a way to speed up switching voices, as this actually needs two commands to Speaker.bot back to back.

There maybe is a speed up possible with sound effects, as we can queue Speaker.bot before a sound effect finishes and get it out quicker.

### Error handling

There is no good error handling currently. Still needs to be battle tested. Is just a POC right now.


## Possible new features

### Sound effect manipulation

As you have full C# control over stuff it would be possible to give chat more control over sound effects or even voices.

Sound effects are after all only sound files, that can be manipulated.

Voices too can be manipulated, because with `'Save TTS audio to file'` they are saved to disk, can be read, manipulated and then played. The voice can be redirected to any audio device. So you could redirect it to some random muted headphones and just play the manipulated `'Save TTS audio to file'` file.