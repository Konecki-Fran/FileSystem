import { useQuery } from '@tanstack/react-query';
import { searchFiles } from '../../../api/searchApi';

export function useFileSearch(prefix: string, folderId: string | undefined) {
  const normalized = prefix.trim();
  return useQuery({
    queryKey: ['search', normalized, folderId],
    queryFn: () => searchFiles(normalized, folderId),
    enabled: normalized.length > 0,
    staleTime: 0
  });
}
