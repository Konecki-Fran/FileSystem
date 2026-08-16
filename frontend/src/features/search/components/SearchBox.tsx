interface Props {
  value: string;
  onChange: (value: string) => void;
  error?: string;
}

export function SearchBox({ value, onChange, error }: Props) {
  return (
    <>
      <input
        aria-label="Search files"
        placeholder="Search files…"
        value={value}
        onChange={e => onChange(e.target.value)}
        maxLength={255}
      />
      {error ? <p role="alert">{error}</p> : null}
    </>
  );
}
