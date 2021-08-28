using System.Threading.Tasks;
using Dalamud.Logging;
using Peon.SeFunctions;
using Peon.Utility;

namespace Peon.Managers
{
    public class InputManager
    {
        private const byte  UnknownFlag   =         50;
        private const uint  KeyDownFlag   =        256;
        private const ulong KeyDownSource =    5373953;
        private const uint  KeyUpFlag     =        257;
        private const ulong KeyUpSource   = 3226599425;

        private readonly InputKey _inputKey;

        public const uint Escape  = 0x1B;
        public const uint Num0    = 0x60;
        public const uint Num5    = 0x65;
        public const uint ButtonA = 0x41;
        public const uint ButtonW = 0x57;

        public InputManager()
            => _inputKey = Service<InputKey>.Get();

        private void SendKeyDown(uint key)
            => _inputKey.Invoke(UnknownFlag, KeyDownFlag, key, KeyDownSource);

        private void SendKeyUp(uint key)
            => _inputKey.Invoke(UnknownFlag, KeyUpFlag, key, KeyUpSource);

        public void SendKey(uint key, bool up)
        {
            if (up)
                SendKeyUp(key);
            else
                SendKeyDown(key);
        }

        public async void SendKeyPress(uint key)
        {
            PluginLog.Verbose($"Input key {key}.");
            SendKeyDown(key);
            await Task.Delay(10);
            SendKeyUp(key);
        }
    }
}
