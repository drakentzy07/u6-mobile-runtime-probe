# HIGHFLY — MASTER SKILL AUDIT v0.8

## Regla maestra

Este catálogo no intenta conservar cada nombre de ataque de las obras de referencia. Conserva cada **mecánica jugable distinta**. Si dos skills hacen lo mismo (por ejemplo gap-closer + corte), una se vuelve la base canónica y la otra queda como evolución, variante visual o se descarta.

Cada skill HIGHFLY debe tener: fantasía, función, firma mecánica, disciplina/arma, input, startup, movimiento, targeting, hits, impacto, recurso, riesgo, cancel window, cooldown, evolución, fusión, animación, VFX, SFX, cámara, presupuesto de performance y destino APEX.

## Fuentes de mecánicas auditadas

- Shangri-La Frontier: Sunraku, Oikatzo, Psyger-0, Psyger-100, Rust, Mold, Animalia y el sistema Skill Garden / Skill Linking. Aporta movilidad, evolución/link, riesgo/recompensa, puños, remote swords, magic archer y buffs/debuffs.
- Sword Art Online: Kirito, Asuna, Yuuki, Eugeo, Alice, Bercouli, Sinon y familias de espada/rapier/dual. Aporta Sword Skills, Skill Connect, cadenas multi-hit, precisión, weapon-memory/area-control y ataques temporales.
- Solo Leveling: Sung Jinwoo, Cha Hae-In, Thomas Andre, Liu Zhigang, Baek Yoonho, Choi Jong-In, Go Gunhee, Tusk, Beru, Bellion e Igris. Aporta telequinesis, sombras, dagas, transformaciones, control, magia elemental y armas híbridas.
- The Exiled Heavy Knight Knows How to Game the System: Elymas, Luce y Mabel. Aporta parry/reflect, shield play, HP sacrifice, DEF→ATK, low-HP builds, acrobacia/clones/critical y debuffs.

## Disciplinas HIGHFLY

El cazador es permanente. El arma cambia básicos, animaciones y rama compatible, no la identidad del personaje.

- Espada 1M / Rapier
- Doble espada
- Doble daga
- Espada + escudo
- Gran espada
- Lanza
- Puños
- Katana
- Arco
- Armas híbridas/remotas

Las skills universales (movilidad, telequinesis, sombras, curación/buffs, algunas defensas) pueden ocupar slots aunque cambie el arma.

## Pirámide

BASE → TÉCNICA → MAESTRÍA → EVOLUCIÓN → FUSIÓN → APEX → TRANSCENDENT

Una APEX debe resumir mecánicamente lo aprendido en su rama; no es la misma skill con más partículas.

## Deduplicación

La clave `MechanicKey` de `HighflyMasterSkillTree.cs` identifica la firma de jugabilidad. Dos botones finales no deben compartir la misma firma salvo que uno sea explícitamente una evolución/fusión del otro.

## v0.8 runtime

Este run no pretende implementar visualmente los ~80 nodos de una vez. Implementa la **arquitectura maestra**, browser scroll y fixes sistémicos, manteniendo las 13 skills runtime de v0.7 como banco de prueba visual. Los demás nodos quedan definidos para construirlos por tandas sin volver a diseñar el árbol.
