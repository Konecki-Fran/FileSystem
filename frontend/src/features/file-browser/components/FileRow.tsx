import type { Entry } from '../../../models/fileSystem';

interface Props {
  entry: Entry;
  selected: boolean;
  onSelect: (entry: Entry) => void;
  onOpen: (entry: Entry) => void;
}

export function FileRow({ entry, selected, onSelect, onOpen }: Props) {
  return (
    <button
      type="button"
      aria-pressed={selected}
      onClick={() => onSelect(entry)}
      onDoubleClick={() => onOpen(entry)}
    >
      {entry.type === 'folder' ? '📁' : '📄'} {entry.name}
    </button>
  );
}
