export type PoolSlot = {
  n: number;
  heldBy: string | null;
  since: string | null;
  unknown: boolean;
};

export type PoolWaiting = {
  taskId: string;
  since: string;
};

export type Pool = {
  pool: string;
  tier: string | null;
  slots: PoolSlot[];
  waiting: PoolWaiting[];
  cooldownUntil: string | null;
  exhaustedUntil: string | null;
};
