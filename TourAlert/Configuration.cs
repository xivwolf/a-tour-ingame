using Dalamud.Configuration;
using System;

namespace TourAlert;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public string SelectedSoundEffect { get; set; } = "FFXIV_Incoming_Tell_1.mp3";
    public float SoundVolume { get; set; } = 1.0f;

    public bool Enable7ANotify { get; set; } = true;
    public bool Enable6ANotify { get; set; } = true;
    public bool Enable5ANotify { get; set; } = true;
    public bool EnableTestNotify { get; set; } = false;

    public const string Role7A = "1255412015757918240";
    public const string Role6A = "934264593470070784";
    public const string Role5A = "934264458069561354";
    public const string RoleTest = "1453484035870425331";

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
