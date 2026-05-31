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
    backendAccessToken: string;
    backendAccessTokenExpiresAt: string;
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    backendAccessToken?: string;
    backendAccessTokenExpiresAt?: string;
    user?: AppSessionUser;
  }
}
