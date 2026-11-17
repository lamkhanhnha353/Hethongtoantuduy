import { View, Text, TouchableOpacity } from 'react-native';
import { Ionicons } from '@expo/vector-icons';
import { router } from 'expo-router';
type BannerProps = {
    student: {
        fullName: string;
        avatar?: string | null;
    };
};
import { Avatar } from '@/components/Avatar';

export function Banner({ student, }: BannerProps) {
    return (
        <View className="relative z-10 flex-row items-center gap-3">
            <TouchableOpacity
                accessibilityLabel="Quay lại"
                accessibilityRole="button"
                activeOpacity={0.5}
                className="h-10 w-10 items-center justify-center rounded-full border border-white/30 bg-white/20"
                onPress={() => router.back()}  >
                <Ionicons color="white" name="chevron-back" size={24} />
            </TouchableOpacity>

            <View className="flex-1">
                <Text className="text-sm text-indigo-100">Toán vui</Text>
                <Text className="text-lg font-bold leading-tight text-white" numberOfLines={1}>
                    {student.fullName}
                </Text>
            </View>
            <Avatar name={student.fullName} size={44} uri={student.avatar} />
        </View>
    )
}