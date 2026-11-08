import { test, expect, type Page } from "@playwright/test";

test("public demo navigation, details, search and read-only controls", async ({
    page,
}) => {
    await page.goto("/demo");
    await expect(
        page.getByRole("heading", { name: "Good things are taking shape." }),
    ).toBeVisible();
    await page.screenshot({
        path: "test-results/demo-overview.png",
        fullPage: true,
    });
    await page.getByRole("link", { name: "Companies", exact: true }).click();
    await expect(page.locator("tbody tr")).toHaveCount(6);
    const firstCompany = await page.locator('tbody .record-link').first().innerText();
    await page.getByRole('textbox', { name: 'Search companies' }).fill(firstCompany);
    await expect(page.locator('tbody tr')).toHaveCount(1);
    await page.getByRole('textbox', { name: 'Search companies' }).fill('');
    await expect(page.locator('tbody tr')).toHaveCount(6);
    await expect(
        page.getByRole("button", { name: /New company/i }),
    ).toBeDisabled();
    await page.screenshot({
        path: "test-results/demo-companies.png",
        fullPage: true,
    });
    await page.locator("tbody .record-link").first().click();
    await expect(
        page.getByRole("button", { name: "Edit", exact: true }),
    ).toBeDisabled();
    await page.getByRole("link", { name: "Pipeline", exact: true }).click();
    await expect(page.locator(".deal-card")).toHaveCount(7);
    await expect(page.locator(".deal-card select").first()).toBeDisabled();
    await page.getByRole("link", { name: "Settings", exact: true }).click();
    await expect(
        page.getByRole("button", { name: "Create invitation" }),
    ).toBeDisabled();
    await page.screenshot({
        path: "test-results/demo-settings.png",
        fullPage: true,
    });
});

test("demo remains usable on mobile without horizontal page overflow", async ({
    page,
}) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto("/demo");
    await expect(
        page.getByRole("heading", { name: "Good things are taking shape." }),
    ).toBeVisible();
    await page.getByRole("button", { name: "Open navigation" }).click();
    await page.getByRole("link", { name: "Companies", exact: true }).click();
    await expect(page.locator('.sidebar')).not.toBeVisible();
    expect(
        await page.evaluate(
            () => document.documentElement.scrollWidth <= window.innerWidth,
        ),
    ).toBe(true);
    await page.screenshot({
        path: "test-results/demo-mobile.png",
        fullPage: true,
    });
});

async function mockApi(page: Page) {
    let stage = "New";
    await page.route("**/api/**", async (route) => {
        const req = route.request(),
            url = new URL(req.url()),
            path = url.pathname;
        const organization = (id: string) => ({
            id,
            name: id === "org1" ? "North Studio" : "South Studio",
            role: "Admin",
            version: "v1",
        });
        const company = (org: string) => ({
            id: "company",
            organizationId: org,
            name: org === "org1" ? "North client" : "South client",
            version: "v1",
            isArchived: false,
        });
        const org = path.split("/")[3];
        let data: unknown = {};
        if (path === "/api/invitations/accept")

            return route.fulfill({
                status: 400,
                json: {
                    detail: "This invitation is expired, revoked, or already used.",
                },
            });
        if (path === "/api/organizations")
            data = {
                items: [organization("org1"), organization("org2")],
                total: 2,
                page: 1,
                pageSize: 100,
            };
        else if (/\/api\/organizations\/[^/]+$/.test(path))
            data = organization(org);
        else if (path.endsWith("/dashboard"))
            data = {
                companies: 1,
                activeOpportunities: 1,
                activeProjects: 0,
                overdueTasks: 0,
                values: [],
                stages: [],
                activity: [],
            };
        else if (path.endsWith("/stage")) {
            stage = req.postDataJSON().stage;
            data = {};
        } else if (path.endsWith("/opportunities"))
            data = {
                items: [
                    {
                        id: "deal",
                        name: "Design project",
                        companyId: "company",
                        value: 100,
                        currency: "USD",
                        stage,
                        version: "v1",
                    },
                ],
                total: 1,
            };
        else if (path.endsWith("/companies"))
            data =
                req.method() === "POST"
                    ? { ...company(org), ...req.postDataJSON() }
                    : { items: [company(org)], total: 1 };
        else if (path.endsWith("/members")) data = [];
        else data = { items: [], total: 0 };
        await route.fulfill({ json: data });
    });
}
test.describe("browser tests with simulated API responses (not full-stack)", () => {
    test.beforeEach(async ({ page }) => mockApi(page));
    test("company form sends input and organization switch never retains the previous client", async ({
        page,
    }) => {
        await page.goto("/tests/fixture.html?target=/app/org1/companies");
        await expect(
            page.getByText("North client", { exact: true }),
        ).toBeVisible();
        await page.getByRole("button", { name: /New company/i }).click();
        await page.getByLabel(/^Name/).fill("Fresh client");
        const sent = page.waitForRequest(
            (r) => r.method() === "POST" && r.url().endsWith("/companies"),
        );
        await page.getByRole("button", { name: "Create", exact: true }).click();
        expect((await sent).postDataJSON().name).toBe("Fresh client");
        await expect(page.getByRole("dialog")).toHaveCount(0);
        await page
            .getByRole("combobox", { name: "Organization", exact: true })
            .selectOption("org2");
        await page
            .getByRole("link", { name: "Companies", exact: true })
            .click();
        await expect(
            page.getByText("South client", { exact: true }),
        ).toBeVisible();
        await expect(
            page.getByText("North client", { exact: true }),
        ).toHaveCount(0);
    });
    test("pipeline supports drag and drop and keyboard stage selection", async ({
        page,
    }) => {
        await page.setViewportSize({ width: 1920, height: 1080 });
        await page.goto("/tests/fixture.html?target=/app/org1/opportunities");
        const card = page.locator(".deal-card");
        await expect(card).toHaveCount(1);
        await card.dragTo(
            page.locator(".kanban-column").filter({
                has: page.getByRole("heading", {
                    name: "Won",
                    exact: true,
                }),
            }),
        );
        await expect(
            page.getByRole("combobox", { name: "Stage for Design project" }),
        ).toHaveValue("Won");
        await page
            .getByRole("combobox", { name: "Stage for Design project" })
            .selectOption("Qualified");
        await expect(
            page.getByRole("combobox", { name: "Stage for Design project" }),
        ).toHaveValue("Qualified");
    });
    test("expired invitation explains why joining failed", async ({ page }) => {
        await page.goto(
            "/tests/fixture.html?target=" +
                encodeURIComponent("/invite#" + "a".repeat(64)),
        );
        await page.getByRole("button", { name: "Accept invitation" }).click();
        await expect(page.getByRole("alert")).toContainText(
            "expired, revoked, or already used",
        );
    });
});

