import type { SearchResult } from '../../../models/fileSystem';
import { EmptyState } from '../../../components/EmptyState';

interface Props { results: SearchResult[]; onOpen?: (result: SearchResult) => void; }

export function SearchResults({ results, onOpen }: Props) {
  if (results.length === 0) return <EmptyState message="No matches." />;
  return (
    <div role="list">
      {results.map(result => (
        <button key={result.id} onClick={() => onOpen?.(result)}>
          <strong>{result.name}</strong>
          <span>{result.path}</span>
        </button>
      ))}
    </div>
  );
}
