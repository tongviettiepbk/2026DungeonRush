using UnityEditor;
using UnityEngine;

// Dùng trong các prefab builder: node "TutorialUI" của bản rip mất component TutorialUI gốc
// (còn GameObject + con đầu là hình tay chỉ) → gắn lại như prefab gốc: node bật, tay thu về scale 0, _child = con đầu.
public static class TutorialUIBuilderUtil
{
    public static TutorialUI Setup(Transform node)
    {
        node.gameObject.SetActive(true);
        TutorialUI tutorial = node.GetComponent<TutorialUI>();
        if (tutorial == null)
            tutorial = node.gameObject.AddComponent<TutorialUI>();

        Transform hand = node.GetChild(0);
        hand.localScale = Vector3.zero;
        SerializedObject so = new SerializedObject(tutorial);
        so.FindProperty("_child").objectReferenceValue = hand;
        so.ApplyModifiedPropertiesWithoutUndo();
        return tutorial;
    }
}
