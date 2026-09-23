using System.Collections;
using UnityEngine;

namespace Highfly.SkillLab
{
    public enum HighflyCuratedDonorSkill
    {
        Drift,
        ArteSacrificio,
        JumpSmash,
        EmberBolt,
        ThunderMark,
        FrostLance,
        MeteorBreak,
        Aegis
    }

    public static class HighflyCuratedAegisState
    {
        private static PlayerStats _owner;
        private static float _shieldHp;
        private static float _until;

        public static void Activate(PlayerStats owner, float hp, float duration)
        {
            _owner = owner;
            _shieldHp = Mathf.Max(1f, hp);
            _until = Time.unscaledTime + Mathf.Max(0.25f, duration);
        }

        public static bool TryAbsorb(PlayerStats owner, ref float damage)
        {
            if (_owner == null || owner != _owner || Time.unscaledTime > _until || _shieldHp <= 0f)
                return false;

            float absorbed = Mathf.Min(_shieldHp, Mathf.Max(0f, damage));
            _shieldHp -= absorbed;
            damage -= absorbed;

            HighflySkillLabMetrics.RecordAction("AEGIS • ABSORBE " + absorbed.ToString("0"), 0);
            return damage <= 0.01f;
        }

        public static float Remaining => Time.unscaledTime <= _until ? Mathf.Max(0f, _shieldHp) : 0f;
    }

    [DisallowMultipleComponent]
    public sealed class HighflyCuratedDonorRuntimeV015 : MonoBehaviour
    {
        public static HighflyCuratedDonorRuntimeV015 Instance { get; private set; }

        private PlayerController _player;
        private PlayerStats _stats;
        private Animator _animator;
        private bool _busy;

        private void Awake()
        {
            Instance = this;
            _player = GetComponent<PlayerController>();
            _stats = GetComponent<PlayerStats>();
            _animator = _player != null ? _player.animator : GetComponentInChildren<Animator>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Preview(HighflyCuratedDonorSkill skill)
        {
            if (_busy) return;

            switch (skill)
            {
                case HighflyCuratedDonorSkill.Drift:
                    HighflyCombatLabV010.Instance?.ForcePreview(HighflyCombatSkillV010.FormulaDrift);
                    return;
                case HighflyCuratedDonorSkill.ArteSacrificio:
                    HighflyArteSacrificioV012.Instance?.ForcePreview();
                    return;
                default:
                    StartCoroutine(Run(skill));
                    return;
            }
        }

        private IEnumerator Run(HighflyCuratedDonorSkill skill)
        {
            _busy = true;
            try
            {
                switch (skill)
                {
                    case HighflyCuratedDonorSkill.JumpSmash:
                        yield return JumpSmash();
                        break;
                    case HighflyCuratedDonorSkill.EmberBolt:
                        yield return ProjectileSkill(
                            "EMBER BOLT • SUBSPACE DONOR",
                            "HIGHFLY/DonorVFX/FireBall",
                            "HIGHFLY/DonorVFX/SmallExplosion",
                            38f, 0.48f, 16f);
                        break;
                    case HighflyCuratedDonorSkill.ThunderMark:
                        yield return ThunderMark();
                        break;
                    case HighflyCuratedDonorSkill.FrostLance:
                        yield return ProjectileSkill(
                            "FROST LANCE • SUBSPACE DONOR",
                            "HIGHFLY/DonorVFX/IceLance",
                            "HIGHFLY/DonorVFX/Sparks",
                            42f, 0.42f, 17f);
                        break;
                    case HighflyCuratedDonorSkill.MeteorBreak:
                        yield return MeteorBreak();
                        break;
                    case HighflyCuratedDonorSkill.Aegis:
                        yield return Aegis();
                        break;
                }
            }
            finally
            {
                _busy = false;
            }
        }

        private CharacterStats FindTarget(float radius = 18f)
        {
            return HighflyPremiumSkillRuntime.Instance != null
                ? HighflyPremiumSkillRuntime.Instance.FindBestTarget(radius, 360f)
                : null;
        }

        private static Vector3 TargetCenter(CharacterStats target)
        {
            if (target == null) return Vector3.zero;
            Collider[] cols = target.GetComponentsInChildren<Collider>(true);
            if (cols == null || cols.Length == 0)
                return target.transform.position + Vector3.up * 0.9f;

            Bounds b = cols[0].bounds;
            for (int i = 1; i < cols.Length; i++)
                if (cols[i] != null) b.Encapsulate(cols[i].bounds);
            return b.center;
        }

        private void Face(CharacterStats target)
        {
            if (target == null) return;
            Vector3 d = target.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }

        private void Hit(CharacterStats target, float damage, float composure, string label)
        {
            if (target == null) return;
            target.TakeDamage(damage, composure, transform);
            HighflySkillLabMetrics.RecordAction(label, 0);
        }

        private static GameObject Spawn(string path, Vector3 pos, float scale = 1f, float life = 2f)
        {
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab == null)
            {
                Debug.LogWarning("[HIGHFLY v0.15] Missing donor VFX: " + path);
                return null;
            }

            GameObject go = Object.Instantiate(prefab, pos, Quaternion.identity);
            go.transform.localScale *= scale;
            Object.Destroy(go, life);
            return go;
        }

