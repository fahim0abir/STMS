import { PageQuery } from './paged-result.model';

export interface AiInteractionLog {
  id: string;
  userId?: string | null;
  userEmail: string;
  userName?: string | null;
  userRole: string;
  taskId?: string | null;
  model: string;
  prompt: string;
  inputText: string;
  outputJson: string;
  durationMs: number;
  createdAt: string;
}

export interface AiInteractionLogQuery extends PageQuery {
  keyword?: string;
  model?: string;
  userId?: string;
  from?: string;
  to?: string;
}
