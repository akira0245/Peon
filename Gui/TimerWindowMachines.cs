using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Utility;
using ImGuiNET;
using ImGuiScene;

namespace Peon.Gui
{
    public partial class TimerWindow
    {
        private StateInfo    _allMachines = StateInfo.Empty;
        private TextureWrap? _airshipIcon;
        private TextureWrap? _submarineIcon;

        private void GetIcons()
        {
            var airshipIcon = Dalamud.GameData.GetIcon(60352);
            if (airshipIcon != null)
                _airshipIcon = Dalamud.PluginInterface.UiBuilder.LoadImageRaw(airshipIcon.GetRgbaImageData(), airshipIcon.Header.Width,
                    airshipIcon.Header.Height, 4);
            var submarineIcon = Dalamud.GameData.GetIcon(60339);
            if (submarineIcon != null)
                _submarineIcon = Dalamud.PluginInterface.UiBuilder.LoadImageRaw(submarineIcon.GetRgbaImageData(), submarineIcon.Header.Width,
                    submarineIcon.Header.Height, 4);
        }

        private void DisposeIcons()
        {
            _airshipIcon?.Dispose();
            _submarineIcon?.Dispose();
            _airshipIcon   = null;
            _submarineIcon = null;
        }

        private void DrawMachineRow(string name, DateTime time, MachineType type)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            var handle = type == MachineType.Submarine ? _submarineIcon?.ImGuiHandle : _airshipIcon?.ImGuiHandle;
            if (handle != null)
            {
                ImGui.Image(handle.Value, Vector2.One * ImGui.GetTextLineHeight());
                ImGui.SameLine();
            }

            ImGui.Selectable(name);
            ImGui.TableNextColumn();
            ImGui.Text(ConvertDateTime(_now, time));
        }

        private void DrawMachines()
        {
            _allMachines = StateInfo.Empty;
            string? removeFc = null;
            foreach (var (fc, machines) in Peon.Timers.Machines)
            {
                var info = new StateInfo(_now, machines.Select(m => m.Value.Item1), true);
                _allMachines = StateInfo.Combine(_allMachines, info);
                var collapse = ColorHeader(fc, info);
                if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                    removeFc = fc;
                if (!collapse)
                    continue;

                using var table = SetupTable($"##Machines_{fc}", _widthTime + 15);
                if (!table)
                    continue;

                foreach (var (name, (time, type)) in machines)
                    DrawMachineRow(name, time, type);
            }

            if (removeFc != null && Peon.Timers.Machines.Remove(removeFc))
                Peon.Timers.SaveMachines();
        }
    }
}
