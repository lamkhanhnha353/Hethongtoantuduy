export const profileQueryKey = ['profile'] as const;

export const studentsQueryKey = ['students'] as const;

export function studentQueryKey(id: number) {
  return ['students', id] as const;
}
