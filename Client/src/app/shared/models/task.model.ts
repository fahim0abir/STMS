import { PageQuery } from './paged-result.model';
import { TaskPriority, TaskStatus, toTaskPriority, toTaskStatus } from './enums';

export interface TaskItem {
  id: string;
  projectId?: string;
  title: string;
  description?: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  assigneeId?: string | null;
  assigneeEmail?: string | null;
  assigneeName?: string | null;
  dueAt?: string | null;
  createdAt?: string;
  updatedAt?: string | null;
}

export interface TaskItemUpsert {
  title: string;
  description?: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  assigneeId?: string | null;
  dueAt?: string | null;
}

export interface TaskItemQuery extends PageQuery {
  status?: TaskStatus;
  priority?: TaskPriority;
  assigneeId?: string;
  dueFrom?: string;
  dueTo?: string;
}

export function normalizeTask(
  raw: Omit<TaskItem, 'status' | 'priority'> & { status: number | string; priority: number | string },
): TaskItem {
  return {
    ...raw,
    status: toTaskStatus(raw.status),
    priority: toTaskPriority(raw.priority),
  };
}
