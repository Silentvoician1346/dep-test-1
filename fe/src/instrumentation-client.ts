import * as Sentry from "@sentry/nextjs";

import {
  getClientSentryDsn,
  getSentryEnvironment,
  isClientSentryEnabled,
} from "@/lib/sentry-env";

if (isClientSentryEnabled()) {
  Sentry.init({
    dsn: getClientSentryDsn(),
    environment: getSentryEnvironment(),
    sendDefaultPii: false,
    tracesSampleRate: 0,
  });
}

export const onRouterTransitionStart = (
  ...args: Parameters<typeof Sentry.captureRouterTransitionStart>
) => {
  if (isClientSentryEnabled()) {
    Sentry.captureRouterTransitionStart(...args);
  }
};
