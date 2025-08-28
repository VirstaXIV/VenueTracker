using System;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;

namespace VenueTracker.Utils;

public class Doorbell(Plugin plugin)
{
    private const string File = "doorbell.wav";
    private WaveStream? audioFileReader;
    private WaveOutEvent? wavEvent;

    public void Load()
    {
        DisposeFile();
        try
        {
            var fileToLoad = Path.Join(Plugin.PluginInterface.AssemblyLocation.Directory!.FullName, File);
            
            if (!System.IO.File.Exists(fileToLoad)) {
                Plugin.Log.Warning($"{File} does not exist.");
                return;
            }
            audioFileReader = new AudioFileReader(fileToLoad);
            (audioFileReader as AudioFileReader)!.Volume = plugin.Configuration.SoundVolume;
            wavEvent = new WaveOutEvent();
            wavEvent.Init(audioFileReader);
        } catch (Exception ex) {
            Plugin.Log.Error(ex, "Error loading sound file " + File);
        }
    }
    
    public void Play() {
        Task.Run(() => {
            if (audioFileReader == null || wavEvent == null) Load();
            if (audioFileReader == null || wavEvent == null) return;
            wavEvent.Stop();
            audioFileReader.Position = 0;
            wavEvent.Play();
        });
    }
    
    public void DisposeFile() {
        audioFileReader?.Dispose();
        wavEvent?.Dispose();

        audioFileReader = null;
        wavEvent = null;
    }
}
