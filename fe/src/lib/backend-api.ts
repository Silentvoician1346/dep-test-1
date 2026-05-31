import type {
  Announcement,
  BackendAuthResponse,
  PagedResponse,
  Project,
  ProjectTask,
} from "@/lib/api-types";

type BackendRequestOptions = RequestInit & {
  sessionId?: string;
};

export const backendSessionHeaderName = "X-Session-Id";

export class BackendApiError extends Error {
  constructor(
    message: string,
    public readonly status: number,
  ) {
    super(message);
    this.name = "BackendApiError";
  }
}

function getBackendApiUrl() {
  const value =
    process.env.BACKEND_API_URL ??
    process.env.API_URL ??
    "http://localhost:5000";

  return value.replace(/\/$/, "");
}

async function requestBackend<T>(
  path: string,
  { sessionId, ...init }: BackendRequestOptions = {},
) {
  const headers = new Headers(init.headers);

  if (sessionId) {
    headers.set(backendSessionHeaderName, sessionId);
  }

  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }

  const response = await fetch(`${getBackendApiUrl()}${path}`, {
    ...init,
    headers,
    cache: "no-store",
  });

  if (!response.ok) {
    throw new BackendApiError(
      `Backend request failed with status ${response.status}`,
      response.status,
    );
  }

  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();

  return (text ? JSON.parse(text) : undefined) as T;
}

function toQueryString(params: Record<string, string | number | undefined>) {
  const searchParams = new URLSearchParams();

  for (const [key, value] of Object.entries(params)) {
    if (value !== undefined) {
      searchParams.set(key, String(value));
    }
  }

  const value = searchParams.toString();

  return value ? `?${value}` : "";
}

export function loginToBackend(
  email: string,
  password: string,
  rememberMe: boolean,
) {
  return requestBackend<BackendAuthResponse>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify({ email, password, rememberMe }),
  });
}

export function logoutBackend(sessionId: string) {
  return requestBackend<void>("/api/auth/logout", {
    method: "POST",
    sessionId,
  });
}

export function fetchProjects(
  sessionId: string,
  page: number,
  pageSize: number,
) {
  return requestBackend<PagedResponse<Project>>(
    `/api/projects${toQueryString({ page, pageSize })}`,
    { sessionId },
  );
}

export function fetchProjectTasks(
  sessionId: string,
  projectId: string,
  page: number,
  pageSize: number,
) {
  return requestBackend<PagedResponse<ProjectTask>>(
    `/api/project-tasks${toQueryString({ projectId, page, pageSize })}`,
    { sessionId },
  );
}

export function fetchAnnouncements(
  sessionId: string,
  page: number,
  pageSize: number,
) {
  return requestBackend<PagedResponse<Announcement>>(
    `/api/announcements${toQueryString({ page, pageSize })}`,
    { sessionId },
  );
}
