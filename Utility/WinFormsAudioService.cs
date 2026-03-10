namespace GenieClient
{
    public class WinFormsAudioService : IAudioService
    {
        public void PlayWaveFile(string filePath) => Genie.Sound.PlayWaveFile(filePath);
        public void PlayWaveSystem(string systemSoundAlias) => Genie.Sound.PlayWaveSystem(systemSoundAlias);
        public void StopPlaying() => Genie.Sound.StopPlaying();
    }
}
