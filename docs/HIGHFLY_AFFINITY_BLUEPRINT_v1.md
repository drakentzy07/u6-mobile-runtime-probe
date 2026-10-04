# HIGHFLY — AFFINITY BLUEPRINT v1

## Regla central
Cada clase conserva identidad, recursos, armas, specs, targeting, animaciones y mecánicas nativas de ClaudeCraft. Una afinidad NO enseña una skill de otra clase y NO aumenta gratis el poder total: redistribuye el presupuesto de la skill entre impacto, DoT, control, proc, movilidad o zona.

BASE siempre permanece disponible y competitiva.

Afinidades v1:
- FIRE: daño sostenido, burn, explosión, zona.
- FROST: control, slow/root/freeze buildup, defensa espacial.
- LIGHTNING: burst, shock, interrupción, velocidad/proc. Internamente puede rutear como Nature cuando Claude no tiene school Lightning.

## Orden de implementación
- TANDA 1: Warrior + Rogue + Mage
- TANDA 2: Paladin + Hunter + Shaman
- TANDA 3: Warlock + Druid + Priest

## TANDA 1

### WARRIOR
Identidad: presión frontal, impacto, arma, rage. Visual top: dual swords o sword+shield según build.
Receptores:
1. Heroic Leap — BASE salto/impacto; FIRE burn de aterrizaje; FROST slow/zona fría; LIGHTNING shock breve.
2. Whirlwind — BASE giro físico; FIRE giro incendiario con burn repartido; FROST corte circular que ralentiza; LIGHTNING pulsos rápidos con shock/proc.
3. Thunder Clap — BASE daño + attack-speed slow; FIRE onda de calor y burn corto; FROST control superior con daño menor; LIGHTNING trueno real con mini-interrupción.
4. Cleave — BASE barrido frontal; FIRE cicatriz ardiente; FROST sweep de control; LIGHTNING arco eléctrico corto entre objetivos.
5. Execute — receptor secundario posterior; no v1 inicial porque su gate <20% HP complica comparación A/B.

### ROGUE
Identidad: dual daggers, precisión, combo points, stealth, velocidad, ejecución.
Receptores:
1. Eviscerate — BASE finisher; FIRE herida cauterizante/DoT; FROST finisher que frena; LIGHTNING descarga burst según combo.
2. Ambush — BASE golpe desde stealth/detrás; FIRE apertura con burn; FROST apertura que ralentiza; LIGHTNING apertura instantánea con shock breve.
3. Sinister Strike — BASE builder; FIRE filo incendiario; FROST filo de control; LIGHTNING builder veloz con proc.
4. Rupture — BASE bleed; FIRE transforma parte del bleed en burn; FROST herida fría con slow sostenido; LIGHTNING pulsos eléctricos periódicos.
5. Shadowstep — NO receptor elemental inicial; conservar movilidad pura para no contaminar su identidad.

### MAGE
Identidad: caster puro, INT/PER, control de escuela y espacio.
Receptores:
1. Fireball — BASE Cinderbolt; FIRE más burn/menos impacto relativo; FROST proyectil frío con slow; LIGHTNING bolt rápido con shock.
2. Frostbolt — BASE Rimelance; FIRE variante térmica con burn; FROST control reforzado sin daño gratis; LIGHTNING descarga con micro-interrupción.
3. Meteor — BASE Fire Skystone; FIRE zona de ignición; FROST impacto glacial + control; LIGHTNING caída tormentosa + shock AoE.
4. Arcane Missiles — BASE canal Arcane; FIRE proyectiles combustivos; FROST dardos que acumulan slow; LIGHTNING darts con proc/chain ligero.
5. Arcane Explosion — expansión posterior; excelente receptor AoE pero spec-only.

## TANDA 2

### PALADIN
Identidad: melee sagrado, defensa, juicio, support.
Receptores:
- Crusader Strike
- Consecration
- Hammer of Wrath
- Dawnfall
- Sunward Disc
FIRE = llama/jucio ardiente; FROST = control protector; LIGHTNING = castigo celestial/shock.

### HUNTER
Identidad: ranged physical, traps, precision, pets/field control.
Receptores:
- Volley
- Frostjaw Trap
- Rapid Fire
- Arcane Shot
- Shrapnel Charge
FIRE = flechas/bombas incendiarias; FROST = trap/control; LIGHTNING = shots/procs/chain.

### SHAMAN
Identidad: naturaleza elemental, melee/caster híbrido.
Receptores:
- Lightning Bolt
- Lava Burst
- Stormstrike
- Earth Shock
- Earthquake
FIRE = magma/lava; FROST = control elemental; LIGHTNING = tormenta/proc/chain.

## TANDA 3

### WARLOCK
Identidad: corrupción, sombras, DoT, destrucción, invocación.
Receptores:
- Shadow Bolt
- Immolate
- Chaos Bolt
- Rain of Fire
- Soul Lance
FIRE = hellfire/burn; FROST = curse control; LIGHTNING = dark storm/shock. Summons/corpses NO se vuelven simples imbues.

### DRUID
Identidad: naturaleza, luna, formas, adaptación.
Receptores:
- Wrath
- Starfire
- Moonfire
- Hurricane
- Wildwake
FIRE = solar/nature fury; FROST = lunar/glacial control; LIGHTNING = storm/nature burst. Forms no son receptores elementales directos.

### PRIEST
Identidad: fe, holy/shadow, control mental, soporte.
Receptores:
- Smite
- Holy Nova
- Mind Blast
- Mind Flay
FIRE = purga/luz ardiente; FROST = control/quietud; LIGHTNING = juicio/shock. Healing puro se audita aparte para no convertir todo en daño.

## Gates globales
1. meta.cls no cambia con afinidad.
2. Cast elemental no cambia spec.
3. Equipo y weapon style no cambian.
4. Recurso nativo permanece.
5. BASE permanece usable.
6. Sólo receptoras compatibles obtienen variantes.
7. SIM mantiene autoridad de daño/control.
8. VFX/SFX/animación nunca son autoridad.
9. Afinidad no aumenta STR/AGI/VIT/PER/INT.
10. Afinidad no es daño gratis.
11. Targeting y movilidad base se reutilizan.
12. BASE vs elemento debe poder compararse en el mismo dummy.
13. Elemento debe leerse visualmente sin mirar el botón cuando llegue el pass premium.
14. Nada proxy: si una variante no conserva la mecánica de la skill, no pasa.
