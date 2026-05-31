export const authSecret =
  process.env.AUTH_SECRET ??
  process.env.NEXTAUTH_SECRET ??
  (process.env.NODE_ENV === "production"
    ? undefined
    : "dep-test-1-local-auth-secret-change-me");

export function getRequiredAuthSecret() {
  if (!authSecret) {
    throw new Error("AUTH_SECRET or NEXTAUTH_SECRET is required.");
  }

  return authSecret;
}
