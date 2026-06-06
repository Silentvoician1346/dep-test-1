export function isProductionEnvironment() {
  return process.env.NODE_ENV === "production";
}

export function getSentryEnvironment() {
  return (
    process.env.NEXT_PUBLIC_SENTRY_ENVIRONMENT ??
    process.env.SENTRY_ENVIRONMENT ??
    "production"
  );
}

export function getClientSentryDsn() {
  return process.env.NEXT_PUBLIC_SENTRY_DSN;
}

export function getServerSentryDsn() {
  return process.env.SENTRY_DSN ?? process.env.NEXT_PUBLIC_SENTRY_DSN;
}

export function isClientSentryEnabled() {
  return isProductionEnvironment() && Boolean(getClientSentryDsn());
}

export function isServerSentryEnabled() {
  return isProductionEnvironment() && Boolean(getServerSentryDsn());
}
