using System;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.Combat
{
    public enum HighflyCombatAction
    {
        None = 0,
        Light = 1,
        Heavy = 2,
        Dodge = 3,
        Parry = 4,
        Counter = 5,
        Skill1 = 10,
        Skill2 = 11,
        Skill3 = 12,
        Skill4 = 13,
        Ultimate = 20
    }

    [Flags]
    public enum HighflyCancelMask
    {
        None = 0,
        Light = 1 << 0,
        Heavy = 1 << 1,
        Dodge = 1 << 2,
        Parry = 1 << 3,
        Skill = 1 << 4,
        Ultimate = 1 << 5,
        AnyAttack = Light | Heavy,
        AnySkill = Skill | Ultimate,
        Defensive = Dodge | Parry,
        All = AnyAttack | AnySkill | Defensive
    }

    [Serializable]
    public struct HighflyCombatWindow
    {
        [Range(0f, 1f)] public float openNormalizedTime;
        [Range(0f, 1f)] public float closeNormalizedTime;
        public HighflyCancelMask allowedActions;

        public bool Contains(float normalizedTime)
        {
            if (closeNormalizedTime < openNormalizedTime)
                return false;

            return normalizedTime >= openNormalizedTime &&
                   normalizedTime <= closeNormalizedTime;
        }

        public bool Allows(HighflyCombatAction action)
        {
            return (allowedActions & ToMask(action)) != 0;
        }

        public static HighflyCancelMask ToMask(HighflyCombatAction action)
        {
            switch (action)
            {
                case HighflyCombatAction.Light:
                    return HighflyCancelMask.Light;
                case HighflyCombatAction.Heavy:
                    return HighflyCancelMask.Heavy;
                case HighflyCombatAction.Dodge:
                    return HighflyCancelMask.Dodge;
                case HighflyCombatAction.Parry:
                case HighflyCombatAction.Counter:
                    return HighflyCancelMask.Parry;
                case HighflyCombatAction.Skill1:
                case HighflyCombatAction.Skill2:
                case HighflyCombatAction.Skill3:
                case HighflyCombatAction.Skill4:
                    return HighflyCancelMask.Skill;
                case HighflyCombatAction.Ultimate:
                    return HighflyCancelMask.Ultimate;
                default:
                    return HighflyCancelMask.None;
            }
        }
    }

    [Serializable]
    public struct HighflyComboEdge
    {
        public HighflyCombatAction input;
        public string nextNodeId;
    }

    [Serializable]
    public sealed class HighflyComboNode
    {
        public string nodeId = "L1";
        public string animationState = "";
        [Min(0f)] public float transitionDuration = 0.08f;

        [Header("Gameplay")]
        [Min(0f)] public float damageMultiplier = 1f;
        [Min(0f)] public float resourceCost = 0f;
        [Min(0f)] public float hitStopSeconds = 0.04f;

        [Header("Input Buffer")]
        [Range(0f, 1f)] public float consumeOpenNormalizedTime = 0.45f;
        [Range(0f, 1f)] public float consumeCloseNormalizedTime = 0.90f;

        [Header("Cancel Windows")]
        public HighflyCombatWindow[] cancelWindows = Array.Empty<HighflyCombatWindow>();

        [Header("Branches")]
        public HighflyComboEdge[] branches = Array.Empty<HighflyComboEdge>();

        public bool IsConsumeWindowOpen(float normalizedTime)
        {
            if (consumeCloseNormalizedTime < consumeOpenNormalizedTime)
                return false;

            return normalizedTime >= consumeOpenNormalizedTime &&
                   normalizedTime <= consumeCloseNormalizedTime;
        }

        public bool CanCancelInto(HighflyCombatAction action, float normalizedTime)
        {
            if (cancelWindows == null)
                return false;

            for (int i = 0; i < cancelWindows.Length; i++)
            {
                if (cancelWindows[i].Contains(normalizedTime) &&
                    cancelWindows[i].Allows(action))
                    return true;
            }

            return false;
        }

        public bool TryResolveBranch(HighflyCombatAction input, out string nextNodeId)
        {
            if (branches != null)
            {
                for (int i = 0; i < branches.Length; i++)
                {
                    if (branches[i].input == input &&
                        !string.IsNullOrWhiteSpace(branches[i].nextNodeId))
                    {
                        nextNodeId = branches[i].nextNodeId;
                        return true;
                    }
                }
            }

            nextNodeId = null;
            return false;
        }
    }

    [CreateAssetMenu(
        fileName = "HighflyComboProfile",
        menuName = "HIGHFLY/Combat/Combo Profile")]
    public sealed class HighflyComboProfile : ScriptableObject
    {
        public string profileId = "default";
        public string lightEntryNode = "L1";
        public string heavyEntryNode = "H1";
        public List<HighflyComboNode> nodes = new List<HighflyComboNode>();

        private Dictionary<string, HighflyComboNode> _index;

        public bool TryGetNode(string nodeId, out HighflyComboNode node)
        {
            EnsureIndex();
            return _index.TryGetValue(nodeId ?? string.Empty, out node);
        }

        public bool TryGetEntry(HighflyCombatAction action, out HighflyComboNode node)
        {
            switch (action)
            {
                case HighflyCombatAction.Light:
                    return TryGetNode(lightEntryNode, out node);
                case HighflyCombatAction.Heavy:
                    return TryGetNode(heavyEntryNode, out node);
                default:
                    node = null;
                    return false;
            }
        }

        public void RebuildIndex()
        {
            _index = null;
            EnsureIndex();
        }

        private void EnsureIndex()
        {
            if (_index != null)
                return;

            _index = new Dictionary<string, HighflyComboNode>(StringComparer.Ordinal);

            if (nodes == null)
                return;

            for (int i = 0; i < nodes.Count; i++)
            {
                HighflyComboNode node = nodes[i];
                if (node == null || string.IsNullOrWhiteSpace(node.nodeId))
                    continue;

                _index[node.nodeId] = node;
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildIndex();
        }
#endif
    }

    public readonly struct HighflyBufferedAction
    {
        public readonly HighflyCombatAction action;
        public readonly float queuedAt;
        public readonly float expiresAt;
        public readonly uint sequence;

        public HighflyBufferedAction(
            HighflyCombatAction action,
            float queuedAt,
            float expiresAt,
            uint sequence)
        {
            this.action = action;
            this.queuedAt = queuedAt;
            this.expiresAt = expiresAt;
            this.sequence = sequence;
        }

        public bool IsExpired(float now)
        {
            return now > expiresAt;
        }
    }

    /// <summary>
    /// Small deterministic ring buffer for combat inputs.
    /// It deliberately owns no PlayerController, Animator, camera, stamina or damage logic.
    /// Those remain in the Lucid/HIGHFLY host and consume requests from this service.
    /// </summary>
    public sealed class HighflyActionBuffer
    {
        private readonly HighflyBufferedAction[] _entries;
        private int _head;
        private int _count;
        private uint _sequence;

        public int Count => _count;
        public int Capacity => _entries.Length;

        public HighflyActionBuffer(int capacity)
        {
            if (capacity < 1)
                capacity = 1;

            _entries = new HighflyBufferedAction[capacity];
        }

        public HighflyBufferedAction Push(
            HighflyCombatAction action,
            float now,
            float ttlSeconds)
        {
            if (ttlSeconds < 0f)
                ttlSeconds = 0f;

            var entry = new HighflyBufferedAction(
                action,
                now,
                now + ttlSeconds,
                ++_sequence);

            int index;
            if (_count < _entries.Length)
            {
                index = (_head + _count) % _entries.Length;
                _count++;
            }
            else
            {
                index = _head;
                _head = (_head + 1) % _entries.Length;
            }

            _entries[index] = entry;
            return entry;
        }

        public bool TryPeek(float now, out HighflyBufferedAction entry)
        {
            DropExpired(now);

            if (_count <= 0)
            {
                entry = default;
                return false;
            }

            entry = _entries[_head];
            return true;
        }

        public bool TryConsume(
            float now,
            Func<HighflyBufferedAction, bool> predicate,
            out HighflyBufferedAction entry)
        {
            DropExpired(now);

            if (_count <= 0)
            {
                entry = default;
                return false;
            }

            for (int offset = 0; offset < _count; offset++)
            {
                int index = (_head + offset) % _entries.Length;
                HighflyBufferedAction candidate = _entries[index];

                if (predicate != null && !predicate(candidate))
                    continue;

                entry = candidate;
                RemoveAtOffset(offset);
                return true;
            }

            entry = default;
            return false;
        }

        public int DropExpired(float now)
        {
            int removed = 0;

            while (_count > 0)
            {
                HighflyBufferedAction first = _entries[_head];
                if (!first.IsExpired(now))
                    break;

                _head = (_head + 1) % _entries.Length;
                _count--;
                removed++;
            }

            return removed;
        }

        public void Clear()
        {
            _head = 0;
            _count = 0;
        }

        private void RemoveAtOffset(int offset)
        {
            for (int i = offset; i < _count - 1; i++)
            {
                int from = (_head + i + 1) % _entries.Length;
                int to = (_head + i) % _entries.Length;
                _entries[to] = _entries[from];
            }

            _count--;

            if (_count == 0)
                _head = 0;
        }
    }

    [DisallowMultipleComponent]
    public sealed class HighflyCombatCore : MonoBehaviour
    {
        [Header("Buffer")]
        [SerializeField, Range(1, 8)] private int bufferCapacity = 3;
        [SerializeField, Min(0f)] private float attackBufferSeconds = 0.28f;
        [SerializeField, Min(0f)] private float defensiveBufferSeconds = 0.18f;
        [SerializeField, Min(0f)] private float skillBufferSeconds = 0.32f;

        [Header("Combo Data")]
        [SerializeField] private HighflyComboProfile comboProfile;

        private HighflyActionBuffer _buffer;
        private uint _bufferedCount;
        private uint _consumedCount;
        private uint _expiredCount;

        public event Action<HighflyBufferedAction> ActionBuffered;
        public event Action<HighflyBufferedAction> ActionConsumed;

        public int PendingCount => _buffer != null ? _buffer.Count : 0;
        public uint BufferedCount => _bufferedCount;
        public uint ConsumedCount => _consumedCount;
        public uint ExpiredCount => _expiredCount;
        public HighflyComboProfile ComboProfile => comboProfile;

        private void Awake()
        {
            _buffer = new HighflyActionBuffer(bufferCapacity);
        }

        private void Update()
        {
            if (_buffer == null)
                return;

            _expiredCount += (uint)_buffer.DropExpired(Time.unscaledTime);
        }

        public HighflyBufferedAction Buffer(HighflyCombatAction action)
        {
            EnsureBuffer();

            float now = Time.unscaledTime;
            float ttl = GetTtl(action);
            HighflyBufferedAction entry = _buffer.Push(action, now, ttl);

            _bufferedCount++;
            ActionBuffered?.Invoke(entry);
            return entry;
        }

        public bool TryConsumeAny(out HighflyBufferedAction entry)
        {
            EnsureBuffer();

            bool consumed = _buffer.TryConsume(
                Time.unscaledTime,
                null,
                out entry);

            if (consumed)
                NotifyConsumed(entry);

            return consumed;
        }

        public bool TryConsumeAllowed(
            HighflyCancelMask allowed,
            out HighflyBufferedAction entry)
        {
            EnsureBuffer();

            bool consumed = _buffer.TryConsume(
                Time.unscaledTime,
                buffered =>
                    (HighflyCombatWindow.ToMask(buffered.action) & allowed) != 0,
                out entry);

            if (consumed)
                NotifyConsumed(entry);

            return consumed;
        }

        public bool TryConsumeForNode(
            HighflyComboNode currentNode,
            float normalizedTime,
            out HighflyBufferedAction entry,
            out HighflyComboNode nextNode)
        {
            entry = default;
            nextNode = null;

            if (currentNode == null ||
                !currentNode.IsConsumeWindowOpen(normalizedTime))
                return false;

            EnsureBuffer();

            bool consumed = _buffer.TryConsume(
                Time.unscaledTime,
                buffered =>
                {
                    string nextId;
                    return currentNode.TryResolveBranch(
                        buffered.action,
                        out nextId);
                },
                out entry);

            if (!consumed)
                return false;

            string resolvedId;
            if (!currentNode.TryResolveBranch(entry.action, out resolvedId) ||
                comboProfile == null ||
                !comboProfile.TryGetNode(resolvedId, out nextNode))
            {
                return false;
            }

            NotifyConsumed(entry);
            return true;
        }

        public bool CanCancel(
            HighflyComboNode currentNode,
            HighflyCombatAction action,
            float normalizedTime)
        {
            return currentNode != null &&
                   currentNode.CanCancelInto(action, normalizedTime);
        }

        public void ClearBuffer()
        {
            if (_buffer != null)
                _buffer.Clear();
        }

        public string GetTelemetrySnapshot()
        {
            return
                "buffered=" + _bufferedCount +
                " consumed=" + _consumedCount +
                " expired=" + _expiredCount +
                " pending=" + PendingCount;
        }

        private void NotifyConsumed(HighflyBufferedAction entry)
        {
            _consumedCount++;
            ActionConsumed?.Invoke(entry);
        }

        private float GetTtl(HighflyCombatAction action)
        {
            switch (action)
            {
                case HighflyCombatAction.Dodge:
                case HighflyCombatAction.Parry:
                case HighflyCombatAction.Counter:
                    return defensiveBufferSeconds;

                case HighflyCombatAction.Skill1:
                case HighflyCombatAction.Skill2:
                case HighflyCombatAction.Skill3:
                case HighflyCombatAction.Skill4:
                case HighflyCombatAction.Ultimate:
                    return skillBufferSeconds;

                default:
                    return attackBufferSeconds;
            }
        }

        private void EnsureBuffer()
        {
            if (_buffer == null)
                _buffer = new HighflyActionBuffer(bufferCapacity);
        }
    }
}
