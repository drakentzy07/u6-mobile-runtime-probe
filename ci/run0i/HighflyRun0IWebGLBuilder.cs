#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Highfly.Run0I.Editor
{
    public static class WebGLBuilder
    {
        public static void Build()
        {
            PrepareAnimations();

            string[] scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>s.path).ToArray();
            if (scenes.Length==0) throw new Exception("[RUN0I] No enabled scenes.");

            PlayerSettings.WebGL.compressionFormat=WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback=false;
            PlayerSettings.WebGL.dataCaching=true;

            const string output="build/WebGL";
            BuildPlayerOptions options=new BuildPlayerOptions
            {
                scenes=scenes,
                locationPathName=output,
                target=BuildTarget.WebGL,
                options=BuildOptions.None
            };

            Debug.Log("[RUN0I.2] Building Directional Melee Core");
            BuildReport report=BuildPipeline.BuildPlayer(options);
            if (report.summary.result!=BuildResult.Succeeded)
                throw new Exception("[RUN0I] WebGL failed: "+report.summary.result);

            File.WriteAllText(Path.Combine(output,"RUN0I2_BUILD.txt"),
                "HIGHFLY RUN0I.2 | DIRECTIONAL MELEE CORE | 9 LOADOUTS | STRIKE GRAMMAR | 360 STEERING | REAL TRACE | UNITY 6000.6.2");
            File.WriteAllText(Path.Combine(output,".nojekyll"),string.Empty);
        }

        private static void PrepareAnimations()
        {
            const string source="Assets/HIGHFLY/Run0I/UAL2_Standard.fbx";
            if (!File.Exists(source)) throw new Exception("[RUN0I] Missing UAL2 source.");

            ModelImporter importer=AssetImporter.GetAtPath(source) as ModelImporter;
            if (importer==null) throw new Exception("[RUN0I] UAL2 ModelImporter missing.");

            bool changed=importer.animationType!=ModelImporterAnimationType.Human || !importer.importAnimation;
            importer.importAnimation=true;
            importer.animationType=ModelImporterAnimationType.Human;
            importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            if (changed) importer.SaveAndReimport();

            AnimationClip[] clips=AssetDatabase.LoadAllAssetsAtPath(source)
                .OfType<AnimationClip>()
                .Where(x=>x!=null && !x.name.StartsWith("__preview__"))
                .ToArray();

            string target="Assets/Resources/HIGHFLY/Run0I/Animations";
            Directory.CreateDirectory(target);

            Copy(clips,target,"Sword_Regular_A","Sword_Regular_A");
            Copy(clips,target,"Sword_Regular_B","Sword_Regular_B");
            Copy(clips,target,"Sword_Regular_C","Sword_Regular_C");
            Copy(clips,target,"Sword_Heavy_Combo","Sword_Heavy_Combo");
            Copy(clips,target,"Slide_Start","Slide_Start");
            Copy(clips,target,"Slide_Loop","Slide_Loop");
            Copy(clips,target,"Slide_Exit","Slide_Exit");
            Copy(clips,target,"NinjaJump_Start","NinjaJump_Start");
            Copy(clips,target,"ClimbUp_1m","ClimbUp_1m");
            Copy(clips,target,"Sword_Dash","Sword_Dash");
            Copy(clips,target,"Sword_Regular_Combo","Sword_Regular_Combo");
            Copy(clips,target,"Melee_Hook","Melee_Hook");

            AnimationClip block=clips.FirstOrDefault(x=>x.name.IndexOf("Sword_Block",StringComparison.OrdinalIgnoreCase)>=0)
                ?? clips.FirstOrDefault(x=>x.name.IndexOf("Block",StringComparison.OrdinalIgnoreCase)>=0);
            if (block==null)
                throw new Exception("[RUN0I] No real block/parry clip found in UAL2; quality gate stops build.");
            SaveCopy(block,target+"/Sword_Block.anim","Sword_Block");

            // RUN0I.2: real unarmed body language from the same KayKit family.
            const string knightSource="Assets/Resources/HIGHFLY/Run0H/KayKitKnight.fbx";
            AnimationClip[] knightClips=AssetDatabase.LoadAllAssetsAtPath(knightSource)
                .OfType<AnimationClip>().Where(x=>x!=null && !x.name.StartsWith("__preview__")).ToArray();
            AnimationClip unarmedPunch=FindByTokens(knightClips,"unarmed","punch");
            AnimationClip unarmedKick=FindByTokens(knightClips,"unarmed","kick");
            if (unarmedPunch==null || unarmedKick==null)
                throw new Exception("[RUN0I.2] Quality gate: KayKit Knight missing real unarmed punch/kick clips.");
            SaveCopy(unarmedPunch,target+"/Unarmed_Punch.anim","Unarmed_Punch");
            SaveCopy(unarmedKick,target+"/Unarmed_Kick.anim","Unarmed_Kick");
            Debug.Log("[RUN0I.2] UNARMED_BANK="+unarmedPunch.name+" | "+unarmedKick.name);

            // RUN0I.1: restore the original Lucid 1->2->3 body language for Warrior.
            SaveCopy(LoadDirect("Assets/Animation/Player/Player_slash_1.anim"),target+"/Warrior_A.anim","Warrior_A");
            SaveCopy(LoadDirect("Assets/Animation/Player/Player_slash_2.anim"),target+"/Warrior_B.anim","Warrior_B");
            SaveCopy(LoadDirect("Assets/Animation/Player/Player_slash_3.anim"),target+"/Warrior_C.anim","Warrior_C");

            // Assassin may NOT fall back to sword/hammer clips anymore.
            // Pull real dual-wield motions from the KayKit Rogue FBX already shipped by this run.
            const string rogueSource="Assets/Resources/HIGHFLY/Run0H/KayKitRogue.fbx";
            AnimationClip[] rogueClips=AssetDatabase.LoadAllAssetsAtPath(rogueSource)
                .OfType<AnimationClip>()
                .Where(x=>x!=null && !x.name.StartsWith("__preview__"))
                .ToArray();

            AnimationClip dualChop=FindByTokens(rogueClips,"dual","chop");
            AnimationClip dualSlice=FindByTokens(rogueClips,"dual","slice");
            AnimationClip dualStab=FindByTokens(rogueClips,"dual","stab");
            if (dualChop==null || dualSlice==null || dualStab==null)
                throw new Exception("[RUN0I.1] Quality gate: KayKit Rogue has no complete Dualwield Chop/Slice/Stab trio. Refusing sword-like proxy fallback.");

            SaveCopy(dualChop,target+"/Assassin_A.anim","Assassin_A");
            SaveCopy(dualSlice,target+"/Assassin_B.anim","Assassin_B");
            SaveCopy(dualStab,target+"/Assassin_C.anim","Assassin_C");

            // Dedicated slots per dual family. Same audited donor bank today; independently replaceable later.
            SaveCopy(dualChop,target+"/DualSword_A.anim","DualSword_A");
            SaveCopy(dualSlice,target+"/DualSword_B.anim","DualSword_B");
            SaveCopy(dualStab,target+"/DualSword_C.anim","DualSword_C");
            SaveCopy(dualChop,target+"/DualAxe_A.anim","DualAxe_A");
            SaveCopy(dualSlice,target+"/DualAxe_B.anim","DualAxe_B");
            SaveCopy(dualStab,target+"/DualAxe_C.anim","DualAxe_C");
            SaveCopy(dualChop,target+"/Dagger_A.anim","Dagger_A");
            SaveCopy(dualSlice,target+"/Dagger_B.anim","Dagger_B");
            SaveCopy(dualStab,target+"/Dagger_C.anim","Dagger_C");

            const string barbarianSource="Assets/Resources/HIGHFLY/Run0H/KayKitBarbarian.fbx";
            AnimationClip[] barbarianClips=AssetDatabase.LoadAllAssetsAtPath(barbarianSource)
                .OfType<AnimationClip>().Where(x=>x!=null && !x.name.StartsWith("__preview__")).ToArray();
            AnimationClip axeChop=FindByTokens(barbarianClips,"1h","chop");
            AnimationClip axeSlice=FindByTokens(barbarianClips,"1h","slice");
            AnimationClip axeStab=FindByTokens(barbarianClips,"1h","stab");
            if (axeChop==null || axeSlice==null || axeStab==null)
                throw new Exception("[RUN0I.2] Quality gate: missing KayKit 1H Chop/Slice/Stab bank for axe family.");
            SaveCopy(axeChop,target+"/Axe_A.anim","Axe_A");
            SaveCopy(axeSlice,target+"/Axe_B.anim","Axe_B");
            SaveCopy(axeStab,target+"/Axe_C.anim","Axe_C");

            const string poleSource="Assets/Resources/HIGHFLY/Run0H/KayKitRogueHooded.fbx";
            AnimationClip[] poleClips=AssetDatabase.LoadAllAssetsAtPath(poleSource)
                .OfType<AnimationClip>().Where(x=>x!=null && !x.name.StartsWith("__preview__")).ToArray();
            AnimationClip poleStab=FindByTokens(poleClips,"2h","stab");
            AnimationClip poleSlice=FindByTokens(poleClips,"2h","slice");
            AnimationClip poleChop=FindByTokens(poleClips,"2h","chop");
            if (poleStab==null || poleSlice==null || poleChop==null)
                throw new Exception("[RUN0I.2] Quality gate: missing KayKit 2H Stab/Slice/Chop bank for spear family.");
            SaveCopy(poleStab,target+"/Spear_A.anim","Spear_A");
            SaveCopy(poleSlice,target+"/Spear_B.anim","Spear_B");
            SaveCopy(poleChop,target+"/Spear_C.anim","Spear_C");

            Debug.Log("[RUN0I.2] DUAL_BANK="+dualChop.name+" | "+dualSlice.name+" | "+dualStab.name);
            Debug.Log("[RUN0I.2] AXE_BANK="+axeChop.name+" | "+axeSlice.name+" | "+axeStab.name);
            Debug.Log("[RUN0I.2] SPEAR_BANK="+poleStab.name+" | "+poleSlice.name+" | "+poleChop.name);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[RUN0I] ANIMATION_BANK_READY clips="+clips.Length);
        }

        private static void Copy(AnimationClip[] clips,string target,string lookup,string output)
        {
            SaveCopy(Find(clips,lookup),target+"/"+output+".anim",output);
        }

        private static AnimationClip LoadDirect(string path)
        {
            AnimationClip clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip==null) throw new Exception("[RUN0I.1] Missing direct animation: "+path);
            return clip;
        }

        private static AnimationClip FindByTokens(AnimationClip[] clips,params string[] tokens)
        {
            return clips.FirstOrDefault(clip =>
            {
                string n=clip.name.ToLowerInvariant();
                return tokens.All(t=>n.Contains(t.ToLowerInvariant()));
            });
        }

        private static AnimationClip Find(AnimationClip[] clips,string name)
        {
            AnimationClip clip=clips.FirstOrDefault(x=>x.name.Equals(name,StringComparison.OrdinalIgnoreCase))
                ?? clips.FirstOrDefault(x=>x.name.IndexOf(name,StringComparison.OrdinalIgnoreCase)>=0);
            if (clip==null)
                throw new Exception("[RUN0I] Missing required real clip: "+name);
            return clip;
        }

        private static void SaveCopy(AnimationClip source,string path,string name)
        {
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path)!=null) AssetDatabase.DeleteAsset(path);
            AnimationClip copy=UnityEngine.Object.Instantiate(source);
            copy.name=name;
            AssetDatabase.CreateAsset(copy,path);
        }
    }
}
#endif
