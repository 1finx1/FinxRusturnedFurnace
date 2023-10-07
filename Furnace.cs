using System.Collections.Generic;
using System.Xml.Serialization;

namespace FinxFurnace.Types
{
    public class Furnace
    {
        [XmlAttribute]
        public ushort StorageId { get; set; }
        [XmlAttribute]


        public int Delay { get; set; }

        [XmlAttribute]
        public ushort FuelId { get; set; } = 61;// Fuel item ID

        [XmlAttribute]
        public ushort EffectId { get; set; } // Common EffectId for all recipes

       

        public List<Recipe> Recipes { get; set; } // List of recipes

        // Method to get a recipe by input ID
        public Recipe GetRecipe(ushort inputId, out ushort outputId)
        {
            foreach (var currentRecipe in Recipes)
            {
                if (currentRecipe.InputId == inputId)
                {
                    outputId = currentRecipe.OutputId;
                    return currentRecipe;
                }
            }
            outputId = 0; // No matching recipe found
            return null;
        }
    }
}
