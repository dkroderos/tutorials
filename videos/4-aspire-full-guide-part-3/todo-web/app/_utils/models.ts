export interface CreateTodoRequest {
  name: string;
}

export interface TodoResponse {
  id: string;
  name: string;
  completed: boolean;
  createdAt: string;
}
