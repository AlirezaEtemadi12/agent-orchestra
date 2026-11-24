import { afterEach, describe, expect, it, vi } from 'vitest';
import { isDesktop, pickFolder } from './bridge';

type Handler = (message: string) => void;

function desktop(): { sent: string[]; answer: (message: string) => void } {
  const sent: string[] = [];
  let handler: Handler = () => undefined;

  vi.stubGlobal('external', {
    sendMessage: (message: string) => {
      sent.push(message);
    },
    receiveMessage: (callback: Handler) => {
      handler = callback;
    },
  });

  return {
    sent,
    answer: (message: string) => {
      handler(message);
    },
  };
}

describe('the desktop bridge', () => {
  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('is not the desktop in a plain browser', () => {
    expect(isDesktop()).toBe(false);
  });

  it('asks the host for a folder and answers with its path', async () => {
    const host = desktop();

    expect(isDesktop()).toBe(true);

    const picked = pickFolder('F:\\Projects');

    host.answer(JSON.stringify({ type: 'pickedFolder', path: 'F:\\Projects\\MyShop' }));

    await expect(picked).resolves.toBe('F:\\Projects\\MyShop');
    expect(JSON.parse(host.sent[0])).toEqual({ type: 'pickFolder', start: 'F:\\Projects' });
  });

  it('answers null when the owner closes the dialog', async () => {
    const host = desktop();
    const picked = pickFolder('');

    host.answer(JSON.stringify({ type: 'pickedFolder', path: '  ' }));

    await expect(picked).resolves.toBeNull();
  });

  it('refuses outside the window', async () => {
    await expect(pickFolder('')).rejects.toThrow('Not running in the Orchestra window.');
  });
});
