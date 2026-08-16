interface Props {
  open: boolean;
  title: string;
  message: string;
  disabled?: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}

export function ConfirmDialog({ open, title, message, disabled = false, onConfirm, onCancel }: Props) {
  if (!open) return null;
  return (
    <div role="dialog" aria-modal="true">
      <h2>{title}</h2>
      <p>{message}</p>
      <button onClick={onConfirm} disabled={disabled}>Yes</button>
      <button onClick={onCancel} disabled={disabled}>No</button>
    </div>
  );
}
