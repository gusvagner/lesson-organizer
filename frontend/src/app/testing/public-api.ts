export type PublicApi<T> = { [K in keyof T]: T[K] };
