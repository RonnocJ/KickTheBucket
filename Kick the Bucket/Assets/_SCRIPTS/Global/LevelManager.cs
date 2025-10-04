using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.SceneManagement;
using Unity.VisualScripting;

[Serializable]
public struct LevelObj
{
    public GameObject obj;
    public bool active;
    public Vector3 position;
    public Quaternion rotation;
    public Vector3 scale;

    public LevelObj(GameObject newObj)
    {
        obj = newObj;
        active = obj.activeInHierarchy;
        position = obj.transform.position;
        rotation = obj.transform.rotation;
        scale = obj.transform.localScale;
    }
}
[Serializable]
public class Level
{
    public string name;
    public List<LevelObj> objects = new List<LevelObj>();
}

public class LevelManager : Singleton<LevelManager>
{
    [Header("Editor / Runtime")]
    public int LoadedLevelIndex = 0;
    public Level LoadedLevel;
    public List<Level> Levels = new List<Level>();
    
    public void LoadLevelFrom(int index)
    {
        if (index < 0 || index >= Levels.Count) return;
        var level = Levels[index];
        if (level == null || level.objects == null) return;

        foreach (var l in level.objects)
        {
            if (l.obj == null) continue;
            l.obj.SetActive(l.active);
            l.obj.transform.position = l.position;
            l.obj.transform.rotation = l.rotation;
            l.obj.transform.localScale = l.scale;
        }

        LoadedLevelIndex = index;
        LoadedLevel = level;
    }

    public void LoadCurrentLevel()
    {
        LoadLevelFrom(LoadedLevelIndex);
    }

    public void SaveLevelTo(int index)
    {
        var currentObjects = new List<LevelObj>();
        foreach (Transform child in transform)
        {
            foreach (Transform grandchild in child)
            {
                currentObjects.Add(new LevelObj(grandchild.gameObject));
            }
        }

        while (Levels.Count <= index)
        {
            Levels.Add(new Level() { name = $"Level {Levels.Count}" });
        }

        Levels[index].objects = currentObjects;
        Levels[index].name = $"Level {index}";

        LoadedLevelIndex = index;
        LoadedLevel = Levels[index];
    }
}


#if UNITY_EDITOR


[CustomEditor(typeof(LevelManager))]
public class LevelManagerEditor : Editor
{
    LevelManager manager;
    ReorderableList list;

    void OnEnable()
    {
        manager = (LevelManager)target;
        SetupList();
    }

    void SetupList()
    {
        // create ReorderableList for the currently selected level's objects
        if (manager == null) return;

        // Make sure there is at least one level to edit
        if (manager.Levels == null) manager.Levels = new System.Collections.Generic.List<Level>();
        if (manager.LoadedLevelIndex >= manager.Levels.Count)
            manager.LoadedLevelIndex = Mathf.Clamp(manager.LoadedLevelIndex, 0, Mathf.Max(0, manager.Levels.Count - 1));

        // We don't bind the list directly to a serialized property for brevity, we will recreate it per selected level
        RecreateList();
    }

