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

export type { Hosting } from '@/lib/hosting/queries';

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

/** `GET api/projects/{id}/connect` (plan 0009 D8). */
export type Connect = {
  reportBlock: string;
  localKeys: string;
  agentPrompt: string;
  /** False when the dashboard cannot tell its profiles folder in host form (D8). */
  hasProfilesPath: boolean;
};
