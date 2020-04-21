using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{

	[Serializable]
	public partial struct InventoryItem
	{
		public ItemDefinition item;
		public float quantity;

		public static InventoryItem Null = new InventoryItem { item = null, quantity = 0 };

		public static bool operator ==(InventoryItem c1, InventoryItem c2) 
		{
			return c1.item == c2.item;
		}

		public static bool operator !=(InventoryItem c1, InventoryItem c2) 
		{
			return c1.item != c2.item;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			if (!(obj is InventoryItem))
				return false;
			InventoryItem i = (InventoryItem)obj;
			return i.item == item;
		}

		public override int GetHashCode ()
		{
			return item.GetHashCode ();
		}

	}


		
}