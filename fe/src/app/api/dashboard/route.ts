import { getToken } from "next-auth/jwt";
import { NextRequest, NextResponse } from "next/server";

import type {
  AuthUser,
  DashboardProject,
  DashboardQuery,
  DashboardResponse,
} from "@/lib/api-types";
import {
  apiProblemTypes,
  type ApiProblem,
} from "@/lib/api-problem";
import { getRequiredAuthSecret } from "@/lib/auth-secret";
import {
  BackendApiError,
  fetchAnnouncements,
  fetchProjects,
  fetchProjectTasks,
} from "@/lib/backend-api";
import { reportError } from "@/lib/sentry-reporting";

export const dynamic = "force-dynamic";

const maxPageSize = 100;
const defaultPage = 1;
const defaultPageSize = 10;

function readPositiveInt(
  params: URLSearchParams,
  name: string,
  fallback: number,
  max = Number.MAX_SAFE_INTEGER,
) {
  const rawValue = params.get(name);
  const value = rawValue ? Number(rawValue) : fallback;

  if (!Number.isFinite(value)) {
    return fallback;
  }

  return Math.min(max, Math.max(1, Math.trunc(value)));
}

function readDashboardQuery(params: URLSearchParams): DashboardQuery {
  return {
    projectsPage: readPositiveInt(params, "projectsPage", defaultPage),
    projectsPageSize: readPositiveInt(
      params,
      "projectsPageSize",
      defaultPageSize,
      maxPageSize,
    ),
    tasksPage: readPositiveInt(params, "tasksPage", defaultPage),
    tasksPageSize: readPositiveInt(
      params,
      "tasksPageSize",
      defaultPageSize,
      maxPageSize,
    ),
    announcementsPage: readPositiveInt(
      params,
      "announcementsPage",
      defaultPage,
    ),
    announcementsPageSize: readPositiveInt(
      params,
      "announcementsPageSize",
      defaultPageSize,
      maxPageSize,
    ),
  };
}

function isExpired(expiresAt?: string) {
  if (!expiresAt) {
    return true;
  }

  return Date.parse(expiresAt) <= Date.now() + 5_000;
}

function createTraceId(request: NextRequest) {
  return request.headers.get("x-request-id") ?? crypto.randomUUID();
}

function problemResponse(
  request: NextRequest,
  status: number,
  title: string,
  type: string,
  detail?: string,
) {
  const traceId = createTraceId(request);
  const problem: ApiProblem = {
    type,
    title,
    status,
    detail,
    instance: request.nextUrl.pathname,
    traceId,
  };

  return NextResponse.json(problem, {
    status,
    headers: {
      "Content-Type": "application/problem+json",
      "X-Request-Id": traceId,
    },
  });
}

export async function GET(request: NextRequest) {
  try {
    const token = await getToken({
      req: request,
      secret: getRequiredAuthSecret(),
    });
    const sessionId =
      typeof token?.backendSessionId === "string"
        ? token.backendSessionId
        : null;
    const user = token?.user as AuthUser | undefined;

    if (!token || !sessionId || !user) {
      return problemResponse(
        request,
        401,
        "Authentication is required.",
        apiProblemTypes.authenticationRequired,
      );
    }

    if (isExpired(token.backendSessionExpiresAt)) {
      return problemResponse(
        request,
        401,
        "Session expired.",
        apiProblemTypes.authenticationRequired,
        "Sign in again to continue.",
      );
    }

    const query = readDashboardQuery(request.nextUrl.searchParams);
    const [projects, announcements] = await Promise.all([
      fetchProjects(sessionId, query.projectsPage, query.projectsPageSize),
      fetchAnnouncements(
        sessionId,
        query.announcementsPage,
        query.announcementsPageSize,
      ),
    ]);

    const projectItems: DashboardProject[] = await Promise.all(
      projects.items.map(async (project) => ({
        ...project,
        tasks: await fetchProjectTasks(
          sessionId,
          project.id,
          query.tasksPage,
          query.tasksPageSize,
        ),
      })),
    );

    const response: DashboardResponse = {
      user,
      query,
      projects: {
        ...projects,
        items: projectItems,
      },
      announcements,
    };

    return NextResponse.json(response);
  } catch (error) {
    if (error instanceof BackendApiError) {
      if (error.status === 401) {
        return problemResponse(
          request,
          401,
          "Authentication is required.",
          apiProblemTypes.authenticationRequired,
          "Sign in again to continue.",
        );
      }

      if (error.status === 403) {
        return problemResponse(
          request,
          403,
          "Access denied.",
          apiProblemTypes.accessDenied,
          "Your account does not have permission to load this dashboard data.",
        );
      }

      if (error.status >= 500) {
        reportError(error, {
          message: "[dashboard] Backend request failed",
          tags: {
            area: "dashboard",
            operation: "load-dashboard",
            status: error.status,
            upstream: "backend",
          },
          extra: {
            backendProblemType: error.problem.type,
            backendTraceId: error.problem.traceId,
            path: request.nextUrl.pathname,
          },
        });

        return problemResponse(
          request,
          502,
          "Backend service failed.",
          apiProblemTypes.upstreamServiceError,
          "The backend service failed while loading the dashboard.",
        );
      }

      return problemResponse(
        request,
        error.status,
        error.problem.title ?? error.message,
        error.problem.type ?? apiProblemTypes.unexpectedError,
        error.problem.detail,
      );
    }

    reportError(error, {
      message: "[dashboard] Unexpected dashboard BFF error",
      tags: {
        area: "dashboard",
        operation: "load-dashboard",
      },
      extra: {
        path: request.nextUrl.pathname,
      },
    });

    return problemResponse(
      request,
      500,
      "An unexpected error occurred.",
      apiProblemTypes.unexpectedError,
    );
  }
}
