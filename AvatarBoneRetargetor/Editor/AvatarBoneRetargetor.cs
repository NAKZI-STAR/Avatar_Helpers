#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

internal sealed class AvatarBoneRetargetor : EditorWindow
{
    private class BoneMatch
    {
        public Transform Source;
        public Transform Target;
        public bool Matched;

        public BoneMatch(Transform source, Transform target, bool matched)
        {
            Source = source;
            Target = target;
            Matched = matched;
        }
    }

    private Transform _avatarRoot;
    private Transform _clothRoot;

    private Vector2 _scroll;

    private List<BoneMatch> _matches = new();
    private Dictionary<string, Transform> _targetBoneMap;

    private bool _ignorePrefix = true;
    private bool _ignoreSuffix = true;

    private string _newPrefix = "";
    private string _newSuffix = "";

    private bool _showPrefixSettings = true;
    private bool _showSuffixSettings = true;

    private const float COL_SOURCE_WIDTH = 260f;
    private const float COL_ARROW_WIDTH = 20f;
    private const float COL_ICON_WIDTH = 20f;

    private List<string> _prefixes = new List<string>();
    private List<string> _suffixes = new List<string>();

    private GUIContent _warnIcon;

    [MenuItem("Nakzi Avatar Script/Avatar Bone Retargetor")]
    private static void open()
    {
        GetWindow<AvatarBoneRetargetor>("Bone Matcher");
    }

    private void OnEnable()
    {
        _warnIcon = EditorGUIUtility.IconContent("console.warnicon");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Avatar Bone Retargetor", EditorStyles.boldLabel);

        _avatarRoot = (Transform)EditorGUILayout.ObjectField(
            new GUIContent("Avatar Root (Target)"),
            _avatarRoot,
            typeof(Transform),
            true);

        _clothRoot = (Transform)EditorGUILayout.ObjectField(
            new GUIContent("Cloth Root (Source)"),
            _clothRoot,
            typeof(Transform),
            true);

        _ignorePrefix = EditorGUILayout.Toggle("Ignore Prefix", _ignorePrefix);
        _ignoreSuffix = EditorGUILayout.Toggle("Ignore Suffix", _ignoreSuffix);

        if (_matches.Count > 0)
        {
            int matched = _matches.Count(x => x.Matched);
            EditorGUILayout.LabelField($"Matched: {matched}/{_matches.Count}");
        }

        EditorGUILayout.Space();
        drawPrefixSuffixUI();
        EditorGUILayout.Space();

        if (GUILayout.Button("Auto Match"))
        {
            autoMatch();
        }

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            GUILayout.Space(2);

            GUILayout.Label("Cloth Bone", EditorStyles.boldLabel, GUILayout.Width(COL_SOURCE_WIDTH));

            GUILayout.Space(COL_ARROW_WIDTH + COL_ICON_WIDTH);

            GUILayout.Label("Avatar Bone", EditorStyles.boldLabel);
        }

        using (var scrollScope = new EditorGUILayout.ScrollViewScope(_scroll))
        {
            _scroll = scrollScope.scrollPosition;

            foreach (var m in _matches)
            {
                using (var scope = new EditorGUILayout.HorizontalScope())
                {
                    bool pro = EditorGUIUtility.isProSkin;

                    Color rowMatched = pro
                        ? new Color(0.2f, 0.35f, 0.25f, 0.25f)   // dark skin
                        : new Color(0.6f, 0.85f, 0.7f, 0.25f);   // light skin

                    Color rowUnmatched = pro
                        ? new Color(0.4f, 0.25f, 0.25f, 0.25f)
                        : new Color(1f, 0.6f, 0.6f, 0.25f);

                    Color bg = m.Matched ? rowMatched : rowUnmatched;
                    EditorGUI.DrawRect(scope.rect, bg);

                    GUILayout.Space(2);

                    Color prev = GUI.backgroundColor;
                    GUI.backgroundColor = new Color(0.7f, 0.85f, 1f);

                    EditorGUILayout.ObjectField(m.Source, typeof(Transform), true, GUILayout.Width(COL_SOURCE_WIDTH));

                    GUI.backgroundColor = prev;

                    GUI.backgroundColor = new Color(0.7f, 1f, 0.7f);

                    GUILayout.Label("→", GUILayout.Width(COL_ARROW_WIDTH));

                    if (m.Matched)
                    {
                        GUILayout.Space(COL_ICON_WIDTH);
                    }
                    else
                    {
                        GUILayout.Label(_warnIcon, GUILayout.Width(COL_ICON_WIDTH), GUILayout.Height(18));
                    }

                    m.Target = (Transform)EditorGUILayout.ObjectField(m.Target, typeof(Transform), true);
                    GUI.backgroundColor = prev;
                }
            }
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Merge Armature"))
        {
            mergeArmature();
            autoMatch();
        }
    }

