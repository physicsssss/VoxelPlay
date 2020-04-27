
using VoxelPlay;

public interface IItemContainer
{
    bool ContainsItem(ItemDefinition item);
    bool RemoveItem(ItemDefinition item);
    bool AddItem(ItemDefinition item);
    bool IsFull();
    int ItemCount(ItemDefinition item);
}
