using FinxFurnace.Types;

using Rocket.API;
using System.Collections.Generic;

public class Config : IRocketPluginConfiguration // config self explanatory
{
    public List<Furnace> Furnaces; // List of recyclers
    public List<StackingItemConversion> StackingItemConversions; // List of item stack conversions

    public int MaxStackAmount { get; set; } = 250;

    public bool EnableDebugLogs { get; set; } = true;

    public string ChatIconUrl { get; set; } = "placeholder";

    public string ChatColor { get; set; } = "#f54842";


    public void LoadDefaults()
    {
        Furnaces = new List<Furnace>
        {
            new Furnace
            {
                StorageId = 328,
                Delay = 2,
                EffectId = 147,
                Recipes = new List<Recipe>
                {
                    new Recipe
                    {
                        InputId = 19101,
                        OutputId = 19100,
                        OutputAmount = 3,
                        FuelCost = 2
                    },
                    new Recipe
                    {
                        InputId = 519,
                        OutputId = 19068,
                        OutputAmount = 10,
                        FuelCost = 2
                    },
                    new Recipe
                    {
                        InputId = 14,
                        OutputId = 92,
                        OutputAmount = 9,
                        FuelCost = 2
                    }
                }
            }
        };

        // Add a new StackingItemConversion to the defaults
        StackingItemConversions = new List<StackingItemConversion>
        {
            new StackingItemConversion
            {
                ID = 19068, // Input item ID
                ID10x = 19079, // Conversion to 10x stack item ID
                ID50x = 125, // Conversion to 50x stack item ID
                ID100x = 126, // Conversion to 100x stack item ID
                ID300x = 127 // Conversion to 300x stack item ID
            }
            // Add more StackingItemConversion entries if needed
        };
    }
}
