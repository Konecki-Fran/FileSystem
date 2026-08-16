interface Props { path: string; }

export function Breadcrumbs({ path }: Props) {
  return <div aria-label="breadcrumb">{path}</div>;
}
