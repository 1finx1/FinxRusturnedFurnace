using Rocket.API.Collections;
using Rocket.Core.Plugins;
using Rocket.Core.Utils;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;
using FinxFurnace.Types;
using static Pathfinding.AdvancedSmooth;

namespace FinxFurnace
{
    public class FurnacePlugin : RocketPlugin<Config>
    {
        public override TranslationList DefaultTranslations => new TranslationList
        {
            { "furnace_start", "Smelting has started!" },
            { "furnace_end", "Smelting finished!" },
            { "not_enough_fuel", "Furnace requires ({0}) ({1}) to smelt this many items!" },
        };

        public static FurnacePlugin Instance { get; private set; }
        public List<StackingItemConversion> StackingItemConversions { get; set; } = new List<StackingItemConversion>();

        protected override void Load()
        {
            Instance = this;
            UnturnedPlayerEvents.OnPlayerUpdateGesture += UnturnedPlayerEvents_OnPlayerUpdateGesture;
            Debug.Log("Made By Finx, contact me on discord: finx1, for any questions");
        }

        

        private void UnturnedPlayerEvents_OnPlayerUpdateGesture(UnturnedPlayer player, UnturnedPlayerEvents.PlayerGesture gesture)
        {
            try
            {
                if (gesture != UnturnedPlayerEvents.PlayerGesture.PunchLeft && gesture != UnturnedPlayerEvents.PlayerGesture.PunchRight && gesture != UnturnedPlayerEvents.PlayerGesture.Point)
                {
                    return;
                }

                var interactableStorage = player.GetRaycastStorage(out ushort storageId);

                if (interactableStorage == null)
                {
                    return;
                }

                var furnace = Configuration.Instance.Furnaces.Find(x => x.StorageId == storageId);

                if (furnace == null)
                {
                    return;
                }

                if (!HasRecyclableItems(interactableStorage, furnace))
                {
                    if (Configuration.Instance.EnableDebugLogs)
                    {
                        Debug.Log("No recyclable items found in the storage. Aborting smelting process.");
                    }
                    return;
                }

                if (!HasRequiredFuel(interactableStorage, furnace))
                {
                    if (Configuration.Instance.EnableDebugLogs)
                    {
                        Debug.Log("Furnace does not have enough fuel. Aborting smelting process.");
                    }

                    int requiredFuelAmount = CalculateTotalFuelCost(interactableStorage, furnace);
                    string fuelName = GetFuelItemName(furnace.FuelId);

                    string notEnoughFuelMessage = Translate("not_enough_fuel", requiredFuelAmount, fuelName);
                    string chatColorCode2 = Configuration.Instance.ChatColor;
                    Color chatColor2;

                    if (ColorUtility.TryParseHtmlString(chatColorCode2, out chatColor2))
                    {
                        string iconUrl = Configuration.Instance.ChatIconUrl;
                        ChatManager.serverSendMessage(notEnoughFuelMessage, chatColor2, null, player.SteamPlayer(), EChatMode.SAY, iconUrl, false);
                    }
                    else
                    {
                        Debug.LogWarning($"Invalid color code: {chatColorCode2}");
                    }

                    return;
                }

                RemoveFuelItems(interactableStorage, furnace.FuelId, CalculateTotalFuelCost(interactableStorage, furnace));

                string chatColorCode = Configuration.Instance.ChatColor;
                Color chatColor;

                if (ColorUtility.TryParseHtmlString(chatColorCode, out chatColor))
                {
                    string iconUrl = Configuration.Instance.ChatIconUrl;
                    ChatManager.serverSendMessage(Translate("furnace_start"), chatColor, null, player.SteamPlayer(), EChatMode.SAY, iconUrl, false);
                }
                else
                {
                    Debug.LogWarning($"Invalid color code: {chatColorCode}");
                }

                StartCoroutine(SmeltItems(player, interactableStorage, furnace));
                
            }

            catch (Exception ex)
            {
                Debug.LogError($"Exception in OnPlayerUpdateGesture: {ex}");
                Rocket.Core.Logging.Logger.LogException(ex, "Exception in OnPlayerUpdateGesture()");
            }
        }




