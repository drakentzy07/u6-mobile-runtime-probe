using System.Collections;
using UnityEngine;

namespace Highfly.Run0I
{
    [DisallowMultipleComponent]
    public sealed class HighflyRun0IAttackDummy : MonoBehaviour
    {
        private PlayerStats _player;
        private float _nextAttack;
        private Renderer _renderer;
        private Material _material;
        private Color _baseColor;
        private Vector3 _repelVelocity;
        private float _repelUntil;

        private void Start()
        {
            _player = FindAnyObjectByType<PlayerStats>();
            _renderer = GetComponentInChildren<Renderer>();
            if (_renderer != null)
            {
                _material = _renderer.material;
                if (_material.HasProperty("_BaseColor")) _baseColor = _material.GetColor("_BaseColor");
                else if (_material.HasProperty("_Color")) _baseColor = _material.GetColor("_Color");
                else _baseColor = Color.gray;
            }
            _nextAttack = Time.unscaledTime + 2.5f;
        }

        private void Update()
        {
            if (_repelUntil > Time.unscaledTime) transform.position += _repelVelocity * Time.deltaTime;
            if (_player == null) { _player = FindAnyObjectByType<PlayerStats>(); return; }
            if (Time.unscaledTime < _nextAttack) return;

            if (Vector3.Distance(transform.position,_player.transform.position) <= 3.3f)
                StartCoroutine(TelegraphAndHit());

            _nextAttack = Time.unscaledTime + 4.0f;
        }

        public void ReceiveRepel(Vector3 direction,float meters)
        {
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.01f) direction = transform.forward;
            const float seconds = 0.20f;
            _repelVelocity = direction.normalized * (meters/seconds);
            _repelUntil = Time.unscaledTime + seconds;
        }

        private IEnumerator TelegraphAndHit()
        {
            SetColor(new Color(1f,0.28f,0.18f,1f));
            yield return new WaitForSecondsRealtime(0.45f);
            if (_player != null && Vector3.Distance(transform.position,_player.transform.position) <= 3.5f)
                _player.TakeDamage(12f,18f,transform);
            SetColor(_baseColor);
        }

        private void SetColor(Color color)
        {
            if (_material == null) return;
            if (_material.HasProperty("_BaseColor")) _material.SetColor("_BaseColor",color);
            if (_material.HasProperty("_Color")) _material.SetColor("_Color",color);
        }
    }
}
