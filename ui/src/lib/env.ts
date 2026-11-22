/** Empty in a production build: the API serves the export, so every call is same-origin. */
export const env = {
  apiBaseUrl: process.env.NEXT_PUBLIC_API_BASE_URL ?? '',
} as const;
