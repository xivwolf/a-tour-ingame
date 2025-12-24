using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using SamplePlugin.Windows;
using System;
using Dalamud.Game.Text;
using SamplePlugin.Models;
using SamplePlugin.Services;
using NAudio.Wave;
using System.Threading.Tasks;

namespace SamplePlugin;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService] internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static ITextureProvider TextureProvider { get; private set; } = null!;
    [PluginService] internal static ICommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static IClientState ClientState { get; private set; } = null!;
    [PluginService] internal static IPlayerState PlayerState { get; private set; } = null!;
    [PluginService] internal static IDataManager DataManager { get; private set; } = null!;
    [PluginService] internal static IPluginLog Log { get; private set; } = null!;
    [PluginService] internal static IChatGui ChatGui { get; private set; } = null!;

    public string Name => "TourAlert";
    private const string CommandName = "/touralert";

    public Configuration Configuration { get; init; }
    
    private readonly NotificationService _notificationService;

    public readonly WindowSystem WindowSystem = new("TourAlert");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // You might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        // Initialize the NotificationService first
        _notificationService = new NotificationService();
        _notificationService.MessageReceived += OnMessageReceived;
        _notificationService.ConnectAsync(new Uri("ws://192.168.2.35:8080/ws"));

        // Now initialize the windows, passing the service
        ConfigWindow = new ConfigWindow(this, _notificationService);
        MainWindow = new MainWindow(this, goatImagePath);

        // Add windows to the WindowSystem
        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "A useful message to display in /xlhelp"
        });

        // Subscribe to the UI builder events
        PluginInterface.UiBuilder.Draw += WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        // Add a simple message to the log
        Log.Information($"===A cool log message from {PluginInterface.Manifest.Name}===");
    }
    
    public void PlaySelectedSoundEffect()
    {
        var soundFileName = Configuration.SelectedSoundEffect;
        if (string.IsNullOrEmpty(soundFileName)) return;

        var soundPath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "sounds", soundFileName);

        if (!File.Exists(soundPath))
        {
            Log.Error($"Sound file not found: {soundPath}");
            return;
        }

        Task.Run(() =>
        {
            try
            {
                using var audioFile = new AudioFileReader(soundPath);
                audioFile.Volume = Configuration.SoundVolume;
                using var outputDevice = new WaveOutEvent();
                outputDevice.Init(audioFile);
                outputDevice.Play();
                while (outputDevice.PlaybackState == PlaybackState.Playing)
                {
                    Task.Delay(100).Wait();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error playing sound effect.");
            }
        });
    }

    private void OnMessageReceived(DiscordMessage message)
    {
        ChatGui.Print($"[{message.Category}]: {message.Content}");
        PlaySelectedSoundEffect();
    }

    public void Dispose()
    {
        // Unregister all actions to not leak anything during disposal of plugin
        PluginInterface.UiBuilder.Draw -= WindowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi -= ToggleConfigUi;
        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;
        
        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(CommandName);
        
        _notificationService.Dispose();
    }

    private void OnCommand(string command, string args)
    {
        // In response to the slash command, toggle the display status of our main ui
        MainWindow.Toggle();
    }
    
    public void ToggleConfigUi() => ConfigWindow.Toggle();
    public void ToggleMainUi() => MainWindow.Toggle();
}
