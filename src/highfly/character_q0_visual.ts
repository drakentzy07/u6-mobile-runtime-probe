export type HighflyCharacterQ0Body = 'claude' | 'qmale' | 'qfemale';

const VALID: readonly HighflyCharacterQ0Body[] = ['claude', 'qmale', 'qfemale'];
let body: HighflyCharacterQ0Body = 'claude';

export function highflyCharacterQ0Body(): HighflyCharacterQ0Body {
  return body;
}

export function setHighflyCharacterQ0Body(next: string): HighflyCharacterQ0Body {
  if (VALID.includes(next as HighflyCharacterQ0Body)) body = next as HighflyCharacterQ0Body;
  return body;
}

export function highflyCharacterQ0VisualKey(baseKey: string): string {
  if (body === 'claude' || !/^player_(warrior|paladin|hunter|rogue|priest|shaman|mage|warlock|druid)$/.test(baseKey)) {
    return baseKey;
  }
  return baseKey + '_' + body;
}
