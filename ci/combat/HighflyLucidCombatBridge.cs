using UnityEngine;
using Highfly.SkillLab;

namespace Highfly.Combat
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed class HighflyLucidCombatBridge : MonoBehaviour
    {
        [Header("Prototype skill cooldowns")]
        [SerializeField] private float skill2Cooldown = 1.25f;
        [SerializeField] private float skill3Cooldown = 3.0f;
        [SerializeField] private float ultimateCooldown = 8.0f;

        private PlayerController _player;
        private CharacterController _characterController;
        private HighflyCombatCore _core;

        private float _lastSkill2 = -99f;
        private float _lastSkill3 = -99f;
        private float _lastUltimate = -99f;

        private readonly Collider[] _hits = new Collider[32];

        private void Awake()
        {
            _player = GetComponent<PlayerController>();
            _characterController = GetComponent<CharacterController>();
            _core = GetComponent<HighflyCombatCore>();

            if (_core == null)
                _core = gameObject.AddComponent<HighflyCombatCore>();
        }

        public void Request(HighflyCombatAction action)
        {
            if (_player == null) return;

            _core?.Buffer(action);

            if (HighflySkillLabMode.IsActive && HighflyPremiumSkillRuntime.Instance != null)
            {
                switch (action)
                {
                    case HighflyCombatAction.Light:
                        _player.HighflyMobileAttack();
                        HighflyPremiumSkillRuntime.Instance.EchoBasicAttack();
                        break;

                    case HighflyCombatAction.Dodge:
                        _player.HighflyMobileRoll();
                        break;

                    case HighflyCombatAction.Parry:
                        _player.HighflyMobileParry();
                        break;

                    case HighflyCombatAction.Skill1:
                        HighflyPremiumSkillRuntime.Instance.Trigger(HighflySkillLoadout.Get(0));
                        break;

                    case HighflyCombatAction.Skill2:
                        HighflyPremiumSkillRuntime.Instance.Trigger(HighflySkillLoadout.Get(1));
                        break;

                    case HighflyCombatAction.Skill3:
                        HighflyPremiumSkillRuntime.Instance.Trigger(HighflySkillLoadout.Get(2));
                        break;

                    case HighflyCombatAction.Skill4:
                        HighflyPremiumSkillRuntime.Instance.Trigger(HighflySkillLoadout.Get(3));
                        break;

                    case HighflyCombatAction.Ultimate:
                        HighflyPremiumSkillRuntime.Instance.Trigger(HighflySkillLoadout.Get(4));
                        break;
                }
            }
            else
            {
                switch (action)
                {
                    case HighflyCombatAction.Light:
                        _player.HighflyMobileAttack();
                        break;
                    case HighflyCombatAction.Dodge:
                        _player.HighflyMobileRoll();
                        break;
                    case HighflyCombatAction.Parry:
                        _player.HighflyMobileParry();
                        break;
                    case HighflyCombatAction.Skill1:
                        _player.HighflyMobileSkill1();
                        break;
                    case HighflyCombatAction.Skill2:
                        TryDragonStep();
                        break;
                    case HighflyCombatAction.Skill3:
                        TryArcPulse();
                        break;
                    case HighflyCombatAction.Ultimate:
                        TryDragonBurst();
                        break;
                }
            }

            if (_core != null)
            {
                HighflyBufferedAction consumed;
                _core.TryConsumeAny(out consumed);
            }
        }

        private void TryDragonStep()
        {
            if (Time.time < _lastSkill2 + skill2Cooldown) return;
            if (_player.currentState == PlayerState.Die ||
                _player.currentState == PlayerState.Interact ||
                _player.currentState == PlayerState.UseItem)
                return;

            _lastSkill2 = Time.time;

            Vector3 direction = GetDesiredMoveDirection();
            SpawnPulse(new Color(0.15f, 0.85f, 1f, 1f), 1.3f, 18);

            if (_characterController != null)
                _characterController.Move(direction * 2.6f);

            if (direction.sqrMagnitude > 0.001f)
                HighflyCombatFacingV027.FaceVisual(direction);

            SpawnPulse(new Color(0.38f, 0.45f, 1f, 1f), 1.0f, 14);
        }

        private void TryArcPulse()
        {
            if (Time.time < _lastSkill3 + skill3Cooldown) return;
            if (_player.currentState == PlayerState.Die ||
                _player.currentState == PlayerState.Interact)
                return;

            _lastSkill3 = Time.time;
            const float radius = 3.25f;

            SpawnPulse(new Color(0.10f, 0.75f, 1f, 1f), radius, 34);
            DamageEnemies(radius, 22f, 25f);
        }

        private void TryDragonBurst()
        {
            if (Time.time < _lastUltimate + ultimateCooldown) return;
            if (_player.currentState == PlayerState.Die ||
                _player.currentState == PlayerState.Interact)
                return;

            _lastUltimate = Time.time;
            const float radius = 5.2f;

            SpawnPulse(new Color(0.58f, 0.20f, 1f, 1f), radius, 58);
            DamageEnemies(radius, 55f, 60f);
        }

        private Vector3 GetDesiredMoveDirection()
        {
            Vector2 input = _player.HighflyMobileMoveInput;

            if (input.sqrMagnitude > 0.025f && _player.cameraTransform != null)
            {
                Vector3 forward = _player.cameraTransform.forward;
                Vector3 right = _player.cameraTransform.right;
                forward.y = 0f;
                right.y = 0f;
                forward.Normalize();
                right.Normalize();

                Vector3 world = forward * input.y + right * input.x;
                if (world.sqrMagnitude > 0.001f)
                    return world.normalized;
            }

            return HighflyCombatFacingV027.Forward(transform);
        }

        private void DamageEnemies(float radius, float damage, float composureDamage)
        {
            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                radius,
                _hits,
                ~0,
                QueryTriggerInteraction.Collide);

            for (int i = 0; i < count; i++)
            {
                Collider hit = _hits[i];
                if (hit == null) continue;

                CharacterStats stats = hit.GetComponentInParent<CharacterStats>();
                if (stats == null || !stats.CompareTag("Enemy")) continue;

                bool duplicate = false;
                for (int j = 0; j < i; j++)
                {
                    Collider previous = _hits[j];
                    if (previous == null) continue;
                    CharacterStats previousStats = previous.GetComponentInParent<CharacterStats>();
                    if (previousStats == stats)
                    {
                        duplicate = true;
                        break;
                    }
                }

                if (!duplicate)
                    stats.TakeDamage(damage, composureDamage, transform);
            }
        }

        private void SpawnPulse(Color color, float radius, int count)
        {
            var go = new GameObject("HIGHFLY_SKILL_PULSE");
            go.transform.position = transform.position + Vector3.up * 0.7f;

            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.loop = false;
            main.duration = 0.35f;
            main.startLifetime = 0.28f;
            main.startSpeed = Mathf.Max(2f, radius * 3.0f);
            main.startSize = Mathf.Clamp(radius * 0.08f, 0.12f, 0.42f);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)Mathf.Clamp(count, 1, 120))
            });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.22f;

            ps.Play();
            Destroy(go, 1.2f);
        }
    }
}
