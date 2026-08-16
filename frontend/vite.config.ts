import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        rewrite: path => path.replace(/^\/api/, '')
      },
      '/folders': 'http://localhost:5000',
      '/files': 'http://localhost:5000',
      '/search': 'http://localhost:5000',
      '/health': 'http://localhost:5000'
    }
  },
  test: {
    environment: 'jsdom',
    setupFiles: './src/testSetup.ts'
  }
});
