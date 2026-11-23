import { z } from 'zod';

export type ProjectSchemaMessages = {
  required: string;
  idFormat: string;
};

/** The form's rules; the server re-validates and answers one code (plan 0003 D12). The profile field is
 * gone: the engine reads the profile from the project's own file (plan 0009 D12). */
export function projectSchema({ required, idFormat }: ProjectSchemaMessages) {
  return z.object({
    name: z.string().trim().min(1, required).max(80, required),
    id: z.string().trim().regex(/^[a-z0-9-]{1,40}$/, idFormat),
    rootPath: z.string().trim().min(1, required),
    runsPath: z.string().trim().min(1, required),
    repos: z.array(z.string()),
  });
}

export type ProjectFormValues = z.infer<ReturnType<typeof projectSchema>>;

export function parseRepos(value: string): string[] {
  return value
    .split(/[,\n]/)
    .map((repo) => repo.trim())
    .filter((repo) => repo.length > 0);
}

/** The id the form builds from the name, until the owner types one (plan 0003 §6.5). */
export function slugOf(name: string): string {
  return name
    .toLowerCase()
    .trim()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '')
    .slice(0, 40)
    .replace(/-+$/g, '');
}

/** The suggested runs path fills an empty field and never replaces a typed one. */
export function runsPathAfterCheck(current: string, suggested: string): string {
  return current.trim().length === 0 ? suggested : current;
}
