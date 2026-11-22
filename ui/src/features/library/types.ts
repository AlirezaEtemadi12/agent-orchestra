export type LibraryCategory = 'Plan' | 'Preview' | 'Idea' | 'Research' | 'Evidence';

export type LibraryItem = {
  projectId: string;
  projectName: string;
  category: LibraryCategory;
  relPath: string;
  title: string;
  number: string | null;
  slug: string;
  repo: string | null;
  status: string | null;
  createdOn: string | null;
  updatedOn: string | null;
  doneOn: string | null;
  plannedBy: string | null;
  previewRelPath: string | null;
  linkedPlanNumber: string | null;
  runKey: string | null;
  group: string | null;
  modifiedAt: string;
  runStatus: string | null;
  linkedPlanTitle: string | null;
};

export type LibraryRun = {
  runKey: string;
  status: string;
  updatedAt: string | null;
};

export type LibraryItemDetail = {
  item: LibraryItem;
  previewUrl: string | null;
  runs: LibraryRun[];
  evidence: LibraryItem[];
};

export type LibraryFilter = {
  project?: string | null;
  category: LibraryCategory;
  status?: string | null;
  sort?: string | null;
  dir?: string | null;
};
