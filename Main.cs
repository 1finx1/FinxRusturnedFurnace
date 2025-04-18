using Rocket.Unturned.Player;
using SDG.Unturned;
using System.Collections.Generic;
using UnityEngine;

namespace FinxFurnace
{
    public static class Main
    {
        public static InteractableStorage GetRaycastStorage(this UnturnedPlayer uPlayer, out ushort storageId) // gets the storage item if there is any when a player does the specified gesture
        {
            storageId = 0;
            if (Physics.Raycast(uPlayer.Player.look.aim.position, uPlayer.Player.look.aim.forward, out RaycastHit hit, 10f, RayMasks.BARRICADE_INTERACT) &&
                BarricadeManager.tryGetInfo(hit.transform, out _, out _, out _, out ushort index, out BarricadeRegion region))
            {
                storageId = region.barricades[index].barricade.id;
                return hit.transform.GetComponent<InteractableStorage>();
            }
            return null;
        }


        public static void AddRange(this List<ItemJar> items, List<ItemJar> itemsToAdd, int count)
        {
            for (int i = 0; i < itemsToAdd.Count && i < count; i++)
                items.Add(itemsToAdd[i]);
        }


        public static void tryAddItems(this InteractableStorage storage, SDG.Unturned.Item item)
        {
            for (byte i = 0; i < item.amount; i++)
            {
                // Create a new Item object with the specified metadata
                SDG.Unturned.Item newItem = new SDG.Unturned.Item(item.id, true);

                // Unfortunately, Unturned's TryaddItem class does not have a direct metadata property you have to set it in the "newitem"


                if (!storage.items.tryAddItem(newItem))
                {
                    // If there's not enough space in the storage, drop the excess items at the storage's position
                    ItemManager.dropItem(newItem, storage.transform.position, true, true, true);
                }
            }
        }



    }
}
