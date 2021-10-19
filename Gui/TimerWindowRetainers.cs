using System;
using ImGuiNET;
using Lumina.Data.Files;

namespace Peon.Gui
{
    public partial class TimerWindow
    {
        private DateTime  _updateTimeRetainer = DateTime.UtcNow;
        private StateInfo _allRetainers       = StateInfo.Empty;

        private bool UpdateRetainers()
        {
            if (_updateTimeRetainer < _now)
            {
                _updateTimeRetainer = _now.AddSeconds(2);
                return _manager.UpdateRetainers();
            }

            return false;
        }

        private void DrawRetainerRow(string name, DateTime time)
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Selectable(name);
            ImGui.TableNextColumn();
            ImGui.Text(ConvertDateTime(_now, time));
        }

        private void DrawRetainers()
        {
            var save = UpdateRetainers();
            _allRetainers = StateInfo.Empty;
            string? removePlayer = null;
            foreach (var (player, retainers) in Peon.Timers.Retainers)
            {
                var playerInfo = new StateInfo(_now, retainers.Values, false);
                _allRetainers = StateInfo.Combine(_allRetainers, playerInfo);
                if (_drawStuff)
                {
                    var collapse = ColorHeader(player, playerInfo);

                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        removePlayer = player;

                    if (!collapse)
                        continue;

                    using var table = SetupTable($"##Retainers_{player}", _widthTime + 15);
                    if (!table)
                        continue;

                    foreach (var (retainer, time) in retainers)
                        DrawRetainerRow(retainer, time);
                }
            }

            save |= removePlayer != null && Peon.Timers.Retainers.Remove(removePlayer);
            if (save)
                Peon.Timers.SaveRetainers();
        }
    }
}
