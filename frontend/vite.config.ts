import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";
import { fileURLToPath, URL } from "node:url";
export default defineConfig({
    resolve: {
        alias: { "@": fileURLToPath(new URL("./src", import.meta.url)) },
    },
    plugins: [react(), tailwindcss()],
    server: {
        watch: { usePolling: process.env.VITE_USE_POLLING === "true" },
        proxy: {
            "/api": { target: process.env.API_PROXY_TARGET || "http://localhost:5080", changeOrigin: true },
        },
    },
    test: {
        environment: "jsdom",
        setupFiles: "./src/test/setup.ts",
        include: ["src/**/*.test.{ts,tsx}"],
    },
});
