import { useEffect, useState } from 'react';
import type { EntryType } from '../../../models/fileSystem';
import { findDuplicateName, normalizeName, validateName } from '../../../lib/nameValidation';

interface Props {
  open: boolean;
  entries: { name: string }[];
  onClose: () => void;
  onCreate: (name: string, type: EntryType) => Promise<void>;
}

export function CreateEntryDialog({ open, entries, onClose, onCreate }: Props) {
  const [name, setName] = useState('');
  const [type, setType] = useState<EntryType>('folder');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [submitError, setSubmitError] = useState<string>();

  useEffect(() => {
    if (open) return;
    setName('');
    setType('folder');
    setSubmitError(undefined);
  }, [open]);

  if (!open) return null;

  const validationError = validateName(name) ?? findDuplicateName(name, entries);
  const message = validationError ?? submitError;

  const submit = async () => {
    if (validationError || isSubmitting) return;

    setIsSubmitting(true);
    setSubmitError(undefined);
    try {
      await onCreate(normalizeName(name), type);
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : 'Could not create the entry. Please try again.');
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div role="dialog" aria-modal="true">
      <h2>New entry</h2>
      <div className="dialog-form">
        <input
          aria-label="Entry name"
          value={name}
          onChange={e => setName(e.target.value)}
          onKeyDown={event => { if (event.key === 'Enter') void submit(); }}
          autoFocus
          maxLength={255}
        />
        <p className="dialog-message" role={message ? 'alert' : undefined}>{message ?? ' '}</p>
        <select value={type} onChange={e => setType(e.target.value as EntryType)}>
          <option value="folder">Folder</option>
          <option value="file">File</option>
        </select>
        <div className="dialog-actions">
          <button onClick={() => void submit()} disabled={Boolean(validationError) || isSubmitting}>Create</button>
          <button onClick={onClose} disabled={isSubmitting}>Cancel</button>
        </div>
      </div>
    </div>
  );
}
