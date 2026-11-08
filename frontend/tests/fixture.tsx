// Test-only entry point. Not imported by src/main.tsx or included in production builds.
import { createRoot } from "react-dom/client";
import { BrowserRouter, Routes, Route } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { Workspace, Onboarding, InvitationAccept } from "../src/App";
import { SessionContext } from "../src/lib/session";
import { apiRequest } from "../src/lib/api";
import { ToastProvider } from "../src/components/feedback";
import "../src/styles.css";
const target =
    new URLSearchParams(window.location.search).get("target") || "/app/org1";
window.history.replaceState(null, "", target);
createRoot(document.getElementById("root")!).render(
    <QueryClientProvider
        client={
            new QueryClient({ defaultOptions: { queries: { retry: false } } })
        }
    >
        <BrowserRouter>
            <ToastProvider>
                <SessionContext.Provider
                    value={{
                        demo: false,
                        identity: "test-user",
                        profile: {
                            id: "user1",
                            displayName: "Alex Morgan",
                            version: "v1",
                        },
                        request: (path, method, body) =>
                            apiRequest("test-only", path, method, body),
                        logout: () => {},
                    }}
                >
                    <Routes>
                        <Route
                            path="/app/:organizationId/*"
                            element={<Workspace />}
                        />
                        <Route path="/app" element={<Onboarding />} />
                        <Route path="/invite" element={<InvitationAccept />} />
                    </Routes>
                </SessionContext.Provider>
            </ToastProvider>
        </BrowserRouter>
    </QueryClientProvider>,
);
