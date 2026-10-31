let csrf: string | null = null;

export async function refreshCsrf(): Promise<void> {
  const response = await fetch('/api/auth/csrf', { credentials: 'same-origin', cache: 'no-store' });
  if (!response.ok) throw new Error('Güvenlik doğrulaması başlatılamadı.');
  const json: { token: string } = await response.json();
  csrf = json.token;
}

export async function request<T>(path: string, method = 'GET', payload?: unknown): Promise<T> {
  if (method !== 'GET' && !csrf) await refreshCsrf();
  const response = await fetch(`/api${path}`, {
    method,
    credentials: 'same-origin',
    cache: 'no-store',
    headers: {
      ...(payload === undefined ? {} : { 'Content-Type': 'application/json' }),
      ...(method === 'GET' ? {} : { 'X-CSRF-TOKEN': csrf! })
    },
    ...(payload === undefined ? {} : { body: JSON.stringify(payload) })
  });
  const body: unknown = await response.json().catch(() => ({}));
  if (!response.ok) {
    const error = body as { error?: string; detail?: string; title?: string };
    throw new Error(error.error || error.detail || error.title || `HTTP ${response.status}`);
  }
  return body as T;
}

export async function logout(): Promise<void> {
  await request('/auth/logout', 'POST');
  csrf = null;
}

export interface Person { name: string; email: string; role: string }
export type RecordRow = Record<string, unknown> & { id: string };
export interface TrendPoint { date: string; value: number }
export interface Dashboard {
  customers: number; equipmentCount: number; availableEquipment: number; activeRentals: number;
  pendingRentals: number; rooms: number; occupiedRooms: number; upcomingStays: number;
  openSlopes: number; totalSlopes: number; netReceived: number; pendingBalance: number;
  trend: TrendPoint[]; upcoming: { id: string; name: string; startsAt: string; instructor: string; capacity: number }[];
  recent: { id: string; actor: string; action: string; entity: string; createdAt: string }[];
}
