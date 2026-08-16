export type EntryType = 'folder' | 'file';

export interface Entry {
  id: string;
  name: string;
  type: EntryType;
}

export interface FolderContents {
  id: string;
  name: string;
  parentId: string | null;
  path: string;
  children: Entry[];
}

export interface SearchResult {
  id: string;
  name: string;
  path: string;
  parentId: string;
}
