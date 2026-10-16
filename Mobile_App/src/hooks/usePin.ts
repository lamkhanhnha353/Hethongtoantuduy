import { useMutation, useQueryClient } from '@tanstack/react-query';

import { profileQueryKey } from '@/lib/queryKeys';
import { setPin } from '@/services/profileService';
import type { SetPinRequestDto } from '@/types/profile';

export function useSetPinMutation() {
  const queryClient = useQueryClient();

  return useMutation<void, unknown, SetPinRequestDto>({
    mutationFn: setPin,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: profileQueryKey });
    },
  });
}
