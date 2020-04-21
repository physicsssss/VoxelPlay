using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace VoxelPlay
{

    public delegate void OnPlayerInventoryEvent (int selectedItemIndex, int prevSelectedItemIndex);
    public delegate void OnPlayerGetDamageEvent (ref int damage, int remainingLifePoints);
    public delegate void OnPlayerIsKilledEvent ();

    [HelpURL ("https://kronnect.freshdesk.com/support/solutions/articles/42000001860-voxel-play-player")]
    public partial class VoxelPlayPlayer : MonoBehaviour, IVoxelPlayPlayer
    {

        public event OnPlayerInventoryEvent OnSelectedItemChange;
        public event OnPlayerGetDamageEvent OnPlayerGetDamage;
        public event OnPlayerIsKilledEvent OnPlayerIsKilled;

        [Header ("Player Info")]
        [SerializeField] string _playerName = "Player";
        [SerializeField] int _totalLife = 20;

        [Header ("Attack")]
        [SerializeField] float _hitDelay = 0.2f;
        [SerializeField] int _hitDamage = 3;
        [SerializeField] float _hitRange = 5f;
        [SerializeField] int _hitDamageRadius = 1;

        [Header ("Bare Hands")]
        public float bareHandsHitDelay = 0.2f;
        public int bareHandsHitDamage = 3;
        public float bareHandsHitRange = 5f;
        public int bareHandsHitDamageRadius = 1;

        int _selectedItemIndex;
        int _life;

        public virtual int life {
            get { return _life; }
        }

        public virtual string playerName {
            get { return _playerName; } set { _playerName = value; }
        }

        public virtual int totalLife {
            get { return _totalLife; }
            set { _totalLife = value; }
        }

        public virtual float hitDelay {
            get { return _hitDelay; }
            set { _hitDelay = value; }
        }

        public virtual float hitRange {
            get { return _hitRange; }
            set { _hitRange = value; }
        }

        public virtual int hitDamage {
            get { return _hitDamage; }
            set { _hitDamage = value; }
        }

        public virtual int hitDamageRadius {
            get { return _hitDamageRadius; }
            set { _hitDamageRadius = value; }
        }
 
        /// <summary>
        /// Gets or sets the index of the currently selected item in the player.items collection
        /// </summary>
        /// <value>The index of the selected item.</value>
        public virtual int selectedItemIndex {
            get { return _selectedItemIndex; }
            set {
                if (_selectedItemIndex != value) {
                    SetSelectedItem (value);
                }
            }
        }

        /// <summary>
        /// Returns a copy of currently selected item (note it's a struct) or InventoryItem.Null if nothing selected.
        /// </summary>
        /// <returns>The selected item.</returns>
        public virtual InventoryItem GetSelectedItem ()
        {
            if (this.items == null) {
                return InventoryItem.Null;
            }

            List<InventoryItem> items = this.items;
            if (_selectedItemIndex >= 0 && _selectedItemIndex < items.Count) {
                return items [_selectedItemIndex];
            }
            return InventoryItem.Null;
        }


        /// <summary>
        /// Unselects any item selected
        /// </summary>
        public virtual void UnSelectItem ()
        {
            _selectedItemIndex = -1;
            ShowSelectedItem ();
        }


        /// <summary>
        /// Selected item by item index
        /// </summary>
        public virtual bool SetSelectedItem (int itemIndex)
        {
            if (this.items == null) {
                return false;
            }

            if (itemIndex >= 0 && itemIndex < items.Count) {
                int prevItemIndex = _selectedItemIndex;
                _selectedItemIndex = itemIndex;
                if (items [_selectedItemIndex].item == null) {
                    _selectedItemIndex = -1;
                    return false;
                }
                hitDamage = items [_selectedItemIndex].item.GetPropertyValue<int> ("hitDamage", bareHandsHitDamage);
                hitDelay = items [_selectedItemIndex].item.GetPropertyValue<float> ("hitDelay", bareHandsHitDelay);
                hitRange = items [_selectedItemIndex].item.GetPropertyValue<float> ("hitRange", bareHandsHitRange);
                hitDamageRadius = items [_selectedItemIndex].item.GetPropertyValue<int> ("hitDamageRadius", bareHandsHitDamageRadius);

                ShowSelectedItem ();
                if (OnSelectedItemChange != null) {
                    OnSelectedItemChange (_selectedItemIndex, prevItemIndex);
                }
            }
            return true;
        }

        public virtual void DamageToPlayer (int damagePoints)
        {
            if (damagePoints > 0 && OnPlayerGetDamage != null) {
                OnPlayerGetDamage (ref damagePoints, _life - damagePoints);
            }
            _life -= damagePoints;
            if (_life <= 0) {
                if (OnPlayerIsKilled != null)
                    OnPlayerIsKilled ();
            }
        }



        /// <summary>
        /// Selects item by item object
        /// </summary>
        /// <param name="item">Item.</param>
        public virtual bool SetSelectedItem (InventoryItem item)
        {
            if (this.items == null) {
                return false;
            }

            List<InventoryItem> items = this.items;

            int count = items.Count;
            for (int k = 0; k < count; k++) {
                if (items [k] == item) {
                    selectedItemIndex = k;
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Selects an item from inventory by its voxel definition type
        /// </summary>
        public virtual bool SetSelectedItem (VoxelDefinition vd)
        {
            if (this.items == null) {
                return false;
            }

            List<InventoryItem> items = this.items;
            int count = items.Count;
            for (int k = 0; k < count; k++) {
                InventoryItem item = items [k];
                if (item.item.category == ItemCategory.Voxel && item.item.voxelType == vd) {
                    selectedItemIndex = k;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Returns a list of currently available items. If build mode is ON, it returns all world items. If buld mode is OFF, it returns playerItems.
        /// </summary>
        /// <value>The current items.</value>
        public virtual  List<InventoryItem> items {
            get {
                VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
                if (env != null && env.buildMode) {
                    return env.allItems;
                }
                return playerItems;
            }
        }


        /// <summary>
        /// The list of items in player inventory (defined in inspector)
        /// </summary>
        [Header ("Items")]
        public List<InventoryItem> playerItems;

        /// <summary>
        /// Synonym for playerItems
        /// </summary>
        public virtual List<InventoryItem> GetPlayerItems ()
        {
            return playerItems;
        }

        AudioSource _audioSource;

        /// <summary>
        /// Returns the AudioSource component attached to the player gameobject
        /// </summary>
        /// <value>The audio source.</value>
        public virtual AudioSource audioSource {
            get {
                if (_audioSource == null) {
                    _audioSource = transform.GetComponentInChildren<AudioSource> (true);
                }
                return _audioSource;
            }
        }

        static IVoxelPlayPlayer _player;

        /// <summary>
        /// Gets the reference to the player component. The player component contains info like name, life and inventory.
        /// </summary>
        /// <value>The instance.</value>
        public static IVoxelPlayPlayer instance {
            get {
                if (_player == null) {
                    VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
                    if (env != null && env.playerGameObject != null) {
                        _player = env.playerGameObject.GetComponentInChildren<VoxelPlayPlayer> ();
                        if (_player == null) {
                            _player = env.playerGameObject.AddComponent<VoxelPlayPlayer> ();
                        }
                    }
                }
                return _player;
            }
        }


        void OnEnable ()
        {
            InitPlayerInventory ();
        }


        protected virtual void InitPlayerInventory ()
        {
            if (items == null) {
                playerItems = new List<InventoryItem> (250);
            }

            _selectedItemIndex = -1;
            ShowSelectedItem ();
        }


        protected void ShowSelectedItem ()
        {
            if (items == null) {
                return;
            }
            VoxelPlayUI ui = VoxelPlayUI.instance;
            if (ui != null) {
                if (_selectedItemIndex >= 0 && _selectedItemIndex < items.Count) {
                    ui.ShowSelectedItem (items [_selectedItemIndex]);
                } else {
                    ui.HideSelectedItem ();
                }
            }
        }

        /// <summary>
        /// Adds a range of items to the inventory
        /// </summary>
        /// <param name="newItems">Items.</param>
        public virtual void AddInventoryItem (ItemDefinition [] newItems)
        {
            if (newItems == null) {
                return;
            }

            for (int k = 0; k < newItems.Length; k++) {
                AddInventoryItem (newItems [k]);
            }
        }


        /// <summary>
        /// Called when character picks an object from the scene
        /// </summary>
        /// <param name="newItem"></param>
        /// <param name="quantity"></param>
        public virtual void PickUpItem (ItemDefinition newItem, float quantity = 1)
        {
            if (newItem == null || quantity <= 0) return;
            {
                if (audioSource == null)
                    return;
                if (newItem.pickupSound != null) {
                    audioSource.PlayOneShot (newItem.pickupSound);
                } else if (VoxelPlayEnvironment.instance.defaultPickupSound != null) {
                    audioSource.PlayOneShot (VoxelPlayEnvironment.instance.defaultPickupSound);
                }
                AddInventoryItem (newItem, quantity);
            }
        }


        /// <summary>
        /// Adds a new item to the inventory
        /// </summary>
        public virtual bool AddInventoryItem (ItemDefinition newItem, float quantity = 1)
        {
            if (newItem == null || items == null) {
                return false;
            }

            // Check if item is already in inventory
            int itemsCount = items.Count;
            InventoryItem i;
            for (int k = 0; k < itemsCount; k++) {
                if (items [k].item == newItem) {
                    i = items [k];
                    i.quantity += quantity;
                    items [k] = i;
                    ShowSelectedItem ();
                    return false;
                }
            }
            i = new InventoryItem ();
            i.item = newItem;
            i.quantity = quantity;
            items.Add (i);

            if (_selectedItemIndex < 0) {
                selectedItemIndex = items.Count - 1;
                ShowSelectedItem ();
            }

            return true;
        }

        /// <summary>
        /// Delay between weapon hits
        /// </summary>
        public virtual float GetHitDelay ()
        {
            return hitDelay;
        }

        /// <summary>
        /// Delay between weapon hits
        /// </summary>
        public virtual float GetHitRange ()
        {
            return hitRange;
        }


        /// <summary>
        /// Get current hit damange
        /// </summary>
        public virtual int GetHitDamage ()
        {
            return hitDamage;
        }



        /// <summary>
        /// Get current hit damange radius
        /// </summary>
        public virtual int GetHitDamageRadius ()
        {
            return hitDamageRadius;
        }


        /// <summary>
        /// Reduces one unit from currently selected item and returns a copy of the InventoryItem or InventoryItem.Null if nothing selected
        /// </summary>
        public virtual InventoryItem ConsumeItem ()
        {
            if (this.items == null) {
                return InventoryItem.Null;
            }

            List<InventoryItem> items = this.items;

            if (_selectedItemIndex >= 0 && _selectedItemIndex < items.Count) {
                InventoryItem i = items [_selectedItemIndex];
                i.quantity--;
                if (i.quantity <= 0) {
                    items.RemoveAt (_selectedItemIndex);
                    selectedItemIndex = 0;
                } else {
                    items [_selectedItemIndex] = i; // update back because it's a struct
                }
                ShowSelectedItem ();
                return i;
            }
            return InventoryItem.Null;
        }

        /// <summary>
        /// Reduces one unit from player inventory. 
        /// </summary>
        /// <param name="item">Item.</param>
        public virtual void ConsumeItem (ItemDefinition item)
        {
            if (items == null) {
                return;
            }

            int itemCount = items.Count;
            for (int k = 0; k < itemCount; k++) {
                if (items [k].item == item) {
                    InventoryItem i = items [_selectedItemIndex];
                    i.quantity--;
                    if (i.quantity <= 0) {
                        items.RemoveAt (k);
                        selectedItemIndex = 0;
                    } else {
                        items [_selectedItemIndex] = i; // update back because it's a struct
                    }
                    break;
                }
            }
            ShowSelectedItem ();
        }


        /// <summary>
        /// Returns true if player has this item in the inventory
        /// </summary>
        public virtual bool HasItem (ItemDefinition item)
        {
            return GetItemQuantity (item) > 0;
        }



        /// <summary>
        /// Returns the number of units of a ItemDefinition the player has (if any)
        /// </summary>
        public virtual float GetItemQuantity (ItemDefinition item)
        {
            if (items == null) {
                return 0;
            }

            int itemCount = items.Count;
            for (int k = 0; k < itemCount; k++) {
                if (items [k].item == item) {
                    InventoryItem i = items [_selectedItemIndex];
                    return i.quantity;
                }
            }
            return 0;
        }


    }
}