        private string GetFuelItemName(ushort fuelId)
        {
            // Use the asset database to get the fuel item by ID
            ItemAsset itemAsset = Assets.find(EAssetType.ITEM, fuelId) as ItemAsset;

            // Check if the item asset is found
            if (itemAsset != null)
            {
                
                return itemAsset.itemName;
            }

            // If the item asset is not found, return a default name 
            return "Unknown Fuel";
        }



        private bool HasRecyclableItems(InteractableStorage storage, Furnace furnace)
        {
            if (Configuration.Instance.EnableDebugLogs)
            {
                Debug.Log("Checking items in storage:");
            }

            foreach (var item in storage.items.items)
            {
                if (Configuration.Instance.EnableDebugLogs)
                {
                    Debug.Log($"Item ID: {item.item.id}, Amount: {item.item.amount}");
                }

                ushort inputId;
                if (furnace.GetRecipe(item.item.id, out inputId) != null)
                {
                    if (Configuration.Instance.EnableDebugLogs)
                    {
                        Debug.Log($"Recyclable item found - Item ID: {item.item.id}, Amount: {item.item.amount}");
                    }
                    return true;
                }
            }

            if (Configuration.Instance.EnableDebugLogs)
            {
                Debug.Log($"No recyclable items found in storage");
            }

            return false;
        }

        private bool HasRequiredFuel(InteractableStorage storage, Furnace furnace)
        {
            int totalFuelCost = CalculateTotalFuelCost(storage, furnace);

            if (totalFuelCost <= 0)
            {
                return true;
            }

            int availableFuelCount = CountFuelItems(storage, furnace.FuelId);

            if (availableFuelCount >= totalFuelCost)
            {
                return true;
            }

            return false;
        }

        private int CalculateTotalFuelCost(InteractableStorage storage, Furnace furnace)
        {
            int totalCost = 0;

            foreach (var storageItem in storage.items.items)
            {
                var itemToSmelt = storageItem.item;

                ushort inputId;
                var recipe = furnace.GetRecipe(itemToSmelt.id, out inputId);

                if (recipe != null)
                {
                    // Instead of multiplying, add the fuel cost of the item to the total cost
                    totalCost += recipe.FuelCost;
                }
            }

            return totalCost;
        }
        
        


        private int CountFuelItems(InteractableStorage storage, ushort fuelId)
        {
            int count = 0;

            foreach (var storageItem in storage.items.items)
            {
                if (storageItem.item.id == fuelId)
                {
                    count += storageItem.item.amount;
                }
            }

            return count;
        }

        private void RemoveFuelItems(InteractableStorage storage, ushort fuelId, int fuelAmount)
        {
            if (Configuration.Instance.EnableDebugLogs)
            {
                Debug.Log($"Removing {fuelAmount} fuel items with ID {fuelId} from storage.");
            }

            List<byte> itemsToRemoveIndices = new List<byte>();

            foreach (var storageItem in storage.items.items)
            {
                if (storageItem.item.id == fuelId)
                {
                    int amountToRemove = Math.Min(storageItem.item.amount, fuelAmount);
                    storageItem.item.amount -= (byte)amountToRemove;
                    fuelAmount -= amountToRemove;

                    if (fuelAmount <= 0)
                    {
                        if (Configuration.Instance.EnableDebugLogs)
                        {
                            Debug.Log($"All required fuel items removed.");
                        }
                        break;
                    }
                }
            }

            foreach (var storageItem in storage.items.items)
            {
                if (storageItem.item.amount == 0)
                {
                    itemsToRemoveIndices.Add((byte)storage.items.items.IndexOf(storageItem));
                }
            }

            itemsToRemoveIndices.Reverse();

            foreach (var index in itemsToRemoveIndices)
            {
                storage.items.removeItem(index);
            }
        }

