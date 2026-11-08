import { useState, type FormEvent } from "react";
import {
    Routes,
    Route,
    Link,
    NavLink,
    Navigate,
    useParams,
    useNavigate,
} from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
    ArrowUpRight,
    ArrowRight,
    LayoutDashboard,
    Building2,
    Users,
    KanbanSquare,
    FolderKanban,
    Settings,
    Menu,
    X,
    LogOut,
    Plus,
    Layers3,
    ShieldCheck,
    ChevronDown,
} from "lucide-react";
import { ConfiguredSession, DemoSession, useSession } from "./lib/session";
import { WorkspaceContext } from "./lib/workspace";
import type { Organization, Page } from "./lib/types";
import { Button } from "./components/ui/button";
import { Loading, ErrorPanel, useToast } from "./components/feedback";
import { Dashboard } from "./pages/Dashboard";
import { ResourcePage, DetailPage } from "./pages/Resources";
import { SettingsPage } from "./pages/Settings";

export default function App() {

    return (
        <Routes>
            <Route path="/" element={<Landing />} />
            <Route
                path="/demo/*"
                element={
                    <DemoSession>
                        <Workspace demo />
                    </DemoSession>
                }
            />
            <Route
                path="/app"
                element={
                    <ConfiguredSession>
                        <Onboarding />
                    </ConfiguredSession>
                }
            />
            <Route
                path="/app/:organizationId/*"
                element={
                    <ConfiguredSession>
                        <Workspace />
                    </ConfiguredSession>
                }
            />
            <Route
                path="/invite"
                element={
                    <ConfiguredSession>
                        <InvitationAccept />
                    </ConfiguredSession>
                }
            />
            <Route
                path="*"
                element={
                    <div className="center-page">
                        <h1>Page not found</h1>
                        <Link to="/">Back to ClientFlow</Link>
                    </div>
                }
            />
        </Routes>
    );
}
function Landing() {

    return (
        <div className="landing">
            <header>
                <Link className="brand" to="/">
                    <span className="brand-mark">
                        <Layers3 size={23} />
                    </span>
                    ClientFlow
                </Link>
                <div className="row">
                    <Link to="/demo">Explore product</Link>
                    <Button asChild variant="outline">
                        <Link to="/app">
                            Sign in <ArrowUpRight size={16} />
                        </Link>
                    </Button>
                </div>
            </header>
            <main className="landing-main">
                <div className="hero-copy">
                    <span className="eyebrow">LESS ADMIN. MORE MOMENTUM.</span>
                    <h1>
                        Great client work.
                        <br />
                        <em>One clear flow.</em>
                    </h1>
                    <p>
                        From the first conversation to the final delivery. A
                        thoughtful workspace for the people building something
                        together.
                    </p>
                    <div className="row">
                        <Button asChild>
                            <Link to="/demo">
                                Explore the demo <ArrowRight size={17} />
                            </Link>
                        </Button>
                        <Button variant="outline" asChild>
                            <Link to="/app">Open your workspace</Link>
                        </Button>
                    </div>
                    <div className="hero-note">
                        <ShieldCheck size={16} />
                        No signup needed to explore · Read-only sample workspace
                    </div>
                </div>
                <div className="product-preview">
                    <div className="preview-chrome">
                        <i />
                        <i />
                        <i />
                        <span>Studio North / Overview</span>
                    </div>
                    <div className="preview-body">
                        <span className="eyebrow">
                            YOUR BUSINESS, AT A GLANCE
                        </span>
                        <h2>A little clarity goes a long way.</h2>
                        <div className="preview-metrics">
                            <div>
                                <small>Open pipeline</small>
                                <strong>$85,500</strong>
                                <span>5 opportunities in motion</span>
                            </div>
                            <div>
                                <small>Won business</small>
                                <strong>$24,000</strong>
                                <span>Ready for the next chapter</span>
                            </div>
                        </div>
                        <div className="preview-bars">
                            {[40, 65, 52, 85, 72, 95, 80, 100].map((v, i) => (
                                <div key={i} style={{ height: v + "%" }} />
                            ))}
                        </div>
                        <div className="row-between">
                            <span>From opportunity to delivery</span>
                            <span className="badge badge-green">In flow</span>
                        </div>
                    </div>
                </div>
            </main>
            <section className="landing-features">
                {[
                    [
                        Building2,
                        "Know your clients",
                        "Keep the people and companies behind the work in one place.",
                    ],
                    [
                        KanbanSquare,
                        "See what’s next",
                        "Turn promising conversations into a clear, actionable pipeline.",
                    ],
                    [
                        FolderKanban,
                        "Deliver with confidence",
                        "Connect won work to projects, owners, and next steps.",
                    ],
                ].map(([Icon, title, text]) => {
                    const I = Icon as typeof Building2;

                    return (
                        <article key={title as string}>
                            <I size={24} />
                            <h3>{title as string}</h3>
                            <p>{text as string}</p>
                        </article>
                    );
                })}
            </section>
            <footer>
                <span>
                    ClientFlow · Client relationships, from first contact to delivery.
                </span>
                <span>Built for small teams with big plans.</span>
            </footer>
        </div>
    );
}
export function Onboarding() {
    const session = useSession(),
        navigate = useNavigate(),
        cache = useQueryClient(),
        [name, setName] = useState("");
    const list = useQuery({
        queryKey: [session.identity, "organizations"],
        queryFn: () =>
            session.request<Page<Organization>>(
                "/api/organizations?pageSize=100",
            ),
    });
    const create = useMutation({
        mutationFn: () =>
            session.request<Organization>("/api/organizations", "POST", {
                name,
            }),
        onSuccess: (org) => {
            cache.clear();
            navigate(`/app/${org.id}`);
        },
    });
    function submit(e: FormEvent) {
        e.preventDefault();
        create.mutate();
    }

    return (
        <div className="center-page">
            <div className="panel setup-panel">
                <Link className="brand" to="/">
                    ◈ ClientFlow
                </Link>
                <h1>Your work starts here.</h1>
                <p>
                    Open an organization or create a new workspace for your
                    team.
                </p>
                {list.isPending ? (
                    <Loading />
                ) : list.error ? (
                    <ErrorPanel
                        error={list.error}
                        retry={() => list.refetch()}
                    />
                ) : (
                    <div className="org-list">
                        {list.data.items.map((org) => (
                            <Link key={org.id} to={`/app/${org.id}`}>
                                <Building2 size={20} />
                                <span>
                                    {org.name}
                                    <small>{org.role}</small>
                                </span>
                                <ArrowRight size={18} />
                            </Link>
                        ))}
                    </div>
                )}
                <form onSubmit={submit}>
                    <label>
                        New organization
                        <input
                            required
                            maxLength={160}
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            placeholder="Your team’s name"
                        />
                    </label>
                    {create.error && <ErrorPanel error={create.error} />}
                    <Button disabled={create.isPending}>
                        {create.isPending ? "Creating…" : "Create organization"}
                    </Button>
                </form>
            </div>
        </div>
    );
}
export function Workspace({ demo = false }: { demo?: boolean }) {
    const params = useParams(),
        orgId = demo ? "demo" : params.organizationId!,
        session = useSession(),
        cache = useQueryClient(),
        navigate = useNavigate();
    const [mobile, setMobile] = useState(false);
    const base = demo ? "/demo" : `/app/${orgId}`;
    const org = useQuery({
        queryKey: [session.identity, orgId, "organization"],
        queryFn: () =>
            session.request<Organization>(`/api/organizations/${orgId}`),
    });
    const orgs = useQuery({
        queryKey: [session.identity, "organizations"],
        queryFn: () =>
            session.request<Page<Organization>>(
                "/api/organizations?pageSize=100",
            ),
    });
    const links = [
        ["", LayoutDashboard, "Overview"],
        ["companies", Building2, "Companies"],
        ["contacts", Users, "Contacts"],
        ["opportunities", KanbanSquare, "Pipeline"],
        ["projects", FolderKanban, "Projects"],
        ["settings", Settings, "Settings"],
    ] as const;
    if (org.isPending)

        return (
            <div className="center-page">
                <Loading />
            </div>
        );
    if (org.error || !org.data)

        return (
            <div className="center-page">
                <ErrorPanel error={org.error} retry={() => org.refetch()} />
                <Link to="/app">Choose another organization</Link>
            </div>
        );

    return (
        <WorkspaceContext.Provider value={{ organization: org.data, base }}>
            <div className="app-shell" key={orgId}>
                {mobile && (
                    <div
                        className="nav-overlay"
                        onClick={() => setMobile(false)}
                    />
                )}
                <aside className={mobile ? "sidebar sidebar-open" : "sidebar"}>
                    <Link to="/" className="brand">
                        <span className="brand-mark">
                            <Layers3 size={23} />
                        </span>
                        ClientFlow
                    </Link>
                    <button
                        className="mobile-close"
                        onClick={() => setMobile(false)}
                        aria-label="Close navigation"
                    >
                        <X />
                    </button>
                    <div className="org-switch">
                        <div className="org-avatar">
                            {org.data.name.slice(0, 1)}
                        </div>
                        <div>
                            <small>WORKSPACE</small>
                            <select
                                aria-label="Organization"
                                value={orgId}
                                onChange={(e) => {
                                    cache.clear();
                                    navigate(`/app/${e.target.value}`);
                                }}
                            >
                                {orgs.data?.items.map((o) => (
                                    <option key={o.id} value={o.id}>
                                        {o.name}
                                    </option>
                                ))}
                            </select>
                        </div>
                        <ChevronDown size={14} />
                    </div>
                    <small className="nav-heading">WORKSPACE</small>
                    <nav>
                        {links.map(([route, Icon, label]) => (
                            <NavLink
                                key={route}
                                to={`${base}/${route}`}
                                end={!route}
                                onClick={() => setMobile(false)}
                            >
                                <Icon size={18} />
                                {label}
                            </NavLink>
                        ))}
                    </nav>
                    <div className="sidebar-bottom">
                        {demo ? (
                            <div className="demo-note">
                                <span className="mini-label">
                                    THE CLIENTFLOW DEMO
                                </span>
                                <strong>Take a look around.</strong>
                                <p>Fictional data. Real possibilities.</p>
                                <Link to="/app">
                                    Open your workspace{" "}
                                    <ArrowUpRight size={14} />
                                </Link>
                            </div>
                        ) : (
                            <Link to="/app" className="sidebar-new">
                                <Plus size={16} />
                                New organization
                            </Link>
                        )}
                        <div className="user-card">
                            <div className="avatar">
                                {session.profile.displayName
                                    .split(" ")
                                    .map((x) => x[0])
                                    .slice(0, 2)
                                    .join("")}
                            </div>
                            <div>
                                <strong>{session.profile.displayName}</strong>
                                <small>{org.data.role}</small>
                            </div>
                            {!demo && (
                                <button
                                    onClick={session.logout}
                                    aria-label="Sign out"
                                >
                                    <LogOut size={17} />
                                </button>
                            )}
                        </div>
                    </div>
                </aside>
                <div className="main-shell">
                    <header className="topbar">
                        <div className="row">
                            <button
                                className="mobile-menu"
                                onClick={() => setMobile(true)}
                                aria-label="Open navigation"
                            >
                                <Menu />
                            </button>
                            <span className="breadcrumb">
                                Workspace <span>/</span>{" "}
                                <strong>{org.data.name}</strong>
                            </span>
                        </div>
                        <div className="row">
                            <span className="connection-dot" />
                            {demo ? "Demo workspace" : "Your team workspace"}
                            <span className="top-avatar">
                                {session.profile.displayName[0]}
                            </span>
                        </div>
                    </header>
                    {demo && (
                        <div className="demo-banner">
                            <ShieldCheck size={15} />
                            <span>
                                Read-only demo · You’re exploring fictional
                                sample data.
                            </span>
                            <Link to="/app">
                                Make it yours <ArrowUpRight size={13} />
                            </Link>
                        </div>
                    )}
                    <main className="workspace-main">
                        <Routes>
                            <Route index element={<Dashboard />} />
                            <Route path="settings" element={<SettingsPage />} />
                            {(
                                [
                                    "companies",
                                    "contacts",
                                    "opportunities",
                                    "projects",
                                ] as const
                            ).map((resource) => (
                                <Route key={resource} path={resource}>
                                    <Route
                                        index
                                        element={
                                            <ResourcePage
                                                key={resource}
                                                resource={resource}
                                            />
                                        }
                                    />
                                    <Route
                                        path=":id"
                                        element={
                                            <DetailPage
                                                key={resource}
                                                resource={resource}
                                            />
                                        }
                                    />
                                </Route>
                            ))}
                            <Route
                                path="*"
                                element={<Navigate to={base} replace />}
                            />
                        </Routes>
                    </main>
                </div>
            </div>
        </WorkspaceContext.Provider>
    );
}
export function InvitationAccept() {
    const session = useSession(),
        navigate = useNavigate(),
        cache = useQueryClient(),
        toast = useToast();
    const token = decodeURIComponent(window.location.hash.slice(1));
    const accept = useMutation({
        mutationFn: () =>
            session.request<{ organizationId: string }>(
                "/api/invitations/accept",
                "POST",
                { token },
            ),
        onSuccess: (r) => {
            cache.clear();
            toast("You joined the organization.");
            navigate(`/app/${r.organizationId}`, { replace: true });
        },
    });

    return (
        <div className="center-page">
            <div className="panel setup-panel">
                <span className="eyebrow">YOU’RE INVITED</span>
                <h1>Good work happens together.</h1>
                <p>
                    Accept this invitation to join the organization as a Member.
                    Links can be used once and expire after seven days.
                </p>
                {accept.error && <ErrorPanel error={accept.error} />}
                <Button
                    disabled={
                        token.length !== 64 ||
                        accept.isPending ||
                        accept.isSuccess
                    }
                    onClick={() => accept.mutate()}
                >
                    {accept.isPending ? "Joining…" : "Accept invitation"}
                </Button>
                {token.length !== 64 && (
                    <p role="alert">This invitation link is incomplete.</p>
                )}
                <Link className="text-link" to="/app">
                    Back to your organizations
                </Link>
            </div>
        </div>
    );
}
