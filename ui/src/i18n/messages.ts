import { boardMessages } from '@/features/board';
import { costMessages } from '@/features/cost';
import { importsMessages } from '@/features/imports';
import { libraryMessages } from '@/features/library';
import { poolsMessages } from '@/features/pools';
import { profilesMessages } from '@/features/profiles';
import { projectsMessages } from '@/features/projects';
import { qualityMessages } from '@/features/quality';
import { runsMessages } from '@/features/runs';
import { startMessages } from '@/features/start';
import { commonEn } from '@/lib/i18n/messages/common.en';
import { commonFa } from '@/lib/i18n/messages/common.fa';

export const messages = {
  en: {
    common: commonEn,
    start: startMessages.en,
    runs: runsMessages.en,
    board: boardMessages.en,
    pools: poolsMessages.en,
    library: libraryMessages.en,
    cost: costMessages.en,
    quality: qualityMessages.en,
    imports: importsMessages.en,
    projects: projectsMessages.en,
    profiles: profilesMessages.en,
  },
  fa: {
    common: commonFa,
    start: startMessages.fa,
    runs: runsMessages.fa,
    board: boardMessages.fa,
    pools: poolsMessages.fa,
    library: libraryMessages.fa,
    cost: costMessages.fa,
    quality: qualityMessages.fa,
    imports: importsMessages.fa,
    projects: projectsMessages.fa,
    profiles: profilesMessages.fa,
  },
} as const;

export type Messages = (typeof messages)['en'];
