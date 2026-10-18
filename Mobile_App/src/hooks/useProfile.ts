import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { profileQueryKey } from '@/lib/queryKeys';
import { getProfile, updateProfile } from '@/services/profileService';
import type { ProfileResponseDto, UpdateProfileRequestDto } from '@/types/profile';

export function useProfileQuery(enabled = true) {
  return useQuery<ProfileResponseDto>({
    queryKey: profileQueryKey,
    queryFn: getProfile,
    staleTime: 60_000,
    enabled,
  });
}

export function useUpdateProfileMutation() {
  const queryClient = useQueryClient();

  return useMutation<ProfileResponseDto, unknown, UpdateProfileRequestDto>({
    mutationFn: updateProfile,
    onSuccess: (profile) => {
      queryClient.setQueryData(profileQueryKey, profile);
    },
  });
}
