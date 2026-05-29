"use client";

import { useState } from "react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";

const apiUrl = process.env.NEXT_PUBLIC_API_URL?.replace(/\/$/, "");

export default function Home() {
  const [isLoading, setIsLoading] = useState(false);

  async function showBackendMessage() {
    if (!apiUrl) {
      toast.error("NEXT_PUBLIC_API_URL is not configured");
      return;
    }

    setIsLoading(true);

    try {
      const response = await fetch(`${apiUrl}/api/message`);

      if (!response.ok) {
        throw new Error(`Request failed with status ${response.status}`);
      }

      const message = await response.text();
      toast.success(message);
    } catch (error) {
      const message =
        error instanceof Error ? error.message : "Unable to reach backend";

      toast.error(message);
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-background px-6 text-foreground">
      <Button onClick={showBackendMessage} disabled={isLoading}>
        {isLoading ? "Loading..." : "Show backend message"}
      </Button>
    </main>
  );
}
