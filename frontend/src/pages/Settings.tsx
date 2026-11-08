import { useState, useEffect, type FormEvent } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { useSession } from "../lib/session";
import { useWorkspace } from "../lib/workspace";
import { date, type Member, type Invitation } from "../lib/types";
import { Button } from "../components/ui/button";
import { Modal } from "../components/ui/dialog";
import { ErrorPanel, Loading, useToast } from "../components/feedback";

export function SettingsPage() {
    const session = useSession(),
        { organization } = useWorkspace(),
        cache = useQueryClient(),
        toast = useToast(),
        navigate = useNavigate();
    const prefix = `/api/organizations/${organization.id}`;
    const [name, setName] = useState(organization.name),
        [displayName, setDisplayName] = useState(session.profile.displayName);
    const [link, setLink] = useState(""),
        [remove, setRemove] = useState<Member | null>(null);
    useEffect(
        () => setName(organization.name),
        [organization.name, organization.version],
    );
    useEffect(
        () => setDisplayName(session.profile.displayName),
        [session.profile.displayName, session.profile.version],
    );
    const admin = organization.role === "Admin",
        disabled = session.demo;
    const members = useQuery({
        queryKey: [session.identity, organization.id, "members"],
        queryFn: () => session.request<Member[]>(prefix + "/members"),
    });
    const invitations = useQuery({
        queryKey: [session.identity, organization.id, "invitations"],
        queryFn: () => session.request<Invitation[]>(prefix + "/invitations"),
        enabled: admin,
    });
    const update = useMutation({
        mutationFn: ({
            path,
            method,
            body,
        }: {
            path: string;
            method: string;
            body?: unknown;
        }) => session.request(path, method, body),
        onSuccess: async () => {
            await cache.invalidateQueries();
            toast("Changes saved.");
        },
    });
    const invite = useMutation({
        mutationFn: () =>
            session.request<{ token: string }>(prefix + "/invitations", "POST"),
        onSuccess: (result) => {
            setLink(`${window.location.origin}/invite#${result.token}`);
            cache.invalidateQueries({
                queryKey: [session.identity, organization.id, "invitations"],
            });
        },
    });
    function save(e: FormEvent, profile: boolean) {
        e.preventDefault();
        update.mutate({
            path: profile ? "/api/me" : prefix,
            method: "PUT",
            body: profile
                ? { displayName, version: session.profile.version }
                : { name, version: organization.version },
        });
    }
    const lastAdmin = (member: Member) =>
        member.role === "Admin" &&
        members.data?.filter((m) => m.role === "Admin").length === 1;

    return (
        <section>
            <div className="page-heading">
                <div>
                    <span className="eyebrow">MAKE ROOM FOR GOOD WORK</span>
                    <h1>Settings</h1>
                    <p>Your profile, your workspace, your people.</p>
                </div>
            </div>
            {disabled && (
                <div className="notice">
                    Settings are available to explore. Changes are disabled in
                    this read-only demo.
                </div>
            )}
            {update.error && (
                <ErrorPanel
                    error={update.error}
                    retry={() => {
                        update.reset();
                        cache.invalidateQueries();
                    }}
                />
            )}
            <div className="settings-grid">
                <form
                    className="panel settings-card"
                    onSubmit={(e) => save(e, true)}
                >
                    <h2>Your profile</h2>
                    <p className="muted">
                        The name your teammates see in assignments and activity.
                    </p>
                    <label>
                        Display name
                        <input
                            required
                            maxLength={100}
                            value={displayName}
                            onChange={(e) => setDisplayName(e.target.value)}
                            disabled={disabled}
                        />
                    </label>
                    <Button disabled={disabled || update.isPending}>
                        Save profile
                    </Button>
                </form>
                <form
                    className="panel settings-card"
                    onSubmit={(e) => save(e, false)}
                >
                    <h2>Organization</h2>
                    <p className="muted">
                        A shared workspace for your clients and projects.
                    </p>
                    <label>
                        Organization name
                        <input
                            required
                            maxLength={160}
                            value={name}
                            onChange={(e) => setName(e.target.value)}
                            disabled={disabled || !admin}
                        />
                    </label>
                    <Button disabled={disabled || !admin || update.isPending}>
                        Save organization
                    </Button>
                    {!admin && (
                        <small>
                            Only administrators can change organization
                            settings.
                        </small>
                    )}
                </form>
            </div>
            <div className="panel settings-card">
                <h2>People</h2>
                <p className="muted">
                    Members manage client work. Administrators also manage the
                    workspace and its people.
                </p>
                {members.isPending ? (
                    <Loading />
                ) : members.error ? (
                    <ErrorPanel error={members.error} />
                ) : (
                    <div className="table-wrap">
                        <table>
                            <thead>
                                <tr>
                                    <th>Name</th>
                                    <th>Role</th>
                                    <th>Membership</th>
                                </tr>
                            </thead>
                            <tbody>
                                {members.data?.map((member) => (
                                    <tr key={member.userId}>
                                        <td>
                                            {member.displayName}
                                            {member.userId ===
                                                session.profile.id && (
                                                <span className="muted">
                                                    {" "}
                                                    · You
                                                </span>
                                            )}
                                        </td>
                                        <td>
                                            <select
                                                aria-label={`Role for ${member.displayName}`}
                                                value={member.role}
                                                disabled={
                                                    disabled ||
                                                    !admin ||
                                                    lastAdmin(member) ||
                                                    update.isPending
                                                }
                                                onChange={(e) =>
                                                    update.mutate({
                                                        path: `${prefix}/members/${member.userId}/role`,
                                                        method: "PUT",
                                                        body: {
                                                            role: e.target
                                                                .value,
                                                            version:
                                                                organization.version,
                                                        },
                                                    })
                                                }
                                            >
                                                <option>Admin</option>
                                                <option>Member</option>
                                            </select>
                                        </td>
                                        <td>
                                            <Button
                                                variant="ghost"
                                                disabled={
                                                    disabled ||
                                                    !admin ||
                                                    lastAdmin(member) ||
                                                    update.isPending
                                                }
                                                onClick={() =>
                                                    setRemove(member)
                                                }
                                            >
                                                Remove
                                            </Button>
                                            {lastAdmin(member) && (
                                                <small className="muted">
                                                    Last administrator
                                                </small>
                                            )}
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>
            {admin && (
                <div className="panel settings-card">
                    <div className="row-between">
                        <div>
                            <h2>Invitations</h2>
                            <p className="muted">
                                Single-use links grant Member access and expire
                                in seven days. Share only with the person you
                                want to invite.
                            </p>
                        </div>
                        <Button
                            disabled={disabled || invite.isPending}
                            onClick={() => invite.mutate()}
                        >
                            Create invitation
                        </Button>
                    </div>
                    {invite.error && <ErrorPanel error={invite.error} />}
                    {link && (
                        <div className="notice">
                            <strong>
                                Copy this link now. It is shown only once.
                            </strong>
                            <label>
                                Invitation link
                                <input
                                    readOnly
                                    value={link}
                                    onFocus={(e) => e.target.select()}
                                />
                            </label>
                            <Button
                                variant="outline"
                                onClick={async () => {
                                    try {
                                        await navigator.clipboard.writeText(
                                            link,
                                        );
                                        toast("Invitation link copied.");
                                    } catch {
                                        toast(
                                            "Select the link and copy it manually.",
                                        );
                                    }
                                }}
                            >
                                Copy link
                            </Button>
                            <Button variant="ghost" onClick={() => setLink("")}>
                                Dismiss
                            </Button>
                        </div>
                    )}
                    {invitations.isPending ? (
                        <Loading />
                    ) : invitations.error ? (
                        <ErrorPanel error={invitations.error} />
                    ) : (
                        <div className="table-wrap">
                            <table>
                                <thead>
                                    <tr>
                                        <th>Created</th>
                                        <th>Expires</th>
                                        <th>Status</th>
                                        <th>Action</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {invitations.data?.map((item) => {
                                        const state = item.acceptedAt
                                            ? "Accepted"
                                            : item.revoked
                                              ? "Revoked"
                                              : Date.parse(item.expiresAt) <=
                                                  Date.now()
                                                ? "Expired"
                                                : "Pending";

                                        return (
                                            <tr key={item.id}>
                                                <td>{date(item.createdAt)}</td>
                                                <td>{date(item.expiresAt)}</td>
                                                <td>{state}</td>
                                                <td>
                                                    <Button
                                                        variant="ghost"
                                                        disabled={
                                                            disabled ||
                                                            state !==
                                                                "Pending" ||
                                                            update.isPending
                                                        }
                                                        onClick={() =>
                                                            update.mutate({
                                                                path: `${prefix}/invitations/${item.id}/revoke`,
                                                                method: "POST",
                                                                body: {
                                                                    version:
                                                                        item.version,
                                                                },
                                                            })
                                                        }
                                                    >
                                                        Revoke
                                                    </Button>
                                                </td>
                                            </tr>
                                        );
                                    })}
                                </tbody>
                            </table>
                            {!invitations.data?.length && (
                                <p className="muted">No invitations yet.</p>
                            )}
                        </div>
                    )}
                </div>
            )}
            {remove && (
                <Modal
                    title={`Remove ${remove.displayName}?`}
                    open
                    onClose={() => setRemove(null)}
                >
                    <p>
                        Their task assignments will be cleared. Historical
                        activity will remain available.
                    </p>
                    <div className="row">
                        <Button
                            variant="outline"
                            onClick={() => setRemove(null)}
                        >
                            Cancel
                        </Button>
                        <Button
                            variant="destructive"
                            disabled={update.isPending}
                            onClick={() =>
                                update.mutate(
                                    {
                                        path: `${prefix}/members/${remove.userId}/remove`,
                                        method: "POST",
                                        body: { version: organization.version },
                                    },
                                    {
                                        onSuccess: () => {
                                            if (
                                                remove.userId ===
                                                session.profile.id
                                            )
                                                navigate("/app");
                                            setRemove(null);
                                        },
                                    },
                                )
                            }
                        >
                            Remove member
                        </Button>
                    </div>
                </Modal>
            )}
        </section>
    );
}
