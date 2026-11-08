using UnityEditor;
using UnityEngine;

namespace FiveNightAnomaly.EditorTools
{
    /// <summary>
    /// Import settings by folder, applied the first time a file is imported (later changes in the Inspector stay).
    /// Sprites (PNG under Assets/_Game/Sprites/&lt;Type&gt;/): Sprite, Bilinear, no compression, no mipmaps;
    /// PPU 128 with a bottom-centre pivot for standing art (Characters, Objects, Buildings), centre pivot for Tiles,
    /// FX and Backgrounds (Backgrounds also get a Full Rect mesh so they can be drawn Tiled, Wrap Mode U = Repeat so
    /// a strip can scroll by its UVs, and a 4096 px size limit for the 3840 px wide menu strips), UI at PPU 100 with a
    /// centre pivot except UI/World (bars drawn in the world over enemies), which stays at PPU 128 like the world art.
    /// Sound effects (WAV under Assets/_Game/Audio/SFX/): mono, decompressed on load, PCM.
    /// </summary>
    public sealed class AssetImportRules : AssetPostprocessor
    {
        public const string SpritesRoot = "Assets/_Game/Sprites/";
        public const string SoundEffectsRoot = "Assets/_Game/Audio/SFX/";
        public const float WorldPixelsPerUnit = 128f;
        public const float UiPixelsPerUnit = 100f;
        public const int BackgroundMaxSize = 4096;

        private void OnPreprocessTexture()
        {
            if (assetImporter.importSettingsMissing && assetImporter is TextureImporter importer)
                ApplySprite(importer);
        }

        private void OnPreprocessAudio()
        {
            if (assetImporter.importSettingsMissing && assetImporter is AudioImporter importer)
                ApplySoundEffect(importer);
        }

        /// <summary>Writes the sprite rules into the importer. Returns true when a setting changed (reimport needed).</summary>
        public static bool ApplySprite(TextureImporter importer)
        {
            string path = importer.assetPath;
            if (!path.StartsWith(SpritesRoot) || !path.EndsWith(".png"))
                return false;
            string[] folders = path.Substring(SpritesRoot.Length).Split('/');
            string type = folders[0];
            bool background = type == "Backgrounds";
            bool ui = type == "UI";
            bool screenUi = ui && !(folders.Length > 2 && folders[1] == "World");
            var alignment = background || ui || type == "Tiles" || type == "FX" ? SpriteAlignment.Center : SpriteAlignment.BottomCenter;
            var meshType = background ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
            var wrapU = background ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            float pixelsPerUnit = screenUi ? UiPixelsPerUnit : WorldPixelsPerUnit;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            bool changed = settings.textureType != TextureImporterType.Sprite
                || settings.spriteMode != (int)SpriteImportMode.Single
                || !Mathf.Approximately(settings.spritePixelsPerUnit, pixelsPerUnit)
                || settings.spriteAlignment != (int)alignment
                || settings.spriteMeshType != meshType
                || settings.filterMode != FilterMode.Bilinear
                || settings.mipmapEnabled
                || !settings.alphaIsTransparency
                || settings.wrapModeU != wrapU
                || settings.wrapModeV != TextureWrapMode.Clamp
                || (background && importer.maxTextureSize < BackgroundMaxSize)
                || importer.textureCompression != TextureImporterCompression.Uncompressed;
            if (!changed)
                return false;

            settings.textureType = TextureImporterType.Sprite;
            settings.spriteMode = (int)SpriteImportMode.Single;
            settings.spritePixelsPerUnit = pixelsPerUnit;
            settings.spriteAlignment = (int)alignment;
            settings.spriteMeshType = meshType;
            settings.filterMode = FilterMode.Bilinear;
            settings.mipmapEnabled = false;
            settings.alphaIsTransparency = true;
            settings.wrapMode = TextureWrapMode.Clamp;
            settings.wrapModeU = wrapU;
            importer.SetTextureSettings(settings);
            if (background && importer.maxTextureSize < BackgroundMaxSize)
                importer.maxTextureSize = BackgroundMaxSize;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            return true;
        }

        /// <summary>Writes the sound effect rules into the importer. Returns true when a setting changed.</summary>
        public static bool ApplySoundEffect(AudioImporter importer)
        {
            if (!importer.assetPath.StartsWith(SoundEffectsRoot))
                return false;
            AudioImporterSampleSettings sample = importer.defaultSampleSettings;
            bool changed = !importer.forceToMono
                || sample.loadType != AudioClipLoadType.DecompressOnLoad
                || sample.compressionFormat != AudioCompressionFormat.PCM;
            if (!changed)
                return false;
            importer.forceToMono = true;
            sample.loadType = AudioClipLoadType.DecompressOnLoad;
            sample.compressionFormat = AudioCompressionFormat.PCM;
            importer.defaultSampleSettings = sample;
            return true;
        }
    }
}
