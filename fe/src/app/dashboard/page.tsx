"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import {
  clearAccessToken,
  fetchCurrentUser,
  fetchProjectTaskJoins,
  getStoredAccessToken,
  type AuthUser,
  type PagedResponse,
  type ProjectTaskJoinRow,
} from "@/lib/auth";

export default function DashboardPage() {
  const router = useRouter();
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isCheckingSession, setIsCheckingSession] = useState(true);
  const [isLoadingJoins, setIsLoadingJoins] = useState(false);
  const [projectTaskJoins, setProjectTaskJoins] =
    useState<PagedResponse<ProjectTaskJoinRow> | null>(null);

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

  async function loadProjectTaskJoins() {
    const accessToken = getStoredAccessToken();

    if (!accessToken) {
      router.replace("/login");
      return;
    }

    setIsLoadingJoins(true);

    try {
      const report = await fetchProjectTaskJoins(accessToken);

      setProjectTaskJoins(report);
      toast.success("Project task data loaded");
    } catch (error) {
      const message =
        error instanceof Error ? error.message : "Unable to load database data";

      toast.error(message);
    } finally {
      setIsLoadingJoins(false);
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
          <div>
            <h2 className="text-base font-semibold">Project Task Join</h2>
            <p className="mt-1 text-sm text-muted-foreground">
              Joined project and task data scoped by your current role.
            </p>
          </div>
          <div>
            <Button
              onClick={loadProjectTaskJoins}
              disabled={isLoadingJoins}
            >
              {isLoadingJoins ? "Loading..." : "Load project task joins"}
            </Button>
          </div>
          {projectTaskJoins ? (
            <pre className="max-h-96 overflow-auto rounded-md border border-border bg-muted p-4 text-xs leading-relaxed text-muted-foreground">
              {JSON.stringify(projectTaskJoins, null, 2)}
            </pre>
          ) : null}
        </section>
      </div>
    </main>
  );
}
