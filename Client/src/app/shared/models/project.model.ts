import { PageQuery } from './paged-result.model';

export interface Project {
  id: string;
  name: string;
  description?: string | null;
  managerId?: string | null;
  managerEmail?: string | null;
  managerName?: string | null;
}

export interface ProjectUpsert {
  name: string;
  description?: string | null;
  managerId?: string | null;
}

export interface ProjectQuery extends PageQuery {}
