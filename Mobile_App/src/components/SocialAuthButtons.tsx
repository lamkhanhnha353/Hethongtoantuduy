import type { ReactNode } from 'react';
import { Text, TouchableOpacity, View } from 'react-native';

import { FacebookLogo, GoogleLogo } from '@/components/brand/BrandLogo';

export type SocialProviderId = 'google' | 'facebook';

type SocialAuthButtonsProps = {
  disabled?: boolean;
  onPressProvider: (provider: SocialProviderId) => void;
};

const FACEBOOK_BRAND_COLOR = '#1877f2';

type ProviderConfig = {
  id: SocialProviderId;
  label: string;
  logo: ReactNode;
  buttonClass: string;
  textClass: string;
};

const PROVIDERS: ProviderConfig[] = [
  {
    id: 'google',
    label: 'Tiếp tục với Google',
    logo: <GoogleLogo size={20} />,
    buttonClass: 'border border-slate-300 bg-white',
    textClass: 'text-slate-800',
  },
  {
    id: 'facebook',
    label: 'Tiếp tục với Facebook',
    logo: <FacebookLogo size={20} />,
    buttonClass: 'border border-transparent',
    textClass: 'text-white',
  },
];

export function Divider({ label }: { label: string }) {
  return (
    <View className="my-6 flex-row items-center gap-3">
      <View className="h-px flex-1 bg-slate-200" />
      <Text className="text-xs uppercase tracking-wide text-slate-400">{label}</Text>
      <View className="h-px flex-1 bg-slate-200" />
    </View>
  );
}

export function SocialAuthButtons({ disabled = false, onPressProvider }: SocialAuthButtonsProps) {
  return (
    <View className="gap-3">
      {PROVIDERS.map((provider) => {
        const isFacebook = provider.id === 'facebook';

        return (
          <TouchableOpacity
            key={provider.id}
            accessibilityLabel={provider.label}
            accessibilityRole="button"
            accessibilityState={{ disabled }}
            activeOpacity={0.85}
            className={`w-full flex-row items-center justify-center gap-3 rounded-xl px-4 py-3.5 ${provider.buttonClass} ${
              disabled ? 'opacity-50' : ''
            }`}
            disabled={disabled}
            onPress={() => onPressProvider(provider.id)}
            style={isFacebook ? { backgroundColor: FACEBOOK_BRAND_COLOR } : undefined}
          >
            {provider.logo}
            <Text className={`text-base font-semibold ${provider.textClass}`}>
              {provider.label}
            </Text>
          </TouchableOpacity>
        );
      })}
    </View>
  );
}