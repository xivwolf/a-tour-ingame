using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using TourAlert.Windows;
using System;
using Dalamud.Game.Text;
using TourAlert.Models;
using TourAlert.Services;
using NAudio.Wave;
using System.Threading.Tasks;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;

namespace TourAlert;

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
    [PluginService] internal static IFramework Framework { get; private set; } = null!;
    [PluginService] internal static IObjectTable ObjectTable { get; private set; } = null!;

    public string Name => "TourAlert";
    private const string CommandName = "/touralert";

    public Configuration Configuration { get; init; }
    
    private readonly NotificationService _notificationService;

    public readonly WindowSystem WindowSystem = new("TourAlert");
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    
    private bool _hasConnected = false;

    public Plugin()
    {
        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

        // You might normally want to embed resources and load them from the manifest stream
        var goatImagePath = Path.Combine(PluginInterface.AssemblyLocation.Directory?.FullName!, "goat.png");

        // Initialize the NotificationService first
        _notificationService = new NotificationService();
        _notificationService.MessageReceived += OnMessageReceived;
        
        // Initial connection attempt
        Framework.Update += OnFrameworkUpdate;

        // Subscribe to login/logout to update identity
        ClientState.Login += OnLogin;
        ClientState.Logout += OnLogout;

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

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (_hasConnected)
        {
            Framework.Update -= OnFrameworkUpdate;
            return;
        }
        
        _hasConnected = true;
        Framework.Update -= OnFrameworkUpdate;
        UpdateWebSocketConnection();
    }
    
    private void UpdateWebSocketConnection()
    {
        // Use ObjectTable[0] to get the local player
        if (ObjectTable.Length == 0 || ObjectTable[0] is not IPlayerCharacter player)
        {
            Log.Debug("LocalPlayer (ObjectTable[0]) is null or not a player, skipping WebSocket connection.");
            return;
        }

        var name = player.Name.TextValue;
        var world = player.HomeWorld.Value.Name.ToString();
        var fullName = $"{name}@{world}";
        
        // Simple manual URI escaping for the name
        var escapedName = Uri.EscapeDataString(fullName);
        var uri = new Uri($"wss://atour.bnsw.tech/ws?username={escapedName}");
        
        Log.Information($"Connecting to WebSocket as: {fullName}");
        _ = _notificationService.ConnectAsync(uri);
    }

    private void OnLogin() => UpdateWebSocketConnection();
    private void OnLogout(int type, int code) => _ = _notificationService.DisconnectAsync();
    
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

        _ = Task.Run(() =>
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

    private void OnMessageReceived(DiscordMessage message, string rawJson)
    {
        // Extract role IDs from mentions using the string Id property
        var mentionedRoleIds = message.RoleMentions.Select(r => r.Id).ToList();
        
        Log.Information($"Received message Category: {message.Category}. Mentions: {string.Join(", ", mentionedRoleIds)}");
        Log.Debug($"Raw JSON: {rawJson}");

        bool shouldNotify = false;

        if (Configuration.Enable7ANotify && mentionedRoleIds.Contains(Configuration.Role7A)) shouldNotify = true;
        if (Configuration.Enable6ANotify && mentionedRoleIds.Contains(Configuration.Role6A)) shouldNotify = true;
        if (Configuration.Enable5ANotify && mentionedRoleIds.Contains(Configuration.Role5A)) shouldNotify = true;
        if (Configuration.EnableTestNotify && mentionedRoleIds.Contains(Configuration.RoleTest)) shouldNotify = true;

        if (shouldNotify)
        {
            ChatGui.Print(new XivChatEntry
            {
                Type = XivChatType.Urgent,
                Message = $"[{message.Category}]: {message.Content}"
            });
            PlaySelectedSoundEffect();
        }
    }

    public void Dispose()
    {
        // Unregister events
        ClientState.Login -= OnLogin;
        ClientState.Logout -= OnLogout;

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
