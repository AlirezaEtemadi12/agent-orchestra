import { fileURLToPath } from 'node:url';
import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    environment: 'jsdom',
    globals: false,
    setupFiles: ['./vitest.setup.ts'],
    include: ['src/**/*.test.{ts,tsx}'],
    // next-intl ships ESM that imports `next/navigation` without its extension; inlining lets Vite resolve it.
    server: {
      deps: {
        inline: ['next-intl'],
      },
    },
  },
  resolve: {
    alias: [
      { find: '@', replacement: fileURLToPath(new URL('./src', import.meta.url)) },
      {
        find: /^next\/navigation$/,
        replacement: fileURLToPath(new URL('./node_modules/next/navigation.js', import.meta.url)),
      },
    ],
  },
});
