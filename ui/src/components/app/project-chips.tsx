'use client';

import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';

type ProjectChipsProps = {
  projects: { id: string; name: string }[];
  selected: string | null;
  allLabel: string;
  onSelect: (project: string | null) => void;
};

export function ProjectChips({ projects, selected, allLabel, onSelect }: ProjectChipsProps) {
  return (
    <div className="flex flex-wrap items-center gap-2">
      <Button
        type="button"
        size="sm"
        variant={selected === null ? 'default' : 'outline'}
        onClick={() => onSelect(null)}
      >
        {allLabel}
      </Button>
      {projects.map((project) => (
        <Button
          key={project.id}
          type="button"
          size="sm"
          variant={selected === project.id ? 'default' : 'outline'}
          onClick={() => onSelect(project.id)}
          className={cn('gap-1')}
        >
          {project.name}
        </Button>
      ))}
    </div>
  );
}
