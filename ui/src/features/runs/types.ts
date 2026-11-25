import type { Paging } from 'gridify-client';

export type RunListItem = {
  projectId: string;
  projectName: string;
  runKey: string;
  taskId: string;
  repo: string | null;
  status: string;
  planNumber: string | null;
  planTitle: string | null;
  task: string | null;
  branch: string | null;
  sessions: number;
  calls: number;
  requests: number;
  costUsd: number | null;
  rounds: number | null;
  models: string[];
  updatedAt: string | null;
  closedAt: string | null;
  source: string;
};

export type StageResult = {
  stage: string;
  round: number | null;
  phase: number | null;
  adapter: string | null;
  model: string | null;
  verdict: string | null;
  blockers: number | null;
  should: number | null;
  nits: number | null;
  changed: number | null;
  outsidePlan: number | null;
  violations: number | null;
  gatesGreen: boolean | null;
  gatesNote: string | null;
  modelVerdict: string | null;
  at: string | null;
};

export type SessionUsage = {
  sessionId: string;
  firstRole: string | null;
  model: string | null;
  calls: number | null;
  requests: number | null;
  input: number | null;
  output: number | null;
  reasoning: number | null;
  cacheRead: number | null;
  costUsd: number | null;
};

export type StageUsage = {
  stage: string;
  model: string | null;
  sessions: number | null;
  calls: number | null;
  requests: number | null;
  input: number | null;
  output: number | null;
  reasoning: number | null;
  cacheRead: number | null;
  seconds: number | null;
  costUsd: number | null;
};

export type StageText = {
  stage: string;
  round: number | null;
  name: string;
  text: string;
};

export type RunPlan = {
  relPath: string;
  number: string | null;
  title: string;
  status: string;
};

/// What the run is doing now, from its latest progress event (plan 0004 §6.1); null when nothing
/// has reported in the last 24 hours.
export type RunLive = {
  stage: string | null;
  model: string | null;
  lastLine: string | null;
  lastOutputAgoSec: number | null;
  pool: string | null;
  heldBy: string | null;
  at: string;
};

export type RunDetail = RunListItem & {
  planVersion: number | null;
  plan: RunPlan | null;
  live: RunLive | null;
  createdAt: string;
  stages: StageResult[];
  roundRows: StageResult[];
  sessionRows: SessionUsage[];
  stageUsage: StageUsage[];
  texts: StageText[];
  totalCostUsd: number | null;
  effectiveConfig: string | null;
};

/** Plan 0010 D7: the KPI cards and the two dropdowns, over the whole filtered set. */
export type RunsSummary = {
  runs: number;
  sessions: number;
  calls: number;
  costUsd: number;
  statuses: string[];
  repos: string[];
};

/** What the Runs page asks from the table and the summary. */
export type RunsQuery = {
  project: string | null;
  status: string | null;
  repo: string | null;
  page: number;
  pageSize: number;
  orderBy: string;
  search: string;
};

export type RunPage = Paging<RunListItem>;
