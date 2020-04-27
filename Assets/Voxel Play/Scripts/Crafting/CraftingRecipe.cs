using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;

[Serializable]
public struct Recipe
{
    public string name;
    public List<ItemDefinition> Item;
    [Range(1, 999)]
    public int itemCount;

    public List<VoxelDefinition> voxelItem;
    [Range(1, 999)]
    public int voxelCount;

    public List<ItemDefinition> itemResult;
    [Range(1,999)]
    public int itemResultCount;

    public List<VoxelDefinition> voxelResult;
    [Range(1, 999)]
    public int voxelResultCount;
}
[CreateAssetMenu(menuName ="Voxel Play/Create Recipe",fileName ="Recipe Definition")]
public class CraftingRecipe : ScriptableObject
{
    public List<Recipe> recipes;

    //public List<ItemAmount>itemResults;
    

    public bool CanCraft(IItemContainer itemContainer)
    {
        foreach (Recipe recipe in recipes)
        {
            
        }
        return true;
    }

    public void Craft(IItemContainer itemContainer)
    {
        if (CanCraft(itemContainer))
        {
            foreach (Recipe recipe in recipes)
            {
                for (int i = 0; i < recipe.Item.Count; i++)
                {
                }
            }
        }
        if (CanCraft(itemContainer))
        {
            //foreach (ItemAmount itemAmount in itemResults)
            //{
            //    for (int i = 0; i < itemAmount.count; i++)
            //    {
            //        itemContainer.AddItem(itemAmount.Item);
            //    }
            //}
        }
    }

}
