export function getApiBaseUrl(): string {
  const w = window as unknown as { __VITE_API_URL__?: string };
  return (w.__VITE_API_URL__ ?? '').trim();
}

export function apiUrl(path: string): string {
  const base = getApiBaseUrl();
  if (!base) return path;
  return `${base}${path}`;
}
