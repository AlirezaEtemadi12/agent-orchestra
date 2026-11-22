'use client';

import { QueryClientProvider } from '@tanstack/react-query';
import { ThemeProvider } from 'next-themes';
import { Direction } from 'radix-ui';
import { useState, type ReactNode } from 'react';
import { Toaster } from 'sonner';
import { TooltipProvider } from '@/components/ui/tooltip';
import { createQueryClient } from '@/lib/query/query-client';

type AppProvidersProps = {
  dir: 'rtl' | 'ltr';
  children: ReactNode;
};

export function AppProviders({ dir, children }: AppProvidersProps) {
  const [queryClient] = useState(createQueryClient);

  return (
    <ThemeProvider attribute="class" defaultTheme="system" enableSystem disableTransitionOnChange>
      <QueryClientProvider client={queryClient}>
        <Direction.Provider dir={dir}>
          <TooltipProvider delayDuration={200}>
            {children}
            <Toaster position="bottom-left" />
          </TooltipProvider>
        </Direction.Provider>
      </QueryClientProvider>
    </ThemeProvider>
  );
}
