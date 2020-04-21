using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{
    public interface IVoxelPlayPlayer
    {
        event OnPlayerInventoryEvent OnSelectedItemChange;
        event OnPlayerGetDamageEvent OnPlayerGetDamage;
        event OnPlayerIsKilledEvent OnPlayerIsKilled;

        // Inventory related
        void AddInventoryItem (ItemDefinition [] newItems);
        bool AddInventoryItem (ItemDefinition newItem, float quantity = 1);
        void PickUpItem (ItemDefinition newItem, float quantity = 1);
        void UnSelectItem ();
        bool SetSelectedItem (int itemIndex);
        bool SetSelectedItem (InventoryItem item);
        bool SetSelectedItem (VoxelDefinition vd);
        InventoryItem GetSelectedItem ();
        List<InventoryItem> items { get; }
        int selectedItemIndex { get; set; }
        List<InventoryItem> GetPlayerItems ();
        bool HasItem (ItemDefinition item);
        InventoryItem ConsumeItem ();
        void ConsumeItem (ItemDefinition item);
        float GetItemQuantity (ItemDefinition item);

        // Combat related
        void DamageToPlayer (int damagePoints);
        float GetHitDelay ();
        float GetHitRange ();
        int GetHitDamage ();
        int GetHitDamageRadius ();

        // General character stats
        string playerName { get; set; }
        int totalLife { get; set; }
        int life { get; }
        float hitDelay { get; set; }
        int hitDamage { get; set; }
        float hitRange { get; set; }
        int hitDamageRadius { get; set; }

    }
}
