export type FolderCheckItem = {
  name: string;
  kind: 'file' | 'folder';
  found: boolean;
};

export type RepoKnowledge = {
  repo: string;
  found: boolean;
};

/** The answer of `POST api/projects/check` (plan 0003 §6.2). */
export type FolderCheck = {
  exists: boolean;
  isAbsolute: boolean;
  items: FolderCheckItem[];
  repos: string[];
  suggestedRunsPath: string;
  repoKnowledge: RepoKnowledge[];
};

export type ProjectBody = {
  id: string;
  name: string;
  rootPath: string;
  runsPath: string;
  repos: string[];
  profile: string;
};

export type SavedProject = ProjectBody;

export type ProjectStoreState = {
  readable: boolean;
  error: string | null;
};

export type ConfigSourceDto = {
  path: string;
  from: 'profile' | 'project';
};

/** `GET api/projects/{id}/effective` (plan 0006 §6.2). */
export type EffectiveConfigDto = {
  effective: Record<string, unknown>;
  sources: ConfigSourceDto[];
  suggestedOrchestratorJson: string;
  profile: string | null;
  profileVersion: number | null;
  /** The file the API read, root-relative: `.orchestra/orchestrator.json` or the old `orchestrator/…` (plan 0007 D4a). */
  configPath: string;
};
