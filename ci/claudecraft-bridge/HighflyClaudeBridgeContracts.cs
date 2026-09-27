using System;
using System.Collections.Generic;
using UnityEngine;

namespace Highfly.ClaudeBridge
{
    public enum ClaudeVfxArchetype
    {
        Unknown,
        Strike,
        Dash,
        Bolt,
        Burst,
        Nova,
        Beam,
        Dot,
        Heal,
        Buff,
        Shout,
        Summon,
        CrowdControl
    }

    public enum ClaudeAudioPolicy
    {
        None,
        HighflyOwned,
        Cc0Verified,
        RebuildOrLicenseClear
    }

    [Serializable]
    public sealed class HighflyClaudeVfxSpec
    {
        public ClaudeVfxArchetype archetype = ClaudeVfxArchetype.Unknown;
        public string palette = "";
        public float power = 1f;
        public string windupStyle = "";
        public float windup;
        public string[] motifs = Array.Empty<string>();
        public string trail = "";
        public int sparks;
        public bool debris;
        public bool smoke;
        public bool blood;
        public bool finisher;
        public bool barrier;
        public float linger;
    }

    [Serializable]
    public sealed class HighflyClaudeSkillDefinition
    {
        public string id = "";
        public string sourceCommit = "";
        public string animationClip = "";
        public string motionIntent = "";
        public float playbackSpeed = 1f;
        public List<Vector2> hitWindows = new List<Vector2>();
        public HighflyClaudeVfxSpec vfx = new HighflyClaudeVfxSpec();
        public string releaseCue = "";
        public string impactCue = "";
        public ClaudeAudioPolicy audioPolicy = ClaudeAudioPolicy.RebuildOrLicenseClear;
    }

    public interface IHighflyClaudeVfxSink
    {
        void PlayWindup(HighflyClaudeSkillDefinition skill, Transform caster);
        void PlayRelease(HighflyClaudeSkillDefinition skill, Transform caster, Transform target);
        void PlayImpact(HighflyClaudeSkillDefinition skill, Vector3 point, Vector3 normal);
    }

    public interface IHighflyClaudeSfxSink
    {
        void PlayRelease(HighflyClaudeSkillDefinition skill, Vector3 point);
        void PlayImpact(HighflyClaudeSkillDefinition skill, Vector3 point);
    }

    /// <summary>
    /// Bridge seam only. This deliberately does not own locomotion, camera,
    /// damage, targeting, weapon sockets or combo state. Those remain HIGHFLY.
    /// </summary>
    public interface IHighflyClaudeSkillExecutor
    {
        bool CanExecute(HighflyClaudeSkillDefinition skill);
        void Execute(HighflyClaudeSkillDefinition skill);
        void Cancel(string reason);
    }
}
