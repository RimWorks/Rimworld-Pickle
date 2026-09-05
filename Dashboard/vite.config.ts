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
      "/pause": "http://127.0.0.1:27750",
      "/scope": "http://127.0.0.1:27750",
      "/pill": "http://127.0.0.1:27750",
      "/steps": "http://127.0.0.1:27750",
      "/step": "http://127.0.0.1:27750",
      "/continue": "http://127.0.0.1:27750",
      "/run": "http://127.0.0.1:27750",
      "/select": "http://127.0.0.1:27750",
      "/filter": "http://127.0.0.1:27750",
      "/fixtures": "http://127.0.0.1:27750",
      "/fixture": "http://127.0.0.1:27750",
      "/mode": "http://127.0.0.1:27750",
      "/wip": "http://127.0.0.1:27750",
      "/break": "http://127.0.0.1:27750",
      "/report": "http://127.0.0.1:27750",
      "/reports": "http://127.0.0.1:27750",
      "/screenshots": "http://127.0.0.1:27750",
    },
  },
});
