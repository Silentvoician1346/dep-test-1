import { getToken } from "next-auth/jwt";
import { NextRequest, NextResponse } from "next/server";

import type {
  AuthUser,
  DashboardProject,
  DashboardQuery,
  DashboardResponse,
} from "@/lib/api-types";
import { getRequiredAuthSecret } from "@/lib/auth-secret";
import {
  BackendApiError,
  fetchAnnouncements,
  fetchProjects,
  fetchProjectTasks,
} from "@/lib/backend-api";

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

function errorResponse(message: string, status: number) {
  return NextResponse.json({ message }, { status });
}

export async function GET(request: NextRequest) {
  const token = await getToken({
    req: request,
    secret: getRequiredAuthSecret(),
  });
  const accessToken =
    typeof token?.backendAccessToken === "string"
      ? token.backendAccessToken
      : null;
  const user = token?.user as AuthUser | undefined;

  if (!token || !accessToken || !user) {
    return errorResponse("Authentication is required.", 401);
  }

  if (isExpired(token.backendAccessTokenExpiresAt)) {
    return errorResponse("Session expired.", 401);
  }

  const query = readDashboardQuery(request.nextUrl.searchParams);

  try {
    const [projects, announcements] = await Promise.all([
      fetchProjects(accessToken, query.projectsPage, query.projectsPageSize),
      fetchAnnouncements(
        accessToken,
        query.announcementsPage,
        query.announcementsPageSize,
      ),
    ]);

    const projectItems: DashboardProject[] = await Promise.all(
      projects.items.map(async (project) => ({
        ...project,
        tasks: await fetchProjectTasks(
          accessToken,
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
      if (error.status === 401 || error.status === 403) {
        return errorResponse("Authentication is required.", 401);
      }

      return errorResponse(error.message, error.status >= 500 ? 502 : error.status);
    }

    throw error;
  }
}
