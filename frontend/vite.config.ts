/// <reference types="vitest/config" />
import path from 'node:path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

const apiTarget = 'http://localhost:5256'
const apiProxyPaths = ['/auth', '/me', '/health', '/requests', '/sponsorship-types', '/system']

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    proxy: Object.fromEntries(
      apiProxyPaths.map((route) => [
        route,
        {
          target: apiTarget,
          changeOrigin: true,
        },
      ]),
    ),
  },
  test: {
    environment: 'happy-dom',
    setupFiles: ['./src/test/setup.ts'],
    css: true,
  },
})
