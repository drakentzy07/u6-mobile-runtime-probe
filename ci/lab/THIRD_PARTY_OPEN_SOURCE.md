# HIGHFLY LAB — Third-Party Open-Source References

This experimental Skill/Combat Lab intentionally keeps donor code and references isolated from the stable app architecture.

## AitorSimona/Traverser
Source: https://github.com/AitorSimona/Traverser
License: MIT
Copyright (c) 2021 Aitor Simona Bouzas

Used as an architectural reference for traversal abilities, environment-contact handling, jump/ledge/parkour separation, and predictive movement concepts. HIGHFLY keeps its existing PlayerController and Golden Camera.

## homemech/unity-pattern-combo
Source: https://github.com/homemech/unity-pattern-combo
License: MIT
Copyright (c) 2024 James Walsh aka homemech

Used as a reference for time-windowed input queues, combo pattern matching, and extensible combo-rule design. HIGHFLY continues to route actions through HighflyCombatCore.

## kr405/UnityAfterimageEffects
Source: https://github.com/kr405/UnityAfterimageEffects
License: MIT
Copyright (c) 2024 kr405

Used as a reference for pooled/queued afterimage rendering strategies suitable for repeated high-speed movement VFX.

## License text (MIT)

Permission is hereby granted, free of charge, to any person obtaining a copy
of the referenced software and associated documentation files (the "Software"),
to deal in the Software without restriction, including without limitation the
rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, subject to inclusion of the applicable copyright notice
and permission notice in copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.


## Dragon Souls donor vertical slice (v0.12)
- Source: btuhany/DragonSouls-Unity3D
- License: MIT (repository LICENSE; code attribution retained in HighflyArteSacrificioV012.cs)
- HIGHFLY use: throw / embed / curved return state-machine concepts and substantial control-flow adaptation.
- Not imported wholesale: DG.Tweening, donor-specific PlayerStateMachine, Sounds, Damage and third-party raw art are intentionally excluded.
- SubspaceHunter-SAO public skill code is used as a sequencing reference for projectile spawn / release / hit-feedback patterns; SAO/IP assets and mixed third-party raw packages are not committed.


## Donor Lab v2.3 Skill Vault references

### Sigil Combat / Sigil GAS
- Source: https://github.com/forestlii/sigil-combat
- License: MIT.
- HIGHFLY use: combat/ability architecture reference and verified sample mechanics (melee trace, ranged shot, Flash, charged Fireball).
- Integration rule: HIGHFLY CORE remains authoritative; Sigil is not installed wholesale.

### Dragon Souls
- Source: https://github.com/btuhany/DragonSouls-Unity3D
- License: MIT.
- HIGHFLY use: sword throw / embed / recall state-machine concepts.
- Third-party raw art/animation/audio from the donor is not redistributed.

### Ashwalker
- Source: https://github.com/HoleInWater/Ashwalker
- License: MIT.
- HIGHFLY use: force push/pull and haste/slow field mechanic study.
- v2.3 uses independent HIGHFLY visuals/runtime wiring.

### Adaptive Boss Arena
- Source: https://github.com/Shadow-46/adaptive-boss-arena
- License: MIT.
- HIGHFLY use: focus special, execution/posture-break presentation and hyper-armour mechanic study.
- v2.3 uses independent HIGHFLY visuals/runtime wiring.

### SubspaceHunter-SAO
- Source: https://github.com/whx-prog/SubspaceHunter-SAO
- License: MIT for the public code repository; repository documentation identifies additional third-party package dependencies.
- HIGHFLY use: fire/electric/ice/meteor/shield/heal sequencing reference.
- SAO/IP-specific raw assets and mixed third-party packages are excluded.

### Project-X
- Source: https://github.com/khoido2003/Project-X
- GitHub repository metadata does not expose a machine-readable root license as of the v2.3 audit.
- HIGHFLY therefore does NOT copy Project-X source code or raw assets in this build.
- Only abstract combat-mechanic ideas are independently reimplemented under HIGHFLY naming/logic for private laboratory comparison.
