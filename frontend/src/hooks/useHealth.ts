import { useQuery } from '@tanstack/react-query';
import { apiFetch } from '../api/client';

export function useHealth() {
  return useQuery({
    queryKey: ['health'],
    queryFn: () => apiFetch<{ status: string }>('/health'),
    retry: false
  });
}
