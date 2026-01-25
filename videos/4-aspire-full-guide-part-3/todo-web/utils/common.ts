export interface ProblemDetail {
  detail: string;
}

export type Result<T = void> = Failure | Success<T>;

export type Failure = {
  success: false;
  error: ProblemDetail;
};

export type Success<T = void> = {
  success: true;
  data: T;
};

export function succeed<T = void>(data: T): Success<T> {
  return {
    success: true,
    data,
  };
}

export function fail(error: ProblemDetail): Failure {
  return {
    success: false,
    error,
  };
}
