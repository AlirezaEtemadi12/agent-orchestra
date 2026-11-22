/** Copies the prompt; false when the browser refuses, so the caller can select the text instead. */
export async function copyText(text: string): Promise<boolean> {
  try {
    await navigator.clipboard.writeText(text);
    return true;
  } catch {
    return false;
  }
}

/** The fallback: the text is selected and one Ctrl+C away. */
export function selectText(node: HTMLElement | null): void {
  if (!node) {
    return;
  }

  const selection = window.getSelection();

  if (!selection) {
    return;
  }

  const range = document.createRange();
  range.selectNodeContents(node);
  selection.removeAllRanges();
  selection.addRange(range);
}
