using System;
using System.Collections.Generic;
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
        private readonly float _widthCount;
        private readonly float _widthTime;

        public TimerWindow(TimerManager manager)
        {
            _manager                               =  manager;
            Dalamud.PluginInterface.UiBuilder.Draw += Draw;

            _widthTime  = ImGui.CalcTextSize("Completed").X / ImGuiHelpers.GlobalScale;
            _widthCount = ImGui.CalcTextSize("10|9|9").X / ImGuiHelpers.GlobalScale;
            _widthTotal = ImGui.CalcTextSize("mmmmmmmmmmmmmmmm").X / ImGuiHelpers.GlobalScale + _widthTime + 20 + _widthCount / 2;
        }

        public void Dispose()
        {
            Dalamud.PluginInterface.UiBuilder.Draw -= Draw;
        }

        private (int, int, int) CountVentures(IEnumerable<DateTime> times)
        {
            int retainersAvailable = 0, retainersFinished = 0, retainersSent = 0;
            foreach (var time in times)
                if (time == DateTime.UnixEpoch)
                    ++retainersAvailable;
                else if (time <= _now)
                    ++retainersFinished;
                else
                    ++retainersSent;

            return (retainersFinished, retainersSent, retainersAvailable);
        }

        private DateTime _now = DateTime.UtcNow;

        private bool ColorHeader(string header, int finished, int away, int available, bool machines = false)
        {
            if (machines && away > 3)
                ImGui.PushStyleColor(ImGuiCol.Header, 0x800000A0);
            else if (away == 0)
                ImGui.PushStyleColor(ImGuiCol.Header, 0x8000A000);
            else if (finished == 0 && available == 0)
                ImGui.PushStyleColor(ImGuiCol.Header, 0x800000A0);
            else
                ImGui.PushStyleColor(ImGuiCol.Header, 0x8000A0A0);

            var text     = $"{finished}|{away}|{available}            ";
            ImGui.BeginGroup();
            var collapse = ImGui.CollapsingHeader(header);
            ImGui.SameLine(_widthTotal - _widthCount * 2);
            ImGui.Text(text);
            ImGui.EndGroup();
            ImGui.PopStyleColor();
            if (ImGui.IsItemHovered())
                ImGui.SetTooltip("Right Click to clear.");
            return collapse;
        }

        private static string TimeSpanString(TimeSpan span)
            => $"{(int) span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}";
        private void Draw()
        {
            if (!Peon.Config.EnableTimers)
                return;

            _now = DateTime.UtcNow;
            if (_updateTimeRetainer < _now)
            {
                _manager.Update(true, true);
                _updateTimeRetainer = _now.AddSeconds(2);
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
                var     startPos     = ImGui.GetCursorPos();
                ImGui.NewLine();
                ImGui.Separator();
                var totalFinishedR  = 0;
                var totalAwayR      = 0;
                var totalAvailableR = 0;
                var totalFinishedM  = 0;
                var totalAwayM      = 0;
                var totalAvailableM = 0;

                foreach (var (player, retainers) in Peon.Timers.Retainers)
                {
                    var (finished, away, available) =  CountVentures(retainers.Values);
                    totalFinishedR                  += finished;
                    totalAwayR                      += away;
                    totalAvailableR                 += available;
                    var collapse = ColorHeader(player, finished, away, available);

                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        removePlayer = player;

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
                        if (time == DateTime.UnixEpoch)
                            ImGui.Text("Not Sent");
                        else if (time <= _now)
                            ImGui.Text("Completed");
                        else
                            ImGui.Text((time - _now).ToString(@"hh\:mm\:ss"));
                    }
                }

                string? removeFc = null;
                foreach (var (fc, machines) in Peon.Timers.Machines.ToArray())
                {
                    var (finished, away, available) =  CountVentures(machines.Values.Select(a => a.Item1));
                    totalFinishedM                  += finished;
                    totalAwayM                      += away;
                    totalAvailableM                 += available;
                    var collapse = ColorHeader(fc, finished, away, available, true);
                    if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
                        removeFc = fc;
                    if (!collapse)
                        continue;

                    using var table = new ImGuiRaii();
                    if (!table.Begin(() => ImGui.BeginTable($"##{fc}", 2), ImGui.EndTable))
                        continue;

                    ImGui.TableSetupColumn("0", ImGuiTableColumnFlags.None, (_widthTotal - _widthTime - 15) * ImGuiHelpers.GlobalScale);
                    ImGui.TableSetupColumn("1", ImGuiTableColumnFlags.None, (_widthTime + 15) * ImGuiHelpers.GlobalScale);

                    foreach (var (machine, (time, _)) in machines)
                    {
                        ImGui.TableNextRow();
                        ImGui.TableNextColumn();
                        ImGui.Text(machine);
                        ImGui.TableNextColumn();
                        if (time == DateTime.UnixEpoch)
                            ImGui.Text("Not Sent");
                        else if (time <= _now)
                            ImGui.Text("Completed");
                        else
                            ImGui.Text(TimeSpanString(time - _now));
                    }
                }

                ImGui.SetCursorPos(startPos);
                ImGui.Text($"Retainers: {totalFinishedR}|{totalAwayR}|{totalAvailableR}, Machines: {totalFinishedM}|{totalAwayM}|{totalAvailableM}");

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
