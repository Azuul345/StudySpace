/// <reference types="vitest/config" />
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'
import { contentSecurityPolicy } from './src/csp.ts'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    react(),
    {
      // CSP only in the production build: the dev server injects an inline refresh script it would block.
      name: 'inject-csp',
      apply: 'build',
      transformIndexHtml: () => [
        { tag: 'meta', attrs: { 'http-equiv': 'Content-Security-Policy', content: contentSecurityPolicy }, injectTo: 'head-prepend' },
      ],
    },
  ],
  server: { proxy: { '/api': 'http://127.0.0.1:5080' } },
  preview: { proxy: { '/api': 'http://127.0.0.1:5080' } },
  test: { environment: 'jsdom', setupFiles: ['./src/test-setup.ts'], globals: false },
})
