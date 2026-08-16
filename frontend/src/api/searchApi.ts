import { apiFetch } from './client';
import type { SearchResult } from '../models/fileSystem';

export function searchFiles(prefix: string, folderId?: string) {
  const params = new URLSearchParams({ prefix, limit: '10' });
  if (folderId) params.set('folder', folderId);
  return apiFetch<SearchResult[]>(`/search?${params.toString()}`);
}
