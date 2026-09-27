#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Highfly.ClaudeBridge.Editor
{
    /// <summary>
    /// Rebuilds ClaudeCraft's five selected bespoke clips directly from the
    /// KayKit FBX donors already proven by frozen SKILL4. No GLB/Assimp bridge.
    /// The timelines mirror ClaudeCraft v0.43.3 pose-sample-and-blend recipes.
    /// </summary>
    public static class HighflyClaudePoseBaker
    {
        private const string KnightPath = "Assets/Resources/HIGHFLY/Run0H/KayKitKnight.fbx";
        private const string RoguePath = "Assets/Resources/HIGHFLY/Run0H/KayKitRogue.fbx";
        private const string HunterPath = "Assets/Resources/HIGHFLY/Run0H/KayKitRogueHooded.fbx";
        private const string TargetDir = "Assets/Resources/HIGHFLY/Run0I/Animations";
        private const float SampleRate = 60f;

        private sealed class BoneState
        {
            public string Name;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;

            public BoneState Clone()
            {
                return new BoneState {
                    Name = Name,
                    Position = Position,
                    Rotation = Rotation,
                    Scale = Scale
                };
            }
        }

        private sealed class Pose
        {
            public readonly Dictionary<string, BoneState> Bones =
                new Dictionary<string, BoneState>(StringComparer.OrdinalIgnoreCase);

            public Pose Clone()
            {
                Pose p = new Pose();
                foreach (var kv in Bones) p.Bones[kv.Key] = kv.Value.Clone();
                return p;
            }

            public string FindPathByBoneName(string boneName)
            {
                foreach (var kv in Bones)
                    if (string.Equals(kv.Value.Name, boneName, StringComparison.OrdinalIgnoreCase))
                        return kv.Key;
                return null;
            }
        }

        private struct KeyPose
        {
            public float Time;
            public Pose Pose;
            public KeyPose(float time, Pose pose) { Time = time; Pose = pose; }
        }

        public static void BakeAll()
        {
            AssetDatabase.Refresh();
            Directory.CreateDirectory(TargetDir);

            AnimationClip[] knight = LoadClips(KnightPath);
            AnimationClip[] rogue = LoadClips(RoguePath);
            GameObject hunterAsset = AssetDatabase.LoadAssetAtPath<GameObject>(HunterPath);
            if (hunterAsset == null) throw new Exception("[CLAUDE BAKER] Missing Hunter FBX: " + HunterPath);

            AnimationClip kIdle = Find(knight, "Idle");
            AnimationClip kChop2H = FindTokens(knight, "2h", "melee", "attack", "chop");
            AnimationClip kJumpIdle = FindTokens(knight, "jump", "idle");

            AnimationClip rIdle = Find(rogue, "Idle");
            AnimationClip rDualChop = FindTokens(rogue, "dual", "melee", "attack", "chop");
            AnimationClip rSlashDiag = FindTokens(rogue, "1h", "melee", "attack", "slice", "diagonal");
            AnimationClip rBlock = Find(rogue, "Block");
            AnimationClip rShoot = FindTokens(rogue, "spellcast", "shoot");

            Debug.Log("[CLAUDE BAKER] DONORS_OK • knight=" +
                kIdle.name + " | " + kChop2H.name + " | " + kJumpIdle.name +
                " • rogue=" + rIdle.name + " | " + rDualChop.name + " | " +
                rSlashDiag.name + " | " + rBlock.name + " | " + rShoot.name);

            GameObject hunter = UnityEngine.Object.Instantiate(hunterAsset);
            hunter.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                // Heroic Leap: exact ClaudeCraft v0.43.3 recipe.
                Pose hIdle = Sample(hunter, kIdle, 0.30f);
                Pose hCoil = Sample(hunter, kChop2H, 0.30f);
                Pose hAir = Sample(hunter, kJumpIdle, 0.50f);
                Pose hSlam = Sample(hunter, kChop2H, 1.00f);
                List<KeyPose> heroic = new List<KeyPose> { new KeyPose(0f, hIdle) };
                AddRamp(heroic, 0f, 0.22f, 4, EaseOutCubic, hIdle, hCoil);
                AddRamp(heroic, 0.22f, 0.50f, 5, EaseOutCubic, hCoil, hAir);
                heroic.Add(new KeyPose(0.68f, hAir));
                AddRamp(heroic, 0.68f, 0.88f, 4, EaseOutCubic, hAir, hSlam);
                AddRamp(heroic, 0.88f, 1.15f, 5, EaseInOutQuad, hSlam, hIdle);
                SaveDense("Claude_Warrior_Heroic_Leap", heroic);

                // Rogue shared donor poses.
                Pose rPIdle = Sample(hunter, rIdle, 0.30f);
                Pose rDwWind = Sample(hunter, rDualChop, 0.25f);
                Pose rDwImpact = Sample(hunter, rDualChop, 0.70f);
                Pose rSlashImpact = Sample(hunter, rSlashDiag, 0.55f);
                Pose rBlockGuard = Sample(hunter, rBlock, 0.55f);
                Pose rShootRelease = Sample(hunter, rShoot, 0.65f);

                // Backstab.
                List<KeyPose> backstab = new List<KeyPose> { new KeyPose(0f, rPIdle) };
                AddRamp(backstab, 0f, 0.15f, 3, EaseOutCubic, rPIdle, rDwWind);
                AddRamp(backstab, 0.15f, 0.35f, 4, EaseOutCubic, rDwWind, rShootRelease);
                AddRamp(backstab, 0.35f, 0.62f, 4, EaseInOutQuad, rShootRelease, rPIdle);
                SaveDense("Claude_Rogue_Backstab", backstab);

                // Ambush.
                List<KeyPose> ambush = new List<KeyPose> { new KeyPose(0f, rPIdle) };
                AddRamp(ambush, 0f, 0.22f, 4, EaseOutCubic, rPIdle, rBlockGuard);
                ambush.Add(new KeyPose(0.40f, rBlockGuard));
                AddRamp(ambush, 0.40f, 0.62f, 4, EaseOutCubic, rBlockGuard, rDwWind);
                AddRamp(ambush, 0.62f, 0.85f, 4, EaseOutCubic, rDwWind, rDwImpact);
                AddRamp(ambush, 0.85f, 1.15f, 5, EaseInOutQuad, rDwImpact, rPIdle);
                SaveDense("Claude_Rogue_Ambush", ambush);

                // Eviscerate / Rogue_Finisher_Slash.
                List<KeyPose> eviscerate = new List<KeyPose> { new KeyPose(0f, rPIdle) };
                AddRamp(eviscerate, 0f, 0.25f, 4, EaseOutCubic, rPIdle, rDwWind);
                eviscerate.Add(new KeyPose(0.42f, rDwWind));
                AddRamp(eviscerate, 0.42f, 0.65f, 4, EaseOutCubic, rDwWind, rDwImpact);
                AddRamp(eviscerate, 0.65f, 0.82f, 3, EaseOutCubic, rDwImpact, rSlashImpact);
                AddRamp(eviscerate, 0.82f, 1.15f, 5, EaseInOutQuad, rSlashImpact, rPIdle);
                SaveDense("Claude_Rogue_Finisher_Slash", eviscerate);

                // Pummel / Punch_A: direct port of ClaudeCraft's authored bone offsets.
                Pose rest = Capture(hunter.transform);
                float[] times = { 0f, 0.14f, 0.32f, 0.50f, 0.70f };
                List<KeyPose> pummel = new List<KeyPose>();
                for (int i = 0; i < times.Length; i++)
                {
                    Pose p = rest.Clone();
                    Apply(p, "upperarm.r", 'z', new[] {-90f,-20f,46f,5f,-90f}[i]);
                    Apply(p, "upperarm.r", 'x', new[] {-120f,-90f,4f,-65f,-120f}[i]);
                    Apply(p, "lowerarm.r", 'z', new[] {60f,100f,12f,55f,60f}[i]);
                    Apply(p, "upperarm.l", 'z', new[] {60f,66f,52f,60f,60f}[i]);
                    Apply(p, "upperarm.l", 'x', new[] {-130f,-118f,-108f,-124f,-130f}[i]);
                    Apply(p, "lowerarm.l", 'z', new[] {-60f,-72f,-50f,-60f,-60f}[i]);
                    Apply(p, "chest", 'y', new[] {0f,-14f,22f,6f,0f}[i]);
                    Apply(p, "spine", 'y', new[] {0f,-8f,14f,4f,0f}[i]);
                    Apply(p, "spine", 'x', new[] {0f,-2f,6f,2f,0f}[i]);
                    Apply(p, "hips", 'y', new[] {0f,-4f,6f,2f,0f}[i]);
                    Apply(p, "hips", 'x', new[] {0f,-2f,5f,1f,0f}[i]);
                    pummel.Add(new KeyPose(times[i], p));
                }
                SaveDense("Claude_Punch_A", pummel);

                Validate("Claude_Warrior_Heroic_Leap", 1.10f);
                Validate("Claude_Rogue_Backstab", 0.60f);
                Validate("Claude_Rogue_Ambush", 1.10f);
                Validate("Claude_Rogue_Finisher_Slash", 1.10f);
                Validate("Claude_Punch_A", 0.68f);
                Debug.Log("[CLAUDE BAKER] FIVE_CLIPS_GREEN");
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
                UnityEngine.Object.DestroyImmediate(hunter);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static AnimationClip[] LoadClips(string path)
        {
            AnimationClip[] clips = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .Where(c => c != null && !c.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (clips.Length == 0) throw new Exception("[CLAUDE BAKER] No clips in " + path);
            return clips;
        }

        private static AnimationClip Find(AnimationClip[] clips, string name)
        {
            AnimationClip clip = clips.FirstOrDefault(c => c.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                ?? clips.FirstOrDefault(c => c.name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0);
            if (clip == null) throw new Exception("[CLAUDE BAKER] Missing donor clip: " + name);
            return clip;
        }

        private static AnimationClip FindTokens(AnimationClip[] clips, params string[] tokens)
        {
            AnimationClip clip = clips.FirstOrDefault(c => {
                string n = c.name.ToLowerInvariant();
                return tokens.All(t => n.Contains(t.ToLowerInvariant()));
            });
            if (clip == null)
                throw new Exception("[CLAUDE BAKER] Missing donor tokens: " + string.Join("+", tokens));
            return clip;
        }

        private static Pose Sample(GameObject target, AnimationClip clip, float time)
        {
            try
            {
                AnimationMode.StartAnimationMode();
                AnimationMode.SampleAnimationClip(target, clip, Mathf.Clamp(time, 0f, clip.length));
                return Capture(target.transform);
            }
            finally
            {
                if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
            }
        }

        private static Pose Capture(Transform root)
        {
            Pose p = new Pose();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == root) continue;
                if (t.GetComponent<Renderer>() != null || t.GetComponent<MeshFilter>() != null) continue;
                string lower = t.name.ToLowerInvariant();
                if (lower.Contains("sword") || lower.Contains("shield") || lower.Contains("dagger") ||
                    lower.Contains("axe") || lower.Contains("bow") || lower.Contains("quiver") ||
                    lower.Contains("weapon")) continue;

                string path = RelativePath(root, t);
                p.Bones[path] = new BoneState {
                    Name = t.name,
                    Position = t.localPosition,
                    Rotation = t.localRotation,
                    Scale = t.localScale
                };
            }
            if (p.Bones.Count < 15)
                throw new Exception("[CLAUDE BAKER] Skeleton capture too small: " + p.Bones.Count);
            return p;
        }

        private static string RelativePath(Transform root, Transform t)
        {
            List<string> names = new List<string>();
            for (Transform p = t; p != null && p != root; p = p.parent) names.Add(p.name);
            names.Reverse();
            return string.Join("/", names);
        }

        private static Pose Blend(Pose a, Pose b, float t)
        {
            Pose p = new Pose();
            foreach (var kv in a.Bones)
            {
                BoneState av = kv.Value;
                BoneState bv;
                if (!b.Bones.TryGetValue(kv.Key, out bv)) bv = av;
                p.Bones[kv.Key] = new BoneState {
                    Name = av.Name,
                    Position = Vector3.LerpUnclamped(av.Position, bv.Position, t),
                    Rotation = Quaternion.SlerpUnclamped(av.Rotation, bv.Rotation, t),
                    Scale = Vector3.LerpUnclamped(av.Scale, bv.Scale, t)
                };
            }
            return p;
        }

        private static void AddRamp(List<KeyPose> list, float from, float to, int steps,
            Func<float,float> ease, Pose a, Pose b)
        {
            for (int s = 1; s <= steps; s++)
            {
                float f = s / (float)steps;
                list.Add(new KeyPose(Mathf.Lerp(from, to, f), Blend(a, b, ease(f))));
            }
        }

        private static float EaseOutCubic(float t)
        {
            float x = 1f - t;
            return 1f - x * x * x;
        }

        private static float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
        }

        private static Pose Evaluate(List<KeyPose> keys, float time)
        {
            if (time <= keys[0].Time) return keys[0].Pose;
            if (time >= keys[keys.Count - 1].Time) return keys[keys.Count - 1].Pose;
            for (int i = 1; i < keys.Count; i++)
            {
                if (time > keys[i].Time) continue;
                KeyPose a = keys[i - 1], b = keys[i];
                float t = Mathf.InverseLerp(a.Time, b.Time, time);
                return Blend(a.Pose, b.Pose, t);
            }
            return keys[keys.Count - 1].Pose;
        }

        private static void SaveDense(string name, List<KeyPose> keys)
        {
            keys = keys.OrderBy(k => k.Time).ToList();
            float duration = keys[keys.Count - 1].Time;
            int frames = Mathf.CeilToInt(duration * SampleRate);
            List<float> times = new List<float>(frames + 2);
            List<Pose> poses = new List<Pose>(frames + 2);
            for (int i = 0; i <= frames; i++)
            {
                float t = Mathf.Min(duration, i / SampleRate);
                times.Add(t);
                poses.Add(Evaluate(keys, t));
            }
            if (times[times.Count - 1] < duration - 0.0001f)
            {
                times.Add(duration);
                poses.Add(Evaluate(keys, duration));
            }

            AnimationClip clip = new AnimationClip { name = name, frameRate = SampleRate, wrapMode = WrapMode.Once };
            string[] paths = poses[0].Bones.Keys.OrderBy(x => x).ToArray();

            foreach (string path in paths)
            {
                List<Keyframe> px = new List<Keyframe>(), py = new List<Keyframe>(), pz = new List<Keyframe>();
                List<Keyframe> rx = new List<Keyframe>(), ry = new List<Keyframe>(), rz = new List<Keyframe>(), rw = new List<Keyframe>();
                List<Keyframe> sx = new List<Keyframe>(), sy = new List<Keyframe>(), sz = new List<Keyframe>();
                Quaternion prev = Quaternion.identity;
                bool havePrev = false;

                for (int i = 0; i < times.Count; i++)
                {
                    BoneState b;
                    if (!poses[i].Bones.TryGetValue(path, out b)) continue;
                    Quaternion q = b.Rotation;
                    if (havePrev && Quaternion.Dot(prev, q) < 0f)
                        q = new Quaternion(-q.x, -q.y, -q.z, -q.w);
                    prev = q; havePrev = true;
                    float t = times[i];

                    px.Add(new Keyframe(t, b.Position.x)); py.Add(new Keyframe(t, b.Position.y)); pz.Add(new Keyframe(t, b.Position.z));
                    rx.Add(new Keyframe(t, q.x)); ry.Add(new Keyframe(t, q.y)); rz.Add(new Keyframe(t, q.z)); rw.Add(new Keyframe(t, q.w));
                    sx.Add(new Keyframe(t, b.Scale.x)); sy.Add(new Keyframe(t, b.Scale.y)); sz.Add(new Keyframe(t, b.Scale.z));
                }

                SetCurve(clip, path, "m_LocalPosition.x", px);
                SetCurve(clip, path, "m_LocalPosition.y", py);
                SetCurve(clip, path, "m_LocalPosition.z", pz);
                SetCurve(clip, path, "m_LocalRotation.x", rx);
                SetCurve(clip, path, "m_LocalRotation.y", ry);
                SetCurve(clip, path, "m_LocalRotation.z", rz);
                SetCurve(clip, path, "m_LocalRotation.w", rw);
                SetCurve(clip, path, "m_LocalScale.x", sx);
                SetCurve(clip, path, "m_LocalScale.y", sy);
                SetCurve(clip, path, "m_LocalScale.z", sz);
            }

            clip.EnsureQuaternionContinuity();
            string pathOut = TargetDir + "/" + name + ".anim";
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(pathOut) != null) AssetDatabase.DeleteAsset(pathOut);
            AssetDatabase.CreateAsset(clip, pathOut);
            Debug.Log("[CLAUDE BAKER] BAKED " + name + " • " + duration.ToString("0.000") +
                      "s • bones=" + paths.Length + " • frames=" + times.Count);
        }

        private static void SetCurve(AnimationClip clip, string path, string property, List<Keyframe> keys)
        {
            AnimationCurve curve = new AnimationCurve(keys.ToArray());
            for (int i = 0; i < curve.length; i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
        }

        private static void Apply(Pose pose, string boneName, char axis, float degrees)
        {
            string path = pose.FindPathByBoneName(boneName);
            if (string.IsNullOrEmpty(path))
                throw new Exception("[CLAUDE BAKER] Pummel bone missing: " + boneName);

            Vector3 v = axis == 'x' ? Vector3.right : axis == 'y' ? Vector3.up : Vector3.forward;
            BoneState b = pose.Bones[path];
            b.Rotation = b.Rotation * Quaternion.AngleAxis(degrees, v);
        }

        private static void Validate(string name, float minimumLength)
        {
            string path = TargetDir + "/" + name + ".anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) throw new Exception("[CLAUDE BAKER] Missing baked clip: " + name);
            if (clip.length < minimumLength)
                throw new Exception("[CLAUDE BAKER] Clip too short: " + name + " / " + clip.length);
            int bindings = AnimationUtility.GetCurveBindings(clip).Length;
            if (bindings < 40)
                throw new Exception("[CLAUDE BAKER] Too few bindings: " + name + " / " + bindings);
        }
    }
}
#endif
