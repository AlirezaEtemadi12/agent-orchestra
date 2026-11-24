export type StartState = {
  projects: { id: string; lastEventAt?: string | null }[];
};

/** Plan 0012 D16: two steps. The first is true by the time the page renders; the second turns on with the
 * first event of the first project, so nothing asks the owner which hosting they use. */
export function startSteps({ projects }: StartState) {
  const pending = projects.filter((project) => !project.lastEventAt);

  return {
    project: projects.length > 0,
    connected: projects.some((project) => Boolean(project.lastEventAt)),
    pending,
  };
}
