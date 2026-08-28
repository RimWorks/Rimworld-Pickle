import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import { viteSingleFile } from "vite-plugin-singlefile";

// The mod serves the dashboard from an HttpListener inside RimWorld, so the build
// has to collapse to one self-contained index.html with no sibling assets to fetch.
// `npm run dev` proxies to a running game instead.
export default defineConfig({
  plugins: [react(), tailwindcss(), viteSingleFile()],
  build: {
    outDir: "dist",
    emptyOutDir: true,
  },
  server: {
    host: "0.0.0.0",
    proxy: {
      "/state": "http://127.0.0.1:27750",
      "/abort": "http://127.0.0.1:27750",
    },
  },
});
