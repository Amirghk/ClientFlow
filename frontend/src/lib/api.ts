export class ApiError extends Error {
    constructor(
        public status: number,
        message: string,
        public errors?: Record<string, string[]>,
        public traceId?: string,
    ) {
        super(message);
    }
}
export async function apiRequest<T>(
    token: string,
    path: string,
    method = "GET",
    body?: unknown,
): Promise<T> {
    const response = await fetch(
        `${(import.meta.env.VITE_API_URL || "http://localhost:5080").replace(/\/$/, "")}${path}`,
        {
            method,
            headers: {
                Authorization: `Bearer ${token}`,
                ...(body ? { "Content-Type": "application/json" } : {}),
            },
            body: body ? JSON.stringify(body) : undefined,
        },
    );
    if (!response.ok) {
        const problem = await response.json().catch(() => ({}));
        const message =
            response.status === 409
                ? "This record changed while you were editing. Reload the latest version, then try again."
                : response.status === 401
                  ? "Your session has expired. Please sign in again."
                  : problem.detail ||
                    Object.values(problem.errors || {})
                        .flat()
                        .join(" ") ||
                    problem.title ||
                    `Request failed (${response.status}).`;
        throw new ApiError(
            response.status,
            message,
            problem.errors,
            problem.traceId,
        );
    }
    if (response.status === 204) return undefined as T;

    return response.json() as Promise<T>;
}
