#if UNITY_EDITOR
using UnityEditor;

namespace Highfly.CI
{
    public sealed class HighflyCharacterCompareImporterV022 : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.Contains("Assets/Resources/HIGHFLY/CharacterCompare/KayKitKnight.fbx"))
                return;

            ModelImporter importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
        }
    }
}
#endif
