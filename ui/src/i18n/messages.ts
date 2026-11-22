import { boardMessages } from '@/features/board';
import { costMessages } from '@/features/cost';
import { importsMessages } from '@/features/imports';
import { libraryMessages } from '@/features/library';
import { poolsMessages } from '@/features/pools';
import { projectsMessages } from '@/features/projects';
import { qualityMessages } from '@/features/quality';
import { runsMessages } from '@/features/runs';
import { commonEn } from '@/lib/i18n/messages/common.en';
import { commonFa } from '@/lib/i18n/messages/common.fa';

export const messages = {
  en: {
    common: commonEn,
    runs: runsMessages.en,
    board: boardMessages.en,
    pools: poolsMessages.en,
    library: libraryMessages.en,
    cost: costMessages.en,
    quality: qualityMessages.en,
    imports: importsMessages.en,
    projects: projectsMessages.en,
  },
  fa: {
    common: commonFa,
    runs: runsMessages.fa,
    board: boardMessages.fa,
    pools: poolsMessages.fa,
    library: libraryMessages.fa,
    cost: costMessages.fa,
    quality: qualityMessages.fa,
    imports: importsMessages.fa,
    projects: projectsMessages.fa,
  },
} as const;

export type Messages = (typeof messages)['en'];
