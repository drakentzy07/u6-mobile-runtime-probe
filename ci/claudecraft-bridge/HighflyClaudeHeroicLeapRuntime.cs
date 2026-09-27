using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Highfly.Combat;
using Highfly.Run0H;

namespace Highfly.ClaudeBridge.Run0B
{
    [DisallowMultipleComponent]
    public sealed class HighflyClaudeHeroicLeapRuntime : MonoBehaviour
    {
        // ClaudeCraft v0.43.3 canonical gameplay values.
        private const float FlightDuration = 0.60f;
        private const float FlightApex = 3.20f;
        private const float MaxRange = 30f;
        private const float LandingRadius = 6f;
        private const int DamageMin = 24;
        private const int DamageMaxInclusive = 32;
        private const float CanonicalCooldown = 30f;
        private const float SweepStep = 0.5f;

        private PlayerController _player;
        private CharacterController _controller;
        private AudioSource _audio;
        private AudioClip _releaseSfx;
        private AudioClip _impactSfx;
        private GameObject _earthShatter;
        private GameObject _sparks;
        private GameObject _smoke;
        private bool _busy;
        private float _readyAt;
        private string _status = "LISTO";
        private GUIStyle _buttonStyle, _labelStyle;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            if (FindAnyObjectByType<HighflyClaudeHeroicLeapRuntime>() != null) return;
            GameObject root = new GameObject("HIGHFLY_CLAUDECRAFT_RUN0B_HEROIC_LEAP");
            DontDestroyOnLoad(root);
            root.AddComponent<HighflyClaudeHeroicLeapRuntime>();
        }

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            _audio = gameObject.AddComponent<AudioSource>();
            _audio.playOnAwake = false;
            _audio.spatialBlend = 0f;
            _releaseSfx = Resources.Load<AudioClip>("HIGHFLY/Run0I/longSwing");
            _impactSfx = Resources.Load<AudioClip>("HIGHFLY/Run0I/hit");
            _earthShatter = Resources.Load<GameObject>("HIGHFLY/ClaudeBridge/EarthShatter");
            _sparks = Resources.Load<GameObject>("HIGHFLY/ClaudeBridge/SparksEffect");
            _smoke = Resources.Load<GameObject>("HIGHFLY/ClaudeBridge/SmokeEffect");
        }

        private void Update()
        {
            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerController>();
                if (_player != null) _controller = _player.GetComponent<CharacterController>();
            }

            if (Input.GetKeyDown(KeyCode.H)) TryCast();
            if (Input.GetKeyDown(KeyCode.BackQuote)) _readyAt = 0f; // LAB-only cooldown reset.
        }

        private void OnGUI()
        {
            if (_buttonStyle == null)
            {
                _buttonStyle = new GUIStyle(GUI.skin.button) {
                    fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter
                };
                _labelStyle = new GUIStyle(GUI.skin.label) {
                    fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter
                };
                _labelStyle.normal.textColor = Color.white;
            }

            float w = Mathf.Min(320f, Screen.width * 0.28f);
            float h = 66f;
            Rect r = new Rect(Screen.width - w - 18f, Screen.height - h - 98f, w, h);
            float remain = Mathf.Max(0f, _readyAt - Time.unscaledTime);
            string text = _busy ? "CLAUDE LEAP • EN VUELO" :
                          remain > 0f ? "CLAUDE LEAP • CD " + remain.ToString("0.0") :
                          "CLAUDE LEAP";
            GUI.enabled = !_busy && remain <= 0f && _player != null;
            if (GUI.Button(r, text, _buttonStyle)) TryCast();
            GUI.enabled = true;
            GUI.Label(new Rect(r.x, r.y - 27f, r.width, 24f),
                "RUN0B • Heroic Leap v0.43.3 • H en PC", _labelStyle);
            GUI.Label(new Rect(r.x, r.y + r.height + 4f, r.width, 24f), _status, _labelStyle);
        }

        public void TryCast()
        {
            if (_busy || _player == null || Time.unscaledTime < _readyAt) return;
            HighflyLucidCombatBridge combat = HighflyLucidCombatBridge.Instance;
            if (combat != null && combat.ActionBusy)
            {
                _status = "OCUPADO: termina la acción actual";
                return;
            }

            Vector3 from = _player.transform.position;
            Vector3 facing = ResolveFacing();
            Vector3 requested = ResolveAimPoint(from, facing);
            Vector3 landing = SweepLanding(from, requested);
            _readyAt = Time.unscaledTime + CanonicalCooldown;
            StartCoroutine(Leap(from, landing, facing));
        }

        private Vector3 ResolveFacing()
        {
            if (_player.LockOnTarget != null)
            {
                Vector3 to = _player.LockOnTarget.position - _player.transform.position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f) return to.normalized;
            }

            Vector2 stick = _player.HighflyMobileMoveInput;
            if (stick.sqrMagnitude > 0.0225f && _player.cameraTransform != null)
            {
                Vector3 f = _player.cameraTransform.forward; f.y = 0f; f.Normalize();
                Vector3 r = _player.cameraTransform.right; r.y = 0f; r.Normalize();
                Vector3 dir = f * stick.y + r * stick.x;
                if (dir.sqrMagnitude > 0.01f) return dir.normalized;
            }

            Vector3 fallback = _player.transform.forward; fallback.y = 0f;
            return fallback.sqrMagnitude > 0.01f ? fallback.normalized : Vector3.forward;
        }

        private Vector3 ResolveAimPoint(Vector3 from, Vector3 facing)
        {
            if (_player.LockOnTarget != null)
            {
                Vector3 target = _player.LockOnTarget.position;
                Vector3 delta = target - from; delta.y = 0f;
                if (delta.magnitude > MaxRange) target = from + delta.normalized * MaxRange;
                return GroundSeat(target, from.y);
            }

            Camera cam = Camera.main;
            if (cam != null)
            {
                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, MaxRange + 10f, ~0, QueryTriggerInteraction.Ignore))
                {
                    Vector3 delta = hit.point - from; delta.y = 0f;
                    if (delta.sqrMagnitude > 1f)
                    {
                        if (delta.magnitude > MaxRange) delta = delta.normalized * MaxRange;
                        return GroundSeat(from + delta, from.y);
                    }
                }
            }

            return GroundSeat(from + facing * 8f, from.y);
        }

        private Vector3 SweepLanding(Vector3 from, Vector3 requested)
        {
            Vector3 flat = requested - from; flat.y = 0f;
            float distance = Mathf.Min(MaxRange, flat.magnitude);
            if (distance < 0.01f) return GroundSeat(from, from.y);
            Vector3 dir = flat.normalized;
            int steps = Mathf.Max(1, Mathf.CeilToInt(distance / SweepStep));
            Vector3 safe = from;

            for (int i = 1; i <= steps; i++)
            {
                float d = Mathf.Min(distance, i * SweepStep);
                Vector3 probe = GroundSeat(from + dir * d, from.y);
                Vector3 lowA = probe + Vector3.up * 0.25f;
                Vector3 lowB = probe + Vector3.up * 1.45f;
                bool groundedBlocked = Physics.CheckCapsule(lowA, lowB, 0.27f, ~0, QueryTriggerInteraction.Ignore);
                if (groundedBlocked)
                {
                    Vector3 crestA = lowA + Vector3.up * FlightApex;
                    Vector3 crestB = lowB + Vector3.up * FlightApex;
                    if (Physics.CheckCapsule(crestA, crestB, 0.27f, ~0, QueryTriggerInteraction.Ignore))
                        break;
                }
                safe = probe;
            }
            return safe;
        }

        private static Vector3 GroundSeat(Vector3 point, float fallbackY)
        {
            RaycastHit hit;
            Vector3 origin = new Vector3(point.x, fallbackY + 8f, point.z);
            if (Physics.Raycast(origin, Vector3.down, out hit, 20f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * 0.02f;
            point.y = fallbackY;
            return point;
        }

        private IEnumerator Leap(Vector3 from, Vector3 to, Vector3 facing)
        {
            _busy = true;
            _status = "VUELO 0.60s • apex 3.20";
            _player.currentState = PlayerState.Skill;

            HighflyRun0HCharacterVisual visual = HighflyRun0HCharacterVisual.Instance;
            if (visual != null)
            {
                visual.SetActionFacing(facing);
                visual.PlayActionClip("Claude_Warrior_Heroic_Leap", 1f);
            }

            if (_audio != null && _releaseSfx != null) _audio.PlayOneShot(_releaseSfx);

            float elapsed = 0f;
            Vector3 previous = from;
            while (elapsed < FlightDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / FlightDuration);
                Vector3 desired = Vector3.Lerp(from, to, t);
                desired.y += FlightApex * 4f * t * (1f - t);
                Vector3 delta = desired - previous;
                if (_controller != null && _controller.enabled) _controller.Move(delta);
                else _player.transform.position = desired;
                previous = desired;

                if (visual != null)
                    visual.SetWeaponTrail(t >= 0.62f);
                yield return null;
            }

            if (_controller != null && _controller.enabled)
                _controller.Move(to - _player.transform.position);
            else
                _player.transform.position = to;

            if (visual != null) visual.SetWeaponTrail(false);
            SpawnLandingVfx(to);
            ApplyLandingDamage(to);
            if (_audio != null && _impactSfx != null) _audio.PlayOneShot(_impactSfx);

            _status = "IMPACTO • AoE 6 • daño 24–32";
            yield return new WaitForSecondsRealtime(0.55f); // donor clip recovery tail to 1.15 s.

            if (visual != null)
            {
                visual.StopActionClip();
                visual.ClearActionFacing();
            }
            if (_player.currentState == PlayerState.Skill) _player.currentState = PlayerState.Locomotion;
            _busy = false;
        }

        private void ApplyLandingDamage(Vector3 center)
        {
            Collider[] hits = Physics.OverlapSphere(center, LandingRadius, ~0, QueryTriggerInteraction.Collide);
            HashSet<CharacterStats> seen = new HashSet<CharacterStats>();
            PlayerStats self = _player.GetComponent<PlayerStats>();
            foreach (Collider c in hits)
            {
                if (c == null) continue;
                CharacterStats stats = c.GetComponentInParent<CharacterStats>();
                if (stats == null || stats == self || !seen.Add(stats)) continue;
                int damage = UnityEngine.Random.Range(DamageMin, DamageMaxInclusive + 1);
                stats.TakeDamage(damage, 20f, _player.transform);
            }
        }

        private void SpawnLandingVfx(Vector3 p)
        {
            Spawn(_earthShatter, p, 3.0f);
            Spawn(_sparks, p + Vector3.up * 0.15f, 2.0f);
            Spawn(_smoke, p + Vector3.up * 0.08f, 2.5f);
            StartCoroutine(RingPulse(p, 2.2f));
        }

        private static void Spawn(GameObject prefab, Vector3 p, float life)
        {
            if (prefab == null) return;
            GameObject fx = Instantiate(prefab, p, Quaternion.identity);
            Destroy(fx, life);
        }

        private IEnumerator RingPulse(Vector3 center, float radius)
        {
            GameObject ring = new GameObject("CLAUDE_VFX_HEROIC_LEAP_RING_2_2");
            LineRenderer lr = ring.AddComponent<LineRenderer>();
            lr.loop = true;
            lr.positionCount = 49;
            lr.useWorldSpace = true;
            lr.widthMultiplier = 0.055f;
            Material m = new Material(Shader.Find("Sprites/Default"));
            m.color = new Color(0.87f, 0.91f, 0.95f, 0.9f);
            lr.material = m;
            for (int i = 0; i < 49; i++)
            {
                float a = (i / 48f) * Mathf.PI * 2f;
                lr.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, 0.04f, Mathf.Sin(a) * radius));
            }
            float t = 0f;
            while (t < 0.28f)
            {
                t += Time.unscaledDeltaTime;
                float a = 1f - Mathf.Clamp01(t / 0.28f);
                Color c = m.color; c.a = a; m.color = c;
                yield return null;
            }
            Destroy(m);
            Destroy(ring);
        }
    }
}
