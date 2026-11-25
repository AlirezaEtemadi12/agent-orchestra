'use client';

import { Search } from 'lucide-react';
import { useTranslations } from 'next-intl';
import { useEffect, useState, type ReactNode } from 'react';

export function Toolbar({
  search,
  onSearch,
  children,
}: {
  search: string;
  onSearch: (value: string) => void;
  children?: ReactNode;
}) {
  const common = useTranslations('common');
  const [value, setValue] = useState(search);
  const [lastSearch, setLastSearch] = useState(search);

  if (search !== lastSearch) {
    setLastSearch(search);
    setValue(search);
  }

  useEffect(() => {
    if (value === search) {
      return;
    }

    const timer = setTimeout(() => onSearch(value), 300);

    return () => clearTimeout(timer);
  }, [value, search, onSearch]);

  return (
    <div className="flex flex-wrap items-center gap-2 text-sm">
      <label className="relative flex items-center">
        <span className="sr-only">{common('table.search')}</span>
        <Search
          className="pointer-events-none absolute start-2 size-4 text-muted-foreground"
          aria-hidden
        />
        <input
          className="h-8 w-64 rounded-md border bg-background pe-2 ps-8"
          value={value}
          onChange={(event) => setValue(event.target.value)}
          placeholder={common('table.searchPlaceholder')}
        />
      </label>

      {children}
    </div>
  );
}
