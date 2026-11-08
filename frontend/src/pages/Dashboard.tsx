import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import {
    ArrowRight,
    ArrowUpRight,
    Building2,
    BriefcaseBusiness,
    FolderKanban,
    Clock3,
    Sparkles,
} from "lucide-react";
import { useSession } from "../lib/session";
import { useWorkspace } from "../lib/workspace";
import {
    money,
    date,
    humanize,
    type DashboardData,
    type Page,
    type Row,
} from "../lib/types";
import { Loading, ErrorPanel, Empty } from "../components/feedback";
import { Button } from "../components/ui/button";
export function Dashboard() {
    const session = useSession(),
        { organization, base } = useWorkspace(),
        [currency, setCurrency] = useState("");
    const data = useQuery({
        queryKey: [session.identity, organization.id, "dashboard"],
        queryFn: () =>
            session.request<DashboardData>(
                `/api/organizations/${organization.id}/dashboard`,
            ),
    });
    const projects = useQuery({
        queryKey: [session.identity, organization.id, "projects", "dashboard"],
        queryFn: () =>
            session.request<Page<Row>>(
                `/api/organizations/${organization.id}/projects?pageSize=3&status=Active`,
            ),
    });
    if (data.isPending) return <Loading />;
    if (data.error || !data.data)

        return <ErrorPanel error={data.error} retry={() => data.refetch()} />;
    const d = data.data,
        selected = d.values.find((v) => v.currency === currency) || d.values[0],
        max = Math.max(1, ...d.stages.map((s) => s.count));

    return (
        <>
            <div className="page-heading">
                <div>
                    <span className="eyebrow">THE BIG PICTURE</span>
                    <h1>Good things are taking shape.</h1>
                    <p>Here’s where your client work stands.</p>
                </div>
                <Button asChild variant="outline">
                    <Link to={`${base}/opportunities`}>
                        View pipeline <ArrowUpRight size={16} />
                    </Link>
                </Button>
            </div>
            <div className="metrics">
                {[
                    [
                        Building2,
                        "Companies",
                        d.companies,
                        "Relationships worth building",
                    ],
                    [
                        BriefcaseBusiness,
                        "Open opportunities",
                        d.activeOpportunities,
                        "Conversations in motion",
                    ],
                    [
                        FolderKanban,
                        "Active projects",
                        d.activeProjects,
                        "Making things happen",
                    ],
                    [
                        Clock3,
                        "Overdue tasks",
                        d.overdueTasks,
                        "Ready for your attention",
                    ],
                ].map(([Icon, label, value, hint]) => {
                    const I = Icon as typeof Building2;

                    return (
                        <article className="metric panel" key={label as string}>
                            <div className="row-between">
                                <span>{label as string}</span>
                                <I size={18} />
                            </div>
                            <strong>{value as number}</strong>
                            <small>{hint as string}</small>
                        </article>
                    );
                })}
            </div>
            <div className="dashboard-grid">
                <section className="panel pipeline-overview">
                    <div className="panel-heading">
                        <div>
                            <h2>Opportunity overview</h2>
                            <p>A clear view of what’s ahead.</p>
                        </div>
                        <select
                            aria-label="Reporting currency"
                            value={selected?.currency || "USD"}
                            onChange={(e) => setCurrency(e.target.value)}
                        >
                            {(d.values.length
                                ? d.values
                                : [{ currency: "USD" }]
                            ).map((v) => (
                                <option key={v.currency}>{v.currency}</option>
                            ))}
                        </select>
                    </div>
                    <div className="pipeline-values">
                        <div>
                            <span>Open pipeline</span>
                            <strong>
                                {money(selected?.openValue, selected?.currency)}
                            </strong>
                        </div>
                        <div>
                            <span>Won business</span>
                            <strong>
                                {money(selected?.wonValue, selected?.currency)}
                            </strong>
                        </div>
                    </div>
                    <div
                        className="stage-chart"
                        aria-label="Opportunity count by stage"
                    >
                        {d.stages.map((s, i) => (
                            <div className="stage-column" key={s.stage}>
                                <span>{s.count}</span>
                                <div className="bar-track">
                                    <div
                                        className={`stage-bar bar-${i}`}
                                        style={{
                                            height: `${Math.max(2, (s.count / max) * 100)}%`,
                                        }}
                                    />
                                </div>
                                <small>{s.stage}</small>
                            </div>
                        ))}
                    </div>
                    <div className="panel-foot">
                        <span>Opportunity counts across all currencies</span>
                        <Link to={`${base}/opportunities`}>
                            Explore pipeline <ArrowRight size={14} />
                        </Link>
                    </div>
                </section>
                <section className="panel activity-panel">
                    <div className="panel-heading">
                        <div>
                            <h2>Recent activity</h2>
                            <p>The little steps that move work forward.</p>
                        </div>
                        <Sparkles size={18} />
                    </div>
                    {d.activity.length ? (
                        <div className="activity-list">
                            {d.activity.slice(0, 5).map((a, i) => (
                                <article key={a.id}>
                                    <div
                                        className={`activity-dot activity-${i % 3}`}
                                    >
                                        {a.actorName[0]}
                                    </div>
                                    <div>
                                        <p>{a.description}</p>
                                        <small>
                                            {a.actorName} · {date(a.createdAt)}
                                        </small>
                                    </div>
                                </article>
                            ))}
                        </div>
                    ) : (
                        <Empty title="A fresh start">
                            Your team’s changes will appear here.
                        </Empty>
                    )}
                </section>
            </div>
            <section className="panel projects-overview">
                <div className="panel-heading">
                    <div>
                        <h2>Projects in motion</h2>
                        <p>From a promising idea to work you’re proud of.</p>
                    </div>
                    <Link className="text-link" to={`${base}/projects`}>
                        All projects <ArrowRight size={14} />
                    </Link>
                </div>
                {projects.isPending ? (
                    <Loading />
                ) : projects.error ? (
                    <ErrorPanel
                        error={projects.error}
                        retry={() => projects.refetch()}
                    />
                ) : projects.data?.items.length ? (
                    <div className="project-preview-grid">
                        {projects.data.items.map((p, i) => (
                            <Link
                                to={`${base}/projects/${p.id}`}
                                key={p.id}
                                className="project-preview"
                            >
                                <div className="row-between">
                                    <span
                                        className={`project-icon project-icon-${i}`}
                                    >
                                        <FolderKanban size={20} />
                                    </span>
                                    <ArrowUpRight size={16} />
                                </div>
                                <h3>{p.name}</h3>
                                <p>
                                    {p.description ||
                                        "Your next great delivery."}
                                </p>
                                <div className="row-between">
                                    <span className="badge badge-blue">
                                        {humanize(p.status || "")}
                                    </span>
                                    <small>Due {date(p.dueDate)}</small>
                                </div>
                            </Link>
                        ))}
                    </div>
                ) : (
                    <Empty title="Your next project starts here">
                        Create a project or convert a won opportunity.
                    </Empty>
                )}
            </section>
        </>
    );
}
