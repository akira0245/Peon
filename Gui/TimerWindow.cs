using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using Peon.Managers;

namespace Peon.Gui
{
    public class TimerWindow : IDisposable
    {
        private readonly TimerManager _manager;
        private          DateTime     _updateTimeRetainer = DateTime.UtcNow;

        private readonly float _widthTotal;
        private readonly float _widthTime;

        public TimerWindow(TimerManager manager)
        {
            _manager                               =  manager;
            Dalamud.PluginInterface.UiBuilder.Draw += Draw;

            _widthTime  = ImGui.CalcTextSize("Completed").X / ImGuiHelpers.GlobalScale;
            _widthTotal = ImGui.CalcTextSize("mmmmmmmmmmmmmmmm").X / ImGuiHelpers.GlobalScale + _widthTime + 20;
        }

        public void Dispose()
        {
            Dalamud.PluginInterface.UiBuilder.Draw -= Draw;
        }

        private void Draw()
        {
            if (!Peon.Config.EnableTimers)
                return;

            var now = DateTime.UtcNow;
            if (_updateTimeRetainer < now)
            {
                _manager.Update(true, true);
                _updateTimeRetainer = now.AddSeconds(2);
            }
            else
            {
                _manager.Update(false, true);
            }

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
                string? removePlayer = null;
                foreach (var (player, retainers) in Peon.Timers.Retainers)
                {
                    var collapse = ImGui.CollapsingHeader(player);
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        removePlayer = player;
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Right Click to clear.");
                    if (!collapse)
                        continue;


                    using var table = new ImGuiRaii();
                    if (!table.Begin(() => ImGui.BeginTable($"##{player}", 2), ImGui.EndTable))
                        continue;
                    ImGui.TableSetupColumn("0", ImGuiTableColumnFlags.None, (_widthTotal - _widthTime - 15) * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.None, (_widthTime + 15) * ImGuiHelpers.GlobalScale);

                    foreach (var (retainer, time) in retainers)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text(retainer);
                        ImGui.TableNextColumn();

                        ImGui.Text(time > now ? (time - now).ToString(@"hh\:mm\:ss") : "Completed");
                    }
                }

                string? removeFc = null;
                foreach (var (fc, machines) in Peon.Timers.Machines.ToArray())
                {
                    var collapse = ImGui.CollapsingHeader(fc);
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        removeFc = fc;
                    if (ImGui.IsItemHovered())
                        ImGui.SetTooltip("Right Click to clear.");
                    if (!collapse)
                        continue;

                    using var table = new ImGuiRaii();
                    if (!table.Begin(() => ImGui.BeginTable($"##{fc}", 2), ImGui.EndTable))
                        continue;

                    foreach (var (machine, (time, type)) in machines)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text(machine);
                        ImGui.TableNextColumn();
                        ImGui.Text(time.ToShortTimeString());
                    }
                }

                var save = removePlayer != null && Peon.Timers.Retainers.Remove(removePlayer);
                save |= removeFc != null && Peon.Timers.Machines.Remove(removeFc);

                if (save)
                    Peon.Timers.Save();
            }
            finally
            {
                ImGui.End();
            }
        }
    }
}
