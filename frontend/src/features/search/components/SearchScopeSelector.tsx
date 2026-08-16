export type SearchScope = 'all' | 'subtree';

interface Props {
  value: SearchScope;
  onChange: (value: SearchScope) => void;
}

export function SearchScopeSelector({ value, onChange }: Props) {
  return (
    <select aria-label="Search scope" value={value} onChange={e => onChange(e.target.value as SearchScope)}>
      <option value="subtree">Current folder + subfolders</option>
      <option value="all">All files</option>
    </select>
  );
}
