import * as Sentry from "@sentry/nextjs";

import { isServerSentryEnabled } from "@/lib/sentry-env";

export async function register() {
  if (!isServerSentryEnabled()) {
    return;
  }

  if (process.env.NEXT_RUNTIME === "nodejs") {
    await import("./sentry.server.config");
  }

  if (process.env.NEXT_RUNTIME === "edge") {
    await import("./sentry.edge.config");
  }
}

export const onRequestError = (
  ...args: Parameters<typeof Sentry.captureRequestError>
) => {
  if (isServerSentryEnabled()) {
    Sentry.captureRequestError(...args);
  }
};
