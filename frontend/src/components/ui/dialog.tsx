import * as DialogPrimitive from "@radix-ui/react-dialog";
import { X } from "lucide-react";
import type { ReactNode } from "react";
export function Modal({
    title,
    description,
    open,
    onClose,
    children,
}: {
    title: string;
    description?: string;
    open: boolean;
    onClose: () => void;
    children: ReactNode;
}) {

    return (
        <DialogPrimitive.Root
            open={open}
            onOpenChange={(value) => {
                if (!value) onClose();
            }}
        >
            <DialogPrimitive.Portal>
                <DialogPrimitive.Overlay className="modal-overlay" />
                <DialogPrimitive.Content className="modal-content">
                    <DialogPrimitive.Title className="modal-title">
                        {title}
                    </DialogPrimitive.Title>
                    <DialogPrimitive.Description className="muted">
                        {description || "Keep your workspace up to date."}
                    </DialogPrimitive.Description>
                    <DialogPrimitive.Close
                        className="modal-close"
                        aria-label="Close dialog"
                    >
                        <X size={20} />
                    </DialogPrimitive.Close>
                    {children}
                </DialogPrimitive.Content>
            </DialogPrimitive.Portal>
        </DialogPrimitive.Root>
    );
}
