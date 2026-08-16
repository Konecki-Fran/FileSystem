import { describe, expect, it } from 'vitest';
import { findDuplicateName, normalizeName, validateName } from './nameValidation';

describe('name validation', () => {
  it('normalizes whitespace and enforces the backend name rules', () => {
    expect(normalizeName('  notes  ')).toBe('notes');
    expect(validateName('   ')).toBe('Name must not be empty.');
    expect(validateName('folder/name')).toContain('must not contain');
    expect(validateName('a'.repeat(256))).toContain('at most 255');
    expect(validateName('valid-name')).toBeUndefined();
  });

  it('detects sibling duplicates case-insensitively after trimming', () => {
    expect(findDuplicateName('  README.TXT ', [{ name: 'Readme.txt' }])).toContain('already exists');
  });
});
