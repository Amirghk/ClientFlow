import { createContext, useContext, useState, type ReactNode } from "react";
import { AlertCircle, CheckCircle2 } from "lucide-react";
import { Button } from "./ui/button";
import { ApiError } from "../lib/api";
export function Loading() {

    return (
        <div className="skeletons" aria-label="Loading content">
            <div />
            <div />
            <div />
        </div>
    );
}
export function ErrorPanel({
    error,
    retry,
}: {
    error: Error | null;
    retry?: () => void;
}) {

    return (
        <div className="error-panel" role="alert">
            <AlertCircle size={20} />
            <div>
                <strong>Something needs your attention</strong>
                <p>{error?.message || "The record could not be loaded."}</p>
                {error instanceof ApiError && error.status === 401 && (
                    <Button asChild>
                        <a href="/app">Sign in again</a>
                    </Button>
                )}
                {retry && (
                    <Button variant="outline" onClick={retry}>
                        Reload latest data
                    </Button>
                )}
            </div>
        </div>
    );
}
export function Empty({
    title,
    children,
}: {
    title: string;
    children: ReactNode;
}) {

    return (
        <div className="empty">
            <div className="empty-icon">◇</div>
            <h3>{title}</h3>
            <p>{children}</p>
        </div>
    );
}
const ToastContext = createContext<(message: string) => void>(() => {});
export const useToast = () => useContext(ToastContext);
export function ToastProvider({ children }: { children: ReactNode }) {
    const [message, setMessage] = useState("");

    return (
        <ToastContext.Provider
            value={(text) => {
                setMessage(text);
                window.setTimeout(() => setMessage(""), 4500);
            }}
        >
            {children}
            {message && (
                <div className="toast" role="status">
                    <CheckCircle2 size={18} />
                    {message}
                    <button
                        onClick={() => setMessage("")}
                        aria-label="Dismiss notification"
                    >
                        ×
                    </button>
                </div>
            )}
        </ToastContext.Provider>
    );
}
