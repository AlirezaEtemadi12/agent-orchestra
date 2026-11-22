export type CostRow = {
  key: string;
  costUsd: number | null;
  runs: number;
  sessions: number;
  calls: number;
  input: number;
  output: number;
  cacheRead: number;
};
