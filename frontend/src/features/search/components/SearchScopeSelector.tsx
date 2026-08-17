export type SearchScope = 'all-prefix' | 'exact-current';

interface Props {
  value: SearchScope;
  onChange: (value: SearchScope) => void;
}

export function SearchScopeSelector({ value, onChange }: Props) {
  return (
    <select aria-label="Search scope" value={value} onChange={e => onChange(e.target.value as SearchScope)}>
      <option value="all-prefix">Starts with, all files</option>
      <option value="exact-current">Exact name, current folder</option>
    </select>
  );
}
