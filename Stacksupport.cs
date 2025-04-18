using System.Collections.Generic;
using SDG.Unturned;

namespace FinxStack.Types
{
    public class StackableItemFinder
    {
        public static List<ItemAsset> FindStackableItems() // checks if any assets have an "amount" property and adds them to the list
        {
            List<ItemAsset> stackableItems = new List<ItemAsset>();

            Asset[] assets = Assets.find(EAssetType.ITEM); // finds all assets with the "ITEM" type

            foreach (Asset asset in assets)
            {
                if (asset is ItemAsset itemAsset)
                {
                    // Check if the asset already has an amount set
                    if (itemAsset.amount > 1)
                    {
                        stackableItems.Add(itemAsset); // if it does then add it to the list

                    }
                }
            }

            return stackableItems;
        }
    }
}
