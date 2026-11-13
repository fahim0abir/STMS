export enum Role {
  Admin = 0,
  ProjectManager = 1,
  TeamMember = 2,
}

export enum TaskStatus {
  ToDo = 0,
  InProgress = 1,
  Completed = 2,
  Cancelled = 3,
}

export enum TaskPriority {
  Low = 0,
  Medium = 1,
  High = 2,
  Critical = 3,
}

export const ROLE_LABELS: Record<Role, string> = {
  [Role.Admin]: 'Admin',
  [Role.ProjectManager]: 'ProjectManager',
  [Role.TeamMember]: 'TeamMember',
};

export const TASK_STATUS_LABELS: Record<TaskStatus, string> = {
  [TaskStatus.ToDo]: 'To Do',
  [TaskStatus.InProgress]: 'In Progress',
  [TaskStatus.Completed]: 'Completed',
  [TaskStatus.Cancelled]: 'Cancelled',
};

export const TASK_PRIORITY_LABELS: Record<TaskPriority, string> = {
  [TaskPriority.Low]: 'Low',
  [TaskPriority.Medium]: 'Medium',
  [TaskPriority.High]: 'High',
  [TaskPriority.Critical]: 'Critical',
};

export function toRoleName(value: number | string | null | undefined): string {
  if (value === null || value === undefined) return '';
  if (typeof value === 'number') return ROLE_LABELS[value as Role] ?? '';
  const name = value.trim();
  return (ROLE_LABELS[Role[name as keyof typeof Role]] ?? name) as string;
}

export function toTaskStatus(value: number | string | null | undefined): TaskStatus {
  if (typeof value === 'number') return value as TaskStatus;
  if (typeof value === 'string' && value in TaskStatus) return TaskStatus[value as keyof typeof TaskStatus];
  return TaskStatus.ToDo;
}

export function toTaskPriority(value: number | string | null | undefined): TaskPriority {
  if (typeof value === 'number') return value as TaskPriority;
  if (typeof value === 'string' && value in TaskPriority) return TaskPriority[value as keyof typeof TaskPriority];
  return TaskPriority.Medium;
}
