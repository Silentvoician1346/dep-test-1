import type { NextConfig } from "next";
import { withSentryConfig } from "@sentry/nextjs";

const nextConfig: NextConfig = {
  output: "standalone",
};

export default process.env.NODE_ENV === "production"
  ? withSentryConfig(nextConfig, {
      silent: true,
      telemetry: false,
      sourcemaps: {
        disable: true,
      },
      release: {
        create: false,
      },
      webpack: {
        treeshake: {
          removeDebugLogging: true,
        },
      },
    })
  : nextConfig;
