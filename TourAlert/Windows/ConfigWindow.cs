using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using TourAlert.Services;

namespace TourAlert.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration configuration;
    private readonly NotificationService _notificationService;
    private readonly Plugin _plugin;

    // We give this window a constant ID using ###.
    // This allows for labels to be dynamic, like "{FPS Counter}fps###XYZ counter window",
    // and the window ID will always be "###XYZ counter window" for ImGui
    public ConfigWindow(Plugin plugin, NotificationService notificationService) : base("設定視窗###With a constant ID")
    {
        Flags = ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
                ImGuiWindowFlags.NoScrollWithMouse;

        Size = new Vector2(232, 320); // Increased height for test option
        SizeCondition = ImGuiCond.Always;

        configuration = plugin.Configuration;
        _notificationService = notificationService;
        _plugin = plugin;
    }

    public void Dispose() { }

    public override void Draw()
    {
        if (_notificationService != null)
        {
            ImGui.Text($"連線狀態: {_notificationService.ConnectionState}");
        }
        else
        {
            ImGui.Text("連線狀態: 未知");
        }

        ImGui.Separator();
        ImGui.Text("通知過濾");

        var enable7A = configuration.Enable7ANotify;
        if (ImGui.Checkbox("啟用 7A 通報", ref enable7A))
        {
            configuration.Enable7ANotify = enable7A;
            configuration.Save();
        }

        var enable6A = configuration.Enable6ANotify;
        if (ImGui.Checkbox("啟用 6A 通報", ref enable6A))
        {
            configuration.Enable6ANotify = enable6A;
            configuration.Save();
        }

        var enable5A = configuration.Enable5ANotify;
        if (ImGui.Checkbox("啟用 5A 通報", ref enable5A))
        {
            configuration.Enable5ANotify = enable5A;
            configuration.Save();
        }

        var enableTest = configuration.EnableTestNotify;
        if (ImGui.Checkbox("啟用開發測試通報", ref enableTest))
        {
            configuration.EnableTestNotify = enableTest;
            configuration.Save();
        }

        ImGui.Separator();
        ImGui.Text("音效設定");

        string[] soundEffects = { "FFXIV_Incoming_Tell_1.mp3", "FFXIV_Incoming_Tell_2.mp3", "FFXIV_Incoming_Tell_3.mp3" };
        string[] labels = { "類型1", "類型2", "類型3" };
        string currentSound = configuration.SelectedSoundEffect;
        int currentIndex = Array.IndexOf(soundEffects, currentSound);
        string preview = currentIndex >= 0 ? labels[currentIndex] : currentSound;

        if (ImGui.BeginCombo("提示音效", preview))
        {
            for (int i = 0; i < soundEffects.Length; i++)
            {
                bool isSelected = currentIndex == i;
                if (ImGui.Selectable(labels[i], isSelected))
                {
                    configuration.SelectedSoundEffect = soundEffects[i];
                    configuration.Save();
                }

                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }
            ImGui.EndCombo();
        }

        var volume = configuration.SoundVolume * 100f;
        if (ImGui.SliderFloat("音量", ref volume, 0f, 100f, "%.0f%%"))
        {
            configuration.SoundVolume = Math.Max(0f, Math.Min(1f, volume / 100f));
            configuration.Save();
        }

        if (ImGui.Button("測試音效"))
        {
            _plugin.PlaySelectedSoundEffect();
        }
    }
}
