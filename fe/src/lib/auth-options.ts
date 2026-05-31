import type { AuthOptions, User } from "next-auth";
import CredentialsProvider from "next-auth/providers/credentials";

import type { AuthUser, BackendAuthResponse } from "@/lib/api-types";
import { authSecret } from "@/lib/auth-secret";
import { loginToBackend } from "@/lib/backend-api";

type BackendTokenUser = User &
  AuthUser & {
    backendAccessToken: string;
    backendAccessTokenExpiresAt: string;
  };

function toBackendTokenUser(response: BackendAuthResponse): BackendTokenUser {
  return {
    id: response.user.id,
    name: response.user.displayName,
    email: response.user.email,
    displayName: response.user.displayName,
    role: response.user.role,
    isActive: response.user.isActive,
    backendAccessToken: response.accessToken,
    backendAccessTokenExpiresAt: response.expiresAt,
  };
}

export const authOptions: AuthOptions = {
  secret: authSecret,
  pages: {
    signIn: "/login",
  },
  session: {
    strategy: "jwt",
    maxAge: 60 * 60,
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
      },
      async authorize(credentials) {
        const email = credentials?.email?.trim();
        const password = credentials?.password;

        if (!email || !password) {
          return null;
        }

        try {
          return toBackendTokenUser(await loginToBackend(email, password));
        } catch {
          return null;
        }
      },
    }),
  ],
  callbacks: {
    async jwt({ token, user }) {
      if (user) {
        const backendUser = user as BackendTokenUser;

        token.backendAccessToken = backendUser.backendAccessToken;
        token.backendAccessTokenExpiresAt =
          backendUser.backendAccessTokenExpiresAt;
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

      return session;
    },
  },
};
