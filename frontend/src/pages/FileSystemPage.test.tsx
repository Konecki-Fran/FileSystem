import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { createEntry, deleteFile, deleteFolder, getFolder } from '../api/foldersApi';
import { searchFiles } from '../api/searchApi';
import type { FolderContents } from '../models/fileSystem';
import { FileSystemPage } from './FileSystemPage';

vi.mock('../api/foldersApi', () => ({
  createEntry: vi.fn(),
  deleteFile: vi.fn(),
  deleteFolder: vi.fn(),
  getFolder: vi.fn()
}));

vi.mock('../api/searchApi', () => ({ searchFiles: vi.fn() }));

const rootId = '00000000-0000-0000-0000-000000000000';
const documentsId = '10000000-0000-0000-0000-000000000001';

const rootFolder: FolderContents = {
  id: rootId,
  name: 'home',
  parentId: null,
  path: 'home',
  children: [
    { id: documentsId, name: 'documents', type: 'folder' },
    { id: '20000000-0000-0000-0000-000000000001', name: 'Readme.txt', type: 'file' }
  ]
};

const documentsFolder: FolderContents = {
  id: documentsId,
  name: 'documents',
  parentId: rootId,
  path: 'home/documents',
  children: []
};

function renderPage() {
  const queryClient = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={queryClient}>
      <FileSystemPage rootId={rootId} />
    </QueryClientProvider>
  );
}

async function expectPath(path: string) {
  await waitFor(() => expect(screen.getByLabelText('breadcrumb')).toHaveTextContent(path));
}

describe('FileSystemPage', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.mocked(getFolder).mockImplementation(id => Promise.resolve(id === documentsId ? documentsFolder : rootFolder));
    vi.mocked(createEntry).mockResolvedValue({ id: 'new-entry', name: 'new entry', type: 'folder' });
    vi.mocked(deleteFile).mockResolvedValue();
    vi.mocked(deleteFolder).mockResolvedValue();
    vi.mocked(searchFiles).mockResolvedValue([]);
  });

  it('navigates into a folder, to its parent, and home', async () => {
    renderPage();

    await screen.findByRole('button', { name: /documents/i });
    fireEvent.doubleClick(screen.getByRole('button', { name: /documents/i }));
    await expectPath('home/documents');

    fireEvent.click(screen.getByRole('button', { name: 'Parent' }));
    await expectPath('home');

    fireEvent.doubleClick(screen.getByRole('button', { name: /documents/i }));
    await expectPath('home/documents');
    fireEvent.click(screen.getByRole('button', { name: 'Home' }));
    await expectPath('home');
  });

  it('creates the selected entry type and trims its name', async () => {
    renderPage();

    await screen.findByRole('button', { name: /documents/i });
    fireEvent.click(screen.getByRole('button', { name: 'New' }));
    const dialog = screen.getByRole('dialog');
    fireEvent.change(within(dialog).getByLabelText('Entry name'), { target: { value: '  report.txt  ' } });
    fireEvent.change(within(dialog).getByRole('combobox'), { target: { value: 'file' } });
    fireEvent.click(within(dialog).getByRole('button', { name: 'Create' }));

    await waitFor(() => expect(createEntry).toHaveBeenCalledWith(rootId, 'report.txt', 'file'));
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
  });

  it('warns before recursive folder deletion and prevents duplicate confirmation clicks', async () => {
    let finishDelete: (() => void) | undefined;
    vi.mocked(deleteFolder).mockImplementation(() => new Promise<void>(resolve => { finishDelete = resolve; }));
    renderPage();

    await screen.findByRole('button', { name: /documents/i });
    fireEvent.click(screen.getByRole('button', { name: /documents/i }));
    fireEvent.click(screen.getByRole('button', { name: 'Delete' }));
    const dialog = screen.getByRole('dialog');
    expect(within(dialog).getByText('Everything inside this folder will also be deleted.')).toBeInTheDocument();

    fireEvent.click(within(dialog).getByRole('button', { name: 'Yes' }));
    expect(deleteFolder).toHaveBeenCalledWith(documentsId);
    expect(within(dialog).getByRole('button', { name: 'Yes' })).toBeDisabled();
    expect(within(dialog).getByRole('button', { name: 'No' })).toBeDisabled();

    finishDelete?.();
    await waitFor(() => expect(screen.queryByRole('dialog')).not.toBeInTheDocument());
  });

  it('searches by all-file prefix or exact name in the current folder and opens a result', async () => {
    vi.mocked(searchFiles).mockResolvedValue([
      { id: 'file-id', name: 'Readme.txt', path: 'home/documents/Readme.txt', parentId: documentsId }
    ]);
    renderPage();

    await screen.findByRole('button', { name: /documents/i });
    fireEvent.change(screen.getByLabelText('Search files'), { target: { value: 'read' } });
    await waitFor(() => expect(searchFiles).toHaveBeenCalledWith('read', 'PrefixAll', undefined));
    fireEvent.change(screen.getByLabelText('Search scope'), { target: { value: 'exact-current' } });
    await waitFor(() => expect(searchFiles).toHaveBeenCalledWith('read', 'ExactCurrent', rootId));

    fireEvent.click(await screen.findByRole('button', { name: /Readme.txt/i }));
    await expectPath('home/documents');
  });

  it('shows a folder request error', async () => {
    vi.mocked(getFolder).mockRejectedValue(new Error('Folder unavailable.'));
    renderPage();

    expect(await screen.findByRole('alert')).toHaveTextContent('Folder unavailable.');
  });
});
