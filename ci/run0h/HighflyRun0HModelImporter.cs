#if UNITY_EDITOR
using UnityEditor;

namespace Highfly.Run0H.Editor
{
    public sealed class HighflyRun0HModelImporter : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.Contains("Assets/Resources/HIGHFLY/Run0H/"))
                return;

            ModelImporter importer = (ModelImporter)assetImporter;

            if (assetPath.EndsWith("KayKitSword1H.fbx") ||
                assetPath.EndsWith("KayKitDagger.fbx"))
            {
                importer.importAnimation = false;
                importer.animationType = ModelImporterAnimationType.None;
                return;
            }

            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.resampleCurves = true;
        }
    }
}
#endif
