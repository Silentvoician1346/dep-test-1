"use client";

import { toast } from "sonner";

import { Button } from "@/components/ui/button";

export default function Home() {
  return (
    <main className="flex min-h-screen items-center justify-center bg-background px-6 text-foreground">
      <Button onClick={() => toast.success("Toast from shadcn button")}>
        Show toast
      </Button>
    </main>
  );
}
