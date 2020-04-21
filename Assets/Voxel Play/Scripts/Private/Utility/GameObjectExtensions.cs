using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelPlay
{
				
	public static class GameObjectExtensions
	{

		public static Rect RectTransformToScreenRect (this GameObject uiGameObject)
		{
			RectTransform yourRectTransform = uiGameObject.GetComponent<RectTransform> ();
			Canvas root = yourRectTransform.transform.transform.root.GetComponent<Canvas> ();
			return RectTransformUtility.PixelAdjustRect (yourRectTransform, root);
		}

	}

}