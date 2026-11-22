import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { Markdown } from './markdown';

describe('the shared markdown renderer', () => {
  it('renders a GFM table', () => {
    const { container } = render(
      <Markdown source={'| a | b |\n| - | - |\n| 1 | 2 |'} />,
    );

    expect(container.querySelector('table')).not.toBeNull();
    expect(screen.getByText('1')).toBeInTheDocument();
  });

  it('renders no script element from raw HTML', () => {
    const { container } = render(
      <Markdown source={'before\n\n<script>window.hacked = true</script>\n\nafter'} />,
    );

    expect(container.querySelector('script')).toBeNull();
    expect(screen.getByText(/before/)).toBeInTheDocument();
    expect(screen.getByText(/after/)).toBeInTheDocument();
  });
});
