export interface DashboardStats {
  scope: string;
  totalProjects: number;
  totalTasks: number;
  completedTasks: number;
  pendingTasks: number;
  upcomingDueTasks: number;
  tasksByStatus: Record<string, number>;
  tasksByPriority: Record<string, number>;
}
