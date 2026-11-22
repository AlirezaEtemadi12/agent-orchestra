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
