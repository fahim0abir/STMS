import { describe, expect, it } from 'vitest';
import { TaskPriority, TaskStatus, toRoleName, toTaskPriority, toTaskStatus } from './enums';
import { normalizeTask } from './task.model';
import { normalizeUser } from './user.model';

describe('enum helpers', () => {
  it('maps numeric roles to their names', () => {
    expect(toRoleName(0)).toBe('Admin');
    expect(toRoleName(1)).toBe('ProjectManager');
    expect(toRoleName(2)).toBe('TeamMember');
  });

  it('maps role names back to canonical names', () => {
    expect(toRoleName('ProjectManager')).toBe('ProjectManager');
    expect(toRoleName('unknown-role')).toBe('unknown-role');
    expect(toRoleName(null)).toBe('');
    expect(toRoleName(undefined)).toBe('');
  });

  it('resolves task statuses from numbers, names and unknown input', () => {
    expect(toTaskStatus(2)).toBe(TaskStatus.Completed);
    expect(toTaskStatus('InProgress')).toBe(TaskStatus.InProgress);
    expect(toTaskStatus('bogus')).toBe(TaskStatus.ToDo);
    expect(toTaskStatus(undefined)).toBe(TaskStatus.ToDo);
  });

  it('resolves task priorities from numbers, names and unknown input', () => {
    expect(toTaskPriority(3)).toBe(TaskPriority.Critical);
    expect(toTaskPriority('Low')).toBe(TaskPriority.Low);
    expect(toTaskPriority('bogus')).toBe(TaskPriority.Medium);
  });
});

describe('model normalizers', () => {
  it('normalizes a task payload coming from the API', () => {
    const normalized = normalizeTask({
      id: 't1',
      title: 'Fix bug',
      status: 'Completed',
      priority: 2,
    });

    expect(normalized.status).toBe(TaskStatus.Completed);
    expect(normalized.priority).toBe(TaskPriority.High);
    expect(normalized.title).toBe('Fix bug');
  });

  it('normalizes users and tolerates missing fields', () => {
    expect(normalizeUser({ id: 'u1', email: 'u@example.com', role: 2 } as never)).toEqual({
      id: 'u1',
      email: 'u@example.com',
      name: null,
      role: 'TeamMember',
    });
    expect(normalizeUser(null)).toBeNull();
    expect(normalizeUser(undefined)).toBeNull();
  });
});
