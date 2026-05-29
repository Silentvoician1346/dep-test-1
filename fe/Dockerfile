# Build context must be the repository root:
# docker build -f fe/Dockerfile .
FROM node:22-alpine AS base
WORKDIR /app

ENV PNPM_HOME="/pnpm"
ENV PATH="$PNPM_HOME:$PATH"
RUN corepack enable && corepack prepare pnpm@11.4.0 --activate

FROM base AS deps

COPY package.json pnpm-lock.yaml pnpm-workspace.yaml ./
COPY fe/package.json ./fe/package.json
RUN pnpm --filter fe... install --frozen-lockfile

FROM base AS builder

COPY --from=deps /app/node_modules ./node_modules
COPY --from=deps /app/fe/node_modules ./fe/node_modules
COPY --from=deps /app/fe/package.json ./fe/package.json
COPY . .
RUN pnpm --filter fe build

FROM node:22-alpine AS runner
WORKDIR /app

ENV NODE_ENV=production
ENV PORT=3000
ENV HOSTNAME=0.0.0.0

RUN addgroup --system --gid 1001 nodejs
RUN adduser --system --uid 1001 nextjs

COPY --from=builder /app/fe/public ./fe/public
COPY --from=builder /app/fe/.next/standalone ./
COPY --from=builder /app/fe/.next/static ./fe/.next/static

USER nextjs

EXPOSE 3000

CMD ["node", "fe/server.js"]
