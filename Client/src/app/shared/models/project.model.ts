import { PageQuery } from './paged-result.model';

export enum ProjectStatus {
  Active = 0,
  OnHold = 1,
  Completed = 2,
  Archived = 3,
}

export const PROJECT_STATUS_LABELS: Record<ProjectStatus, string> = {
  [ProjectStatus.Active]: 'Active',
  [ProjectStatus.OnHold]: 'On Hold',
  [ProjectStatus.Completed]: 'Completed',
  [ProjectStatus.Archived]: 'Archived',
};

export function toProjectStatus(value: number | string | null | undefined): ProjectStatus {
  if (typeof value === 'number') return value as ProjectStatus;
  if (typeof value === 'string' && value in ProjectStatus) return ProjectStatus[value as keyof typeof ProjectStatus];
  return ProjectStatus.Active;
}

export interface Project {
  id: string;
  name: string;
  description?: string | null;
  clientName?: string | null;
  techStack: string;
  startDate: string;
  endDate?: string | null;
  status: ProjectStatus;
  managerId?: string | null;
  managerEmail?: string | null;
  managerName?: string | null;
}

export interface ProjectUpsert {
  name: string;
  description?: string | null;
  clientName?: string | null;
  techStack: string;
  startDate: string;
  endDate?: string | null;
  status: ProjectStatus;
  managerId?: string | null;
}

export interface ProjectQuery extends PageQuery {
  status?: ProjectStatus;
  includeArchived?: boolean;
}
