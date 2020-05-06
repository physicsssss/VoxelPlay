using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;

public class BrickBuilder : MonoBehaviour
{
    public static BrickBuilder _instance;

    public enum TypeOfBuilder
    {
        normal,
        advanced
    }

    public TypeOfBuilder typeOfBuilder = TypeOfBuilder.normal;

    public int normalBrickBuilderQuantity;
    public int advancedBrickBuilderQuantity;
    public List<BrickVoxels> brickVoxels;
    public List<VoxelDefinition> ignoreList;
    private List<ItemDefinition> alreadyBuiltBricks = new List<ItemDefinition> ();

    void Awake ()
    {
        if (_instance == null)
            _instance = this;
    }

    public void BuildBrick (ItemDefinition itemToCreateBrickOutOf, int quantity)
    {
        if (itemToCreateBrickOutOf.category != ItemCategory.Voxel)
            return;

        if (CheckIgnoreList (itemToCreateBrickOutOf))
        {
            Debug.LogError("Item in ignore list, Cannot build brick.");
            return;
        }

        Debug.Log ("Building bricks");

        switch (typeOfBuilder)
        {
            case TypeOfBuilder.normal:
                if (quantity < normalBrickBuilderQuantity)
                    Debug.LogError ("Brick Cannot be built. item quantity not enough." + (normalBrickBuilderQuantity - quantity) + " more " + itemToCreateBrickOutOf.voxelType + " required");
                else
                {
                    if (GetNewBrick (itemToCreateBrickOutOf) != null)
                    {
                        for (int i = 0; i < quantity / normalBrickBuilderQuantity; i++)
                        {
                            VoxelPlayPlayer.instance.AddInventoryItem (GetNewBrick (itemToCreateBrickOutOf));
                        }

                        Debug.Log ("normalBrickBuilderQuantity * (quantity / normalBrickBuilderQuantity): " + normalBrickBuilderQuantity * (quantity / normalBrickBuilderQuantity));

                        for (int i = 0; i < normalBrickBuilderQuantity * (quantity / normalBrickBuilderQuantity); i++)
                        {
                            Debug.Log ("Removing items: " + i);
                            VoxelPlayPlayer.instance.ConsumeItem (itemToCreateBrickOutOf);
                        }
                    }
                }
                break;
            case TypeOfBuilder.advanced:
                if (quantity < advancedBrickBuilderQuantity)
                    Debug.LogError ("Brick Cannot be built. item quantity not enough." + (advancedBrickBuilderQuantity - quantity) + " more " + itemToCreateBrickOutOf.voxelType + " required");
                else
                {
                    if (GetNewBrick (itemToCreateBrickOutOf) != null)
                    {
                        for (int i = 0; i < quantity / advancedBrickBuilderQuantity; i++)
                        {
                            VoxelPlayPlayer.instance.AddInventoryItem (GetNewBrick (itemToCreateBrickOutOf));
                        }

                        for (int i = 0; i < advancedBrickBuilderQuantity * (quantity / advancedBrickBuilderQuantity); i++)
                        {
                            VoxelPlayPlayer.instance.ConsumeItem (itemToCreateBrickOutOf);
                        }
                    }
                }
                break;

        }
    }

    private ItemDefinition GetNewBrick (ItemDefinition itemToCreateBrickOutOf)
    {
        ItemDefinition newItem = new ItemDefinition ();

        newItem = Instantiate (itemToCreateBrickOutOf);
        newItem.title = itemToCreateBrickOutOf.voxelType.name + "Brick";

        for (int i = 0; i < brickVoxels.Count; i++)
        {
            if (newItem.title.Equals (brickVoxels[i].voxelType.name))
            {
                newItem.voxelType = brickVoxels[i].voxelType;
                newItem.icon = newItem.voxelType.icon;
                break;
            }
            else
            {
                Debug.LogError ("Brick does not exist. Please add brick voxel in brickVoxels Array");
                return null;
            }
        }

        for (int i = 0; i < alreadyBuiltBricks.Count; i++)
        {
            if (alreadyBuiltBricks[i].title.Equals (newItem.title))
                return alreadyBuiltBricks[i];
        }

        alreadyBuiltBricks.Add (newItem);
        return newItem;
    }

    public bool CheckIgnoreList (ItemDefinition item)
    {
        for (int i = 0; i < ignoreList.Count; i++)
        {
            if (item.voxelType == ignoreList[i])
            {
                return true;
            }
        }

        return false;
    }

    public void RemoveAlreadyBuiltBricks (ItemDefinition brickToRemove)
    {
        if (alreadyBuiltBricks.Contains (brickToRemove))
            alreadyBuiltBricks.Remove (brickToRemove);
    }
}

[System.Serializable]
public class BrickVoxels
{
    public string name;
    public VoxelDefinition voxelType;
}