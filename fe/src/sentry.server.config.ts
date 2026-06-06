import * as Sentry from "@sentry/nextjs";

import {
  getSentryEnvironment,
  getServerSentryDsn,
  isServerSentryEnabled,
} from "@/lib/sentry-env";

if (isServerSentryEnabled()) {
  Sentry.init({
    dsn: getServerSentryDsn(),
    environment: getSentryEnvironment(),
    sendDefaultPii: false,
    tracesSampleRate: 0,
  });
}
