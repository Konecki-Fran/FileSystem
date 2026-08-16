import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { SearchBox } from './SearchBox';

describe('SearchBox', () => {
  it('emits text changes', () => {
    let value = '';
    render(<SearchBox value={value} onChange={next => { value = next; }} />);
    const input = screen.getByLabelText('Search files');
    fireEvent.change(input, { target: { value: 'doc' } });
    expect(value).toBe('doc');
  });

  it('shows client-side validation feedback', () => {
    render(<SearchBox value="bad/name" onChange={() => undefined} error="Name must not contain '/'." />);
    expect(screen.getByRole('alert')).toHaveTextContent('must not contain');
  });
});
