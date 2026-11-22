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
