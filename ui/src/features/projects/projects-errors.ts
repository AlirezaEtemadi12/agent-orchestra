import { ApiError } from '@/lib/api/api-error';

/** `Projects.Project.IdTaken` belongs on the id field; every other error is a page message. */
export function isIdTaken(error: unknown): boolean {
  return error instanceof ApiError && error.code === 'Projects.Project.IdTaken';
}

export function errorText(error: unknown, fallback: string): string {
  return error instanceof ApiError ? error.message : fallback;
}
