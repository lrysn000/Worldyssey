using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using Worldyssey.Data;
using Worldyssey.UI;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 把翻译表里用到的全部字符预先烘进界面字体的图集。
    /// 界面字体是动态字体，但单张图集按 90 号字取样只放得下一百个左右的字形，中文一多就满了，缺的字显示成方块。
    /// 这里打开多图集（满了自动加一张），并在编辑器里一次性加入所需字符，保存进字体资源，发布版不依赖运行时生成。
    /// 翻译表加了新字后重新运行即可。
    /// </summary>
    public static class FontCharacterSetup
    {
        private const string MenuPath = "Worldyssey/Content/Add Translation Characters To Font";

        [MenuItem(MenuPath)]
        public static void Setup()
        {
            var skin = GameUiBuilder.FindAsset<UiSkin>(null);
            var table = AssetDatabase.LoadAssetAtPath<TextAsset>(LocalizationSetup.TablePath);
            if (skin == null || skin.BodyFont == null || table == null)
            {
                Debug.LogError($"[{nameof(FontCharacterSetup)}] 找不到 UiSkin 的正文字体或翻译表 {LocalizationSetup.TablePath}");
                return;
            }

            string characters = CollectCharacters(table);
            var fonts = new HashSet<TMP_FontAsset> { skin.BodyFont };
            if (skin.TitleFont != null)
            {
                fonts.Add(skin.TitleFont);
            }

            foreach (TMP_FontAsset font in fonts)
            {
                AddCharacters(font, characters);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>翻译表所有列里出现的字符，再加上全部可打印 ASCII（关卡名、数字、符号）。</summary>
        private static string CollectCharacters(TextAsset table)
        {
            var set = new SortedSet<char>();
            for (char c = ' '; c <= '~'; c++)
            {
                set.Add(c);
            }

            foreach (List<string> row in CsvTable.Parse(table.text))
            {
                foreach (string cell in row)
                {
                    foreach (char c in cell)
                    {
                        if (!char.IsControl(c))
                        {
                            set.Add(c);
                        }
                    }
                }
            }

            var builder = new StringBuilder(set.Count);
            foreach (char c in set)
            {
                builder.Append(c);
            }

            return builder.ToString();
        }

        private static void AddCharacters(TMP_FontAsset font, string characters)
        {
            if (font.atlasPopulationMode != AtlasPopulationMode.Dynamic)
            {
                Debug.LogError($"[{nameof(FontCharacterSetup)}] {font.name} 不是动态字体，无法自动加字；请在 Font Asset Creator 里用翻译表的字符重新生成", font);
                return;
            }

            Undo.RecordObject(font, "Add Translation Characters");
            font.isMultiAtlasTexturesEnabled = true;
            font.TryAddCharacters(characters, out string missing);
            EditorUtility.SetDirty(font);

            int atlasCount = font.atlasTextures != null ? font.atlasTextures.Length : 0;
            if (string.IsNullOrEmpty(missing))
            {
                Debug.Log($"[{nameof(FontCharacterSetup)}] {font.name} 已包含翻译表的全部 {characters.Length} 个字符，共 {atlasCount} 张图集", font);
            }
            else
            {
                Debug.LogWarning($"[{nameof(FontCharacterSetup)}] {font.name} 的源字体里没有这些字符：{missing}", font);
            }
        }
    }
}
