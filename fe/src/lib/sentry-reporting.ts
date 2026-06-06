import * as Sentry from "@sentry/nextjs";

import { ApiRequestError } from "@/lib/api-problem";
import { isProductionEnvironment } from "@/lib/sentry-env";

type ReportErrorOptions = {
  message?: string;
  tags?: Record<string, string | number | boolean | undefined>;
  extra?: Record<string, unknown>;
};

export function shouldReportToSentry(error: unknown) {
  return !(error instanceof ApiRequestError) || error.status >= 500;
}

export function reportError(
  error: unknown,
  { message = "Application error", tags, extra }: ReportErrorOptions = {},
) {
  console.error(message, error, {
    tags: normalizeTags(tags),
    extra,
  });

  const sentryEventSent = sendErrorToSentry(error, { tags, extra });

  return { sentryEventSent };
}

export function flushReportedErrors(timeout = 2_000) {
  if (!isProductionEnvironment() || !Sentry.isInitialized()) {
    return Promise.resolve(false);
  }

  return Sentry.flush(timeout);
}

function sendErrorToSentry(
  error: unknown,
  { tags, extra }: Omit<ReportErrorOptions, "message">,
) {
  if (
    !isProductionEnvironment() ||
    !shouldReportToSentry(error) ||
    !Sentry.isInitialized()
  ) {
    return false;
  }

  Sentry.captureException(error, {
    tags: normalizeTags(tags),
    extra,
  });

  return true;
}

function normalizeTags(tags: ReportErrorOptions["tags"]) {
  if (!tags) {
    return undefined;
  }

  return Object.fromEntries(
    Object.entries(tags)
      .filter((entry): entry is [string, string | number | boolean] => {
        const [, value] = entry;

        return value !== undefined;
      })
      .map(([key, value]) => [key, String(value)]),
  );
}
