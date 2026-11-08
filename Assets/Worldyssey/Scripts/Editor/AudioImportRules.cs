using UnityEditor;
using UnityEngine;

namespace Worldyssey.EditorTools
{
    /// <summary>
    /// 音频导入规则：Audio/SFX 下的短音效解压到内存（播放零延迟）；Audio/Ambience 和 Audio/Music 下的长音频边播边读（不占大块内存）。
    /// 新放进来的文件自动套用；已有文件用菜单一次性重设。之后手动改过的导入设置不会被覆盖。
    /// </summary>
    public class AudioImportRules : AssetPostprocessor
    {
        private const string MenuPath = "Worldyssey/Setup/Apply Audio Import Settings";
        private const string AudioFolder = "Assets/Worldyssey/Audio";
        private const string SfxFolder = AudioFolder + "/SFX/";
        private const string AmbienceFolder = AudioFolder + "/Ambience/";
        private const string MusicFolder = AudioFolder + "/Music/";
        private const float VorbisQuality = 0.7f;

        private void OnPreprocessAudio()
        {
            // 只在第一次导入时套用，尊重之后在 Inspector 里的手动调整
            if (assetImporter.importSettingsMissing)
            {
                Apply((AudioImporter)assetImporter);
            }
        }

        [MenuItem(MenuPath)]
        public static void ApplyToAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { AudioFolder });
            int changed = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (AssetImporter.GetAtPath(path) is AudioImporter importer && Apply(importer))
                {
                    importer.SaveAndReimport();
                    changed++;
                }
            }

            Debug.Log($"[{nameof(AudioImportRules)}] 已按规则重设 {changed} 个音频的导入设置");
        }

        /// <summary>按所在文件夹设置导入方式，不在规则文件夹里的返回 false。</summary>
        private static bool Apply(AudioImporter importer)
        {
            string path = importer.assetPath;
            bool isSfx = path.StartsWith(SfxFolder);
            bool isLong = path.StartsWith(AmbienceFolder) || path.StartsWith(MusicFolder);
            if (!isSfx && !isLong)
            {
                return false;
            }

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = isSfx ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = VorbisQuality;
            importer.defaultSampleSettings = settings;

            // 长音频在后台加载，进入关卡时不卡一下
            importer.loadInBackground = isLong;
            return true;
        }
    }
}
