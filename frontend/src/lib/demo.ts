import type {
    Row,
    Resource,
    Organization,
    DashboardData,
    Member,
} from "./types";
import { stages, rowName } from "./types";
export const demoOrg: Organization = {
    id: "demo",
    name: "Studio North",
    role: "Admin",
    version: "demo",
    createdAt: "2026-01-05T00:00:00Z",
};
const base = {
    organizationId: "demo",
    version: "demo",
    isArchived: false,
    createdAt: "2026-10-01T09:00:00Z",
    updatedAt: "2026-10-12T11:00:00Z",
    name: "",
};
export const demoMembers: Member[] = [
    { userId: "user1", displayName: "Alex Morgan", role: "Admin" },
    { userId: "user2", displayName: "Jordan Lee", role: "Member" },
    { userId: "user3", displayName: "Sam Rivera", role: "Member" },
];
export const demoRows: Record<Resource, Row[]> = {
    companies: [
        {
            ...base,
            id: "co1",
            name: "Aster & Co.",
            industry: "Retail & lifestyle",
            website: "https://example.com",
            email: "hello@aster.example",
            phone: "+1 555 0101",
        },
        {
            ...base,
            id: "co2",
            name: "Forma Health",
            industry: "Healthcare",
            website: "https://example.com",
            email: "team@forma.example",
            phone: "+1 555 0102",
        },
        {
            ...base,
            id: "co3",
            name: "Meridian Ventures",
            industry: "Finance",
            email: "hello@meridian.example",
            website: "",
            phone: "",
        },
        {
            ...base,
            id: "co4",
            name: "Fieldwork Coffee",
            industry: "Food & beverage",
            email: "hello@fieldwork.example",
            website: "",
            phone: "",
        },
        {
            ...base,
            id: "co5",
            name: "Orbit Logistics",
            industry: "Transport",
            email: "team@orbit.example",
            website: "",
            phone: "",
        },
        {
            ...base,
            id: "co6",
            name: "Evergreen Collective",
            industry: "Sustainability",
            email: "team@evergreen.example",
            website: "",
            phone: "",
        },
    ],
    contacts: [
        {
            ...base,
            id: "ct1",
            companyId: "co1",
            firstName: "Elena",
            lastName: "Brooks",
            jobTitle: "Head of Brand",
            email: "elena@aster.example",
            phone: "+1 555 0111",
        },
        {
            ...base,
            id: "ct2",
            companyId: "co2",
            firstName: "Marcus",
            lastName: "Chen",
            jobTitle: "Product Director",
            email: "marcus@forma.example",
            phone: "+1 555 0112",
        },
        {
            ...base,
            id: "ct3",
            companyId: "co3",
            firstName: "Priya",
            lastName: "Shah",
            jobTitle: "Managing Partner",
            email: "priya@meridian.example",
            phone: "",
        },
        {
            ...base,
            id: "ct4",
            companyId: "co4",
            firstName: "Owen",
            lastName: "Reed",
            jobTitle: "Founder",
            email: "owen@fieldwork.example",
            phone: "",
        },
    ],
    opportunities: [
        {
            ...base,
            id: "op1",
            name: "Brand identity & digital launch",
            companyId: "co1",
            contactId: "ct1",
            value: 24000,
            currency: "USD",
            stage: "Won",
            expectedCloseDate: "2026-10-05",
        },
        {
            ...base,
            id: "op2",
            name: "Patient portal redesign",
            companyId: "co2",
            contactId: "ct2",
            value: 32000,
            currency: "USD",
            stage: "Proposal",
            expectedCloseDate: "2026-11-10",
        },
        {
            ...base,
            id: "op3",
            name: "Investor experience platform",
            companyId: "co3",
            contactId: "ct3",
            value: 18000,
            currency: "USD",
            stage: "Negotiation",
            expectedCloseDate: "2026-10-30",
        },
        {
            ...base,
            id: "op4",
            name: "E-commerce discovery",
            companyId: "co4",
            contactId: "ct4",
            value: 8500,
            currency: "USD",
            stage: "Qualified",
            expectedCloseDate: "2026-11-15",
        },
        {
            ...base,
            id: "op5",
            name: "Customer dashboard",
            companyId: "co5",
            value: 15000,
            currency: "USD",
            stage: "New",
            expectedCloseDate: "2026-11-30",
        },
        {
            ...base,
            id: "op6",
            name: "Impact report website",
            companyId: "co6",
            value: 12000,
            currency: "USD",
            stage: "Proposal",
            expectedCloseDate: "2026-11-20",
        },
        {
            ...base,
            id: "op7",
            name: "Campaign creative",
            companyId: "co4",
            value: 4500,
            currency: "USD",
            stage: "Lost",
            expectedCloseDate: "2026-09-28",
        },
    ],
    projects: [
        {
            ...base,
            id: "pr1",
            name: "Aster brand & digital launch",
            companyId: "co1",
            sourceOpportunityId: "op1",
            description:
                "Build a distinctive identity and a thoughtful digital experience for Aster’s next chapter.",
            status: "Active",
            startDate: "2026-10-06",
            dueDate: "2026-11-20",
        },
        {
            ...base,
            id: "pr2",
            name: "Forma research sprint",
            companyId: "co2",
            description:
                "Understand patient needs through interviews, journey mapping, and a focused discovery sprint.",
            status: "Active",
            startDate: "2026-10-01",
            dueDate: "2026-10-28",
        },
        {
            ...base,
            id: "pr3",
            name: "Meridian strategy workshop",
            companyId: "co3",
            description:
                "Align stakeholders around the product vision and a practical delivery roadmap.",
            status: "Planned",
            startDate: "2026-11-02",
            dueDate: "2026-11-06",
        },
    ],
    tasks: [
        {
            ...base,
            id: "tk1",
            title: "Discovery & brand workshop",
            projectId: "pr1",
            description: "Review the brief and agree on creative direction.",
            assignedToUserId: "user1",
            status: "Done",
            priority: "High",
            dueDate: "2026-10-08",
        },
        {
            ...base,
            id: "tk2",
            title: "Visual direction exploration",
            projectId: "pr1",
            description: "Present three concepts for discussion.",
            assignedToUserId: "user2",
            status: "Done",
            priority: "High",
            dueDate: "2026-10-12",
        },
        {
            ...base,
            id: "tk3",
            title: "Homepage design",
            projectId: "pr1",
            description: "Responsive desktop and mobile layouts.",
            assignedToUserId: "user2",
            status: "InProgress",
            priority: "High",
            dueDate: "2026-10-22",
        },
        {
            ...base,
            id: "tk4",
            title: "Component library",
            projectId: "pr1",
            description: "Build reusable UI foundations.",
            assignedToUserId: "user3",
            status: "ToDo",
            priority: "Medium",
            dueDate: "2026-10-28",
        },
        {
            ...base,
            id: "tk5",
            title: "Recruit research participants",
            projectId: "pr2",
            description: "Schedule five patient interviews.",
            assignedToUserId: "user1",
            status: "InProgress",
            priority: "Urgent",
            dueDate: "2026-10-13",
        },
        {
            ...base,
            id: "tk6",
            title: "Prepare interview guide",
            projectId: "pr2",
            description: "",
            assignedToUserId: "user3",
            status: "ToDo",
            priority: "High",
            dueDate: "2026-10-14",
        },
    ],
};
for (const rows of Object.values(demoRows))
    for (const row of rows) row.name = rowName(row);
