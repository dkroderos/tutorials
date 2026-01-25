import { api } from "@/utils/api";
import { fail, Result, succeed } from "@/utils/common";
import { AxiosError } from "axios";
import { CreateTodoRequest, TodoResponse } from "./models";

export async function createTodoAsync(
  request: CreateTodoRequest,
): Promise<Result<TodoResponse>> {
  try {
    const response = await api.post("todos", request);
    return succeed(response.data);
  } catch (error: unknown) {
    if (error instanceof AxiosError) {
      if (error.response) {
        return fail(error.response.data);
      }
    }

    return fail({ detail: "An unknown error occurred during todo creation." });
  }
}

export async function getTodosAsync(): Promise<Result<TodoResponse[]>> {
  try {
    const response = await api.get("todos");
    return succeed(response.data);
  } catch (error: unknown) {
    if (error instanceof AxiosError) {
      if (error.response) {
        return fail(error.response.data);
      }
    }

    return fail({ detail: "An unknown error occurred while fetching todos." });
  }
}

export async function markTodoAsCompleteAsync(id: string) {
  try {
    const response = await api.put(`todos/${id}`);
    return succeed(response.data);
  } catch (error: unknown) {
    if (error instanceof AxiosError) {
      if (error.response) {
        return fail(error.response.data);
      }
    }

    return fail({ detail: "An unknown error occurred while updating todo." });
  }
}

export async function deleteTodoAsync(id: string) {
  try {
    const response = await api.delete(`todos/${id}`);
    return succeed(response.data);
  } catch (error: unknown) {
    if (error instanceof AxiosError) {
      if (error.response) {
        return fail(error.response.data);
      }
    }

    return fail({ detail: "An unknown error occurred while deleting todo." });
  }
}
