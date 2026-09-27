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
            PrepareWeaponDonors();
            ValidateKayKitHunter();
            ValidateArsenal();
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

            // CI retry marker: verify raw FBX bytes with Git filters disabled.\n            Debug.Log("[SKILL4] Building Hunter Base • RogueHooded • KayKit Fantasy grip-origin arsenal");
            BuildReport report=BuildPipeline.BuildPlayer(options);
            if (report.summary.result!=BuildResult.Succeeded)
                throw new Exception("[RUN0I] WebGL failed: "+report.summary.result);

            File.WriteAllText(Path.Combine(output,"RUN0I3_BUILD.txt"),
                "HIGHFLY SKILL4 | KAYKIT WEAPON FOUNDATION | HUNTER BASE ROGUE HOODED | KAYKIT FANTASY GRIP ORIGIN | 10 LOADOUTS | UNARMED R-L-KICK | SINGLE 1-2-3 SPIN | DUAL SWORD R-L-X | DUAL DAGGER CHOP-SLICE-STAB | DUAL AXE CLASSIC 1-2-3 SPIN | SHIELD HEAVY SHOVE | SPEAR THRUST-SWEEP-THRUST | UNITY 6000.6.2");
            File.WriteAllText(Path.Combine(output,".nojekyll"),string.Empty);
        }

        private static void PrepareWeaponDonors()
        {
            AssetDatabase.Refresh();

            string swordPath=FindPrideGameObjectPath("sword");
            // Pride Weapons 1.1 prefabs shipped with transform issues later fixed upstream.
            // Use the raw FBX for the spear so our KayKit hand-space correction owns the transform.
            string spearPath=FindPrideGameObjectPath("spear",true);
            if(string.IsNullOrWhiteSpace(swordPath))
                throw new Exception("[SKILL3-ARSENAL] POLYGON Pride sword not found after package import.");
            if(string.IsNullOrWhiteSpace(spearPath))
                throw new Exception("[SKILL3-ARSENAL] POLYGON Pride spear not found after package import.");

            SaveRuntimePrefab(swordPath,"Assets/Resources/HIGHFLY/Run0I/PrideSword.prefab");
            SaveRuntimePrefab(spearPath,"Assets/Resources/HIGHFLY/Run0I/PrideSpear.prefab");

            string sidekickAxePath=AssetDatabase.GetAllAssetPaths()
                .FirstOrDefault(p=>p.EndsWith("SK_Axe.fbx",StringComparison.OrdinalIgnoreCase) &&
                                   p.IndexOf("Goblin_Axe",StringComparison.OrdinalIgnoreCase)>=0);
            if(string.IsNullOrWhiteSpace(sidekickAxePath))
                throw new Exception("[SKILL3-ARSENAL] Official Sidekick SK_Axe.fbx not found.");
            SaveRuntimePrefab(sidekickAxePath,"Assets/Resources/HIGHFLY/Run0I/SidekickAxe.prefab");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SKILL3-ARSENAL] DONORS_READY prideSword="+swordPath+" prideSpear="+spearPath+" sidekickAxe="+sidekickAxePath);
        }

        private static string FindPrideGameObjectPath(string token,bool preferFbx=false)
        {
            string[] paths=AssetDatabase.GetAllAssetPaths()
                .Where(p=>p.IndexOf("pride",StringComparison.OrdinalIgnoreCase)>=0 &&
                          p.IndexOf(token,StringComparison.OrdinalIgnoreCase)>=0 &&
                          (p.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase) ||
                           p.EndsWith(".prefab",StringComparison.OrdinalIgnoreCase)))
                .OrderBy(p=>preferFbx
                    ? (p.EndsWith(".fbx",StringComparison.OrdinalIgnoreCase)?0:1)
                    : (p.EndsWith(".prefab",StringComparison.OrdinalIgnoreCase)?0:1))
                .ThenBy(p=>p.Length)
                .ToArray();

            foreach(string path in paths)
            {
                GameObject go=AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if(go==null) continue;
                if(go.GetComponentsInChildren<Renderer>(true).Length==0 &&
                   go.GetComponentsInChildren<MeshFilter>(true).Length==0) continue;
                return path;
            }
            return null;
        }

        private static void SaveRuntimePrefab(string sourcePath,string targetPath)
        {
            GameObject source=AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            if(source==null) throw new Exception("[SKILL3-ARSENAL] Could not load "+sourcePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetPath));
            if(AssetDatabase.LoadAssetAtPath<GameObject>(targetPath)!=null)
                AssetDatabase.DeleteAsset(targetPath);

            GameObject instance=UnityEngine.Object.Instantiate(source);
            instance.name=Path.GetFileNameWithoutExtension(targetPath);
            PrefabUtility.SaveAsPrefabAsset(instance,targetPath);
            UnityEngine.Object.DestroyImmediate(instance);
        }

        private static void ValidateArsenal()
        {
            string[] resources={
                "HIGHFLY/Run0I/PrideSword",
                "HIGHFLY/Run0I/KayKitFantasyDaggerA",
                "HIGHFLY/Run0I/KayKitFantasyAxeA",
                "HIGHFLY/Run0I/KayKitFantasySpearA",
                "HIGHFLY/Run0I/PrideSpear",
                "HIGHFLY/Run0I/SidekickAxe",
                "HIGHFLY/Run0I/QSwordGolden",
                "HIGHFLY/Run0I/QDagger2",
                "HIGHFLY/Run0I/QAxeDouble",
                "HIGHFLY/Run0I/QShieldCelticGolden",
                "HIGHFLY/Run0H/KayKitShieldRound",
                "HIGHFLY/Run0H/KayKitDagger"
            };

            foreach(string resource in resources)
            {
                GameObject go=Resources.Load<GameObject>(resource);
                if(go==null) throw new Exception("[SKILL3-ARSENAL] Missing resource: "+resource);
                Renderer[] rs=go.GetComponentsInChildren<Renderer>(true);
                MeshFilter[] mfs=go.GetComponentsInChildren<MeshFilter>(true);
                if(rs.Length==0 && mfs.Length==0)
                    throw new Exception("[SKILL3-ARSENAL] Weapon has no renderable mesh: "+resource);

                foreach(Renderer renderer in rs)
                {
                    if(renderer==null) continue;
                    foreach(Material material in renderer.sharedMaterials)
                    {
                        if(material==null) continue; // FBX may legitimately use Unity's default material.
                        if(material.shader==null || material.shader.name=="Hidden/InternalErrorShader")
                            throw new Exception("[SKILL3-ARSENAL] Broken weapon material: "+resource+" / "+material.name);
                    }
                }
            }

            Debug.Log("[SKILL4] ARSENAL_GATE_OK • RogueHooded Hunter Base • KayKit Fantasy dagger/axe/spear grip-origin • KayKit shield • Pride sword");
        }

        private static Transform FindDescendant(Transform root,string exact)
        {
            if(root==null) return null;
            Transform[] all=root.GetComponentsInChildren<Transform>(true);
            for(int i=0;i<all.Length;i++)
                if(all[i]!=null && string.Equals(all[i].name,exact,StringComparison.OrdinalIgnoreCase))
                    return all[i];
            return null;
        }

        private static void ValidateKayKitHunter()
        {
            const string hunterPath="Assets/Resources/HIGHFLY/Run0H/KayKitRogueHooded.fbx";
            GameObject hunterAsset=AssetDatabase.LoadAssetAtPath<GameObject>(hunterPath);
            if(hunterAsset==null) throw new Exception("[SKILL3-CROWN] Missing KayKit Hunter: "+hunterPath);

            GameObject hunter=UnityEngine.Object.Instantiate(hunterAsset);
            try
            {
                Animator animator=hunter.GetComponent<Animator>();
                if(animator==null) animator=hunter.GetComponentInChildren<Animator>(true);
                if(animator==null || animator.avatar==null || !animator.avatar.isValid || !animator.avatar.isHuman)
                    throw new Exception("[SKILL3-CROWN] KayKit Hunter humanoid avatar invalid.");

                Renderer[] renderers=hunter.GetComponentsInChildren<Renderer>(true);
                if(renderers.Length==0) throw new Exception("[SKILL3-CROWN] KayKit Hunter has no renderers.");

                Transform right=animator.GetBoneTransform(HumanBodyBones.RightHand);
                Transform left=animator.GetBoneTransform(HumanBodyBones.LeftHand);
                if(right==null || left==null)
                    throw new Exception("[SKILL3-CROWN] KayKit Hunter missing humanoid hand bones.");

                Debug.Log("[SKILL4] HUNTER_BASE_ROGUE_HOODED_GATE_OK renderers="+renderers.Length+
                          " right="+right.name+" left="+left.name);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(hunter);
            }
        }

        private static void ValidateSidekick126()
        {
            const string prefabPath = "Assets/Resources/HIGHFLY/Run0I/Sidekick126.prefab";
            const string shaderGuid = "db628544640279b41a4a7aa5d75c0322";

            string shaderPath = AssetDatabase.GUIDToAssetPath(shaderGuid);
            if (string.IsNullOrWhiteSpace(shaderPath))
                throw new Exception("[SKILL3] Sidekick shader GUID is missing: " + shaderGuid);

            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
            if (shader == null)
                throw new Exception("[SKILL3] Sidekick shader could not be imported: " + shaderPath);

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
                throw new Exception("[SKILL3] Sidekick 1.2.6 runtime prefab missing: " + prefabPath);

            Animator animator = prefab.GetComponent<Animator>();
            if (animator == null || animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
                throw new Exception("[SKILL3] Sidekick prefab does not have a valid Humanoid Avatar.");

            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new Exception("[SKILL3] Sidekick prefab has no renderers.");

            int materialCount = 0;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null) continue;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                        throw new Exception("[SKILL3] Sidekick prefab contains a null material.");
                    if (material.shader == null ||
                        material.shader.name == "Hidden/InternalErrorShader")
                        throw new Exception("[SKILL3] Broken Sidekick material: " + material.name);
                    materialCount++;
                }
            }

            if (materialCount == 0)
                throw new Exception("[SKILL3] Sidekick prefab resolved zero materials.");

            Debug.Log("[SKILL3] SIDEKICK126_GATE_OK prefab=" + prefab.name +
                      " renderers=" + renderers.Length +
                      " materials=" + materialCount +
                      " shader=" + shader.name);
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

            // KayKit Character Animations 1.1 • real shield attack donor.
            const string kaykitCombatSource="Assets/HIGHFLY/Run0I/KayKit_Rig_Medium_CombatMelee.fbx";
            if(!File.Exists(kaykitCombatSource))
                throw new Exception("[SKILL3-CROWN] Missing KayKit Character Animations 1.1 melee donor.");

            ModelImporter kaykitCombatImporter=AssetImporter.GetAtPath(kaykitCombatSource) as ModelImporter;
            if(kaykitCombatImporter==null)
                throw new Exception("[SKILL3-CROWN] KayKit melee donor importer missing.");
            bool kaykitChanged=kaykitCombatImporter.animationType!=ModelImporterAnimationType.Human || !kaykitCombatImporter.importAnimation;
            kaykitCombatImporter.importAnimation=true;
            kaykitCombatImporter.animationType=ModelImporterAnimationType.Human;
            kaykitCombatImporter.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            kaykitCombatImporter.animationCompression=ModelImporterAnimationCompression.Optimal;
            kaykitCombatImporter.resampleCurves=true;
            if(kaykitChanged) kaykitCombatImporter.SaveAndReimport();

            AnimationClip[] kaykitCombatClips=AssetDatabase.LoadAllAssetsAtPath(kaykitCombatSource)
                .OfType<AnimationClip>()
                .Where(x=>x!=null && !x.name.StartsWith("__preview__"))
                .ToArray();
            AnimationClip shieldBash=kaykitCombatClips.FirstOrDefault(x=>
                x.name.IndexOf("Melee_Block_Attack",StringComparison.OrdinalIgnoreCase)>=0 ||
                (x.name.IndexOf("Block",StringComparison.OrdinalIgnoreCase)>=0 &&
                 x.name.IndexOf("Attack",StringComparison.OrdinalIgnoreCase)>=0));
            AnimationClip shieldBlock=kaykitCombatClips.FirstOrDefault(x=>
                x.name.Equals("Melee_Blocking",StringComparison.OrdinalIgnoreCase) ||
                x.name.IndexOf("Melee_Blocking",StringComparison.OrdinalIgnoreCase)>=0);
            if(shieldBash==null)
                throw new Exception("[SKILL3-CROWN] KayKit Melee_Block_Attack not found.");
            if(shieldBlock==null)
                throw new Exception("[SKILL3-CROWN] KayKit Melee_Blocking not found.");
            SaveCopy(shieldBash,target+"/Shield_Bash.anim","Shield_Bash");
            SaveCopy(shieldBlock,target+"/Shield_Block.anim","Shield_Block");
            Debug.Log("[SKILL3-CROWN] SHIELD_BANK_READY bash="+shieldBash.name+" block="+shieldBlock.name);

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
            AnimationClip[] unarmedPunches=knightClips
                .Where(x=>x.name.IndexOf("unarmed",StringComparison.OrdinalIgnoreCase)>=0 &&
                          x.name.IndexOf("punch",StringComparison.OrdinalIgnoreCase)>=0)
                .OrderBy(x=>x.name)
                .ToArray();
            AnimationClip unarmedKick=FindByTokens(knightClips,"unarmed","kick");
            if (unarmedPunches.Length<2 || unarmedKick==null)
                throw new Exception("[RUN0I.3] Quality gate: need two distinct KayKit unarmed punches plus one kick.");

            AnimationClip unarmedPunchA=unarmedPunches
                .FirstOrDefault(x=>x.name.IndexOf("Punch_A",StringComparison.OrdinalIgnoreCase)>=0)
                ?? unarmedPunches[0];
            AnimationClip unarmedPunchB=unarmedPunches
                .FirstOrDefault(x=>x.name.IndexOf("Punch_B",StringComparison.OrdinalIgnoreCase)>=0)
                ?? unarmedPunches.First(x=>x!=unarmedPunchA);

            if (unarmedPunchA==unarmedPunchB)
                throw new Exception("[RUN0I.3] Quality gate: unarmed punch A/B resolved to same clip.");

            SaveCopy(unarmedPunchA,target+"/Unarmed_Punch_A.anim","Unarmed_Punch_A");
            SaveCopy(unarmedKick,target+"/Unarmed_Kick.anim","Unarmed_Kick");
            SaveCopy(unarmedPunchB,target+"/Unarmed_Punch_B.anim","Unarmed_Punch_B");

            // RUN0I.3 Lite: do not depend on unverified block/bash names.
            // CrossGuard uses already-audited dual strike poses. Shield/Pole/Weapon guard
            // fall back to the known-good Sword_Block from the existing UAL2 bank.
            Debug.Log("[RUN0I.3] UNARMED_BANK="+unarmedPunchA.name+" | "+unarmedKick.name+" | "+unarmedPunchB.name);
            Debug.Log("[RUN0I.3-LITE] GUARD_BANK=known-good Sword_Block + dual cross poses");

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

            // SKILL4: one universal dual grammar for every dual loadout.
            // A is the right-hand opening slash, B the left-hand return slash.
            // Runtime composes A+B inside basic #3 to make a literal X instead of
            // pretending the stab clip is a crossed finisher.
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
            if (poleStab==null || poleSlice==null)
                throw new Exception("[SKILL4] Quality gate: missing KayKit 2H Stab/Slice bank for spear family.");
            SaveCopy(poleStab,target+"/Spear_A.anim","Spear_A");
            SaveCopy(poleSlice,target+"/Spear_B.anim","Spear_B");
            SaveCopy(poleStab,target+"/Spear_C.anim","Spear_C");

            Debug.Log("[RUN0I.2] DUAL_BANK="+dualChop.name+" | "+dualSlice.name+" | "+dualStab.name);
            Debug.Log("[RUN0I.2] AXE_BANK="+axeChop.name+" | "+axeSlice.name+" | "+axeStab.name);
            Debug.Log("[SKILL4] SPEAR_BANK="+poleStab.name+" | "+poleSlice.name+" | "+poleStab.name+" (thrust finisher)");

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
