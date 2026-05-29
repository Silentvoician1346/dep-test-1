import { ArrowUpRight, Server } from "lucide-react";

import { buttonVariants } from "@/components/ui/button";
import { cn } from "@/lib/utils";

export default function Home() {
  return (
    <main className="min-h-screen bg-background px-6 py-10 text-foreground">
      <section className="mx-auto flex min-h-[calc(100vh-5rem)] w-full max-w-4xl flex-col justify-center gap-8">
        <div className="flex items-center gap-3 text-sm font-medium text-muted-foreground">
          <Server className="size-4" aria-hidden="true" />
          Next.js + ASP.NET Core
        </div>

        <div className="space-y-4">
          <h1 className="max-w-2xl text-4xl font-semibold tracking-normal sm:text-5xl">
            Monorepo starter
          </h1>
          <p className="max-w-2xl text-base leading-7 text-muted-foreground sm:text-lg">
            The frontend lives in <code>fe</code>, the controller API lives in{" "}
            <code>be</code>, and both are started from root-level pnpm scripts.
          </p>
        </div>

        <div className="flex flex-col gap-3 sm:flex-row">
          <a
            className={cn(buttonVariants({ size: "lg" }))}
            href="http://localhost:5000/swagger"
          >
            Open Swagger
            <ArrowUpRight data-icon="inline-end" />
          </a>
          <a
            className={cn(buttonVariants({ variant: "outline", size: "lg" }))}
            href="http://localhost:5000/WeatherForecast"
          >
            Weather endpoint
            <ArrowUpRight data-icon="inline-end" />
          </a>
        </div>
      </section>
    </main>
  );
}
