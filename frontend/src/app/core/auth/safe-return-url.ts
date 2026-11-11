const DEFAULT_PAGE = '/lesson-plans';

export function safeReturnUrl(value: string | null): string {
  return value !== null && isAppPath(value) ? value : DEFAULT_PAGE;
}

function isAppPath(value: string): boolean {
  return value.startsWith('/') && !value.startsWith('//') && !value.startsWith('/\\');
}
