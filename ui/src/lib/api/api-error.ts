export type ProblemDetails = {
  title?: string;
  status?: number;
  detail?: string;
  code?: string;
};

export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly code: string | undefined,
    message: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }

  static async fromResponse(response: Response): Promise<ApiError> {
    let problem: ProblemDetails = {};

    try {
      problem = (await response.json()) as ProblemDetails;
    } catch {
      // A body that is not JSON leaves the generic message in place.
    }

    return new ApiError(
      response.status,
      problem.code,
      problem.title ?? response.statusText ?? 'Request failed.',
    );
  }
}
