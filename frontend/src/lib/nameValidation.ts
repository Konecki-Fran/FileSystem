export const MAX_NAME_LENGTH = 255;

export function normalizeName(value: string): string {
  return value.trim();
}

export function validateName(value: string): string | undefined {
  const normalized = normalizeName(value);

  if (!normalized) return 'Name must not be empty.';
  if (normalized.length > MAX_NAME_LENGTH) return `Name must be at most ${MAX_NAME_LENGTH} characters.`;
  if (normalized.includes('/') || normalized.includes('\\')) return "Name must not contain '/' or '\\'.";

  return undefined;
}

export function findDuplicateName(value: string, entries: { name: string }[]): string | undefined {
  const normalized = normalizeName(value).toLocaleLowerCase();
  return entries.some(entry => entry.name.toLocaleLowerCase() === normalized)
    ? 'An entry with this name already exists in this folder.'
    : undefined;
}
