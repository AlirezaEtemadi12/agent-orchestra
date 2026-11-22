import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { apiClient } from './client';

const fetchMock = vi.fn();

function response(status: number, body: string): Response {
  return { ok: status < 400, status, text: async () => body } as Response;
}

describe('the api client', () => {
  beforeEach(() => {
    fetchMock.mockReset();
    vi.stubGlobal('fetch', fetchMock);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('resolves a 204 delete to undefined instead of parsing a body', async () => {
    fetchMock.mockResolvedValue(response(204, ''));

    await expect(apiClient.delete('projects/shop', 'en')).resolves.toBeUndefined();
    expect(fetchMock).toHaveBeenCalledWith('/api/projects/shop', expect.objectContaining({ method: 'DELETE' }));
  });

  it('sends the JSON body on put', async () => {
    fetchMock.mockResolvedValue(response(200, JSON.stringify({ id: 'shop' })));

    const saved = await apiClient.put<{ id: string }>('projects/shop', 'en', { name: 'MyShop' });

    expect(saved).toEqual({ id: 'shop' });

    const [, options] = fetchMock.mock.calls[0] as [string, { method: string; body: string }];

    expect(options.method).toBe('PUT');
    expect(options.body).toBe(JSON.stringify({ name: 'MyShop' }));
  });
});
