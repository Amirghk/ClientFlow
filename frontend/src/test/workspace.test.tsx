import { afterEach, describe, expect, it, vi } from "vitest";
import { cleanup, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import App from "../App";
import { ToastProvider } from "../components/feedback";
import { SessionContext } from "../lib/session";
import { WorkspaceContext } from "../lib/workspace";
import { Editor } from "../pages/Editor";
import { ResourcePage } from "../pages/Resources";
import { ApiError } from "../lib/api";
import { demoRequest } from "../lib/demo";

afterEach(cleanup);
const organization = {
    id: "org1",
    name: "Studio",
    role: "Admin" as const,
    version: "v1",
    createdAt: "",
};
function mount(
    children: React.ReactNode,
    request = vi.fn().mockResolvedValue({ items: [], total: 0 }),
) {

    return render(
        <QueryClientProvider
            client={
                new QueryClient({
                    defaultOptions: { queries: { retry: false } },
                })
            }
        >
            <MemoryRouter>
                <ToastProvider>
                    <SessionContext.Provider
                        value={{
                            demo: false,
                            identity: "user",
                            profile: {
                                id: "user",
                                displayName: "Alex",
                                version: "v1",
                            },
                            request,
                            logout: vi.fn(),
                        }}
                    >
                        <WorkspaceContext.Provider
                            value={{ organization, base: "/app/org1" }}
                        >
                            {children}
                        </WorkspaceContext.Provider>
                    </SessionContext.Provider>
                </ToastProvider>
            </MemoryRouter>
        </QueryClientProvider>,
    );
}
describe("workspace presentation with simulated API responses", () => {
    it("submits a company and closes only after success", async () => {
        const request = vi.fn().mockResolvedValue({ items: [], total: 0 }),
            close = vi.fn();
        mount(<Editor resource="companies" onClose={close} />, request);
        await userEvent.type(screen.getByLabelText(/^Name/), "My studio");
        await userEvent.click(screen.getByRole("button", { name: "Create" }));
        await waitFor(() =>
            expect(request).toHaveBeenCalledWith(
                "/api/organizations/org1/companies",
                "POST",
                expect.objectContaining({ name: "My studio" }),
            ),
        );
        expect(close).toHaveBeenCalled();
    });
    it("keeps failed forms open and offers reload for stale changes", async () => {
        const request = vi
            .fn()
            .mockImplementation((_path, method) =>
                method
                    ? Promise.reject(
                          new ApiError(409, "Reload the latest version."),
                      )
                    : Promise.resolve({ items: [], total: 0 }),
            );
        const close = vi.fn();
        mount(<Editor resource="companies" onClose={close} />, request);
        await userEvent.type(screen.getByLabelText(/^Name/), "A studio");
        await userEvent.click(screen.getByRole("button", { name: "Create" }));
        expect(await screen.findByRole("alert")).toHaveTextContent(
            "Reload the latest version",
        );
        expect(close).not.toHaveBeenCalled();
        await userEvent.click(
            screen.getByRole("button", { name: "Reload latest data" }),
        );
        expect(close).toHaveBeenCalled();
    });
    it("rolls back failed optimistic pipeline moves", async () => {
        const row = {
            id: "deal",
            name: "Design",
            companyId: "company",
            stage: "New",
            value: 10,
            currency: "USD",
            version: "v1",
        };
        const request = vi
            .fn()
            .mockImplementation((path: string, method?: string) =>
                method
                    ? Promise.reject(new ApiError(409, "Reload latest data"))
                    : Promise.resolve({
                          items: path.includes("opportunities") ? [row] : [],
                          total: path.includes("opportunities") ? 1 : 0,
                      }),
            );
        mount(<ResourcePage resource="opportunities" />, request);
        const stage = await screen.findByRole("combobox", {
            name: "Stage for Design",
        });
        await userEvent.selectOptions(stage, "Won");
        await waitFor(() =>
            expect(
                screen.getByRole("combobox", { name: "Stage for Design" }),
            ).toHaveValue("New"),
        );
        expect(await screen.findByRole("alert")).toBeVisible();
    });
    it("the demo rejects writes at the data boundary", async () => {
        await expect(
            demoRequest("/api/organizations/demo/companies", "POST"),
        ).rejects.toThrow();
    });
    it("missing authentication settings show setup and never a login bypass", async () => {
        render(
            <QueryClientProvider client={new QueryClient()}>
                <MemoryRouter initialEntries={["/app"]}>
                    <App />
                </MemoryRouter>
            </QueryClientProvider>,
        );
        expect(
            await screen.findByText("A few details, then you’re in."),
        ).toBeVisible();
        expect(
            screen.queryByText("Good things are taking shape."),
        ).not.toBeInTheDocument();
    });
});
