import type { AuthOptions, User } from "next-auth";
import CredentialsProvider from "next-auth/providers/credentials";

import type { AuthUser, BackendAuthResponse } from "@/lib/api-types";
import { authSecret } from "@/lib/auth-secret";
import {
  BackendApiError,
  loginToBackend,
  logoutBackend,
} from "@/lib/backend-api";
import { reportError } from "@/lib/sentry-reporting";

type BackendSessionUser = User &
  AuthUser & {
    backendSessionId: string;
    backendSessionExpiresAt: string;
  };

function toBackendSessionUser(response: BackendAuthResponse): BackendSessionUser {
  return {
    id: response.user.id,
    name: response.user.displayName,
    email: response.user.email,
    displayName: response.user.displayName,
    role: response.user.role,
    isActive: response.user.isActive,
    backendSessionId: response.sessionId,
    backendSessionExpiresAt: response.expiresAt,
  };
}

function parseRememberMe(value: string | undefined) {
  return value === "true" || value === "on" || value === "1";
}

export const authOptions: AuthOptions = {
  secret: authSecret,
  pages: {
    signIn: "/login",
  },
  session: {
    strategy: "jwt",
    maxAge: 14 * 24 * 60 * 60,
  },
  providers: [
    CredentialsProvider({
      name: "Email and password",
      credentials: {
        email: {
          label: "Email",
          type: "email",
        },
        password: {
          label: "Password",
          type: "password",
        },
        rememberMe: {
          label: "Remember me",
          type: "checkbox",
        },
      },
      async authorize(credentials) {
        const email = credentials?.email?.trim();
        const password = credentials?.password;
        const rememberMe = parseRememberMe(credentials?.rememberMe);

        if (!email || !password) {
          return null;
        }

        try {
          return toBackendSessionUser(
            await loginToBackend(email, password, rememberMe),
          );
        } catch (error) {
          reportError(error, {
            message: "[auth] Backend credentials login failed",
            tags: {
              area: "auth",
              operation: "credentials-login",
              status: error instanceof BackendApiError ? error.status : undefined,
            },
            extra:
              error instanceof BackendApiError
                ? {
                    backendProblemType: error.problem.type,
                    backendTraceId: error.problem.traceId,
                  }
                : undefined,
          });

          return null;
        }
      },
    }),
  ],
  callbacks: {
    async jwt({ token, user }) {
      if (user) {
        const backendUser = user as BackendSessionUser;

        token.backendSessionId = backendUser.backendSessionId;
        token.backendSessionExpiresAt = backendUser.backendSessionExpiresAt;
        token.user = {
          id: backendUser.id,
          email: backendUser.email,
          displayName: backendUser.displayName,
          role: backendUser.role,
          isActive: backendUser.isActive,
        };
      }

      return token;
    },
    async session({ session, token }) {
      if (token.user) {
        session.user = {
          ...session.user,
          ...token.user,
          name: token.user.displayName,
          email: token.user.email,
        };
      }

      if (typeof token.backendSessionExpiresAt === "string") {
        session.expires = token.backendSessionExpiresAt;
      }

      return session;
    },
  },
  events: {
    async signOut({ token }) {
      const sessionId =
        typeof token?.backendSessionId === "string"
          ? token.backendSessionId
          : null;

      if (!sessionId) {
        return;
      }

      try {
        await logoutBackend(sessionId);
      } catch {
        // Auth.js should still clear its cookie when backend session cleanup fails.
      }
    },
  },
};
