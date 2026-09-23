using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.SkillLab
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class HighflyApexPassSkillRuntime : MonoBehaviour
    {
        public static HighflyApexPassSkillRuntime Instance { get; private set; }

        private PlayerController _player;
        private CharacterController _cc;
        private PlayerStats _stats;
        private HighflyPremiumSkillRuntime _premium;

        private readonly Dictionary<HighflyPremiumSkillId, float> _cooldowns =
            new Dictionary<HighflyPremiumSkillId, float>();

        private float _foolUntil;
        private float _scrapPowerUntil;
        private bool _reserveReady;
        private Vector3 _reservePoint;
        private bool _gravityRoutine;
        private bool _momentRoutine;
        private bool _scrapRoutine;
        private bool _beastRoutine;

        public bool FoolActive => Time.unscaledTime < _foolUntil;

        private void Awake()
        {
            Instance = this;
            _player = GetComponent<PlayerController>();
            _cc = GetComponent<CharacterController>();
            _stats = GetComponent<PlayerStats>();
            _premium = GetComponent<HighflyPremiumSkillRuntime>();
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void ForcePreview(HighflyPremiumSkillId id)
        {
            ClearPreviewCooldown(id);

            if (id == HighflyPremiumSkillId.ReserveSpell)
            {
                _reserveReady = false;
                StartCoroutine(PreviewReserveSpell());
                return;
            }

            Trigger(id);
        }

        private IEnumerator PreviewReserveSpell()
        {
            TriggerReserveSpell();
            yield return new WaitForSecondsRealtime(0.48f);
            TriggerReserveSpell();
        }

        public void ClearPreviewCooldown(HighflyPremiumSkillId id)
        {
            _cooldowns[id] = -99f;
        }

        public float GetCooldownRemaining(HighflyPremiumSkillId id)
        {
            float until;
            if (!_cooldowns.TryGetValue(id, out until))
                return 0f;

            return Mathf.Max(0f, until - Time.unscaledTime);
        }

        public void Trigger(HighflyPremiumSkillId id)
        {
            switch (id)
            {
                case HighflyPremiumSkillId.GravityZero: TriggerGravityZero(); break;
                case HighflyPremiumSkillId.MomentSight: TriggerMomentSight(); break;
                case HighflyPremiumSkillId.FormulaDrift: TriggerFormulaDrift(); break;
                case HighflyPremiumSkillId.SevenSinker: TriggerSevenSinker(); break;
                case HighflyPremiumSkillId.ScrapBuild: TriggerScrapBuild(); break;
                case HighflyPremiumSkillId.VictimArts: TriggerVictimArts(); break;
                case HighflyPremiumSkillId.BeastPossession: TriggerBeastPossession(); break;
                case HighflyPremiumSkillId.DemonStrike: TriggerDemonStrike(); break;
                case HighflyPremiumSkillId.SpiritArmament: TriggerSpiritArmament(); break;
                case HighflyPremiumSkillId.ShadowCreation: TriggerShadowCreation(); break;
                case HighflyPremiumSkillId.TemporalCut: TriggerTemporalCut(); break;
                case HighflyPremiumSkillId.MemoryRelease: TriggerMemoryRelease(); break;
                case HighflyPremiumSkillId.BoundlessMassacre: TriggerBoundlessMassacre(); break;
                case HighflyPremiumSkillId.TheFool: TriggerTheFool(); break;
                case HighflyPremiumSkillId.ReserveSpell: TriggerReserveSpell(); break;
            }
        }

        private bool CanUse(HighflyPremiumSkillId id)
        {
            if (_player == null ||
                _player.currentState == PlayerState.Die ||
                _player.currentState == PlayerState.Interact ||
                _player.currentState == PlayerState.UseItem)
                return false;

            if (HighflySkillLabMode.IsActive)
                return true;

            return GetCooldownRemaining(id) <= 0.001f;
        }

        private void BeginCooldown(HighflyPremiumSkillId id, float seconds)
        {
            if (HighflySkillLabMode.IsActive)
                return;

            float factor =
                FoolActive && id != HighflyPremiumSkillId.TheFool
                    ? 0.52f
                    : 1f;

            _cooldowns[id] =
                Time.unscaledTime + Mathf.Max(0.05f, seconds * factor);
        }

        private CharacterStats Target(float radius = 13f, float angle = 240f)
        {
            return _premium != null
                ? _premium.FindBestTarget(radius, angle)
                : null;
        }

        private void Hit(CharacterStats target, float damage, string label, float composure = 14f)
        {
            if (target == null || target == _stats)
                return;

            float finalDamage =
                Time.unscaledTime < _scrapPowerUntil
                    ? damage * 1.42f
                    : damage;

            target.TakeDamage(finalDamage, composure, transform);
            HighflySkillLabMetrics.RecordHit(finalDamage);
            HighflySkillLabMetrics.RecordAction(label, 0);
        }

        // -----------------------------------------------------------------
        // SLF — Gravity Zero: surface-run prototype. Physics trajectory stays
        // authoritative; the visual fantasy is a gravity-vector conversion.
        // -----------------------------------------------------------------
        private void TriggerGravityZero()
        {
            if (!CanUse(HighflyPremiumSkillId.GravityZero) || _gravityRoutine)
                return;

            BeginCooldown(HighflyPremiumSkillId.GravityZero, 6f);
            StartCoroutine(GravityZeroRoutine());
        }

        private IEnumerator GravityZeroRoutine()
        {
            _gravityRoutine = true;
            HighflySkillLabMetrics.RecordAction("APEX • GRAVEDAD CERO", 0);

            Vector3 wallNormal;
            Vector3 wallPoint;
            FindNearbyWall(out wallNormal, out wallPoint);

            Color color = new Color(0.20f, 0.90f, 1f, 1f);
            SpawnRing(transform.position + Vector3.up * 0.12f, color, 1.15f, 0.8f);

            float start = Time.unscaledTime;
            const float duration = 0.82f;

            Vector3 forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.01f) forward = Vector3.forward;
            forward.Normalize();

            Vector3 tangent =
                wallNormal.sqrMagnitude > 0.01f
                    ? Vector3.Cross(Vector3.up, wallNormal).normalized
                    : forward;

            if (Vector3.Dot(tangent, forward) < 0f)
                tangent = -tangent;

            while (Time.unscaledTime - start < duration)
            {
                if (_cc != null && _cc.enabled)
                {
                    float t = (Time.unscaledTime - start) / duration;
                    Vector3 direction =
                        (tangent * 3.2f + Vector3.up * Mathf.Lerp(5.0f, 1.4f, t));

                    _cc.Move(direction * Time.unscaledDeltaTime);
                }

                if (Random.value < 0.22f)
                    HighflyPremiumFx.SpawnAfterImage(transform, color, 0.16f);

                yield return null;
            }

            SpawnRing(transform.position + Vector3.up * 0.2f, color, 1.7f, 0.45f);
            _gravityRoutine = false;
        }

        // -----------------------------------------------------------------
        // SLF — Moment Sight: world slows while player animation and movement
        // are compensated in unscaled time for a perception-speed window.
        // -----------------------------------------------------------------
        private void TriggerMomentSight()
        {
            if (!CanUse(HighflyPremiumSkillId.MomentSight) || _momentRoutine)
                return;

            BeginCooldown(HighflyPremiumSkillId.MomentSight, 9f);
            StartCoroutine(MomentSightRoutine());
        }

        private IEnumerator MomentSightRoutine()
        {
            _momentRoutine = true;
            HighflySkillLabMetrics.RecordAction("APEX • VISTA DEL INSTANTE", 0);

            float oldMove = _player.moveSpeed;
            float oldSprint = _player.sprintSpeed;
            AnimatorUpdateMode oldMode = _player.animator != null
                ? _player.animator.updateMode
                : AnimatorUpdateMode.Normal;

            const float scale = 0.34f;
            _player.moveSpeed = oldMove / scale;
            _player.sprintSpeed = oldSprint / scale;

            if (_player.animator != null)
                _player.animator.updateMode = AnimatorUpdateMode.UnscaledTime;

            HighflyTimeDilationManager.RequestHitStop(1.20f, scale);
            SpawnRing(transform.position + Vector3.up * 1.0f, new Color(0.75f, 0.95f, 1f, 1f), 1.25f, 1.2f);

            yield return new WaitForSecondsRealtime(1.20f);

            _player.moveSpeed = oldMove;
            _player.sprintSpeed = oldSprint;

            if (_player.animator != null)
                _player.animator.updateMode = oldMode;

            _momentRoutine = false;
        }

        // -----------------------------------------------------------------
        // SLF — Formula Drift: target-relative orbit ending on blind side.
        // -----------------------------------------------------------------
        private void TriggerFormulaDrift()
        {
            if (!CanUse(HighflyPremiumSkillId.FormulaDrift))
                return;

            CharacterStats target = Target(11.5f, 300f);
            if (target == null) return;

            BeginCooldown(HighflyPremiumSkillId.FormulaDrift, 3.8f);
            StartCoroutine(FormulaDriftRoutine(target));
        }

        private IEnumerator FormulaDriftRoutine(CharacterStats target)
        {
            if (target == null) yield break;
            HighflySkillLabMetrics.RecordAction("APEX • DRIFT DE FÓRMULA", 0);

            Vector3 startDelta = transform.position - target.transform.position;
            startDelta.y = 0f;
            float radius = Mathf.Clamp(startDelta.magnitude, 2.2f, 4.2f);
            if (startDelta.sqrMagnitude < 0.01f)
                startDelta = -target.transform.forward * radius;
            else
                startDelta = startDelta.normalized * radius;

            float begin = Time.unscaledTime;
            const float duration = 0.54f;
            Color color = new Color(0.24f, 0.82f, 1f, 1f);

            while (target != null && Time.unscaledTime - begin < duration)
            {
                float t = (Time.unscaledTime - begin) / duration;
                Vector3 radial =
                    Quaternion.AngleAxis(Mathf.Lerp(0f, 165f, t), Vector3.up) *
                    startDelta;

                Vector3 pos = target.transform.position + radial;
                pos.y = transform.position.y;
                Warp(pos);

                Vector3 face = target.transform.position - transform.position;
                face.y = 0f;
                if (face.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(face.normalized, Vector3.up);

                if (Random.value < 0.36f)
                    HighflyPremiumFx.SpawnAfterImage(transform, color, 0.13f);

                yield return null;
            }

            _player.HighflyLabPlayMeleePulse();
            Hit(target, 26f, "APEX • DRIFT / BLINDSIDE", 18f);
        }

        // -----------------------------------------------------------------
        // SLF Psyger-100 — Seven Sinker formation.
        // -----------------------------------------------------------------
        private void TriggerSevenSinker()
        {
            if (!CanUse(HighflyPremiumSkillId.SevenSinker))
                return;

            CharacterStats target = Target(14f, 300f);
            if (target == null) return;

            BeginCooldown(HighflyPremiumSkillId.SevenSinker, 12f);
            StartCoroutine(SevenSinkerRoutine(target));
        }

        private IEnumerator SevenSinkerRoutine(CharacterStats target)
        {
            if (target == null) yield break;

            HighflySkillLabMetrics.RecordAction("APEX • SEVEN SINKER", 0);
            var status = EnsureStatus(target);
            status?.ApplyRoot(1.35f);

            Color color = new Color(0.50f, 0.30f, 1f, 1f);
            List<GameObject> blades = new List<GameObject>(7);

            for (int i = 0; i < 7; i++)
            {
                float a = i / 7f * Mathf.PI * 2f;
                Vector3 center = target.transform.position + Vector3.up * 1.0f;
                Vector3 pos = center + new Vector3(Mathf.Cos(a), 0.25f, Mathf.Sin(a)) * 2.2f;
                GameObject blade = SpawnBlade(pos, Quaternion.LookRotation((center - pos).normalized), color, 1.8f, 0.10f);
                blades.Add(blade);
                SpawnLine(pos, center, color, 0.035f, 1.1f);
            }

            yield return new WaitForSecondsRealtime(0.62f);

            if (target != null)
            {
                Vector3 center = target.transform.position + Vector3.up * 0.6f;
                SpawnRing(center, color, 2.4f, 0.52f);
                HighflyPremiumFx.SpawnResource("EnergyExplosion", center, Quaternion.identity, 0.62f, 0.85f, color);
                Hit(target, 52f, "APEX • SEVEN SINKER / COMPRESIÓN", 38f);
                HighflyTimeDilationManager.RequestHitStop(0.065f, 0.05f);
            }
        }

        // -----------------------------------------------------------------
        // Oikatzo-inspired sacrifice engine. LAB simulates the destruction so
        // we can tune the mechanic without touching persistent inventory.
        // -----------------------------------------------------------------
        private void TriggerScrapBuild()
        {
            if (!CanUse(HighflyPremiumSkillId.ScrapBuild) || _scrapRoutine)
                return;

            BeginCooldown(HighflyPremiumSkillId.ScrapBuild, 14f);
            StartCoroutine(ScrapBuildRoutine());
        }

        private IEnumerator ScrapBuildRoutine()
        {
            _scrapRoutine = true;
            HighflySkillLabMetrics.RecordAction("APEX • SCRAP & BUILD / SACRIFICIO SIMULADO", 0);

            float oldMove = _player.moveSpeed;
            float oldSprint = _player.sprintSpeed;

            _player.moveSpeed *= 1.24f;
            _player.sprintSpeed *= 1.30f;
            _scrapPowerUntil = Time.unscaledTime + 5.0f;

            Color color = new Color(1f, 0.52f, 0.12f, 1f);
            SpawnShardBurst(transform.position + Vector3.up * 1.0f, color, 12);
            SpawnRing(transform.position + Vector3.up * 0.2f, color, 1.6f, 0.8f);

            yield return new WaitForSecondsRealtime(5.0f);

            _player.moveSpeed = oldMove;
            _player.sprintSpeed = oldSprint;
            _scrapPowerUntil = 0f;

            _scrapRoutine = false;
        }

        private void TriggerVictimArts()
        {
            if (!CanUse(HighflyPremiumSkillId.VictimArts))
                return;

            BeginCooldown(HighflyPremiumSkillId.VictimArts, 11f);
            StartCoroutine(VictimArtsRoutine());
        }

        private IEnumerator VictimArtsRoutine()
        {
            HighflySkillLabMetrics.RecordAction("APEX • ARTE DEL SACRIFICIO", 0);
            CharacterStats target = Target(13f, 300f);
            Vector3 destination =
                target != null
                    ? target.transform.position + Vector3.up * 0.8f
                    : transform.position + transform.forward * 5.5f + Vector3.up * 0.8f;

            Color color = new Color(1f, 0.22f, 0.10f, 1f);
            GameObject blade = SpawnBlade(transform.position + Vector3.up * 1.0f, transform.rotation, color, 1.2f, 0.12f);

            Vector3 from = blade.transform.position;
            float start = Time.unscaledTime;
            while (blade != null && Time.unscaledTime - start < 0.34f)
            {
                float t = (Time.unscaledTime - start) / 0.34f;
                blade.transform.position = Vector3.Lerp(from, destination, t);
                yield return null;
            }

            if (blade != null) Destroy(blade);
            HighflyPremiumFx.SpawnResource("PlasmaExplosion", destination, Quaternion.identity, 1.15f, 1.05f, color);
            SpawnRing(destination, color, 2.5f, 0.52f);
            DamageArea(destination, 2.8f, 62f, "APEX • VICTIM ARTS / DETONACIÓN");
            HighflyTimeDilationManager.RequestHitStop(0.075f, 0.05f);
        }

        // -----------------------------------------------------------------
        // Solo Leveling Ragnarok — Bond mechanics.
        // -----------------------------------------------------------------
        private void TriggerBeastPossession()
        {
            if (!CanUse(HighflyPremiumSkillId.BeastPossession) || _beastRoutine)
                return;

            BeginCooldown(HighflyPremiumSkillId.BeastPossession, 16f);
            StartCoroutine(BeastPossessionRoutine());
        }

        private IEnumerator BeastPossessionRoutine()
        {
            _beastRoutine = true;
            HighflySkillLabMetrics.RecordAction("APEX • POSESIÓN BESTIAL", 0);

            float oldMove = _player.moveSpeed;
            float oldSprint = _player.sprintSpeed;
            _player.moveSpeed *= 1.35f;
            _player.sprintSpeed *= 1.42f;

            Color color = new Color(0.82f, 0.90f, 1f, 1f);
            SpawnClaws(transform, color, 6.0f);
            SpawnRing(transform.position + Vector3.up * 0.4f, color, 1.8f, 0.7f);

            float end = Time.unscaledTime + 6.0f;
            while (Time.unscaledTime < end)
            {
                if (Random.value < 0.10f)
                    HighflyPremiumFx.SpawnAfterImage(transform, color, 0.16f);
                yield return null;
            }

            _player.moveSpeed = oldMove;
            _player.sprintSpeed = oldSprint;
            _beastRoutine = false;
        }

        private void TriggerDemonStrike()
        {
            if (!CanUse(HighflyPremiumSkillId.DemonStrike))
                return;

            BeginCooldown(HighflyPremiumSkillId.DemonStrike, 7f);
            StartCoroutine(DemonStrikeRoutine());
        }

        private IEnumerator DemonStrikeRoutine()
        {
            HighflySkillLabMetrics.RecordAction("APEX • GOLPE DEMONÍACO / CARGA", 0);
            Color color = new Color(0.95f, 0.08f, 0.06f, 1f);

            for (int i = 0; i < 4; i++)
            {
                SpawnRing(transform.position + transform.forward * 0.7f + Vector3.up * (0.55f + i * 0.18f), color, 0.45f + i * 0.16f, 0.55f);
                yield return new WaitForSecondsRealtime(0.09f);
            }

            _player.HighflyLabPlayMeleePulse();

            CharacterStats target = Target(4.0f, 120f);
            Vector3 impact = transform.position + transform.forward * 1.55f + Vector3.up * 0.9f;
            HighflyPremiumFx.SpawnResource("EnergyExplosion", impact, Quaternion.identity, 0.72f, 0.9f, color);
            SpawnRing(impact, color, 1.8f, 0.45f);

            if (target != null)
                Hit(target, 70f, "APEX • GOLPE DEMONÍACO", 45f);

            HighflyTimeDilationManager.RequestHitStop(0.085f, 0.04f);
        }

        private void TriggerSpiritArmament()
        {
            if (!CanUse(HighflyPremiumSkillId.SpiritArmament))
                return;

            BeginCooldown(HighflyPremiumSkillId.SpiritArmament, 15f);
            StartCoroutine(SpiritArmamentRoutine());
        }

        private IEnumerator SpiritArmamentRoutine()
        {
            HighflySkillLabMetrics.RecordAction("APEX • ARMAMENTO ESPIRITUAL", 0);
            HighflyShadowMinion[] shadows =
                Object.FindObjectsByType<HighflyShadowMinion>(FindObjectsSortMode.None);

            Color color = new Color(0.45f, 0.90f, 1f, 1f);
            List<Vector3> oldScales = new List<Vector3>(shadows.Length);

            for (int i = 0; i < shadows.Length; i++)
            {
                if (shadows[i] == null) continue;
                oldScales.Add(shadows[i].transform.localScale);
                shadows[i].transform.localScale *= 1.18f;
                SpawnRing(shadows[i].transform.position + Vector3.up * 0.2f, color, 1.1f, 0.8f);
                HighflyPremiumFx.SpawnResource("ParticlesLight", shadows[i].transform.position + Vector3.up * 1.0f, Quaternion.identity, 0.48f, 0.9f, color);
            }

            if (shadows.Length == 0)
                SpawnRing(transform.position + Vector3.up * 0.3f, color, 1.5f, 0.8f);

            yield return new WaitForSecondsRealtime(5.0f);

            int restored = 0;
            for (int i = 0; i < shadows.Length; i++)
            {
                if (shadows[i] == null) continue;
                if (restored < oldScales.Count)
                    shadows[i].transform.localScale = oldScales[restored++];
            }
        }

        private void TriggerShadowCreation()
        {
            if (!CanUse(HighflyPremiumSkillId.ShadowCreation))
                return;

            BeginCooldown(HighflyPremiumSkillId.ShadowCreation, 6f);
            StartCoroutine(ShadowCreationRoutine());
        }

        private IEnumerator ShadowCreationRoutine()
        {
            HighflySkillLabMetrics.RecordAction("APEX • CREACIÓN DE SOMBRA", 0);
            CharacterStats target = Target(13f, 300f);
            Color color = new Color(0.22f, 0.03f, 0.42f, 1f);

            Vector3 startPos = transform.position + transform.right * 0.75f + Vector3.up * 1.05f;
            GameObject weapon = SpawnBlade(startPos, transform.rotation, color, 2.0f, 0.16f);
            weapon.transform.localScale *= 1.35f;

            yield return new WaitForSecondsRealtime(0.22f);

            Vector3 end =
                target != null
                    ? target.transform.position + Vector3.up * 0.9f
                    : transform.position + transform.forward * 7f + Vector3.up;

            float start = Time.unscaledTime;
            Vector3 from = weapon.transform.position;
            while (weapon != null && Time.unscaledTime - start < 0.36f)
            {
                float t = (Time.unscaledTime - start) / 0.36f;
                weapon.transform.position = Vector3.Lerp(from, end, t);
                yield return null;
            }

            if (target != null)
                Hit(target, 44f, "APEX • ARMA DE SOMBRA", 24f);

            HighflyPremiumFx.SpawnResource("Sparks", end, Quaternion.identity, 0.42f, 0.55f, new Color(0.62f, 0.18f, 1f, 1f));
        }

        // -----------------------------------------------------------------
        // SAO Alicization — delayed temporal cut + weapon-memory release.
        // -----------------------------------------------------------------
        private void TriggerTemporalCut()
        {
            if (!CanUse(HighflyPremiumSkillId.TemporalCut))
                return;

            CharacterStats target = Target(14f, 320f);
            if (target == null) return;

            BeginCooldown(HighflyPremiumSkillId.TemporalCut, 10f);
            StartCoroutine(TemporalCutRoutine(target));
        }

        private IEnumerator TemporalCutRoutine(CharacterStats target)
        {
            Vector3 mark = target.transform.position;
            HighflySkillLabMetrics.RecordAction("APEX • CORTE FUTURO / MARCA", 0);

            Color color = new Color(0.95f, 0.78f, 0.20f, 1f);
            SpawnRing(mark + Vector3.up * 0.04f, color, 1.25f, 1.1f);
            SpawnLine(mark + Vector3.up * 0.08f, mark + Vector3.up * 2.3f, color, 0.035f, 1.1f);

            yield return new WaitForSecondsRealtime(0.88f);

            HighflyAnimeFx.SpawnBladeScar(mark + Vector3.up * 1.0f, transform.forward, color, 46f, 2.7f, 0.9f);
            HighflyAnimeFx.SpawnBladeScar(mark + Vector3.up * 1.0f, transform.forward, Color.white, -46f, 2.45f, 0.9f);
            DamageArea(mark + Vector3.up * 0.8f, 2.0f, 58f, "APEX • CORTE FUTURO / IMPACTO");
            HighflyTimeDilationManager.RequestHitStop(0.070f, 0.05f);
        }

        private void TriggerMemoryRelease()
        {
            if (!CanUse(HighflyPremiumSkillId.MemoryRelease))
                return;

            BeginCooldown(HighflyPremiumSkillId.MemoryRelease, 18f);
            StartCoroutine(MemoryReleaseRoutine());
        }

        private IEnumerator MemoryReleaseRoutine()
        {
            HighflySkillLabMetrics.RecordAction("APEX • LIBERACIÓN DE MEMORIA", 0);
            CharacterStats target = Target(15f, 320f);
            Vector3 center = transform.position + Vector3.up * 1.0f;
            Color color = new Color(1f, 0.82f, 0.30f, 1f);

            List<GameObject> petals = new List<GameObject>(18);
            for (int i = 0; i < 18; i++)
            {
                float a = i / 18f * Mathf.PI * 2f;
                Vector3 pos = center + new Vector3(Mathf.Cos(a), Mathf.Sin(a * 2f) * 0.45f, Mathf.Sin(a)) * 1.8f;
                petals.Add(SpawnBlade(pos, Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 90f), color, 2.2f, 0.055f));
            }

            SpawnRing(transform.position + Vector3.up * 0.18f, color, 2.0f, 1.0f);
            yield return new WaitForSecondsRealtime(0.55f);

            Vector3 destination =
                target != null
                    ? target.transform.position + Vector3.up * 0.9f
                    : transform.position + transform.forward * 6f + Vector3.up;

            float start = Time.unscaledTime;
            while (Time.unscaledTime - start < 0.42f)
            {
                float t = (Time.unscaledTime - start) / 0.42f;
                for (int i = 0; i < petals.Count; i++)
                {
                    if (petals[i] == null) continue;
                    petals[i].transform.position =
                        Vector3.Lerp(petals[i].transform.position, destination, 0.16f + t * 0.18f);
                }
                yield return null;
            }

            HighflyPremiumFx.SpawnResource("EnergyExplosion", destination, Quaternion.identity, 1.05f, 1.1f, color);
            DamageArea(destination, 3.0f, 78f, "APEX • MEMORY RELEASE");
            HighflyTimeDilationManager.RequestHitStop(0.09f, 0.035f);
        }

        // -----------------------------------------------------------------
        // Continuous assault / rule modifier / reserve casting.
        // -----------------------------------------------------------------
        private void TriggerBoundlessMassacre()
        {
            if (!CanUse(HighflyPremiumSkillId.BoundlessMassacre))
                return;

            CharacterStats target = Target(11f, 300f);
            if (target == null) return;

            BeginCooldown(HighflyPremiumSkillId.BoundlessMassacre, 10f);
            StartCoroutine(BoundlessMassacreRoutine(target));
        }

        private IEnumerator BoundlessMassacreRoutine(CharacterStats target)
        {
            HighflySkillLabMetrics.RecordAction("APEX • MASACRE SIN LÍMITE", 0);
            Color cyan = new Color(0.08f, 0.82f, 1f, 1f);
            Color violet = new Color(0.62f, 0.18f, 1f, 1f);

            for (int i = 0; i < 12 && target != null; i++)
            {
                Vector3 center = target.transform.position + Vector3.up * (0.75f + (i % 3) * 0.18f);
                Vector3 dir = target.transform.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude < 0.001f) dir = transform.forward;

                HighflyAnimeFx.SpawnBladeScar(
                    center,
                    dir.normalized,
                    i % 2 == 0 ? cyan : violet,
                    -58f + i * 19f,
                    1.8f + (i % 4) * 0.24f,
                    0.42f);

                Hit(target, 8f, "APEX • MASACRE " + (i + 1), 5f);
                if (i % 3 == 2)
                    HighflyPremiumFx.SpawnAfterImage(transform, i % 2 == 0 ? cyan : violet, 0.15f);

                yield return new WaitForSecondsRealtime(0.075f);
            }

            if (target != null)
            {
                HighflyAnimeFx.SpawnImpactCross(target.transform.position + Vector3.up, transform.forward, Color.white, 2.8f);
                Hit(target, 30f, "APEX • MASACRE / FINISHER", 30f);
                HighflyTimeDilationManager.RequestHitStop(0.075f, 0.045f);
            }
        }

        private void TriggerTheFool()
        {
            if (!CanUse(HighflyPremiumSkillId.TheFool))
                return;

            BeginCooldown(HighflyPremiumSkillId.TheFool, 20f);
            _foolUntil = Time.unscaledTime + 8.0f;
            HighflySkillLabMetrics.RecordAction("APEX • EL LOCO / RECAST x0.52", 0);

            Color color = new Color(1f, 0.15f, 0.55f, 1f);
            SpawnRing(transform.position + Vector3.up * 0.2f, color, 1.7f, 1.0f);
            SpawnShardBurst(transform.position + Vector3.up * 1.1f, color, 8);
        }

        private void TriggerReserveSpell()
        {
            if (!_reserveReady)
            {
                if (!CanUse(HighflyPremiumSkillId.ReserveSpell))
                    return;

                CharacterStats target = Target(14f, 320f);
                _reservePoint =
                    target != null
                        ? target.transform.position + Vector3.up * 0.8f
                        : transform.position + transform.forward * 6f + Vector3.up * 0.8f;

                _reserveReady = true;
                HighflySkillLabMetrics.RecordAction("APEX • RESERVA ARCANA / ALMACENADA", 0);
                SpawnRing(transform.position + Vector3.up * 1.0f, new Color(0.38f, 0.70f, 1f, 1f), 0.85f, 1.8f);
                return;
            }

            _reserveReady = false;
            BeginCooldown(HighflyPremiumSkillId.ReserveSpell, 5f);
            StartCoroutine(ReleaseReservedSpell(_reservePoint));
        }

        private IEnumerator ReleaseReservedSpell(Vector3 point)
        {
            HighflySkillLabMetrics.RecordAction("APEX • RESERVA ARCANA / LIBERAR", 0);
            Color color = new Color(0.35f, 0.70f, 1f, 1f);

            Vector3 from = transform.position + Vector3.up * 1.25f;
            GameObject orb = SpawnOrb(from, color, 0.34f, 1.4f);

            float start = Time.unscaledTime;
            while (orb != null && Time.unscaledTime - start < 0.36f)
            {
                float t = (Time.unscaledTime - start) / 0.36f;
                orb.transform.position = Vector3.Lerp(from, point, t);
                yield return null;
            }

            if (orb != null) Destroy(orb);
            HighflyPremiumFx.SpawnResource("EnergyExplosion", point, Quaternion.identity, 0.76f, 0.85f, color);
            DamageArea(point, 2.4f, 46f, "APEX • RESERVA ARCANA / IMPACTO");
        }

        private HighflyLabStatusReceiver EnsureStatus(CharacterStats target)
        {
            if (target == null) return null;
            HighflyLabStatusReceiver status = target.GetComponent<HighflyLabStatusReceiver>();
            if (status == null)
                status = target.gameObject.AddComponent<HighflyLabStatusReceiver>();
            return status;
        }

        private void DamageArea(Vector3 center, float radius, float damage, string label)
        {
            CharacterStats[] all =
                Object.FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);

            for (int i = 0; i < all.Length; i++)
            {
                CharacterStats target = all[i];
                if (target == null || target == _stats) continue;

                Vector3 delta = target.transform.position - center;
                if (delta.sqrMagnitude <= radius * radius)
                    Hit(target, damage, label, 24f);
            }
        }

        private void Warp(Vector3 position)
        {
            bool enabled = _cc != null && _cc.enabled;
            if (_cc != null && enabled) _cc.enabled = false;
            transform.position = position;
            if (_cc != null && enabled) _cc.enabled = true;
        }

        private bool FindNearbyWall(out Vector3 normal, out Vector3 point)
        {
            normal = Vector3.zero;
            point = transform.position;

            Vector3 origin = transform.position + Vector3.up * 1.0f;
            Vector3[] directions =
            {
                transform.forward,
                transform.right,
                -transform.right
            };

            for (int i = 0; i < directions.Length; i++)
            {
                RaycastHit hit;
                if (Physics.Raycast(
                    origin,
                    directions[i],
                    out hit,
                    1.8f,
                    ~0,
                    QueryTriggerInteraction.Ignore))
                {
                    if (Mathf.Abs(hit.normal.y) < 0.45f)
                    {
                        normal = hit.normal;
                        point = hit.point;
                        return true;
                    }
                }
            }

            return false;
        }

        private static GameObject SpawnBlade(
            Vector3 position,
            Quaternion rotation,
            Color color,
            float life,
            float thickness)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "HF_APEX_BLADE";
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = new Vector3(thickness, 0.10f, 1.35f);

            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            Material mat = HighflyPremiumFx.CreateTransparentMaterial(
                new Color(color.r, color.g, color.b, 0.86f),
                color * 3.4f);

            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;

            var lifeComp = go.AddComponent<HighflyApexFxLifetime>();
            lifeComp.Initialize(life, mat);
            return go;
        }

        private static GameObject SpawnOrb(Vector3 position, Color color, float scale, float life)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "HF_APEX_ORB";
            go.transform.position = position;
            go.transform.localScale = Vector3.one * scale;

            Collider col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);

            Material mat = HighflyPremiumFx.CreateTransparentMaterial(
                new Color(color.r, color.g, color.b, 0.74f),
                color * 3.2f);

            Renderer r = go.GetComponent<Renderer>();
            if (r != null) r.sharedMaterial = mat;

            go.AddComponent<HighflyApexFxLifetime>().Initialize(life, mat);
            return go;
        }

        private static void SpawnRing(Vector3 position, Color color, float radius, float life)
        {
            GameObject go = new GameObject("HF_APEX_RING");
            go.transform.position = position;

            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.useWorldSpace = false;
            lr.positionCount = 48;
            lr.widthMultiplier = 0.045f;

            Material mat = HighflyPremiumFx.CreateTransparentMaterial(
                new Color(color.r, color.g, color.b, 0.76f),
                color * 3.0f);

            lr.sharedMaterial = mat;
            for (int i = 0; i < 48; i++)
            {
                float a = i / 48f * Mathf.PI * 2f;
                lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius));
            }

            go.AddComponent<HighflyApexFxLifetime>().Initialize(life, mat);
        }

        private static void SpawnLine(Vector3 a, Vector3 b, Color color, float width, float life)
        {
            GameObject go = new GameObject("HF_APEX_LINE");
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.widthMultiplier = width;

            Material mat = HighflyPremiumFx.CreateTransparentMaterial(
                new Color(color.r, color.g, color.b, 0.72f),
                color * 2.8f);

            lr.sharedMaterial = mat;
            lr.SetPosition(0, a);
            lr.SetPosition(1, b);
            go.AddComponent<HighflyApexFxLifetime>().Initialize(life, mat);
        }

        private static void SpawnShardBurst(Vector3 center, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float a = i / (float)Mathf.Max(1, count) * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Cos(a), 0.25f + (i % 3) * 0.22f, Mathf.Sin(a)).normalized;
                GameObject shard = SpawnBlade(center + dir * 0.55f, Quaternion.LookRotation(dir), color, 0.85f, 0.06f);
                shard.transform.localScale = new Vector3(0.06f, 0.06f, 0.48f);
                shard.AddComponent<HighflyApexShardMotion>().Initialize(dir, 2.8f);
            }
        }

        private static void SpawnClaws(Transform owner, Color color, float life)
        {
            if (owner == null) return;

            for (int side = -1; side <= 1; side += 2)
            {
                for (int finger = 0; finger < 3; finger++)
                {
                    Vector3 pos =
                        owner.position +
                        owner.right * (side * (0.34f + finger * 0.06f)) +
                        owner.forward * 0.65f +
                        Vector3.up * (0.75f + finger * 0.08f);

                    GameObject claw = SpawnBlade(
                        pos,
                        Quaternion.LookRotation(owner.forward + Vector3.down * 0.12f),
                        color,
                        life,
                        0.045f);

                    claw.transform.SetParent(owner, true);
                    claw.transform.localScale = new Vector3(0.05f, 0.05f, 0.55f);
                }
            }
        }
    }

    public sealed class HighflyApexFxLifetime : MonoBehaviour
    {
        private float _end;
        private Material _material;

        public void Initialize(float life, Material material)
        {
            _end = Time.unscaledTime + Mathf.Max(0.05f, life);
            _material = material;
        }

        private void Update()
        {
            if (Time.unscaledTime < _end)
                return;

            if (_material != null)
                Destroy(_material);

            Destroy(gameObject);
        }
    }

    public sealed class HighflyApexShardMotion : MonoBehaviour
    {
        private Vector3 _direction;
        private float _speed;

        public void Initialize(Vector3 direction, float speed)
        {
            _direction = direction.normalized;
            _speed = speed;
        }

        private void Update()
        {
            transform.position += _direction * _speed * Time.unscaledDeltaTime;
            _speed = Mathf.MoveTowards(_speed, 0f, Time.unscaledDeltaTime * 4.5f);
        }
    }
}
