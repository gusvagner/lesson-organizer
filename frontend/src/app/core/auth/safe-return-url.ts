const DEFAULT_PAGE = '/lesson-plans';

export function safeReturnUrl(value: string | null | undefined): string {
  return typeof value === 'string' && isAppPath(value) ? value : DEFAULT_PAGE;
}

function isAppPath(value: string): boolean {
  return value.startsWith('/') && !value.startsWith('//') && !value.startsWith('/\\');
}
