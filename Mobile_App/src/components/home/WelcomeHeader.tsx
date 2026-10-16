import { Text, View } from 'react-native';

import {
  ROLE_AVATAR_CLASS,
  ROLE_BADGE_CLASS,
  ROLE_LABELS,
  getGreeting,
} from '@/content/homeContent';
import { initialsFrom } from '@/lib/name';
import type { UserRole } from '@/types/auth';

type WelcomeHeaderProps = {
  displayName: string;
  role: UserRole;
};

export function WelcomeHeader({ displayName, role }: WelcomeHeaderProps) {
  return (
    <View className="flex-row items-center gap-4">
      <View
        className={`h-14 w-14 items-center justify-center rounded-full ${ROLE_AVATAR_CLASS[role]}`}
      >
        <Text className="text-xl font-bold text-white">{initialsFrom(displayName)}</Text>
      </View>

      <View className="flex-1">
        <Text className="text-sm text-slate-500">{getGreeting()}</Text>
        <Text className="mt-0.5 text-xl font-bold text-slate-900" numberOfLines={1}>
          {displayName}
        </Text>
        <View
          className={`mt-1.5 self-start rounded-full px-2.5 py-1 ${ROLE_BADGE_CLASS[role]}`}
        >
          <Text className="text-xs font-semibold">{ROLE_LABELS[role]}</Text>
        </View>
      </View>
    </View>
  );
}