import type { LucideIcon } from 'lucide-react';

export const FEATURE_KEYS = [
  'start',
  'runs',
  'board',
  'pools',
  'library',
  'cost',
  'quality',
  'imports',
  'projects',
  'profiles',
] as const;

export type FeatureKey = (typeof FEATURE_KEYS)[number];

export type MenuCategory = 'overview' | 'work' | 'system' | 'settings';

export type FeatureManifest = {
  key: FeatureKey;
  route: string;
  slices: readonly string[];
};

export type Feature = {
  key: FeatureKey;
  route: string;
  menu: MenuCategory;
  icon: LucideIcon;
  manifest: FeatureManifest;
  messages: { fa: Record<string, unknown>; en: Record<string, unknown> };
};

export function defineFeature(feature: Feature): Feature {
  return feature;
}
