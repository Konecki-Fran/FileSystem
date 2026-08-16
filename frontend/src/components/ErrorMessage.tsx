interface Props {
  message: string;
}

export function ErrorMessage({ message }: Props) {
  return <div role="alert">{message}</div>;
}
