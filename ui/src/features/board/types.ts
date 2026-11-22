export type BoardWaitingFor = 'approval' | 'manual' | 'needInput' | 'verifyFail' | 'pool';

export type BoardCard = {
  projectId: string;
  runKey: string | null;
  taskId: string;
  status: string;
  planNumber: string | null;
  planTitle: string | null;
  stage: string | null;
  model: string | null;
  startedAt: string;
  elapsedSec: number;
  lastLine: string | null;
  waitingFor: BoardWaitingFor | null;
  pool: string | null;
  heldBy: string | null;
};

export type BoardLanes = {
  running: BoardCard[];
  needsYou: BoardCard[];
  done: BoardCard[];
  failed: BoardCard[];
};

export type BoardResponse = {
  reporter: 'on' | 'off';
  lanes: BoardLanes;
};
