export type ReviewerQuality = {
  model: string;
  runs: number;
  avgRounds: number | null;
  passFirstRound: number | null;
  findingsShare: number | null;
  finalBlockers: number;
  finalShould: number;
  finalNits: number;
};

export type ReadBackQuality = {
  model: string;
  runs: number;
  finalFindingsShare: number | null;
  finalBlockers: number;
  finalShould: number;
  finalNits: number;
};

export type ImplementerQuality = {
  model: string;
  phases: number;
  needInput: number;
  changedAvg: number | null;
  outsidePlan: number;
  violations: number;
  verifyRuns: number;
  verifyFailRate: number | null;
};

export type StageTime = {
  stage: string;
  model: string;
  runs: number;
  avgSeconds: number | null;
};

export type CacheQuality = {
  model: string;
  input: number;
  cacheRead: number;
  hitRate: number | null;
};

export type ModelQuality = {
  reviewers: ReviewerQuality[];
  readBacks: ReadBackQuality[];
  implementers: ImplementerQuality[];
  stageTimes: StageTime[];
  cache: CacheQuality[];
};
