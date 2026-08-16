import { useQuery } from '@tanstack/react-query';
import { FileSystemPage } from './pages/FileSystemPage';
import { API_BASE_URL } from './api/client';

export default function App() {
  const root = useQuery({
    queryKey: ['root'],
    queryFn: async () => {
      const response = await fetch(`${API_BASE_URL}/`);
      if (!response.ok) throw new Error('Could not resolve the root folder.');
      return new URL(response.url).pathname.split('/').filter(Boolean).pop() ?? '';
    }
  });

  if (root.isLoading) return <div>Loading…</div>;
  if (root.isError || !root.data) return <div role="alert">{(root.error as Error | null)?.message ?? 'Root folder unavailable.'}</div>;
  return <FileSystemPage rootId={root.data} />;
}
