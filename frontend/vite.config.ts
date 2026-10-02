import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

export default defineConfig(({ mode }) => {
  plugins: [react()]
  const env = loadEnv(mode, process.cwd(), '')
  const port = Number(env.FRONTEND_PORT) || 3000

  return {
    server: {
      port,
      host: true,
    },
  }
})