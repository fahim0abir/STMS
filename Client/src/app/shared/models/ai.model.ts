export interface AiEnhanceRequest {
  description: string;
  title?: string | null;
  context?: string | null;
}

export interface AiEnhanceResponse {
  enhancedDescription: string;
  suggestedTitle: string;
  acceptanceCriteria: string[];
  subtasks: string[];
  suggestedPriority: number | string;
  estimatedHours?: number | null;
  model: string;
  usedFallback: boolean;
}
