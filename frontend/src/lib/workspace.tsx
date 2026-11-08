import { createContext, useContext } from "react";
import { useQuery } from "@tanstack/react-query";
import { useSession } from "./session";
import type { Organization, Page, Resource, Row } from "./types";
export const WorkspaceContext = createContext<{
    organization: Organization;
    base: string;
} | null>(null);
export function useWorkspace() {
    const workspace = useContext(WorkspaceContext);
    if (!workspace) throw new Error("Workspace required");

    return workspace;
}
export function useLookup(resource: Resource) {
    const session = useSession(),
        { organization } = useWorkspace();

    return useQuery({
        queryKey: [session.identity, organization.id, resource, "lookup"],
        queryFn: async () => {
            const items: Row[] = [];
            let page = 1;
            while (true) {
                const result = await session.request<Page<Row>>(
                    `/api/organizations/${organization.id}/${resource}?pageSize=100&page=${page}`,
                );
                items.push(...result.items);
                if (items.length >= result.total || result.items.length === 0)

                    return items;
                page++;
            }
        },
    });
}
