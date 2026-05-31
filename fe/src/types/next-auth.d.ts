import type { DefaultSession, DefaultUser } from "next-auth";

type AppSessionUser = {
  id: string;
  email: string;
  displayName: string;
  role: string;
  isActive: boolean;
};

declare module "next-auth" {
  interface Session {
    user: AppSessionUser & DefaultSession["user"];
  }

  interface User extends DefaultUser, AppSessionUser {
    backendSessionId: string;
    backendSessionExpiresAt: string;
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    backendSessionId?: string;
    backendSessionExpiresAt?: string;
    user?: AppSessionUser;
  }
}
