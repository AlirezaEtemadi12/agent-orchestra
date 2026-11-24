/** Plan 0012 D3b: in the Orchestra window the folder dialog belongs to the host; in a browser there is none. */

type DesktopExternal = {
  sendMessage: (message: string) => void;
  receiveMessage: (callback: (message: string) => void) => void;
};

const PICK_TIMEOUT_MS = 5 * 60 * 1000;

function external(): DesktopExternal | undefined {
  if (typeof window === 'undefined') {
    return undefined;
  }

  const candidate = (window as unknown as { external?: Partial<DesktopExternal> }).external;

  return typeof candidate?.sendMessage === 'function' && typeof candidate.receiveMessage === 'function'
    ? (candidate as DesktopExternal)
    : undefined;
}

export function isDesktop(): boolean {
  return external() !== undefined;
}

/** Asks the host for a folder; `null` when the owner closed the dialog without choosing. */
export function pickFolder(start: string): Promise<string | null> {
  return new Promise((resolve, reject) => {
    const api = external();

    if (!api) {
      reject(new Error('Not running in the Orchestra window.'));
      return;
    }

    const timer = window.setTimeout(() => {
      reject(new Error('The folder dialog did not answer.'));
    }, PICK_TIMEOUT_MS);

    api.receiveMessage((message) => {
      let answer: { type?: string; path?: string };

      try {
        answer = JSON.parse(message) as { type?: string; path?: string };
      } catch {
        return;
      }

      if (answer.type !== 'pickedFolder') {
        return;
      }

      window.clearTimeout(timer);

      const path = (answer.path ?? '').trim();

      resolve(path.length > 0 ? path : null);
    });

    api.sendMessage(JSON.stringify({ type: 'pickFolder', start }));
  });
}
