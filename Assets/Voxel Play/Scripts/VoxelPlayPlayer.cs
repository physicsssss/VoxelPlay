using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;


namespace VoxelPlay
{

    public delegate void OnPlayerInventoryEvent(int selectedItemIndex, int prevSelectedItemIndex);
    public delegate void OnPlayerGetDamageEvent(ref int damage, int remainingLifePoints);
    public delegate void OnPlayerIsKilledEvent();

    [HelpURL("https://kronnect.freshdesk.com/support/solutions/articles/42000001860-voxel-play-player")]
    public partial class VoxelPlayPlayer : MonoBehaviour, IVoxelPlayPlayer
    {
        public event OnPlayerInventoryEvent OnSelectedItemChange;
        public event OnPlayerGetDamageEvent OnPlayerGetDamage;
        public event OnPlayerIsKilledEvent OnPlayerIsKilled;


        [Header("Player Info")]
        [SerializeField] string _playerName = "Player";
        [SerializeField] int _totalLife = 20;
        [SerializeField] Transform _pickupTransform;
        [SerializeField] Transform weaponIK;

        [Header("Attack")]
        [SerializeField] float _hitDelay = 0.2f;
        [SerializeField] int _hitDamage = 3;
        [SerializeField] float _hitRange = 30f;
        [SerializeField] int _hitDamageRadius = 1;
        [SerializeField] int _durability = 1;
        // [SerializeField] int hitItemType = 1;
        [SerializeField] int _inventorySize = 16; // Ayaz: Added for custom inventory size
        [SerializeField] int _recipeSize = 16; //Ahmed: idk why but i am just copying ^ :p
        [SerializeField] bool _isInventoryFull = false; // Ayaz: Added tp check if inventory is full
        [SerializeField] int setInventorySizeDemo = 0; // Ayaz: just to test Remove afterwords

        [Header("Bare Hands")]
        public float bareHandsHitDelay = 0.2f;
        public int bareHandsHitDamage = 3;
        public float bareHandsHitRange = 30f;
        public int bareHandsHitDamageRadius = 1;
        public int bareHandsDurability = -1; // -1 = infinite
        public WeaponType bareHandsType;

        int _selectedItemIndex;
        int _life;

        public virtual int life
        {
            get { return _life; }
        }

        public virtual string playerName
        {
            get { return _playerName; }
            set { _playerName = value; }
        }

        public virtual int totalLife
        {
            get { return _totalLife; }
            set { _totalLife = value; }
        }

        public virtual float hitDelay
        {
            get { return _hitDelay; }
            set { _hitDelay = value; }
        }

        public virtual float hitRange
        {
            get { return _hitRange; }
            set { _hitRange = value; }
        }

        public virtual int hitDamage
        {
            get { return _hitDamage; }
            set { _hitDamage = value; }
        }

        public virtual int hitDamageRadius
        {
            get { return _hitDamageRadius; }
            set { _hitDamageRadius = value; }
        }

        public virtual int durability
        {
            get { return _durability; }
            set { _durability = value; }
        }

        public virtual int inventorySize
        {
            get { return _inventorySize; }
            set { _inventorySize = value; }
        }
        public virtual int recipeSize
        {
            get { return _recipeSize; }
            set { _recipeSize = value; }
        }
        public virtual bool isInventoryFull
        {
            get { return _isInventoryFull; }
            set { _isInventoryFull = value; }
        }
        public virtual Transform pickupTransform
        {
            get { return _pickupTransform; }
            set { _pickupTransform = value; }
        }

        /// <summary>
        /// Gets or sets the index of the currently selected item in the player.items collection
        /// </summary>
        /// <value>The index of the selected item.</value>
        public virtual int selectedItemIndex
        {
            get { return _selectedItemIndex; }
            set
            {
                if (_selectedItemIndex != value)
                {
                    SetSelectedItem(value);
                }
            }
        }

        private void Update()
        { // Ayaz: Just to test, remove afterwards
            bleedHp += recoverBleedSpeed * Time.deltaTime;
            if (bleedHp > MaxbleedHp)
            {
                bleedHp = MaxbleedHp;
            }
            BleedBehavior.minBloodAmount = maxBloodIndication * (MaxbleedHp - bleedHp) / MaxbleedHp;

            if (Input.GetKey(KeyCode.P))
                SetInventorySize(setInventorySizeDemo);
        }

