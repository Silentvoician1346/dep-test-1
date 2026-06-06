export type ApiProblem = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  errors?: Record<string, string[]>;
  [key: string]: unknown;
};

export const apiProblemTypes = {
  authenticationRequired: "urn:dep-test-1:problem:authentication-required",
  accessDenied: "urn:dep-test-1:problem:access-denied",
  upstreamServiceError: "urn:dep-test-1:problem:upstream-service-error",
  unexpectedError: "urn:dep-test-1:problem:unexpected-error",
} as const;

export class ApiRequestError extends Error {
  constructor(
    public readonly problem: ApiProblem,
    public readonly status: number,
  ) {
    super(getApiProblemMessage(problem, `Request failed with status ${status}`));
    this.name = "ApiRequestError";
  }
}

export function parseApiProblem(value: unknown, fallbackStatus: number) {
  if (!isRecord(value)) {
    return null;
  }

  const hasProblemShape =
    typeof value.type === "string" ||
    typeof value.title === "string" ||
    typeof value.detail === "string" ||
    typeof value.status === "number" ||
    isValidationErrors(value.errors);

  if (!hasProblemShape && typeof value.message !== "string") {
    return null;
  }

  return {
    ...value,
    title:
      typeof value.title === "string"
        ? value.title
        : typeof value.message === "string"
          ? value.message
          : undefined,
    status:
      typeof value.status === "number"
        ? value.status
        : fallbackStatus,
    errors: isValidationErrors(value.errors) ? value.errors : undefined,
  } satisfies ApiProblem;
}

export function getApiProblemMessage(problem: ApiProblem, fallback: string) {
  const validationError = firstValidationError(problem.errors);

  return validationError ?? problem.detail ?? problem.title ?? fallback;
}

export async function readApiProblemResponse(response: Response) {
  const text = await response.text();

  if (!text) {
    return null;
  }

  try {
    return parseApiProblem(JSON.parse(text), response.status);
  } catch {
    return {
      title: text,
      status: response.status,
    } satisfies ApiProblem;
  }
}

function firstValidationError(errors: ApiProblem["errors"]) {
  if (!errors) {
    return null;
  }

  for (const messages of Object.values(errors)) {
    const message = messages.find(Boolean);

    if (message) {
      return message;
    }
  }

  return null;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isValidationErrors(value: unknown): value is Record<string, string[]> {
  if (!isRecord(value)) {
    return false;
  }

  return Object.values(value).every(
    (messages) =>
      Array.isArray(messages) &&
      messages.every((message) => typeof message === "string"),
  );
}
