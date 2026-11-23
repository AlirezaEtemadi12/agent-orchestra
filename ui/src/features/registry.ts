import { boardFeature } from './board';
import { costFeature } from './cost';
import { importsFeature } from './imports';
import { libraryFeature } from './library';
import { poolsFeature } from './pools';
import { profilesFeature } from './profiles';
import { projectsFeature } from './projects';
import { qualityFeature } from './quality';
import { runsFeature } from './runs';
import { startFeature } from './start';

export const FEATURES = [
  startFeature,
  runsFeature,
  boardFeature,
  poolsFeature,
  libraryFeature,
  costFeature,
  qualityFeature,
  importsFeature,
  projectsFeature,
  profilesFeature,
] as const;

export const MENU_ITEMS = FEATURES;
