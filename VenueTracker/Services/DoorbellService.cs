using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using VenueTracker.Services.Mediator;
using NAudio.Wave;
using VenueTracker.Services.Config;

namespace VenueTracker.Services;

public class DoorbellService : IHostedService, IMediatorSubscriber
{
    private const string file = "doorbell.wav";
    private WaveStream? _audioFileReader;
    private WaveOutEvent? _wavEvent;
    private readonly ILogger<DoorbellService> _logger;
    private readonly ConfigService _configService;
    private readonly IDalamudPluginInterface _pluginInterface;
    
    public DoorbellService(ILogger<DoorbellService> logger, IDalamudPluginInterface pluginInterface, 
        ConfigService configService)
    {
        _logger = logger;
        _configService = configService;
        _pluginInterface = pluginInterface;
    }
    
    public VSyncMediator Mediator { get; }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    public void PlayDoorbell()
    {
        Play();
    }
    
    public void ReloadDoorbell()
    {
        Load();
    }
    
    private void Load()
    {
        DisposeFile();
        try
        {
            var fileToLoad = Path.Join(_pluginInterface.AssemblyLocation.Directory!.FullName, file);
            
            if (!System.IO.File.Exists(fileToLoad)) {
                Plugin.Log.Warning($"{file} does not exist.");
                return;
            }
            _audioFileReader = new AudioFileReader(fileToLoad);
            (_audioFileReader as AudioFileReader)!.Volume = _configService.Current.SoundVolume;
            _wavEvent = new WaveOutEvent();
            _wavEvent.Init(_audioFileReader);
        } catch (Exception ex) {
            Plugin.Log.Error(ex, "Error loading sound file " + file);
        }
    }
    
    private void Play() {
        Task.Run(() => {
            if (_audioFileReader == null || _wavEvent == null) Load();
            if (_audioFileReader == null || _wavEvent == null) return;
            _wavEvent.Stop();
            _audioFileReader.Position = 0;
            _wavEvent.Play();
        });
    }
    
    private void DisposeFile() {
        _audioFileReader?.Dispose();
        _wavEvent?.Dispose();

        _audioFileReader = null;
        _wavEvent = null;
    }
}
