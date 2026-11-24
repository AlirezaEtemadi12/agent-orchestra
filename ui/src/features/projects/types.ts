export type FolderCheckItem = {
  name: string;
  kind: 'file' | 'folder';
  found: boolean;
  /** Plan 0009 D7: found in every detected repo, not at the root. */
  inEachRepo?: boolean;
};

export type RepoKnowledge = {
  repo: string;
  found: boolean;
};

/** The answer of `POST api/projects/check` (plan 0003 §6.2, plan 0009 D4/D7). */
export type FolderCheck = {
  exists: boolean;
  isAbsolute: boolean;
  items: FolderCheckItem[];
  repos: string[];
  suggestedRunsPath: string;
  repoKnowledge: RepoKnowledge[];
  hostPath: string;
  suggestedName: string;
  isReady: boolean;
  visible: boolean;
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

export type FolderEntry = {
  name: string;
  path: string;
  isReady: boolean;
  isRepo: boolean;
};

/** `GET api/system/folders` (plan 0009 §6.2). */
export type FolderList = {
  path: string | null;
  parent: string | null;
  folders: FolderEntry[];
};

/** `POST api/projects/setup/plan` and `/apply`: the form's values, and the owner's ticks and commands. */
export type SetupRequest = {
  rootPath: string;
  name: string;
  id: string;
  repos: string[];
  // One list per repo, one command per entry: the API binds string[] (plan 0012 §6.3.1).
  verifyCommands: Record<string, string[]> | null;
  skip: string[] | null;
};

export type SetupAction = 'create' | 'add' | 'update' | 'exists';

/** One row of the checklist; `id` is the same across calls, so a removed tick stays removed. */
export type SetupItem = {
  id: string;
  path: string;
  action: SetupAction;
  optional: boolean;
  detail: string | null;
};

export type SetupPlan = {
  engineFound: boolean;
  isSingleRepo: boolean;
  items: SetupItem[];
  verifyCommands: Record<string, string[]>;
};

export type SetupResultItem = {
  id: string;
  ok: boolean;
  error: string | null;
  detail: string | null;
};

export type SetupResult = {
  items: SetupResultItem[];
  project: SavedProject;
  knowledgePrompt: string;
};
