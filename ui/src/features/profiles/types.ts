export type ProfileSummary = {
  id: string;
  name: string;
  currentVersion: number;
  updatedAt: string | null;
};

export type ProfileVersion = {
  id: string;
  version: number;
  json: string;
  createdAt: string;
};

export type ProfileVersionSummary = {
  version: number;
  reason: string;
  createdAt: string;
};

export type ConfigChange = {
  path: string;
  from: string | null;
  to: string | null;
};

export type ProfileVersions = {
  versions: ProfileVersionSummary[];
  diff: ConfigChange[] | null;
};

export type ConfigSource = {
  path: string;
  from: 'profile' | 'project';
};

export type EffectiveConfig = {
  effective: Record<string, unknown>;
  sources: ConfigSource[];
  suggestedOrchestratorJson: string;
  profile: string | null;
  profileVersion: number | null;
};
