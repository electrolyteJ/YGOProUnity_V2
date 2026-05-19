//----------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2015 Tasharen Entertainment
//----------------------------------------------

using UnityEditor;
using UnityEngine;

/// <summary>
/// Small compatibility layer for legacy NGUI editor code running on modern Unity prefab APIs.
/// </summary>
public static class NGUIPrefabUtilityCompat
{
	static public bool IsPrefabAsset (Object obj)
	{
		return obj != null && PrefabUtility.IsPartOfPrefabAsset(obj);
	}

	static public bool IsPrefabInstance (GameObject go)
	{
		return go != null && PrefabUtility.IsPartOfPrefabInstance(go);
	}

	static public GameObject GetPrefabAsset (GameObject go)
	{
		if (go == null) return null;

		GameObject asset = PrefabUtility.GetCorrespondingObjectFromSource(go);
		if (asset != null) return asset;

		if (PrefabUtility.IsPartOfPrefabAsset(go))
		{
			string path = AssetDatabase.GetAssetPath(go);
			if (!string.IsNullOrEmpty(path))
				return AssetDatabase.LoadAssetAtPath<GameObject>(path);
		}
		return null;
	}

	static public GameObject SaveAsPrefabAsset (GameObject go, string path)
	{
		return string.IsNullOrEmpty(path) ? null : PrefabUtility.SaveAsPrefabAsset(go, path);
	}

	static public GameObject GetOutermostPrefabInstanceRoot (GameObject go)
	{
		if (go == null) return null;

		GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);
		if (root != null) return root;

		if (PrefabUtility.IsPartOfPrefabAsset(go))
		{
			string path = AssetDatabase.GetAssetPath(go);
			if (!string.IsNullOrEmpty(path))
				return AssetDatabase.LoadAssetAtPath<GameObject>(path);
		}
		return null;
	}
}
