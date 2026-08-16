import type { Entry } from '../../../models/fileSystem';
import { EmptyState } from '../../../components/EmptyState';
import { FileRow } from './FileRow';

interface Props {
  entries: Entry[];
  selectedId?: string;
  onSelect: (entry: Entry) => void;
  onOpen: (entry: Entry) => void;
}

export function FileList({ entries, selectedId, onSelect, onOpen }: Props) {
  if (entries.length === 0) return <EmptyState message="This folder is empty." />;
  return (
    <div role="list">
      {entries.map(entry => (
        <FileRow key={entry.id} entry={entry} selected={entry.id === selectedId} onSelect={onSelect} onOpen={onOpen} />
      ))}
    </div>
  );
}
