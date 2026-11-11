export function typeInto(root: HTMLElement, selector: string, value: string): void {
  const input = query<HTMLInputElement | HTMLTextAreaElement>(root, selector);
  input.value = value;
  input.dispatchEvent(new Event('input'));
  input.dispatchEvent(new Event('blur'));
}

export function click(root: HTMLElement, selector: string): void {
  query<HTMLElement>(root, selector).click();
}

export function query<T extends Element>(root: ParentNode, selector: string): T {
  const element = root.querySelector<T>(selector);
  if (element === null) {
    throw new Error(`No element matches "${selector}"; expected one in the rendered template.`);
  }
  return element;
}

export function textOf(root: ParentNode): string {
  return (root as HTMLElement).textContent?.replace(/\s+/g, ' ').trim() ?? '';
}

export function settle(ms = 0): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

export async function chooseOption(
  root: HTMLElement,
  selectSelector: string,
  optionText: string,
): Promise<void> {
  click(root, `${selectSelector} .mat-mdc-select-trigger`);
  await settle();
  const options = Array.from(document.querySelectorAll<HTMLElement>('mat-option'));
  const option = options.find((candidate) => textOf(candidate) === optionText);
  if (option === undefined) {
    throw new Error(
      `No option "${optionText}" in ${selectSelector}; found ${options.map(textOf).join(', ')}.`,
    );
  }
  option.click();
  await settle();
}