        /// <summary>
        /// Returns a copy of currently selected item (note it's a struct) or InventoryItem.Null if nothing selected.
        /// </summary>
        /// <returns>The selected item.</returns>
        public virtual InventoryItem GetSelectedItem()
        {
            if (this.items == null)
            {
                return InventoryItem.Null;
            }

            List<InventoryItem> items = this.items;
            if (_selectedItemIndex >= 0 && _selectedItemIndex < items.Count)
            {
                return items[_selectedItemIndex];
            }

            if(items.Count.Equals(0))
            {
                ItemProperty[] properties = 
                {
                    new ItemProperty{ name = "hitDamage", value = bareHandsHitDamage.ToString()},
                    new ItemProperty{ name = "hitDelay", value = bareHandsHitDelay.ToString()},
                    new ItemProperty{ name = "hitRange", value = bareHandsHitRange.ToString()},
                    new ItemProperty{ name = "durability", value = bareHandsDurability.ToString()}
                };

                return new InventoryItem 
                { 
                    item = new ItemDefinition
                    {
                        category = ItemCategory.General,
                        weaponType = bareHandsType,
                        properties = properties
                    }, 
                    quantity = 1, 
                    durability = -1 
                };
            }
            return InventoryItem.Null;
        }


        /// <summary>
        /// Unselects any item selected
        /// </summary>
        public virtual void UnSelectItem()
        {
            _selectedItemIndex = -1;
            ShowSelectedItem();
        }


        /// <summary>
        /// Selected item by item index
        /// </summary>
        public virtual bool SetSelectedItem(int itemIndex)
        {
            if (this.items == null)
            {
                return false;
            }

            if (itemIndex >= 0 && itemIndex < items.Count)
            {
                int prevItemIndex = _selectedItemIndex;
                _selectedItemIndex = itemIndex;
                if (items[_selectedItemIndex].item == null)
                {
                    _selectedItemIndex = -1;
                    return false;
                }
                PrintLog("Selected Item Index -> " + _selectedItemIndex);
                hitDamage = items[_selectedItemIndex].item.GetPropertyValue<int>("hitDamage", bareHandsHitDamage);
                hitDelay = items[_selectedItemIndex].item.GetPropertyValue<float>("hitDelay", bareHandsHitDelay);
                // * hitRange = 30; Faiq
                hitRange = items[_selectedItemIndex].item.GetPropertyValue<float>("hitRange", bareHandsHitRange);//TODO: RESET RANGE items [_selectedItemIndex].item.GetPropertyValue<float> ("hitRange", bareHandsHitRange);
                hitDamageRadius = items[_selectedItemIndex].item.GetPropertyValue<int>("hitDamageRadius", bareHandsHitDamageRadius);

                // * Faiq: Durability code
                durability = items[_selectedItemIndex].durability;
                // * Faiq: Durability code

                for (int i = 0; i < weaponIK.childCount; i++)
                {
                    Destroy(weaponIK.GetChild(i).gameObject);
                }

                GameObject weapon = Instantiate(items[_selectedItemIndex].item.prefab, weaponIK);
                weapon.transform.localScale = new Vector3(3, 3, 3);

                ShowSelectedItem();

                if (OnSelectedItemChange != null)
                {
                    OnSelectedItemChange(_selectedItemIndex, prevItemIndex);
                }
            }
            return true;
        }


        // * Faiq: Durability code
        public void ChangeDurability(int value, bool exactValue = false)
        {
            PrintLog("ChangeDurability -> " + value + ", " + durability);
            if (durability.Equals(-1)) return;

            durability = exactValue ? value : durability + value;

            InventoryItem i = items[_selectedItemIndex];
            i.durability = durability;
            items[_selectedItemIndex] = i;

            if (durability.Equals(0))
                ConsumeItem();
        }

