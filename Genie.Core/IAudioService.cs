namespace GenieClient
{
    public interface IAudioService
    {
        void PlayWaveFile(string filePath);
        void PlayWaveSystem(string systemSoundAlias);
        void StopPlaying();
    }
}
