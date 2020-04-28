using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelPlay;

[Serializable]
public partial struct RecipeItem
{
    [Tooltip("Name is just for personal reference")]
    public string name;
    public List<RecipeItemDefinition> Items;
    public List<RecipeVoxelDefinition> voxelItems;
    

    public ItemDefinition itemResult;
    [Range(1,999)]
    public int itemResultCount;
    

}
[Serializable]
public struct RecipeItemDefinition
{
    public ItemDefinition item;
    public int count;
}
[Serializable]
public struct RecipeVoxelDefinition
{
    public VoxelDefinition item;
    public int count;
}

[CreateAssetMenu(menuName ="Voxel Play/Create Recipe",fileName ="Recipe Definition")]
public class CraftingRecipe : ScriptableObject
{
    public List<RecipeItem> recipes;

   

}
