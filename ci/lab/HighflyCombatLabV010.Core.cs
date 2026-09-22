using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Highfly.SkillLab
{
    public enum HighflyCombatCategoryV010
    {
        Damage,
        Movement,
        Control,
        Shadow,
        Buff,
        Defense,
        Tactical
    }

    public enum HighflyCombatSkillV010
    {
        TwinDanceReforged = 1,
        DualImpact = 2,
        PileBreaker = 3,
        BoundlessChain = 4,
        DemonStrikeReforged = 5,
        PhantomStep = 6,
        FormulaDrift = 7,
        VoraciousEchoReforged = 8,
        SovereignLift = 9,
        AbyssalShackleReforged = 10,
        SevenSinkerReforged = 11,
        ShadowCallMirror = 12,
        ShadowRelay = 13,
        ShadowCreationBlade = 14,
        VitalPactReforged = 15,
        VitalDomainReforged = 16,
        ReserveArcana = 17,
        ReturnWallReforged = 18,
        MomentSightReactive = 19,
        FutureCutReforged = 20
    }

    [Serializable]
    public sealed class HighflyCombatSkillDefinitionV010
    {
        public HighflyCombatSkillV010 Id;
        public string Name;
        public HighflyCombatCategoryV010 Category;
        public string Role;
        public string SkillExpression;
        public float PreviewSeconds;

        public HighflyCombatSkillDefinitionV010(
            HighflyCombatSkillV010 id,
            string name,
            HighflyCombatCategoryV010 category,
            string role,
            string skillExpression,
            float previewSeconds)
        {
            Id = id;
            Name = name;
            Category = category;
            Role = role;
            SkillExpression = skillExpression;
            PreviewSeconds = previewSeconds;
        }
    }

    public static class HighflyCombatCatalogV010
    {
        public static readonly HighflyCombatSkillDefinitionV010[] All =
        {
            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.TwinDanceReforged, "DANZA GEMELA • REFORGED", HighflyCombatCategoryV010.Damage, "Combo técnico 2→3→5 cortes", "TIMING • dirección • confirmación de impacto", 2.05f),
            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.DualImpact, "IMPACTO DUAL", HighflyCombatCategoryV010.Damage, "Marca + segundo impacto detonante", "TIMING • confirmación", 1.45f),
            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.PileBreaker, "PILE BREAKER", HighflyCombatCategoryV010.Damage, "Carga corta + golpe de ruptura", "CARGA • apuntado • riesgo", 1.65f),
            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.BoundlessChain, "CADENA SIN LÍMITE", HighflyCombatCategoryV010.Damage, "Multicorte encadenado dirigible", "TIMING • dirección • stamina conceptual", 2.50f),
            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.DemonStrikeReforged, "GOLPE DEMONÍACO • REFORGED", HighflyCombatCategoryV010.Damage, "Concentración en brazo + descarga", "DISTANCIA • impacto", 1.60f),

            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.PhantomStep, "PASO FANTASMA", HighflyCombatCategoryV010.Movement, "Gap closer inmediato + corte", "DIRECCIÓN • target", 1.15f),
            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.FormulaDrift, "DRIFT DE FÓRMULA", HighflyCombatCategoryV010.Movement, "Órbita rápida al flanco", "IZQ/DER • posicionamiento", 1.55f),
            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.VoraciousEchoReforged, "ECO VORAZ • REFORGED", HighflyCombatCategoryV010.Movement, "Esquiva lateral + corte retardado", "IZQ/DER • counter", 1.40f),

            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.SovereignLift, "GRAVEDAD CERO • SOVEREIGN LIFT", HighflyCombatCategoryV010.Control, "Launcher telequinético local", "JUGGLE • follow-up aéreo", 2.10f),
            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.AbyssalShackleReforged, "GRILLETE ABISAL • HUNTER ARM", HighflyCombatCategoryV010.Control, "Brazo umbrío agarra y atrae", "CONTROL • posicionamiento", 2.00f),
            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.SevenSinkerReforged, "SEVEN SINKER • REFORGED", HighflyCombatCategoryV010.Control, "Siete armas forman cerco rompible", "COLOCACIÓN • zoning", 2.40f),

            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.ShadowCallMirror, "ECO DE SOMBRA", HighflyCombatCategoryV010.Shadow, "Sombras replican tu ofensiva", "SINCRONÍA • target", 2.40f),
            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.ShadowRelay, "RELEVO UMBRÍO", HighflyCombatCategoryV010.Shadow, "Colocar ancla y volver/intercambiar", "SETUP • reposicionamiento", 1.85f),
            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.ShadowCreationBlade, "CREACIÓN DE SOMBRA • BLADE", HighflyCombatCategoryV010.Shadow, "Sombra del suelo materializa arma", "GEOMETRÍA • proyectil", 1.75f),

            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.VitalPactReforged, "PACTO VITAL • REFORGED", HighflyCombatCategoryV010.Buff, "Sigilo personal luminoso", "RIESGO/RECOMPENSA", 1.60f),
            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.VitalDomainReforged, "DOMINIO VITAL • REFORGED", HighflyCombatCategoryV010.Buff, "Drena enemigos + cura/buff aliados", "COLOCACIÓN • área", 2.20f),
            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.ReserveArcana, "RESERVA ARCANA", HighflyCombatCategoryV010.Buff, "Guardar hechizo y liberarlo luego", "SETUP • timing", 1.90f),

            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.ReturnWallReforged, "MURALLA DE RETORNO", HighflyCombatCategoryV010.Defense, "Muralla arcana con reflect window", "TIMING • dirección", 1.85f),
            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.MomentSightReactive, "VISTA DEL INSTANTE", HighflyCombatCategoryV010.Defense, "Última oportunidad ante daño letal", "REACCIÓN • dodge/parry", 2.10f),

            new HighflyCombatSkillDefinitionV010(HighflyCombatSkillV010.FutureCutReforged, "CORTE FUTURO • REFORGED", HighflyCombatCategoryV010.Tactical, "Marca local + corte retardado", "PREDICCIÓN • zoning", 1.90f)
        };

        public static HighflyCombatSkillDefinitionV010 Get(HighflyCombatSkillV010 id)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Id == id) return All[i];
            return null;
        }
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerController))]
    public sealed partial class HighflyCombatLabV010 : MonoBehaviour
    {
        public static HighflyCombatLabV010 Instance { get; private set; }

        private PlayerController _player;
        private PlayerStats _stats;
        private CharacterController _cc;
        private Animator _animator;
        private readonly Collider[] _hits = new Collider[64];
        private readonly Dictionary<HighflyCombatSkillV010, float> _cooldowns = new Dictionary<HighflyCombatSkillV010, float>();

        private int _twinStage;
        private float _lastTwinTap = -99f;
        private int _perfectTwinLinks;
        private Vector3? _shadowRelayPoint;
        private bool _reserveReady;
        private Vector3 _reservePoint;
        private bool _momentArmed;
        private float _momentArmedUntil;
        private bool _momentPending;
        private bool _momentEscaped;
        private Vector3 _momentStartPosition;

        private const float TwinMinWindow = 0.17f;
        private const float TwinPerfectMin = 0.24f;
        private const float TwinPerfectMax = 0.43f;
        private const float TwinReset = 0.78f;

        public bool MomentSightArmed => _momentArmed && Time.unscaledTime <= _momentArmedUntil;

        private void Awake()
        {
            Instance = this;
            _player = GetComponent<PlayerController>();
            _stats = GetComponent<PlayerStats>();
            _cc = GetComponent<CharacterController>();
            _animator = _player != null ? _player.animator : GetComponentInChildren<Animator>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (_momentArmed && Time.unscaledTime > _momentArmedUntil)
                _momentArmed = false;

            if (_twinStage > 0 && Time.unscaledTime - _lastTwinTap > TwinReset)
            {
                _twinStage = 0;
                _perfectTwinLinks = 0;
            }
        }

        public void ForcePreview(HighflyCombatSkillV010 id)
        {
            _cooldowns[id] = -99f;
            StopAllCoroutines();
            HighflyTimeDilationManager.ForceReset();
            StartCoroutine(PreviewRoutine(id));
        }

        public void TriggerManual(HighflyCombatSkillV010 id)
        {
            if (id == HighflyCombatSkillV010.TwinDanceReforged)
            {
                TriggerTwinDanceTap();
                return;
            }

            if (id == HighflyCombatSkillV010.ReserveArcana)
            {
                TriggerReserveArcana();
                return;
            }

            StartCoroutine(ExecuteOnce(id));
        }

        private IEnumerator PreviewRoutine(HighflyCombatSkillV010 id)
        {
            FaceTarget();
            yield return new WaitForSecondsRealtime(0.05f);

            switch (id)
            {
                case HighflyCombatSkillV010.TwinDanceReforged:
                    _twinStage = 0;
                    _perfectTwinLinks = 0;
                    TriggerTwinDanceTap();
                    yield return new WaitForSecondsRealtime(0.31f);
                    TriggerTwinDanceTap();
                    yield return new WaitForSecondsRealtime(0.33f);
                    TriggerTwinDanceTap();
                    break;
                case HighflyCombatSkillV010.BoundlessChain:
                    yield return StartCoroutine(BoundlessChainRoutine(true));
                    break;
                case HighflyCombatSkillV010.ShadowCallMirror:
                    yield return StartCoroutine(ShadowCallMirrorRoutine());
                    break;
                case HighflyCombatSkillV010.ShadowRelay:
                    _cooldowns[id] = -99f;
                    yield return StartCoroutine(ExecuteOnce(id));
                    yield return new WaitForSecondsRealtime(0.42f);
                    _cooldowns[id] = -99f;
                    yield return StartCoroutine(ExecuteOnce(id));
                    break;
                case HighflyCombatSkillV010.ReserveArcana:
                    _reserveReady = false;
                    TriggerReserveArcana();
                    yield return new WaitForSecondsRealtime(0.58f);
                    TriggerReserveArcana();
                    break;
                case HighflyCombatSkillV010.MomentSightReactive:
                    ArmMomentSight(true);
                    yield return new WaitForSecondsRealtime(0.52f);
                    StartCoroutine(MomentSightDemoThreat());
                    break;
                default:
                    yield return StartCoroutine(ExecuteOnce(id));
                    break;
            }
        }

        private IEnumerator ExecuteOnce(HighflyCombatSkillV010 id)
        {
            if (!CanUse(id)) yield break;
            BeginCooldown(id, CooldownFor(id));

            switch (id)
            {
                case HighflyCombatSkillV010.DualImpact: yield return StartCoroutine(DualImpactRoutine()); break;
                case HighflyCombatSkillV010.PileBreaker: yield return StartCoroutine(PileBreakerRoutine()); break;
                case HighflyCombatSkillV010.BoundlessChain: yield return StartCoroutine(BoundlessChainRoutine(false)); break;
                case HighflyCombatSkillV010.DemonStrikeReforged: yield return StartCoroutine(DemonStrikeRoutine()); break;
                case HighflyCombatSkillV010.PhantomStep: yield return StartCoroutine(PhantomStepRoutine()); break;
                case HighflyCombatSkillV010.FormulaDrift: yield return StartCoroutine(FormulaDriftRoutine()); break;
                case HighflyCombatSkillV010.VoraciousEchoReforged: yield return StartCoroutine(VoraciousEchoRoutine()); break;
                case HighflyCombatSkillV010.SovereignLift: yield return StartCoroutine(SovereignLiftRoutine()); break;
                case HighflyCombatSkillV010.AbyssalShackleReforged: yield return StartCoroutine(AbyssalShackleRoutine()); break;
                case HighflyCombatSkillV010.SevenSinkerReforged: yield return StartCoroutine(SevenSinkerRoutine()); break;
                case HighflyCombatSkillV010.ShadowCallMirror: yield return StartCoroutine(ShadowCallMirrorRoutine()); break;
                case HighflyCombatSkillV010.ShadowRelay: yield return StartCoroutine(ShadowRelayRoutine()); break;
                case HighflyCombatSkillV010.ShadowCreationBlade: yield return StartCoroutine(ShadowCreationBladeRoutine()); break;
                case HighflyCombatSkillV010.VitalPactReforged: yield return StartCoroutine(VitalPactRoutine()); break;
                case HighflyCombatSkillV010.VitalDomainReforged: yield return StartCoroutine(VitalDomainRoutine()); break;
                case HighflyCombatSkillV010.ReturnWallReforged: yield return StartCoroutine(ReturnWallRoutine()); break;
                case HighflyCombatSkillV010.MomentSightReactive: ArmMomentSight(false); break;
                case HighflyCombatSkillV010.FutureCutReforged: yield return StartCoroutine(FutureCutRoutine()); break;
            }
        }

        private bool CanUse(HighflyCombatSkillV010 id)
        {
            if (_player == null || _player.currentState == PlayerState.Die || _player.currentState == PlayerState.Interact)
                return false;
            float until;
            return !_cooldowns.TryGetValue(id, out until) || Time.unscaledTime >= until;
        }

        private void BeginCooldown(HighflyCombatSkillV010 id, float seconds)
        {
            _cooldowns[id] = Time.unscaledTime + Mathf.Max(0.05f, seconds);
        }

        private float CooldownFor(HighflyCombatSkillV010 id)
        {
            switch (id)
            {
                case HighflyCombatSkillV010.PhantomStep: return 1.2f;
                case HighflyCombatSkillV010.FormulaDrift: return 3.5f;
                case HighflyCombatSkillV010.VoraciousEchoReforged: return 3.0f;
                case HighflyCombatSkillV010.DualImpact: return 3.0f;
                case HighflyCombatSkillV010.PileBreaker: return 5.5f;
                case HighflyCombatSkillV010.SovereignLift: return 6.5f;
                case HighflyCombatSkillV010.AbyssalShackleReforged: return 7.5f;
                case HighflyCombatSkillV010.SevenSinkerReforged: return 11f;
                case HighflyCombatSkillV010.ShadowCallMirror: return 10f;
                case HighflyCombatSkillV010.ShadowRelay: return 4f;
                case HighflyCombatSkillV010.VitalPactReforged: return 10f;
                case HighflyCombatSkillV010.VitalDomainReforged: return 14f;
                case HighflyCombatSkillV010.ReturnWallReforged: return 8f;
                case HighflyCombatSkillV010.MomentSightReactive: return 18f;
                case HighflyCombatSkillV010.FutureCutReforged: return 7f;
                default: return 4.5f;
            }
        }

        private CharacterStats Target(float radius = 14f)
        {
            CharacterStats[] all = UnityEngine.Object.FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
            CharacterStats best = null;
            float bestSq = radius * radius;
            for (int i = 0; i < all.Length; i++)
            {
                CharacterStats c = all[i];
                if (c == null || c == _stats) continue;
                Vector3 d = c.transform.position - transform.position;
                d.y = 0f;
                float sq = d.sqrMagnitude;
                if (sq < bestSq)
                {
                    bestSq = sq;
                    best = c;
                }
            }
            return best;
        }

        private void FaceTarget()
        {
            CharacterStats t = Target();
            if (t == null) return;
            Vector3 d = t.transform.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
        }

        private void Deal(CharacterStats target, float damage, string label, float composure = 16f)
        {
            if (target == null || target == _stats) return;
            target.TakeDamage(damage, composure, transform);
            HighflySkillLabMetrics.RecordHit(damage);
            HighflySkillLabMetrics.RecordAction(label, 0);
        }
    }
}
