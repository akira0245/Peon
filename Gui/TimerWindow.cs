using System;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using Peon.Managers;
using DateTime = System.DateTime;

namespace Peon.Gui
{
    public partial class TimerWindow : IDisposable
    {
        public const uint GreenHeader      = 0x8000A000;
        public const uint GreenText        = 0xFF00C000;
        public const uint RedHeader        = 0x800000A0;
        public const uint RedText          = 0xFF0000C0;
        public const uint YellowHeader     = 0x8000A0A0;
        public const uint YellowText       = 0xFF00C0C0;
        public const uint PurpleHeader     = 0x80A000A0;
        public const uint PurpleText       = 0xFFC000C0;
        public const uint LightGreenHeader = 0x8040C000;
        public const uint LightGreenText   = 0xFF60F000;

        private readonly TimerManager _manager;

        private readonly float _widthTotal;
        private readonly float _widthCount;
        private readonly float _widthShortTime;
        private readonly float _widthTime;

        private DateTime _now = DateTime.UtcNow;

        public TimerWindow(TimerManager manager)
        {
            _manager = manager;

            _widthTime      = ImGui.CalcTextSize("Completed").X / ImGuiHelpers.GlobalScale;
            _widthShortTime = ImGui.CalcTextSize("00:00:00").X / ImGuiHelpers.GlobalScale;
            _widthCount     = ImGui.CalcTextSize("10|9|9").X / ImGuiHelpers.GlobalScale;
            _widthTotal = ImGui.CalcTextSize("mmmmmmmmmmmmmmmmmmmm").X / ImGuiHelpers.GlobalScale
              + _widthTime
              + ImGui.GetStyle().ScrollbarSize / ImGuiHelpers.GlobalScale;
            GetIcons();

            Dalamud.PluginInterface.UiBuilder.Draw += Draw;
        }

        public void Dispose()
        {
            Dalamud.PluginInterface.UiBuilder.Draw -= Draw;
            DisposeIcons();
        }


        private void Draw()
        {
            if (!Peon.Config.EnableTimers)
                return;

            _now = DateTime.UtcNow;

            var minSize = new Vector2(_widthTotal * ImGuiHelpers.GlobalScale, ImGui.GetTextLineHeightWithSpacing() * 5);
            var maxSize = new Vector2(minSize.X,                              100000);
            ImGui.SetNextWindowSizeConstraints(minSize, maxSize);
            if (!ImGui.Begin("Peon Timers"))
            {
                ImGui.End();
                return;
            }

            try
            {
                var startPos = ImGui.GetCursorPos();
                ImGui.NewLine();
                ImGui.Separator();

                DrawCrops();
                DrawRetainers();
                DrawMachines();

                ImGui.SetCursorPos(startPos);
                ImGui.Text(
                    $"Retainers: {_allRetainers.FinishedObjects}|{_allRetainers.SentObjects}|{_allRetainers.AvailableObjects}, Machines: {_allMachines.FinishedObjects}|{_allMachines.SentObjects}|{_allMachines.AvailableObjects}");
            }
            finally
            {
                ImGui.End();
            }
        }
    }
}
