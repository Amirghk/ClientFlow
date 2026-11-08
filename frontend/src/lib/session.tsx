import {
    createContext,
    useContext,
    useEffect,
    useRef,
    type ReactNode,
} from "react";
import { Auth0Provider, useAuth0 } from "@auth0/auth0-react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Link, useNavigate } from "react-router-dom";
import { apiRequest, ApiError } from "./api";
import { demoRequest } from "./demo";
import type { Profile } from "./types";
import { Button } from "../components/ui/button";

type Request = <T>(path: string, method?: string, body?: unknown) => Promise<T>;
interface Session {
    demo: boolean;
    identity: string;
    profile: Profile;
    request: Request;
    logout: () => void;
}
export const SessionContext = createContext<Session | null>(null);
export function useSession() {
    const value = useContext(SessionContext);
    if (!value) throw new Error("Session required");

    return value;
}
export function DemoSession({ children }: { children: ReactNode }) {

    return (
        <SessionContext.Provider
            value={{
                demo: true,
                identity: "demo",
                profile: {
                    id: "user1",
                    displayName: "Alex Morgan",
                    version: "demo",
                },
                request: demoRequest,
                logout: () => {},
            }}
        >
            {children}
        </SessionContext.Provider>
    );
}
const domain = import.meta.env.VITE_AUTH0_DOMAIN,
    clientId = import.meta.env.VITE_AUTH0_CLIENT_ID,
    audience = import.meta.env.VITE_AUTH0_AUDIENCE;
export function ConfiguredSession({ children }: { children: ReactNode }) {
    const navigate = useNavigate();
    if (!domain || !clientId || !audience)

        return (
            <div className="center-page">
                <Link className="brand" to="/">
                    ◈ ClientFlow
                </Link>
                <div className="panel setup-panel">
                    <span className="eyebrow">YOUR WORKSPACE</span>
                    <h1>Sign-in is not available yet.</h1>
                    <p>
                        Authentication is not configured for this instance.
                        Explore the read-only demo to see ClientFlow in action.
                    </p>
                    <Button asChild>
                        <Link to="/demo">Explore the demo →</Link>
                    </Button>
                </div>
            </div>
        );

    return (
        <Auth0Provider
            domain={domain}
            clientId={clientId}
            authorizationParams={{
                audience,
                redirect_uri: window.location.origin + "/app",
                scope: "openid profile email",
            }}
            onRedirectCallback={(state) =>
                navigate(
                    typeof state?.returnTo === "string" &&
                        /^\/(app|invite)(\/|#|$)/.test(state.returnTo)
                        ? state.returnTo
                        : "/app",
                    { replace: true },
                )
            }
        >
            <LiveSession>{children}</LiveSession>
        </Auth0Provider>
    );
}
function LiveSession({ children }: { children: ReactNode }) {
    const auth = useAuth0(),
        cache = useQueryClient(),
        previous = useRef<string | undefined>(undefined);
    const identity = auth.user?.sub;
    useEffect(() => {
        if (previous.current && previous.current !== identity) cache.clear();
        previous.current = identity;
    }, [identity, cache]);
    const request: Request = async (path, method, body) => {
        let token: string | undefined;
        try {
            token = await auth.getAccessTokenSilently();
        } catch (error) {
            if (
                error &&
                typeof error === "object" &&
                "error" in error &&
                [
                    "login_required",
                    "consent_required",
                    "interaction_required",
                ].includes(String(error.error))
            ) {
                throw new ApiError(
                    401,
                    "Your session has expired. Please sign in again.",
                );
            }
            throw error;
        }
        if (!token)
            throw new ApiError(
                401,
                "Your session has expired. Please sign in again.",
            );

        return apiRequest(token, path, method, body);
    };
    const profile = useQuery({
        queryKey: [identity, "profile"],
        queryFn: () => request<Profile>("/api/me", "POST"),
        enabled: auth.isAuthenticated,
        retry: false,
        staleTime: 300000,
    });
    if (auth.isLoading)

        return <div className="center-page">Opening your workspace…</div>;
    if (!auth.isAuthenticated || auth.error)

        return (
            <div className="center-page">
                <div className="panel setup-panel">
                    <span className="eyebrow">CLIENTFLOW</span>
                    <h1>Welcome to your next chapter.</h1>
                    <p>
                        Sign in to bring your clients, opportunities, and
                        projects together.
                    </p>
                    {auth.error && (
                        <p role="alert">
                            Sign-in couldn’t be completed. Please try again.
                        </p>
                    )}
                    <Button
                        onClick={() =>
                            auth.loginWithRedirect({
                                appState: {
                                    returnTo:
                                        window.location.pathname +
                                        window.location.hash,
                                },
                            })
                        }
                    >
                        Sign in securely →
                    </Button>
                    <Link to="/demo" className="text-link">
                        Or explore the read-only demo
                    </Link>
                </div>
            </div>
        );
    if (profile.isPending)

        return <div className="center-page">Preparing your workspace…</div>;
    if (profile.error || !profile.data)

        return (
            <div className="center-page">
                <div className="panel setup-panel">
                    <h1>We couldn’t open your workspace.</h1>
                    <p role="alert">{profile.error?.message}</p>
                    <Button onClick={() => profile.refetch()}>Try again</Button>
                    <Button
                        variant="ghost"
                        onClick={() => auth.loginWithRedirect()}
                    >
                        Sign in again
                    </Button>
                </div>
            </div>
        );

    return (
        <SessionContext.Provider
            value={{
                demo: false,
                identity: identity!,
                profile: profile.data,
                request,
                logout: () => {
                    cache.clear();
                    auth.logout({
                        logoutParams: { returnTo: window.location.origin },
                    });
                },
            }}
        >
            {children}
        </SessionContext.Provider>
    );
}
