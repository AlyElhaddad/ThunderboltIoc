using System.Collections.Generic;

using ThunderboltIocSamples.XamarinForms.Models;

namespace ThunderboltIocSamples.XamarinForms.Services
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
