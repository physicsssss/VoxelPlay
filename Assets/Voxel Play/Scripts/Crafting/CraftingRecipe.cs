using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;

[Serializable]
public struct Recipe
{
    public string name;
    public List<ItemDefinition> Items;
    [Range(1, 999)]
    public int itemCount;

    public List<VoxelDefinition> voxelItems;
    [Range(1, 999)]
    public int voxelCount;

    public ItemDefinition itemResult;
    [Range(1,999)]
    public int itemResultCount;

    public VoxelDefinition voxelResult;
    [Range(1, 999)]
    public int voxelResultCount;
}
[CreateAssetMenu(menuName ="Voxel Play/Create Recipe",fileName ="Recipe Definition")]
public class CraftingRecipe : ScriptableObject
{
    public List<Recipe> recipes;

    //public List<ItemAmount>itemResults;
    

    public ItemDefinition CanCraft(IItemContainer itemContainer)
    {
        List<ItemDefinition> listCraftables = new List<ItemDefinition>();
        foreach (Recipe recipe in recipes)
        {
            bool containsAllItems = true;
            if (recipe.Items != null)
            {
                
                foreach (ItemDefinition singleItem in recipe.Items) 
                {
                    if (!itemContainer.ContainsItem(singleItem))
                    {
                        containsAllItems = false;
                    }
                }
                

            }
            if (recipe.voxelItems != null)
            {
                foreach (VoxelDefinition singleVItem in recipe.voxelItems)
                {
                    if (!itemContainer.ContainsItem(singleVItem.dropItem))
                    {
                        containsAllItems = false;
                    }
                }
            }
        }
        return null;
    }

    public void Craft(IItemContainer itemContainer)
    {
        if (CanCraft(itemContainer))
        {
            foreach (Recipe recipe in recipes)
            {
                //for (int i = 0; i < recipe.Item.Count; i++)
                //{
                //}
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
