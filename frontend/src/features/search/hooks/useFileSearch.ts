import { useQuery } from '@tanstack/react-query';
import { searchFiles } from '../../../api/searchApi';

export function useFileSearch(prefix: string, mode: 'PrefixAll' | 'ExactCurrent', folderId: string | undefined) {
  const normalized = prefix.trim();
  return useQuery({
    queryKey: ['search', normalized, mode, folderId],
    queryFn: () => searchFiles(normalized, mode, folderId),
    enabled: normalized.length > 0,
    staleTime: 0
  });
}
