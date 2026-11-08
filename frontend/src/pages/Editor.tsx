import { useState, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useSession } from "../lib/session";
import { useLookup, useWorkspace } from "../lib/workspace";
import {
    projectStatuses,
    taskStatuses,
    priorities,
    humanize,
    rowName,
    type Resource,
    type Row,
    type Member,
} from "../lib/types";
import { Modal } from "../components/ui/dialog";
import { Button } from "../components/ui/button";
import { ErrorPanel, useToast } from "../components/feedback";
const zero = "00000000-0000-0000-0000-000000000000";
export function Editor({
    resource,
    record,
    projectId,
    companyId,
    onClose,
    onSaved,
}: {
    resource: Resource;
    record?: Row;
    projectId?: string;
    companyId?: string;
    onClose: () => void;
    onSaved?: () => void;
}) {
    const session = useSession(),
        { organization } = useWorkspace(),
        cache = useQueryClient(),
        toast = useToast();
    const [fields, setFields] = useState<Record<string, string>>(() => ({
        name: record?.name || "",
        firstName: record?.firstName || "",
        lastName: record?.lastName || "",
        email: record?.email || "",
        phone: record?.phone || "",
        website: record?.website || "",
        industry: record?.industry || "",
        jobTitle: record?.jobTitle || "",
        companyId: record?.companyId || companyId || "",
        contactId: record?.contactId || "",
        value: String(record?.value ?? 0),
        currency: record?.currency || "USD",
        expectedCloseDate: record?.expectedCloseDate || "",
        description: record?.description || "",
        status: record?.status || (resource === "tasks" ? "ToDo" : "Planned"),
        startDate: record?.startDate || "",
        dueDate: record?.dueDate || "",
        title: record?.title || "",
        assignedToUserId: record?.assignedToUserId || "",
        priority: record?.priority || "Medium",
    }));
    const companies = useLookup("companies"),
        contacts = useLookup("contacts");
    const members = useQuery({
        queryKey: [session.identity, organization.id, "members"],
        queryFn: () =>
            session.request<Member[]>(
                `/api/organizations/${organization.id}/members`,
            ),
        enabled: resource === "tasks",
    });
    const save = useMutation({
        mutationFn: async () => {
            const body = {
                ...fields,
                version: record?.version || zero,
                value: Number(fields.value),
                contactId: fields.contactId || null,
                assignedToUserId: fields.assignedToUserId || null,
                expectedCloseDate: fields.expectedCloseDate || null,
                startDate: fields.startDate || null,
                dueDate: fields.dueDate || null,
            };
            const path =
                resource === "tasks"
                    ? `projects/${projectId || record?.projectId}/tasks`
                    : resource;

            return session.request(
                `/api/organizations/${organization.id}/${path}${record ? "/" + record.id : ""}`,
                record ? "PUT" : "POST",
                body,
            );
        },
        onSuccess: () => {
            cache.invalidateQueries({
                queryKey: [session.identity, organization.id],
            });
            toast(record ? "Changes saved." : "Record created.");
            onSaved?.();
            onClose();
        },
    });
    const set = (key: string, value: string) =>
        setFields((previous) => ({
            ...previous,
            [key]: value,
            ...(key === "companyId" ? { contactId: "" } : {}),
        }));
    const input = (
        key: string,
        label: string,
        type = "text",
        required = false,
        maxLength = 160,
    ) => (
        <label key={key}>
            {label}
            {required && <span className="required"> *</span>}
            <input
                name={key}
                type={type}
                value={fields[key]}
                onChange={(e) => set(key, e.target.value)}
                required={required}
                maxLength={maxLength}
                {...(type === "number"
                    ? { min: 0, max: 999999999999.99, step: "0.01" }
                    : {})}
            />
        </label>
    );
    const select = (
        key: string,
        label: string,
        options: { id: string; name: string }[],
        required = false,
    ) => (
        <label key={key}>
            {label}
            {required && <span className="required"> *</span>}
            <select
                disabled={
                    resource === "contacts" && !!record && key === "companyId"
                }
                name={key}
                value={fields[key]}
                onChange={(e) => set(key, e.target.value)}
                required={required}
            >
                <option value="">{required ? "Select…" : "None"}</option>
                {fields[key] && !options.some((x) => x.id === fields[key]) && (
                    <option value={fields[key]}>
                        Current selection (archived)
                    </option>
                )}
                {options.map((x) => (
                    <option key={x.id} value={x.id}>
                        {x.name}
                    </option>
                ))}
            </select>
        </label>
    );
    const opts = (values: string[]) =>
        values.map((v) => ({ id: v, name: humanize(v) }));
    function submit(e: FormEvent) {
        e.preventDefault();
        if (!session.demo) save.mutate();
    }

    return (
        <Modal
            open
            title={`${record ? "Edit" : "New"} ${resource === "companies" ? "company" : resource === "opportunities" ? "opportunity" : resource.slice(0, -1)}`}
            onClose={onClose}
        >
            <form onSubmit={submit} className="editor-form">
                <div className="form-grid">
                    {resource !== "companies" &&
                        resource !== "tasks" &&
                        select(
                            "companyId",
                            "Company",
                            companies.data?.map((x) => ({
                                id: x.id,
                                name: x.name,
                            })) || [],
                            true,
                        )}
                    {resource === "contacts" ? (
                        <>
                            {input("firstName", "First name", "text", true, 80)}
                            {input("lastName", "Last name", "text", true, 80)}
                            {input("jobTitle", "Job title", "text", false, 120)}
                        </>
                    ) : resource === "tasks" ? (
                        input("title", "Task title", "text", true)
                    ) : (
                        input("name", "Name", "text", true)
                    )}
                    {resource === "companies" && (
                        <>
                            {input("industry", "Industry", "text", false, 120)}
                            {input("website", "Website", "url", false, 500)}
                        </>
                    )}
                    {(resource === "companies" || resource === "contacts") && (
                        <>
                            {input("email", "Email", "email", false, 254)}
                            {input("phone", "Phone", "tel", false, 50)}
                        </>
                    )}
                    {resource === "opportunities" && (
                        <>
                            {select(
                                "contactId",
                                "Contact",
                                (contacts.data || [])
                                    .filter(
                                        (x) => x.companyId === fields.companyId,
                                    )
                                    .map((x) => ({
                                        id: x.id,
                                        name: rowName(x),
                                    })),
                            )}
                            {input("value", "Deal value", "number", true)}
                            {input(
                                "currency",
                                "Currency code",
                                "text",
                                true,
                                3,
                            )}
                            {input(
                                "expectedCloseDate",
                                "Expected close",
                                "date",
                            )}
                        </>
                    )}
                    {(resource === "projects" || resource === "tasks") && (
                        <>
                            {select(
                                "status",
                                "Status",
                                opts(
                                    resource === "tasks"
                                        ? taskStatuses
                                        : projectStatuses,
                                ),
                                true,
                            )}
                            {resource === "projects" &&
                                input("startDate", "Start date", "date")}
                            {input("dueDate", "Due date", "date")}
                            <label className="full-width">
                                Description
                                <textarea
                                    maxLength={4000}
                                    rows={4}
                                    value={fields.description}
                                    onChange={(e) =>
                                        set("description", e.target.value)
                                    }
                                />
                            </label>
                        </>
                    )}
                    {resource === "tasks" && (
                        <>
                            {select(
                                "assignedToUserId",
                                "Assigned to",
                                (members.data || []).map((x) => ({
                                    id: x.userId,
                                    name: x.displayName,
                                })),
                            )}
                            {select(
                                "priority",
                                "Priority",
                                opts(priorities),
                                true,
                            )}
                        </>
                    )}
                </div>
                {companies.error &&
                    resource !== "companies" &&
                    resource !== "tasks" && (
                        <ErrorPanel error={companies.error} />
                    )}{" "}
                {contacts.error && resource === "opportunities" && (
                    <ErrorPanel error={contacts.error} />
                )}{" "}
                {members.error && resource === "tasks" && (
                    <ErrorPanel error={members.error} />
                )}{" "}
                {save.error && (
                    <ErrorPanel
                        error={save.error}
                        retry={() => {
                            cache.invalidateQueries({
                                queryKey: [session.identity, organization.id],
                            });
                            onClose();
                        }}
                    />
                )}
                <div className="dialog-actions">
                    <Button type="button" variant="outline" onClick={onClose}>
                        Cancel
                    </Button>
                    <Button disabled={save.isPending || session.demo}>
                        {save.isPending
                            ? "Saving…"
                            : record
                              ? "Save changes"
                              : "Create"}
                    </Button>
                </div>
            </form>
        </Modal>
    );
}
