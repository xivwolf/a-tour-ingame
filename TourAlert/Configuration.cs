using Dalamud.Configuration;
using System;

namespace SamplePlugin;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public string SelectedSoundEffect { get; set; } = "FFXIV_Incoming_Tell_1.mp3";
    public float SoundVolume { get; set; } = 1.0f;

    // The below exists just to make saving less cumbersome
    public void Save()
    {
        Plugin.PluginInterface.SavePluginConfig(this);
    }
}
