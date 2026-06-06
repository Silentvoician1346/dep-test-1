"use client";

import { useEffect } from "react";

import { Button } from "@/components/ui/button";
import { reportError } from "@/lib/sentry-reporting";

export default function AppError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    reportError(error, {
      message: "[app] React error boundary caught an error",
      tags: {
        area: "app",
        operation: "react-error-boundary",
      },
      extra: {
        digest: error.digest,
      },
    });
  }, [error]);

  return (
    <main className="flex min-h-screen items-center justify-center bg-background px-6 text-foreground">
      <div className="w-full max-w-md rounded-md border border-border p-6">
        <h1 className="text-xl font-semibold">Something went wrong</h1>
        <p className="mt-2 text-sm text-muted-foreground">
          Refresh the page or try again in a moment.
        </p>
        <Button type="button" className="mt-6" onClick={reset}>
          Try again
        </Button>
      </div>
    </main>
  );
}
