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
        SigilFireball,

        PxVortexEdge,
        PxVeilStrike,
        PxBurstArrow,
        PxHunterClaw,
        PxTitanSwing,
        PxVitalDrain,
        PxOverchargeDomain,
        PxPlasmaGuard,
        PxBarrage,
        PxHuntDrone,
        PxRailShot,
        PxAegisForm,

        AshForcePush,
        AshForcePull,
        AshHasteDomain,
        AshSlowDomain,

        SubEmberBolt,
        SubThunderMark,
        SubFrostLance,
        SubMeteorBreak,
        SubAegis,
        SubHeal,

        AdaptiveFocusSpecial,
        AdaptiveExecution,
        AdaptiveHyperArmorHeavy
    }

    // LAB-only defensive state shared with PlayerStats.Mobile.cs.
    // This keeps shield/hyper-armour mechanics real without replacing HIGHFLY CORE.
    public static class HighflyDonorDefenseState
    {
        private static PlayerStats _owner;
        private static float _shieldHp;
        private static float _shieldUntil;
        private static float _hyperArmorUntil;

        public static void ActivateShield(PlayerStats owner, float hp, float seconds)
        {
            _owner = owner;
            _shieldHp = Mathf.Max(1f, hp);
            _shieldUntil = Time.unscaledTime + Mathf.Max(0.1f, seconds);
        }

        public static void ActivateHyperArmor(PlayerStats owner, float seconds)
        {
            _owner = owner;
            _hyperArmorUntil = Mathf.Max(_hyperArmorUntil, Time.unscaledTime + Mathf.Max(0.1f, seconds));
        }

        public static bool TryModifyIncoming(PlayerStats owner, ref float damage, ref float composureDamage)
        {
            if (owner == null || owner != _owner) return false;

            if (Time.unscaledTime <= _hyperArmorUntil)
                composureDamage = 0f;

            if (Time.unscaledTime <= _shieldUntil && _shieldHp > 0f && damage > 0f)
            {
                float absorbed = Mathf.Min(_shieldHp, damage);
                _shieldHp -= absorbed;
                damage -= absorbed;
                HighflySkillLabMetrics.RecordAction(
                    "DONOR SHIELD • ABSORBE " + absorbed.ToString("0") +
                    " • RESTA " + Mathf.Max(0f, _shieldHp).ToString("0"),
                    0);
            }

            return damage <= 0.01f;
        }

        public static float ShieldRemaining =>
            Time.unscaledTime <= _shieldUntil ? Mathf.Max(0f, _shieldHp) : 0f;

        public static bool HyperArmorActive => Time.unscaledTime <= _hyperArmorUntil;
    }

    [DisallowMultipleComponent]
    public sealed class HighflyDonorExpandedRuntimeV022 : MonoBehaviour
    {
        public static HighflyDonorExpandedRuntimeV022 Instance { get; private set; }

        private PlayerController _player;
        private PlayerStats _stats;
        private CharacterController _cc;
        private bool _busy;

        private readonly Dictionary<HighflyDonorExpandedSkillV022, float> _readyAt =
            new Dictionary<HighflyDonorExpandedSkillV022, float>();

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
            if (_busy)
            {
                HighflySkillLabMetrics.RecordAction("SKILL VAULT • OCUPADO", 0);
                return;
            }

            float now = Time.unscaledTime;
            if (!HighflySkillLabMode.IsActive &&
                _readyAt.TryGetValue(skill, out float ready) && now < ready)
            {
                HighflySkillLabMetrics.RecordAction(
                    SkillLabel(skill) + " • CD " + (ready - now).ToString("0.0") + "s",
                    0);
                return;
            }

            if (!HighflySkillLabMode.IsActive)
                _readyAt[skill] = now + CooldownFor(skill);

            StartCoroutine(Run(skill));
        }

        private IEnumerator Run(HighflyDonorExpandedSkillV022 skill)
        {
            _busy = true;
            try
            {
                switch (skill)
                {
                    case HighflyDonorExpandedSkillV022.SigilMelee: yield return SigilMelee(); break;
                    case HighflyDonorExpandedSkillV022.SigilRanged: yield return SigilRanged(); break;
                    case HighflyDonorExpandedSkillV022.SigilFlash: yield return SigilFlash(); break;
                    case HighflyDonorExpandedSkillV022.SigilFireball: yield return SigilFireball(); break;

                    case HighflyDonorExpandedSkillV022.PxVortexEdge: yield return VortexEdge(); break;
                    case HighflyDonorExpandedSkillV022.PxVeilStrike: yield return VeilStrike(); break;
                    case HighflyDonorExpandedSkillV022.PxBurstArrow: yield return BurstArrow(); break;
                    case HighflyDonorExpandedSkillV022.PxHunterClaw: yield return HunterClaw(); break;
                    case HighflyDonorExpandedSkillV022.PxTitanSwing: yield return TitanSwing(); break;
                    case HighflyDonorExpandedSkillV022.PxVitalDrain: yield return VitalDrain(); break;
                    case HighflyDonorExpandedSkillV022.PxOverchargeDomain: yield return OverchargeDomain(); break;
                    case HighflyDonorExpandedSkillV022.PxPlasmaGuard: yield return PlasmaGuard(); break;
                    case HighflyDonorExpandedSkillV022.PxBarrage: yield return Barrage(); break;
                    case HighflyDonorExpandedSkillV022.PxHuntDrone: yield return HuntDrone(); break;
                    case HighflyDonorExpandedSkillV022.PxRailShot: yield return RailShot(); break;
                    case HighflyDonorExpandedSkillV022.PxAegisForm: yield return AegisForm(); break;

                    case HighflyDonorExpandedSkillV022.AshForcePush: yield return ForcePush(); break;
                    case HighflyDonorExpandedSkillV022.AshForcePull: yield return ForcePull(); break;
                    case HighflyDonorExpandedSkillV022.AshHasteDomain: yield return HasteDomain(); break;
                    case HighflyDonorExpandedSkillV022.AshSlowDomain: yield return SlowDomain(); break;

                    case HighflyDonorExpandedSkillV022.SubEmberBolt: yield return EmberBolt(); break;
                    case HighflyDonorExpandedSkillV022.SubThunderMark: yield return ThunderMark(); break;
                    case HighflyDonorExpandedSkillV022.SubFrostLance: yield return FrostLance(); break;
                    case HighflyDonorExpandedSkillV022.SubMeteorBreak: yield return MeteorBreak(); break;
                    case HighflyDonorExpandedSkillV022.SubAegis: yield return SubAegis(); break;
                    case HighflyDonorExpandedSkillV022.SubHeal: yield return SubHeal(); break;

                    case HighflyDonorExpandedSkillV022.AdaptiveFocusSpecial: yield return FocusSpecial(); break;
                    case HighflyDonorExpandedSkillV022.AdaptiveExecution: yield return Execution(); break;
                    case HighflyDonorExpandedSkillV022.AdaptiveHyperArmorHeavy: yield return HyperArmorHeavy(); break;
                }
            }
            finally
            {
                _player?.HighflyLabForceLocomotion();
                _busy = false;
            }
        }

        private static string SkillLabel(HighflyDonorExpandedSkillV022 skill)
        {
            return skill.ToString().ToUpperInvariant();
        }

        private static float CooldownFor(HighflyDonorExpandedSkillV022 skill)
        {
            switch (skill)
            {
                case HighflyDonorExpandedSkillV022.SigilMelee: return 0.5f;
                case HighflyDonorExpandedSkillV022.SigilRanged: return 0.4f;
                case HighflyDonorExpandedSkillV022.SigilFlash: return 3f;
                case HighflyDonorExpandedSkillV022.SigilFireball: return 4f;
                case HighflyDonorExpandedSkillV022.PxVortexEdge: return 4f;
                case HighflyDonorExpandedSkillV022.PxVeilStrike: return 5f;
                case HighflyDonorExpandedSkillV022.PxBurstArrow: return 3f;
                case HighflyDonorExpandedSkillV022.PxHunterClaw: return 4f;
                case HighflyDonorExpandedSkillV022.PxTitanSwing: return 4f;
                case HighflyDonorExpandedSkillV022.PxVitalDrain: return 6f;
                case HighflyDonorExpandedSkillV022.PxOverchargeDomain: return 8f;
                case HighflyDonorExpandedSkillV022.PxPlasmaGuard: return 7f;
                case HighflyDonorExpandedSkillV022.PxBarrage: return 3f;
                case HighflyDonorExpandedSkillV022.PxHuntDrone: return 8f;
                case HighflyDonorExpandedSkillV022.PxRailShot: return 4.5f;
                case HighflyDonorExpandedSkillV022.PxAegisForm: return 14f;
                case HighflyDonorExpandedSkillV022.AshForcePush: return 2f;
                case HighflyDonorExpandedSkillV022.AshForcePull: return 2f;
                case HighflyDonorExpandedSkillV022.AshHasteDomain: return 8f;
                case HighflyDonorExpandedSkillV022.AshSlowDomain: return 8f;
                case HighflyDonorExpandedSkillV022.SubEmberBolt: return 1.5f;
                case HighflyDonorExpandedSkillV022.SubThunderMark: return 2f;
                case HighflyDonorExpandedSkillV022.SubFrostLance: return 2f;
                case HighflyDonorExpandedSkillV022.SubMeteorBreak: return 6f;
                case HighflyDonorExpandedSkillV022.SubAegis: return 7f;
                case HighflyDonorExpandedSkillV022.SubHeal: return 8f;
                case HighflyDonorExpandedSkillV022.AdaptiveFocusSpecial: return 5f;
                case HighflyDonorExpandedSkillV022.AdaptiveExecution: return 8f;
                case HighflyDonorExpandedSkillV022.AdaptiveHyperArmorHeavy: return 5f;
                default: return 1f;
            }
        }

        // --------------------------------------------------------------------
        // SIGIL — verified architecture donor. HIGHFLY bridge, no framework swap.
        // --------------------------------------------------------------------

        private IEnumerator SigilMelee()
        {
            FaceNearestTarget(8f);
            HighflySkillLabMetrics.RecordAction("SIGIL • MELEE TRACE • DMG18 / POISE1.5", 1);
            HighflyParkourAnimationV010.Instance?.Play("Sword_Regular_A", 1f, 0.46f);

            var hit = new HashSet<CharacterStats>();
            float end = Time.unscaledTime + 0.30f;
            while (Time.unscaledTime < end)
            {
                Vector3 socket = transform.position + Vector3.up * 1f + transform.forward * 1.2f;
                Collider[] cols = Physics.OverlapSphere(socket, 1f, ~0, QueryTriggerInteraction.Ignore);
                ApplyUniqueHits(cols, hit, 18f, 1.5f, 1f);
                yield return null;
            }

            HighflySkillLabMetrics.RecordAction("SIGIL • MELEE • HITS " + hit.Count, hit.Count);
        }

        private IEnumerator SigilRanged()
        {
            FaceNearestTarget(30f);
            HighflySkillLabMetrics.RecordAction("SIGIL • RANGED • 26m/s", 1);
            HighflyParkourAnimationV010.Instance?.Play("OverhandThrow", 1.35f, 0.42f);
            yield return new WaitForSecondsRealtime(0.12f);

            Vector3 muzzle = transform.position + Vector3.up * 1.4f + transform.forward * 0.6f;
            SpawnProjectile(
                "SIGIL_RANGED",
                muzzle,
                AimDirection(muzzle, 30f),
                26f, 0.35f, 3f, 12f, 1f, 1f,
                new Color(0.25f, 0.72f, 1f, 1f),
                0f);
        }

        private IEnumerator SigilFlash()
        {
            if (_stats != null && !_stats.UseVolition(20f))
            {
                HighflySkillLabMetrics.RecordAction("SIGIL FLASH • VOLITION INSUFICIENTE", 0);
                yield break;
            }

            HighflyParkourAnimationV010.Instance?.Play("Sword_Dash", 1.35f, 0.30f);

            Vector3 dir = ForwardFlat();
            float dist = 5f;
            Vector3 origin = transform.position + Vector3.up;

            if (Physics.SphereCast(origin, 0.4f, dir, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore))
            {
                CharacterStats target = hit.collider != null ? hit.collider.GetComponentInParent<CharacterStats>() : null;
                if (target == null || target == GetComponent<CharacterStats>())
                    dist = Mathf.Max(0f, hit.distance - 0.4f);
            }

            SpawnPulse(transform.position + Vector3.up, 0.65f, new Color(0.25f, 0.75f, 1f, 1f), 0.20f);
            SafeMove(transform.position + dir * dist);
            SpawnPulse(transform.position + Vector3.up, 0.65f, new Color(0.65f, 0.35f, 1f, 1f), 0.20f);
            HighflySkillLabMetrics.RecordAction("SIGIL • FLASH • 5m", 0);
            yield return new WaitForSecondsRealtime(0.10f);
        }

        private IEnumerator SigilFireball()
        {
            if (_stats != null && !_stats.UseVolition(25f))
            {
                HighflySkillLabMetrics.RecordAction("SIGIL FIREBALL • VOLITION INSUFICIENTE", 0);
                yield break;
            }

            FaceNearestTarget(16f);
            HighflySkillLabMetrics.RecordAction("SIGIL • FIREBALL • CARGA 1.2s", 0);
            GameObject charge = SpawnPulse(
                transform.position + Vector3.up * 1.35f + transform.forward * 0.65f,
                0.40f,
                new Color(1f, 0.38f, 0.06f, 1f),
                1.35f);

            float start = Time.unscaledTime;
            while (Time.unscaledTime - start < 1.2f)
            {
                if (charge != null)
                {
                    float u = Mathf.Clamp01((Time.unscaledTime - start) / 1.2f);
                    charge.transform.localScale = Vector3.one * Mathf.Lerp(0.4f, 1.15f, u);
                    charge.transform.position = transform.position + Vector3.up * 1.35f + transform.forward * 0.65f;
                }
                yield return null;
            }

            if (charge != null) Destroy(charge);
            Vector3 muzzle = transform.position + Vector3.up * 1.4f + transform.forward * 0.65f;
            SpawnProjectile(
                "SIGIL_FIREBALL",
                muzzle,
                AimDirection(muzzle, 18f),
                16f, 1.0f, 3f, 55f, 5f, 1f,
                new Color(1f, 0.42f, 0.08f, 1f),
                2.5f);
            HighflySkillLabMetrics.RecordAction("SIGIL • FIREBALL RELEASE • DMG55 / STUN2.5", 1);
        }

        // --------------------------------------------------------------------
        // PROJECT-X MECHANIC STUDY
        // Independent HIGHFLY implementations. No Project-X source/assets copied:
        // the repository lacks a machine-readable root LICENSE in the GitHub API.
        // --------------------------------------------------------------------

        private IEnumerator VortexEdge()
        {
            HighflySkillLabMetrics.RecordAction("PROJECT-X REF • VORTEX EDGE • SPIN AOE", 1);
            float end = Time.unscaledTime + 2f;
            float nextTick = 0f;
            while (Time.unscaledTime < end)
            {
                transform.Rotate(0f, 900f * Time.unscaledDeltaTime, 0f, Space.World);
                if (Time.unscaledTime >= nextTick)
                {
                    nextTick = Time.unscaledTime + 0.25f;
                    AreaDamageAt(transform.position, 3f, 6f, 4f, 0.35f);
                    SpawnRing(transform.position + Vector3.up * 0.15f, 3f, new Color(0.25f, 0.75f, 1f, 0.75f), 0.18f);
                }
                yield return null;
            }
        }

        private IEnumerator VeilStrike()
        {
            CharacterStats target = FindNearestTarget(14f);
            if (target == null)
            {
                HighflySkillLabMetrics.RecordAction("VEIL STRIKE • SIN OBJETIVO", 0);
                yield break;
            }

            Renderer[] rr = GetComponentsInChildren<Renderer>(true);
            bool[] old = new bool[rr.Length];
            for (int i = 0; i < rr.Length; i++)
            {
                old[i] = rr[i] != null && rr[i].enabled;
                if (rr[i] != null) rr[i].enabled = false;
            }

            HighflySkillLabMetrics.RecordAction("PROJECT-X REF • VEIL STRIKE • CLOAK", 0);
            SpawnPulse(transform.position + Vector3.up, 0.8f, new Color(0.45f, 0.15f, 0.8f, 1f), 0.25f);
            yield return new WaitForSecondsRealtime(0.55f);

            Vector3 behind = target.transform.position - target.transform.forward * 1.35f;
            behind.y = transform.position.y;
            SafeMove(behind);

            for (int i = 0; i < rr.Length; i++)
                if (rr[i] != null) rr[i].enabled = old[i];

            Face(target);
            HighflyParkourAnimationV010.Instance?.Play("Sword_Regular_B", 1.35f, 0.40f);
            target.TakeDamage(48f, 28f, transform);
            SpawnImpact(TargetCenter(target), 1.1f, new Color(0.65f, 0.20f, 1f, 1f));
            HighflyTimeDilationManager.RequestHitStop(0.07f, 0.07f);
            HighflySkillLabMetrics.RecordAction("VEIL STRIKE • BACK HIT 48", 1);
        }

        private IEnumerator BurstArrow()
        {
            FaceNearestTarget(24f);
            HighflySkillLabMetrics.RecordAction("PROJECT-X REF • BURST ARROW • EXPLOSIVE PROJECTILE", 0);
            HighflyParkourAnimationV010.Instance?.Play("OverhandThrow", 1.2f, 0.35f);
            yield return new WaitForSecondsRealtime(0.15f);
            Vector3 muzzle = transform.position + Vector3.up * 1.35f + transform.forward * 0.65f;
            yield return ExplosiveProjectile(
                "BURST_ARROW",
                muzzle,
                AimPoint(22f),
                15f,
                40f,
                3f,
                new Color(1f, 0.35f, 0.06f, 1f));
        }

        private IEnumerator HunterClaw()
        {
            CharacterStats target = FindNearestTarget(10f);
            if (target == null)
            {
                HighflySkillLabMetrics.RecordAction("HUNTER CLAW • SIN OBJETIVO", 0);
                yield break;
            }

            Face(target);
            GameObject lineGo = new GameObject("HUNTER_CLAW_LINE");
            LineRenderer line = lineGo.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.startWidth = 0.10f;
            line.endWidth = 0.04f;
            line.sharedMaterial = HighflyLabVisuals.CreateFxMaterial(new Color(0.25f, 0.85f, 1f, 1f));

            Vector3 start = transform.position;
            Vector3 goal = target.transform.position - (target.transform.position - transform.position).normalized * 1.25f;
            goal.y = transform.position.y;

            HighflySkillLabMetrics.RecordAction("PROJECT-X REF • HUNTER CLAW • GRAPPLE", 0);
            float t = 0f;
            while (t < 0.34f)
            {
                t += Time.unscaledDeltaTime;
                line.SetPosition(0, transform.position + Vector3.up * 1.1f);
                line.SetPosition(1, TargetCenter(target));
                SafeMove(Vector3.Lerp(start, goal, Mathf.Clamp01(t / 0.34f)));
                yield return null;
            }

            Destroy(lineGo);
            target.TakeDamage(20f, 12f, transform);
            StartCoroutine(StunTarget(target, 0.5f));
            HighflySkillLabMetrics.RecordAction("HUNTER CLAW • IMPACT + STUN 0.5s", 1);
        }

        private IEnumerator TitanSwing()
        {
            CharacterStats target = FindNearestTarget(5f);
            if (target != null) Face(target);

            HighflySkillLabMetrics.RecordAction("PROJECT-X REF • TITAN SWING • CHARGE", 0);
            HighflyParkourAnimationV010.Instance?.Play("Sword_Heavy_Combo", 0.78f, 0.75f);
            SpawnPulse(transform.position + Vector3.up * 0.9f, 0.5f, new Color(1f, 0.75f, 0.15f, 1f), 0.55f);
            yield return new WaitForSecondsRealtime(0.50f);

            if (target != null)
            {
                target.TakeDamage(36f, 45f, transform);
                Vector3 away = target.transform.position - transform.position;
                ApplyKnockback(target.transform, away, 8f);
                StartCoroutine(StunTarget(target, 1.5f));
                SpawnImpact(TargetCenter(target), 1.5f, new Color(1f, 0.55f, 0.08f, 1f));
            }

            HighflyTimeDilationManager.RequestHitStop(0.09f, 0.05f);
            HighflySkillLabMetrics.RecordAction("TITAN SWING • KNOCKBACK 8 + STUN 1.5", 1);
        }

        private IEnumerator VitalDrain()
        {
            HighflySkillLabMetrics.RecordAction("PROJECT-X REF • VITAL DRAIN • 4 TICKS", 0);
            for (int tick = 0; tick < 4; tick++)
            {
                int hits = AreaDamageAt(transform.position, 4f, 10f, 4f, 0f);
                if (_stats != null && hits > 0)
                    _stats.RestoreEgo(7.5f * hits);
                SpawnRing(transform.position + Vector3.up * 0.15f, 4f, new Color(0.65f, 0.10f, 0.35f, 0.8f), 0.30f);
                yield return new WaitForSecondsRealtime(0.50f);
            }
            HighflySkillLabMetrics.RecordAction("VITAL DRAIN • COMPLETO", 0);
        }

        private IEnumerator OverchargeDomain()
        {
            if (_player == null) yield break;

            float oldMove = _player.moveSpeed;
            float oldSprint = _player.sprintSpeed;
            _player.moveSpeed = oldMove * 1.30f;
            _player.sprintSpeed = oldSprint * 1.30f;

            GameObject field = SpawnField(transform.position, 4f, new Color(0.10f, 0.80f, 1f, 0.25f), 5.2f);
            if (field != null) field.transform.SetParent(transform, true);

            HighflySkillLabMetrics.RecordAction("PROJECT-X REF • OVERCHARGE DOMAIN • MOVE +30%", 0);
            yield return new WaitForSecondsRealtime(5f);

            if (_player != null)
            {
                _player.moveSpeed = oldMove;
                _player.sprintSpeed = oldSprint;
            }
        }

        private IEnumerator PlasmaGuard()
        {
            float hp = 70f;
            HighflyDonorDefenseState.ActivateShield(_stats, hp, 5f);
            GameObject shell = SpawnPulse(transform.position + Vector3.up, 1.25f, new Color(0.15f, 0.75f, 1f, 0.65f), 5.1f);
            if (shell != null) shell.transform.SetParent(transform, true);
            HighflySkillLabMetrics.RecordAction("PROJECT-X REF • PLASMA GUARD • SHIELD 70 / 5s", 0);
            yield return new WaitForSecondsRealtime(0.10f);
        }

        private IEnumerator Barrage()
        {
            FaceNearestTarget(28f);
            HighflySkillLabMetrics.RecordAction("PROJECT-X REF • BARRAGE • 5 SHOTS", 0);
            for (int i = 0; i < 5; i++)
            {
                Vector3 muzzle = transform.position + Vector3.up * 1.35f + transform.forward * 0.65f;
                SpawnProjectile(
                    "BARRAGE_" + i,
                    muzzle,
                    AimDirection(muzzle, 28f),
                    20f, 0.28f, 2.5f, 10f, 1f, 0.3f,
                    new Color(1f, 0.75f, 0.15f, 1f),
                    0f);
                yield return new WaitForSecondsRealtime(0.10f);
            }
        }

        private IEnumerator HuntDrone()
        {
            CharacterStats target = FindNearestTarget(20f);
            if (target == null)
            {
                HighflySkillLabMetrics.RecordAction("HUNT DRONE • SIN OBJETIVO", 0);
                yield break;
            }

            GameObject drone = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            drone.name = "HIGHFLY_HUNT_DRONE";
            Collider dc = drone.GetComponent<Collider>();
            if (dc != null) Destroy(dc);
            drone.transform.position = transform.position + Vector3.up * 2.2f + transform.right * 0.8f;
            drone.transform.localScale = Vector3.one * 0.55f;
            Renderer dr = drone.GetComponent<Renderer>();
            if (dr != null)
                dr.sharedMaterial = HighflyLabVisuals.CreateMaterial(
                    new Color(0.15f, 0.85f, 1f, 1f),
                    new Color(0.05f, 0.60f, 1f, 1f));

            HighflySkillLabMetrics.RecordAction("PROJECT-X REF • HUNT DRONE • SEEK + EXPLODE", 0);

            float end = Time.unscaledTime + 1.4f;
            while (drone != null && target != null && Time.unscaledTime < end)
            {
                drone.transform.position = Vector3.MoveTowards(
                    drone.transform.position,
                    TargetCenter(target),
                    12f * Time.unscaledDeltaTime);

                if (Vector3.Distance(drone.transform.position, TargetCenter(target)) < 0.55f)
                    break;

                yield return null;
            }

            Vector3 p = drone != null ? drone.transform.position : TargetCenter(target);
            if (drone != null) Destroy(drone);
            AreaDamageAt(p, 4f, 80f, 50f, 2.5f);
            SpawnImpact(p, 2.2f, new Color(0.05f, 0.75f, 1f, 1f));
            SpawnResourceFx("HIGHFLY/SkillVFX/PlasmaExplosion", p, 1.1f, 1.6f);
            HighflyTimeDilationManager.RequestHitStop(0.09f, 0.05f);
        }

        private IEnumerator RailShot()
        {
            FaceNearestTarget(35f);
            HighflySkillLabMetrics.RecordAction("PROJECT-X REF • RAIL SHOT • CHARGE 1s", 0);
            GameObject charge = SpawnPulse(
                transform.position + Vector3.up * 1.45f + transform.forward * 0.55f,
                0.30f,
                new Color(0.85f, 0.95f, 1f, 1f),
                1.1f);
            yield return new WaitForSecondsRealtime(1f);
            if (charge != null) Destroy(charge);

            Vector3 muzzle = transform.position + Vector3.up * 1.45f + transform.forward * 0.65f;
            SpawnProjectile(
                "RAIL_SHOT",
                muzzle,
                AimDirection(muzzle, 35f),
                30f, 0.24f, 3f, 72f, 25f, 2f,
                new Color(0.80f, 0.95f, 1f, 1f),
                0.35f);
        }

        private IEnumerator AegisForm()
        {
            if (_stats == null || _player == null) yield break;

            float oldMove = _player.moveSpeed;
            float oldSprint = _player.sprintSpeed;
            Vector3 oldScale = transform.localScale;

            float shield = Mathf.Max(80f, _stats.maxEgo * 0.5f);
            HighflyDonorDefenseState.ActivateShield(_stats, shield, 10f);
            HighflyDonorDefenseState.ActivateHyperArmor(_stats, 10f);

            _player.moveSpeed = oldMove * 0.8f;
            _player.sprintSpeed = oldSprint * 0.8f;
            transform.localScale = oldScale * 1.08f;

            GameObject shell = SpawnPulse(transform.position + Vector3.up, 1.6f, new Color(0.15f, 0.55f, 1f, 0.55f), 10.2f);
            if (shell != null) shell.transform.SetParent(transform, true);

            HighflySkillLabMetrics.RecordAction(
                "PROJECT-X REF • AEGIS FORM • SHIELD50% + HYPER ARMOR / 10s",
                0);

            yield return new WaitForSecondsRealtime(10f);

            if (_player != null)
            {
                _player.moveSpeed = oldMove;
                _player.sprintSpeed = oldSprint;
            }
            transform.localScale = oldScale;
        }

        // --------------------------------------------------------------------
        // ASHWALKER — MIT mechanic donor study.
        // --------------------------------------------------------------------

        private IEnumerator ForcePush()
        {
            CharacterStats target = FindNearestTarget(12f);
            if (target == null) yield break;
            Face(target);
            Vector3 dir = target.transform.position - transform.position;
            target.TakeDamage(10f, 12f, transform);
            ApplyKnockback(target.transform, dir, 5f);
            SpawnRing(TargetCenter(target), 2.2f, new Color(0.35f, 0.85f, 1f, 0.8f), 0.25f);
            HighflySkillLabMetrics.RecordAction("ASHWALKER • FORCE PUSH • 5m", 1);
            yield return new WaitForSecondsRealtime(0.15f);
        }

        private IEnumerator ForcePull()
        {
            CharacterStats target = FindNearestTarget(12f);
            if (target == null) yield break;
            Face(target);
            Vector3 dir = transform.position - target.transform.position;
            ApplyKnockback(target.transform, dir, 5f);
            SpawnRing(TargetCenter(target), 2.2f, new Color(0.75f, 0.25f, 1f, 0.8f), 0.25f);
            HighflySkillLabMetrics.RecordAction("ASHWALKER • FORCE PULL • 5m", 1);
            yield return new WaitForSecondsRealtime(0.15f);
        }

        private IEnumerator HasteDomain()
        {
            if (_player == null) yield break;
            float oldMove = _player.moveSpeed;
            float oldSprint = _player.sprintSpeed;
            _player.moveSpeed = oldMove * 1.50f;
            _player.sprintSpeed = oldSprint * 1.50f;

            GameObject field = SpawnField(transform.position, 5f, new Color(0.95f, 0.80f, 0.12f, 0.25f), 4.2f);
            if (field != null) field.transform.SetParent(transform, true);

            HighflySkillLabMetrics.RecordAction("ASHWALKER • HASTE DOMAIN • MOVE +50% / 4s", 0);
            yield return new WaitForSecondsRealtime(4f);

            if (_player != null)
            {
                _player.moveSpeed = oldMove;
                _player.sprintSpeed = oldSprint;
            }
        }

        private IEnumerator SlowDomain()
        {
            List<Animator> animators = new List<Animator>();
            List<float> oldSpeeds = new List<float>();

            foreach (CharacterStats target in FindTargetsInRadius(transform.position, 5f))
            {
                Animator a = target.GetComponentInChildren<Animator>(true);
                if (a == null) continue;
                animators.Add(a);
                oldSpeeds.Add(a.speed);
                a.speed = Mathf.Max(0.05f, a.speed * 0.35f);
            }

            SpawnField(transform.position, 5f, new Color(0.20f, 0.45f, 1f, 0.28f), 4.2f);
            HighflySkillLabMetrics.RecordAction("ASHWALKER • SLOW DOMAIN • ANIM x0.35 / 4s", 0);
            yield return new WaitForSecondsRealtime(4f);

            for (int i = 0; i < animators.Count; i++)
                if (animators[i] != null) animators[i].speed = oldSpeeds[i];
        }

        // --------------------------------------------------------------------
        // SUBSPACEHUNTER — MIT code sequencing reference; raw SAO/IP assets excluded.
        // --------------------------------------------------------------------

        private IEnumerator EmberBolt()
        {
            FaceNearestTarget(20f);
            Vector3 muzzle = transform.position + Vector3.up * 1.35f + transform.forward * 0.65f;
            SpawnProjectile(
                "SUB_EMBER_BOLT",
                muzzle,
                AimDirection(muzzle, 20f),
                18f, 0.42f, 2.8f, 38f, 14f, 0.8f,
                new Color(1f, 0.32f, 0.05f, 1f),
                0f);
            HighflySkillLabMetrics.RecordAction("SUBSPACE • EMBER BOLT", 1);
            yield return new WaitForSecondsRealtime(0.10f);
        }

        private IEnumerator ThunderMark()
        {
            CharacterStats target = FindNearestTarget(20f);
            if (target == null) yield break;
            Face(target);
            Vector3 p = TargetCenter(target);
            SpawnResourceFx("HIGHFLY/SkillVFX/ElectricalSparks", p + Vector3.up * 0.5f, 1.1f, 1.4f);
            yield return new WaitForSecondsRealtime(0.14f);
            target.TakeDamage(48f, 34f, transform);
            StartCoroutine(StunTarget(target, 0.4f));
            SpawnResourceFx("HIGHFLY/SkillVFX/PlasmaExplosion", p, 0.85f, 1.4f);
            HighflyTimeDilationManager.RequestHitStop(0.06f, 0.08f);
            HighflySkillLabMetrics.RecordAction("SUBSPACE • THUNDER MARK • HIT48", 1);
        }

        private IEnumerator FrostLance()
        {
            FaceNearestTarget(20f);
            Vector3 muzzle = transform.position + Vector3.up * 1.35f + transform.forward * 0.65f;
            SpawnProjectile(
                "SUB_FROST_LANCE",
                muzzle,
                AimDirection(muzzle, 20f),
                19f, 0.36f, 2.8f, 42f, 22f, 0.5f,
                new Color(0.35f, 0.85f, 1f, 1f),
                1.0f);
            HighflySkillLabMetrics.RecordAction("SUBSPACE • FROST LANCE • STUN1s", 1);
            yield return new WaitForSecondsRealtime(0.10f);
        }

        private IEnumerator MeteorBreak()
        {
            CharacterStats target = FindNearestTarget(22f);
            Vector3 impact = target != null ? TargetCenter(target) : transform.position + transform.forward * 8f;
            Vector3 start = impact + Vector3.up * 8f;
            GameObject meteor = SpawnPulse(start, 1.2f, new Color(1f, 0.25f, 0.04f, 1f), 2f);

            HighflySkillLabMetrics.RecordAction("SUBSPACE • METEOR BREAK", 0);
            float t = 0f;
            while (t < 0.65f)
            {
                t += Time.unscaledDeltaTime;
                if (meteor != null)
                {
                    float u = Mathf.Clamp01(t / 0.65f);
                    meteor.transform.position = Vector3.Lerp(start, impact, u * u);
                }
                yield return null;
            }

            if (meteor != null) Destroy(meteor);
            AreaDamageAt(impact, 3.5f, 68f, 55f, 2f);
            SpawnResourceFx("HIGHFLY/AcceptedVFX/EarthShatter", impact, 1.15f, 2.2f);
            SpawnImpact(impact, 2.0f, new Color(1f, 0.25f, 0.02f, 1f));
            HighflyTimeDilationManager.RequestHitStop(0.095f, 0.045f);
        }

        private IEnumerator SubAegis()
        {
            HighflyDonorDefenseState.ActivateShield(_stats, 85f, 5f);
            GameObject shell = SpawnPulse(transform.position + Vector3.up, 1.3f, new Color(0.15f, 0.60f, 1f, 0.62f), 5.2f);
            if (shell != null) shell.transform.SetParent(transform, true);
            SpawnResourceFx("HIGHFLY/SkillVFX/ParticlesLight", transform.position + Vector3.up * 0.9f, 0.9f, 5.1f);
            HighflySkillLabMetrics.RecordAction("SUBSPACE • AEGIS • SHIELD85 / 5s", 0);
            yield return new WaitForSecondsRealtime(0.10f);
        }

        private IEnumerator SubHeal()
        {
            if (_stats != null)
                _stats.RestoreEgo(35f);
            SpawnResourceFx("HIGHFLY/SkillVFX/ParticlesLight", transform.position + Vector3.up, 1.0f, 1.8f);
            SpawnRing(transform.position + Vector3.up * 0.1f, 2.5f, new Color(0.20f, 1f, 0.55f, 0.8f), 0.35f);
            HighflySkillLabMetrics.RecordAction("SUBSPACE • HEAL • +35 EGO", 0);
            yield return new WaitForSecondsRealtime(0.25f);
        }

        // --------------------------------------------------------------------
        // ADAPTIVE BOSS ARENA — MIT combat-system study.
        // These are test buttons for the reusable combat mechanics, not "anime skills".
        // --------------------------------------------------------------------

        private IEnumerator FocusSpecial()
        {
            HighflySkillLabMetrics.RecordAction("ADAPTIVE • FOCUS SPECIAL • FULL-METER PREVIEW", 0);
            HighflyParkourAnimationV010.Instance?.Play("Sword_Heavy_Combo", 1.25f, 0.60f);
            yield return new WaitForSecondsRealtime(0.42f);
            int hits = AreaDamageAt(transform.position + transform.forward * 1.5f, 4f, 72f, 70f, 3f);
            SpawnRing(transform.position + transform.forward * 1.5f, 4f, new Color(0.70f, 0.25f, 1f, 0.9f), 0.35f);
            HighflyTimeDilationManager.RequestHitStop(0.11f, 0.045f);
            HighflySkillLabMetrics.RecordAction("ADAPTIVE • FOCUS SPECIAL • HITS " + hits, hits);
        }

        private IEnumerator Execution()
        {
            CharacterStats target = FindNearestTarget(4f);
            if (target == null)
            {
                HighflySkillLabMetrics.RecordAction("ADAPTIVE EXECUTION • SIN OBJETIVO", 0);
                yield break;
            }

            Face(target);
            HighflyParkourAnimationV010.Instance?.Play("Sword_Heavy_Combo", 0.85f, 0.80f);
            HighflySkillLabMetrics.RecordAction("ADAPTIVE • EXECUTION • PREVIEW", 0);
            yield return new WaitForSecondsRealtime(0.52f);

            target.TakeDamage(120f, 100f, transform);
            SpawnImpact(TargetCenter(target), 1.8f, new Color(0.95f, 0.10f, 0.18f, 1f));
            HighflyTimeDilationManager.RequestHitStop(0.16f, 0.025f);
            HighflySkillLabMetrics.RecordAction("ADAPTIVE • EXECUTION • DMG120", 1);
        }

        private IEnumerator HyperArmorHeavy()
        {
            HighflyDonorDefenseState.ActivateHyperArmor(_stats, 1.25f);
            HighflySkillLabMetrics.RecordAction("ADAPTIVE • HYPER ARMOR HEAVY • 1.25s", 0);
            HighflyParkourAnimationV010.Instance?.Play("Sword_Heavy_Combo", 0.9f, 0.75f);
            yield return new WaitForSecondsRealtime(0.48f);
            AreaDamageAt(transform.position + transform.forward * 1.25f, 2.6f, 55f, 60f, 2.2f);
            SpawnImpact(transform.position + transform.forward * 1.5f + Vector3.up * 0.7f, 1.4f, new Color(1f, 0.50f, 0.10f, 1f));
            HighflyTimeDilationManager.RequestHitStop(0.09f, 0.06f);
        }

        // --------------------------------------------------------------------
        // Shared helpers
        // --------------------------------------------------------------------

        private IEnumerator ExplosiveProjectile(
            string label,
            Vector3 start,
            Vector3 aim,
            float speed,
            float damage,
            float explosionRadius,
            Color color)
        {
            GameObject projectile = CreateProjectile(label, start, 0.45f, color);
            Vector3 direction = aim - start;
            if (direction.sqrMagnitude < 0.001f) direction = transform.forward;
            direction.Normalize();

            float maxLife = 3f;
            float end = Time.unscaledTime + maxLife;
            Vector3 previous = start;
            Vector3 impact = start;

            while (projectile != null && Time.unscaledTime < end)
            {
                float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
                Vector3 next = previous + direction * speed * dt;
                projectile.transform.position = next;
                impact = next;

                Collider[] cols = Physics.OverlapCapsule(previous, next, 0.35f, ~0, QueryTriggerInteraction.Ignore);
                bool touched = false;
                for (int i = 0; i < cols.Length; i++)
                {
                    CharacterStats cs = cols[i] != null ? cols[i].GetComponentInParent<CharacterStats>() : null;
                    if (cs != null && cs != GetComponent<CharacterStats>())
                    {
                        touched = true;
                        break;
                    }
                }

                if (touched || Vector3.Distance(next, aim) < 0.5f)
                    break;

                previous = next;
                yield return null;
            }

            if (projectile != null) Destroy(projectile);
            int hits = AreaDamageAt(impact, explosionRadius, damage, 35f, 2f);
            SpawnImpact(impact, explosionRadius * 0.7f, color);
            SpawnResourceFx("HIGHFLY/SkillVFX/EnergyExplosion", impact, 0.8f, 1.4f);
            HighflySkillLabMetrics.RecordAction(label + " • EXPLOSION • HITS " + hits, hits);
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
            GameObject projectile = CreateProjectile(label, position, radius, color);
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

        private GameObject CreateProjectile(string label, Vector3 position, float radius, Color color)
        {
            return HighflyFinalFxV024.CreateProjectile(label, position, radius, color);
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
                    ApplyKnockback(target.transform, direction, knockback);
                    if (stunSeconds > 0f)
                        StartCoroutine(StunTarget(target, stunSeconds));
                    SpawnImpact(next, Mathf.Max(0.55f, radius), color);
                    Destroy(projectile);
                    HighflySkillLabMetrics.RecordAction(projectile != null ? projectile.name + " • HIT" : "PROJECTILE • HIT", 1);
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

        private int AreaDamageAt(
            Vector3 point,
            float radius,
            float damage,
            float poise,
            float knockback)
        {
            List<CharacterStats> targets = FindTargetsInRadius(point, radius);
            for (int i = 0; i < targets.Count; i++)
            {
                CharacterStats target = targets[i];
                target.TakeDamage(damage, poise, transform);
                Vector3 dir = target.transform.position - point;
                ApplyKnockback(target.transform, dir, knockback);
                HighflySkillLabMetrics.RecordHit(damage);
            }
            return targets.Count;
        }

        private List<CharacterStats> FindTargetsInRadius(Vector3 point, float radius)
        {
            CharacterStats owner = GetComponent<CharacterStats>();
            CharacterStats[] all = Object.FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
            List<CharacterStats> result = new List<CharacterStats>();
            float r2 = radius * radius;

            for (int i = 0; i < all.Length; i++)
            {
                CharacterStats candidate = all[i];
                if (candidate == null || candidate == owner) continue;
                if ((candidate.transform.position - point).sqrMagnitude <= r2)
                    result.Add(candidate);
            }
            return result;
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
                HighflySkillLabMetrics.RecordHit(damage);
            }
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
            if (target != null) Face(target);
        }

        private void Face(CharacterStats target)
        {
            if (target == null) return;
            Vector3 dir = target.transform.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        private Vector3 AimDirection(Vector3 origin, float maxDistance)
        {
            CharacterStats target = FindNearestTarget(maxDistance);
            if (target != null)
            {
                Vector3 to = TargetCenter(target) - origin;
                if (to.sqrMagnitude > 0.0001f) return to.normalized;
            }
            return ForwardFlat();
        }

        private Vector3 AimPoint(float maxDistance)
        {
            CharacterStats target = FindNearestTarget(maxDistance);
            if (target != null) return TargetCenter(target);
            return transform.position + ForwardFlat() * maxDistance + Vector3.up * 0.7f;
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

        private Vector3 ForwardFlat()
        {
            Vector3 dir = transform.forward;
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

        private GameObject SpawnPulse(Vector3 position, float size, Color color, float life)
        {
            return HighflyFinalFxV024.SpawnPulse(position, size, color, life);
        }

        private GameObject SpawnRing(Vector3 position, float radius, Color color, float life)
        {
            return HighflyFinalFxV024.SpawnField(position, radius, color, life);
        }

        private GameObject SpawnField(Vector3 position, float radius, Color color, float life)
        {
            return HighflyFinalFxV024.SpawnField(position, radius, color, life);
        }

        private void SpawnImpact(Vector3 position, float radius, Color color)
        {
            HighflyFinalFxV024.SpawnImpact(position, radius, color);
        }

        private GameObject SpawnResourceFx(string path, Vector3 position, float scale, float life)
        {
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab == null) return null;
            GameObject go = Instantiate(prefab, position, Quaternion.identity);
            go.transform.localScale *= scale;
            Destroy(go, life);
            return go;
        }
    }
}
