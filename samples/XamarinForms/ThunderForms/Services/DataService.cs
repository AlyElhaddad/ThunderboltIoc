using System.Collections.Generic;

using ThunderForms.Models;

namespace ThunderForms.Services
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
