#if UNITY_EDITOR
using UnityEditor;

namespace Highfly.Clean.Editor
{
    public sealed class HighflyCleanModelImporter : AssetPostprocessor
    {
        private void OnPreprocessModel()
        {
            if (!assetPath.Contains("Assets/Resources/HIGHFLY/Run0/"))
                return;

            ModelImporter importer =
                (ModelImporter)assetImporter;

            if (assetPath.EndsWith("KayKitSword1H.fbx"))
            {
                importer.importAnimation = false;
                importer.animationType =
                    ModelImporterAnimationType.None;
                return;
            }

            importer.importAnimation = true;
            importer.animationType =
                ModelImporterAnimationType.Human;

            importer.avatarSetup =
                ModelImporterAvatarSetup.CreateFromThisModel;

            importer.animationCompression =
                ModelImporterAnimationCompression.Optimal;

            importer.resampleCurves = true;

            if (!assetPath.EndsWith("UAL2_Standard.fbx"))
                return;

            ModelImporterClipAnimation[] clips =
                importer.defaultClipAnimations;

            for (int i = 0; i < clips.Length; i++)
            {
                ModelImporterClipAnimation clip = clips[i];

                string n =
                    clip.name.ToLowerInvariant();

                // HIGHFLY movement is driven by CharacterController.
                // UAL2 is presentation only, so all root motion must be
                // baked into the humanoid pose instead of rotating/translating
                // the KayKit root.
                clip.lockRootRotation = true;
                clip.keepOriginalOrientation = false;
                clip.rotationOffset = 0f;

                clip.lockRootHeightY = true;
                clip.keepOriginalPositionY = false;
                clip.heightFromFeet = true;
                clip.heightOffset = 0f;

                clip.lockRootPositionXZ = true;
                clip.keepOriginalPositionXZ = false;

                if (n.Contains("idle") ||
                    n.Contains("walk") ||
                    n.Contains("run"))
                {
                    clip.loopTime = true;
                    clip.loopPose = true;
                }

                clips[i] = clip;
            }

            importer.clipAnimations = clips;
        }
    }
}
#endif
