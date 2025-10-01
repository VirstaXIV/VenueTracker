using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Dalamud.Plugin;
using Dalamud.Utility;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace VenueTracker.Utils;

public class FileStore
{
    private readonly ILogger<FileStore>  _logger;
    private readonly IDalamudPluginInterface  _pluginInterface;
    
    public FileStore(ILogger<FileStore> logger, IDalamudPluginInterface pluginInterface)
    {
        _logger = logger;
        _pluginInterface = pluginInterface;
    }
    
    public void SaveClassToFile(string path, Type fileType, object objectData)
    {
        try
        {
            string output = JsonConvert.SerializeObject(objectData, fileType, new JsonSerializerSettings { Formatting = Formatting.Indented });
            FilesystemUtil.WriteAllTextSafe(path, output);
        }
        catch (Exception exception)
        {
            _logger.LogError("Failed to save file: " + exception.ToString());
        }
    }
    
    public void SaveClassToFileInPluginDir(string fileName, Type fileType, object objectData)
    {
        var fileInfo = GetFileInfo(fileName);
        SaveClassToFile(fileInfo.FullName, fileType, objectData);
    }
    
    public T LoadFile<T>(string filePath, object targetObject)
    {
        if (LoadFile(filePath, targetObject.GetType(), out var loadedData))
        {
            return (T)loadedData;
        }

        SaveClassToFileInPluginDir(filePath, targetObject.GetType(), targetObject);
        return (T)targetObject;
    }
    
    private bool LoadFile(string fileName, Type fileType, [NotNullWhen(true)] out object? loadedData)
    {
        try
        {
            var fileInfo = GetFileInfo(fileName);

            if (fileInfo is { Exists: false })
            {
                loadedData = null;
                return false;
            }

            var jsonString = File.ReadAllText(fileInfo.FullName);
            loadedData = JsonConvert.DeserializeObject(jsonString, fileType)!;
            return true;
        }
        catch (Exception exception)
        {
            _logger.LogError("Error loading file " + fileName + "." + exception.ToString());
            loadedData = null;
            return false;
        }
    }
    
    public FileInfo GetFileInfo(string fileName)
    {
        var configDirectory = _pluginInterface.ConfigDirectory;
        return new FileInfo(Path.Combine(configDirectory.FullName, fileName));
    }
}
