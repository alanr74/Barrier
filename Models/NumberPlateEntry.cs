using System;

namespace Ava.Models
{
    public class NumberPlateEntry
    {
        public string Plate { get; set; } = string.Empty;
        public DateTime Start { get; set; }
        public DateTime Finish { get; set; }
    }
}
