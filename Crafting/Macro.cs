using System.Collections.Generic;
using Newtonsoft.Json;

namespace Peon.Crafting
{
    public class Macro
    {
        public string         Name    { get; }
        public List<ActionId> Actions { get; } = new();

        public Macro(string name)
            => Name = name;

        public ActionInfo Step(int idx)
            => Actions[idx - 1].Use();

        [JsonIgnore]
        public int Count
            => Actions.Count;
    }
}
