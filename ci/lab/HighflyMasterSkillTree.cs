using System;
using UnityEngine;

namespace Highfly.SkillLab
{
    public enum HighflyMasterTier
    {
        Base,
        Technique,
        Mastery,
        Evolution,
        Fusion,
        Apex,
        Transcendent
    }

    [Serializable]
    public sealed class HighflyMasterSkillNode
    {
        public string Id;
        public string Name;
        public string Weapon;
        public HighflyMasterTier Tier;
        public string Role;
        public string MechanicKey;
        public string Parent;
        public string Destination;
        public string Inspiration;
        public bool RuntimeReady;

        public HighflyMasterSkillNode(
            string id,
            string name,
            string weapon,
            HighflyMasterTier tier,
            string role,
            string mechanicKey,
            string parent,
            string destination,
            string inspiration,
            bool runtimeReady = false)
        {
            Id = id;
            Name = name;
            Weapon = weapon;
            Tier = tier;
            Role = role;
            MechanicKey = mechanicKey;
            Parent = parent;
            Destination = destination;
            Inspiration = inspiration;
            RuntimeReady = runtimeReady;
        }
    }

    public static class HighflyMasterSkillTree
    {
        // v0.8 rule: one mechanical signature = one canonical node.
        // Named reference skills that perform the same job become evolutions,
        // variants or presentation layers instead of duplicate buttons.
        public static readonly HighflyMasterSkillNode[] All =
        {
            // UNIVERSAL MOVEMENT / DEFENSE
            N("MOV-01","IMPULSO","UNIVERSAL",HighflyMasterTier.Base,"Movilidad","dash","-","ACELERACIÓN","SLF Accel / Solo Leveling Dash"),
            N("MOV-02","DERIVA","UNIVERSAL",HighflyMasterTier.Technique,"Evasión","directional-slide","IMPULSO","PASO FANTASMA","SLF Skate Foot / Drift Step"),
            N("MOV-03","PASO AÉREO","UNIVERSAL",HighflyMasterTier.Technique,"Aéreo","air-step","SALTO","CADENA AÉREA","SLF Best Step / Moon Jumper"),
            N("MOV-04","CADENA AÉREA","UNIVERSAL",HighflyMasterTier.Evolution,"Aéreo","multi-air-step","PASO AÉREO","APEX MOVILIDAD","SLF Five/Six-Boat Leap"),
            N("MOV-05","PASO DE MURO","UNIVERSAL",HighflyMasterTier.Technique,"Parkour","wall-rebound","SALTO","TRICK WALL","Exiled Luce Acrobatic Steps + HIGHFLY"),
            N("MOV-06","TRICK WALL","UNIVERSAL",HighflyMasterTier.Evolution,"Parkour","contextual-flip","PASO DE MURO","APEX MOVILIDAD","HIGHFLY: backflip/sideflip contextual"),
            N("MOV-07","PASO FANTASMA","UNIVERSAL",HighflyMasterTier.Evolution,"I-frame","blink-reposition","DERIVA","DANZA FANTASMA","SLF high-speed movement / HIGHFLY",true),
            N("MOV-08","INTERCAMBIO UMBRÍO","UNIVERSAL",HighflyMasterTier.Apex,"Teleport","shadow-swap","SOMBRA","TRANSCENDENT","Solo Leveling Shadow Exchange"),
            N("DEF-01","PARRY","UNIVERSAL",HighflyMasterTier.Base,"Defensa","timed-parry","-","CONTRA PERFECTA","Exiled Elymas / SAO Parry"),
            N("DEF-02","DESVÍO","UNIVERSAL",HighflyMasterTier.Technique,"Defensa","projectile-deflect","PARRY","REFLEJO TOTAL","SAO Bullet Deflect / SLF Oikatzo"),
            N("DEF-03","CORTE DE HECHIZO","UNIVERSAL",HighflyMasterTier.Technique,"Defensa","spell-intercept","PARRY","REFLEJO TOTAL","SAO Spell Blast"),
            N("DEF-04","CONTRA PERFECTA","UNIVERSAL",HighflyMasterTier.Evolution,"Counter","perfect-counter","PARRY","APEX COUNTER","SLF Split-Second React / Exiled Parry"),
            N("DEF-05","SEÑUELO","UNIVERSAL",HighflyMasterTier.Technique,"Evasión","afterimage-decoy","DERIVA","ECO VORAZ","SLF Mirror of Fading / Exiled Doppel Illusion"),
            N("DEF-06","ECO VORAZ","UNIVERSAL",HighflyMasterTier.Evolution,"Counter","decoy-blink-counter","SEÑUELO","APEX COUNTER","HIGHFLY synthesis",true),

            // ONE-HAND / RAPIER / DUAL SWORD
            N("SWD-01","CORTE HORIZONTAL","ESPADA 1M",HighflyMasterTier.Base,"Daño","horizontal-slash","-","CUADRADO HORIZONTAL","SAO Horizontal"),
            N("SWD-02","CORTE VERTICAL","ESPADA 1M",HighflyMasterTier.Base,"Daño","vertical-slash","-","ARCO VERTICAL","SAO Vertical"),
            N("SWD-03","CORTE DIAGONAL","ESPADA 1M",HighflyMasterTier.Base,"Daño","diagonal-slash","-","CADENA TÉCNICA","SAO Slant"),
            N("SWD-04","SALTO SÓNICO","ESPADA 1M",HighflyMasterTier.Technique,"Gap closer","leap-slash","CORTE VERTICAL","VORPAL","SAO Sonic Leap"),
            N("SWD-05","VORPAL","ESPADA 1M",HighflyMasterTier.Evolution,"Commit","long-thrust-recovery","SALTO SÓNICO","APEX ESPADA","SAO Vorpal Strike"),
            N("RAP-01","LINEAL","RAPIER",HighflyMasterTier.Base,"Precisión","single-thrust","-","LLUVIA ESTELAR","SAO Linear / Asuna"),
            N("RAP-02","LLUVIA ESTELAR","RAPIER",HighflyMasterTier.Technique,"Multi-hit","precision-thrust-chain","LINEAL","ROSARIO","SAO Star Splash / Asuna"),
            N("RAP-03","PENETRACIÓN LUMINOSA","RAPIER",HighflyMasterTier.Evolution,"Gap closer","light-thrust-dash","LINEAL","ROSARIO","SAO Flashing Penetrator"),
            N("RAP-04","ROSARIO","RAPIER",HighflyMasterTier.Apex,"Weakpoint","cross-thrust-11hit","LLUVIA ESTELAR","TRANSCENDENT","SAO Yuuki Mother's Rosario"),
            N("DUAL-01","CORTE GEMELO","DOBLE ESPADA",HighflyMasterTier.Base,"Combo","alternating-double-cut","-","DANZA GEMELA","SAO Dual Blades + HIGHFLY"),
            N("DUAL-02","DANZA GEMELA","DOBLE ESPADA",HighflyMasterTier.Technique,"Combo","timed-double-chain","CORTE GEMELO","DANZA FANTASMA","HIGHFLY",true),
            N("DUAL-03","SKILL CONNECT","DOBLE ESPADA",HighflyMasterTier.Mastery,"Sistema","cancel-link","DOMINIO DE ARMA","ECLIPSE","SAO Skill Connect"),
            N("DUAL-04","DANZA FANTASMA","DOBLE ESPADA",HighflyMasterTier.Fusion,"Combo+movilidad","blink-multicut","DANZA GEMELA + PASO FANTASMA","ECLIPSE","HIGHFLY",true),
            N("DUAL-05","ECLIPSE","DOBLE ESPADA",HighflyMasterTier.Apex,"Execution","long-multihit-finisher","SKILL CONNECT + DANZA FANTASMA","TRANSCENDENT","SAO Eclipse / Starburst concept"),
            N("DUAL-06","DESGARRO ECLIPSE","DOBLE ESPADA",HighflyMasterTier.Evolution,"Execution","visible-seven-cut-x-confirm","DANZA FANTASMA","ECLIPSE","HIGHFLY",true),

            // DAGGER / ASSASSIN
            N("DAG-01","PUNTO VITAL","DOBLE DAGA",HighflyMasterTier.Base,"Assassin","weakpoint-strike","-","MUTILACIÓN","Solo Leveling Vital Points"),
            N("DAG-02","PERFORACIÓN ASESINA","DOBLE DAGA",HighflyMasterTier.Technique,"Assassin","unaware-bonus","PUNTO VITAL","MUTILACIÓN","SLF Assassin Pierce"),
            N("DAG-03","MUTILACIÓN","DOBLE DAGA",HighflyMasterTier.Evolution,"Execution","multi-weakpoint","PUNTO VITAL","APEX DAGAS","Solo Leveling Mutilation"),
            N("DAG-04","LLUVIA DE DAGAS","DOBLE DAGA",HighflyMasterTier.Evolution,"Control","multi-angle-daggers","PUNTO VITAL","APEX DAGAS","Solo Leveling Dagger Rush"),

            // SWORD + SHIELD / HEAVY
            N("SHD-01","GUARDIA","ESPADA+ESCUDO",HighflyMasterTier.Base,"Defensa","guard","-","PROTECT","Exiled Heavy Knight"),
            N("SHD-02","PROTECT","ESPADA+ESCUDO",HighflyMasterTier.Technique,"Barrier","temporary-damage-buffer","GUARDIA","MURALLA","Exiled Protect"),
            N("SHD-03","GOLPE DE ESCUDO","ESPADA+ESCUDO",HighflyMasterTier.Technique,"Control","shield-bash-knockback","GUARDIA","RAMPART","Exiled Shield Bash"),
            N("SHD-04","RAMPART","ESPADA+ESCUDO",HighflyMasterTier.Evolution,"Counter","defense-to-counter","GUARDIA + PARRY","MURALLA","Exiled Rampart Return/Reversal"),
            N("SHD-05","MURALLA DE RETORNO","ESPADA+ESCUDO",HighflyMasterTier.Apex,"Reflect","physical-wall-perfect-reflect","RAMPART + PROTECT","FORTRESS","HIGHFLY / Exiled",true),
            N("SHD-06","ESCUDO VITAL","ESPADA+ESCUDO",HighflyMasterTier.Evolution,"Barrier","hp-to-shield","PROTECT","FORTRESS","Exiled Life Shield"),
            N("HVY-01","RUPTURA PESADA","GRAN ESPADA",HighflyMasterTier.Base,"Break","heavy-stagger","-","ONDA DE RUPTURA","Psyger-0 / heavy weapon"),
            N("HVY-02","ONDA DE RUPTURA","GRAN ESPADA",HighflyMasterTier.Evolution,"AoE","ground-shockwave","RUPTURA PESADA","APEX GRAN ESPADA","Psyger-0 / Solo Thomas Collapse"),

            // SPEAR / FISTS / KATANA / BOW
            N("SPR-01","PERFORACIÓN ESPIRAL","LANZA",HighflyMasterTier.Base,"Pierce","drill-pierce","-","PERFORACIÓN LUMINOSA","SLF Screw/Drill Piercer"),
            N("SPR-02","PERFORACIÓN LUMINOSA","LANZA",HighflyMasterTier.Evolution,"Pierce","weakpoint-drill","PERFORACIÓN ESPIRAL","APEX LANZA","SLF Glowing Pierce"),
            N("FST-01","PUÑO ESPIRITUAL","PUÑOS",HighflyMasterTier.Base,"Stance","energy-fist-stance","-","PUÑO IMPACTO","SLF Oikatzo"),
            N("FST-02","PUÑO IMPACTO","PUÑOS",HighflyMasterTier.Technique,"Break","pile-bunker-punch","PUÑO ESPIRITUAL","APEX PUÑOS","SLF Oikatzo"),
            N("FST-03","COLAPSO","PUÑOS",HighflyMasterTier.Evolution,"AoE","ground-punch-shockwave","PUÑO IMPACTO","APEX PUÑOS","Solo Leveling Thomas Andre"),
            N("KAT-01","IAI","KATANA",HighflyMasterTier.Base,"Precision","draw-cut","-","IAI COUNTER","SAO Katana / kendo family"),
            N("KAT-02","IAI COUNTER","KATANA",HighflyMasterTier.Evolution,"Counter","draw-parry-cut","IAI + PARRY","APEX KATANA","HIGHFLY synthesis"),
            N("BOW-01","DISPARO CARGADO","ARCO",HighflyMasterTier.Base,"Ranged","charge-shot","-","PERFORADOR","SAO Sinon / SLF Rust"),
            N("BOW-02","LLUVIA DE FLECHAS","ARCO",HighflyMasterTier.Technique,"AoE","arrow-rain","DISPARO CARGADO","APEX ARCO","SAO bow variants / SLF Rust"),
            N("BOW-03","FLECHA MÁGICA","ARCO",HighflyMasterTier.Evolution,"Elemental","enchanted-projectile","DISPARO CARGADO","APEX ARCO","SLF Rust Magic Archer"),

            // REMOTE / TRANSFORMING WEAPONS
            N("REM-01","ESPADAS SIRVIENTES","ESPADA",HighflyMasterTier.Mastery,"Remote","remote-swords","DOMINIO ESPADA","ORQUESTA","SLF Psyger-100 Sworvant"),
            N("REM-02","ORQUESTA DE ESPADAS","ESPADA",HighflyMasterTier.Apex,"Remote","multi-sword-orbit-command","ESPADAS SIRVIENTES","TRANSCENDENT","SLF Psyger-100"),
            N("REM-03","PÉTALOS ARMADOS","ESPADA",HighflyMasterTier.Apex,"Remote","weapon-fragment-swarm","DOMINIO ESPADA","TRANSCENDENT","SAO Alice Fragrant Olive"),
            N("WHIP-01","ESPADA LÁTIGO","HÍBRIDA",HighflyMasterTier.Evolution,"Control","sword-whip-ensnare","DOMINIO ESPADA","APEX HÍBRIDA","Solo Leveling Bellion"),

            // CONTROL / MAGIC / STATUS
            N("CTL-01","AUTORIDAD","UNIVERSAL",HighflyMasterTier.Technique,"Control","telekinetic-pull","-","MANO DEL SOBERANO","Solo Leveling Ruler's Authority"),
            N("CTL-02","MANO DEL SOBERANO","UNIVERSAL",HighflyMasterTier.Evolution,"Control","visible-hand-grab-pull","AUTORIDAD","APEX CONTROL","HIGHFLY visual synthesis",true),
            N("CTL-03","GRILLETE DE SOMBRA","UNIVERSAL",HighflyMasterTier.Technique,"Root","shadow-root","-","GRILLETE ABISAL","Exiled Shadow Stomp / HIGHFLY",true),
            N("CTL-04","GRILLETE ABISAL","UNIVERSAL",HighflyMasterTier.Evolution,"Multi-control","multi-root-converge","GRILLETE DE SOMBRA","APEX CONTROL","HIGHFLY",true),
            N("CTL-05","POZO GRAVITATORIO","MAGIA",HighflyMasterTier.Evolution,"Control","gravity-well","MAGIA","APEX MAGIA","SLF Mold / Solo Tusk"),
            N("CTL-06","MIEDO","MAGIA",HighflyMasterTier.Technique,"Debuff","fear","-","TERROR","Solo Leveling Bloodlust / Dragon Fear"),
            N("CTL-07","DESARME","UNIVERSAL",HighflyMasterTier.Technique,"Debuff","attack-down-on-hit","-","APEX DEBUFF","Exiled Disarm"),
            N("CTL-08","LENTITUD","MAGIA",HighflyMasterTier.Technique,"Debuff","slow","-","APEX DEBUFF","Exiled Mabel"),
            N("MAG-01","FUEGO MODELADO","MAGIA",HighflyMasterTier.Base,"Elemental","shapeable-fire","-","INFERNO","Solo Leveling Choi Jong-In"),
            N("MAG-02","PRISIÓN DE HIELO","MAGIA",HighflyMasterTier.Evolution,"Control","freeze-bind","-","DOMINIO GLACIAL","SAO Eugeo"),
            N("MAG-03","DOMINIO GLACIAL","MAGIA",HighflyMasterTier.Apex,"Domain","area-freeze-control","PRISIÓN DE HIELO","TRANSCENDENT","SAO Eugeo"),
            N("MAG-04","CORTE DE LUZ","ESPADA",HighflyMasterTier.Evolution,"Elemental","light-blade","DOMINIO ESPADA","APEX LUZ","Solo Cha Hae-In Sword of Light"),
            N("MAG-05","DANZA DE LUZ","ESPADA",HighflyMasterTier.Fusion,"Speed","attack-speed-dance","CORTE DE LUZ","APEX LUZ","Solo Cha Hae-In Sword Dance"),
            N("MAG-06","MARCA TEMPORAL","ESPADA",HighflyMasterTier.Apex,"Temporal","delayed-position-strike","DOMINIO ESPADA","TRANSCENDENT","SAO Bercouli"),
            N("MAG-07","REFUERZO","UNIVERSAL",HighflyMasterTier.Evolution,"Transformation","armor-body-enhance","-","FORMA ESPIRITUAL","Solo Thomas Andre / Ruler vessels"),
            N("MAG-08","BESTIALIZACIÓN","PUÑOS",HighflyMasterTier.Apex,"Transformation","beast-transform","-","TRANSCENDENT","Solo Baek Yoonho"),
            N("MAG-09","GIGANTIFICACIÓN","UNIVERSAL",HighflyMasterTier.Apex,"Transformation","giant-form","REFUERZO","TRANSCENDENT","Solo Thomas Andre"),

            // SHADOW / SUMMON
            N("SHW-01","EXTRACCIÓN","SOMBRA",HighflyMasterTier.Base,"Summon","raise-defeated-shadow","-","LLAMADO","Solo Leveling Shadow Extraction"),
            N("SHW-02","LLAMADO DE LA SOMBRA","SOMBRA",HighflyMasterTier.Technique,"Summon","summon-pair","EXTRACCIÓN","VÍNCULO UMBRÍO","Solo Leveling + HIGHFLY",true),
            N("SHW-03","VÍNCULO UMBRÍO","SOMBRA",HighflyMasterTier.Fusion,"Summon+drain","shadow-coordinated-drain","LLAMADO + PACTO VITAL","JUICIO","HIGHFLY",true),
            N("SHW-04","JUICIO DE LA SOMBRA","SOMBRA",HighflyMasterTier.Apex,"Execution","summon-execution","VÍNCULO UMBRÍO","TRANSCENDENT","HIGHFLY",true),
            N("SHW-05","DOMINIO DEL MONARCA","SOMBRA",HighflyMasterTier.Apex,"Aura","summon-domain-buff","MAESTRÍA SOMBRA","TRANSCENDENT","Solo Leveling Monarch's Domain"),

            // RISK / BUFF / EVOLUTION SYSTEMS
            N("RSK-01","PACTO VITAL","UNIVERSAL",HighflyMasterTier.Technique,"Sustain","damage-to-life-loop","-","DOMINIO VITAL","HIGHFLY / risk-sustain",true),
            N("RSK-02","DOMINIO VITAL","UNIVERSAL",HighflyMasterTier.Evolution,"Domain","persistent-drain-sustain","PACTO VITAL","APEX VITAL","HIGHFLY",true),
            N("RSK-03","NITRO","UNIVERSAL",HighflyMasterTier.Technique,"Risk buff","hp-sacrifice-speed-power","-","CLÍMAX","SLF Nitro Gain"),
            N("RSK-04","SOBRECALENTAR","UNIVERSAL",HighflyMasterTier.Evolution,"Ramp","time-gated-buff-penalty","-","CLÍMAX","SLF Overheat"),
            N("RSK-05","CLÍMAX","UNIVERSAL",HighflyMasterTier.Apex,"Underdog","risk-scaling-buff","NITRO + ESPÍRITU","TRANSCENDENT","SLF Climax Boost"),
            N("RSK-06","DRAGÓN MORIBUNDO","UNIVERSAL",HighflyMasterTier.Evolution,"Low HP","low-hp-atk-agi","-","GOLPE DE LOCURA","Exiled Half-Dead Savage Dragon"),
            N("RSK-07","VIGOR IMPLACABLE","UNIVERSAL",HighflyMasterTier.Evolution,"Conversion","defense-to-attack","-","GOLPE DE LOCURA","Exiled Relentless Vigor"),
            N("RSK-08","GOLPE DE LOCURA","UNIVERSAL",HighflyMasterTier.Apex,"Finisher","critical-hp-finisher","DRAGÓN MORIBUNDO + VIGOR","TRANSCENDENT","Exiled Scorched Madness Strike"),
            N("RSK-09","FORTUNA","UNIVERSAL",HighflyMasterTier.Mastery,"Critical","luck-critical-engine","-","APEX FORTUNA","Exiled Luce / SLF Airgetlam concept"),
            N("RSK-10","DADO CRÍTICO","DAGA",HighflyMasterTier.Evolution,"Critical","risk-random-crit","FORTUNA","APEX FORTUNA","Exiled Luce Dice Thrust"),

            // APEX PASS — SHANGRI-LA / SAO / RAGNAROK / EXILED DEEP AUDIT
            N("MOV-09","GRAVEDAD CERO","UNIVERSAL",HighflyMasterTier.Evolution,"Traversal","gravity-vector-surface-run","PASO DE MURO","APEX MOVILIDAD","SLF Gravity Zero",true),
            N("MOV-10","DRIFT DE FÓRMULA","UNIVERSAL",HighflyMasterTier.Evolution,"Positioning","target-orbit-blindside","DERIVA","APEX MOVILIDAD","SLF Formula Drift",true),
            N("PER-01","VISTA DEL INSTANTE","UNIVERSAL",HighflyMasterTier.Mastery,"Percepción","accelerated-perception-window","PRECISIÓN","APEX PERCEPCIÓN","SLF Moment Sight",true),
            N("PER-02","PARALELISMO","MAGIA",HighflyMasterTier.Apex,"Casting","parallel-cast-action","RESERVA","TRANSCENDENT","SLF Deep Slaughter parallel processing"),
            N("SYS-04","RESERVA ARCANA","MAGIA",HighflyMasterTier.Mastery,"Casting","stored-delayed-spell","MAGIA","PARALELISMO","SLF Reserve Spell",true),
            N("RSK-11","EL LOCO","UNIVERSAL",HighflyMasterTier.Apex,"Rule modifier","cooldown-for-debuff-risk","MAESTRÍA RIESGO","TRANSCENDENT","SLF The Fool",true),
            N("RSK-12","MASACRE SIN LÍMITE","DOBLE ESPADA",HighflyMasterTier.Apex,"Endurance","stamina-bound-continuous-assault","SKILL CONNECT","TRANSCENDENT","SLF Boundless Massacre",true),
            N("FST-04","IMPACTO DOBLE","PUÑOS",HighflyMasterTier.Evolution,"Detonation","mark-then-detonate","PUÑO ESPIRITUAL","APEX PUÑOS","SLF Oikatzo Dual Impact"),
            N("FST-05","SCRAP & BUILD","PUÑOS",HighflyMasterTier.Apex,"Sacrifice buff","destroy-weapon-to-stats","MAESTRÍA PUÑOS","TRANSCENDENT","SLF Oikatzo",true),
            N("DAG-05","ARTE DEL SACRIFICIO","DAGA",HighflyMasterTier.Apex,"Sacrifice burst","weapon-sacrifice-explosion","MAESTRÍA DAGA","TRANSCENDENT","SLF Victim Arts",true),
            N("REM-04","SEVEN SINKER","ESPADA",HighflyMasterTier.Apex,"Formation control","seven-sword-gravity-prison","ESPADAS SIRVIENTES","TRANSCENDENT","SLF Psyger-100",true),
            N("REM-05","SYNCHRONIST","ESPADA",HighflyMasterTier.Fusion,"Remote combo","multi-sword-synchronized-strike","ESPADAS SIRVIENTES + LINK","SEVEN SINKER","SLF Sworvant formation"),
            N("REM-06","FORMACIÓN ROMPIBLE","ESPADA",HighflyMasterTier.Mastery,"Counterplay","formation-break-condition","ESPADAS SIRVIENTES","SEVEN SINKER","SLF Seven Sinker counterplay"),
            N("MAG-10","LIBERACIÓN DE MEMORIA","ESPADA",HighflyMasterTier.Apex,"Weapon release","weapon-memory-release","DOMINIO ESPADA","TRANSCENDENT","SAO Alicization Divine Object release",true),
            N("MAG-11","RECOLECCIÓN","ESPADA",HighflyMasterTier.Transcendent,"Weapon release","memory-recollection-overdrive","LIBERACIÓN DE MEMORIA","ÚNICA","SAO Alicization recollection concept"),
            N("MAG-12","CORTE FUTURO","ESPADA",HighflyMasterTier.Apex,"Temporal","future-position-delayed-cut","MARCA TEMPORAL","TRANSCENDENT","SAO Bercouli",true),
            N("MAG-13","CORTE DEL PASADO","ESPADA",HighflyMasterTier.Transcendent,"Temporal","past-state-interaction","CORTE FUTURO","ÚNICA","SAO Bercouli concept"),
            N("BND-01","POSESIÓN BESTIAL","UNIVERSAL",HighflyMasterTier.Fusion,"Bond","entity-fusion-transformation","VÍNCULO","APEX BOND","Solo Leveling Ragnarok Beast Possession",true),
            N("BND-02","GOLPE DEMONÍACO","PUÑOS",HighflyMasterTier.Fusion,"Bond charge","charged-bond-finisher","VÍNCULO","APEX BOND","Solo Leveling Ragnarok Demon Strike",true),
            N("BND-03","ARMAMENTO ESPIRITUAL","SOMBRA",HighflyMasterTier.Fusion,"Summon enhance","sacrifice-spirit-to-summon-buff","VÍNCULO + SOMBRA","APEX BOND","Solo Leveling Ragnarok Spirit Armament",true),
            N("SHW-06","CREACIÓN DE SOMBRA","SOMBRA",HighflyMasterTier.Evolution,"Utility","shadow-to-item-weapon","EXTRACCIÓN","APEX SOMBRA","Solo Leveling Ragnarok Shadow Creation",true),
            N("MAG-14","VENTISCA FRÍGIDA","MAGIA",HighflyMasterTier.Evolution,"Debuff","ice-storm-slow-attack-speed","PRISIÓN DE HIELO","DOMINIO GLACIAL","Solo Leveling Ragnarok Frigid Blizzard"),
            N("MAG-15","TORMENTA NEGRA","ESPADA",HighflyMasterTier.Fusion,"Elemental","wind-slashes-black-flame","STORM SLASH + FUEGO","APEX DRAGÓN","Solo Leveling Ragnarok Storm of Black Flames"),
            N("MAG-16","CUERPO DE HIERRO","PUÑOS",HighflyMasterTier.Apex,"Spiritual strike","physical-hit-spirit-body","REFUERZO","TRANSCENDENT","Solo Leveling Ragnarok Iron Body Technique"),
            N("CTL-09","ARCHIVO DE VENENOS","UNIVERSAL",HighflyMasterTier.Mastery,"Status","ingest-copy-poison","RESISTENCIA","APEX DEBUFF","Solo Leveling Ragnarok Poison"),
            N("MOV-11","PASO ÉLFICO","UNIVERSAL",HighflyMasterTier.Technique,"Stealth","trackless-movement","DERIVA","APEX SIGILO","Solo Leveling Ragnarok Elf's Steps"),
            N("SHD-07","VOTO DEL CABALLERO","ESPADA+ESCUDO",HighflyMasterTier.Mastery,"Stability","negate-berserk-penalty","GUARDIA","FORTRESS","Exiled Knight's Belief"),
            N("RSK-13","LOCURA CONTROLADA","UNIVERSAL",HighflyMasterTier.Fusion,"Low HP","berserk-with-penalty-cancel","GOLPE DE LOCURA + VOTO","TRANSCENDENT","Exiled Heavy Knight synthesis"),
            N("CRT-01","DOBLE ILUSIÓN","UNIVERSAL",HighflyMasterTier.Technique,"Clone","combat-doppelganger","SEÑUELO","APEX FORTUNA","Exiled Luce Doppellusion"),
            N("CRT-02","RÁFAGA ACROBÁTICA","DAGA",HighflyMasterTier.Evolution,"Mobility combo","acrobatics-multihit","TRICK WALL + FORTUNA","APEX FORTUNA","Exiled Luce Stunt Barrage"),

            // SYSTEM / FUSION APEX
            N("SYS-01","LINK","SISTEMA",HighflyMasterTier.Mastery,"Fusion","skill-link","MAESTRÍA","HIGH CONNECTION","SLF Skill Garden"),
            N("SYS-02","HIGH CONNECTION","SISTEMA",HighflyMasterTier.Apex,"Fusion","multi-skill-link","LINK","TRANSCENDENT","SLF high-level linking"),
            N("SYS-03","TRANSCENDENT","CAZADOR",HighflyMasterTier.Transcendent,"Unique","cross-discipline-apex","2+ APEX DOMINADAS","ÚNICA","HIGHFLY endgame synthesis")
        };

        public static HighflyMasterSkillNode Get(string id)
        {
            for (int i = 0; i < All.Length; i++)
                if (string.Equals(All[i].Id, id, StringComparison.Ordinal))
                    return All[i];

            return null;
        }

        private static HighflyMasterSkillNode N(
            string id, string name, string weapon, HighflyMasterTier tier,
            string role, string mechanicKey, string parent, string destination,
            string inspiration, bool runtimeReady = false)
        {
            return new HighflyMasterSkillNode(
                id, name, weapon, tier, role, mechanicKey,
                parent, destination, inspiration, runtimeReady);
        }
    }
}
