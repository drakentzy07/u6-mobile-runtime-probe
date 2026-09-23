using System.Collections;
using UnityEngine;

namespace Highfly.SkillLab
{
    public enum HighflyAcceptedSkillV016
    {
        Drift,
        JumpSmash
    }

    [DisallowMultipleComponent]
    public sealed class HighflyAcceptedSkillRuntimeV016 : MonoBehaviour
    {
        public static HighflyAcceptedSkillRuntimeV016 Instance { get; private set; }

        private PlayerController _player;
        private Animator _animator;
        private bool _busy;

        private void Awake()
        {
            Instance = this;
            _player = GetComponent<PlayerController>();
            _animator = _player != null ? _player.animator : GetComponentInChildren<Animator>();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Preview(HighflyAcceptedSkillV016 skill)
        {
            if (_busy) return;

            // Character A/B lab can swap the active humanoid Animator at runtime.
            // Resolve it on every preview instead of keeping Lucid's startup Animator cached.
            if (_player != null && _player.animator != null)
                _animator = _player.animator;

            if (skill == HighflyAcceptedSkillV016.Drift)
            {
                HighflyCombatLabV010.Instance?.ForcePreview(HighflyCombatSkillV010.FormulaDrift);
                return;
            }

            StartCoroutine(JumpSmash());
        }

        private IEnumerator JumpSmash()
        {
            _busy = true;
            try
            {
                CharacterStats target = FindTarget(10f);

                if (target != null)
                {
                    Vector3 d = target.transform.position - transform.position;
                    d.y = 0f;
                    if (d.sqrMagnitude > 0.001f)
                        transform.rotation = Quaternion.LookRotation(d.normalized, Vector3.up);
                }

                HighflySkillLabMetrics.RecordAction("JUMP SMASH • KEEP", 0);

                if (_animator != null)
                {
                    // Clean entry: discard any queued basic-attack trigger before native Lucid smash.
                    _animator.ResetTrigger("doAttack");
                    _animator.SetInteger("ComboStep", 0);
                    _animator.SetTrigger("doSmash");
                }

                yield return new WaitForSecondsRealtime(0.58f);

                Vector3 impact = transform.position + transform.forward * 1.25f;
                Spawn("HIGHFLY/AcceptedVFX/EarthShatter", impact, 0.88f, 2.0f);
                Spawn("HIGHFLY/AcceptedVFX/EnergyExplosion", impact, 0.60f, 1.5f);

                if (target != null && Vector3.Distance(target.transform.position, transform.position) <= 4.25f)
                {
                    target.TakeDamage(44f, 65f, transform);
                    HighflySkillLabMetrics.RecordAction("JUMP SMASH • IMPACT", 0);
                }

                HighflyTimeDilationManager.RequestHitStop(0.07f, 0.07f);
            }
            finally
            {
                _busy = false;
            }
        }

        private CharacterStats FindTarget(float radius)
        {
            CharacterStats[] all = Object.FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
            CharacterStats owner = GetComponent<CharacterStats>();
            CharacterStats best = null;
            float bestSq = radius * radius;

            for (int i = 0; i < all.Length; i++)
            {
                CharacterStats candidate = all[i];
                if (candidate == null || candidate == owner) continue;

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

        private static GameObject Spawn(string path, Vector3 pos, float scale, float life)
        {
            GameObject prefab = Resources.Load<GameObject>(path);
            if (prefab == null) return null;

            GameObject go = Object.Instantiate(prefab, pos, Quaternion.identity);
            go.transform.localScale *= scale;
            Object.Destroy(go, life);
            return go;
        }
    }
}
