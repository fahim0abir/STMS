export interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string | null;
  statusCode: number;
}
