export type LabSkillStage = 'base' | 'evo' | 'mutation';
export type LabTarget = 'enemy' | 'position' | 'none' | 'self';

export interface LabSkillSeat {
  side:'main'|'heritage';
  label:string;
  ids:[string,string,string];
  names:[string,string,string];
  target:LabTarget;
  spec?:string;
  dummyDistance?:number;
  stealth?:boolean;
  combo?:number;
  executeWindow?:boolean;
  hurtSelf?:boolean;
  heritageUndead?:boolean;
  moontideMutation?:boolean;
}

export interface LabSkillPair {
  pair:string;
  principal:'warrior'|'rogue'|'mage'|'hunter';
  heritage:'paladin'|'warlock'|'shaman'|'druid';
  seats:LabSkillSeat[];
}

const fixed=(side:'main'|'heritage',label:string,id:string,name:string,target:LabTarget,spec?:string):LabSkillSeat=>({
  side,label,ids:[id,id,id],names:[name,name,name],target,...(spec?{spec}:{})
});

export const HIGHFLY_LABSKILL_LOADOUTS: Record<string,LabSkillPair> = {
  warrior:{pair:'WARRIOR + PALADIN',principal:'warrior',heritage:'paladin',seats:[
    {side:'main',label:'S1',ids:['heroic_leap','hf_demolishing_leap_01','hf_ascending_cataclysm_01'],names:['Salto Heroico','Salto Demoledor','Cataclismo Ascendente'],target:'position'},
    {side:'main',label:'S2',ids:['whirlwind','hf_cutting_whirlwind_01','hf_colossus_tempest_01'],names:['Torbellino','Torbellino Cortante','Tempestad del Coloso'],target:'enemy',spec:'fury',dummyDistance:3.5},
    {side:'main',label:'S3',ids:['faultline','hf_seismic_fault_01','hf_world_fracture_01'],names:['Falla','Falla Sísmica','Fractura del Mundo'],target:'none',spec:'prot',dummyDistance:4},
    {side:'main',label:'S4',ids:['execute','hf_bloody_verdict_01','hf_kings_end_01'],names:['Ejecución','Veredicto Sangriento','Fin del Rey'],target:'enemy',executeWindow:true,dummyDistance:2.5},
    fixed('main','S5','charge','Onrush','enemy'),
    {side:'heritage',label:'S6',ids:['hf_wp_consecration_01','hf_wp_radiant_sanctuary_01','hf_wp_dawn_domain_01'],names:['Tierra Consagrada','Santuario Radiante','Dominio del Alba'],target:'none',dummyDistance:3},
    {side:'heritage',label:'S7',ids:['hf_wp_valkyrs_calling_01','hf_wp_valkyr_descent_01','hf_wp_divine_descent_01'],names:['Llamado de Valquiria','Descenso de Valquiria','Descenso Divino'],target:'enemy',dummyDistance:10},
    {side:'heritage',label:'S8',ids:['hf_wp_aegis_first_dawn_01','hf_wp_dawn_aegis_01','hf_wp_unbreakable_dawn_01'],names:['Égida del Primer Alba','Égida del Alba','Amanecer Inquebrantable'],target:'none',hurtSelf:true},
    fixed('heritage','S9','hf_lab_wp_sunward_disc_01','Disco Solar','enemy'),
    fixed('heritage','S10','hf_lab_wp_final_edict_01','Edicto Final','enemy'),
  ]},
  rogue:{pair:'ROGUE + WARLOCK',principal:'rogue',heritage:'warlock',seats:[
    {side:'main',label:'S1',ids:['ambush','hf_shadow_hunt_01','hf_eclipse_mortal_01'],names:['Emboscada','Cacería Sombría','Eclipse Mortal'],target:'enemy',spec:'subtlety',stealth:true,dummyDistance:2.5},
    {side:'main',label:'S2',ids:['eviscerate','hf_cruel_finish_01','hf_last_whisper_01'],names:['Remate','Remate Cruel','Último Susurro'],target:'enemy',combo:5,dummyDistance:2.5},
    {side:'main',label:'S3',ids:['vanish','hf_shadow_vanish_01','hf_absolute_void_01'],names:['Desvanecer','Desvanecer Sombrío','Vacío Absoluto'],target:'none',spec:'subtlety'},
    {side:'main',label:'S4',ids:['shadowstep','hf_umbral_step_01','hf_abyss_step_01'],names:['Paso Sombrío','Paso Umbrío','Paso del Abismo'],target:'enemy',spec:'subtlety',stealth:true,dummyDistance:10},
    fixed('main','S5','sinister_strike','Wicked Slash','enemy'),
    {side:'heritage',label:'S6',ids:['hf_rw_reaping_command_01','hf_rw_unholy_dominion_01','hf_rw_march_of_dead_01'],names:['Mandato de Siega','Dominio Profano','Marcha de los Muertos'],target:'enemy',heritageUndead:true,dummyDistance:7},
    {side:'heritage',label:'S7',ids:['hf_rw_evil_eye_01','hf_rw_abyss_gaze_01','hf_rw_eye_of_end_01'],names:['Ojo Maldito','Mirada del Abismo','Ojo del Fin'],target:'enemy',dummyDistance:8},
    {side:'heritage',label:'S8',ids:['hf_rw_umbral_anchor_01','hf_rw_umbral_return_01','hf_rw_point_no_return_01'],names:['Ancla Umbral','Retorno Umbrío','Punto de No Retorno'],target:'none'},
    fixed('heritage','S9','hf_lab_rw_gloom_bolt_01','Proyectil Sombrío','enemy'),
    fixed('heritage','S10','hf_lab_rw_consume_01','Consumir','enemy'),
  ]},
  mage:{pair:'MAGE + SHAMAN',principal:'mage',heritage:'shaman',seats:[
    {side:'main',label:'S1',ids:['pyroblast','hf_ms_crimson_pyrelance_01','hf_ms_crimson_rain_01'],names:['Lanza Pírica','Lanza Pírica Carmesí','Lluvia Carmesí'],target:'enemy',spec:'fire',dummyDistance:8},
    {side:'main',label:'S2',ids:['meteor','hf_ms_fallen_star_01','hf_ms_celestial_extinction_01'],names:['Meteorito','Estrella Caída','Extinción Celeste'],target:'position',spec:'fire',dummyDistance:8},
    {side:'main',label:'S3',ids:['arcane_missiles','hf_ms_aether_storm_01','hf_ms_thousand_celestial_darts_01'],names:['Dardos Etéreos','Tormenta de Éter','Mil Dardos Celestes'],target:'enemy',spec:'arcane',dummyDistance:8},
    {side:'main',label:'S4',ids:['dragons_breath','hf_ms_dragon_breath_01','hf_ms_dragon_king_breath_01'],names:['Aliento Dracónico','Aliento del Dragón','Aliento del Rey Dragón'],target:'none',spec:'fire',dummyDistance:8},
    fixed('main','S5','fireball','Cinderbolt','enemy','fire'),
    {side:'heritage',label:'S6',ids:['hf_ms_faultwake_01','hf_ms_primordial_cataclysm_01','hf_ms_storms_end_01'],names:['Falla','Cataclismo Primordial','Fin de la Tormenta'],target:'position',spec:'fire',dummyDistance:8},
    {side:'heritage',label:'S7',ids:['hf_ms_arc_bolt_01','hf_ms_overcharged_bolt_01','hf_ms_judgment_sky_01'],names:['Rayo Arcano','Rayo Sobrecargado','Juicio del Cielo'],target:'enemy',spec:'fire',dummyDistance:8},
    {side:'heritage',label:'S8',ids:['hf_ms_magma_burst_01','hf_ms_volcanic_core_01','hf_ms_primordial_eruption_01'],names:['Explosión Magmática','Núcleo Volcánico','Erupción Primordial'],target:'enemy',spec:'fire',dummyDistance:8},
    fixed('heritage','S9','hf_ms_cinder_jolt_01','Cinder Jolt','enemy','fire'),
    fixed('heritage','S10','hf_lab_ms_earthen_jolt_01','Sacudida Telúrica','enemy','fire'),
  ]},
  hunter:{pair:'HUNTER + DRUID',principal:'hunter',heritage:'druid',seats:[
    {side:'main',label:'S1',ids:['frostjaw_trap','hf_hunter_prison_01','hf_hd_boreal_domain_01'],names:['Trampa Colmillo Helado','Prisión del Cazador','Dominio Boreal'],target:'enemy',dummyDistance:6},
    {side:'main',label:'S2',ids:['rapid_fire','hf_hd_frenzied_salvo_01','hf_hd_predator_rain_01'],names:['Disparo Frenético','Salva Frenética','Lluvia del Depredador'],target:'enemy',spec:'marksmanship',dummyDistance:12},
    {side:'main',label:'S3',ids:['volley','hf_hd_arrow_storm_01','hf_hd_hunters_judgment_01'],names:['Lluvia de Flechas','Tormenta de Flechas','Juicio del Cazador'],target:'position',dummyDistance:8},
    {side:'main',label:'S4',ids:['stampede','hf_hd_wild_pack_01','hf_hd_kings_hunt_01'],names:['Estampida','Manada Salvaje','Cacería del Rey'],target:'enemy',spec:'beast_mastery',dummyDistance:12},
    fixed('main','S5','arcane_shot','Fell Shot','enemy'),
    {side:'heritage',label:'S6',ids:['hf_hd_moonseed_01','hf_hd_crescent_seed_01','hf_hd_lunar_wave_01'],names:['Semilla Lunar','Semilla Creciente','Oleada Lunar'],target:'enemy',dummyDistance:12,moontideMutation:true},
    {side:'heritage',label:'S7',ids:['hf_hd_lunar_tempest_01','hf_hd_lunar_eclipse_01','hf_hd_eternal_night_01'],names:['Tormenta Lunar','Eclipse Lunar','Noche Eterna'],target:'enemy',dummyDistance:12},
    {side:'heritage',label:'S8',ids:['hf_hd_galeheart_01','hf_hd_wild_hurricane_01','hf_hd_natures_wrath_01'],names:['Corazón del Vendaval','Huracán Salvaje','Ira de la Naturaleza'],target:'position',dummyDistance:8},
    fixed('heritage','S9','hf_lab_hd_wildbolt_01','Wildbolt','enemy'),
    fixed('heritage','S10','hf_lab_hd_skyfall_01','Skyfall','enemy'),
  ]},
};

export const HIGHFLY_LABSKILL_STAGE_INDEX:Record<LabSkillStage,number>={base:0,evo:1,mutation:2};
