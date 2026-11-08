export type Resource =
    "companies" | "contacts" | "opportunities" | "projects" | "tasks";
export interface Row {
    id: string;
    organizationId: string;
    name: string;
    version: string;
    isArchived: boolean;
    createdAt: string;
    updatedAt: string;
    firstName?: string;
    lastName?: string;
    title?: string;
    description?: string;
    companyId?: string;
    contactId?: string | null;
    projectId?: string;
    assignedToUserId?: string | null;
    sourceOpportunityId?: string | null;
    industry?: string;
    website?: string;
    email?: string;
    phone?: string;
    jobTitle?: string;
    value?: number;
    currency?: string;
    stage?: string;
    status?: string;
    priority?: string;
    expectedCloseDate?: string | null;
    startDate?: string | null;
    dueDate?: string | null;
}
export interface Page<T> {
    items: T[];
    page: number;
    pageSize: number;
    total: number;
}
export interface Organization {
    id: string;
    name: string;
    role: "Admin" | "Member";
    version: string;
    createdAt: string;
}
export interface Profile {
    id: string;
    displayName: string;
    version: string;
}
export interface Member {
    userId: string;
    displayName: string;
    role: "Admin" | "Member";
}
export interface Invitation {
    id: string;
    createdAt: string;
    expiresAt: string;
    revoked: boolean;
    acceptedAt: string | null;
    version: string;
}
export interface Activity {
    id: string;
    actorName: string;
    action: string;
    resourceType: string;
    resourceId: string;
    description: string;
    createdAt: string;
}
export interface DashboardData {
    companies: number;
    activeOpportunities: number;
    activeProjects: number;
    overdueTasks: number;
    values: { currency: string; openValue: number; wonValue: number }[];
    stages: { stage: string; count: number }[];
    activity: Activity[];
}
export interface ProjectDetails {
    project: Row;
    totalTasks: number;
    completedTasks: number;
    progress: number;
}
export const stages = [
    "New",
    "Qualified",
    "Proposal",
    "Negotiation",
    "Won",
    "Lost",
] as const;
export const projectStatuses = [
    "Planned",
    "Active",
    "OnHold",
    "Completed",
    "Cancelled",
];
export const taskStatuses = ["ToDo", "InProgress", "Done"];
export const priorities = ["Low", "Medium", "High", "Urgent"];
export const humanize = (text: string) =>
    text.replace(/([a-z])([A-Z])/g, "$1 $2");
export const rowName = (row: Partial<Row>) =>
    row.name ||
    row.title ||
    [row.firstName, row.lastName].filter(Boolean).join(" ");
export const money = (value = 0, currency = "USD") => {
    try {

        return new Intl.NumberFormat("en-US", {
            style: "currency",
            currency,
            maximumFractionDigits: 2,
        }).format(value);
    } catch {

        return `${currency} ${value.toLocaleString()}`;
    }
};
export const date = (value?: string | null) =>
    value
        ? new Intl.DateTimeFormat("en", {
              month: "short",
              day: "numeric",
              year: "numeric",
              timeZone: "UTC",
          }).format(new Date(value))
        : "Not set";
