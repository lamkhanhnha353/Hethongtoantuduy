import { useMutation, useQueryClient } from '@tanstack/react-query';

import { profileQueryKey } from '@/lib/queryKeys';
import { setPin, verifyPin } from '@/services/profileService';
import type { SetPinRequestDto, VerifyPinResponseDto } from '@/types/profile';

export function useSetPinMutation() {
  const queryClient = useQueryClient();

  return useMutation<void, unknown, SetPinRequestDto>({
    mutationFn: setPin,
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: profileQueryKey });
    },
  });
}

export function useVerifyPinMutation() {
  return useMutation<VerifyPinResponseDto, unknown, string>({
    mutationFn: (pin) => verifyPin({ pin }),
  });
}
