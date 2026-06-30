using System;
using System.IO;
using NAudio.Vorbis;
using NAudio.Wave;

namespace UndertaleModTool
{
    /// <summary>
    /// plays WAV/OGG audio. decoding (NAudio / NAudio.Vorbis) is cross-platform; audio OUTPUT
    /// (<see cref="WaveOutEvent"/>) is windows-only, so on other platforms playback throws a clear
    /// <see cref="PlatformNotSupportedException"/> that the caller surfaces as an error message.
    /// </summary>
    public sealed class AudioPlayer : IDisposable
    {
        private IWavePlayer output;
        private WaveStream reader;

        public static bool IsWav(byte[] data)
            => data.Length >= 4 && data[0] == 'R' && data[1] == 'I' && data[2] == 'F' && data[3] == 'F';

        public static bool IsOgg(byte[] data)
            => data.Length >= 4 && data[0] == 'O' && data[1] == 'g' && data[2] == 'g' && data[3] == 'S';

        public void Play(byte[] data)
        {
            Stop();

            if (IsWav(data))
                reader = new WaveFileReader(new MemoryStream(data));
            else if (IsOgg(data))
                reader = new VorbisWaveReader(new MemoryStream(data));
            else
                throw new InvalidOperationException("Not a WAV or OGG.");

            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Audio playback is only available on Windows in this build.");

            output = new WaveOutEvent { DeviceNumber = 0 };
            output.Init(reader);
            output.Play();
        }

        public void PlayFile(string path)
        {
            Stop();

            reader = Path.GetExtension(path).ToLower() switch
            {
                ".wav" => new WaveFileReader(path),
                ".ogg" => new VorbisWaveReader(path),
                ".mp3" => new Mp3FileReader(path),
                _ => throw new Exception("Unknown file type.")
            };

            if (!OperatingSystem.IsWindows())
                throw new PlatformNotSupportedException("Audio playback is only available on Windows in this build.");

            output = new WaveOutEvent { DeviceNumber = 0 };
            output.Init(reader);
            output.Play();
        }

        public void Stop()
        {
            output?.Stop();
            output?.Dispose();
            output = null;
            reader?.Dispose();
            reader = null;
        }

        public void Dispose() => Stop();
    }
}
