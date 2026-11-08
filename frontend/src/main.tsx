import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ToastProvider } from "./components/feedback";
import App from "./App";
import "./styles.css";
const client = new QueryClient({
    defaultOptions: {
        queries: {
            retry: false,
            staleTime: 30000,
            refetchOnWindowFocus: false,
        },
        mutations: { retry: false },
    },
});
ReactDOM.createRoot(document.getElementById("root")!).render(
    <React.StrictMode>
        <QueryClientProvider client={client}>
            <BrowserRouter>
                <ToastProvider>
                    <App />
                </ToastProvider>
            </BrowserRouter>
        </QueryClientProvider>
    </React.StrictMode>,
);
