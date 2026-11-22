import { boardFeature } from './board';
import { costFeature } from './cost';
import { importsFeature } from './imports';
import { libraryFeature } from './library';
import { poolsFeature } from './pools';
import { projectsFeature } from './projects';
import { qualityFeature } from './quality';
import { runsFeature } from './runs';

export const FEATURES = [
  runsFeature,
  boardFeature,
  poolsFeature,
  libraryFeature,
  costFeature,
  qualityFeature,
  importsFeature,
  projectsFeature,
] as const;

export const MENU_ITEMS = FEATURES;
