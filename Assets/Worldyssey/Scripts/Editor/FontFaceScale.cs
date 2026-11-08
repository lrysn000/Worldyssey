using TMPro;
using UnityEditor;
using UnityEngine;
using Worldyssey.Data;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 把 <see cref="UiSkin.SdfFontScale"/> 写进标题、正文 SDF 字体资源的 Face Info Scale：用这个字体的所有文字一起放大，
    /// 不改每个文字的字号，重复运行结果不变。点阵像素字体不处理（它们靠字号取整对齐像素）。
    /// </summary>
    public static class FontFaceScale
    {
        private const string MenuPath = "Worldyssey/UI/Apply SDF Font Scale";
        private const string ScaleProperty = "m_FaceInfo.m_Scale";

        [MenuItem(MenuPath)]
        public static void ApplyFromMenu()
        {
            UiSkin skin = PixelUiSkinDefaults.GetOrCreateSkin();
            if (skin == null)
            {
                return;
            }

            int count = Apply(skin.TitleFont, skin.SdfFontScale);
            if (skin.BodyFont != skin.TitleFont)
            {
                count += Apply(skin.BodyFont, skin.SdfFontScale);
            }

            Debug.Log($"[{nameof(FontFaceScale)}] 已把 {count} 个 SDF 字体的缩放设为 {skin.SdfFontScale}", skin);
        }

        /// <summary>设置一个 SDF 字体的整体缩放，点阵字体或空引用返回 0。</summary>
        internal static int Apply(TMP_FontAsset font, float scale)
        {
            if (font == null || UiStyleKit.IsPixelFont(font))
            {
                return 0;
            }

            var serialized = new SerializedObject(font);
            SerializedProperty property = serialized.FindProperty(ScaleProperty);
            if (property == null)
            {
                Debug.LogError($"[{nameof(FontFaceScale)}] 在 {font.name} 上找不到 {ScaleProperty}，TextMeshPro 版本可能不同，请在字体资源的 Face Info 里手动设置 Scale", font);
                return 0;
            }

            property.floatValue = scale;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssets();

            // 通知所有文字重新排版，场景里立即看到变化
            TMPro_EventManager.ON_FONT_PROPERTY_CHANGED(true, font);
            return 1;
        }
    }
}
