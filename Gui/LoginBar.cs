using System;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using Peon.Managers;

namespace Peon.Gui
{
    public class LoginBar : IDisposable
    {
        private readonly LoginManager     _login;
        private readonly InterfaceManager _interface;

        public LoginBar(LoginManager login, InterfaceManager interfaceManager)
        {
            _login     = login;
            _interface = interfaceManager;

            Dalamud.PluginInterface.UiBuilder.Draw += Draw;
        }

        public void Dispose()
        {
            Dalamud.PluginInterface.UiBuilder.Draw -= Draw;
        }

        private const ImGuiWindowFlags WindowFlags =
            ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoBackground | ImGuiWindowFlags.NoDecoration;

        private static readonly Vector2 WindowPosOffset = new(120,
            -8 * (ImGui.GetTextLineHeightWithSpacing() + ImGui.GetStyle().FramePadding.Y) / ImGui.GetIO().FontGlobalScale + 10);

        private void Draw()
        {
            if (!Peon.Config.EnableLoginButtons || !Peon.Config.CharacterNames.Any() || !_interface.TitleMenu())
                return;

            var ss = ImGui.GetMainViewport().Size + ImGui.GetMainViewport().Pos;
            ImGui.SetNextWindowViewport(ImGui.GetMainViewport().ID);
            var textLength = ImGui.CalcTextSize("MMMMMMMMMMMMMMMMMMM").X;
            var pos        = new Vector2((ss.X - textLength) / 2, ImGui.GetMainViewport().Pos.Y + 30 * ImGui.GetIO().FontGlobalScale);

            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
            if (!ImGui.Begin("##LaunchButtons", WindowFlags))
                return;

            ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing,   Vector2.Zero);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 0f);
            foreach (var name in Peon.Config.CharacterNames)
                if (ImGui.Button($"{name}##login", Vector2.UnitX * textLength))
                    _login.LogTo(name, 10000);

            ImGui.PopStyleVar(2);

            ImGui.End();
        }
    }
}
