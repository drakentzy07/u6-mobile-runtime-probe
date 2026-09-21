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
