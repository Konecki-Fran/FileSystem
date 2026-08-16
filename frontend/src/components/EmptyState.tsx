interface Props {
  message: string;
}

export function EmptyState({ message }: Props) {
  return <div>{message}</div>;
}
