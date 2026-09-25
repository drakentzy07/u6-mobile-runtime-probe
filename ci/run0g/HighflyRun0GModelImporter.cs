#if UNITY_EDITOR
using UnityEditor;

namespace Highfly.Run0G.Editor
{
    public sealed class HighflyRun0GModelImporter : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.Contains("Assets/Resources/HIGHFLY/Run0G/KayKitKnight.fbx"))
                return;

            ModelImporter importer = (ModelImporter)assetImporter;
            importer.importAnimation = true;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.resampleCurves = true;
        }
    }
}
#endif