        // * Faiq: Durability code
        float MaxbleedHp = 100;
        float bleedHp = 100;
        float recoverBleedSpeed = 2;
        [SerializeField]
        private float damageBloodAmount = 3; //amount of blood when taking damage (relative to damage taken (relative to HP remaining))
        [SerializeField]
        private float maxBloodIndication = 0.5f; //max amount of blood when not taking damage (relative to HP lost)
        public virtual void DamageToPlayer(int damagePoints)
        {
            BleedBehavior.BloodAmount += Mathf.Clamp01(damageBloodAmount * damagePoints / bleedHp);
            if (damagePoints > 0 && OnPlayerGetDamage != null)
            {
                OnPlayerGetDamage(ref damagePoints, _life - damagePoints);
            }
            _life -= damagePoints;
            if (_life <= 0)
            {
                if (OnPlayerIsKilled != null)
                    OnPlayerIsKilled();
            }
            BleedBehavior.minBloodAmount = maxBloodIndication * (MaxbleedHp - bleedHp) / MaxbleedHp;
        }



        /// <summary>
        /// Selects item by item object
        /// </summary>
        /// <param name="item">Item.</param>
        public virtual bool SetSelectedItem(InventoryItem item)
        {
            if (this.items == null)
            {
                return false;
            }

            List<InventoryItem> items = this.items;

            int count = items.Count;
            for (int k = 0; k < count; k++)
            {
                if (items[k] == item)
                {
                    selectedItemIndex = k;
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Selects an item from inventory by its voxel definition type
        /// </summary>
        public virtual bool SetSelectedItem(VoxelDefinition vd)
        {
            if (this.items == null)
            {
                return false;
            }

            List<InventoryItem> items = this.items;
            int count = items.Count;
            for (int k = 0; k < count; k++)
            {
                InventoryItem item = items[k];
                if (item.item.category == ItemCategory.Voxel && item.item.voxelType == vd)
                {
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
        public virtual List<InventoryItem> items
        {
            get
            {
                VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
                if (env != null && env.buildMode)
                {
                    //Debug.Log("env.allItems");
                    return env.allItems;
                }
                //Debug.Log("env.allItems");

                return playerItems;
            }
        }


        /// <summary>
        /// The list of items in player inventory (defined in inspector)
        /// </summary>
        [Header("Items")]
        public List<InventoryItem> playerItems;

        /// <summary>
        /// Synonym for playerItems
        /// </summary>
        public virtual List<InventoryItem> GetPlayerItems()
        {
            return playerItems;
        }

        AudioSource _audioSource;

        /// <summary>
        /// Returns the AudioSource component attached to the player gameobject
        /// </summary>
        /// <value>The audio source.</value>
        public virtual AudioSource audioSource
        {
            get
            {
                if (_audioSource == null)
                {
                    _audioSource = transform.GetComponentInChildren<AudioSource>(true);
                }
                return _audioSource;
            }
        }

        static IVoxelPlayPlayer _player;

        /// <summary>
        /// Gets the reference to the player component. The player component contains info like name, life and inventory.
        /// </summary>
        /// <value>The instance.</value>
        public static IVoxelPlayPlayer instance
        {
            get
            {
                if (_player == null)
                {
                    VoxelPlayEnvironment env = VoxelPlayEnvironment.instance;
                    if (env != null && env.playerGameObject != null)
                    {
                        _player = env.playerGameObject.GetComponentInChildren<VoxelPlayPlayer>();
                        if (_player == null)
                        {
                            _player = env.playerGameObject.AddComponent<VoxelPlayPlayer>();
                        }
                    }
                }
                return _player;
            }
        }


        void OnEnable()
        {
            InitPlayerInventory();
        }


        protected virtual void InitPlayerInventory()
        {
            if (items == null)
            {
                playerItems = new List<InventoryItem>(250);
            }

            _selectedItemIndex = -1;
            ShowSelectedItem();
        }


        protected void ShowSelectedItem()
        {
            if (items == null)
            {
                return;
            }
            VoxelPlayUI ui = VoxelPlayUI.instance;
            if (ui != null)
            {
                if (_selectedItemIndex >= 0 && _selectedItemIndex < items.Count)
                {
                    ui.ShowSelectedItem(items[_selectedItemIndex]);
                }
                else
                {
                    ui.HideSelectedItem();
                }
            }
        }

        /// <summary>
        /// Adds a range of items to the inventory
        /// </summary>
        /// <param name="newItems">Items.</param>
        public virtual void AddInventoryItem(ItemDefinition[] newItems)
        {
            if (newItems == null)
            {
                return;
            }

            for (int k = 0; k < newItems.Length; k++)
            {
                AddInventoryItem(newItems[k]);
            }
        }


        /// <summary>
        /// Called when character picks an object from the scene
        /// </summary>
        /// <param name="newItem"></param>
        /// <param name="quantity"></param>
        public virtual void PickUpItem(ItemDefinition newItem, float quantity = 1)
        {
            if (newItem == null || quantity <= 0) return;
            {
                if (audioSource == null)
                    return;
                if (newItem.pickupSound != null)
                {
                    audioSource.PlayOneShot(newItem.pickupSound);
                }
                else if (VoxelPlayEnvironment.instance.defaultPickupSound != null)
                {
                    audioSource.PlayOneShot(VoxelPlayEnvironment.instance.defaultPickupSound);
                }
                AddInventoryItem(newItem, quantity);
            }
        }


        /// <summary>
        /// Adds a new item to the inventory
        /// </summary>
        public virtual bool AddInventoryItem(ItemDefinition newItem, float quantity = 1)
        {
            if (newItem == null || items == null)
            {
                return false;
            }

            // Check if item is already in inventory
            int itemsCount = items.Count;
            InventoryItem i;
            for (int k = 0; k < itemsCount; k++)
            {
                if (items[k].item == newItem)
                {
                    // Debug.Log("VoxelPlayPlayer: AddInventoryItem -> " + items[k].title);
                    i = items[k];
                    i.quantity += quantity;
                    i.durability = items[k].item.GetPropertyValue<int>("durability", bareHandsDurability);
                    items[k] = i;
                    ShowSelectedItem();
                    return false;
                }
            }

            if (isInventoryFull) // Ayaz: Check if there is no space available for new items in inventory
                return false;

            i = new InventoryItem();
            i.item = newItem;
            i.quantity = quantity;
            i.durability = newItem.GetPropertyValue<int>("durability", bareHandsDurability);
            items.Add(i);
            //PrintLog("Adding -> " + i.item.title);
            //PrintLog("_selectedItemIndex -> " + _selectedItemIndex);
            if (_selectedItemIndex < 0)
            {
                selectedItemIndex = items.Count - 1;
                ShowSelectedItem();
            }

            if (items.Count >= inventorySize)
            {
                isInventoryFull = true;
            }

            return true;
        }

        void PrintLog(string log)
        {
            Debug.Log("VoxelPlaPlayer: " + log);
        }

        /// <summary>
        /// Checks if item is already present in the inventory outside of this class
        /// </summary>
        public virtual bool ExternalCheckIfItemIsInInventory(ItemDefinition newItem)
        {
            int itemsCount = items.Count;
            for (int k = 0; k < itemsCount; k++)
            {
                if (items[k].item == newItem)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Sets inventory size and refreshes UI
        /// </summary>
        public virtual void SetInventorySize(int newSize)
        {
            if (newSize < 0)
                return;

            if (newSize < inventorySize)
                isInventoryFull = true;
            else if (newSize > inventorySize && playerItems.Count < newSize)
                isInventoryFull = false;

            inventorySize = newSize;
            VoxelPlayUIDefault.instance.AccessToggleInventoryVisibility(true);
        }

        /// <summary>
        /// Delay between weapon hits
        /// </summary>
        public virtual float GetHitDelay()
        {
            return hitDelay;
        }

        /// <summary>
        /// Delay between weapon hits
        /// </summary>
        public virtual float GetHitRange()
        {
            return hitRange;
        }


        /// <summary>
        /// Get current hit damange
        /// </summary>
        public virtual int GetHitDamage()
        {
            return hitDamage;
        }



        /// <summary>
        /// Get current hit damange radius
        /// </summary>
        public virtual int GetHitDamageRadius()
        {
            return hitDamageRadius;
        }

        public virtual int GetInventorySize()
        {
            return inventorySize;
        }

        public virtual int GetRecipeSize()
        {
            return recipeSize;
        }
        public virtual bool GetInventoryFullStatus()
        {
            return isInventoryFull;
        }

        /// <summary>
        /// Reduces one unit from currently selected item and returns a copy of the InventoryItem or InventoryItem.Null if nothing selected
        /// </summary>
        public virtual InventoryItem ConsumeItem()
        {
            if (this.items == null)
            {
                return InventoryItem.Null;
            }

            List<InventoryItem> items = this.items;

            if (_selectedItemIndex >= 0 && _selectedItemIndex < items.Count)
            {
                InventoryItem i = items[_selectedItemIndex];
                i.quantity--;
                if (i.quantity <= 0)
                {
                    for (int j = 0; j < weaponIK.childCount; j++)
                    {
                        Destroy(weaponIK.GetChild(j).gameObject);
                    }
                    items.RemoveAt(_selectedItemIndex);
                    selectedItemIndex = 0;
                    isInventoryFull = false; // Ayaz: space now available in inventory upon removal of an item
                    BrickBuilder._instance.RemoveAlreadyBuiltBricks(items[_selectedItemIndex].item); // Ayaz: remove brick
                }
                else
                {
                    durability = items[_selectedItemIndex].item.GetPropertyValue<int>("durability", bareHandsDurability);
                    i.durability = durability;
                    items[_selectedItemIndex] = i; // update back because it's a struct
                }
                ShowSelectedItem();
                return i;
            }
            return InventoryItem.Null;
        }

        /// <summary>
        /// Reduces one unit from player inventory. 
        /// </summary>
        /// <param name="item">Item.</param>
        public virtual void ConsumeItem(ItemDefinition item)
        {
            if (items == null)
            {
                return;
            }

            int itemCount = items.Count;
            for (int k = 0; k < itemCount; k++)
            {
                if (items[k].item == item)
                {
                    InventoryItem i = items[_selectedItemIndex];
                    i.quantity--;
                    if (i.quantity <= 0)
                    {
                        items.RemoveAt(k);
                        selectedItemIndex = 0;
                        isInventoryFull = false; // Ayaz: space now available in inventory upon removal of an item
                        BrickBuilder._instance.RemoveAlreadyBuiltBricks(items[_selectedItemIndex].item); // Ayaz: remove brick
                    }
                    else
                    {
                        items[_selectedItemIndex] = i; // update back because it's a struct
                    }
                    break;
                }
            }
            ShowSelectedItem();
        }


        /// <summary>
        /// Returns true if player has this item in the inventory
        /// </summary>
        public virtual bool HasItem(ItemDefinition item)
        {
            return GetItemQuantity(item) > 0;
        }



        /// <summary>
        /// Returns the number of units of a ItemDefinition the player has (if any)
        /// </summary>
        public virtual float GetItemQuantity(ItemDefinition item)
        {
            if (items == null)
            {
                return 0;
            }

            int itemCount = items.Count;
            for (int k = 0; k < itemCount; k++)
            {
                if (items[k].item == item)
                {
                    InventoryItem i = items[_selectedItemIndex];
                    return i.quantity;
                }
            }
            return 0;
        }

        public void OpenInventoryAndStopPlayer()
        {
            VoxelPlayUIDefault.instance.AccessToggleInventoryVisibility(true);

            GetComponent<VoxelPlayFirstPersonController>()?.ToggleCharacterController(false);
            GetComponent<VoxelPlayThirdPersonController>()?.ToggleCharacterController(false);
        }

        public void CloseInventoryAndStopPlayer()
        {
            VoxelPlayUIDefault.instance.AccessToggleInventoryVisibility(true);

            GetComponent<VoxelPlayFirstPersonController>()?.ToggleCharacterController(true);
            GetComponent<VoxelPlayThirdPersonController>()?.ToggleCharacterController(true);
        }

        public void BuildBricks()
        {
            Debug.Log("Selected item: " + selectedItemIndex + ": " + items[selectedItemIndex].item.voxelType);
            BrickBuilder._instance.BuildBrick(items[_selectedItemIndex].item, Mathf.FloorToInt(items[_selectedItemIndex].quantity));
        }

    }

}
