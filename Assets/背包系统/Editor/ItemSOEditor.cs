using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ItemSO))]
[CanEditMultipleObjects]
public class ItemSOEditor : UnityEditor.Editor
{
    private static readonly Vector2 PreviewBaseSizeDelta = new Vector2(80f, 80f);

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (targets.Length > 1)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("背包内图标预览", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("多选时无法预览。", MessageType.None);
            return;
        }

        var item = (ItemSO)target;
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("背包内图标预览", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            $"假定格子 Icon 基准为 {PreviewBaseSizeDelta.x:0.}×{PreviewBaseSizeDelta.y:0.}（仅预览比例，可与预制体不同）。",
            MessageType.None);

        Vector2 d = item.iconSlotSizeDelta;
        Vector2 eff = new Vector2(
            d.x > 0.001f ? d.x : PreviewBaseSizeDelta.x,
            d.y > 0.001f ? d.y : PreviewBaseSizeDelta.y);

        EditorGUILayout.LabelField("有效 sizeDelta", $"{eff.x:0.##} × {eff.y:0.##}");

        if (item.icon == null)
        {
            EditorGUILayout.LabelField("未设置 icon。");
            return;
        }

        const float maxEdge = 168f;
        float scale = Mathf.Min(maxEdge / eff.x, maxEdge / eff.y, 1f);
        Vector2 drawPx = eff * scale;
        Rect rect = GUILayoutUtility.GetRect(drawPx.x, drawPx.y, GUILayout.ExpandWidth(false));
        DrawSprite(rect, item.icon);
    }

    private static void DrawSprite(Rect rect, Sprite sprite)
    {
        Texture2D tex = sprite.texture;
        if (tex == null)
            return;

        Rect tr = sprite.textureRect;
        float tw = tex.width;
        float th = tex.height;
        Rect uv = new Rect(tr.x / tw, tr.y / th, tr.width / tw, tr.height / th);
        GUI.DrawTextureWithTexCoords(rect, tex, uv, true);
    }
}
