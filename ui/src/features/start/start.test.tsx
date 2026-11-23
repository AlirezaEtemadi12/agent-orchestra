import { describe, expect, it } from 'vitest';
import { startSteps } from './start-steps';

const hosting = { mode: 'container' as const, roots: ['F:/Projects'], mapped: true };

describe('the start page steps', () => {
  it('is done on every step once a project is connected and a run exists', () => {
    const state = startSteps({
      hosting,
      projects: [{ id: 'shop', lastEventAt: '2026-11-23T08:00:00Z' }],
      runCount: 3,
    });

    expect(state.dashboard).toBe(true);
    expect(state.project).toBe(true);
    expect(state.connected).toBe(true);
    expect(state.run).toBe(true);
    expect(state.pending).toEqual([]);
  });

  it('is not done when Docker has no shared folder', () => {
    const state = startSteps({
      hosting: { mode: 'container', roots: [], mapped: false },
      projects: [],
      runCount: 0,
    });

    expect(state.dashboard).toBe(false);
    expect(state.project).toBe(false);
    expect(state.connected).toBe(false);
    expect(state.run).toBe(false);
  });

  it('is done on step 1 without Docker and lists the projects still unconnected', () => {
    const state = startSteps({
      hosting: { mode: 'direct', roots: ['C:/'], mapped: false },
      projects: [{ id: 'shop' }, { id: 'blog', lastEventAt: null }],
      runCount: 0,
    });

    expect(state.dashboard).toBe(true);
    expect(state.docker).toBe(false);
    expect(state.project).toBe(true);
    expect(state.connected).toBe(false);
    expect(state.pending.map((project) => project.id)).toEqual(['shop', 'blog']);
  });
});
