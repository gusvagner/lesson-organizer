import { HttpErrorResponse } from '@angular/common/http';

export interface ApiProblem {
  status: number;
  type: string | null;
  detail: string | null;
  fieldErrors: Record<string, string[]>;
}

interface ProblemBody {
  type?: unknown;
  detail?: unknown;
  errors?: unknown;
}

export function parseProblem(error: unknown): ApiProblem {
  if (!(error instanceof HttpErrorResponse)) {
    return { status: 0, type: null, detail: null, fieldErrors: {} };
  }
  const body = isObject(error.error) ? (error.error as ProblemBody) : {};
  return {
    status: error.status,
    type: typeof body.type === 'string' ? body.type : null,
    detail: typeof body.detail === 'string' ? body.detail : null,
    fieldErrors: fieldErrorsFrom(body.errors),
  };
}

function fieldErrorsFrom(errors: unknown): Record<string, string[]> {
  if (!isObject(errors)) {
    return {};
  }
  const entries = Object.entries(errors as Record<string, unknown>)
    .filter((entry): entry is [string, unknown[]] => Array.isArray(entry[1]))
    .map(([field, messages]) => [field, messages.filter((message) => typeof message === 'string')]);
  return Object.fromEntries(entries);
}

function isObject(value: unknown): boolean {
  return typeof value === 'object' && value !== null;
}
