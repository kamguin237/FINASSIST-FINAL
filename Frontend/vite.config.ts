/// <reference types="vitest" />
import { defineConfig } from 'vite';

export default defineConfig({
  server: {
    allowedHosts: ['all']
  },
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['src/test-setup.ts'],
    include: ['src/**/*.spec.ts'],
    coverage: {
      provider: 'v8',
      reporter: ['text', 'html'],
      include: ['src/app/core/**/*.ts']
    }
  }
});
