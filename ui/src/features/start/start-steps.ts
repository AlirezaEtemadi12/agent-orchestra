export type StartHosting = {
  mode: 'container' | 'direct';
  roots: string[];
  mapped: boolean;
};

export type StartState = {
  hosting: StartHosting | undefined;
  projects: { id: string; lastEventAt?: string | null }[];
  runCount: number;
};

/** Plan 0009 §6.6: each step is a card with a done/not-done mark from real state; step 5 is text only. */
export function startSteps({ hosting, projects, runCount }: StartState) {
  const connected = projects.filter((project) => Boolean(project.lastEventAt));

  return {
    dashboard: Boolean(hosting && (hosting.mode === 'direct' || hosting.mapped)),
    project: projects.length > 0,
    connected: connected.length > 0,
    run: runCount > 0,
    pending: projects.filter((project) => !project.lastEventAt),
    docker: hosting?.mode === 'container',
  };
}
