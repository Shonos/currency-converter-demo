import { describe, it, expect } from 'vitest';

describe('Setup Test', () => {
  it('should pass a basic test', () => {
    expect(1 + 1).toBe(2);
  });

  it('should have DOM testing library available', () => {
    const div = document.createElement('div');
    div.textContent = 'Hello';
    expect(div.textContent).toBe('Hello');
  });
});