        private System.Collections.IEnumerator SmeltItems(UnturnedPlayer player, InteractableStorage storage, Furnace furnace)
        {
            float delay = furnace.Delay;

            if (delay > 0)
            {
                if (Configuration.Instance.EnableDebugLogs)
                {
                    Debug.Log($"Delaying smelting for {delay} seconds.");
                }

                yield return new WaitForSeconds(delay);
            }

            try
            {
                SmeltNextItem(player, storage, furnace);
                
            }
            catch (Exception ex)
            {
                Debug.LogError($"Exception in SmeltItems: {ex}");
                Rocket.Core.Logging.Logger.LogException(ex, "Exception in SmeltItems()");
            }
            finally
            {
                if (Configuration.Instance.EnableDebugLogs)
                {
                    Debug.Log("Exiting SmeltItems coroutine.");
                }
                ConvertItems(storage);

            }
        }



        private void SmeltNextItem(UnturnedPlayer player, InteractableStorage storage, Furnace furnace)
        {
            foreach (var storageItem in storage.items.items)
            {
                var itemToSmelt = storageItem.item;

                ushort inputId;
                var recipe = furnace.GetRecipe(itemToSmelt.id, out inputId);

                if (recipe != null)
                {
                    int inputAmount = itemToSmelt.amount;

                    ItemAsset outputItemAsset = Assets.find(EAssetType.ITEM, recipe.OutputId) as ItemAsset;

                    if (outputItemAsset != null && outputItemAsset.amount > 1)
                    {
                        if (Configuration.Instance.EnableDebugLogs)
                        {
                            Debug.Log($"Output item asset ID {recipe.OutputId} already has an amount specified: {outputItemAsset.amount}");
                        }

                        int adjustedOutputAmount = recipe.OutputAmount * inputAmount;

                        while (adjustedOutputAmount > 0)
                        {
                            int stackSize = Math.Min(adjustedOutputAmount, Configuration.Instance.MaxStackAmount);
                            Item newItem = new Item(recipe.OutputId, (byte)stackSize, 0);

                            if (storage.items.tryAddItem(newItem, true))
                            {
                                if (Configuration.Instance.EnableDebugLogs)
                                {
                                    Debug.Log($"Added {stackSize} items of ID {recipe.OutputId} to storage.");
                                }
                                adjustedOutputAmount -= stackSize;
                               
                            }
                            else
                            {
                                if (Configuration.Instance.EnableDebugLogs)
                                {
                                    Debug.LogWarning("Not enough space in storage. Exiting the coroutine.");
                                }
                                return;
                            }
                        }
                    }
                    else
                    {
                        int outputAmount = recipe.OutputAmount <= 0 ? 1 : recipe.OutputAmount;
                        Item newItem = new Item(recipe.OutputId, (byte)outputAmount, 100);

                        while (outputAmount > 0)
                        {
                            if (storage.items.tryAddItem(newItem, true))
                            {
                                if (Configuration.Instance.EnableDebugLogs)
                                {
                                    Debug.Log($"Added {outputAmount} items of ID {recipe.OutputId} to storage.");
                                }
                                outputAmount--;
                            }
                            else
                            {
                                if (Configuration.Instance.EnableDebugLogs)
                                {
                                    Debug.LogWarning("Not enough space in storage. Exiting the coroutine.");
                                }
                                return;
                            }
                        }
                    }

                   
                    byte byteIndexToRemove = (byte)storage.items.items.IndexOf(storageItem);
                    storage.items.removeItem(byteIndexToRemove);
                    SmeltNextItem(player, storage, furnace);
                    return;

                    
                }
            }




            if (Configuration.Instance.EnableDebugLogs)
            {
                Debug.Log("Smelting finished!");
            }


            ConvertItems(storage);
            string chatColorCode = Configuration.Instance.ChatColor;
            Color chatColor;

            if (ColorUtility.TryParseHtmlString(chatColorCode, out chatColor))
            {
                string iconUrl = Configuration.Instance.ChatIconUrl;
                ChatManager.serverSendMessage(Translate("furnace_end"), chatColor, null, player.SteamPlayer(), EChatMode.SAY, iconUrl, false);
            }
            else
            {
                Debug.LogWarning($"Invalid color code: {chatColorCode}");
            }

            ushort effectId = furnace.EffectId;
            if (effectId != 0)
            {
                if (Configuration.Instance.EnableDebugLogs)
                {
                    Debug.Log($"Triggering effect with ID: {effectId} at position: {storage.transform.position} for player: {player.CharacterName}");
                }

                TriggerEffect(effectId, storage.transform.position, player.CSteamID);
            }
        }


