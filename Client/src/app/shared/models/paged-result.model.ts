export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface PageQuery {
  keyword?: string;
  sortBy?: string;
  sortOrder?: 'asc' | 'desc';
  pageNumber?: number;
  pageSize?: number;
}
