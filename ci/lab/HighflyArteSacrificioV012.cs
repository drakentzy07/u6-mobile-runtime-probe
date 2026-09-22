/*
 * HIGHFLY Skill Lab v0.12 — ARTE DEL SACRIFICIO donor vertical slice
 *
 * Dragon Souls donor reference:
 *   https://github.com/btuhany/DragonSouls-Unity3D
 *   Copyright (c) 2023 btuhany — MIT License.
 *
 * This HIGHFLY integration preserves the donor's core throw -> embed -> return
 * architecture while adapting it to Lucid/HIGHFLY interfaces and removing
 * donor-only dependencies (DG.Tweening, donor PlayerStateMachine, donor Damage/Sound).
 *
 * SubspaceHunter-SAO is used as an additional sequencing reference for
 * spawn/release/hit-feedback. No SAO/IP raw assets are included here.
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    public sealed class HighflyArteSacrificioV012 : MonoBehaviour
    {
        public static HighflyArteSacrificioV012 Instance { get; private set; }

        private enum WeaponState
        {
            Ready,
            Outbound,
            Embedded,
            Anchored,
            Returning
        }

        private PlayerController _player;
        private CharacterStats _ownerStats;
        private PlayerWeapon _playerWeapon;
        private Transform _weapon;
        private Transform _originalParent;
        private Vector3 _originalLocalPosition;
        private Quaternion _originalLocalRotation;
        private Vector3 _originalLocalScale;
        private bool _weaponScriptEnabled;
        private Collider _weaponCollider;
        private bool _weaponColliderEnabled;
        private TrailRenderer _trail;
        private bool _trailWasEnabled;
        private bool _trailCreatedByUs;
        private CharacterStats _embeddedTarget;
        private WeaponState _state = WeaponState.Ready;
        private GameObject _fallbackWeapon;

        private static readonly Color OutboundColor = new Color(0.28f, 0.88f, 1f, 1f);
        private static readonly Color ReturnColor = new Color(0.82f, 0.30f, 1f, 1f);
        private static readonly Color DetonateColor = new Color(1f, 0.34f, 0.18f, 1f);

        private void Awake()
        {
            Instance = this;
            _player = GetComponent<PlayerController>();
            _ownerStats = GetComponent<CharacterStats>();
            AcquireWeapon();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            RestoreWeaponImmediate();
        }

        public void TriggerManual()
        {
            switch (_state)
            {
                case WeaponState.Ready:
                    StartCoroutine(ThrowRoutine(false));
                    break;
                case WeaponState.Embedded:
                case WeaponState.Anchored:
                    StartCoroutine(RecallRoutine(false));
                    break;
            }
        }

        public void ForcePreview()
        {
            StopAllCoroutines();
            HighflyTimeDilationManager.ForceReset();
            RestoreWeaponImmediate();
            StartCoroutine(PreviewRoutine());
        }

        public void TriggerDetonateRecall()
        {
            if (_state == WeaponState.Embedded || _state == WeaponState.Anchored)
                StartCoroutine(DetonateRecallRoutine());
        }

        private IEnumerator PreviewRoutine()
        {
            yield return StartCoroutine(ThrowRoutine(true));
            if (_state != WeaponState.Embedded && _state != WeaponState.Anchored)
                yield break;

            yield return new WaitForSecondsRealtime(0.42f);
            yield return StartCoroutine(DetonateRoutine());
            yield return StartCoroutine(RecallRoutine(true));
        }

        private void AcquireWeapon()
        {
            if (_playerWeapon == null)
                _playerWeapon = GetComponentInChildren<PlayerWeapon>(true);

            if (_playerWeapon != null)
            {
                _weapon = _playerWeapon.transform;
                return;
            }

            if (_fallbackWeapon == null)
                _fallbackWeapon = BuildFallbackBlade();

            _weapon = _fallbackWeapon != null ? _fallbackWeapon.transform : null;
        }

        private GameObject BuildFallbackBlade()
        {
            GameObject root = new GameObject("HF_ARTE_FALLBACK_BLADE");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = new Vector3(0.42f, 1.05f, 0.34f);
            root.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);

            Mesh mesh = new Mesh { name = "HF_FallbackBladeMesh" };
            mesh.vertices = new[]
            {
                new Vector3(-0.055f, 0f, -0.025f), new Vector3(0.055f, 0f, -0.025f),
                new Vector3(-0.035f, 1.05f, -0.018f), new Vector3(0.035f, 1.05f, -0.018f),
                new Vector3(0f, 1.26f, 0f),
                new Vector3(-0.055f, 0f, 0.025f), new Vector3(0.055f, 0f, 0.025f),
                new Vector3(-0.035f, 1.05f, 0.018f), new Vector3(0.035f, 1.05f, 0.018f)
            };
            mesh.triangles = new[]
            {
                0,2,1, 1,2,3, 2,4,3,
                6,7,5, 5,7,2, 7,4,2,
                0,5,2, 5,7,2,
                1,3,6, 6,3,8,
                2,7,4, 7,8,4,
                0,1,5, 1,6,5
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            MeshFilter mf = root.AddComponent<MeshFilter>();
            mf.sharedMesh = mesh;
            MeshRenderer mr = root.AddComponent<MeshRenderer>();
            mr.sharedMaterial = HighflyLabVisuals.CreateMaterial(
                new Color(0.72f, 0.82f, 0.92f, 1f),
                new Color(0.12f, 0.42f, 0.68f, 1f));

            GameObject guard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            guard.name = "Guard";
            guard.transform.SetParent(root.transform, false);
            guard.transform.localPosition = new Vector3(0f, -0.06f, 0f);
            guard.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            guard.transform.localScale = new Vector3(0.055f, 0.24f, 0.055f);
            Object.Destroy(guard.GetComponent<Collider>());
            guard.GetComponent<Renderer>().sharedMaterial = HighflyLabVisuals.CreateMaterial(
                new Color(0.12f, 0.15f, 0.20f, 1f),
                new Color(0.06f, 0.20f, 0.36f, 1f));

            return root;
        }

        private void PrepareWeaponForThrow()
        {
            AcquireWeapon();
            if (_weapon == null) return;

            _originalParent = _weapon.parent;
            _originalLocalPosition = _weapon.localPosition;
            _originalLocalRotation = _weapon.localRotation;
            _originalLocalScale = _weapon.localScale;

            if (_playerWeapon != null)
            {
                _weaponScriptEnabled = _playerWeapon.enabled;
                _playerWeapon.enabled = false;
            }

            _weaponCollider = _weapon.GetComponentInChildren<Collider>(true);
            if (_weaponCollider != null)
            {
                _weaponColliderEnabled = _weaponCollider.enabled;
                _weaponCollider.enabled = false;
            }

            _trail = _weapon.GetComponentInChildren<TrailRenderer>(true);
            if (_trail == null)
            {
                _trail = _weapon.gameObject.AddComponent<TrailRenderer>();
                _trailCreatedByUs = true;
                _trail.time = 0.20f;
                _trail.minVertexDistance = 0.035f;
                _trail.widthMultiplier = 0.12f;
                _trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _trail.receiveShadows = false;
            }
            else
            {
                _trailCreatedByUs = false;
                _trailWasEnabled = _trail.enabled;
            }

            _trail.sharedMaterial = HighflyLabVisuals.CreateFxMaterial(OutboundColor);
            _trail.startColor = OutboundColor;
            _trail.endColor = new Color(OutboundColor.r, OutboundColor.g, OutboundColor.b, 0f);
            _trail.enabled = true;
            _trail.Clear();

            Vector3 worldPos = _weapon.position;
            Quaternion worldRot = _weapon.rotation;
            _weapon.SetParent(null, true);
            _weapon.position = worldPos;
            _weapon.rotation = worldRot;
        }

        private IEnumerator ThrowRoutine(bool preview)
        {
            if (_state != WeaponState.Ready) yield break;

            CharacterStats target = FindBestTarget(12f);
            PrepareWeaponForThrow();
            if (_weapon == null)
            {
                HighflySkillLabMetrics.RecordAction("ARTE • SIN ARMA DISPONIBLE", 0);
                yield break;
            }

            FaceTarget(target);
            HighflyParkourAnimationV010.Instance?.Play("OverhandThrow", 1.08f, 0.66f);
            HighflySkillLabMetrics.RecordAction("ARTE DEL SACRIFICIO • PREPARAR", 0);

            yield return new WaitForSecondsRealtime(0.12f);

            _state = WeaponState.Outbound;
            Vector3 start = transform.position + Vector3.up * 1.16f + transform.forward * 0.48f;
            Vector3 end = target != null ? TargetCenter(target) : ResolveWorldAnchor();
            Vector3 control = Vector3.Lerp(start, end, 0.52f) + Vector3.up * 0.75f;

            _weapon.position = start;
            SpawnResource("HIGHFLY/SkillVFX/ElectricalSparks", start, 0.55f, 1.1f);
            SpawnArcRing(start, OutboundColor, 0.50f, 0.22f);

            const float duration = 0.34f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                float s = Mathf.SmoothStep(0f, 1f, u);
                Vector3 a = Vector3.Lerp(start, control, s);
                Vector3 b = Vector3.Lerp(control, end, s);
                _weapon.position = Vector3.Lerp(a, b, s);
                _weapon.Rotate(980f * Time.unscaledDeltaTime, 420f * Time.unscaledDeltaTime, 130f * Time.unscaledDeltaTime, Space.Self);
                yield return null;
            }

            _weapon.position = end;
            SpawnImpact(end);

            if (target != null)
            {
                Deal(target, 24f, 22f, "ARTE • IMPACTO / EMBED");
                _embeddedTarget = target;
                _weapon.SetParent(target.transform, true);
                _state = WeaponState.Embedded;
                HighflySkillLabMetrics.RecordAction("ARTE DEL SACRIFICIO • EMBEDDED", 1);
            }
            else
            {
                _embeddedTarget = null;
                _state = WeaponState.Anchored;
                HighflySkillLabMetrics.RecordAction("ARTE DEL SACRIFICIO • ANCHORED", 1);
            }

            if (_trail != null)
                _trail.enabled = false;

            HighflyTimeDilationManager.RequestHitStop(0.055f, 0.08f);

            if (!preview)
                yield break;
        }

        private IEnumerator DetonateRecallRoutine()
        {
            yield return StartCoroutine(DetonateRoutine());
            yield return StartCoroutine(RecallRoutine(true));
        }

        private IEnumerator DetonateRoutine()
        {
            if (_state != WeaponState.Embedded && _state != WeaponState.Anchored)
                yield break;

            Vector3 center = _weapon != null ? _weapon.position : transform.position + transform.forward * 3f;
            HighflySkillLabMetrics.RecordAction("ARTE DEL SACRIFICIO • CARGA DE DETONACIÓN", 2);

            for (int i = 0; i < 4; i++)
            {
                SpawnArcRing(center, DetonateColor, 0.65f + i * 0.22f, 0.32f);
                if (i == 1)
                    SpawnResource("HIGHFLY/SkillVFX/ParticlesLight", center, 0.75f, 1.15f);
                yield return new WaitForSecondsRealtime(0.075f);
            }

            SpawnResource("HIGHFLY/SkillVFX/EnergyExplosion", center, 1.25f, 1.8f);
            SpawnResource("HIGHFLY/SkillVFX/PlasmaExplosion", center, 0.92f, 1.6f);
            SpawnArcRing(center, new Color(1f, 0.84f, 0.28f, 1f), 2.8f, 0.28f);

            CharacterStats[] all = Object.FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                CharacterStats candidate = all[i];
                if (candidate == null || candidate == _ownerStats) continue;
                if (Vector3.Distance(candidate.transform.position, center) <= 3.35f)
                {
                    bool large = candidate.name.IndexOf("LARGE", System.StringComparison.OrdinalIgnoreCase) >= 0;
                    Deal(candidate, large ? 38f : 46f, large ? 55f : 42f, "ARTE • DETONACIÓN");
                }
            }

            HighflyTimeDilationManager.RequestHitStop(0.085f, 0.05f);
            HighflySkillLabMetrics.RecordAction("ARTE DEL SACRIFICIO • DETONACIÓN", 3);
            yield return new WaitForSecondsRealtime(0.08f);
        }

        private IEnumerator RecallRoutine(bool fromDetonation)
        {
            if (_state != WeaponState.Embedded && _state != WeaponState.Anchored)
                yield break;
            if (_weapon == null)
            {
                RestoreWeaponImmediate();
                yield break;
            }

            if (_embeddedTarget != null)
            {
                Deal(_embeddedTarget, 13f, 18f, "ARTE • EXTRACCIÓN");
                SpawnImpact(_weapon.position);
            }

            _weapon.SetParent(null, true);
            _state = WeaponState.Returning;

            if (_trail != null)
            {
                _trail.sharedMaterial = HighflyLabVisuals.CreateFxMaterial(ReturnColor);
                _trail.startColor = ReturnColor;
                _trail.endColor = new Color(ReturnColor.r, ReturnColor.g, ReturnColor.b, 0f);
                _trail.enabled = true;
                _trail.Clear();
            }

            Vector3 start = _weapon.position;
            Vector3 end = CatchPoint();
            Vector3 side = transform.right * 1.25f;
            Vector3 control = Vector3.Lerp(start, end, 0.48f) + Vector3.up * 1.7f + side;
            HashSet<CharacterStats> hitOnReturn = new HashSet<CharacterStats>();

            HighflySkillLabMetrics.RecordAction(
                fromDetonation ? "ARTE • RETORNO POST-DETONACIÓN" : "ARTE • RECALL",
                4);

            const float duration = 0.48f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                float s = 1f - Mathf.Pow(1f - u, 2.3f);
                Vector3 a = Vector3.Lerp(start, control, s);
                Vector3 b = Vector3.Lerp(control, end, s);
                _weapon.position = Vector3.Lerp(a, b, s);
                _weapon.Rotate(-1080f * Time.unscaledDeltaTime, 360f * Time.unscaledDeltaTime, 170f * Time.unscaledDeltaTime, Space.Self);

                CharacterStats[] all = Object.FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
                for (int i = 0; i < all.Length; i++)
                {
                    CharacterStats candidate = all[i];
                    if (candidate == null || candidate == _ownerStats || hitOnReturn.Contains(candidate)) continue;
                    if (Vector3.Distance(TargetCenter(candidate), _weapon.position) <= 1.10f)
                    {
                        hitOnReturn.Add(candidate);
                        Deal(candidate, 16f, 16f, "ARTE • RETURN HIT");
                        SpawnResource("HIGHFLY/SkillVFX/Sparks", _weapon.position, 0.42f, 0.8f);
                    }
                }
                yield return null;
            }

            SpawnResource("HIGHFLY/SkillVFX/ParticlesLight", end, 0.56f, 0.9f);
            SpawnArcRing(end, new Color(0.84f, 0.96f, 1f, 1f), 0.42f, 0.18f);
            HighflyTimeDilationManager.RequestHitStop(0.028f, 0.20f);
            RestoreWeaponImmediate();
            HighflySkillLabMetrics.RecordAction("ARTE DEL SACRIFICIO • CATCH", 5);
        }

        private CharacterStats FindBestTarget(float radius)
        {
            CharacterStats[] all = Object.FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
            CharacterStats best = null;
            float bestSq = radius * radius;

            for (int i = 0; i < all.Length; i++)
            {
                CharacterStats candidate = all[i];
                if (candidate == null || candidate == _ownerStats) continue;

                Vector3 d = candidate.transform.position - transform.position;
                d.y = 0f;
                float sq = d.sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = candidate;
                }
            }

            return best;
        }

        private void FaceTarget(CharacterStats target)
        {
            if (target == null) return;
            Vector3 d = target.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }

        private Vector3 TargetCenter(CharacterStats target)
        {
            if (target == null)
                return transform.position + transform.forward * 4f + Vector3.up;

            Collider[] cols = target.GetComponentsInChildren<Collider>(true);
            if (cols == null || cols.Length == 0)
                return target.transform.position + Vector3.up * 0.9f;

            Bounds b = cols[0].bounds;
            for (int i = 1; i < cols.Length; i++)
                if (cols[i] != null) b.Encapsulate(cols[i].bounds);
            return b.center;
        }

        private Vector3 ResolveWorldAnchor()
        {
            Vector3 origin = transform.position + Vector3.up * 1.1f;
            Vector3 dir = transform.forward;
            if (Physics.Raycast(origin, dir, out RaycastHit hit, 10f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point;
            return origin + dir * 6f;
        }

        private Vector3 CatchPoint()
        {
            if (_originalParent != null)
                return _originalParent.TransformPoint(_originalLocalPosition);
            return transform.position + Vector3.up * 1.15f + transform.right * 0.38f + transform.forward * 0.25f;
        }

        private void Deal(CharacterStats target, float damage, float composure, string label)
        {
            if (target == null || target == _ownerStats) return;
            target.TakeDamage(damage, composure, transform);
            HighflySkillLabMetrics.RecordAction(label, 0);
        }

        private void SpawnImpact(Vector3 point)
        {
            SpawnResource("HIGHFLY/SkillVFX/Sparks", point, 0.60f, 1.0f);
            SpawnResource("HIGHFLY/SkillVFX/ElectricalSparks", point, 0.48f, 1.0f);
            SpawnArcRing(point, new Color(0.90f, 0.96f, 1f, 1f), 0.78f, 0.18f);
        }

        private static void SpawnResource(string resourcePath, Vector3 position, float scale, float life)
        {
            GameObject prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null) return;

            GameObject fx = Object.Instantiate(prefab, position, Quaternion.identity);
            fx.transform.localScale *= scale;
            Object.Destroy(fx, life);
        }

        private static void SpawnArcRing(Vector3 center, Color color, float radius, float life)
        {
            GameObject go = new GameObject("HF_ARTE_RUNE_RING");
            go.transform.position = center + Vector3.up * 0.025f;

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.useWorldSpace = false;
            lr.positionCount = 64;
            lr.widthMultiplier = Mathf.Clamp(radius * 0.035f, 0.025f, 0.085f);
            lr.sharedMaterial = HighflyLabVisuals.CreateFxMaterial(color);

            for (int i = 0; i < 64; i++)
            {
                float a = (i / 64f) * Mathf.PI * 2f;
                float wobble = 1f + Mathf.Sin(a * 6f) * 0.055f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius * wobble, 0f, Mathf.Sin(a) * radius * wobble));
            }

            Object.Destroy(go, life);
        }

        private void RestoreWeaponImmediate()
        {
            if (_weapon != null && _originalParent != null)
            {
                _weapon.SetParent(_originalParent, false);
                _weapon.localPosition = _originalLocalPosition;
                _weapon.localRotation = _originalLocalRotation;
                _weapon.localScale = _originalLocalScale;
            }

            if (_playerWeapon != null)
                _playerWeapon.enabled = _weaponScriptEnabled;

            if (_weaponCollider != null)
                _weaponCollider.enabled = _weaponColliderEnabled;

            if (_trail != null)
            {
                _trail.Clear();
                _trail.enabled = _trailCreatedByUs ? false : _trailWasEnabled;
            }

            _embeddedTarget = null;
            _state = WeaponState.Ready;
        }
    }
}
