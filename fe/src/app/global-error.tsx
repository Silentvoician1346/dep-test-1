"use client";

import { useEffect } from "react";

import { reportError } from "@/lib/sentry-reporting";

export default function GlobalError({
  error,
}: {
  error: Error & { digest?: string };
}) {
  useEffect(() => {
    reportError(error, {
      message: "[app] Global React error boundary caught an error",
      tags: {
        area: "app",
        operation: "global-react-error-boundary",
      },
      extra: {
        digest: error.digest,
      },
    });
  }, [error]);

  return (
    <html lang="en">
      <body>
        <main
          style={{
            alignItems: "center",
            display: "flex",
            minHeight: "100vh",
            justifyContent: "center",
            padding: "24px",
            fontFamily: "system-ui, sans-serif",
          }}
        >
          <div style={{ maxWidth: "420px" }}>
            <h1>Something went wrong</h1>
            <p>Refresh the page or try again in a moment.</p>
          </div>
        </main>
      </body>
    </html>
  );
}
