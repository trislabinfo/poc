import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';

// When VITE_API_BASE_URL is not set (e.g. dashboard run standalone), proxy /api to the gateway so test-runtime works.
const gatewayTarget = process.env.VITE_API_BASE_URL ?? 'https://localhost:64229';

export default defineConfig({
  plugins: [sveltekit()],
  server: {
    port: process.env.PORT ? Number(process.env.PORT) : 5174,
    proxy: {
      '/api': {
        target: gatewayTarget,
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
