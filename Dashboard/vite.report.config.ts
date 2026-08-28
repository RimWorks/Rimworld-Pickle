import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import { viteSingleFile } from "vite-plugin-singlefile";

// Second build pass. viteSingleFile forces inlineDynamicImports, which rollup will
// not allow with more than one input, so the report is built separately into the
// same dist rather than as another entry of the dashboard build.
export default defineConfig({
  plugins: [react(), tailwindcss(), viteSingleFile()],
  build: {
    outDir: "dist",
    emptyOutDir: false,
    rollupOptions: {
      input: "report.html",
    },
  },
});
