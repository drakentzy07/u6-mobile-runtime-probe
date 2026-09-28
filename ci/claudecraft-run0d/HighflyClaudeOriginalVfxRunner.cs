using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.ClaudeBridge.Run0D
{
    /// <summary>
    /// Unity port of the tier-0 ClaudeCraft v0.43.3 strike sequencer used by
    /// Lurker's Strike, Craven Thrust, Dirt Nap and Jawcrack.
    /// Source pin: levy-street/world-of-claudecraft@cecebab4...
    ///
    /// This intentionally preserves the donor's procedural primitive anatomy:
    /// release flipbook/halo/sparks, 150ms instant impact, authored strike arc,
    /// impact trail, staggered follow-ups, afterglow, implosion and CC stars.
    /// It does NOT copy ClaudeCraft audio bytes.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class HighflyClaudeOriginalVfxRunner : MonoBehaviour
    {
        public enum Skill
        {
            Ambush,
            Backstab,
            Eviscerate,
            Pummel
        }

        private enum FlipStyle
        {
            Electric,
            Flame,
            Void
        }

        private sealed class StrikeSpec
        {
            public Skill Skill;
            public Color Color;
            public Color Accent;
            public float Power;
            public float Windup;
            public string WindupStyle;
            public string Arc;
            public int Swings;
            public bool Bleed;
            public bool ImpactFlipbook;
            public bool ImpactRing;
            public float ImpactRingAuthored;
            public bool VerticalRing;
            public int Sparks;
            public bool Debris;
            public bool Smoke;
            public bool Blood;
            public string Trail;
            public float Light;
            public bool Finisher;
            public bool Implosion;
            public bool Stars;
            public float Linger;
            public bool ScreenFx;
            public FlipStyle Flip;
        }

        // ClaudeCraft spectacle.ts constants, tier 0 / quality 1.
        private const float ReleaseRingR = 2.5f;
        private const float ReleaseLight = 2.6f;
        private const float ReleaseSparks = 1.8f;
        private const float ReleaseFlipbook = 4.2f;
        private const float ImpactFlipbookScale = 3.4f;
        private const float ImpactSparkCount = 2.0f;
        private const float ImpactSparkPower = 1.5f;
        private const float ImpactLight = 3.6f;
        private const float VerticalRingScale = 2.3f;
        private const float FollowRingScale = 1.9f;
        private const float StrikeArcScale = 2.9f;
        private const float Flipbook2Delay = 0.30f;
        private const float FlipbookHdr = 1.65f;
        private const float PillarRadius = 1.3f;
        private const float PillarHeight = 8.5f;
        private const float PillarDuration = 1.3f;
        private const float LightRange = 12f;
        private const float ImpactShake = 0.30f;
        private const float FinisherShake = 0.45f;
        private const float FinisherWaveR = 6.0f;
        private const float FinisherWaveVR = 4.4f;
        private const float StrikeEchoDelay = 0.32f;
        private const float StrikeAfterglowDuration = 1.3f;
        private const float AfterglowEvery = 0.40f;
        private const float AfterglowSize = 2.2f;
        private const float FlipDuration = 0.55f;
        private const float ImplodeDuration = 0.42f;

        // ClaudeCraft CC_BAND_SPECS.stun.
        private const int StunStarCount = 4;
        private const float StunStarRadius = 0.45f;
        private const float StunStarLift = 0.55f;
        private const float StunStarRate = 2.4f;
        private const float StunStarSize = 0.20f;

        private Material _lineMaterial;
        private Material _quadMaterial;
        private Material _sparkMaterial;
        private Material _smokeMaterial;
        private Texture2D _starTexture;
        private Texture2D _electricAtlas;
        private Texture2D _flameAtlas;
        private Texture2D _voidAtlas;

        private readonly List<GameObject> _owned = new List<GameObject>();

        private static readonly Color BloodParticle = Hex("#a01222");
        private static readonly Color StunYellow = Hex("#ffd700");

        public static HighflyClaudeOriginalVfxRunner Ensure(GameObject owner)
        {
            HighflyClaudeOriginalVfxRunner runner =
                owner.GetComponent<HighflyClaudeOriginalVfxRunner>();
            if (runner == null)
                runner = owner.AddComponent<HighflyClaudeOriginalVfxRunner>();
            return runner;
        }

        private void Awake()
        {
            _lineMaterial = MakeTransparentMaterial();
            _quadMaterial = MakeTransparentMaterial();

            GameObject sparks = Resources.Load<GameObject>("HIGHFLY/ClaudeBridge/SparksEffect");
            GameObject smoke = Resources.Load<GameObject>("HIGHFLY/ClaudeBridge/SmokeEffect");
            _sparkMaterial = CloneParticleMaterial(sparks) ?? MakeTransparentMaterial();
            _smokeMaterial = CloneParticleMaterial(smoke) ?? _sparkMaterial;

            _starTexture = BuildStarTexture(64);
            _electricAtlas = BuildFlipbookAtlas(FlipStyle.Electric);
            _flameAtlas = BuildFlipbookAtlas(FlipStyle.Flame);
            _voidAtlas = BuildFlipbookAtlas(FlipStyle.Void);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _owned.Count; i++)
                if (_owned[i] != null) Destroy(_owned[i]);

            DestroySafe(_lineMaterial);
            DestroySafe(_quadMaterial);
            if (_sparkMaterial != _lineMaterial) DestroySafe(_sparkMaterial);
            if (_smokeMaterial != _sparkMaterial && _smokeMaterial != _lineMaterial)
                DestroySafe(_smokeMaterial);
            DestroySafe(_starTexture);
            DestroySafe(_electricAtlas);
            DestroySafe(_flameAtlas);
            DestroySafe(_voidAtlas);
        }

        public float WindupSeconds(Skill skill)
        {
            return SpecFor(skill).Windup;
        }

        public void Play(Skill skill, Transform caster, CharacterStats target)
        {
            if (caster == null || target == null) return;
            StartCoroutine(Sequence(SpecFor(skill), caster, target));
        }

        private IEnumerator Sequence(StrikeSpec spec, Transform caster, CharacterStats target)
        {
            if (spec.Windup > 0f)
            {
                if (spec.WindupStyle == "vortex")
                    yield return StartCoroutine(WindupVortex(caster, spec.Color, spec.Windup));
                else
                    yield return StartCoroutine(WindupStance(caster, spec.Color, spec.Windup));
            }

            if (caster == null || target == null) yield break;

            Vector3 casterAt = Anchor(caster, 0.58f);
            int releaseCount = Mathf.RoundToInt(10f * spec.Power * ReleaseSparks);
            Burst(casterAt, spec.Color, releaseCount, 1.1f, "sparks");
            PulseLight(casterAt, spec.Color, 3.2f * spec.Power * ReleaseLight, 0.22f, LightRange);

            SpawnFlipbook(
                casterAt + Vector3.up * 0.10f,
                ReleaseFlipbook * spec.Power,
                spec.Color,
                1.35f * FlipbookHdr,
                spec.Flip);

            SpawnRing(
                casterAt,
                ReleaseRingR * spec.Power,
                0.40f,
                Lighten(spec.Color, 0.40f),
                0.10f,
                true);

            if (spec.Implosion)
                StartCoroutine(Implosion(target, spec.Color));

            // ClaudeCraft compressed instant timing: impact = release + 0.15 sec.
            yield return new WaitForSecondsRealtime(0.15f);
            if (target == null) yield break;
            Impact(spec, caster, target);
        }

        private void Impact(StrikeSpec spec, Transform caster, CharacterStats target)
        {
            Vector3 at = Anchor(target.transform, 0.55f);
            Vector3 ground = GroundPoint(at);
            Color accent = spec.Accent;

            float ringScale = RingScale(spec);

            if (spec.ImpactFlipbook)
            {
                SpawnFlipbook(
                    at,
                    2f * spec.Power * ImpactFlipbookScale,
                    spec.Color,
                    (spec.Finisher ? 1.6f : 1.25f) * FlipbookHdr,
                    spec.Flip);

                StartCoroutine(DelayedFlipbook(
                    at + Vector3.up * 0.30f,
                    2f * spec.Power * ImpactFlipbookScale * 0.85f,
                    accent,
                    1.1f * FlipbookHdr,
                    spec.Flip,
                    Flipbook2Delay));
            }

            if (spec.ImpactRing)
            {
                float radius = 2.4f * spec.Power * FollowRingScale * ringScale;
                SpawnRing(ground, radius, 0.70f, spec.Color, 0.10f, false);
                StartCoroutine(DelayedRing(
                    ground,
                    2.6f * ringScale * FollowRingScale,
                    0.65f,
                    accent,
                    0.08f,
                    false,
                    0.18f));
            }

            if (spec.VerticalRing)
            {
                SpawnRing(
                    at + Vector3.up * 0.40f,
                    2.6f * spec.Power * VerticalRingScale,
                    0.60f,
                    accent,
                    0.09f,
                    true);
            }

            int sparkCount = Mathf.Clamp(
                Mathf.RoundToInt(spec.Sparks * spec.Power * ImpactSparkCount),
                4,
                60);
            Burst(at, accent, sparkCount, 1.1f * spec.Power * ImpactSparkPower, "sparks");

            if (spec.Debris)
                Burst(at, spec.Color, Mathf.RoundToInt(14f * spec.Power), 1.2f, "debris");
            if (spec.Smoke)
                Burst(at + Vector3.up * 0.30f, spec.Color, 5, 0.5f, "smoke");
            if (spec.Blood)
                Burst(at, BloodParticle, Mathf.RoundToInt(12f * spec.Power), 0.9f, "blood");

            FlashTarget(target, spec.Color, 0.20f);

            if (spec.Light > 0f)
            {
                PulseLight(
                    at,
                    spec.Color,
                    Mathf.Min(10f, 2.5f * spec.Light * spec.Power * ImpactLight),
                    0.30f,
                    LightRange);
            }

            if (!String.IsNullOrEmpty(spec.Trail))
                SpawnSlash(at, accent, spec.Trail, StrikeArcScale);

            // Archetype strike identity.
            SpawnSlash(at, spec.Color, spec.Arc, StrikeArcScale);
            if (spec.Bleed)
                Burst(at, BloodParticle, 8, 0.7f, "blood");

            float secondDelay = spec.Swings > 1 ? 0.22f : StrikeEchoDelay;
            StartCoroutine(SecondStrike(spec, target, secondDelay));

            if (spec.Finisher)
            {
                SpawnPillar(
                    ground,
                    PillarRadius * spec.Power,
                    PillarHeight,
                    spec.Color,
                    PillarDuration);
                StartCoroutine(FinisherWave(spec, at, ground, 0.12f));
                KickCamera(FinisherShake);
            }
            else if (spec.ScreenFx)
            {
                KickCamera(ImpactShake * Mathf.Min(1.3f, spec.Power));
            }

            StartCoroutine(Afterglow(spec, at, ground));

            if (spec.Stars)
                StartCoroutine(StunStars(target, 4.0f));
        }

        private IEnumerator SecondStrike(StrikeSpec spec, CharacterStats target, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (target == null) yield break;

            Vector3 at = Anchor(target.transform, 0.55f);
            Vector3 ground = GroundPoint(at);
            float rs = RingScale(spec);

            SpawnSlash(at, spec.Color, spec.Arc, StrikeArcScale);
            Burst(at, spec.Color, 18, 1.2f, "sparks");

            // Exact sequencer second contact beat: ground shockwave + vertical halo.
            SpawnRing(
                ground,
                4.2f * rs * FollowRingScale,
                0.75f,
                spec.Accent,
                0.09f,
                false);
            SpawnRing(
                at + Vector3.up * 0.30f,
                2.4f * rs * VerticalRingScale,
                0.50f,
                spec.Accent,
                0.08f,
                true);
            SpawnStarFlash(at, Color.white, 1.6f * spec.Power, 0.22f);
            PulseLight(at, spec.Color, 4.5f * spec.Power, 0.35f, LightRange);
            KickCamera(0.25f);
        }

        private IEnumerator FinisherWave(StrikeSpec spec, Vector3 at, Vector3 ground, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            float rs = RingScale(spec);
            SpawnRing(ground, FinisherWaveR * RingScale(spec), 0.60f, spec.Accent, 0.12f, false);
            SpawnRing(
                at + Vector3.up * 0.60f,
                FinisherWaveVR * rs * FollowRingScale,
                0.55f,
                Color.white,
                0.11f,
                true);
        }

        private IEnumerator DelayedFlipbook(
            Vector3 at,
            float size,
            Color tint,
            float hdr,
            FlipStyle style,
            float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            SpawnFlipbook(at, size, tint, hdr, style);
        }

        private IEnumerator DelayedRing(
            Vector3 at,
            float radius,
            float duration,
            Color color,
            float width,
            bool vertical,
            float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            SpawnRing(at, radius, duration, color, width, vertical);
        }

        private IEnumerator WindupVortex(Transform caster, Color color, float duration)
        {
            const int count = 12;
            GameObject[] motes = new GameObject[count];
            for (int i = 0; i < count; i++)
            {
                motes[i] = MakeBillboard("CLAUDE_WINDUP_VORTEX", color, 0.13f, null);
            }

            float t = 0f;
            while (t < duration && caster != null)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / Mathf.Max(0.01f, duration));
                Vector3 center = Anchor(caster, 0.55f);
                for (int i = 0; i < count; i++)
                {
                    float a = Time.unscaledTime * 7f + i * (Mathf.PI * 2f / count);
                    float r = Mathf.Lerp(1.6f, 0.28f, p);
                    float y = Mathf.Sin(a * 1.7f + i) * Mathf.Lerp(0.55f, 0.10f, p);
                    SetBillboard(motes[i], center + new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
                }
                yield return null;
            }

            for (int i = 0; i < count; i++) ReleaseOwned(motes[i]);
        }

        private IEnumerator WindupStance(Transform caster, Color color, float duration)
        {
            GameObject glow = MakeBillboard("CLAUDE_WINDUP_STANCE", color, 0.35f, null);
            float t = 0f;
            while (t < duration && caster != null)
            {
                t += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(t / Mathf.Max(0.01f, duration));
                Vector3 at = GroundPoint(caster.position) + Vector3.up * (0.18f + 0.08f * p);
                glow.transform.localScale = Vector3.one * Mathf.Lerp(0.35f, 0.9f, p);
                SetBillboard(glow, at);
                if (UnityEngine.Random.value < 0.32f)
                    Burst(at, color, 2, 0.35f, "debris");
                yield return null;
            }
            ReleaseOwned(glow);
        }

        private IEnumerator Implosion(CharacterStats target, Color color)
        {
            if (target == null) yield break;

            const int motes = 8;
            GameObject[] points = new GameObject[motes];
            for (int i = 0; i < motes; i++)
                points[i] = MakeBillboard("CLAUDE_IMPLOSION_MOTE", color, 0.18f, null);

            float t = 0f;
            while (t < ImplodeDuration && target != null)
            {
                t += Time.unscaledDeltaTime;
                float pull = Mathf.Max(0f, 1f - t / ImplodeDuration);
                Vector3 center = Anchor(target.transform, 0.55f);
                for (int k = 0; k < motes; k++)
                {
                    float a = Time.unscaledTime * 7f + k * 0.79f;
                    float r = 0.35f + 2.1f * pull;
                    float y = Mathf.Sin(a * 1.7f + k) * (0.30f + 0.50f * pull);
                    SetBillboard(points[k], center + new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
                }
                yield return null;
            }

            for (int i = 0; i < motes; i++) ReleaseOwned(points[i]);
            KickCamera(0.28f);
        }

        private IEnumerator Afterglow(StrikeSpec spec, Vector3 at, Vector3 ground)
        {
            float t = 0f;
            float next = 0f;
            GameObject dome = MakeBillboard("CLAUDE_STRIKE_AFTERGLOW", spec.Color, AfterglowSize, null);

            while (t < StrikeAfterglowDuration)
            {
                t += Time.unscaledDeltaTime;
                float rem = Mathf.Clamp01(1f - t / StrikeAfterglowDuration);
                if (dome != null)
                {
                    dome.transform.localScale = Vector3.one * AfterglowSize * (0.70f + 0.50f * rem);
                    SetBillboard(dome, at + Vector3.up * 0.25f);
                    SetBillboardAlpha(dome, 0.28f * rem);
                }

                next -= Time.unscaledDeltaTime;
                if (next <= 0f)
                {
                    next = AfterglowEvery;
                    Burst(at + Vector3.up * 0.20f, spec.Accent, 7, 0.55f, "embers");
                    PulseLight(at, spec.Color, 4.5f * rem, AfterglowEvery + 0.15f, LightRange);
                    SpawnRing(ground, 3.2f, 0.50f, spec.Color, 0.055f, false);
                }

                yield return null;
            }

            ReleaseOwned(dome);
        }

        private IEnumerator StunStars(CharacterStats target, float duration)
        {
            GameObject[] stars = new GameObject[StunStarCount];
            for (int i = 0; i < stars.Length; i++)
                stars[i] = MakeBillboard("CLAUDE_JAWCRACK_STAR", StunYellow, StunStarSize, _starTexture);

            float t = 0f;
            while (t < duration && target != null)
            {
                t += Time.unscaledDeltaTime;
                Vector3 head = Anchor(target.transform, 1.0f);
                for (int k = 0; k < stars.Length; k++)
                {
                    float a = Time.unscaledTime * StunStarRate +
                              (k / (float)StunStarCount) * Mathf.PI * 2f;
                    Vector3 p = head + new Vector3(
                        Mathf.Cos(a) * StunStarRadius,
                        StunStarLift,
                        Mathf.Sin(a) * StunStarRadius);
                    SetBillboard(stars[k], p);
                }
                yield return null;
            }

            for (int i = 0; i < stars.Length; i++) ReleaseOwned(stars[i]);
        }

        private void SpawnFlipbook(Vector3 position, float size, Color tint, float hdr, FlipStyle style)
        {
            StartCoroutine(FlipbookRoutine(position, size, tint, hdr, style));
        }

        private IEnumerator FlipbookRoutine(Vector3 position, float size, Color tint, float hdr, FlipStyle style)
        {
            Texture2D atlas = Atlas(style);
            GameObject quad = MakeBillboard("CLAUDE_IMPACT_FLIPBOOK_" + style, tint, size * 0.65f, atlas);
            if (quad == null) yield break;

            Renderer renderer = quad.GetComponent<Renderer>();
            Material mat = renderer != null ? renderer.material : null;
            if (mat != null)
            {
                mat.mainTexture = atlas;
                mat.mainTextureScale = new Vector2(1f / 8f, 1f / 8f);
            }

            float age = 0f;
            while (age < FlipDuration)
            {
                age += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(age / FlipDuration);
                float frame = t * 63f;
                int fi = Mathf.Clamp(Mathf.FloorToInt(frame), 0, 63);
                int col = fi % 8;
                int row = fi / 8;

                if (mat != null)
                {
                    mat.mainTextureOffset = new Vector2(col / 8f, (7 - row) / 8f);
                    float opacity = t > 0.70f ? 1f - (t - 0.70f) / 0.30f : 1f;
                    Color c = tint;
                    c.r = Mathf.Min(1f, c.r * hdr);
                    c.g = Mathf.Min(1f, c.g * hdr);
                    c.b = Mathf.Min(1f, c.b * hdr);
                    c.a = Mathf.Clamp01(opacity);
                    SetMaterialColor(mat, c);
                }

                float grown = size * (0.65f + 0.55f * EaseOutCubic(t));
                grown = BoundQuadSize(position, grown, 0.95f);
                quad.transform.localScale = Vector3.one * grown;
                SetBillboard(quad, position);
                yield return null;
            }

            ReleaseOwned(quad);
        }

        private float BoundQuadSize(Vector3 position, float desired, float fraction)
        {
            Camera cam = Camera.main;
            if (cam == null || cam.orthographic) return desired;

            float distance = Mathf.Max(0.05f, Vector3.Distance(cam.transform.position, position));
            float tan = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f);
            float viewportHeight = 2f * distance * tan;
            return Mathf.Min(desired, viewportHeight * fraction);
        }

        private void SpawnRing(
            Vector3 center,
            float maxRadius,
            float duration,
            Color color,
            float width,
            bool vertical)
        {
            StartCoroutine(RingRoutine(center, maxRadius, duration, color, width, vertical));
        }

        private IEnumerator RingRoutine(
            Vector3 center,
            float maxRadius,
            float duration,
            Color color,
            float width,
            bool vertical)
        {
            GameObject go = Own(new GameObject(vertical ? "CLAUDE_VRING" : "CLAUDE_RING"));
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.material = new Material(_lineMaterial);
            lr.useWorldSpace = true;
            lr.loop = true;
            lr.positionCount = 48;
            lr.numCornerVertices = 2;
            lr.numCapVertices = 2;

            float age = 0f;
            while (age < duration)
            {
                age += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(age / Mathf.Max(0.01f, duration));
                float radius = maxRadius * EaseOutCubic(t);
                float alpha = Mathf.Pow(1f - t, 0.75f);
                Color c = color;
                c.a = alpha;
                lr.startColor = c;
                lr.endColor = c;
                lr.startWidth = width * Mathf.Lerp(1.5f, 0.55f, t);
                lr.endWidth = lr.startWidth;

                Vector3 right;
                Vector3 up;
                if (vertical)
                {
                    Camera cam = Camera.main;
                    right = cam != null ? cam.transform.right : Vector3.right;
                    up = Vector3.up;
                }
                else
                {
                    right = Vector3.right;
                    up = Vector3.forward;
                }

                for (int i = 0; i < 48; i++)
                {
                    float a = (i / 48f) * Mathf.PI * 2f;
                    lr.SetPosition(i, center + right * Mathf.Cos(a) * radius + up * Mathf.Sin(a) * radius);
                }

                yield return null;
            }

            Destroy(lr.material);
            ReleaseOwned(go);
        }

        private void SpawnSlash(Vector3 at, Color color, string style, float scale)
        {
            if (style == "x" || style == "cross")
            {
                SlashOne(at, color, 1.05f * scale, 0.26f, 1.1f, 0.10f, 0.30f * SlashWidthScale(scale));
                SlashOne(at, color, 1.05f * scale, 0.30f, -1.1f, 0.10f, 0.30f * SlashWidthScale(scale));
                return;
            }

            float span;
            float life;
            float tilt;
            float lift;
            float width;
            float w = SlashWidthScale(scale);

            switch (style)
            {
                case "vertical":
                case "overhead":
                    span = 0.35f * scale;
                    life = 0.26f;
                    tilt = 3.2f;
                    lift = 0.55f * scale;
                    width = 0.30f * w;
                    break;
                case "uppercut":
                    span = 0.50f * scale;
                    life = 0.26f;
                    tilt = -2.6f;
                    lift = 0.50f * scale;
                    width = 0.30f * w;
                    break;
                case "thrust":
                    span = 1.50f * scale;
                    life = 0.18f;
                    tilt = 0.05f;
                    lift = 0f;
                    width = 0.12f * w;
                    break;
                default:
                    span = 1.15f * scale;
                    life = 0.22f;
                    tilt = 0f;
                    lift = 0f;
                    width = 0.34f * w;
                    break;
            }

            SlashOne(at, color, span, life, tilt, lift, width);
        }

        private void SlashOne(
            Vector3 at,
            Color color,
            float span,
            float life,
            float tilt,
            float lift,
            float width)
        {
            StartCoroutine(SlashRoutine(at, color, span, life, tilt, lift, width));
        }

        private IEnumerator SlashRoutine(
            Vector3 at,
            Color color,
            float span,
            float life,
            float tilt,
            float lift,
            float width)
        {
            const int points = 12;
            GameObject go = Own(new GameObject("CLAUDE_SLASH_RIBBON"));
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.material = new Material(_lineMaterial);
            lr.useWorldSpace = true;
            lr.positionCount = points;
            lr.numCapVertices = 4;
            lr.numCornerVertices = 3;
            lr.startWidth = width;
            lr.endWidth = width * 0.65f;

            Camera cam = Camera.main;
            Vector3 camPos = cam != null ? cam.transform.position : at - Vector3.forward * 5f;
            Vector3 ray = at - camPos;
            ray.y = 0f;
            if (ray.sqrMagnitude < 0.001f) ray = Vector3.forward;
            ray.Normalize();
            Vector3 side = new Vector3(-ray.z, 0f, ray.x);

            for (int i = 0; i < points; i++)
            {
                float u = i / (float)(points - 1);
                float swing = (u - 0.5f) * 2f;
                float bowY = Mathf.Sin(u * Mathf.PI) * 0.34f;
                lr.SetPosition(
                    i,
                    at +
                    side * swing * span +
                    Vector3.up * (lift + bowY + swing * tilt * 0.40f));
            }

            float age = 0f;
            while (age < life)
            {
                age += Time.unscaledDeltaTime;
                float a = Mathf.Pow(1f - Mathf.Clamp01(age / life), 0.70f);
                Color c = Color.Lerp(color, Color.white, 0.50f);
                c.a = a;
                lr.startColor = c;
                Color end = color;
                end.a = a * 0.70f;
                lr.endColor = end;
                yield return null;
            }

            Destroy(lr.material);
            ReleaseOwned(go);
        }

        private void SpawnPillar(Vector3 ground, float radius, float height, Color color, float duration)
        {
            StartCoroutine(PillarRoutine(ground, radius, height, color, duration));
        }

        private IEnumerator PillarRoutine(Vector3 ground, float radius, float height, Color color, float duration)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = "CLAUDE_FINISHER_PILLAR";
            Own(go);
            Collider col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer r = go.GetComponent<Renderer>();
            Material mat = new Material(_quadMaterial);
            r.material = mat;

            go.transform.position = ground + Vector3.up * height * 0.5f;
            go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);

            float age = 0f;
            while (age < duration)
            {
                age += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(age / duration);
                Color c = color;
                c.a = 0.22f * Mathf.Pow(1f - t, 0.7f);
                SetMaterialColor(mat, c);
                yield return null;
            }

            Destroy(mat);
            ReleaseOwned(go);
        }

        private void SpawnStarFlash(Vector3 at, Color color, float size, float duration)
        {
            StartCoroutine(StarFlashRoutine(at, color, size, duration));
        }

        private IEnumerator StarFlashRoutine(Vector3 at, Color color, float size, float duration)
        {
            GameObject q = MakeBillboard("CLAUDE_CONTACT_STAR", color, size, _starTexture);
            float age = 0f;
            while (age < duration)
            {
                age += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(age / duration);
                q.transform.localScale = Vector3.one * size * Mathf.Lerp(0.6f, 1.25f, EaseOutCubic(t));
                SetBillboardAlpha(q, 1f - t);
                SetBillboard(q, at);
                yield return null;
            }
            ReleaseOwned(q);
        }

        private void Burst(Vector3 at, Color color, int count, float power, string kind)
        {
            count = Mathf.Clamp(count, 1, 60);
            GameObject go = Own(new GameObject("CLAUDE_BURST_" + kind));
            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.maxParticles = Mathf.Max(64, count + 4);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            bool smoke = kind == "smoke";
            bool debris = kind == "debris";
            bool blood = kind == "blood";
            bool embers = kind == "embers";

            main.startLifetime = smoke
                ? new ParticleSystem.MinMaxCurve(0.45f, 0.75f)
                : new ParticleSystem.MinMaxCurve(0.22f, 0.48f);
            main.startSize = smoke
                ? new ParticleSystem.MinMaxCurve(0.20f, 0.42f)
                : new ParticleSystem.MinMaxCurve(0.045f, embers ? 0.11f : 0.085f);
            main.startColor = color;
            main.gravityModifier = debris || blood ? 0.55f : 0.03f;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.enabled = false;

            ParticleSystemRenderer pr = go.GetComponent<ParticleSystemRenderer>();
            pr.renderMode = ParticleSystemRenderMode.Billboard;
            pr.material = smoke ? _smokeMaterial : _sparkMaterial;

            go.transform.position = at;
            ps.Play(false);

            for (int i = 0; i < count; i++)
            {
                Vector3 dir = UnityEngine.Random.onUnitSphere;
                if (smoke)
                    dir = new Vector3(dir.x * 0.45f, Mathf.Abs(dir.y) * 0.35f + 0.15f, dir.z * 0.45f);
                else if (debris)
                    dir = new Vector3(dir.x, Mathf.Abs(dir.y) * 0.8f + 0.25f, dir.z);
                else if (blood)
                    dir = new Vector3(dir.x, Mathf.Abs(dir.y) * 0.45f + 0.10f, dir.z);

                ParticleSystem.EmitParams ep = new ParticleSystem.EmitParams();
                ep.position = at + UnityEngine.Random.insideUnitSphere * 0.12f;
                ep.velocity = dir.normalized * UnityEngine.Random.Range(0.45f, 1.15f) * power;
                ep.startColor = color;
                ep.startLifetime = smoke ? UnityEngine.Random.Range(0.45f, 0.75f) : UnityEngine.Random.Range(0.22f, 0.48f);
                ep.startSize = smoke ? UnityEngine.Random.Range(0.20f, 0.42f) : UnityEngine.Random.Range(0.045f, 0.10f);
                ps.Emit(ep, 1);
            }

            StartCoroutine(DestroyAfter(go, smoke ? 1.0f : 0.75f));
        }

        private void PulseLight(Vector3 at, Color color, float intensity, float duration, float range)
        {
            StartCoroutine(LightRoutine(at, color, intensity, duration, range));
        }

        private IEnumerator LightRoutine(Vector3 at, Color color, float intensity, float duration, float range)
        {
            GameObject go = Own(new GameObject("CLAUDE_LIGHT_PULSE"));
            Light light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.shadows = LightShadows.None;
            go.transform.position = at;

            float age = 0f;
            while (age < duration)
            {
                age += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(age / Mathf.Max(0.01f, duration));
                light.intensity = intensity * Mathf.Pow(1f - t, 1.3f);
                yield return null;
            }
            ReleaseOwned(go);
        }

        private void FlashTarget(CharacterStats target, Color color, float duration)
        {
            if (target != null) StartCoroutine(TargetFlashRoutine(target, color, duration));
        }

        private IEnumerator TargetFlashRoutine(CharacterStats target, Color color, float duration)
        {
            Renderer[] renderers = target.GetComponentsInChildren<Renderer>(true);
            MaterialPropertyBlock block = new MaterialPropertyBlock();
            int baseColor = Shader.PropertyToID("_BaseColor");
            int colorId = Shader.PropertyToID("_Color");

            float age = 0f;
            while (age < duration && target != null)
            {
                age += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(age / duration);
                Color flash = Color.Lerp(color, Color.white, 0.35f);
                flash.a = Mathf.Lerp(0.75f, 0f, t);

                for (int i = 0; i < renderers.Length; i++)
                {
                    Renderer r = renderers[i];
                    if (r == null) continue;
                    r.GetPropertyBlock(block);
                    if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(baseColor))
                        block.SetColor(baseColor, flash);
                    else
                        block.SetColor(colorId, flash);
                    r.SetPropertyBlock(block);
                }
                yield return null;
            }

            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i] != null) renderers[i].SetPropertyBlock(null);
        }

        private void KickCamera(float amount)
        {
            Camera cam = Camera.main;
            if (cam == null || amount <= 0f) return;
            ClaudeCameraTrauma trauma = cam.GetComponent<ClaudeCameraTrauma>();
            if (trauma == null) trauma = cam.gameObject.AddComponent<ClaudeCameraTrauma>();
            trauma.Kick(amount);
        }

        private GameObject MakeBillboard(
            string name,
            Color color,
            float size,
            Texture texture)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Own(go);
            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            Renderer r = go.GetComponent<Renderer>();
            Material mat = new Material(_quadMaterial);
            if (texture != null) mat.mainTexture = texture;
            SetMaterialColor(mat, color);
            r.material = mat;

            go.transform.localScale = Vector3.one * size;
            return go;
        }

        private void SetBillboard(GameObject go, Vector3 position)
        {
            if (go == null) return;
            go.transform.position = position;
            Camera cam = Camera.main;
            if (cam != null) go.transform.rotation = cam.transform.rotation;
        }

        private static void SetBillboardAlpha(GameObject go, float alpha)
        {
            if (go == null) return;
            Renderer r = go.GetComponent<Renderer>();
            if (r == null || r.material == null) return;
            Color c = r.material.color;
            c.a = Mathf.Clamp01(alpha);
            SetMaterialColor(r.material, c);
        }

        private IEnumerator DestroyAfter(GameObject go, float seconds)
        {
            yield return new WaitForSecondsRealtime(seconds);
            ReleaseOwned(go);
        }

        private GameObject Own(GameObject go)
        {
            if (go != null) _owned.Add(go);
            return go;
        }

        private void ReleaseOwned(GameObject go)
        {
            if (go == null) return;
            _owned.Remove(go);
            Renderer r = go.GetComponent<Renderer>();
            if (r != null && r.material != null &&
                r.material != _lineMaterial &&
                r.material != _quadMaterial &&
                r.material != _sparkMaterial &&
                r.material != _smokeMaterial)
            {
                Destroy(r.material);
            }
            Destroy(go);
        }

        private static void DestroySafe(UnityEngine.Object obj)
        {
            if (obj != null) Destroy(obj);
        }

        private static Material CloneParticleMaterial(GameObject prefab)
        {
            if (prefab == null) return null;
            ParticleSystemRenderer pr = prefab.GetComponentInChildren<ParticleSystemRenderer>(true);
            if (pr == null || pr.sharedMaterial == null) return null;
            return new Material(pr.sharedMaterial);
        }

        private static Material MakeTransparentMaterial()
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Transparent");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader == null) shader = Shader.Find("Standard");
            Material m = new Material(shader);
            SetMaterialColor(m, Color.white);
            return m;
        }

        private static void SetMaterialColor(Material m, Color c)
        {
            if (m == null) return;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            m.color = c;
        }

        private static Color Hex(string value)
        {
            Color c;
            if (ColorUtility.TryParseHtmlString(value, out c)) return c;
            return Color.white;
        }

        private static Color Lighten(Color c, float amount)
        {
            return new Color(
                Mathf.Lerp(c.r, 1f, amount),
                Mathf.Lerp(c.g, 1f, amount),
                Mathf.Lerp(c.b, 1f, amount),
                c.a);
        }

        private static Vector3 Anchor(Transform t, float frac)
        {
            if (t == null) return Vector3.zero;
            Collider c = t.GetComponentInChildren<Collider>();
            if (c != null)
            {
                Bounds b = c.bounds;
                return new Vector3(
                    b.center.x,
                    Mathf.Lerp(b.min.y, b.max.y, Mathf.Clamp01(frac)),
                    b.center.z);
            }
            return t.position + Vector3.up * Mathf.Lerp(0.15f, 1.65f, Mathf.Clamp01(frac));
        }

        private static Vector3 GroundPoint(Vector3 at)
        {
            RaycastHit hit;
            Vector3 origin = at + Vector3.up * 3f;
            if (Physics.Raycast(origin, Vector3.down, out hit, 8f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * 0.025f;
            at.y = 0.025f;
            return at;
        }

        private static float RingScale(StrikeSpec spec)
        {
            float raw = spec.ImpactRingAuthored > 0f ? Mathf.Min(2f, spec.ImpactRingAuthored) : 1f;
            return raw * (0.7f + 0.3f * spec.Power);
        }

        private static float SlashWidthScale(float scale)
        {
            return 0.55f + 0.45f * scale;
        }

        private static float EaseOutCubic(float t)
        {
            t = Mathf.Clamp01(t);
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        private Texture2D Atlas(FlipStyle style)
        {
            switch (style)
            {
                case FlipStyle.Flame: return _flameAtlas;
                case FlipStyle.Void: return _voidAtlas;
                default: return _electricAtlas;
            }
        }

        private static StrikeSpec SpecFor(Skill skill)
        {
            switch (skill)
            {
                case Skill.Ambush:
                    return new StrikeSpec {
                        Skill = skill,
                        Color = Hex("#2e2640"),
                        Accent = Hex("#6242a8"),
                        Power = 1.4f,
                        Windup = 0.5f,
                        WindupStyle = "vortex",
                        Arc = "vertical",
                        Swings = 2,
                        Bleed = false,
                        ImpactFlipbook = false,
                        ImpactRing = false,
                        ImpactRingAuthored = 0f,
                        VerticalRing = false,
                        Sparks = 18,
                        Debris = false,
                        Smoke = true,
                        Blood = true,
                        Trail = "x",
                        Light = 0.35f,
                        Finisher = false,
                        Implosion = true,
                        Stars = false,
                        Linger = 0.6f,
                        ScreenFx = false,
                        Flip = FlipStyle.Void
                    };
                case Skill.Backstab:
                    return new StrikeSpec {
                        Skill = skill,
                        Color = Hex("#8a6ab8"),
                        Accent = Lighten(Hex("#8a6ab8"), 0.40f),
                        Power = 1.0f,
                        Windup = 0f,
                        WindupStyle = "none",
                        Arc = "thrust",
                        Swings = 1,
                        ImpactFlipbook = true,
                        ImpactRing = false,
                        VerticalRing = true,
                        Sparks = 8,
                        Blood = true,
                        Light = 0.6f,
                        Trail = null,
                        ScreenFx = true,
                        Flip = FlipStyle.Void
                    };
                case Skill.Eviscerate:
                    return new StrikeSpec {
                        Skill = skill,
                        Color = Hex("#d41f2e"),
                        Accent = Lighten(Hex("#d41f2e"), 0.40f),
                        Power = 1.45f,
                        Windup = 0.2f,
                        WindupStyle = "stance",
                        Arc = "uppercut",
                        Swings = 2,
                        Bleed = true,
                        ImpactFlipbook = true,
                        ImpactRing = true,
                        ImpactRingAuthored = 0f,
                        VerticalRing = true,
                        Sparks = 36,
                        Debris = true,
                        Smoke = false,
                        Blood = true,
                        Trail = "x",
                        Light = 0.5f,
                        Finisher = true,
                        ScreenFx = true,
                        Flip = FlipStyle.Flame
                    };
                default:
                    return new StrikeSpec {
                        Skill = skill,
                        Color = Hex("#dfe6ee"),
                        Accent = Lighten(Hex("#dfe6ee"), 0.40f),
                        Power = 0.7f,
                        Windup = 0f,
                        WindupStyle = "none",
                        Arc = "uppercut",
                        Swings = 1,
                        ImpactFlipbook = false,
                        ImpactRing = false,
                        VerticalRing = true,
                        Sparks = 10,
                        Debris = false,
                        Smoke = false,
                        Blood = false,
                        Trail = "x",
                        Light = 0.5f,
                        Finisher = false,
                        Stars = true,
                        Linger = 4.2f,
                        ScreenFx = true,
                        Flip = FlipStyle.Electric
                    };
            }
        }

        private static Texture2D BuildStarTexture(int size)
        {
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            Color32[] pixels = new Color32[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float outer = size * 0.46f;
            float inner = outer * 0.43f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    Vector2 p = new Vector2(x, y) - center;
                    float a = Mathf.Atan2(p.y, p.x) + Mathf.PI * 0.5f;
                    float r = p.magnitude;
                    float sector = (a + Mathf.PI * 2f) % (Mathf.PI * 0.4f);
                    float edge = Mathf.Lerp(outer, inner, Mathf.Abs(sector - Mathf.PI * 0.2f) / (Mathf.PI * 0.2f));
                    float alpha = Mathf.Clamp01((edge - r) * 0.45f);
                    byte v = (byte)Mathf.RoundToInt(alpha * 255f);
                    pixels[y * size + x] = new Color32(255, 255, 255, v);
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.name = "CLAUDE_ORIGINAL_STAR";
            return tex;
        }

        private static Texture2D BuildFlipbookAtlas(FlipStyle style)
        {
            const int grid = 8;
            const int cell = 64;
            const int sheet = grid * cell;

            Texture2D tex = new Texture2D(sheet, sheet, TextureFormat.RGBA32, false, true);
            Color32[] pixels = new Color32[sheet * sheet];

            for (int frame = 0; frame < 64; frame++)
            {
                float t = frame / 63f;
                int cellX = frame % grid;
                int cellY = 7 - frame / grid;

                for (int py = 0; py < cell; py++)
                {
                    for (int px = 0; px < cell; px++)
                    {
                        float x = ((px + 0.5f) / cell - 0.5f) * 128f;
                        float y = ((py + 0.5f) / cell - 0.5f) * 128f;
                        float intensity;

                        switch (style)
                        {
                            case FlipStyle.Flame:
                                intensity = FlamePixel(x, y, t, frame);
                                break;
                            case FlipStyle.Void:
                                intensity = VoidPixel(x, y, t);
                                break;
                            default:
                                intensity = ElectricPixel(x, y, t, frame);
                                break;
                        }

                        byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(intensity) * 255f);
                        int sx = cellX * cell + px;
                        int sy = cellY * cell + py;
                        pixels[sy * sheet + sx] = new Color32(255, 255, 255, a);
                    }
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(false, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.name = "CLAUDE_ORIGINAL_FLIP_" + style;
            return tex;
        }

        private static float ElectricPixel(float x, float y, float t, int frame)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float flashA = Mathf.Pow(Mathf.Max(0f, 1f - t * 2.6f), 1.7f);
            float r0 = 8f + 30f * EaseOutCubic(Mathf.Min(1f, t * 3f));
            float flash = flashA * Mathf.Clamp01(1f - r / Mathf.Max(1f, r0));

            float ringR = 8f + 50f * (1f - Mathf.Pow(1f - t, 4f));
            float ringWidth = 6.5f * (1f - t) + 1.2f;
            float ring = Mathf.Exp(-Mathf.Pow((r - ringR) / Mathf.Max(0.6f, ringWidth), 2f)) *
                         Mathf.Pow(1f - t, 1.4f) * 0.9f;

            float theta = Mathf.Atan2(y, x);
            float fil = 0f;
            int n = Mathf.Max(2, Mathf.FloorToInt(11f * (1f - t * 0.7f)));
            for (int k = 0; k < n; k++)
            {
                float baseA = Hash01(frame, k, 11) * Mathf.PI * 2f;
                float wobble = Mathf.Sin(r * 0.18f + k * 2.13f) * 0.20f;
                float da = Mathf.Abs(Mathf.DeltaAngle(theta * Mathf.Rad2Deg, (baseA + wobble) * Mathf.Rad2Deg)) * Mathf.Deg2Rad;
                float reach = ringR * Mathf.Lerp(0.75f, 1.2f, Hash01(frame, k, 17));
                if (r <= reach)
                    fil = Mathf.Max(fil, Mathf.Clamp01(1f - da / 0.035f) * Mathf.Pow(1f - t, 1.2f));
            }

            return Mathf.Clamp01(flash + ring + fil);
        }

        private static float FlamePixel(float x, float y, float t, int frame)
        {
            float value = 0f;
            int n = Mathf.Max(3, Mathf.FloorToInt(12f * (1f - t * 0.5f)));
            float spread = 10f + 34f * EaseOutCubic(t);

            for (int k = 0; k < n; k++)
            {
                float a = Hash01(frame, k, 21) * Mathf.PI * 2f;
                float radial = Mathf.Lerp(0.20f, 1f, Hash01(frame, k, 22));
                float vertical = Mathf.Lerp(0.15f, 0.70f, Hash01(frame, k, 23));
                float cx = Mathf.Cos(a) * spread * radial;
                float cy = Mathf.Sin(a) * spread * vertical -
                           t * 34f * Mathf.Lerp(0.40f, 1f, Hash01(frame, k, 24));
                float rad = Mathf.Lerp(8f, 22f, Hash01(frame, k, 25)) * (1f - t * 0.45f);
                float heat = Mathf.Lerp(0.55f, 1f, Hash01(frame, k, 26));
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                value = Mathf.Max(value, Mathf.Clamp01(1f - d / Mathf.Max(1f, rad)) *
                    Mathf.Pow(1f - t, 1.1f) * heat);
            }

            if (t < 0.3f)
            {
                float r = Mathf.Sqrt(x * x + y * y);
                value = Mathf.Max(value, Mathf.Clamp01(1f - r / 26f) * (1f - t / 0.3f));
            }

            return Mathf.Clamp01(value);
        }

        private static float VoidPixel(float x, float y, float t)
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float ringR = 56f * (1f - EaseOutCubic(t)) + 7f;
            float alpha = Mathf.Pow(Mathf.Min(1f, t * 2.5f), 0.8f) *
                          Mathf.Pow(1f - t, 0.55f);
            float width = 5f * (1f - t) + 1.6f;
            float ring = Mathf.Exp(-Mathf.Pow((r - ringR) / Mathf.Max(0.7f, width), 2f)) * alpha;

            float theta = Mathf.Atan2(y, x);
            float tendril = 0f;
            for (int k = 0; k < 7; k++)
            {
                float a0 = (k / 7f) * Mathf.PI * 2f + t * 2f;
                for (int s = 0; s <= 8; s++)
                {
                    float u = s / 8f;
                    float a = a0 + u * 1.6f;
                    float d = ringR + (1f - u) * 22f;
                    float tx = Mathf.Cos(a) * d;
                    float ty = Mathf.Sin(a) * d;
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(tx, ty));
                    tendril = Mathf.Max(tendril, Mathf.Clamp01(1f - dist / 2.2f) * alpha * 0.70f);
                }
            }

            float pop = 0f;
            if (t > 0.72f)
            {
                float p = (t - 0.72f) / 0.28f;
                float rr = 30f * p + 6f;
                pop = Mathf.Clamp01(1f - r / rr) * (1f - p);
            }

            return Mathf.Clamp01(ring + tendril + pop);
        }

        private static float Hash01(int a, int b, int c)
        {
            unchecked
            {
                uint x = (uint)(a * 73856093 ^ b * 19349663 ^ c * 83492791);
                x ^= x << 13;
                x ^= x >> 17;
                x ^= x << 5;
                return (x & 0x00ffffff) / 16777215f;
            }
        }

        [DefaultExecutionOrder(10000)]
        private sealed class ClaudeCameraTrauma : MonoBehaviour
        {
            private float _trauma;

            public void Kick(float amount)
            {
                _trauma = Mathf.Clamp(_trauma + amount, 0f, 0.55f);
            }

            private void LateUpdate()
            {
                if (_trauma <= 0.001f) return;
                _trauma = Mathf.Max(0f, _trauma - Time.unscaledDeltaTime * 0.8f);

                float amp = _trauma * _trauma;
                float t = Time.unscaledTime * 47f;
                transform.position +=
                    transform.right * Mathf.Sin(t * 1.17f) * 0.045f * amp +
                    transform.up * Mathf.Cos(t * 1.83f) * 0.035f * amp;
                transform.rotation =
                    Quaternion.AngleAxis(Mathf.Sin(t * 1.31f) * 0.65f * amp, transform.forward) *
                    transform.rotation;
            }
        }
    }
}
