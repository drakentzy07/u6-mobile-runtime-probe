#if UNITY_EDITOR
using UnityEditor;

namespace Highfly.CI
{
    public sealed class HighflyRun0Importer : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.Contains("Assets/Resources/HIGHFLY/Run0/"))
                return;

            ModelImporter importer = (ModelImporter)assetImporter;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;
            importer.resampleCurves = true;
        }
    }
}
#endif
