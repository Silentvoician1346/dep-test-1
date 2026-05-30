const accessTokenStorageKey = "dep-test-1.access-token";

const apiUrl = process.env.NEXT_PUBLIC_API_URL?.replace(/\/$/, "");

export type AuthUser = {
  id: string;
  email: string;
  displayName: string;
  role: string;
  isActive: boolean;
};

export type AuthResponse = {
  accessToken: string;
  tokenType: string;
  expiresAt: string;
  user: AuthUser;
};

export type ProjectTaskJoinRow = {
  userId: string;
  projectId: string;
  projectName: string;
  projectStatus: string;
  taskId: string;
  taskTitle: string;
  taskIsDone: boolean;
  taskCreatedAt: string;
};

export type PagedResponse<T> = {
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
  items: T[];
};

export function getStoredAccessToken() {
  if (typeof window === "undefined") {
    return null;
  }

  return window.localStorage.getItem(accessTokenStorageKey);
}

export function storeAccessToken(accessToken: string) {
  window.localStorage.setItem(accessTokenStorageKey, accessToken);
}

export function clearAccessToken() {
  window.localStorage.removeItem(accessTokenStorageKey);
}

export async function login(email: string, password: string) {
  if (!apiUrl) {
    throw new Error("NEXT_PUBLIC_API_URL is not configured");
  }

  const response = await fetch(`${apiUrl}/api/auth/login`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ email, password }),
  });

  if (!response.ok) {
    throw new Error("Invalid email or password");
  }

  return (await response.json()) as AuthResponse;
}

export async function fetchCurrentUser(accessToken: string) {
  if (!apiUrl) {
    throw new Error("NEXT_PUBLIC_API_URL is not configured");
  }

  const response = await fetch(`${apiUrl}/api/auth/me`, {
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  });

  if (!response.ok) {
    throw new Error("Authentication is required");
  }

  return (await response.json()) as AuthUser;
}

export async function fetchProjectTaskJoins(accessToken: string) {
  if (!apiUrl) {
    throw new Error("NEXT_PUBLIC_API_URL is not configured");
  }

  const response = await fetch(`${apiUrl}/api/projects/task-joins`, {
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  });

  if (!response.ok) {
    throw new Error(`Request failed with status ${response.status}`);
  }

  return (await response.json()) as PagedResponse<ProjectTaskJoinRow>;
}
