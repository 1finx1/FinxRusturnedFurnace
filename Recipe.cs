using System.Xml.Serialization;

namespace FinxFurnace.Types
{
    public class Recipe
    {
        [XmlAttribute]
        public ushort InputId { get; set; } // Input item ID
        [XmlAttribute]
        public ushort OutputId { get; set; } // Output item ID
        [XmlAttribute]
        public int OutputAmount { get; set; } // Amount of output items
        [XmlAttribute]
        public int FuelCost { get; set; } // Fuel cost for this recipe
    }
}
