using System.Collections.Generic;

using ThunderWpfCore.Models;

namespace ThunderWpfCore.Services
{
    public class DataService
    {
        public List<Thing> Things { get; } = new List<Thing>()
        {
            new Thing() { Name = "Item1" },
            new Thing() { Name = "Item2" },
            new Thing() { Name = "Item3" }
        };
    }
}
