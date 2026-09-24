#if UNITY_EDITOR
using UnityEditor;

namespace Highfly.Clean.Editor
{
    public sealed class HighflyCleanModelImporter : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.Contains("Assets/Resources/HIGHFLY/Run0/")) return;

            ModelImporter importer = (ModelImporter)assetImporter;

            if (assetPath.EndsWith("KayKitSword1H.fbx"))
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

            if (assetPath.EndsWith("UAL2_Standard.fbx"))
            {
                ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
                for (int i = 0; i < clips.Length; i++)
                {
                    string n = clips[i].name.ToLowerInvariant();
                    if (n.Contains("idle") || n.Contains("walk") || n.Contains("run"))
                        clips[i].loopTime = true;
                }
                importer.clipAnimations = clips;
            }
        }
    }
}
#endif
