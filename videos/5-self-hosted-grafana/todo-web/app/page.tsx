import TodoList from "./_components/TodoList";

export default function Home() {
  return (
    <main className="flex min-h-screen justify-center pt-16">
      <div className="mx-auto w-full max-w-7xl">
        <TodoList />
      </div>
    </main>
  );
}
