import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import { resolve } from 'path'
import pkg from './package.json'
import { VitePWA } from 'vite-plugin-pwa'

// https://vitejs.dev/config/
export default defineConfig({
  base: process.env.ELECTRON === 'true' ? './' : '/',
  plugins: [
    vue({
      template: {
        compilerOptions: {
          isCustomElement: (tag) => tag === 'webview',
        },
      },
    }),
    VitePWA({
      registerType: 'prompt',
      manifest: {
        name: "Tools",
        short_name: "huiitre.fr",
        description: "Tools - huiitre.fr",
        theme_color: "#42b883",
        icons: [
          {
            src: "/pwa-192x192.png",
            sizes: "192x192",
            type: "image/png"
          },
          {
            src: "/pwa-512x512.png",
            sizes: "512x512",
            type: "image/png"
          }
        ]
      },
      workbox: {
        // 1. Taille max OK
        maximumFileSizeToCacheInBytes: 2 * 1024 * 1024,

        // 2. PRECACHE : AUCUN CSS
        globPatterns: [
          '**/*.{js,html,svg,png,ico}'
        ],

        // 3. Navigation SPA
        navigateFallback: 'index.html',
        navigateFallbackDenylist: [/\.[jt]s$/, /\.css$/, /\.wasm$/],

        // 4. Ignorer explicitement TOUT ce qui peut poser problème
        globIgnores: [
          '**/*.css',
          '**/themes/**',
          '**/*worker*.js',
          '**/ts.worker*.js',
          '**/editor.worker*.js',
          '**/index.*.js'
        ],

        // 6. Runtime : thèmes = NETWORK ONLY (OBLIGATOIRE)
        runtimeCaching: [
          {
            urlPattern: /^https:\/\/qa\.tools\.huiitre\.fr\/themes\/.*\.css$/,
            handler: 'NetworkOnly'
          },
          {
            urlPattern: ({ request }) => request.destination === 'image',
            handler: 'CacheFirst',
            options: {
              cacheName: 'images-cache',
              expiration: {
                maxEntries: 50,
                maxAgeSeconds: 30 * 24 * 60 * 60,
              }
            }
          }
        ]
      }
    })
  ],
  resolve: {
    alias: {
      '@': resolve(__dirname, 'src')
    }
  },
  css: {
    preprocessorOptions: {
      scss: {
        additionalData: `
          @use "@/assets/styles/reset.scss";
        `
      }
    },
  },
  server: {
    host: true,
    port: 5173,
    strictPort: true,
    hmr: {
      clientPort: 5173
    }
  },
  logLevel: 'info',
  define: {
    __APP_VERSION__: JSON.stringify(pkg.version), // Injection de la version
  },
})
