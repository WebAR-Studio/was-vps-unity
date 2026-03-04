#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NavigationPointsCatalog))]
public class NavigationPointsCatalogEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Point"))
            {
                var catalog = (NavigationPointsCatalog)target;
                Undo.RecordObject(catalog, "Add navigation point");
                catalog.AddEmptyPoint();
                EditorUtility.SetDirty(catalog);
            }

            if (GUILayout.Button("Sync IDs"))
            {
                var catalog = (NavigationPointsCatalog)target;
                SyncIds(catalog);
            }
        }
    }

    private static void SyncIds(NavigationPointsCatalog catalog)
    {
        if (catalog == null || catalog.Points == null)
        {
            return;
        }

        Undo.RecordObject(catalog, "Sync navigation point IDs");
        for (var i = 0; i < catalog.Points.Count; i++)
        {
            var point = catalog.Points[i];
            if (point == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(point.Id))
            {
                point.Id = $"point_{i + 1}";
            }
        }

        EditorUtility.SetDirty(catalog);
    }
}
#endif
