import { useState } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { useDebounce } from '../hooks/useDebounce';
import { useCurrentFolder } from '../features/file-browser/hooks/useCurrentFolder';
import { Breadcrumbs } from '../features/file-browser/components/Breadcrumbs';
import { FileList } from '../features/file-browser/components/FileList';
import { CreateEntryDialog } from '../features/file-browser/components/CreateEntryDialog';
import { SearchBox } from '../features/search/components/SearchBox';
import { SearchResults } from '../features/search/components/SearchResults';
import { SearchScopeSelector, type SearchScope } from '../features/search/components/SearchScopeSelector';
import { useFileSearch } from '../features/search/hooks/useFileSearch';
import { createEntry, deleteFile, deleteFolder } from '../api/foldersApi';
import { ConfirmDialog } from '../components/ConfirmDialog';
import { ErrorMessage } from '../components/ErrorMessage';
import { LoadingSpinner } from '../components/LoadingSpinner';
import type { Entry, SearchResult } from '../models/fileSystem';
import { validateName } from '../lib/nameValidation';

interface Props { rootId: string; }

export function FileSystemPage({ rootId }: Props) {
  const queryClient = useQueryClient();
  const [folderId, setFolderId] = useState(rootId);
  const [selected, setSelected] = useState<Entry | undefined>();
  const [createOpen, setCreateOpen] = useState(false);
  const [search, setSearch] = useState('');
  const [scope, setScope] = useState<SearchScope>('subtree');
  const debouncedSearch = useDebounce(search.trim(), 200);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [actionError, setActionError] = useState<string>();

  const folder = useCurrentFolder(folderId);
  const searchError = search ? validateName(search) : undefined;
  const searchResult = useFileSearch(searchError ? '' : debouncedSearch, scope === 'subtree' ? folderId : undefined);

  if (folder.isLoading) return <LoadingSpinner />;
  if (folder.isError) return <ErrorMessage message={folder.error.message} />;

  const data = folder.data!;
  const isSearching = search.trim().length > 0;
  const searchPending = isSearching && !searchError && debouncedSearch !== search.trim();

  const refresh = () => Promise.all([
    queryClient.invalidateQueries({ queryKey: ['folder'] }),
    queryClient.invalidateQueries({ queryKey: ['search'] })
  ]);

  const handleCreate = async (name: string, type: Entry['type']) => {
    await createEntry(folderId, name, type);
    setCreateOpen(false);
    await refresh();
  };

  const handleDelete = async () => {
    if (!selected) return;
    if (selected.type === 'folder') await deleteFolder(selected.id);
    else await deleteFile(selected.id);
    setSelected(undefined);
    setDeleteOpen(false);
    await refresh();
  };

  const handleOpen = (entry: Entry) => {
    if (entry.type === 'folder') {
      setFolderId(entry.id);
      setSelected(undefined);
      setActionError(undefined);
    }
  };

  const openFolder = (id: string) => {
    setFolderId(id);
    setSelected(undefined);
    setActionError(undefined);
  };

  const openSearchResult = (result: SearchResult) => openFolder(result.parentId);

  const confirmDelete = async () => {
    if (isDeleting) return;

    setIsDeleting(true);
    try {
      setActionError(undefined);
      await handleDelete();
    } catch (error) {
      setActionError(error instanceof Error ? error.message : 'Could not delete the entry. Please try again.');
      setDeleteOpen(false);
    } finally {
      setIsDeleting(false);
    }
  };

  return (
    <main>
      <header>
        <button onClick={() => openFolder(rootId)} disabled={folderId === rootId}>Home</button>
        <button onClick={() => data.parentId && openFolder(data.parentId)} disabled={!data.parentId}>Parent</button>
        <Breadcrumbs path={data.path} />
      </header>

      <section>
        <SearchBox value={search} onChange={setSearch} error={searchError} />
        <SearchScopeSelector value={scope} onChange={setScope} />
      </section>

      {isSearching ? (
        <section>
          {searchPending || (!searchError && searchResult.isFetching) ? <LoadingSpinner /> : null}
          {searchResult.isError ? <ErrorMessage message={searchResult.error.message} /> : null}
          {!searchError && !searchPending && searchResult.data ? <SearchResults results={searchResult.data} onOpen={openSearchResult} /> : null}
        </section>
      ) : (
        <section>
          <button onClick={() => setCreateOpen(true)}>New</button>
          <button disabled={!selected} onClick={() => setDeleteOpen(true)}>Delete</button>
          <FileList entries={data.children} selectedId={selected?.id} onSelect={setSelected} onOpen={handleOpen} />
        </section>
      )}

      {actionError ? <ErrorMessage message={actionError} /> : null}
      <CreateEntryDialog open={createOpen} entries={data.children} onClose={() => setCreateOpen(false)} onCreate={handleCreate} />
      <ConfirmDialog
        open={deleteOpen}
        title="Delete entry"
        message={selected?.type === 'folder' ? 'Everything inside this folder will also be deleted.' : 'Delete this file?'}
        disabled={isDeleting}
        onConfirm={() => void confirmDelete()}
        onCancel={() => setDeleteOpen(false)}
      />
    </main>
  );
}
