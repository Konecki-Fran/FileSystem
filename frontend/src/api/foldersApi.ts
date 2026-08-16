import { apiFetch } from './client';
import type { Entry, FolderContents } from '../models/fileSystem';

export function getFolder(id: string) {
  return apiFetch<FolderContents>(`/folders/${id}`);
}

export function createEntry(parentId: string, name: string, type: Entry['type']) {
  return apiFetch<Entry>(`/folders/${parentId}`, {
    method: 'POST',
    body: JSON.stringify({ name, type: type === 'folder' ? 'Folder' : 'File' })
  });
}

export function deleteFolder(id: string) {
  return apiFetch<void>(`/folders/${id}`, { method: 'DELETE' });
}

export function deleteFile(id: string) {
  return apiFetch<void>(`/files/${id}`, { method: 'DELETE' });
}
