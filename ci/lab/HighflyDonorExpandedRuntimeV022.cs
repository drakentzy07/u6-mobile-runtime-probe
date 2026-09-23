using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.SkillLab
{
    public enum HighflyDonorExpandedSkillV022
    {
        SigilMelee,
        SigilRanged,
        SigilFlash,
        SigilFireball
    }

    [DisallowMultipleComponent]
    public sealed class HighflyDonorExpandedRuntimeV022 : MonoBehaviour
    {
        public static HighflyDonorExpandedRuntimeV022 Instance { get; private set; }

        private const float MeleeTraceWindow = 0.30f;
        private const float MeleeTraceRadius = 1.00f;
        private const float MeleeDamage = 18f;
        private const float MeleePoise = 1.5f;
        private const float MeleeCooldown = 0.50f;

        private const float RangedSpeed = 26f;
        private const float RangedRadius = 0.35f;
        private const float RangedDamage = 12f;
        private const float RangedPoise = 1f;
        private const float RangedDuration = 3f;
        private const float RangedCooldown = 0.40f;

        private const float FlashDistance = 5f;
        private const float FlashObstacleRadius = 0.4f;
        private const float FlashCost = 20f;
        private const float FlashCooldown = 3f;

        private const float FireballBaseDamage = 22f;
        private const float FireballBasePoise = 2f;
        private const float FireballSpeed = 16f;
        private const float FireballDuration = 3f;
        private const float FireballChargeSeconds = 1.2f;
        private const float FireballMinRadius = 0.6f;
        private const float FireballMaxRadius = 2.5f;
        private const float FireballMinStun = 0.8f;
        private const float FireballMaxStun = 2.5f;
        private const float FireballMaxMultiplier = 2.5f;
        private const float FireballAimDistance = 14f;
        private const float FireballCost = 25f;
        private const float FireballCooldown = 4f;

        private PlayerController _player;
        private PlayerStats _stats;
        private CharacterController _cc;
        private bool _busy;
        private float _meleeReadyAt;
        private float _rangedReadyAt;
        private float _flashReadyAt;
        private float _fireballReadyAt;

        private void Awake()
        {
            Instance = this;
            _player = GetComponent<PlayerController>();
            _stats = GetComponent<PlayerStats>();
            _cc = GetComponent<CharacterController>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Preview(HighflyDonorExpandedSkillV022 skill)
        {
            if (_busy) return;

            switch (skill)
            {
                case HighflyDonorExpandedSkillV022.SigilMelee:
                    StartIfReady(_meleeReadyAt, "SIGIL MELEE", () => StartCoroutine(SigilMelee()));
                    break;
                case HighflyDonorExpandedSkillV022.SigilRanged:
                    StartIfReady(_rangedReadyAt, "SIGIL RANGED", () => StartCoroutine(SigilRanged()));
                    break;
                case HighflyDonorExpandedSkillV022.SigilFlash:
                    StartIfReady(_flashReadyAt, "SIGIL FLASH", () => StartCoroutine(SigilFlash()));
                    break;
                case HighflyDonorExpandedSkillV022.SigilFireball:
                    StartIfReady(_fireballReadyAt, "SIGIL FIREBALL", () => StartCoroutine(SigilFireballFullCharge()));
                    break;
            }
        }

        private void StartIfReady(float readyAt, string label, System.Action start)
        {
            if (Time.unscaledTime < readyAt)
            {
                HighflySkillLabMetrics.RecordAction(
                    label + " • CD " + Mathf.Max(0f, readyAt - Time.unscaledTime).ToString("0.0") + "s",
                    0);
                return;
            }
            start();
        }

        private IEnumerator SigilMelee()
        {
            _busy = true;
            _meleeReadyAt = Time.unscaledTime + MeleeCooldown;
            try
            {
                FaceNearestTarget(8f);
                HighflySkillLabMetrics.RecordAction(
                    "SIGIL • MELEE • RAW SOURCE VERIFIED • 0.30s / r=1.0 / DMG 18 / POISE 1.5",
                    1);

                HighflyParkourAnimationV010.Instance?.Play("Sword_Regular_A", 1f, 0.46f);

                var hit = new HashSet<CharacterStats>();
                float end = Time.unscaledTime + MeleeTraceWindow;
                while (Time.unscaledTime < end)
                {
                    Vector3 socket = transform.position + Vector3.up * 1f + transform.forward * 1.2f;
                    Collider[] cols = Physics.OverlapSphere(socket, MeleeTraceRadius, ~0, QueryTriggerInteraction.Ignore);
                    ApplyUniqueHits(cols, hit, MeleeDamage, MeleePoise, 1f);
                    yield return null;
                }

                HighflySkillLabMetrics.RecordAction("SIGIL • MELEE • hits " + hit.Count, hit.Count);
            }
            finally { _busy = false; }
        }

        private IEnumerator SigilRanged()
        {
            _busy = true;
            _rangedReadyAt = Time.unscaledTime + RangedCooldown;
            try
            {
                FaceNearestTarget(30f);
                HighflySkillLabMetrics.RecordAction(
                    "SIGIL • RANGED • 26m/s / r=0.35 / DMG 12 / POISE 1 / life 3s",
                    1);

                HighflyParkourAnimationV010.Instance?.Play("OverhandThrow", 1.35f, 0.42f);
                yield return new WaitForSecondsRealtime(0.12f);

                Vector3 muzzle = transform.position + Vector3.up * 1.4f + transform.forward * 0.6f;
                Vector3 dir = AimDirection(muzzle, 30f);
                SpawnProjectile(
                    "SIGIL_RANGED_RAW",
                    muzzle,
                    dir,
                    RangedSpeed,
                    RangedRadius,
                    RangedDuration,
                    RangedDamage,
                    RangedPoise,
                    1f,
                    new Color(0.25f, 0.72f, 1f, 1f),
                    0f);
            }
            finally { _busy = false; }
        }

        private IEnumerator SigilFlash()
        {
            _busy = true;
            try
            {
                if (_stats != null && !_stats.UseVolition(FlashCost))
                {
                    HighflySkillLabMetrics.RecordAction("SIGIL FLASH • VOLITION INSUFICIENTE (20)", 0);
                    yield break;
                }

                _flashReadyAt = Time.unscaledTime + FlashCooldown;
                Vector3 dir = InputOrForward();
                float dist = FlashDistance;

                bool wasEnabled = _cc != null && _cc.enabled;
                if (_cc != null) _cc.enabled = false;

                Vector3 origin = transform.position + Vector3.up;
                if (Physics.SphereCast(
                    origin,
                    FlashObstacleRadius,
                    dir,
                    out RaycastHit obstacle,
                    FlashDistance,
                    ~0,
                    QueryTriggerInteraction.Ignore))
                {
                    CharacterStats target = obstacle.collider != null
                        ? obstacle.collider.GetComponentInParent<CharacterStats>()
                        : null;

                    if (target == null || target == GetComponent<CharacterStats>())
                        dist = Mathf.Max(0f, obstacle.distance - FlashObstacleRadius);
                }

                if (_cc != null) _cc.enabled = wasEnabled;

                HighflySkillLabMetrics.RecordAction(
                    "SIGIL • FLASH • 5m / sphere 0.4 / COST 20 / CD 3s",
                    0);

                SpawnFlashMarker(transform.position, new Color(0.35f, 0.75f, 1f, 1f));
                SafeMove(transform.position + dir * dist);
                SpawnFlashMarker(transform.position, new Color(0.65f, 0.35f, 1f, 1f));
                yield return new WaitForSecondsRealtime(0.10f);
            }
            finally { _busy = false; }
        }

        private IEnumerator SigilFireballFullCharge()
        {
            _busy = true;
            try
            {
                if (_stats != null && !_stats.UseVolition(FireballCost))
                {
                    HighflySkillLabMetrics.RecordAction("SIGIL FIREBALL • VOLITION INSUFICIENTE (25)", 0);
                    yield break;
                }

                _fireballReadyAt = Time.unscaledTime + FireballCooldown;
                FaceNearestTarget(FireballAimDistance);

                HighflySkillLabMetrics.RecordAction(
                    "SIGIL • FIREBALL • AUTO PREVIEW FULL CHARGE 1.2s",
                    0);

                GameObject reticle = BuildReticle(FireballMaxRadius);
                float start = Time.unscaledTime;
                while (Time.unscaledTime - start < FireballChargeSeconds)
                {
                    Vector3 aim = AimPoint(FireballAimDistance);
                    if (reticle != null)
                        reticle.transform.position = aim + Vector3.up * 0.025f;
                    yield return null;
                }

                Vector3 muzzle = transform.position + Vector3.up * 1.4f + transform.forward * 0.6f;
                Vector3 aimPoint = AimPoint(FireballAimDistance);
                Vector3 dir = aimPoint - muzzle;
                if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;

                float damage = FireballBaseDamage * FireballMaxMultiplier;
                float poise = FireballBasePoise * FireballMaxMultiplier;

                SpawnProjectile(
                    "SIGIL_FIREBALL_RAW",
                    muzzle,
                    dir.normalized,
                    FireballSpeed,
                    FireballMaxRadius,
                    FireballDuration,
                    damage,
                    poise,
                    1f,
                    new Color(1f, 0.42f, 0.08f, 1f),
                    FireballMaxStun);

                if (reticle != null) Destroy(reticle);

                HighflySkillLabMetrics.RecordAction(
                    "SIGIL • FIREBALL RELEASE • DMG 55 / r=2.5 / STUN 2.5s / CD 4s",
                    1);
            }
            finally { _busy = false; }
        }

        private void SpawnProjectile(
            string label,
            Vector3 position,
            Vector3 direction,
            float speed,
            float radius,
            float duration,
            float damage,
            float poise,
            float knockback,
            Color color,
            float stunSeconds)
        {
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = label;
            Collider ownCollider = projectile.GetComponent<Collider>();
            if (ownCollider != null) Destroy(ownCollider);
            projectile.transform.position = position;
            projectile.transform.localScale = Vector3.one * Mathf.Clamp(radius * 0.55f, 0.18f, 1.2f);

            Renderer r = projectile.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial = HighflyLabVisuals.CreateMaterial(color, color * 1.8f);

            TrailRenderer trail = projectile.AddComponent<TrailRenderer>();
            trail.time = 0.22f;
            trail.startWidth = Mathf.Max(0.05f, radius * 0.32f);
            trail.endWidth = 0f;
            trail.sharedMaterial = HighflyLabVisuals.CreateFxMaterial(color);

            StartCoroutine(ProjectileRoutine(
                projectile,
                direction.normalized,
                speed,
                radius,
                duration,
                damage,
                poise,
                knockback,
                stunSeconds,
                color));
        }

        private IEnumerator ProjectileRoutine(
            GameObject projectile,
            Vector3 direction,
            float speed,
            float radius,
            float duration,
            float damage,
            float poise,
            float knockback,
            float stunSeconds,
            Color color)
        {
            CharacterStats owner = GetComponent<CharacterStats>();
            float end = Time.unscaledTime + duration;
            Vector3 previous = projectile != null ? projectile.transform.position : transform.position;

            while (projectile != null && Time.unscaledTime < end)
            {
                float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
                Vector3 next = previous + direction * speed * dt;

                Collider[] cols = Physics.OverlapCapsule(previous, next, radius, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < cols.Length; i++)
                {
                    CharacterStats target = cols[i] != null ? cols[i].GetComponentInParent<CharacterStats>() : null;
                    if (target == null || target == owner) continue;

                    projectile.transform.position = next;
                    target.TakeDamage(damage, poise, transform);
                    HighflySkillLabMetrics.RecordHit(damage);
                    ApplyKnockback(target.transform, direction, knockback);
                    if (stunSeconds > 0f)
                        StartCoroutine(StunTarget(target, stunSeconds));
                    SpawnImpact(next, radius, color);
                    Destroy(projectile);
                    yield break;
                }

                projectile.transform.position = next;
                previous = next;
                yield return null;
            }

            if (projectile != null) Destroy(projectile);
        }

        private IEnumerator StunTarget(CharacterStats target, float seconds)
        {
            if (target == null) yield break;
            Enemy enemy = target.GetComponentInParent<Enemy>();
            if (enemy == null) yield break;

            bool old = enemy.enabled;
            enemy.enabled = false;
            yield return new WaitForSecondsRealtime(seconds);
            if (enemy != null) enemy.enabled = old;
        }

        private void ApplyUniqueHits(
            Collider[] cols,
            HashSet<CharacterStats> hit,
            float damage,
            float poise,
            float knockback)
        {
            CharacterStats owner = GetComponent<CharacterStats>();
            for (int i = 0; i < cols.Length; i++)
            {
                CharacterStats target = cols[i] != null ? cols[i].GetComponentInParent<CharacterStats>() : null;
                if (target == null || target == owner || hit.Contains(target)) continue;
                hit.Add(target);
                target.TakeDamage(damage, poise, transform);
                ApplyKnockback(target.transform, transform.forward, knockback);
            }
        }

        private void ApplyKnockback(Transform target, Vector3 direction, float distance)
        {
            if (target == null || distance <= 0f) return;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f) direction = transform.forward;
            direction.Normalize();

            CharacterController targetCc = target.GetComponent<CharacterController>();
            if (targetCc != null && targetCc.enabled)
                targetCc.Move(direction * distance);
            else
                target.position += direction * distance;
        }

        private Vector3 AimDirection(Vector3 origin, float maxDistance)
        {
            CharacterStats target = FindNearestTarget(maxDistance);
            if (target != null)
            {
                Vector3 to = target.transform.position + Vector3.up - origin;
                if (to.sqrMagnitude > 0.0001f) return to.normalized;
            }
            return transform.forward;
        }

        private Vector3 AimPoint(float maxDistance)
        {
            CharacterStats target = FindNearestTarget(maxDistance);
            if (target != null)
                return target.transform.position + Vector3.up * 0.7f;

            Vector3 flat = transform.forward;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) flat = Vector3.forward;
            return transform.position + flat.normalized * maxDistance;
        }

        private CharacterStats FindNearestTarget(float maxDistance)
        {
            CharacterStats owner = GetComponent<CharacterStats>();
            CharacterStats[] all = Object.FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
            CharacterStats best = null;
            float bestSq = maxDistance * maxDistance;

            for (int i = 0; i < all.Length; i++)
            {
                CharacterStats candidate = all[i];
                if (candidate == null || candidate == owner) continue;
                float sq = (candidate.transform.position - transform.position).sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = candidate;
                }
            }
            return best;
        }

        private void FaceNearestTarget(float maxDistance)
        {
            CharacterStats target = FindNearestTarget(maxDistance);
            if (target == null) return;

            Vector3 dir = target.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        private Vector3 InputOrForward()
        {
            Vector3 dir = transform.forward;
            if (_player != null && Camera.main != null)
            {
                // Preview path intentionally remains deterministic on mobile: facing direction
                // is the fallback used by the original donor when there is no movement input.
                dir = transform.forward;
            }

            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = Vector3.forward;
            return dir.normalized;
        }

        private void SafeMove(Vector3 position)
        {
            bool wasEnabled = _cc != null && _cc.enabled;
            if (_cc != null) _cc.enabled = false;
            transform.position = position;
            if (_cc != null) _cc.enabled = wasEnabled;
        }

        private GameObject BuildReticle(float radius)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "SIGIL_FIREBALL_RETICLE_RAW";
            Collider c = go.GetComponent<Collider>();
            if (c != null) Destroy(c);
            go.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);
            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
                r.sharedMaterial = HighflyLabVisuals.CreateMaterial(
                    new Color(1f, 0.45f, 0.1f, 0.25f),
                    new Color(1f, 0.18f, 0.02f, 0.8f));
            return go;
        }

        private void SpawnFlashMarker(Vector3 position, Color color)
        {
            GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fx.name = "SIGIL_FLASH_CUE_RAW";
            Collider c = fx.GetComponent<Collider>();
            if (c != null) Destroy(c);
            fx.transform.position = position + Vector3.up;
            fx.transform.localScale = Vector3.one * 0.65f;
            Renderer r = fx.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = HighflyLabVisuals.CreateMaterial(color, color * 2f);
            Destroy(fx, 0.18f);
        }

        private void SpawnImpact(Vector3 position, float radius, Color color)
        {
            GameObject fx = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            fx.name = "SIGIL_PROJECTILE_HIT_RAW";
            Collider c = fx.GetComponent<Collider>();
            if (c != null) Destroy(c);
            fx.transform.position = position;
            fx.transform.localScale = Vector3.one * Mathf.Max(0.35f, radius * 0.8f);
            Renderer r = fx.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = HighflyLabVisuals.CreateMaterial(color, color * 2.2f);
            Destroy(fx, 0.14f);
        }
    }
}
