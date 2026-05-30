"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import {
  clearAccessToken,
  fetchCurrentUser,
  fetchProjectTaskReport,
  getApiUrl,
  getStoredAccessToken,
  type AuthUser,
  type ProjectTaskReportResponse,
} from "@/lib/auth";

export default function DashboardPage() {
  const router = useRouter();
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isCheckingSession, setIsCheckingSession] = useState(true);
  const [isLoadingMessage, setIsLoadingMessage] = useState(false);
  const [isLoadingReport, setIsLoadingReport] = useState(false);
  const [projectTaskReport, setProjectTaskReport] =
    useState<ProjectTaskReportResponse | null>(null);

  useEffect(() => {
    let isCurrent = true;

    async function loadSession() {
      const accessToken = getStoredAccessToken();

      if (!accessToken) {
        router.replace("/login");
        return;
      }

      try {
        const currentUser = await fetchCurrentUser(accessToken);

        if (isCurrent) {
          setUser(currentUser);
        }
      } catch (error) {
        clearAccessToken();

        if (isCurrent) {
          const message =
            error instanceof Error ? error.message : "Authentication failed";

          toast.error(message);
          router.replace("/login");
        }
      } finally {
        if (isCurrent) {
          setIsCheckingSession(false);
        }
      }
    }

    loadSession();

    return () => {
      isCurrent = false;
    };
  }, [router]);

  async function showBackendMessage() {
    const accessToken = getStoredAccessToken();

    if (!accessToken) {
      router.replace("/login");
      return;
    }

    const apiUrl = getApiUrl();

    if (!apiUrl) {
      toast.error("NEXT_PUBLIC_API_URL is not configured");
      return;
    }

    setIsLoadingMessage(true);

    try {
      const response = await fetch(`${apiUrl}/api/message`, {
        headers: {
          Authorization: `Bearer ${accessToken}`,
        },
      });

      if (!response.ok) {
        throw new Error(`Request failed with status ${response.status}`);
      }

      const message = await response.text();
      toast.success(message);
    } catch (error) {
      const message =
        error instanceof Error ? error.message : "Unable to reach backend";

      toast.error(message);
    } finally {
      setIsLoadingMessage(false);
    }
  }

  async function loadProjectTaskReport() {
    const accessToken = getStoredAccessToken();

    if (!accessToken) {
      router.replace("/login");
      return;
    }

    setIsLoadingReport(true);

    try {
      const report = await fetchProjectTaskReport(accessToken);

      setProjectTaskReport(report);
      toast.success("Database report loaded");
    } catch (error) {
      const message =
        error instanceof Error ? error.message : "Unable to load database data";

      toast.error(message);
    } finally {
      setIsLoadingReport(false);
    }
  }

  function logout() {
    clearAccessToken();
    router.replace("/login");
  }

  if (isCheckingSession) {
    return (
      <main className="flex min-h-screen items-center justify-center bg-background px-6 text-foreground">
        <p className="text-sm text-muted-foreground">Checking session...</p>
      </main>
    );
  }

  return (
    <main className="min-h-screen bg-background px-6 py-8 text-foreground">
      <div className="mx-auto flex w-full max-w-4xl flex-col gap-8">
        <header className="flex flex-col gap-4 border-b border-border pb-6 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h1 className="text-2xl font-semibold">Dashboard</h1>
            <p className="mt-1 text-sm text-muted-foreground">
              Signed in as {user?.displayName ?? "Unknown user"}.
            </p>
          </div>
          <Button variant="outline" onClick={logout}>
            Log out
          </Button>
        </header>

        <section className="grid gap-4 sm:grid-cols-3">
          <div className="rounded-md border border-border p-4">
            <p className="text-xs font-medium uppercase text-muted-foreground">
              Email
            </p>
            <p className="mt-2 text-sm font-medium">{user?.email}</p>
          </div>
          <div className="rounded-md border border-border p-4">
            <p className="text-xs font-medium uppercase text-muted-foreground">
              Role
            </p>
            <p className="mt-2 text-sm font-medium">{user?.role}</p>
          </div>
          <div className="rounded-md border border-border p-4">
            <p className="text-xs font-medium uppercase text-muted-foreground">
              Status
            </p>
            <p className="mt-2 text-sm font-medium">
              {user?.isActive ? "Active" : "Inactive"}
            </p>
          </div>
        </section>

        <section className="flex flex-col gap-3">
          <h2 className="text-base font-semibold">Backend Check</h2>
          <div>
            <Button onClick={showBackendMessage} disabled={isLoadingMessage}>
              {isLoadingMessage ? "Loading..." : "Show backend message"}
            </Button>
          </div>
        </section>

        <section className="flex flex-col gap-3">
          <div>
            <h2 className="text-base font-semibold">Database Report</h2>
            <p className="mt-1 text-sm text-muted-foreground">
              Admin-only joined data from users, projects, and tasks.
            </p>
          </div>
          <div>
            <Button
              onClick={loadProjectTaskReport}
              disabled={isLoadingReport || user?.role !== "admin"}
            >
              {isLoadingReport ? "Loading..." : "Load project task report"}
            </Button>
          </div>
          {user?.role !== "admin" ? (
            <p className="text-sm text-muted-foreground">
              Sign in as an admin user to load this report.
            </p>
          ) : null}
          {projectTaskReport ? (
            <pre className="max-h-96 overflow-auto rounded-md border border-border bg-muted p-4 text-xs leading-relaxed text-muted-foreground">
              {JSON.stringify(projectTaskReport, null, 2)}
            </pre>
          ) : null}
        </section>
      </div>
    </main>
  );
}