        private void ConvertItems(InteractableStorage storage)
        {
            foreach (var conversion in StackingItemConversions)
            {
                int itemCount = CountItems(storage, conversion.ID);

                if (Configuration.Instance.EnableDebugLogs)
                {
                    Debug.Log($"Checking items for conversion - Item ID: {conversion.ID}, Count: {itemCount}");
                }

                if (itemCount >= 10)
                {
                    int stacksToConvert = itemCount / 10;

                    if (Configuration.Instance.EnableDebugLogs)
                    {
                        Debug.Log($"Converting {stacksToConvert} stacks of 10 items each.");
                    }

                    for (int i = 0; i < stacksToConvert; i++)
                    {
                        if (Configuration.Instance.EnableDebugLogs)
                        {
                            Debug.Log($"Converting stack {i + 1} of 10 items.");
                        }

                        // Remove the corresponding items from storage
                        RemoveItems(storage, conversion.ID, 10);

                        // Create and add the converted item
                        AddItems(storage, conversion.ID10x, 1);
                    }

                    // Update the stack count
                    int remainingItems = itemCount % 10;
                    if (remainingItems > 0)
                    {
                        if (Configuration.Instance.EnableDebugLogs)
                        {
                            Debug.Log($"Converting {remainingItems} individual items.");
                        }

                        // Remove the corresponding items from storage
                        RemoveItems(storage, conversion.ID, remainingItems);

                        // Create and add the converted items
                        AddItems(storage, conversion.ID10x, remainingItems);
                    }
                }
            }
        }



        private int CountItems(InteractableStorage storage, ushort itemId)
        {
            int itemCount = 0;
            foreach (var storageItem in storage.items.items)
            {
                if (storageItem.item.id == itemId)
                {
                    itemCount += storageItem.item.amount;
                }
            }
            if (Configuration.Instance.EnableDebugLogs)
            {
                Debug.Log($"Counted {itemCount} items with ID {itemId} in storage.");
            }
            return itemCount;
        }

        private void RemoveItems(InteractableStorage storage, ushort itemId, int amount)
        {
            List<byte> itemsToRemoveIndices = new List<byte>();

            foreach (var storageItem in storage.items.items)
            {
                if (storageItem.item.id == itemId)
                {
                    int amountToRemove = Math.Min(storageItem.item.amount, amount);
                    storageItem.item.amount -= (byte)amountToRemove;
                    amount -= amountToRemove;

                    if (amount <= 0)
                    {
                        break;
                    }
                }
            }

            foreach (var storageItem in storage.items.items)
            {
                if (storageItem.item.amount == 0)
                {
                    itemsToRemoveIndices.Add((byte)storage.items.items.IndexOf(storageItem));
                }
            }

            itemsToRemoveIndices.Reverse();

            foreach (var index in itemsToRemoveIndices)
            {
                storage.items.removeItem(index);
            }

            if (Configuration.Instance.EnableDebugLogs)
            {
                Debug.Log($"Removed {amount} items with ID {itemId} from storage.");
            }
        }

        private void AddItems(InteractableStorage storage, ushort itemId, int count)
        {
            for (int i = 0; i < count; i++)
            {
                SDG.Unturned.Item newItem = new SDG.Unturned.Item(itemId, 1, 0);

                if (storage.items.tryAddItem(newItem))
                {
                    if (Configuration.Instance.EnableDebugLogs)
                    {
                        Debug.Log($"Added 1 item with ID {itemId} to storage.");
                    }
                }
                else
                {
                    ItemManager.dropItem(newItem, storage.transform.position, true, true, true);
                }
            }
        }




        public void TriggerEffect(ushort effectId, Vector3 position, CSteamID relevantPlayerID)
        {
            TriggerEffectParameters parameters = new TriggerEffectParameters(effectId);
            parameters.position = position;
            parameters.relevantPlayerID = relevantPlayerID;
            EffectManager.triggerEffect(parameters);
        }

        protected override void Unload()
        {
            UnturnedPlayerEvents.OnPlayerUpdateGesture -= UnturnedPlayerEvents_OnPlayerUpdateGesture;
        }
    }
}
