import type { Paging } from 'gridify-client';

export type ImportErrorDto = {
  file: string;
  location: string | null;
  message: string;
};

export type ImportResultDto = {
  projectId: string;
  startedAt: string;
  finishedAt: string;
  filesRead: number;
  blocksRead: number;
  runsBuilt: number;
  libraryItems: number;
  errorCount: number;
};

export type ImportLogDto = ImportResultDto & {
  id: string;
  errors: ImportErrorDto[];
};

export type ImportLogPage = Paging<ImportLogDto>;

export type ImportLogQuery = {
  page: number;
  pageSize: number;
  orderBy: string;
  search: string;
};
