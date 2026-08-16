import { useQuery } from '@tanstack/react-query';
import { getFolder } from '../../../api/foldersApi';

export function useCurrentFolder(folderId: string) {
  return useQuery({
    queryKey: ['folder', folderId],
    queryFn: () => getFolder(folderId),
    enabled: Boolean(folderId)
  });
}
