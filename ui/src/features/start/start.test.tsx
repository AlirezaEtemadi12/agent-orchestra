import { describe, expect, it } from 'vitest';
import { startSteps } from './start-steps';

describe('the start page steps', () => {
  it('is done on the project step once one project has an event', () => {
    const state = startSteps({
      projects: [{ id: 'shop', lastEventAt: '2026-11-23T08:00:00Z' }],
    });

    expect(state.project).toBe(true);
    expect(state.connected).toBe(true);
    expect(state.pending).toEqual([]);
  });

  it('lists every project without an event as pending', () => {
    const state = startSteps({
      projects: [{ id: 'shop' }, { id: 'blog', lastEventAt: null }],
    });

    expect(state.project).toBe(true);
    expect(state.connected).toBe(false);
    expect(state.pending.map((project) => project.id)).toEqual(['shop', 'blog']);
  });

  it('is not done with no project at all', () => {
    const state = startSteps({ projects: [] });

    expect(state.project).toBe(false);
    expect(state.connected).toBe(false);
  });
});