        private IEnumerator ProjectileSkill(
            string label,
            string projectilePath,
            string impactPath,
            float damage,
            float travelSeconds,
            float radius)
        {
            CharacterStats target = FindTarget(radius);
            if (target == null)
            {
                HighflySkillLabMetrics.RecordAction(label + " • SIN OBJETIVO", 0);
                yield break;
            }

            Face(target);
            Vector3 start = transform.position + Vector3.up * 1.15f + transform.forward * 0.65f;
            Vector3 end = TargetCenter(target);

            GameObject projectile = Spawn(projectilePath, start, 0.72f, travelSeconds + 0.8f);
            if (projectile == null) yield break;

            HighflySkillLabMetrics.RecordAction(label, 0);

            float t = 0f;
            while (t < travelSeconds)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / travelSeconds);
                projectile.transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, u));
                Vector3 d = end - projectile.transform.position;
                if (d.sqrMagnitude > 0.001f)
                    projectile.transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
                yield return null;
            }

            Vector3 impact = TargetCenter(target);
            Object.Destroy(projectile);
            Spawn(impactPath, impact, 1.0f, 1.6f);
            Spawn("HIGHFLY/DonorVFX/EnergyExplosion", impact, 0.55f, 1.2f);
            Hit(target, damage, 25f, label + " • HIT");
            HighflyTimeDilationManager.RequestHitStop(0.05f, 0.10f);
        }

        private IEnumerator ThunderMark()
        {
            CharacterStats target = FindTarget(18f);
            if (target == null) yield break;

            Face(target);
            Vector3 p = TargetCenter(target);
            HighflySkillLabMetrics.RecordAction("THUNDER MARK • SUBSPACE DONOR", 0);

            Spawn("HIGHFLY/DonorVFX/ElectricalSparks", p + Vector3.up * 0.7f, 1.15f, 1.5f);
            yield return new WaitForSecondsRealtime(0.16f);
            Spawn("HIGHFLY/DonorVFX/PlasmaExplosion", p, 0.82f, 1.6f);
            Spawn("HIGHFLY/DonorVFX/Sparks", p, 1.2f, 1.2f);
            Hit(target, 48f, 34f, "THUNDER MARK • IMPACT");
            HighflyTimeDilationManager.RequestHitStop(0.065f, 0.08f);
        }

        private IEnumerator MeteorBreak()
        {
            CharacterStats target = FindTarget(20f);
            if (target == null) yield break;

            Face(target);
            Vector3 impact = TargetCenter(target);
            Vector3 start = impact + Vector3.up * 8.0f;
            GameObject meteor = Spawn("HIGHFLY/DonorVFX/FireBall", start, 1.35f, 2.2f);
            if (meteor == null) yield break;

            HighflySkillLabMetrics.RecordAction("METEOR BREAK • SUBSPACE DONOR", 0);

            const float duration = 0.62f;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float u = Mathf.Clamp01(t / duration);
                meteor.transform.position = Vector3.Lerp(start, impact, u * u);
                meteor.transform.Rotate(220f * Time.unscaledDeltaTime, 150f * Time.unscaledDeltaTime, 60f * Time.unscaledDeltaTime);
                yield return null;
            }

            Object.Destroy(meteor);
            Spawn("HIGHFLY/DonorVFX/BigExplosion", impact, 1.25f, 2.4f);
            Spawn("HIGHFLY/DonorVFX/EarthShatter", impact, 1.10f, 2.4f);
            Hit(target, 68f, 55f, "METEOR BREAK • IMPACT");
            HighflyTimeDilationManager.RequestHitStop(0.095f, 0.045f);
        }

        private IEnumerator JumpSmash()
        {
            CharacterStats target = FindTarget(10f);
            if (target != null) Face(target);

            HighflySkillLabMetrics.RecordAction("JUMP SMASH • LUCID NATIVO", 0);
            if (_animator != null)
                _animator.SetTrigger("doSmash");

            yield return new WaitForSecondsRealtime(0.58f);

            Vector3 impact = transform.position + transform.forward * 1.25f;
            Spawn("HIGHFLY/DonorVFX/EarthShatter", impact, 0.88f, 2.0f);
            Spawn("HIGHFLY/DonorVFX/EnergyExplosion", impact, 0.60f, 1.5f);

            if (target != null && Vector3.Distance(target.transform.position, transform.position) <= 4.25f)
                Hit(target, 44f, 65f, "JUMP SMASH • IMPACT");

            HighflyTimeDilationManager.RequestHitStop(0.07f, 0.07f);
        }

        private IEnumerator Aegis()
        {
            HighflySkillLabMetrics.RecordAction("AEGIS • SUBSPACE SHIELD DONOR", 0);
            HighflyCuratedAegisState.Activate(_stats, 85f, 5.0f);

            GameObject aura = Spawn("HIGHFLY/DonorVFX/ParticlesLight", transform.position + Vector3.up * 0.9f, 0.9f, 5.2f);
            if (aura != null)
                aura.transform.SetParent(transform, true);

            Spawn("HIGHFLY/DonorVFX/EnergyExplosion", transform.position + Vector3.up * 0.7f, 0.45f, 1.1f);
            yield return new WaitForSecondsRealtime(0.1f);
        }
    }
}