    private void drawPrefixSuffixUI()
    {
        EditorGUILayout.LabelField("Name Ignore Settings", EditorStyles.boldLabel);

        // PREFIX
        _showPrefixSettings = EditorGUILayout.Foldout(_showPrefixSettings, "Ignore Prefix List");

        if (_showPrefixSettings)
        {
            EditorGUI.indentLevel++;

            for (int i = 0; i < _prefixes.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                _prefixes[i] = EditorGUILayout.TextField(_prefixes[i]);

                if (GUILayout.Button("X", GUILayout.Width(22)))
                {
                    _prefixes.RemoveAt(i);
                    GUI.FocusControl(null);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();

            _newPrefix = EditorGUILayout.TextField(_newPrefix);

            if (GUILayout.Button("Add", GUILayout.Width(60)))
            {
                if (!string.IsNullOrEmpty(_newPrefix))
                {
                    _prefixes.Add(_newPrefix);
                    _newPrefix = "";
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        // SUFFIX
        _showSuffixSettings = EditorGUILayout.Foldout(_showSuffixSettings, "Ignore Suffix List");

        if (_showSuffixSettings)
        {
            EditorGUI.indentLevel++;

            for (int i = 0; i < _suffixes.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                _suffixes[i] = EditorGUILayout.TextField(_suffixes[i]);

                if (GUILayout.Button("X", GUILayout.Width(22)))
                {
                    _suffixes.RemoveAt(i);
                    GUI.FocusControl(null);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();

            _newSuffix = EditorGUILayout.TextField(_newSuffix);

            if (GUILayout.Button("Add", GUILayout.Width(60)))
            {
                if (!string.IsNullOrEmpty(_newSuffix))
                {
                    _suffixes.Add(_newSuffix);
                    _newSuffix = "";
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }
    }

    private void autoMatch()
    {
        _matches.Clear();

        if (_avatarRoot == null || _clothRoot == null)
            return;

        buildTargetMap(_avatarRoot);

        foreach (Transform t in _clothRoot.GetComponentsInChildren<Transform>(true))
        {
            string key = normalizeName(t.name);

            BoneMatch bm = _targetBoneMap.TryGetValue(key, out Transform found) ?
                new BoneMatch(t, found, true) :
                new BoneMatch(t, null, false);

            _matches.Add(bm);
        }
    }


    private string normalizeName(string name)
    {
        string n = name.ToLower();

        if (_ignorePrefix)
        {
            foreach (var p in _prefixes)
            {
                string pl = p.ToLower();
                if (n.StartsWith(pl))
                {
                    n = n[pl.Length..];
                    break;
                }
            }
        }

        if (_ignoreSuffix)
        {
            foreach (var s in _suffixes)
            {
                string sl = s.ToLower();
                if (n.EndsWith(sl))
                {
                    n = n[..^sl.Length];
                    break;
                }
            }
        }

        return n;
    }

    private void mergeArmature()
    {
        if (_matches == null || _matches.Count == 0)
            return;

        ensureUnpacked(_avatarRoot.gameObject);
        ensureUnpacked(_clothRoot.gameObject);

        Undo.RegisterFullObjectHierarchyUndo(_clothRoot.gameObject, "Merge Armature");

        foreach (var m in _matches)
        {
            if (!m.Matched || m.Source == null || m.Target == null)
                continue;

            Transform clothBone = m.Source;
            Transform avatarBone = m.Target;

            // 부모 맞추기 (선택 가능 옵션)
            Undo.SetTransformParent(clothBone, avatarBone, true, "Merge Armature");
            EditorUtility.SetDirty(clothBone);
        }

        Debug.Log("Merge Armature Complete");
    }

    private void ensureUnpacked(GameObject go)
    {
        if (!PrefabUtility.IsPartOfPrefabInstance(go))
            return;

        GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(go);

        if (root == null)
            return;

        PrefabUtility.UnpackPrefabInstance(
            root,
            PrefabUnpackMode.Completely,
            InteractionMode.UserAction);
    }

    private void buildTargetMap(Transform root)
    {
        _targetBoneMap = new Dictionary<string, Transform>();

        foreach (Transform tBone in root.GetComponentsInChildren<Transform>(true))
        {
            string key = normalizeName(tBone.name);

            if (!_targetBoneMap.ContainsKey(key))
                _targetBoneMap.Add(key, tBone);
        }
    }
}
#endif
