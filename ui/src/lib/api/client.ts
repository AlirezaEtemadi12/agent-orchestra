import { env } from '@/lib/env';
import type { Locale } from '@/lib/i18n/config';
import { ApiError } from './api-error';

type Method = 'GET' | 'POST' | 'PUT' | 'DELETE';

const base = (): string => env.apiBaseUrl.replace(/\/+$/, '');

async function request<T>(method: Method, path: string, locale: Locale, body?: unknown): Promise<T> {
  const url = `${base()}/api/${path.replace(/^\/+/, '')}`;

  const response = await fetch(url, {
    method,
    headers: {
      Accept: 'application/json',
      'Accept-Language': locale,
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
    },
    ...(body === undefined ? {} : { body: JSON.stringify(body) }),
  });

  if (!response.ok) {
    throw await ApiError.fromResponse(response);
  }

  // A 204, or a body that is not JSON, has no value to parse.
  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();

  return (text.trim().length === 0 ? undefined : JSON.parse(text)) as T;
}

async function requestText(path: string, locale: Locale): Promise<string> {
  const response = await fetch(`${base()}/api/${path.replace(/^\/+/, '')}`, {
    method: 'GET',
    headers: { 'Accept-Language': locale },
  });

  if (!response.ok) {
    throw await ApiError.fromResponse(response);
  }

  return response.text();
}

export const apiClient = {
  get: <T>(path: string, locale: Locale): Promise<T> => request<T>('GET', path, locale),
  text: (path: string, locale: Locale): Promise<string> => requestText(path, locale),
  post: <T>(path: string, locale: Locale, body?: unknown): Promise<T> =>
    request<T>('POST', path, locale, body),
  put: <T>(path: string, locale: Locale, body?: unknown): Promise<T> =>
    request<T>('PUT', path, locale, body),
  delete: (path: string, locale: Locale): Promise<void> => request<void>('DELETE', path, locale),
};
