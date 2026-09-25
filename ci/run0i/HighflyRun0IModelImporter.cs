#if UNITY_EDITOR
using UnityEditor;

namespace Highfly.Run0I.Editor
{
    public sealed class HighflyRun0IModelImporter : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (assetPath.Contains("Assets/Resources/HIGHFLY/Run0H/KayKitKnight.fbx") ||
                assetPath.Contains("Assets/Resources/HIGHFLY/Run0H/KayKitRogue.fbx"))
            {
                ModelImporter importer=(ModelImporter)assetImporter;
                importer.importAnimation=true;
                importer.animationType=ModelImporterAnimationType.Human;
                importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
                importer.animationCompression=ModelImporterAnimationCompression.Optimal;
                importer.resampleCurves=true;
                return;
            }

            if (assetPath.Contains("Assets/HIGHFLY/Run0I/UAL2_Standard.fbx"))
            {
                ModelImporter importer=(ModelImporter)assetImporter;
                importer.importAnimation=true;
                importer.animationType=ModelImporterAnimationType.Human;
                importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
                importer.animationCompression=ModelImporterAnimationCompression.Optimal;
                importer.resampleCurves=true;
            }
        }
    }
}
#endif
