import { PageQuery } from './paged-result.model';

export interface Project {
  id: string;
  name: string;
  description?: string | null;
  ownerEmail: string;
}

export interface ProjectUpsert {
  name: string;
  description?: string | null;
}

export interface ProjectQuery extends PageQuery {}
