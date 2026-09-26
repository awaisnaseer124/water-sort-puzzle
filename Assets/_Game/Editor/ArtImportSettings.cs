using UnityEditor;

namespace ColorSort.EditorTools
{
    // Everything under Assets/_Game/Art is UI art: tinted at runtime and full of soft gradients,
    // so import it as uncompressed sprites without mipmaps.
    public sealed class ArtImportSettings : AssetPostprocessor
    {
        private const string ArtFolder = "Assets/_Game/Art/";

        private void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(ArtFolder))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = UnityEngine.TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }
    }
}
