import { apiFetch } from './client';
import type { SearchResult } from '../models/fileSystem';

export function searchFiles(prefix: string, mode: 'PrefixAll' | 'ExactCurrent', folderId?: string) {
  const params = new URLSearchParams({ prefix, mode, limit: '10' });
  if (folderId) params.set('folder', folderId);
  return apiFetch<SearchResult[]>(`/search?${params.toString()}`);
}
