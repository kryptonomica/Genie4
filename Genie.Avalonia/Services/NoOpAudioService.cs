namespace GenieClient.Avalonia.Services
{
    public class NoOpAudioService : IAudioService
    {
        public void PlayWaveFile(string filePath) { }
        public void PlayWaveSystem(string systemSoundAlias) { }
        public void StopPlaying() { }
    }
}
