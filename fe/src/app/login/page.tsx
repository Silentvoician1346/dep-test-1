"use client";

import { FormEvent, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { toast } from "sonner";

import { Button } from "@/components/ui/button";
import { getStoredAccessToken, login, storeAccessToken } from "@/lib/auth";

export default function LoginPage() {
  const router = useRouter();
  const [email, setEmail] = useState("admin@example.local");
  const [password, setPassword] = useState("Admin123!");
  const [isSubmitting, setIsSubmitting] = useState(false);

  useEffect(() => {
    if (getStoredAccessToken()) {
      router.replace("/dashboard");
    }
  }, [router]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);

    try {
      const result = await login(email, password);

      storeAccessToken(result.accessToken);
      toast.success(`Signed in as ${result.user.displayName}`);
      router.replace("/dashboard");
    } catch (error) {
      const message = error instanceof Error ? error.message : "Login failed";

      toast.error(message);
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-background px-6 py-10 text-foreground">
      <form
        onSubmit={handleSubmit}
        className="flex w-full max-w-sm flex-col gap-5 rounded-md border border-border p-6"
      >
        <div>
          <h1 className="text-2xl font-semibold">Login</h1>
          <p className="mt-1 text-sm text-muted-foreground">
            Sign in to continue to the dashboard.
          </p>
        </div>

        <div className="flex flex-col gap-2">
          <label htmlFor="email" className="text-sm font-medium">
            Email
          </label>
          <input
            id="email"
            type="email"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            className="h-9 rounded-md border border-input bg-background px-3 text-sm outline-none transition-colors focus:border-ring focus:ring-3 focus:ring-ring/50"
            autoComplete="email"
            required
          />
        </div>

        <div className="flex flex-col gap-2">
          <label htmlFor="password" className="text-sm font-medium">
            Password
          </label>
          <input
            id="password"
            type="password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            className="h-9 rounded-md border border-input bg-background px-3 text-sm outline-none transition-colors focus:border-ring focus:ring-3 focus:ring-ring/50"
            autoComplete="current-password"
            required
          />
        </div>

        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting ? "Signing in..." : "Sign in"}
        </Button>
      </form>
    </main>
  );
}
