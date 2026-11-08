import { useState, useEffect } from "react";
import { Link, useParams, useNavigate } from "react-router-dom";
import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import {
    Plus,
    Search,
    ArrowUpRight,
    ArrowLeft,
    LayoutGrid,
    List,
    CalendarDays,
    Building2,
    Archive,
    Pencil,
    RotateCcw,
    CheckCircle2,
} from "lucide-react";
import { useSession } from "../lib/session";
import { useWorkspace, useLookup } from "../lib/workspace";
import {
    stages,
    humanize,
    money,
    date,
    rowName,
    projectStatuses,
    taskStatuses,
    type Resource,
    type Row,
    type Page,
    type ProjectDetails,
    type Member,
} from "../lib/types";
import { Button } from "../components/ui/button";
import { Modal } from "../components/ui/dialog";
import { Loading, ErrorPanel, Empty, useToast } from "../components/feedback";
import { Editor } from "./Editor";
const titles: Record<Resource, [string, string]> = {
    companies: ["Companies", "Build relationships that last."],
    contacts: ["Contacts", "The people behind the possibilities."],
    opportunities: ["Pipeline", "Move the right conversations forward."],
    projects: ["Projects", "Turn good ideas into great deliveries."],
    tasks: ["Tasks", "Clear next steps. Shared momentum."],
};
export function Badge({ value }: { value?: string }) {

    return (
        <span className={`badge badge-${(value || "").toLowerCase()}`}>
            {humanize(value || "Active")}
        </span>
    );
}
export function ResourcePage({
    resource,
    companyId,
    projectId,
    embedded = false,
    parentArchived = false,
}: {
    resource: Resource;
    companyId?: string;
    projectId?: string;
    embedded?: boolean;
    parentArchived?: boolean;
}) {
    const session = useSession(),
        { organization, base } = useWorkspace(),
        cache = useQueryClient(),
        toast = useToast();
    const [search, setSearch] = useState(""),
        [page, setPage] = useState(1),
        [archived, setArchived] = useState(false),
        [status, setStatus] = useState(""),
        [sort, setSort] = useState("name"),
        [board, setBoard] = useState(resource === "opportunities"),
        [editor, setEditor] = useState<Row | true | null>(null);
    const companies = useLookup("companies");
    const [companyFilter, setCompanyFilter] = useState("");
    const [debouncedSearch, setDebouncedSearch] = useState(search);
    useEffect(() => {
        const timer = window.setTimeout(() => setDebouncedSearch(search), 250);

        return () => window.clearTimeout(timer);
    }, [search]);
    const query = new URLSearchParams({
        page: String(page),
        pageSize: "20",
        search: debouncedSearch,
        archived: String(archived),
        sort,
    });
    if (companyId || companyFilter)
        query.set("companyId", companyId || companyFilter);
    if (projectId) query.set("projectId", projectId);
    if (status) query.set("status", status);
    const key = [session.identity, organization.id, resource, query.toString()];
    const list = useQuery({
        queryKey: key,
        queryFn: () =>
            session.request<Page<Row>>(
                `/api/organizations/${organization.id}/${resource}?${query}`,
            ),
    });
    const stageMutation = useMutation({
        mutationFn: ({ row, stage }: { row: Row; stage: string }) =>
            session.request(
                `/api/organizations/${organization.id}/opportunities/${row.id}/stage`,
                "POST",
                { stage, version: row.version },
            ),
        onMutate: async ({ row, stage }) => {
            await cache.cancelQueries({ queryKey: key });
            const previous = cache.getQueryData<Page<Row>>(key);
            cache.setQueryData<Page<Row>>(key, (old) =>
                old
                    ? {
                          ...old,
                          items: old.items.map((x) =>
                              x.id === row.id ? { ...x, stage } : x,
                          ),
                      }
                    : old,
            );

            return { previous };
        },
        onError: (_e, _v, context) => {
            if (context?.previous) cache.setQueryData(key, context.previous);
        },
        onSuccess: () => toast("Pipeline updated."),
        onSettled: () =>
            cache.invalidateQueries({
                queryKey: [session.identity, organization.id],
            }),
    });
    const archiveTask = useMutation({
        mutationFn: (row: Row) =>
            session.request(
                `/api/organizations/${organization.id}/tasks/${row.id}/${row.isArchived ? "restore" : "archive"}`,
                "POST",
                { version: row.version },
            ),
        onSuccess: () => {
            cache.invalidateQueries({
                queryKey: [session.identity, organization.id],
            });
            toast("Task updated.");
        },
    });
    const companyName = (id?: string) =>
        companies.data?.find((x) => x.id === id)?.name || "Company";
    const link = (row: Row) =>
        resource === "tasks"
            ? `${base}/projects/${row.projectId}`
            : `${base}/${resource}/${row.id}`;
    const move = (row: Row, stage: string) => {
        if (!session.demo && !row.isArchived && !stageMutation.isPending)
            stageMutation.mutate({ row, stage });
    };
    const statuses =
        resource === "opportunities"
            ? [...stages]
            : resource === "projects"
              ? projectStatuses
              : resource === "tasks"
                ? taskStatuses
                : [];

    return (
        <section className={embedded ? "embedded-section" : ""}>
            {!embedded && (
                <span className="eyebrow">
                    {resource === "opportunities"
                        ? "FROM HELLO TO HANDSHAKE"
                        : "YOUR WORKSPACE"}
                </span>
            )}
            <div className="page-heading">
                <div>
                    {embedded ? (
                        <h2>{titles[resource][0]}</h2>
                    ) : (
                        <h1>{titles[resource][0]}</h1>
                    )}
                    <p>{titles[resource][1]}</p>
                </div>
                <Button
                    disabled={session.demo || parentArchived}
                    title={session.demo ? "Read-only demo" : undefined}
                    onClick={() => setEditor(true)}
                >
                    <Plus size={16} />
                    New{" "}
                    {resource === "companies"
                        ? "company"
                        : resource === "opportunities"
                          ? "opportunity"
                          : resource.slice(0, -1)}
                </Button>
            </div>
            <div className="list-toolbar">
                <div className="search-field">
                    <Search size={17} />
                    <input
                        aria-label={`Search ${resource}`}
                        placeholder={`Search ${resource}…`}
                        value={search}
                        onChange={(e) => {
                            setSearch(e.target.value);
                            setPage(1);
                        }}
                        maxLength={160}
                    />
                </div>
                <div className="toolbar-filters">
                    {!companyId &&
                        ["contacts", "opportunities", "projects"].includes(
                            resource,
                        ) && (
                            <select
                                aria-label="Company filter"
                                value={companyFilter}
                                onChange={(event) => {
                                    setCompanyFilter(event.target.value);
                                    setPage(1);
                                }}
                            >
                                <option value="">All companies</option>
                                {companies.data?.map((company) => (
                                    <option key={company.id} value={company.id}>
                                        {company.name}
                                    </option>
                                ))}
                            </select>
                        )}
                    {statuses.length > 0 && (
                        <select
                            aria-label="Status filter"
                            value={status}
                            onChange={(e) => {
                                setStatus(e.target.value);
                                setPage(1);
                            }}
                        >
                            <option value="">
                                All{" "}
                                {resource === "opportunities"
                                    ? "stages"
                                    : "statuses"}
                            </option>
                            {statuses.map((x) => (
                                <option key={x} value={x}>
                                    {humanize(x)}
                                </option>
                            ))}
                        </select>
                    )}
                    <select
                        aria-label="Archive filter"
                        value={String(archived)}
                        onChange={(e) => {
                            setArchived(e.target.value === "true");
                            setPage(1);
                        }}
                    >
                        <option value="false">Active records</option>
                        <option value="true">Archived</option>
                    </select>
                    <select
                        aria-label="Sort records"
                        value={sort}
                        onChange={(e) => {
                            setSort(e.target.value);
                            setPage(1);
                        }}
                    >
                        <option value="name">Name A–Z</option>
                        <option value="nameDesc">Name Z–A</option>
                        <option value="newest">Newest first</option>
                    </select>
                    {resource === "opportunities" && (
                        <div className="view-toggle">
                            <button
                                aria-label="Board view"
                                aria-pressed={board}
                                className={board ? "selected" : ""}
                                onClick={() => setBoard(true)}
                            >
                                <LayoutGrid size={17} />
                            </button>
                            <button
                                aria-label="Table view"
                                aria-pressed={!board}
                                className={!board ? "selected" : ""}
                                onClick={() => setBoard(false)}
                            >
                                <List size={17} />
                            </button>
                        </div>
                    )}
                </div>
            </div>
            {archiveTask.error && (
                <ErrorPanel
                    error={archiveTask.error}
                    retry={() => {
                        archiveTask.reset();
                        list.refetch();
                    }}
                />
            )}
            {stageMutation.error && (
                <ErrorPanel
                    error={stageMutation.error}
                    retry={() => {
                        stageMutation.reset();
                        list.refetch();
                    }}
                />
            )}
            {list.isPending ? (
                <Loading />
            ) : list.error ? (
                <ErrorPanel error={list.error} retry={() => list.refetch()} />
            ) : !list.data?.items.length ? (
                <div className="panel">
                    <Empty
                        title={
                            search || status
                                ? "No matches found"
                                : `No ${resource} yet`
                        }
                    >
                        {search || status
                            ? "Try another search or clear your filters."
                            : archived
                              ? "Archived records will appear here."
                              : "Add your first record to start building momentum."}
                    </Empty>
                </div>
            ) : board && resource === "opportunities" ? (
                <div
                    className="kanban"
                    onDragOver={(event) => {
                        if (session.demo) return;
                        const bounds =
                            event.currentTarget.getBoundingClientRect();
                        if (event.clientX > bounds.right - 60)
                            event.currentTarget.scrollLeft += 18;
                        if (event.clientX < bounds.left + 60)
                            event.currentTarget.scrollLeft -= 18;
                    }}
                >
                    {stages.map((stage) => {
                        const rows = list.data.items.filter(
                            (x) => x.stage === stage,
                        );

                        return (
                            <section
                                key={stage}
                                className="kanban-column"
                                onDragOver={(e) => {
                                    if (!session.demo) e.preventDefault();
                                }}
                                onDrop={(e) => {
                                    e.preventDefault();
                                    const row = list.data.items.find(
                                        (x) =>
                                            x.id ===
                                            e.dataTransfer.getData(
                                                "text/plain",
                                            ),
                                    );
                                    if (row) move(row, stage);
                                }}
                            >
                                <div className="column-heading">
                                    <span
                                        className={`stage-dot stage-${stage.toLowerCase()}`}
                                    />
                                    <h3>{stage}</h3>
                                    <span>{rows.length}</span>
                                </div>
                                <div className="column-body">
                                    {rows.map((row) => (
                                        <article
                                            key={row.id}
                                            className="deal-card"
                                            draggable={
                                                !session.demo &&
                                                !archived &&
                                                !stageMutation.isPending
                                            }
                                            onDragStart={(e) =>
                                                e.dataTransfer.setData(
                                                    "text/plain",
                                                    row.id,
                                                )
                                            }
                                        >
                                            <small>
                                                {companyName(row.companyId)}
                                            </small>
                                            <Link to={link(row)}>
                                                {row.name}
                                            </Link>
                                            <strong>
                                                {money(row.value, row.currency)}
                                            </strong>
                                            <div className="deal-foot">
                                                <span>
                                                    <CalendarDays size={13} />
                                                    {date(
                                                        row.expectedCloseDate,
                                                    )}
                                                </span>
                                                <select
                                                    aria-label={`Stage for ${row.name}`}
                                                    value={row.stage}
                                                    disabled={
                                                        session.demo ||
                                                        archived ||
                                                        stageMutation.isPending
                                                    }
                                                    onChange={(e) =>
                                                        move(
                                                            row,
                                                            e.target.value,
                                                        )
                                                    }
                                                >
                                                    {stages.map((x) => (
                                                        <option key={x}>
                                                            {x}
                                                        </option>
                                                    ))}
                                                </select>
                                            </div>
                                        </article>
                                    ))}
                                    {!rows.length && (
                                        <div className="column-empty">
                                            No opportunities
                                        </div>
                                    )}
                                </div>
                            </section>
                        );
                    })}
                </div>
            ) : (
                <div className="panel table-wrap">
                    <table>
                        <thead>
                            <tr>
                                <th>
                                    {resource === "contacts"
                                        ? "Contact"
                                        : resource === "tasks"
                                          ? "Task"
                                          : "Name"}
                                </th>
                                <th>
                                    {resource === "companies"
                                        ? "Industry"
                                        : resource === "contacts"
                                          ? "Company"
                                          : resource === "tasks"
                                            ? "Priority"
                                            : "Company"}
                                </th>
                                <th>
                                    {resource === "companies" ||
                                    resource === "contacts"
                                        ? "Email"
                                        : resource === "opportunities"
                                          ? "Value"
                                          : "Due date"}
                                </th>
                                <th>
                                    {resource === "companies" ||
                                    resource === "contacts"
                                        ? "Phone"
                                        : "Status"}
                                </th>
                                <th>
                                    <span className="sr-only">Open</span>
                                </th>
                            </tr>
                        </thead>
                        <tbody>
                            {list.data.items.map((row) => (
                                <tr key={row.id}>
                                    <td>
                                        <div className="name-cell">
                                            <span
                                                className={`record-avatar avatar-${row.id.charCodeAt(0) % 4}`}
                                            >
                                                {rowName(row).slice(0, 1)}
                                            </span>
                                            {resource === "tasks" ? (
                                                <button
                                                    className="record-link"
                                                    onClick={() =>
                                                        setEditor(row)
                                                    }
                                                >
                                                    {rowName(row)}
                                                </button>
                                            ) : (
                                                <Link
                                                    className="record-link"
                                                    to={link(row)}
                                                >
                                                    {rowName(row)}
                                                </Link>
                                            )}
                                        </div>
                                    </td>
                                    <td>
                                        {resource === "companies" ? (
                                            row.industry || "—"
                                        ) : resource === "tasks" ? (
                                            <Badge value={row.priority} />
                                        ) : (
                                            companyName(row.companyId)
                                        )}
                                    </td>
                                    <td>
                                        {resource === "companies" ||
                                        resource === "contacts"
                                            ? row.email || "—"
                                            : resource === "opportunities"
                                              ? money(row.value, row.currency)
                                              : date(row.dueDate)}
                                    </td>
                                    <td>
                                        {resource === "companies" ||
                                        resource === "contacts" ? (
                                            row.phone || "—"
                                        ) : (
                                            <Badge
                                                value={row.stage || row.status}
                                            />
                                        )}
                                    </td>
                                    <td>
                                        {resource === "tasks" && (
                                            <Button
                                                variant="ghost"
                                                disabled={
                                                    session.demo ||
                                                    archiveTask.isPending
                                                }
                                                onClick={() =>
                                                    archiveTask.mutate(row)
                                                }
                                            >
                                                {row.isArchived
                                                    ? "Restore"
                                                    : "Archive"}
                                            </Button>
                                        )}
                                        <Link
                                            aria-label={`Open ${rowName(row)}`}
                                            to={link(row)}
                                        >
                                            <ArrowUpRight size={16} />
                                        </Link>
                                    </td>
                                </tr>
                            ))}
                        </tbody>
                    </table>
                </div>
            )}
            {list.data && (
                <div className="pagination">
                    <span>
                        {list.data.total} {resource}
                        {board && resource === "opportunities"
                            ? " · Board shows the current page"
                            : ""}
                    </span>
                    <div className="row">
                        <Button
                            variant="outline"
                            disabled={page === 1}
                            onClick={() => setPage((p) => p - 1)}
                        >
                            Previous
                        </Button>
                        <span>
                            Page {page} of{" "}
                            {Math.max(1, Math.ceil(list.data.total / 20))}
                        </span>
                        <Button
                            variant="outline"
                            disabled={page * 20 >= list.data.total}
                            onClick={() => setPage((p) => p + 1)}
                        >
                            Next
                        </Button>
                    </div>
                </div>
            )}
            {editor &&
                (!session.demo && (editor === true || !editor.isArchived) ? (
                    <Editor
                        resource={resource}
                        record={editor === true ? undefined : editor}
                        projectId={projectId}
                        companyId={companyId}
                        onClose={() => setEditor(null)}
                    />
                ) : (
                    <Modal
                        title="Task details"
                        open
                        onClose={() => setEditor(null)}
                    >
                        <h3>{editor !== true && rowName(editor)}</h3>
                        <p>{editor !== true && editor.description}</p>
                        <p className="muted">
                            {session.demo
                                ? "This demo is read-only."
                                : "Restore this task before editing it."}
                        </p>
                    </Modal>
                ))}
        </section>
    );
}
export function DetailPage({ resource }: { resource: Resource }) {
    const { id } = useParams(),
        session = useSession(),
        { organization, base } = useWorkspace(),
        cache = useQueryClient(),
        toast = useToast(),
        navigate = useNavigate();
    const [editing, setEditing] = useState(false),
        [confirm, setConfirm] = useState(false);
    const data = useQuery({
        queryKey: [session.identity, organization.id, resource, id],
        queryFn: () =>
            session.request<Row | ProjectDetails>(
                `/api/organizations/${organization.id}/${resource}/${id}`,
            ),
    });
    const companies = useLookup("companies");
    const row = data.data
        ? resource === "projects"
            ? (data.data as ProjectDetails).project
            : (data.data as Row)
        : undefined;
    const action = useMutation({
        mutationFn: (operation: string) =>
            session.request<Row>(
                `/api/organizations/${organization.id}/${resource}/${id}/${operation}`,
                "POST",
                { version: row?.version },
            ),
        onSuccess: (result, operation) => {
            cache.invalidateQueries({
                queryKey: [session.identity, organization.id],
            });
            setConfirm(false);
            toast(
                operation === "convert" ? "Project ready." : "Record updated.",
            );
            if (operation === "convert")
                navigate(`${base}/projects/${result.id}`);
        },
    });
    const stage = useMutation({
        mutationFn: (value: string) =>
            session.request(
                `/api/organizations/${organization.id}/opportunities/${id}/stage`,
                "POST",
                { stage: value, version: row?.version },
            ),
        onSuccess: () => {
            cache.invalidateQueries({
                queryKey: [session.identity, organization.id],
            });
            toast("Stage updated.");
        },
    });
    if (data.isPending) return <Loading />;
    if (data.error || !row)

        return <ErrorPanel error={data.error} retry={() => data.refetch()} />;
    const company = companies.data?.find((x) => x.id === row.companyId),
        details = data.data as ProjectDetails;

    return (
        <>
            <Link className="back-link" to={`${base}/${resource}`}>
                <ArrowLeft size={15} />
                Back to {titles[resource][0].toLowerCase()}
            </Link>
            <div className="page-heading">
                <div>
                    <span className="eyebrow">
                        {resource === "companies"
                            ? "COMPANY"
                            : resource.slice(0, -1).toUpperCase()}
                    </span>
                    <h1>{rowName(row)}</h1>
                    <p>
                        {row.isArchived
                            ? "Archived · retained for your history"
                            : resource === "contacts"
                              ? row.jobTitle
                              : company?.name || "A closer look at the work."}
                    </p>
                </div>
                <div className="row">
                    <Button
                        variant="outline"
                        disabled={session.demo || row.isArchived}
                        onClick={() => setEditing(true)}
                    >
                        <Pencil size={15} />
                        Edit
                    </Button>
                    <Button
                        variant="outline"
                        disabled={session.demo}
                        onClick={() => setConfirm(true)}
                    >
                        {row.isArchived ? (
                            <RotateCcw size={15} />
                        ) : (
                            <Archive size={15} />
                        )}{" "}
                        {row.isArchived ? "Restore" : "Archive"}
                    </Button>
                </div>
            </div>
            {(action.error || stage.error) && (
                <ErrorPanel
                    error={action.error || stage.error}
                    retry={() => {
                        action.reset();
                        stage.reset();
                        data.refetch();
                    }}
                />
            )}
            <div className="detail-grid">
                <section className="panel detail-card">
                    <h2>Details</h2>
                    <dl>
                        {resource === "companies" ? (
                            <>
                                <dt>Industry</dt>
                                <dd>{row.industry || "—"}</dd>
                                <dt>Website</dt>
                                <dd>
                                    {row.website ? (
                                        <a
                                            href={row.website}
                                            target="_blank"
                                            rel="noreferrer"
                                        >
                                            {row.website}
                                            <ArrowUpRight size={13} />
                                        </a>
                                    ) : (
                                        "—"
                                    )}
                                </dd>
                            </>
                        ) : (
                            <>
                                <dt>Company</dt>
                                <dd>
                                    <Link
                                        to={`${base}/companies/${row.companyId}`}
                                    >
                                        {company?.name || "View company"}
                                    </Link>
                                </dd>
                            </>
                        )}
                        {(resource === "companies" ||
                            resource === "contacts") && (
                            <>
                                <dt>Email</dt>
                                <dd>{row.email || "—"}</dd>
                                <dt>Phone</dt>
                                <dd>{row.phone || "—"}</dd>
                            </>
                        )}
                        {resource === "opportunities" && (
                            <>
                                <dt>Deal value</dt>
                                <dd className="detail-value">
                                    {money(row.value, row.currency)}
                                </dd>
                                <dt>Contact</dt>
                                <dd>
                                    {row.contactId ? (
                                        <Link
                                            to={`${base}/contacts/${row.contactId}`}
                                        >
                                            View contact{" "}
                                            <ArrowUpRight size={13} />
                                        </Link>
                                    ) : (
                                        "None"
                                    )}
                                </dd>
                                <dt>Expected close</dt>
                                <dd>{date(row.expectedCloseDate)}</dd>
                                <dt>Stage</dt>
                                <dd>
                                    <select
                                        aria-label="Opportunity stage"
                                        value={row.stage}
                                        disabled={
                                            session.demo ||
                                            row.isArchived ||
                                            stage.isPending
                                        }
                                        onChange={(e) =>
                                            stage.mutate(e.target.value)
                                        }
                                    >
                                        {stages.map((x) => (
                                            <option key={x}>{x}</option>
                                        ))}
                                    </select>
                                </dd>
                            </>
                        )}
                        {resource === "projects" && (
                            <>
                                <dt>Status</dt>
                                <dd>
                                    <Badge value={row.status} />
                                </dd>
                                <dt>Start</dt>
                                <dd>{date(row.startDate)}</dd>
                                <dt>Due</dt>
                                <dd>{date(row.dueDate)}</dd>
                                {row.sourceOpportunityId && (
                                    <>
                                        <dt>Won opportunity</dt>
                                        <dd>
                                            <Link
                                                to={`${base}/opportunities/${row.sourceOpportunityId}`}
                                            >
                                                View original deal{" "}
                                                <ArrowUpRight size={14} />
                                            </Link>
                                        </dd>
                                    </>
                                )}
                            </>
                        )}
                        <dt>Created</dt>
                        <dd>{date(row.createdAt)}</dd>
                    </dl>
                </section>
                <section className="panel detail-card">
                    {resource === "projects" ? (
                        <>
                            <div className="row-between">
                                <h2>Project progress</h2>
                                <span className="progress-value">
                                    {details.progress}%
                                </span>
                            </div>
                            <div className="progress-track">
                                <div
                                    style={{ width: details.progress + "%" }}
                                />
                            </div>
                            <p className="muted">
                                {details.completedTasks} of {details.totalTasks}{" "}
                                active tasks completed
                            </p>
                            <h3>About this project</h3>
                            <p className="description">
                                {row.description ||
                                    "Add a description to give your team context."}
                            </p>
                        </>
                    ) : resource === "opportunities" ? (
                        <>
                            <span className="feature-icon">
                                <CheckCircle2 size={24} />
                            </span>
                            <h2>From opportunity to delivery</h2>
                            <p>
                                Once this deal is won, turn it into a project
                                and give the work a clear home.
                            </p>
                            <Button
                                disabled={
                                    session.demo ||
                                    row.isArchived ||
                                    row.stage !== "Won" ||
                                    action.isPending
                                }
                                onClick={() => action.mutate("convert")}
                            >
                                Open or create project{" "}
                                <ArrowUpRight size={15} />
                            </Button>
                            <small className="muted block">
                                Converting twice opens the same project.
                            </small>
                        </>
                    ) : (
                        <>
                            <span className="feature-icon">
                                <Building2 size={24} />
                            </span>
                            <h2>Keep the relationship in view.</h2>
                            <p>
                                {resource === "companies"
                                    ? "Contacts, opportunities, and projects below bring this client’s story together."
                                    : "This contact is connected to their company. View the company to explore related opportunities and projects."}
                            </p>
                        </>
                    )}
                </section>
            </div>
            {resource === "companies" && (
                <>
                    <ResourcePage
                        resource="contacts"
                        companyId={id}
                        parentArchived={row.isArchived}
                        embedded
                    />
                    <ResourcePage
                        resource="opportunities"
                        companyId={id}
                        parentArchived={row.isArchived}
                        embedded
                    />
                    <ResourcePage
                        resource="projects"
                        companyId={id}
                        parentArchived={row.isArchived}
                        embedded
                    />
                </>
            )}
            {resource === "projects" && (
                <ResourcePage
                    resource="tasks"
                    projectId={id}
                    parentArchived={row.isArchived}
                    embedded
                />
            )}
            {editing && (
                <Editor
                    resource={resource}
                    record={row}
                    onClose={() => setEditing(false)}
                />
            )}
            {confirm && (
                <Modal
                    open
                    title={`${row.isArchived ? "Restore" : "Archive"} ${rowName(row)}?`}
                    onClose={() => setConfirm(false)}
                >
                    <p>
                        {row.isArchived
                            ? "Make this record available again. Its parent records must be active."
                            : "This keeps the record in your history and removes it from active lists. Related records are not automatically archived."}
                    </p>
                    {action.error && <ErrorPanel error={action.error} />}
                    <div className="dialog-actions">
                        <Button
                            variant="outline"
                            onClick={() => setConfirm(false)}
                        >
                            Cancel
                        </Button>
                        <Button
                            disabled={action.isPending}
                            onClick={() =>
                                action.mutate(
                                    row.isArchived ? "restore" : "archive",
                                )
                            }
                        >
                            {row.isArchived ? "Restore" : "Archive"}
                        </Button>
                    </div>
                </Modal>
            )}
        </>
    );
}
