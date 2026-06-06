"use client";

import { useEffect, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { Bug, ChevronLeft, ChevronRight, LogOut, RefreshCcw } from "lucide-react";
import { signOut } from "next-auth/react";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import type { DashboardQuery, DashboardResponse } from "@/lib/api-types";
import {
  ApiRequestError,
  readApiProblemResponse,
} from "@/lib/api-problem";
import { flushReportedErrors, reportError } from "@/lib/sentry-reporting";

const pageSizeOptions = [10, 25, 50, 100];
const dateFormatter = new Intl.DateTimeFormat("en", {
  dateStyle: "medium",
  timeStyle: "short",
});

const initialQuery: DashboardQuery = {
  projectsPage: 1,
  projectsPageSize: 10,
  tasksPage: 1,
  tasksPageSize: 10,
  announcementsPage: 1,
  announcementsPageSize: 10,
};

async function fetchDashboard(query: DashboardQuery) {
  const searchParams = new URLSearchParams();

  for (const [key, value] of Object.entries(query)) {
    searchParams.set(key, String(value));
  }

  const response = await fetch(`/api/dashboard?${searchParams}`, {
    credentials: "same-origin",
  });

  if (!response.ok) {
    throw new ApiRequestError(
      (await readApiProblemResponse(response)) ?? {
        title: `Request failed with status ${response.status}`,
        status: response.status,
      },
      response.status,
    );
  }

  return (await response.json()) as DashboardResponse;
}

function formatDate(value: string) {
  return dateFormatter.format(new Date(value));
}

function PageSizeSelect({
  label,
  value,
  onChange,
}: {
  label: string;
  value: number;
  onChange: (value: number) => void;
}) {
  return (
    <label className="flex flex-col gap-1.5 text-xs font-medium uppercase text-muted-foreground">
      {label}
      <select
        value={value}
        onChange={(event) => onChange(Number(event.target.value))}
        className="h-8 rounded-md border border-input bg-background px-2 text-sm normal-case text-foreground outline-none transition-colors focus:border-ring focus:ring-3 focus:ring-ring/50"
      >
        {pageSizeOptions.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
    </label>
  );
}

function PageStepper({
  label,
  page,
  totalPages,
  onChange,
}: {
  label: string;
  page: number;
  totalPages: number;
  onChange: (page: number) => void;
}) {
  return (
    <div className="flex items-end gap-2">
      <div className="min-w-0 flex-1">
        <p className="text-xs font-medium uppercase text-muted-foreground">
          {label}
        </p>
        <p className="mt-1 text-sm font-medium">
          {page} / {Math.max(totalPages, 1)}
        </p>
      </div>
      <div className="flex gap-1">
        <Button
          type="button"
          variant="outline"
          size="icon"
          title={`Previous ${label.toLowerCase()}`}
          disabled={page <= 1}
          onClick={() => onChange(Math.max(1, page - 1))}
        >
          <ChevronLeft />
        </Button>
        <Button
          type="button"
          variant="outline"
          size="icon"
          title={`Next ${label.toLowerCase()}`}
          disabled={page >= totalPages}
          onClick={() => onChange(page + 1)}
        >
          <ChevronRight />
        </Button>
      </div>
    </div>
  );
}

export default function DashboardPage() {
  const [query, setQuery] = useState(initialQuery);
  const dashboard = useQuery({
    queryKey: ["dashboard", query],
    queryFn: () => fetchDashboard(query),
  });
  const data = dashboard.data;

  useEffect(() => {
    if (
      dashboard.error instanceof ApiRequestError &&
      dashboard.error.status === 401
    ) {
      toast.error(dashboard.error.message);
      void signOut({ callbackUrl: "/login" });
    }
  }, [dashboard.error]);

  function updateQuery(update: Partial<DashboardQuery>) {
    setQuery((current) => ({
      ...current,
      ...update,
    }));
  }

  function logout() {
    void signOut({ callbackUrl: "/login" });
  }

  async function sendSentryTestError() {
    const { sentryEventSent } = reportError(
      new Error("Manual dashboard Sentry test error"),
      {
        message: "[dashboard] Manual Sentry test error",
        tags: {
          area: "dashboard",
          operation: "manual-sentry-test",
        },
        extra: {
          trigger: "dashboard-test-button",
          userId: data?.user.id,
        },
      },
    );

    if (sentryEventSent) {
      toast.success("Sentry test error sent.");
    } else {
      toast.info("Sentry is disabled outside production or without a DSN.");
      return;
    }

    const flushed = await flushReportedErrors();

    if (flushed) {
      toast.success("Sentry test error queued and flushed.");
    } else {
      toast.error("Sentry test error was queued but did not flush.");
    }
  }

  if (dashboard.isLoading) {
    return (
      <main className="flex min-h-screen items-center justify-center bg-background px-6 text-foreground">
        <p className="text-sm text-muted-foreground">Loading dashboard...</p>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-background px-6 py-8 text-foreground">
      <div className="mx-auto flex w-full max-w-6xl flex-col gap-8">
        <header className="flex flex-col gap-4 border-b border-border pb-6 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h1 className="text-2xl font-semibold">Dashboard</h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Signed in as {data?.user.displayName ?? "Unknown user"}.
            </p>
          </div>
          <div className="flex gap-2">
            <Button
              variant="outline"
              title="Refresh dashboard"
              disabled={dashboard.isFetching}
              onClick={() => dashboard.refetch()}
            >
              <RefreshCcw />
              {dashboard.isFetching ? "Refreshing" : "Refresh"}
            </Button>
            <Button
              variant="outline"
              title="Send test error to Sentry"
              onClick={() => void sendSentryTestError()}
            >
              <Bug />
              Test Sentry
            </Button>
            <Button variant="outline" title="Log out" onClick={logout}>
              <LogOut />
              Log out
            </Button>
          </div>
        </header>

        {dashboard.error &&
        !(
          dashboard.error instanceof ApiRequestError &&
          dashboard.error.status === 401
        ) ? (
          <div className="rounded-md border border-destructive/30 bg-destructive/10 p-4 text-sm text-destructive">
            {dashboard.error.message}
          </div>
        ) : null}

        <section className="grid gap-4 sm:grid-cols-3">
          <div className="rounded-md border border-border p-4">
            <p className="text-xs font-medium uppercase text-muted-foreground">
              Email
            </p>
            <p className="mt-2 truncate text-sm font-medium">
              {data?.user.email ?? "-"}
            </p>
          </div>
          <div className="rounded-md border border-border p-4">
            <p className="text-xs font-medium uppercase text-muted-foreground">
              Role
            </p>
            <p className="mt-2 text-sm font-medium">
              {data?.user.role ?? "-"}
            </p>
          </div>
          <div className="rounded-md border border-border p-4">
            <p className="text-xs font-medium uppercase text-muted-foreground">
              Status
            </p>
            <p className="mt-2 text-sm font-medium">
              {data?.user.isActive ? "Active" : "Inactive"}
            </p>
          </div>
        </section>

        <section className="grid gap-4 rounded-md border border-border p-4 md:grid-cols-3">
          <div className="flex flex-col gap-3">
            <PageStepper
              label="Projects page"
              page={query.projectsPage}
              totalPages={data?.projects.totalPages ?? query.projectsPage}
              onChange={(projectsPage) => updateQuery({ projectsPage })}
            />
            <PageSizeSelect
              label="Projects size"
              value={query.projectsPageSize}
              onChange={(projectsPageSize) =>
                updateQuery({ projectsPage: 1, projectsPageSize })
              }
            />
          </div>
          <div className="flex flex-col gap-3">
            <PageStepper
              label="Tasks page"
              page={query.tasksPage}
              totalPages={Math.max(
                1,
                ...(data?.projects.items.map(
                  (project) => project.tasks.totalPages,
                ) ?? [query.tasksPage]),
              )}
              onChange={(tasksPage) => updateQuery({ tasksPage })}
            />
            <PageSizeSelect
              label="Tasks size"
              value={query.tasksPageSize}
              onChange={(tasksPageSize) =>
                updateQuery({ tasksPage: 1, tasksPageSize })
              }
            />
          </div>
          <div className="flex flex-col gap-3">
            <PageStepper
              label="Announcements page"
              page={query.announcementsPage}
              totalPages={
                data?.announcements.totalPages ?? query.announcementsPage
              }
              onChange={(announcementsPage) =>
                updateQuery({ announcementsPage })
              }
            />
            <PageSizeSelect
              label="Announcements size"
              value={query.announcementsPageSize}
              onChange={(announcementsPageSize) =>
                updateQuery({
                  announcementsPage: 1,
                  announcementsPageSize,
                })
              }
            />
          </div>
        </section>

        <div className="grid gap-8 lg:grid-cols-[minmax(0,1fr)_minmax(320px,380px)]">
          <section className="flex flex-col gap-4">
            <div className="flex items-end justify-between gap-3">
              <div>
                <h2 className="text-base font-semibold">Projects</h2>
                <p className="mt-1 text-sm text-muted-foreground">
                  {data?.projects.totalItems ?? 0} total
                </p>
              </div>
            </div>

            <div className="flex flex-col gap-3">
              {data?.projects.items.length ? (
                data.projects.items.map((project) => (
                  <article
                    key={project.id}
                    className="rounded-md border border-border p-4"
                  >
                    <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between">
                      <div className="min-w-0">
                        <h3 className="truncate text-sm font-semibold">
                          {project.name}
                        </h3>
                        <p className="mt-1 text-xs text-muted-foreground">
                          {project.ownerEmail} - {formatDate(project.createdAt)}
                        </p>
                      </div>
                      <div className="flex shrink-0 gap-2 text-xs">
                        <span className="rounded-md bg-muted px-2 py-1 font-medium">
                          {project.status}
                        </span>
                        <span className="rounded-md bg-muted px-2 py-1 font-medium">
                          {project.taskCount} tasks
                        </span>
                      </div>
                    </div>

                    <div className="mt-4 border-t border-border pt-3">
                      {project.tasks.items.length ? (
                        <ul className="flex flex-col gap-2">
                          {project.tasks.items.map((task) => (
                            <li
                              key={task.id}
                              className="flex items-start justify-between gap-3 text-sm"
                            >
                              <span className="min-w-0 break-words">
                                {task.title}
                              </span>
                              <span className="shrink-0 rounded-md bg-muted px-2 py-1 text-xs font-medium text-muted-foreground">
                                {task.isDone ? "Done" : "Open"}
                              </span>
                            </li>
                          ))}
                        </ul>
                      ) : (
                        <p className="text-sm text-muted-foreground">
                          No tasks.
                        </p>
                      )}
                    </div>
                  </article>
                ))
              ) : (
                <div className="rounded-md border border-border p-4 text-sm text-muted-foreground">
                  No projects.
                </div>
              )}
            </div>
          </section>

          <section className="flex flex-col gap-4">
            <div>
              <h2 className="text-base font-semibold">Announcements</h2>
              <p className="mt-1 text-sm text-muted-foreground">
                {data?.announcements.totalItems ?? 0} total
              </p>
            </div>
            <div className="flex flex-col gap-3">
              {data?.announcements.items.length ? (
                data.announcements.items.map((announcement) => (
                  <article
                    key={announcement.id}
                    className="rounded-md border border-border p-4"
                  >
                    <h3 className="text-sm font-semibold">
                      {announcement.title}
                    </h3>
                    <p className="mt-1 text-xs text-muted-foreground">
                      {formatDate(announcement.publishedAt)}
                    </p>
                    <p className="mt-3 text-sm leading-6">
                      {announcement.body}
                    </p>
                  </article>
                ))
              ) : (
                <div className="rounded-md border border-border p-4 text-sm text-muted-foreground">
                  No announcements.
                </div>
              )}
            </div>
          </section>
        </div>
      </div>
    </main>
  );
}
