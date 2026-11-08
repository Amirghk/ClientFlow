import { afterEach, expect, it, vi } from "vitest";
import { apiRequest } from "../lib/api";

afterEach(() => {
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
});

it("uses the same-origin proxy when the API origin is explicitly empty", async () => {
    vi.stubEnv("VITE_API_URL", "");
    const fetch = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", fetch);
    await apiRequest("test-token", "/api/me");

    expect(fetch).toHaveBeenCalledWith("/api/me", expect.objectContaining({ headers: { Authorization: "Bearer test-token" } }));
});

it("preserves a configured API origin for host development", async () => {
    vi.stubEnv("VITE_API_URL", "http://localhost:5080/");
    const fetch = vi.fn().mockResolvedValue(new Response(null, { status: 204 }));
    vi.stubGlobal("fetch", fetch);
    await apiRequest("test-token", "/api/me");

    expect(fetch).toHaveBeenCalledWith("http://localhost:5080/api/me", expect.anything());
});
