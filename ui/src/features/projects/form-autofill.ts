import type { FolderCheck } from './types';

export type AutoFillInput = {
  name: string;
  id: string;
  runsPath: string;
  repos: string[];
  idTouched: boolean;
  reposTouched: boolean;
};

export type AutoFillPatch = {
  name?: string;
  id?: string;
  runsPath?: string;
  repos?: string[];
};

/**
 * Plan 0009 D14: a check fills only what is empty — a typed value is never overwritten — except the repo
 * list, which the detected repos replace while the owner has not changed it since the last check.
 */
export function fillFromCheck(values: AutoFillInput, check: FolderCheck): AutoFillPatch {
  const patch: AutoFillPatch = {};

  if (values.name.trim().length === 0 && check.suggestedName.length > 0) {
    patch.name = check.suggestedName;
  }

  // The id is not filled here: the form derives it from the name (slugOf) while the owner has not typed one.

  if (values.runsPath.trim().length === 0 && check.suggestedRunsPath.length > 0) {
    patch.runsPath = check.suggestedRunsPath;
  }

  if (!values.reposTouched) {
    patch.repos = [...check.repos];
  }

  return patch;
}