    void RecreateList()
    {
        // dispose previous list reference if present (not strictly necessary)
        list = null;

        if (manager == null || manager.Levels == null || manager.Levels.Count == 0)
            return;

        var levelIndex = Mathf.Clamp(manager.LoadedLevelIndex, 0, manager.Levels.Count - 1);
        var level = manager.Levels[levelIndex];
        if (level == null) return;

        // Create a reorderable list backed by the serialized property for safety
        var so = new SerializedObject(manager);
        var levelsProp = so.FindProperty("Levels");
        if (levelsProp == null) return;
        var levelProp = levelsProp.GetArrayElementAtIndex(levelIndex);
        if (levelProp == null) return;
        var objectsProp = levelProp.FindPropertyRelative("objects");
        if (objectsProp == null) return;

        list = new ReorderableList(so, objectsProp, true, true, true, true);

        list.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, $"Objects in Level {levelIndex} (Count: {objectsProp.arraySize})");
        };

        list.drawElementCallback = (rect, index, isActive, isFocused) =>
        {
            var elem = objectsProp.GetArrayElementAtIndex(index);
            rect.y += 2;
            var r1 = new Rect(rect.x, rect.y, rect.width * 0.4f, EditorGUIUtility.singleLineHeight);
            var r2 = new Rect(rect.x + rect.width * 0.4f + 4, rect.y, rect.width * 0.6f - 4, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(r1, elem.FindPropertyRelative("obj"), GUIContent.none);

            // show a compact position + active label
            var pos = elem.FindPropertyRelative("position");
            var active = elem.FindPropertyRelative("active");
            EditorGUI.LabelField(r2, $"pos:({pos.vector3Value.x:0.00},{pos.vector3Value.y:0.00}) active:{active.boolValue}");
        };

        list.onAddCallback = (r) =>
        {
            // Add a default element (manual editing in inspector required)
            objectsProp.arraySize++;
            objectsProp.GetArrayElementAtIndex(objectsProp.arraySize - 1).FindPropertyRelative("obj").objectReferenceValue = null;
            so.ApplyModifiedProperties();
        };

        list.onRemoveCallback = (r) =>
        {
            objectsProp.DeleteArrayElementAtIndex(r.index);
            so.ApplyModifiedProperties();
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw top controls: index slider/dropdown + Save/Load/Add/Remove
        EditorGUILayout.LabelField("Level Manager (Editor Tools)", EditorStyles.boldLabel);

        // Index field + dropdown
        EditorGUILayout.BeginHorizontal();
        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUILayout.IntField("Level Index", manager.LoadedLevelIndex);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(manager, "Change Level Index");
            manager.LoadedLevelIndex = Mathf.Clamp(newIndex, 0, Mathf.Max(0, manager.Levels.Count - 1));
            EditorUtility.SetDirty(manager);
            RecreateList();
        }

        if (GUILayout.Button("◀", GUILayout.Width(24)))
        {
            Undo.RecordObject(manager, "Dec Level Index");
            manager.LoadedLevelIndex = Mathf.Max(0, manager.LoadedLevelIndex - 1);
            EditorUtility.SetDirty(manager);
            RecreateList();
        }
        if (GUILayout.Button("▶", GUILayout.Width(24)))
        {
            Undo.RecordObject(manager, "Inc Level Index");
            manager.LoadedLevelIndex = Mathf.Min(Mathf.Max(0, manager.Levels.Count - 1), manager.LoadedLevelIndex + 1);
            EditorUtility.SetDirty(manager);
            RecreateList();
        }
        EditorGUILayout.EndHorizontal();

        // Quick buttons
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save To Index"))
        {
            Undo.RecordObject(manager, "Save Level To Index");
            manager.SaveLevelTo(manager.LoadedLevelIndex);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
            RecreateList();
        }
        if (GUILayout.Button("Load From Index"))
        {
            Undo.RecordObject(manager, "Load Level From Index");
            manager.LoadLevelFrom(manager.LoadedLevelIndex);
            EditorUtility.SetDirty(manager);
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }
        EditorGUILayout.EndHorizontal();

        // Show the ReorderableList for the selected level (editable)
        if (list != null)
        {
            list.DoLayoutList();
        }
        else
        {
            EditorGUILayout.HelpBox("No levels yet. Use Add Level, then Save To Index to capture objects.", MessageType.Info);
        }

        // Draw the rest of the default inspector (shows fields like LoadedLevel if desired)
        EditorGUILayout.Space();
        DrawDefaultInspector();

        serializedObject.ApplyModifiedProperties();
    }

    // Optional: menu hotkeys for quick Save/Load when LevelManager is selected
    [MenuItem("Tools/Levels/Save Selected Level %#s")] // Ctrl+Shift+S (Win) or Cmd+Shift+S (Mac)
    static void MenuSaveSelected()
    {
        if (Selection.activeGameObject == null) return;
        var lm = Selection.activeGameObject.GetComponent<LevelManager>();
        if (lm != null)
        {
            Undo.RecordObject(lm, "Save Level (Menu)");
            lm.SaveLevelTo(lm.LoadedLevelIndex);
            EditorUtility.SetDirty(lm);
            EditorSceneManager.MarkSceneDirty(lm.gameObject.scene);
            Debug.Log($"Saved level {lm.LoadedLevelIndex}");
        }
    }

    [MenuItem("Tools/Levels/Load Selected Level %#l")] // Ctrl+Shift+L
    static void MenuLoadSelected()
    {
        if (Selection.activeGameObject == null) return;
        var lm = Selection.activeGameObject.GetComponent<LevelManager>();
        if (lm != null)
        {
            Undo.RecordObject(lm, "Load Level (Menu)");
            lm.LoadLevelFrom(lm.LoadedLevelIndex);
            EditorUtility.SetDirty(lm);
            EditorSceneManager.MarkSceneDirty(lm.gameObject.scene);
            Debug.Log($"Loaded level {lm.LoadedLevelIndex}");
        }
    }
}
#endif