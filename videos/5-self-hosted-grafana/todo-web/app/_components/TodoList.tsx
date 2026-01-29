"use client";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Skeleton } from "@/components/ui/skeleton";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { useEffect, useState } from "react";
import { toast } from "sonner";
import { TodoResponse } from "../_utils/models";
import {
  createTodoAsync,
  deleteTodoAsync,
  getTodosAsync,
  markTodoAsCompleteAsync,
} from "../_utils/services";

export default function TodoList() {
  const [todos, setTodos] = useState<TodoResponse[]>([]);
  const [newTodoName, setNewTodoName] = useState<string>("");
  const [isTodosLoading, setIsTodosLoading] = useState<boolean>(false);
  const [isBusy, setIsBusy] = useState<boolean>(false);

  const handleGetTodos = async () => {
    setIsTodosLoading(true);

    const result = await getTodosAsync();

    if (!result.success) {
      toast.error(result.error.detail);
      return;
    }

    setTodos(result.data);

    setIsTodosLoading(false);
  };

  const handleCreateTodo = async () => {
    if (!newTodoName.trim()) return;

    setIsBusy(true);

    toast.promise(createTodoAsync({ name: newTodoName }), {
      loading: "Creating todo...",
      success: async (result) => {
        if (!result.success) throw new Error(result.error.detail);

        setNewTodoName("");
        await handleGetTodos();
        return "Todo created successfully!";
      },
      error: (err) => err.message,
      finally: () => {
        setIsBusy(false);
      },
    });
  };

  const handleMarkTodoAsCompleted = async (id: string) => {
    setIsBusy(true);

    toast.promise(markTodoAsCompleteAsync(id), {
      loading: "Marking todo as complete...",
      success: async (result) => {
        if (!result.success) throw new Error(result.error.detail);

        await handleGetTodos();
        return "Todo marked as completed!";
      },
      error: (err) => err.message,
      finally: () => setIsBusy(false),
    });
  };

  const handleDeleteTodo = async (id: string) => {
    setIsBusy(true);

    toast.promise(deleteTodoAsync(id), {
      loading: "Deleting todo...",
      success: async (result) => {
        if (!result.success) throw new Error(result.error.detail);

        await handleGetTodos();
        return "Todo deleted successfully!";
      },
      error: (err) => err.message,
      finally: () => setIsBusy(false),
    });
  };

  useEffect(() => {
    const fetchTodos = async () => {
      await handleGetTodos();
    };

    fetchTodos();
  }, []);

  return (
    <div className="w-full">
      <form
        className="flex items-center justify-between gap-2 py-2"
        onSubmit={(e) => {
          e.preventDefault();
          handleCreateTodo();
        }}
      >
        <Input
          type="text"
          placeholder="Create a new todo..."
          className="max-w-sm"
          value={newTodoName}
          onChange={(e) => setNewTodoName(e.target.value)}
          disabled={isBusy}
        />
        <Button
          type="submit"
          variant="default"
          className="cursor-pointer"
          disabled={isBusy || !newTodoName.trim()}
        >
          Create
        </Button>
      </form>
      <div className="overflow-x-auto rounded-md border">
        <Table className="w-full table-fixed">
          <TableHeader>
            <TableRow>
              <TableHead className="w-1/4">Name</TableHead>
              <TableHead className="w-1/4">Created At</TableHead>
              <TableHead className="w-1/4">Completed</TableHead>
              <TableHead className="w-1/4">Actions</TableHead>
            </TableRow>
          </TableHeader>

          <TableBody>
            {isTodosLoading
              ? Array.from({ length: 10 }).map((_, idx) => (
                  <TableRow key={idx}>
                    <TableCell className="w-1/5">
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                    <TableCell className="w-1/5">
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                    <TableCell className="w-1/5">
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                    <TableCell className="w-1/5">
                      <Skeleton className="h-4 w-full" />
                    </TableCell>
                  </TableRow>
                ))
              : todos.map((todo) => (
                  <TableRow key={todo.id}>
                    <TableCell className="w-1/4">{todo.name}</TableCell>
                    <TableCell className="w-1/4">
                      {new Date(todo.createdAt).toLocaleString()}
                    </TableCell>
                    <TableCell className="w-1/4">
                      {todo.completed ? (
                        <span className="font-medium text-green-600">✓</span>
                      ) : (
                        <span className="font-medium text-red-600">✗</span>
                      )}
                    </TableCell>
                    <TableCell className="w-1/4">
                      <div className="flex gap-2">
                        {!todo.completed && (
                          <Button
                            className="cursor-pointer"
                            variant="default"
                            onClick={() => handleMarkTodoAsCompleted(todo.id)}
                          >
                            Complete
                          </Button>
                        )}
                        <Button
                          className="cursor-pointer"
                          onClick={() => handleDeleteTodo(todo.id)}
                          variant="destructive"
                        >
                          Delete
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
          </TableBody>
        </Table>
      </div>
    </div>
  );
}