export const demoDashboard: DashboardData = {
    companies: 6,
    activeOpportunities: 5,
    activeProjects: 2,
    overdueTasks: 2,
    values: [{ currency: "USD", openValue: 85500, wonValue: 24000 }],
    stages: stages.map((stage) => ({
        stage,
        count: demoRows.opportunities.filter((x) => x.stage === stage).length,
    })),
    activity: [
        {
            id: "a1",
            actorName: "Alex Morgan",
            action: "converted",
            resourceType: "projects",
            resourceId: "pr1",
            description: "Brand identity & digital launch became a project",
            createdAt: "2026-10-15T09:40:00Z",
        },
        {
            id: "a2",
            actorName: "Jordan Lee",
            action: "updated",
            resourceType: "tasks",
            resourceId: "tk3",
            description: "Homepage design moved to In Progress",
            createdAt: "2026-10-15T08:30:00Z",
        },
        {
            id: "a3",
            actorName: "Sam Rivera",
            action: "created",
            resourceType: "opportunities",
            resourceId: "op6",
            description: "Impact report website added to the pipeline",
            createdAt: "2026-10-14T15:20:00Z",
        },
    ],
};
export async function demoRequest<T>(path: string, method = "GET"): Promise<T> {
    if (method !== "GET") throw new Error("This demo is read-only.");
    const url = new URL(path, "http://demo.local");
    const parts = url.pathname.split("/").filter(Boolean);
    if (url.pathname === "/api/organizations")

        return { items: [demoOrg], page: 1, pageSize: 20, total: 1 } as T;
    if (parts[2] !== "demo") throw new Error("Demo record not found.");
    const resource = parts[3];
    if (!resource) return demoOrg as T;
    if (resource === "dashboard") return demoDashboard as T;
    if (resource === "members") return demoMembers as T;
    if (resource === "invitations") return [] as T;
    let rows = demoRows[resource as Resource];
    if (!rows) throw new Error("Demo record not found.");
    if (parts[4]) {
        const row = rows.find((x) => x.id === parts[4]);
        if (!row) throw new Error("Demo record not found.");
        if (resource === "projects") {
            const tasks = demoRows.tasks.filter((x) => x.projectId === row.id);
            const completedTasks = tasks.filter(
                (x) => x.status === "Done",
            ).length;

            return {
                project: row,
                totalTasks: tasks.length,
                completedTasks,
                progress: tasks.length
                    ? Math.round((100 * completedTasks) / tasks.length)
                    : 0,
            } as T;
        }

        return row as T;
    }
    const q = url.searchParams;
    rows = rows.filter((x) => x.isArchived === (q.get("archived") === "true"));
    for (const key of ["companyId", "projectId", "assignedToUserId"] as const)
        if (q.get(key)) rows = rows.filter((x) => x[key] === q.get(key));
    if (q.get("status"))
        rows = rows.filter((x) => (x.stage ?? x.status) === q.get("status"));
    if (q.get("search"))
        rows = rows.filter((x) =>
            (x.name + " " + (x.email ?? ""))
                .toLowerCase()
                .includes(q.get("search")!.toLowerCase()),
        );
    rows = [...rows].sort((a, b) =>
        q.get("sort") === "newest"
            ? b.createdAt.localeCompare(a.createdAt)
            : a.name.localeCompare(b.name) *
              (q.get("sort") === "nameDesc" ? -1 : 1),
    );
    const page = Number(q.get("page") || 1),
        pageSize = Number(q.get("pageSize") || 20);

    return {
        items: rows.slice((page - 1) * pageSize, page * pageSize),
        total: rows.length,
        page,
        pageSize,
    } as T;
}
